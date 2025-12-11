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
using SurveyBot.Tests.Infrastructure;

namespace SurveyBot.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for ResponsesController HTTP endpoints.
/// Tests response submission, answer saving, completion, and listing.
/// </summary>
/// <remarks>
/// TEST-FLAKY-AUTH-003 (Phase 2): No longer uses IClassFixture pattern.
/// Factory is created per test in IntegrationTestBase.InitializeAsync() for complete isolation.
/// </remarks>
public class ResponsesControllerIntegrationTests : IntegrationTestBase
{
    // No constructor needed - factory is created per test in InitializeAsync()

    [Fact]
    public async Task StartResponse_ForActiveSurvey_Success()
    {
        // Arrange
        Factory.ClearDatabase();
        int surveyId = 0;

        Factory.SeedDatabase(db =>
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

        var response = await Client.PostAsJsonAsync($"/api/surveys/{surveyId}/responses", createDto);

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
        Factory.ClearDatabase();
        int responseId = 0, questionId = 0;

        Factory.SeedDatabase(db =>
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

        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/answers", submitDto);

        // Assert - API may return Created for new answer or OK for existing answer update (upsert behavior)
        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.Created, HttpStatusCode.OK },
            because: "API performs upsert - returns Created for new answer or OK for existing answer update");

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AnswerDto>>();
        result!.Data!.QuestionId.Should().Be(questionId);
        result.Data.AnswerText.Should().Be("My answer text");
    }

    [Fact]
    public async Task CompleteResponse_AfterAnsweringQuestions_Success()
    {
        // Arrange
        Factory.ClearDatabase();
        int responseId = 0;

        Factory.SeedDatabase(db =>
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
        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/complete", completeDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ResponseDto>>();
        result!.Data!.IsComplete.Should().BeTrue();
        result.Data.SubmittedAt.Should().NotBeNull();
        result.Data.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ListResponses_WithPagination_Success()
    {
        // Arrange
        Factory.ClearDatabase();
        int surveyId = 0;

        Factory.SeedDatabase(db =>
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
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Get all responses
        var allResponse = await Client.GetAsync($"/api/surveys/{surveyId}/responses?pageSize=10");
        var allResult = await allResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResultDto<ResponseDto>>>();

        // Act - Get only completed responses
        var completedResponse = await Client.GetAsync($"/api/surveys/{surveyId}/responses?completedOnly=true");
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
        Factory.ClearDatabase();
        int responseId = 0;

        Factory.SeedDatabase(db =>
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
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync($"/api/responses/{responseId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ResponseDto>>();
        result!.Data!.Answers.Should().HaveCount(2);
        result.Data.Answers.Should().Contain(a => a.AnswerText == "Answer 1");
        result.Data.Answers.Should().Contain(a => a.AnswerText == "Answer 2");
    }

    #region Location Answer Validation Tests

    [Fact]
    public async Task SaveAnswer_WithValidLocationJson_PassesValidation()
    {
        // Arrange
        Factory.ClearDatabase();
        int responseId = 0, questionId = 0;

        Factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            // Create a required location question
            var question = EntityBuilder.CreateQuestion(
                surveyId: survey.Id,
                questionText: "Where are you located?",
                questionType: QuestionType.Location,
                isRequired: true);
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act - Submit valid location answer
        var submitDto = new SubmitAnswerDto
        {
            Answer = new CreateAnswerDto
            {
                QuestionId = questionId,
                AnswerJson = "{\"latitude\":40.7128,\"longitude\":-74.0060,\"accuracy\":10.0,\"timestamp\":\"2025-11-27T10:00:00Z\"}"
            }
        };

        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/answers", submitDto);

        // Assert - API may return Created (201) for new answer or OK (200) for update (upsert behavior)
        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.Created, HttpStatusCode.OK },
            because: "valid location JSON should pass validation - API performs upsert");

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AnswerDto>>();
        result!.Data.Should().NotBeNull();
        result.Data!.QuestionId.Should().Be(questionId);
        result.Data.Latitude.Should().Be(40.7128);
        result.Data.Longitude.Should().Be(-74.0060);
        result.Data.LocationAccuracy.Should().Be(10.0);
    }

    [Fact]
    public async Task SaveAnswer_LocationQuestion_MissingAnswerJson_FailsValidation()
    {
        // Arrange
        Factory.ClearDatabase();
        int responseId = 0, questionId = 0;

        Factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            // Create a required location question
            var question = EntityBuilder.CreateQuestion(
                surveyId: survey.Id,
                questionText: "Where are you located?",
                questionType: QuestionType.Location,
                isRequired: true);
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act - Submit without answerJson (null)
        var submitDto = new SubmitAnswerDto
        {
            Answer = new CreateAnswerDto
            {
                QuestionId = questionId,
                AnswerJson = null // Missing location data
            }
        };

        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/answers", submitDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "missing location answer should fail validation");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Location answer is required", "error message should indicate missing location");
    }

    [Fact]
    public async Task SaveAnswer_LocationQuestion_EmptyAnswerJson_FailsValidation()
    {
        // Arrange
        Factory.ClearDatabase();
        int responseId = 0, questionId = 0;

        Factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            // Create a required location question
            var question = EntityBuilder.CreateQuestion(
                surveyId: survey.Id,
                questionText: "Where are you located?",
                questionType: QuestionType.Location,
                isRequired: true);
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act - Submit with empty string
        var submitDto = new SubmitAnswerDto
        {
            Answer = new CreateAnswerDto
            {
                QuestionId = questionId,
                AnswerJson = "" // Empty location data
            }
        };

        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/answers", submitDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "empty location answer should fail validation");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Location answer is required", "error message should indicate missing location");
    }

    [Fact]
    public async Task SaveAnswer_LocationQuestion_InvalidJson_FailsValidation()
    {
        // Arrange
        Factory.ClearDatabase();
        int responseId = 0, questionId = 0;

        Factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            // Create a required location question
            var question = EntityBuilder.CreateQuestion(
                surveyId: survey.Id,
                questionText: "Where are you located?",
                questionType: QuestionType.Location,
                isRequired: true);
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act - Submit with malformed JSON
        var submitDto = new SubmitAnswerDto
        {
            Answer = new CreateAnswerDto
            {
                QuestionId = questionId,
                AnswerJson = "{invalid json}" // Malformed JSON
            }
        };

        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/answers", submitDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "malformed JSON should fail validation");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid", "error message should indicate validation failure");
    }

