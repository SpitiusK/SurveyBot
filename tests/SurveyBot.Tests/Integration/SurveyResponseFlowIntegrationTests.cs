using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Answer;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Entities;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Integration;

/// <summary>
/// Integration tests for survey response submission flow.
/// Tests the complete response lifecycle: start response, save answers, complete response.
/// </summary>
public class SurveyResponseFlowIntegrationTests : IClassFixture<WebApplicationFactoryFixture<Program>>
{
    private readonly WebApplicationFactoryFixture<Program> _factory;
    private readonly HttpClient _client;

    public SurveyResponseFlowIntegrationTests(WebApplicationFactoryFixture<Program> factory)
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
    public async Task StartResponse_WithValidSurvey_ReturnsResponseId()
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

            db.Questions.Add(EntityBuilder.CreateQuestion(surveyId: survey.Id));
        });

        // Get survey ID
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
            var surveyId = db.Surveys.First().Id;

            // Act - No authentication required for starting a response
            var createResponseDto = new CreateResponseDto
            {
                RespondentTelegramId = 999888777,
                RespondentUsername = "respondent"
            };

            var response = await _client.PostAsJsonAsync($"/api/surveys/{surveyId}/responses", createResponseDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ResponseDto>>();
            result.Should().NotBeNull();
            result!.Data.Should().NotBeNull();
            result.Data!.Id.Should().BeGreaterThan(0);
            result.Data.IsComplete.Should().BeFalse();
            result.Data.RespondentTelegramId.Should().Be(999888777);
        }
    }

    [Fact]
    public async Task SubmitAnswers_ForResponse_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        int surveyId = 0, questionId = 0, responseId = 0;

        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();
            surveyId = survey.Id;

            var question = EntityBuilder.CreateQuestion(surveyId: survey.Id, questionText: "Your name?");
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act
        var submitAnswerDto = new SubmitAnswerDto
        {
            Answer = new CreateAnswerDto
            {
                QuestionId = questionId,
                AnswerText = "John Doe"
            }
        };

        var response = await _client.PostAsJsonAsync($"/api/responses/{responseId}/answers", submitAnswerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AnswerDto>>();
        result!.Data!.AnswerText.Should().Be("John Doe");
    }

    [Fact]
    public async Task CompleteResponse_WithAllRequiredAnswers_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        int responseId = 0;

        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            var question = EntityBuilder.CreateQuestion(surveyId: survey.Id, isRequired: true);
            db.Questions.Add(question);
            db.SaveChanges();

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;

            // Add answer for required question
            db.Answers.Add(EntityBuilder.CreateAnswer(responseId: response.Id, questionId: question.Id));
        });

        // Act
        var completeDto = new CompleteResponseDto();
        var response = await _client.PostAsJsonAsync($"/api/responses/{responseId}/complete", completeDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ResponseDto>>();
        result!.Data!.IsComplete.Should().BeTrue();
        result.Data.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteResponse_Twice_ShouldFail()
    {
        // Arrange
        _factory.ClearDatabase();
        int responseId = 0;

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

            // Create already completed response
            var response = EntityBuilder.CreateResponse(
                surveyId: survey.Id,
                respondentTelegramId: 999888777,
                isComplete: true);
            response.SetSubmittedAt(DateTime.UtcNow);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;

            db.Answers.Add(EntityBuilder.CreateAnswer(responseId: response.Id, questionId: question.Id));
        });

        // Act - Try to complete again
        var completeDto = new CompleteResponseDto();
        var response = await _client.PostAsJsonAsync($"/api/responses/{responseId}/complete", completeDto);

        // Assert - Should fail
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StatisticsUpdated_AfterResponseComplete_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        int surveyId = 0, responseId = 0;

        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();
            surveyId = survey.Id;

            var question = EntityBuilder.CreateQuestion(surveyId: survey.Id);
            db.Questions.Add(question);
            db.SaveChanges();

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;

            db.Answers.Add(EntityBuilder.CreateAnswer(responseId: response.Id, questionId: question.Id));
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Complete the response
        var completeDto = new CompleteResponseDto();
        await _client.PostAsJsonAsync($"/api/responses/{responseId}/complete", completeDto);

        // Get statistics
        var statsResponse = await _client.GetAsync($"/api/surveys/{surveyId}/statistics");

        // Assert
        statsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var statsResult = await statsResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        statsResult.Should().NotBeNull();
        statsResult!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task StartResponse_OnInactiveSurvey_ShouldFail()
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

        // Get survey ID
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
        var surveyId = db.Surveys.First().Id;

        // Act
        var createResponseDto = new CreateResponseDto
        {
            RespondentTelegramId = 999888777
        };

        var response = await _client.PostAsJsonAsync($"/api/surveys/{surveyId}/responses", createResponseDto);

        // Assert - Should fail because survey is inactive
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
