using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Answer;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Core.DTOs.Common;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for ResponsesController HTTP endpoints.
/// Tests response submission, answer saving, completion, and listing.
/// </summary>
public class ResponsesControllerIntegrationTests : IClassFixture<WebApplicationFactoryFixture<Program>>
{
    private readonly WebApplicationFactoryFixture<Program> _factory;
    private readonly HttpClient _client;

    public ResponsesControllerIntegrationTests(WebApplicationFactoryFixture<Program> factory)
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
    public async Task StartResponse_ForActiveSurvey_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        int surveyId = 0;

        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();
            surveyId = survey.Id;

            db.Questions.Add(EntityBuilder.CreateQuestion(surveyId: survey.Id));
        });

        // Act - No auth required for starting response
        var createDto = new CreateResponseDto
        {
            RespondentTelegramId = 999888777,
            RespondentUsername = "respondent",
            RespondentFirstName = "John"
        };

        var response = await _client.PostAsJsonAsync($"/api/surveys/{surveyId}/responses", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ResponseDto>>();
        result!.Data!.RespondentTelegramId.Should().Be(999888777);
        result.Data.IsComplete.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAnswer_ToResponse_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        int responseId = 0, questionId = 0;

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
            questionId = question.Id;

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act
        var submitDto = new SubmitAnswerDto
        {
            Answer = new CreateAnswerDto
            {
                QuestionId = questionId,
                AnswerText = "My answer text"
            }
        };

        var response = await _client.PostAsJsonAsync($"/api/responses/{responseId}/answers", submitDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AnswerDto>>();
        result!.Data!.QuestionId.Should().Be(questionId);
        result.Data.AnswerText.Should().Be("My answer text");
    }

    [Fact]
    public async Task CompleteResponse_AfterAnsweringQuestions_Success()
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
        result.Data.CompletedAt.Should().NotBeNull();
        result.Data.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ListResponses_WithPagination_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        int surveyId = 0;

        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();
            surveyId = survey.Id;

            // Create 5 responses
            for (int i = 0; i < 5; i++)
            {
                db.Responses.Add(EntityBuilder.CreateResponse(
                    surveyId: survey.Id,
                    respondentTelegramId: 900000000 + i,
                    isComplete: i < 3)); // First 3 are complete
            }
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Get all responses
        var allResponse = await _client.GetAsync($"/api/surveys/{surveyId}/responses?pageSize=10");
        var allResult = await allResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResultDto<ResponseDto>>>();

        // Act - Get only completed responses
        var completedResponse = await _client.GetAsync($"/api/surveys/{surveyId}/responses?completedOnly=true");
        var completedResult = await completedResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResultDto<ResponseDto>>>();

        // Assert
        allResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        allResult!.Data!.Items.Should().HaveCount(5);

        completedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        completedResult!.Data!.Items.Should().HaveCount(3);
        completedResult.Data.Items.Should().OnlyContain(r => r.IsComplete);
    }

    [Fact]
    public async Task GetResponseById_WithAnswers_Success()
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

            var question1 = EntityBuilder.CreateQuestion(surveyId: survey.Id, questionText: "Question 1");
            var question2 = EntityBuilder.CreateQuestion(surveyId: survey.Id, questionText: "Question 2");
            db.Questions.AddRange(question1, question2);
            db.SaveChanges();

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;

            db.Answers.Add(EntityBuilder.CreateAnswer(responseId: response.Id, questionId: question1.Id, answerText: "Answer 1"));
            db.Answers.Add(EntityBuilder.CreateAnswer(responseId: response.Id, questionId: question2.Id, answerText: "Answer 2"));
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/responses/{responseId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ResponseDto>>();
        result!.Data!.Answers.Should().HaveCount(2);
        result.Data.Answers.Should().Contain(a => a.AnswerText == "Answer 1");
        result.Data.Answers.Should().Contain(a => a.AnswerText == "Answer 2");
    }
}
