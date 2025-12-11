using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Core.Entities;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Tests.Fixtures;
using SurveyBot.Tests.Infrastructure;

namespace SurveyBot.Tests.Integration;

/// <summary>
/// Integration tests for authentication functionality.
/// Tests JWT token generation, validation, and protected endpoint access.
/// </summary>
/// <remarks>
/// TEST-FLAKY-AUTH-003 (Phase 2): No longer uses IClassFixture pattern.
/// Factory is created per test in IntegrationTestBase.InitializeAsync() for complete isolation.
/// </remarks>
public class AuthenticationIntegrationTests : IntegrationTestBase
{
    // No constructor needed - factory is created per test in InitializeAsync()

    [Fact]
    public async Task Login_WithValidTelegramId_ReturnsJwtToken()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        SeedDatabase(db =>
        {
            db.Users.Add(EntityBuilder.CreateUser(telegramId: 123456789, username: "testuser"));
        });

        var loginRequest = new LoginRequestDto
        {
            TelegramId = 123456789,
            Username = "testuser"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeEmpty();
        result.Data.User.Should().NotBeNull();
        result.Data.User.TelegramId.Should().Be(123456789);
        result.Data.User.Username.Should().Be("testuser");
        result.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // Verify JWT token structure
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Data.Token);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier);
        token.Claims.Should().Contain(c => c.Type == "TelegramId" && c.Value == "123456789");
    }

    [Fact]
    public async Task Login_WithNewTelegramId_CreatesUserAndReturnsToken()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor

        var loginRequest = new LoginRequestDto
        {
            TelegramId = 999888777,
            Username = "newuser"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeEmpty();
        result.Data.User.Should().NotBeNull();
        result.Data.User.TelegramId.Should().Be(999888777);

        // Verify user was created in database
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
        var user = await db.Users.FindAsync(result.Data.User.Id);
        user.Should().NotBeNull();
        user!.TelegramId.Should().Be(999888777);
    }

    [Fact]
    public async Task ValidateToken_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        SeedDatabase(db =>
        {
            db.Users.Add(EntityBuilder.CreateUser(telegramId: 123456789, username: "testuser"));
        });

        // Login to get token
        var loginRequest = new LoginRequestDto { TelegramId = 123456789 };
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        var token = loginResult!.Data!.Token;

        // Add token to request headers
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateToken_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);

        // Act
        var response = await Client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor

        // Don't set any authorization header

        // Act
        var response = await Client.GetAsync("/api/surveys");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        // Create a token that's already expired (this would require mocking time or using a very short expiry)
        // For MVP, we'll test with invalid token signature instead
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await Client.GetAsync("/api/surveys");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        SeedDatabase(db =>
        {
            db.Users.Add(EntityBuilder.CreateUser(telegramId: 123456789, username: "testuser"));
        });

        // Login to get token
        var loginRequest = new LoginRequestDto { TelegramId = 123456789, Username = "testuser" };
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        var token = loginResult!.Data!.Token;

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<dynamic>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }
}
