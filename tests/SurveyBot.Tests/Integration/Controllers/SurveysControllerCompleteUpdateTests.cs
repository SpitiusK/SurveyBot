using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for PUT /api/surveys/{id}/complete endpoint.
/// Tests the complete survey update functionality with atomic question replacement.
///
/// Test Coverage:
/// - Authentication: Token validation, unauthorized access
/// - Authorization: Ownership validation, forbidden access
/// - Validation: Empty questions, invalid indexes, null data
/// - Success: Complete update, new question IDs, metadata changes
/// - Errors: Survey not found, cycle detection
/// </summary>
public class SurveysControllerCompleteUpdateTests : IClassFixture<WebApplicationFactoryFixture<Program>>
{
    private readonly WebApplicationFactoryFixture<Program> _factory;
    private readonly HttpClient _client;

    public SurveysControllerCompleteUpdateTests(WebApplicationFactoryFixture<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    #region Helper Methods

    /// <summary>
    /// Gets a valid JWT token for the specified Telegram user ID.
    /// </summary>
    private async Task<string> GetAuthTokenAsync(long telegramId = 123456789)
    {
        var loginRequest = new LoginRequestDto { TelegramId = telegramId };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        return result!.Data!.Token;
    }

    /// <summary>
    /// Creates a test survey owned by the specified user.
    /// </summary>
    private Task<int> CreateTestSurveyAsync(int userId, string title = "Test Survey")
    {
        var survey = EntityBuilder.CreateSurvey(title: title, creatorId: userId, isActive: false);

        _factory.SeedDatabase(db =>
        {
            db.Surveys.Add(survey);
        });

        return Task.FromResult(survey.Id);
    }

    /// <summary>
    /// Builds a valid UpdateSurveyWithQuestionsDto with default questions.
    /// </summary>
    private UpdateSurveyWithQuestionsDto BuildValidUpdateDto(
        string title = "Updated Survey",
        int questionCount = 2,
        bool activateAfterUpdate = false)
    {
        var questions = new List<CreateQuestionWithFlowDto>();

        for (int i = 0; i < questionCount; i++)
        {
            questions.Add(new CreateQuestionWithFlowDto
            {
                QuestionText = $"Question {i + 1}",
                QuestionType = QuestionType.Text,
                IsRequired = true,
                OrderIndex = i,
                DefaultNextQuestionIndex = i + 1 < questionCount ? i + 1 : null // Sequential flow
            });
        }

        return new UpdateSurveyWithQuestionsDto
        {
            Title = title,
            Description = "Updated description",
            AllowMultipleResponses = false,
            ShowResults = true,
            ActivateAfterUpdate = activateAfterUpdate,
            Questions = questions
        };
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task UpdateSurveyComplete_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        _factory.ClearDatabase();
        var dto = BuildValidUpdateDto();

        // Don't set Authorization header - no token

        // Act
        var response = await _client.PutAsJsonAsync("/api/surveys/1/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSurveyComplete_WithInvalidToken_Returns401Unauthorized()
    {
        // Arrange
        _factory.ClearDatabase();
        var dto = BuildValidUpdateDto();

        // Set invalid token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token-xyz");

        // Act
        var response = await _client.PutAsJsonAsync("/api/surveys/1/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSurveyComplete_WithExpiredToken_Returns401Unauthorized()
    {
        // Arrange - This test assumes token expiration logic is in place
        // For now, we test with an obviously malformed token
        _factory.ClearDatabase();
        var dto = BuildValidUpdateDto();

        // Malformed token that will fail validation
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.expired.token");

        // Act
        var response = await _client.PutAsJsonAsync("/api/surveys/1/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSurveyComplete_WithValidToken_ProceedsToNextStage()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id);
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = BuildValidUpdateDto();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        // Should not be 401, might be 200 OK or other status depending on validation
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task UpdateSurveyComplete_WhenUserDoesNotOwnSurvey_Returns403Forbidden()
    {
        // Arrange
        _factory.ClearDatabase();

        // Create two users
        var owner = EntityBuilder.CreateUser(telegramId: 111111111, username: "owner");
        var otherUser = EntityBuilder.CreateUser(telegramId: 222222222, username: "otherUser");

        _factory.SeedDatabase(db =>
        {
            db.Users.Add(owner);
            db.Users.Add(otherUser);
        });

        // Create survey owned by first user
        var surveyId = await CreateTestSurveyAsync(owner.Id, "Owner's Survey");

        // Get token for second user (not the owner)
        var token = await GetAuthTokenAsync(otherUser.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = BuildValidUpdateDto();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");
    }

    [Fact]
    public async Task UpdateSurveyComplete_WhenUserOwnsSurvey_Returns200OK()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id);
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = BuildValidUpdateDto();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task UpdateSurveyComplete_WithEmptyTitle_Returns400BadRequest()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id);
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = BuildValidUpdateDto();
        dto.Title = ""; // Invalid: empty title

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateSurveyComplete_WithShortTitle_Returns400BadRequest()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id);
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = BuildValidUpdateDto();
        dto.Title = "AB"; // Invalid: < 3 characters

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSurveyComplete_WithEmptyQuestions_Returns400BadRequest()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id);
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = BuildValidUpdateDto();
        dto.Questions = new List<CreateQuestionWithFlowDto>(); // Invalid: no questions

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("question");
    }

    [Fact]
    public async Task UpdateSurveyComplete_WithInvalidQuestionIndexReference_Returns400BadRequest()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id);
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = BuildValidUpdateDto(questionCount: 2);
        // Set invalid index reference - pointing beyond array bounds
        dto.Questions[0].DefaultNextQuestionIndex = 999; // Out of bounds (only 2 questions: indexes 0-1)

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("out of bounds");
    }

    [Fact]
    public async Task UpdateSurveyComplete_WithSelfReferencingQuestion_Returns400BadRequest()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id);
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = BuildValidUpdateDto(questionCount: 3);
        // Question 1 points to itself (invalid)
        dto.Questions[1].DefaultNextQuestionIndex = 1;

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("reference itself");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task UpdateSurveyComplete_WithValidData_Returns200AndUpdatedSurvey()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id, "Original Title");
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = BuildValidUpdateDto(title: "Updated Title", questionCount: 3);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Updated Title");
        result.Data.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateSurveyComplete_CreatesNewQuestionIds_DifferentFromTemporaryIds()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id);
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = BuildValidUpdateDto(questionCount: 3);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        result.Should().NotBeNull();
        result!.Data!.Questions.Should().NotBeNull();
        result.Data.Questions.Should().HaveCount(3);

        // All questions should have positive database IDs (not 0 or negative temporary IDs)
        result.Data.Questions.Should().AllSatisfy(q =>
        {
            q.Id.Should().BeGreaterThan(0);
        });

        // Question IDs should be unique
        var questionIds = result.Data.Questions.Select(q => q.Id).ToList();
        questionIds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task UpdateSurveyComplete_UpdatesSurveyMetadata_Correctly()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id, "Original Survey");
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new UpdateSurveyWithQuestionsDto
        {
            Title = "Completely New Title",
            Description = "Brand new description",
            AllowMultipleResponses = true,
            ShowResults = false,
            ActivateAfterUpdate = false,
            Questions = new List<CreateQuestionWithFlowDto>
            {
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "New Question",
                    QuestionType = QuestionType.Text,
                    IsRequired = true,
                    OrderIndex = 0
                }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Completely New Title");
        result.Data.Description.Should().Be("Brand new description");
        result.Data.AllowMultipleResponses.Should().BeTrue();
        result.Data.ShowResults.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateSurveyComplete_WithQuestionsInCorrectOrder_MaintainsOrderIndex()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id);
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new UpdateSurveyWithQuestionsDto
        {
            Title = "Ordered Survey",
            Description = "Test order",
            Questions = new List<CreateQuestionWithFlowDto>
            {
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "First Question",
                    QuestionType = QuestionType.Text,
                    OrderIndex = 0
                },
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "Second Question",
                    QuestionType = QuestionType.Text,
                    OrderIndex = 1
                },
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "Third Question",
                    QuestionType = QuestionType.Text,
                    OrderIndex = 2
                }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        result.Should().NotBeNull();
        result!.Data!.Questions.Should().HaveCount(3);

        var orderedQuestions = result.Data.Questions.OrderBy(q => q.OrderIndex).ToList();
        orderedQuestions[0].QuestionText.Should().Be("First Question");
        orderedQuestions[1].QuestionText.Should().Be("Second Question");
        orderedQuestions[2].QuestionText.Should().Be("Third Question");
    }

    [Fact]
    public async Task UpdateSurveyComplete_WithActivateAfterUpdate_ActivatesSurvey()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id);
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = BuildValidUpdateDto(activateAfterUpdate: true);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.IsActive.Should().BeTrue();
    }

    #endregion

    #region Error Scenarios

    [Fact]
    public async Task UpdateSurveyComplete_WithNonExistentSurvey_Returns404NotFound()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = BuildValidUpdateDto();
        var nonExistentSurveyId = 99999;

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{nonExistentSurveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateSurveyComplete_WithCycleInFlow_Returns409Conflict()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id);
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a cycle: Q0 -> Q1 -> Q2 -> Q0
        var dto = new UpdateSurveyWithQuestionsDto
        {
            Title = "Cyclic Survey",
            Description = "Has cycle",
            ActivateAfterUpdate = true, // Activation triggers cycle detection
            Questions = new List<CreateQuestionWithFlowDto>
            {
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "Question 0",
                    QuestionType = QuestionType.Text,
                    OrderIndex = 0,
                    DefaultNextQuestionIndex = 1 // Q0 -> Q1
                },
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "Question 1",
                    QuestionType = QuestionType.Text,
                    OrderIndex = 1,
                    DefaultNextQuestionIndex = 2 // Q1 -> Q2
                },
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "Question 2",
                    QuestionType = QuestionType.Text,
                    OrderIndex = 2,
                    DefaultNextQuestionIndex = 0 // Q2 -> Q0 (CYCLE!)
                }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        // Should return 400 Bad Request with cycle error (validation fails before activation)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("cycle", because: "Error message should mention cycle detection");
    }

    [Fact]
    public async Task UpdateSurveyComplete_WithSingleChoiceAndInvalidOptionFlow_Returns400BadRequest()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id);
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new UpdateSurveyWithQuestionsDto
        {
            Title = "Invalid Option Flow Survey",
            Description = "Test",
            Questions = new List<CreateQuestionWithFlowDto>
            {
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "Choose option",
                    QuestionType = QuestionType.SingleChoice,
                    OrderIndex = 0,
                    Options = new List<string> { "Option A", "Option B" },
                    OptionNextQuestionIndexes = new Dictionary<int, int?>
                    {
                        { 0, 999 }, // Invalid: index 999 out of bounds
                        { 1, null }
                    }
                }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public async Task UpdateSurveyComplete_WithMultipleQuestionTypes_CreatesAllCorrectly()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        var surveyId = await CreateTestSurveyAsync(user.Id);
        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new UpdateSurveyWithQuestionsDto
        {
            Title = "Mixed Question Types",
            Description = "Various question types",
            Questions = new List<CreateQuestionWithFlowDto>
            {
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "Text Question",
                    QuestionType = QuestionType.Text,
                    OrderIndex = 0,
                    IsRequired = true
                },
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "Single Choice Question",
                    QuestionType = QuestionType.SingleChoice,
                    OrderIndex = 1,
                    Options = new List<string> { "Option A", "Option B", "Option C" },
                    IsRequired = true
                },
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "Multiple Choice Question",
                    QuestionType = QuestionType.MultipleChoice,
                    OrderIndex = 2,
                    Options = new List<string> { "Choice 1", "Choice 2" },
                    IsRequired = false
                },
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "Rating Question",
                    QuestionType = QuestionType.Rating,
                    OrderIndex = 3,
                    IsRequired = true
                }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        result.Should().NotBeNull();
        result!.Data!.Questions.Should().HaveCount(4);
        result.Data.Questions.Select(q => q.QuestionType).Should().Contain(new[]
        {
            QuestionType.Text,
            QuestionType.SingleChoice,
            QuestionType.MultipleChoice,
            QuestionType.Rating
        });
    }

    [Fact]
    public async Task UpdateSurveyComplete_ReplacesOldQuestions_WithNewOnes()
    {
        // Arrange
        _factory.ClearDatabase();

        var user = EntityBuilder.CreateUser(telegramId: 123456789);
        _factory.SeedDatabase(db =>
        {
            db.Users.Add(user);
        });

        // Create survey with initial questions
        var surveyId = await CreateTestSurveyAsync(user.Id, "Survey with Old Questions");

        _factory.SeedDatabase(db =>
        {
            var survey = db.Surveys.Find(surveyId);
            if (survey != null)
            {
                var oldQuestion1 = EntityBuilder.CreateQuestion(surveyId, "Old Question 1", QuestionType.Text, 0);
                var oldQuestion2 = EntityBuilder.CreateQuestion(surveyId, "Old Question 2", QuestionType.Text, 1);
                db.Questions.AddRange(oldQuestion1, oldQuestion2);
            }
        });

        var token = await GetAuthTokenAsync(user.TelegramId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create new questions that will replace old ones
        var dto = new UpdateSurveyWithQuestionsDto
        {
            Title = "Survey with New Questions",
            Description = "Updated",
            Questions = new List<CreateQuestionWithFlowDto>
            {
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "New Question 1",
                    QuestionType = QuestionType.SingleChoice,
                    OrderIndex = 0,
                    Options = new List<string> { "Yes", "No" }
                },
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "New Question 2",
                    QuestionType = QuestionType.Rating,
                    OrderIndex = 1
                },
                new CreateQuestionWithFlowDto
                {
                    QuestionText = "New Question 3",
                    QuestionType = QuestionType.Text,
                    OrderIndex = 2
                }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/surveys/{surveyId}/complete", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        result.Should().NotBeNull();
        result!.Data!.Questions.Should().HaveCount(3, because: "Old questions should be replaced");
        result.Data.Questions.Should().AllSatisfy(q =>
        {
            q.QuestionText.Should().StartWith("New Question", because: "All questions should be new");
        });
        result.Data.Questions.Should().NotContain(q => q.QuestionText.StartsWith("Old"),
            because: "Old questions should be deleted");
    }

    #endregion
}
