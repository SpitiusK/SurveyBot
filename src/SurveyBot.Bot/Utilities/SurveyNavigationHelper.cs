using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SurveyBot.Core.DTOs.Question;

namespace SurveyBot.Bot.Utilities;

/// <summary>
/// Helper class for determining next question in conditional survey flows.
/// Centralizes logic for calling API endpoints that handle conditional navigation.
/// </summary>
public class SurveyNavigationHelper
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SurveyNavigationHelper> _logger;

    public SurveyNavigationHelper(
        IHttpClientFactory httpClientFactory,
        ILogger<SurveyNavigationHelper> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the next question to display based on the current question and answer.
    /// Calls API endpoint that handles conditional flow logic.
    /// </summary>
    /// <param name="responseId">The response ID</param>
    /// <param name="currentQuestionId">The current question ID</param>
    /// <param name="answerText">The answer text (for conditional evaluation)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Navigation result indicating next question or completion</returns>
    public async Task<QuestionNavigationResult> GetNextQuestionAsync(
        int responseId,
        int currentQuestionId,
        string answerText,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Getting next question for response {ResponseId}, current question {QuestionId}",
                responseId,
                currentQuestionId);

            var httpClient = _httpClientFactory.CreateClient("SurveyBotApi");

            // Call API endpoint to get next question
            // API handles: conditional flow evaluation, cycle detection, endpoint detection
            var response = await httpClient.GetAsync(
                $"/api/responses/{responseId}/next-question?currentQuestionId={currentQuestionId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get next question: {StatusCode}",
                    response.StatusCode);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return QuestionNavigationResult.NotFound(currentQuestionId);
                }

                return QuestionNavigationResult.Error("Unable to determine next question");
            }

            // Check for 204 No Content (survey complete)
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogInformation("Survey completed for response {ResponseId}", responseId);
                return QuestionNavigationResult.SurveyComplete();
            }

            // Deserialize API response wrapper
            var apiResponseWrapper = await response.Content.ReadFromJsonAsync<ApiResponseWrapper>(cancellationToken);

            if (apiResponseWrapper == null)
            {
                _logger.LogError("Failed to deserialize next question response");
                return QuestionNavigationResult.Error("Invalid response from server");
            }

            // Return next question
            if (apiResponseWrapper.Data != null)
            {
                _logger.LogDebug(
                    "Next question for response {ResponseId}: {NextQuestionId}",
                    responseId,
                    apiResponseWrapper.Data.Id);

                return QuestionNavigationResult.WithNextQuestion(apiResponseWrapper.Data);
            }

            // No next question in data - unexpected state
            _logger.LogWarning(
                "Unexpected state: no next question data for response {ResponseId}",
                responseId);

            return QuestionNavigationResult.Error("Unable to determine survey state");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting next question for response {ResponseId}", responseId);
            return QuestionNavigationResult.Error("Network error while getting next question");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next question for response {ResponseId}", responseId);
            return QuestionNavigationResult.Error("Unable to determine next question");
        }
    }

    /// <summary>
    /// Gets the first question in a survey.
    /// Called when starting a new survey response.
    /// </summary>
    /// <param name="surveyId">The survey ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>First question or null if survey has no questions</returns>
    public async Task<QuestionDto?> GetFirstQuestionAsync(
        int surveyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting first question for survey {SurveyId}", surveyId);

            var httpClient = _httpClientFactory.CreateClient("SurveyBotApi");

            // Get survey questions
            var response = await httpClient.GetAsync(
                $"/api/surveys/{surveyId}/questions",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get questions for survey {SurveyId}: {StatusCode}",
                    surveyId,
                    response.StatusCode);
                return null;
            }

            var questions = await response.Content.ReadFromJsonAsync<List<QuestionDto>>(cancellationToken);

            if (questions == null || questions.Count == 0)
            {
                _logger.LogWarning("Survey {SurveyId} has no questions", surveyId);
                return null;
            }

            // Return first question by OrderIndex
            var firstQuestion = questions.OrderBy(q => q.OrderIndex).First();

            _logger.LogDebug(
                "First question for survey {SurveyId}: {QuestionId}",
                surveyId,
                firstQuestion.Id);

            return firstQuestion;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting first question for survey {SurveyId}", surveyId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting first question for survey {SurveyId}", surveyId);
            return null;
        }
    }
}

/// <summary>
/// API response wrapper that matches SurveyBot.API.Models.ApiResponse<T> structure.
/// Used for deserializing API responses containing question data.
/// </summary>
internal class ApiResponseWrapper
{
    public bool Success { get; set; }
    public QuestionDto? Data { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Result of navigation operation indicating next question or completion.
/// </summary>
public class QuestionNavigationResult
{
    /// <summary>
    /// Survey is complete (no more questions).
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// An error occurred during navigation.
    /// </summary>
    public bool IsError { get; set; }

    /// <summary>
    /// Error message if IsError is true.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The next question to display (null if complete or error).
    /// </summary>
    public QuestionDto? NextQuestion { get; set; }

    /// <summary>
    /// Creates a result indicating survey completion.
    /// </summary>
    public static QuestionNavigationResult SurveyComplete() =>
        new() { IsComplete = true };

    /// <summary>
    /// Creates a result with the next question.
    /// </summary>
    public static QuestionNavigationResult WithNextQuestion(QuestionDto question) =>
        new() { NextQuestion = question };

    /// <summary>
    /// Creates a result indicating an error occurred.
    /// </summary>
    public static QuestionNavigationResult Error(string message) =>
        new() { IsError = true, ErrorMessage = message };

    /// <summary>
    /// Creates a result indicating a question was not found.
    /// </summary>
    public static QuestionNavigationResult NotFound(int questionId) =>
        new() { IsError = true, ErrorMessage = $"Question {questionId} not found" };
}
