using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for SurveysController HTTP endpoints.
/// Tests full CRUD cycle, authorization, and validation.
/// </summary>
public class SurveysControllerIntegrationTests : IClassFixture<WebApplicationFactoryFixture<Program>>
{
    private readonly WebApplicationFactoryFixture<Program> _factory;
    private readonly HttpClient _client;

    public SurveysControllerIntegrationTests(WebApplicationFactoryFixture<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private async Task<string> GetAuthTokenAsync(long telegramId = 123456789)
    {
        var loginRequest = new LoginRequestDto { TelegramId = telegramId };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        return result!.Data!.Token;
    }

    [Fact]
    public async Task FullCrudCycle_CreateReadUpdateDelete_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(EntityBuilder.CreateUser(telegramId: 123456789));
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Step 1: CREATE
        var createDto = new CreateSurveyDto
        {
            Title = "Integration Test Survey",
            Description = "Testing CRUD operations",
            IsActive = false
        };

        var createResponse = await _client.PostAsJsonAsync("/api/surveys", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        var surveyId = createResult!.Data!.Id;

        // Step 2: READ
        var getResponse = await _client.GetAsync($"/api/surveys/{surveyId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        getResult!.Data!.Title.Should().Be("Integration Test Survey");

        // Step 3: UPDATE
        var updateDto = new UpdateSurveyDto
        {
            Title = "Updated Survey Title",
            Description = "Updated description"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateResult = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        updateResult!.Data!.Title.Should().Be("Updated Survey Title");

        // Step 4: DELETE
        var deleteResponse = await _client.DeleteAsync($"/api/surveys/{surveyId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var verifyResponse = await _client.GetAsync($"/api/surveys/{surveyId}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateSurvey_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _factory.ClearDatabase();

        // Don't set authorization header
        var createDto = new CreateSurveyDto
        {
            Title = "Test Survey",
            Description = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/surveys", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateSurvey_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        _factory.ClearDatabase();
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(EntityBuilder.CreateUser(telegramId: 123456789));
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Missing required title
        var invalidDto = new CreateSurveyDto
        {
            Title = "", // Invalid: empty title
            Description = "Test"
        };

        var response = await _client.PostAsJsonAsync("/api/surveys", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSurvey_ThatDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _factory.ClearDatabase();
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(EntityBuilder.CreateUser(telegramId: 123456789));
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/surveys/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ToggleSurveyStatus_ActivateAndDeactivate_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: false);
            db.Surveys.Add(survey);
            db.SaveChanges();

            // Add at least one question (required for activation)
            db.Questions.Add(EntityBuilder.CreateQuestion(surveyId: survey.Id));
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get survey ID
        var surveysResponse = await _client.GetAsync("/api/surveys");
        var surveysResult = await surveysResponse.Content.ReadFromJsonAsync<ApiResponse<List<SurveyDto>>>();
        var surveyId = surveysResult!.Data![0].Id;

        // Act - Activate
        var activateDto = new ToggleSurveyStatusDto { IsActive = true };
        var activateResponse = await _client.PatchAsJsonAsync($"/api/surveys/{surveyId}/status", activateDto);

        // Assert - Activation
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var activateResult = await activateResponse.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        activateResult!.Data!.IsActive.Should().BeTrue();

        // Act - Deactivate
        var deactivateDto = new ToggleSurveyStatusDto { IsActive = false };
        var deactivateResponse = await _client.PatchAsJsonAsync($"/api/surveys/{surveyId}/status", deactivateDto);

        // Assert - Deactivation
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deactivateResult = await deactivateResponse.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        deactivateResult!.Data!.IsActive.Should().BeFalse();
    }
}
