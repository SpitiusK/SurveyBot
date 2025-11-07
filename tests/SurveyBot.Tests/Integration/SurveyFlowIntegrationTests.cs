using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.DTOs.Statistics;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Entities;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Integration;

/// <summary>
/// Integration tests for end-to-end survey flow.
/// Tests the complete lifecycle: create survey, add questions, activate, and prevent modifications.
/// </summary>
public class SurveyFlowIntegrationTests : IClassFixture<WebApplicationFactoryFixture<Program>>
{
    private readonly WebApplicationFactoryFixture<Program> _factory;
    private readonly HttpClient _client;

    public SurveyFlowIntegrationTests(WebApplicationFactoryFixture<Program> factory)
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
        return result!.Data!.AccessToken;
    }

    [Fact]
    public async Task CompleteSurveyFlow_CreateAddQuestionsActivate_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(EntityBuilder.CreateUser(telegramId: 123456789));
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Step 1: Create survey
        var createSurveyDto = new CreateSurveyDto
        {
            Title = "Complete Flow Survey",
            Description = "Testing complete flow",
            IsActive = false
        };

        var createResponse = await _client.PostAsJsonAsync("/api/surveys", createSurveyDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        var surveyId = createResult!.Data!.Id;

        // Step 2: Add questions
        var question1 = new CreateQuestionDto
        {
            QuestionText = "What is your name?",
            QuestionType = QuestionType.Text,
            IsRequired = true
        };

        var q1Response = await _client.PostAsJsonAsync($"/api/surveys/{surveyId}/questions", question1);
        q1Response.StatusCode.Should().Be(HttpStatusCode.Created);

        var question2 = new CreateQuestionDto
        {
            QuestionText = "How satisfied are you?",
            QuestionType = QuestionType.SingleChoice,
            IsRequired = true,
            Options = new List<string> { "Very Satisfied", "Satisfied", "Neutral", "Dissatisfied" }
        };

        var q2Response = await _client.PostAsJsonAsync($"/api/surveys/{surveyId}/questions", question2);
        q2Response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Step 3: Verify survey has questions
        var questionsResponse = await _client.GetAsync($"/api/surveys/{surveyId}/questions");
        questionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var questionsResult = await questionsResponse.Content.ReadFromJsonAsync<ApiResponse<List<QuestionDto>>>();
        questionsResult!.Data.Should().HaveCount(2);

        // Step 4: Activate survey
        var toggleDto = new ToggleSurveyStatusDto { IsActive = true };
        var activateResponse = await _client.PatchAsJsonAsync($"/api/surveys/{surveyId}/status", toggleDto);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Verify survey is active
        var surveyResponse = await _client.GetAsync($"/api/surveys/{surveyId}");
        var surveyResult = await surveyResponse.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        surveyResult!.Data!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateSurvey_ThenTryToModify_ShouldFail()
    {
        // Arrange
        _factory.ClearDatabase();
        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            db.Questions.Add(EntityBuilder.CreateQuestion(surveyId: survey.Id, questionText: "Question 1"));
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get survey ID
        var surveysResponse = await _client.GetAsync("/api/surveys");
        var surveysResult = await surveysResponse.Content.ReadFromJsonAsync<ApiResponse<List<SurveyDto>>>();
        var surveyId = surveysResult!.Data![0].Id;

        // Act: Try to add question to active survey
        var newQuestion = new CreateQuestionDto
        {
            QuestionText = "New Question",
            QuestionType = QuestionType.Text
        };

        var response = await _client.PostAsJsonAsync($"/api/surveys/{surveyId}/questions", newQuestion);

        // Assert: Should fail
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ViewStatistics_AsCreator_ReturnsStatistics()
    {
        // Arrange
        _factory.ClearDatabase();
        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            var question = EntityBuilder.CreateQuestion(surveyId: survey.Id);
            db.Questions.Add(question);
            db.SaveChanges();

            // Add a response
            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, isComplete: true);
            db.Responses.Add(response);
            db.SaveChanges();

            db.Answers.Add(EntityBuilder.CreateAnswer(responseId: response.Id, questionId: question.Id));
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get survey ID
        var surveysResponse = await _client.GetAsync("/api/surveys");
        var surveysResult = await surveysResponse.Content.ReadFromJsonAsync<ApiResponse<List<SurveyDto>>>();
        var surveyId = surveysResult!.Data![0].Id;

        // Act
        var statsResponse = await _client.GetAsync($"/api/surveys/{surveyId}/statistics");

        // Assert
        statsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var statsResult = await statsResponse.Content.ReadFromJsonAsync<ApiResponse<SurveyStatisticsDto>>();
        statsResult.Should().NotBeNull();
        statsResult!.Data.Should().NotBeNull();
        statsResult.Data!.TotalResponses.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ViewOtherUsersSurvey_ShouldFail()
    {
        // Arrange
        _factory.ClearDatabase();
        _factory.SeedDatabase(db =>
        {
            var user1 = EntityBuilder.CreateUser(telegramId: 111111111, username: "user1");
            var user2 = EntityBuilder.CreateUser(telegramId: 222222222, username: "user2");
            db.Users.AddRange(user1, user2);
            db.SaveChanges();

            // Create survey owned by user1
            var survey = EntityBuilder.CreateSurvey(creatorId: user1.Id);
            db.Surveys.Add(survey);
        });

        // Login as user2
        var token = await GetAuthTokenAsync(222222222);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get user1's survey ID
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
        var surveyId = db.Surveys.First().Id;

        // Act: User2 tries to view User1's survey
        var response = await _client.GetAsync($"/api/surveys/{surveyId}");

        // Assert: Should fail (403 Forbidden or 404 Not Found)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSurvey_AsOwner_Success()
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
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get survey ID
        var surveysResponse = await _client.GetAsync("/api/surveys");
        var surveysResult = await surveysResponse.Content.ReadFromJsonAsync<ApiResponse<List<SurveyDto>>>();
        var surveyId = surveysResult!.Data![0].Id;

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/surveys/{surveyId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify survey is deleted
        var getResponse = await _client.GetAsync($"/api/surveys/{surveyId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateSurvey_WhileInactive_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(
                title: "Original Title",
                creatorId: user.Id,
                isActive: false);
            db.Surveys.Add(survey);
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get survey ID
        var surveysResponse = await _client.GetAsync("/api/surveys");
        var surveysResult = await surveysResponse.Content.ReadFromJsonAsync<ApiResponse<List<SurveyDto>>>();
        var surveyId = surveysResult!.Data![0].Id;

        // Act
        var updateDto = new UpdateSurveyDto
        {
            Title = "Updated Title",
            Description = "Updated Description"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}", updateDto);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        result!.Data!.Title.Should().Be("Updated Title");
        result.Data.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task UpdateSurvey_WhileActive_ShouldFail()
    {
        // Arrange
        _factory.ClearDatabase();
        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(
                title: "Original Title",
                creatorId: user.Id,
                isActive: true);
            db.Surveys.Add(survey);
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get survey ID
        var surveysResponse = await _client.GetAsync("/api/surveys");
        var surveysResult = await surveysResponse.Content.ReadFromJsonAsync<ApiResponse<List<SurveyDto>>>();
        var surveyId = surveysResult!.Data![0].Id;

        // Act
        var updateDto = new UpdateSurveyDto
        {
            Title = "Updated Title",
            Description = "Updated Description"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}", updateDto);

        // Assert: Should fail because survey is active
        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListSurveys_AsUser_ReturnsOnlyOwnSurveys()
    {
        // Arrange
        _factory.ClearDatabase();
        _factory.SeedDatabase(db =>
        {
            var user1 = EntityBuilder.CreateUser(telegramId: 111111111);
            var user2 = EntityBuilder.CreateUser(telegramId: 222222222);
            db.Users.AddRange(user1, user2);
            db.SaveChanges();

            // Create surveys for both users
            db.Surveys.Add(EntityBuilder.CreateSurvey(title: "User 1 Survey", creatorId: user1.Id));
            db.Surveys.Add(EntityBuilder.CreateSurvey(title: "User 2 Survey", creatorId: user2.Id));
        });

        // Login as user1
        var token = await GetAuthTokenAsync(111111111);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/surveys");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SurveyDto>>>();
        result!.Data.Should().HaveCount(1);
        result.Data![0].Title.Should().Be("User 1 Survey");
    }
}