    [Fact]
    public async Task SaveAnswer_LocationQuestion_InvalidLatitude_FailsValidation()
    {
        // Arrange
        Factory.ClearDatabase();
        int responseId = 0, questionId = 0;

        Factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            // Create a required location question
            var question = EntityBuilder.CreateQuestion(
                surveyId: survey.Id,
                questionText: "Where are you located?",
                questionType: QuestionType.Location,
                isRequired: true);
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act - Submit with invalid latitude (exceeds valid range of -90 to 90)
        // Note: Using truly invalid coordinates, not missing ones (missing defaults to 0.0 which is valid)
        var submitDto = new SubmitAnswerDto
        {
            Answer = new CreateAnswerDto
            {
                QuestionId = questionId,
                AnswerJson = "{\"latitude\":999.0,\"longitude\":-74.0060}" // Invalid latitude (>90)
            }
        };

        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/answers", submitDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "invalid latitude (999.0) should fail validation");

        var content = await response.Content.ReadAsStringAsync();
        content.ToLower().Should().Contain("latitude", "error message should mention latitude");
    }

    [Fact]
    public async Task SaveAnswer_LocationQuestion_InvalidLongitude_FailsValidation()
    {
        // Arrange
        Factory.ClearDatabase();
        int responseId = 0, questionId = 0;

        Factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            // Create a required location question
            var question = EntityBuilder.CreateQuestion(
                surveyId: survey.Id,
                questionText: "Where are you located?",
                questionType: QuestionType.Location,
                isRequired: true);
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act - Submit with invalid longitude (exceeds valid range of -180 to 180)
        // Note: Using truly invalid coordinates, not missing ones (missing defaults to 0.0 which is valid)
        var submitDto = new SubmitAnswerDto
        {
            Answer = new CreateAnswerDto
            {
                QuestionId = questionId,
                AnswerJson = "{\"latitude\":40.7128,\"longitude\":-999.0}" // Invalid longitude (<-180)
            }
        };

        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/answers", submitDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "invalid longitude (-999.0) should fail validation");

        var content = await response.Content.ReadAsStringAsync();
        content.ToLower().Should().Contain("longitude", "error message should mention longitude");
    }

