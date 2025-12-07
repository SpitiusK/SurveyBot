using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests that provides database isolation and common utilities.
/// Each test starts with a clean database to ensure test independence.
/// </summary>
/// <remarks>
/// This base class addresses TEST-FAIL-001: Database State Pollution issue.
/// By calling ClearDatabase() in the constructor, each test gets a fresh database state.
/// </remarks>
public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactoryFixture<Program>>
{
    protected readonly WebApplicationFactoryFixture<Program> Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(WebApplicationFactoryFixture<Program> factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Clear database before each test to ensure isolation
        ClearDatabase();
    }

    /// <summary>
    /// Clears all data from the database to ensure test isolation.
    /// </summary>
    protected void ClearDatabase()
    {
        Factory.ClearDatabase();
    }

    /// <summary>
    /// Seeds the database with test data using the provided action.
    /// </summary>
    protected void SeedDatabase(Action<SurveyBot.Infrastructure.Data.SurveyBotDbContext> seedAction)
    {
        Factory.SeedDatabase(seedAction);
    }

    /// <summary>
    /// Gets an authentication token for test requests with proper null-safety.
    /// Throws descriptive exceptions if login fails instead of NullReferenceException.
    /// </summary>
    /// <param name="telegramId">The Telegram ID to use for login. Defaults to 123456789.</param>
    /// <returns>The JWT token for authentication.</returns>
    /// <exception cref="InvalidOperationException">Thrown when login fails or token is null.</exception>
    /// <remarks>
    /// This method addresses TEST-FAIL-001: Null-Safety in Test Helpers issue.
    /// It validates HTTP status code and response structure before accessing the token.
    /// </remarks>
    protected async Task<string> GetAuthTokenAsync(long telegramId = 123456789)
    {
        var loginRequest = new LoginRequestDto { TelegramId = telegramId };
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // 1. Validate HTTP status code
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Login failed with status {response.StatusCode}. " +
                $"Telegram ID: {telegramId}. " +
                $"Response: {errorContent}");
        }

        // 2. Deserialize with null-safety
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();

        // 3. Validate response structure
        if (result == null)
        {
            throw new InvalidOperationException(
                $"Login response deserialization returned null for Telegram ID: {telegramId}");
        }

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Login failed: {result.Message} for Telegram ID: {telegramId}");
        }

        if (result.Data?.Token == null)
        {
            throw new InvalidOperationException(
                $"Login succeeded but token is null for Telegram ID: {telegramId}");
        }

        return result.Data.Token;
    }

    /// <summary>
    /// Sets the authorization header with a JWT token for subsequent requests.
    /// </summary>
    protected async Task AuthenticateAsync(long telegramId = 123456789)
    {
        var token = await GetAuthTokenAsync(telegramId);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Clears the authorization header.
    /// </summary>
    protected void ClearAuthentication()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }
}
