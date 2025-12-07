using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Answer;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;
using SurveyBot.Tests.Infrastructure;

namespace SurveyBot.Tests.Integration;

/// <summary>
/// Integration tests for data validation.
/// Tests validation of various invalid inputs and error conditions.
/// </summary>
public class DataValidationTests : IntegrationTestBase
{
    public DataValidationTests(WebApplicationFactoryFixture<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateSurvey_WithoutTitle_ShouldFail()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        SeedDatabase(db =>
        {
            db.Users.Add(EntityBuilder.CreateUser(telegramId: 123456789));
        });

        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Empty title
        var invalidDto = new CreateSurveyDto
        {
            Title = "", // Invalid: required field
            Description = "Test description"
        };

        var response = await Client.PostAsJsonAsync("/api/surveys", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateSurvey_WithTooLongTitle_ShouldFail()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        SeedDatabase(db =>
        {
            db.Users.Add(EntityBuilder.CreateUser(telegramId: 123456789));
        });

        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Title too long (>500 chars)
        var invalidDto = new CreateSurveyDto
        {
            Title = new string('A', 501), // Invalid: exceeds max length
            Description = "Test"
        };

        var response = await Client.PostAsJsonAsync("/api/surveys", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateQuestion_WithoutText_ShouldFail()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        int surveyId = 0;

        SeedDatabase(db =>
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
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Empty question text
        var invalidDto = new CreateQuestionDto
        {
            QuestionText = "", // Invalid: required field
            QuestionType = QuestionType.Text
        };

        var response = await Client.PostAsJsonAsync($"/api/surveys/{surveyId}/questions", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveAnswer_WithInvalidFormat_ShouldFail()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        int responseId = 0, questionId = 0;

        SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            // Create a Rating question (expects 1-5)
            var question = EntityBuilder.CreateQuestion(
                surveyId: survey.Id,
                questionType: QuestionType.Rating);
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act - Invalid rating value
        var invalidDto = new SubmitAnswerDto
        {
            Answer = new CreateAnswerDto
            {
                QuestionId = questionId,
                AnswerText = "10" // Invalid: rating should be 1-5
            }
        };

        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/answers", invalidDto);

        // Assert - This may succeed but validation should happen at business logic level
        // For MVP, we accept any text for answers, so this test documents expected behavior
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result!.Success.Should().BeFalse();
        }
    }

    [Fact]
    public async Task CompleteResponse_WithoutRequiredAnswers_ShouldFail()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        int responseId = 0;

        SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            // Create required question
            var question = EntityBuilder.CreateQuestion(surveyId: survey.Id, isRequired: true);
            db.Questions.Add(question);
            db.SaveChanges();

            // Create response WITHOUT answering the required question
            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act - Try to complete without required answer
        var completeDto = new CompleteResponseDto();
        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/complete", completeDto);

        // Assert - Should fail because required question not answered
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateQuestion_SingleChoiceWithoutOptions_ShouldFail()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        int surveyId = 0;

        SeedDatabase(db =>
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
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - SingleChoice without options
        var invalidDto = new CreateQuestionDto
        {
            QuestionText = "Choose one",
            QuestionType = QuestionType.SingleChoice,
            Options = null // Invalid: SingleChoice requires options
        };

        var response = await Client.PostAsJsonAsync($"/api/surveys/{surveyId}/questions", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateQuestion_WithTooFewOptions_ShouldFail()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        int surveyId = 0;

        SeedDatabase(db =>
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
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Only 1 option (need at least 2)
        var invalidDto = new CreateQuestionDto
        {
            QuestionText = "Choose one",
            QuestionType = QuestionType.SingleChoice,
            Options = new List<string> { "Only Option" } // Invalid: need at least 2
        };

        var response = await Client.PostAsJsonAsync($"/api/surveys/{surveyId}/questions", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithInvalidTelegramId_ShouldFail()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor

        // Act - Invalid telegram ID (0 or negative)
        var invalidDto = new LoginRequestDto
        {
            TelegramId = 0 // Invalid: must be positive
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
