using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for QuestionsController HTTP endpoints.
/// Tests question CRUD operations, type validation, and ordering.
/// </summary>
public class QuestionsControllerIntegrationTests : IClassFixture<WebApplicationFactoryFixture<Program>>
{
    private readonly WebApplicationFactoryFixture<Program> _factory;
    private readonly HttpClient _client;

    public QuestionsControllerIntegrationTests(WebApplicationFactoryFixture<Program> factory)
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
    public async Task AddQuestion_ToInactiveSurvey_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        int surveyId = 0;

        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: false);
            db.Surveys.Add(survey);
            db.SaveChanges();
            surveyId = survey.Id;
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var createDto = new CreateQuestionDto
        {
            QuestionText = "What is your favorite color?",
            QuestionType = QuestionType.Text,
            IsRequired = true
        };

        var response = await _client.PostAsJsonAsync($"/api/surveys/{surveyId}/questions", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<QuestionDto>>();
        result!.Data!.QuestionText.Should().Be("What is your favorite color?");
        result.Data.QuestionType.Should().Be(QuestionType.Text);
    }

    [Fact]
    public async Task UpdateQuestion_WithValidData_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        int surveyId = 0, questionId = 0;

        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: false);
            db.Surveys.Add(survey);
            db.SaveChanges();
            surveyId = survey.Id;

            var question = EntityBuilder.CreateQuestion(
                surveyId: survey.Id,
                questionText: "Original Question");
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var updateDto = new UpdateQuestionDto
        {
            QuestionText = "Updated Question Text",
            IsRequired = false
        };

        var response = await _client.PutAsJsonAsync($"/api/questions/{questionId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<QuestionDto>>();
        result!.Data!.QuestionText.Should().Be("Updated Question Text");
        result.Data.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteQuestion_FromInactiveSurvey_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        int questionId = 0;

        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: false);
            db.Surveys.Add(survey);
            db.SaveChanges();

            var question = EntityBuilder.CreateQuestion(surveyId: survey.Id);
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/questions/{questionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateQuestion_WithInvalidType_ReturnsBadRequest()
    {
        // Arrange
        _factory.ClearDatabase();
        int surveyId = 0;

        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: false);
            db.Surveys.Add(survey);
            db.SaveChanges();
            surveyId = survey.Id;
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - SingleChoice question without options
        var invalidDto = new CreateQuestionDto
        {
            QuestionText = "Choose one",
            QuestionType = QuestionType.SingleChoice,
            IsRequired = true,
            Options = null // Invalid: SingleChoice requires options
        };

        var response = await _client.PostAsJsonAsync($"/api/surveys/{surveyId}/questions", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderQuestions_InSurvey_Success()
    {
        // Arrange
        _factory.ClearDatabase();
        int surveyId = 0;
        var questionIds = new List<int>();

        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: false);
            db.Surveys.Add(survey);
            db.SaveChanges();
            surveyId = survey.Id;

            // Create 3 questions
            for (int i = 0; i < 3; i++)
            {
                var question = EntityBuilder.CreateQuestion(
                    surveyId: survey.Id,
                    questionText: $"Question {i + 1}",
                    orderIndex: i);
                db.Questions.Add(question);
                db.SaveChanges();
                questionIds.Add(question.Id);
            }
        });

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Reverse the order
        var reorderDto = new ReorderQuestionsDto
        {
            QuestionIds = new List<int> { questionIds[2], questionIds[1], questionIds[0] }
        };

        var response = await _client.PostAsJsonAsync($"/api/surveys/{surveyId}/questions/reorder", reorderDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify new order
        var getResponse = await _client.GetAsync($"/api/surveys/{surveyId}/questions");
        var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<List<QuestionDto>>>();

        getResult!.Data!.Should().HaveCount(3);
        getResult.Data[0].Id.Should().Be(questionIds[2]);
        getResult.Data[1].Id.Should().Be(questionIds[1]);
        getResult.Data[2].Id.Should().Be(questionIds[0]);
    }
}
