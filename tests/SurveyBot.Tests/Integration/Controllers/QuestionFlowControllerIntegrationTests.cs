using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Entities;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for QuestionFlowController and conditional flow endpoints.
/// Tests HTTP endpoints for flow configuration, validation, and navigation.
/// TEST-003: 10+ API endpoint tests for Conditional Question Flow feature.
/// </summary>
public class QuestionFlowControllerIntegrationTests : IClassFixture<WebApplicationFactoryFixture<Program>>
{
    private readonly WebApplicationFactoryFixture<Program> _factory;
    private readonly HttpClient _client;

    public QuestionFlowControllerIntegrationTests(WebApplicationFactoryFixture<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    #region Test Helper Methods

    /// <summary>
    /// Gets authentication token for a test user.
    /// </summary>
    private async Task<string> GetAuthTokenAsync(long telegramId = 123456789)
    {
        var loginRequest = new LoginRequestDto { TelegramId = telegramId };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        return result!.Data!.Token;
    }

    /// <summary>
    /// Seeds database with test data: user, survey, questions with branching flow.
    /// </summary>
    private (int surveyId, int q1Id, int q2Id, int q3Id, int q4Id) SeedTestSurvey()
    {
        int surveyId = 0, q1Id = 0, q2Id = 0, q3Id = 0, q4Id = 0;

        _factory.SeedDatabase(db =>
        {
            // Create user
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            // Create survey
            var survey = EntityBuilder.CreateSurvey(
                title: "Test Survey with Flow",
                creatorId: user.Id,
                isActive: false);
            db.Surveys.Add(survey);
            db.SaveChanges();
            surveyId = survey.Id;

            // Create branching SingleChoice question (Q1)
            var q1 = Question.CreateSingleChoiceQuestion(
                surveyId: survey.Id,
                questionText: "Do you like this feature?",
                orderIndex: 0,
                optionsJson: "[\"Yes\", \"No\"]",
                isRequired: true);
            db.Questions.Add(q1);
            db.SaveChanges();
            q1Id = q1.Id;

            // Create follow-up question for "Yes" (Q2)
            var q2 = Question.CreateTextQuestion(
                surveyId: survey.Id,
                questionText: "What do you like most?",
                orderIndex: 1,
                isRequired: true);
            db.Questions.Add(q2);
            db.SaveChanges();
            q2Id = q2.Id;

            // Create follow-up question for "No" (Q3)
            var q3 = Question.CreateTextQuestion(
                surveyId: survey.Id,
                questionText: "What would you improve?",
                orderIndex: 2,
                isRequired: true);
            db.Questions.Add(q3);
            db.SaveChanges();
            q3Id = q3.Id;

            // Create final question (Q4)
            var q4 = Question.CreateTextQuestion(
                surveyId: survey.Id,
                questionText: "Any additional comments?",
                orderIndex: 3,
                isRequired: false);
            db.Questions.Add(q4);
            db.SaveChanges();
            q4Id = q4.Id;

            // Set up question options with flow
            var option1 = QuestionOption.Create(
                questionId: q1.Id,
                text: "Yes",
                orderIndex: 0,
                next: NextQuestionDeterminant.ToQuestion(q2.Id));

            var option2 = QuestionOption.Create(
                questionId: q1.Id,
                text: "No",
                orderIndex: 1,
                next: NextQuestionDeterminant.ToQuestion(q3.Id));

            db.QuestionOptions.Add(option1);
            db.QuestionOptions.Add(option2);
            db.SaveChanges();
        });

        return (surveyId, q1Id, q2Id, q3Id, q4Id);
    }

    /// <summary>
    /// Seeds database with survey containing a cycle (Q1 -> Q2 -> Q1).
    /// </summary>
    private (int surveyId, int q1Id, int q2Id) SeedSurveyWithCycle()
    {
        int surveyId = 0, q1Id = 0, q2Id = 0;

        _factory.SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(
                title: "Survey with Cycle",
                creatorId: user.Id,
                isActive: false);
            db.Surveys.Add(survey);
            db.SaveChanges();
            surveyId = survey.Id;

            // Q1 -> Q2
            var q1 = Question.CreateTextQuestion(
                surveyId: survey.Id,
                questionText: "Question 1",
                orderIndex: 0,
                isRequired: true);
            db.Questions.Add(q1);
            db.SaveChanges();
            q1Id = q1.Id;

            // Q2 -> Q1 (creates cycle)
            var q2 = Question.CreateTextQuestion(
                surveyId: survey.Id,
                questionText: "Question 2",
                orderIndex: 1,
                isRequired: true);
            db.Questions.Add(q2);
            db.SaveChanges();
            q2Id = q2.Id;

            // Update Q1 to point to Q2, and Q2 to point to Q1 (creates cycle)
            q1.SetDefaultNext(NextQuestionDeterminant.ToQuestion(q2.Id));
            q2.SetDefaultNext(NextQuestionDeterminant.ToQuestion(q1.Id)); // Points back to Q1
            db.SaveChanges();
        });