    [Fact]
    public async Task SaveAnswer_LocationQuestion_InvalidLatitudeRange_FailsValidation()
    {
        // Arrange
        Factory.ClearDatabase();
        int responseId = 0, questionId = 0;

        Factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            // Create a required location question
            var question = EntityBuilder.CreateQuestion(
                surveyId: survey.Id,
                questionText: "Where are you located?",
                questionType: QuestionType.Location,
                isRequired: true);
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act - Submit with invalid latitude (> 90)
        var submitDto = new SubmitAnswerDto
        {
            Answer = new CreateAnswerDto
            {
                QuestionId = questionId,
                AnswerJson = "{\"latitude\":95.0,\"longitude\":-74.0060}" // Invalid latitude
            }
        };

        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/answers", submitDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "invalid latitude range should fail validation");

        var content = await response.Content.ReadAsStringAsync();
        content.ToLower().Should().Contain("latitude", "error message should mention latitude");
    }

    [Fact]
    public async Task SaveAnswer_LocationQuestion_InvalidLongitudeRange_FailsValidation()
    {
        // Arrange
        Factory.ClearDatabase();
        int responseId = 0, questionId = 0;

        Factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            // Create a required location question
            var question = EntityBuilder.CreateQuestion(
                surveyId: survey.Id,
                questionText: "Where are you located?",
                questionType: QuestionType.Location,
                isRequired: true);
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act - Submit with invalid longitude (> 180)
        var submitDto = new SubmitAnswerDto
        {
            Answer = new CreateAnswerDto
            {
                QuestionId = questionId,
                AnswerJson = "{\"latitude\":40.7128,\"longitude\":185.0}" // Invalid longitude
            }
        };

        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/answers", submitDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "invalid longitude range should fail validation");

        var content = await response.Content.ReadAsStringAsync();
        content.ToLower().Should().Contain("longitude", "error message should mention longitude");
    }

    [Fact]
    public async Task SaveAnswer_LocationQuestion_NotRequired_EmptyAnswer_PassesValidation()
    {
        // Arrange
        Factory.ClearDatabase();
        int responseId = 0, questionId = 0;

        Factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            // Create an optional location question
            var question = EntityBuilder.CreateQuestion(
                surveyId: survey.Id,
                questionText: "Where are you located (optional)?",
                questionType: QuestionType.Location,
                isRequired: false); // Not required
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act - Submit without answerJson for optional question
        var submitDto = new SubmitAnswerDto
        {
            Answer = new CreateAnswerDto
            {
                QuestionId = questionId,
                AnswerJson = null // No location provided for optional question
            }
        };

        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/answers", submitDto);

        // Assert - API may return Created (201) for new answer or OK (200) for update
        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.Created, HttpStatusCode.OK },
            because: "optional location question should allow empty answer");

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AnswerDto>>();
        result!.Data.Should().NotBeNull();
        result.Data!.QuestionId.Should().Be(questionId);
    }

    [Fact]
    public async Task SaveAnswer_LocationQuestion_WithOptionalFields_PassesValidation()
    {
        // Arrange
        Factory.ClearDatabase();
        int responseId = 0, questionId = 0;

        Factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();

            // Create a required location question
            var question = EntityBuilder.CreateQuestion(
                surveyId: survey.Id,
                questionText: "Where are you located?",
                questionType: QuestionType.Location,
                isRequired: true);
            db.Questions.Add(question);
            db.SaveChanges();
            questionId = question.Id;

            var response = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 999888777);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Act - Submit with all optional fields (accuracy, timestamp)
        var submitDto = new SubmitAnswerDto
        {
            Answer = new CreateAnswerDto
            {
                QuestionId = questionId,
                AnswerJson = "{\"latitude\":40.7128,\"longitude\":-74.0060,\"accuracy\":10.5,\"timestamp\":\"2025-11-27T10:00:00Z\"}"
            }
        };

        var response = await Client.PostAsJsonAsync($"/api/responses/{responseId}/answers", submitDto);

        // Assert - API may return Created (201) for new answer or OK (200) for update
        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.Created, HttpStatusCode.OK },
            because: "location with optional fields should pass validation");

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AnswerDto>>();
        result!.Data.Should().NotBeNull();
        result.Data!.QuestionId.Should().Be(questionId);
        result.Data.Latitude.Should().Be(40.7128);
        result.Data.Longitude.Should().Be(-74.0060);
        result.Data.LocationAccuracy.Should().Be(10.5);
        result.Data.LocationTimestamp.Should().NotBeNull();
    }

    #endregion
}