        return (surveyId, q1Id, q2Id);
    }

    #endregion

    #region QuestionFlowController - GetQuestionFlow Tests

    [Fact]
    public async Task GetQuestionFlow_ValidQuestion_Returns200WithFlowConfiguration()
    {
        // Arrange
        _factory.ClearDatabase();
        var (surveyId, q1Id, _, _, _) = SeedTestSurvey();

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/surveys/{surveyId}/questions/{q1Id}/flow");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<ConditionalFlowDto>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.QuestionId.Should().Be(q1Id);
        content.Data.SupportsBranching.Should().BeTrue(); // SingleChoice question
        content.Data.OptionFlows.Should().HaveCount(2); // "Yes" and "No" options
    }

    [Fact]
    public async Task GetQuestionFlow_NonExistentQuestion_Returns404NotFound()
    {
        // Arrange
        _factory.ClearDatabase();
        var (surveyId, _, _, _, _) = SeedTestSurvey();

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentQuestionId = 99999;

        // Act
        var response = await _client.GetAsync($"/api/surveys/{surveyId}/questions/{nonExistentQuestionId}/flow");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetQuestionFlow_WithoutAuthorization_Returns401Unauthorized()
    {
        // Arrange
        _factory.ClearDatabase();
        var (surveyId, q1Id, _, _, _) = SeedTestSurvey();

        // Don't set authorization header
        var clientNoAuth = _factory.CreateClient();

        // Act
        var response = await clientNoAuth.GetAsync($"/api/surveys/{surveyId}/questions/{q1Id}/flow");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region QuestionFlowController - UpdateQuestionFlow Tests

    [Fact]
    public async Task UpdateQuestionFlow_ValidBranchingUpdate_Returns200WithUpdatedFlow()
    {
        // Arrange
        _factory.ClearDatabase();
        var (surveyId, q1Id, q2Id, q3Id, q4Id) = SeedTestSurvey();

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Update branching flow: Option 0 -> Q2, Option 1 -> Q4 (skip Q3)
        var updateDto = new UpdateQuestionFlowDto
        {
            OptionNextDeterminants = new Dictionary<int, NextQuestionDeterminantDto>
            {
                { 0, NextQuestionDeterminantDto.ToQuestion(q2Id) }, // "Yes" -> Q2
                { 1, NextQuestionDeterminantDto.ToQuestion(q4Id) }  // "No" -> Q4 (changed from Q3)
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/surveys/{surveyId}/questions/{q1Id}/flow",
            updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<ConditionalFlowDto>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.OptionFlows.Should().HaveCount(2);

        // Verify option flows updated
        var option1Flow = content.Data.OptionFlows.FirstOrDefault(o => o.OptionId == 0);
        option1Flow.Should().NotBeNull();
        option1Flow!.Next.NextQuestionId.Should().Be(q2Id);

        var option2Flow = content.Data.OptionFlows.FirstOrDefault(o => o.OptionId == 1);
        option2Flow.Should().NotBeNull();
        option2Flow!.Next.NextQuestionId.Should().Be(q4Id); // Changed
    }

    [Fact]
    public async Task UpdateQuestionFlow_ValidNonBranchingUpdate_Returns200WithUpdatedFlow()
    {
        // Arrange
        _factory.ClearDatabase();
        var (surveyId, _, q2Id, q3Id, q4Id) = SeedTestSurvey();

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Update non-branching text question (Q2) to point to Q4
        var updateDto = new UpdateQuestionFlowDto
        {
            DefaultNext = NextQuestionDeterminantDto.ToQuestion(q4Id) // Skip Q3
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/surveys/{surveyId}/questions/{q2Id}/flow",
            updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<ConditionalFlowDto>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.DefaultNext.Should().NotBeNull();
        content.Data.DefaultNext!.NextQuestionId.Should().Be(q4Id);
    }

    [Fact]
    public async Task UpdateQuestionFlow_CycleCausingUpdate_Returns400WithCycleDetails()
    {
        // Arrange
        _factory.ClearDatabase();
        var (surveyId, q1Id, q2Id, _, _) = SeedTestSurvey();

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create cycle: Q2 -> Q1 (Q1 already points to Q2 via branching)
        var updateDto = new UpdateQuestionFlowDto
        {
            DefaultNext = NextQuestionDeterminantDto.ToQuestion(q1Id) // Points back to Q1
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/surveys/{surveyId}/questions/{q2Id}/flow",
            updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.Message.Should().Contain("cycle");

        // Verify cyclePath is included in response
        var dataElement = ((JsonElement)content.Data!);
        dataElement.TryGetProperty("cyclePath", out var cyclePath).Should().BeTrue();
    }

    #endregion

    #region QuestionFlowController - ValidateSurveyFlow Tests

    [Fact]
    public async Task ValidateSurveyFlow_ValidSurvey_Returns200WithSuccess()
    {
        // Arrange
        _factory.ClearDatabase();
        var (surveyId, _, _, _, _) = SeedTestSurvey();

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync($"/api/surveys/{surveyId}/questions/validate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();

        var dataElement = ((JsonElement)content.Data!);
        dataElement.TryGetProperty("valid", out var validProperty).Should().BeTrue();
        validProperty.GetBoolean().Should().BeTrue();

        dataElement.TryGetProperty("endpointCount", out var endpointCount).Should().BeTrue();
        endpointCount.GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ValidateSurveyFlow_SurveyWithCycle_Returns200WithCycleError()
    {
        // Arrange
        _factory.ClearDatabase();
        var (surveyId, _, _) = SeedSurveyWithCycle();

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync($"/api/surveys/{surveyId}/questions/validate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK); // Validation endpoint returns 200 even with errors

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();

        var dataElement = ((JsonElement)content.Data!);

        // Should indicate invalid
        dataElement.TryGetProperty("valid", out var validProperty).Should().BeTrue();
        validProperty.GetBoolean().Should().BeFalse();

        // Should include cyclePath
        dataElement.TryGetProperty("cyclePath", out var cyclePath).Should().BeTrue();
        cyclePath.ValueKind.Should().NotBe(JsonValueKind.Null);

        // Should include errors
        dataElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.GetArrayLength().Should().BeGreaterThan(0);
    }

    #endregion

    #region ResponsesController - GetNextQuestion Tests

    [Fact]
    public async Task GetNextQuestion_ValidResponse_Returns200WithNextQuestion()
    {
        // Arrange
        _factory.ClearDatabase();
        int responseId = 0;

        var (surveyId, q1Id, q2Id, _, _) = SeedTestSurvey();

        // Seed a response
        _factory.SeedDatabase(db =>
        {
            var response = Response.Start(surveyId: surveyId, respondentTelegramId: 987654321);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;
        });

        // Public endpoint - no auth required
        var clientNoAuth = _factory.CreateClient();

        // Act
        var response = await clientNoAuth.GetAsync($"/api/responses/{responseId}/next-question");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<QuestionDto>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.Id.Should().BeOneOf(q1Id, q2Id); // Should return first question or next unanswered
    }

    [Fact]
    public async Task GetNextQuestion_SurveyComplete_Returns204NoContent()
    {
        // Arrange
        _factory.ClearDatabase();
        int responseId = 0;

        var (surveyId, q1Id, q2Id, q3Id, q4Id) = SeedTestSurvey();

        // Seed a completed response with all answers
        _factory.SeedDatabase(db =>
        {
            var response = Response.Start(surveyId: surveyId, respondentTelegramId: 987654321);
            response.SetIsComplete(true);
            response.SetSubmittedAt(DateTime.UtcNow);
            db.Responses.Add(response);
            db.SaveChanges();
            responseId = response.Id;

            // Add answers to all questions
            var answer1 = Answer.CreateJsonAnswer(
                responseId: response.Id,
                questionId: q1Id,
                answerJson: "{\"selectedOption\": \"Yes\"}");
            db.Answers.Add(answer1);

            var answer2 = Answer.CreateTextAnswer(
                responseId: response.Id,
                questionId: q2Id,
                answerText: "Great feature!");
            db.Answers.Add(answer2);

            var answer3 = Answer.CreateTextAnswer(
                responseId: response.Id,
                questionId: q4Id,
                answerText: "No additional comments");
            db.Answers.Add(answer3);

            db.SaveChanges();
        });

        // Public endpoint - no auth required
        var clientNoAuth = _factory.CreateClient();

        // Act
        var response = await clientNoAuth.GetAsync($"/api/responses/{responseId}/next-question");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty(); // 204 has no body
    }

    #endregion

    #region SurveysController - ActivateSurvey Tests

    [Fact]
    public async Task ActivateSurvey_ValidSurvey_Returns200WithActivatedSurvey()
    {
        // Arrange
        _factory.ClearDatabase();
        var (surveyId, _, _, _, _) = SeedTestSurvey();

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync($"/api/surveys/{surveyId}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateSurvey_SurveyWithCycle_Returns400WithCycleError()
    {
        // Arrange
        _factory.ClearDatabase();
        var (surveyId, _, _) = SeedSurveyWithCycle();

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync($"/api/surveys/{surveyId}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.Message.Should().Contain("cycle");

        // Verify cyclePath in response
        var dataElement = ((JsonElement)content.Data!);
        dataElement.TryGetProperty("cyclePath", out var cyclePath).Should().BeTrue();
    }

    [Fact]
    public async Task ActivateSurvey_NonExistentSurvey_Returns404NotFound()
    {
        // Arrange
        _factory.ClearDatabase();
        SeedTestSurvey(); // Seed a user

        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentSurveyId = 99999;

        // Act
        var response = await _client.PostAsync($"/api/surveys/{nonExistentSurveyId}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ActivateSurvey_WithoutAuthorization_Returns401Unauthorized()
    {
        // Arrange
        _factory.ClearDatabase();
        var (surveyId, _, _, _, _) = SeedTestSurvey();

        // Don't set authorization header
        var clientNoAuth = _factory.CreateClient();

        // Act
        var response = await clientNoAuth.PostAsync($"/api/surveys/{surveyId}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helper Methods for Claims Extraction

    /// <summary>
    /// Helper to extract user ID from JWT claims (for reference).
    /// </summary>
    private int GetUserIdFromClaims()
    {
        // This would be in the controller, not the test
        // Shown here for reference
        throw new NotImplementedException("This is implemented in the actual controller");
    }

    #endregion
}
