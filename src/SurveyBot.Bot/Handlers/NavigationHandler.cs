using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Configuration;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Models;
using SurveyBot.Bot.Services;
using SurveyBot.Core.DTOs.Answer;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.DTOs.Survey;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SurveyBot.Bot.Handlers;

/// <summary>
/// Handles navigation actions during survey taking (Back/Skip).
/// Manages question flow and state transitions.
/// </summary>
public class NavigationHandler
{
    private readonly IBotService _botService;
    private readonly IConversationStateManager _stateManager;
    private readonly IEnumerable<IQuestionHandler> _questionHandlers;
    private readonly HttpClient _httpClient;
    private readonly BotConfiguration _configuration;
    private readonly BotPerformanceMonitor _performanceMonitor;
    private readonly SurveyCache _surveyCache;
    private readonly ILogger<NavigationHandler> _logger;

    public NavigationHandler(
        IBotService botService,
        IConversationStateManager stateManager,
        IEnumerable<IQuestionHandler> questionHandlers,
        HttpClient httpClient,
        Microsoft.Extensions.Options.IOptions<BotConfiguration> configuration,
        BotPerformanceMonitor performanceMonitor,
        SurveyCache surveyCache,
        ILogger<NavigationHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _questionHandlers = questionHandlers ?? throw new ArgumentNullException(nameof(questionHandlers));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _surveyCache = surveyCache ?? throw new ArgumentNullException(nameof(surveyCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure HttpClient base address
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(_configuration.ApiBaseUrl);
        }
    }

    /// <summary>
    /// Handles the "Back" button callback.
    /// Navigates to previous question and displays stored answer.
    /// </summary>
    public async Task<bool> HandleBackAsync(
        CallbackQuery callbackQuery,
        int questionId,
        CancellationToken cancellationToken = default)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message?.Chat.Id ?? userId;

        _logger.LogInformation(
            "User {UserId} pressed Back on question {QuestionId}",
            userId,
            questionId);

        // Get current state
        var state = await _stateManager.GetStateAsync(userId);
        if (state == null || !state.CurrentQuestionIndex.HasValue)
        {
            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Session expired. Please restart the survey.",
                showAlert: true,
                cancellationToken: cancellationToken);

            return false;
        }

        // Check if on first question
        if (state.IsFirstQuestion)
        {
            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "You're already on the first question.",
                showAlert: true,
                cancellationToken: cancellationToken);

            return false;
        }

        // Move to previous question
        var success = await _stateManager.PreviousQuestionAsync(userId);
        if (!success)
        {
            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Cannot go back.",
                showAlert: true,
                cancellationToken: cancellationToken);

            return false;
        }

        // Get updated state
        state = await _stateManager.GetStateAsync(userId);
        if (state == null || !state.CurrentQuestionIndex.HasValue || !state.CurrentSurveyId.HasValue)
        {
            _logger.LogError("State lost after going back for user {UserId}", userId);
            return false;
        }

        // Fetch survey with questions
        var survey = await FetchSurveyWithQuestionsAsync(state.CurrentSurveyId.Value, cancellationToken);
        if (survey == null || survey.Questions == null || survey.Questions.Count == 0)
        {
            _logger.LogError("Failed to fetch survey {SurveyId} for user {UserId}", state.CurrentSurveyId, userId);
            return false;
        }

        // Get previous question
        var questionDto = survey.Questions.ElementAtOrDefault(state.CurrentQuestionIndex.Value);
        if (questionDto == null)
        {
            _logger.LogError(
                "Question at index {Index} not found in survey {SurveyId}",
                state.CurrentQuestionIndex,
                state.CurrentSurveyId);
            return false;
        }

        // Answer callback query
        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "Going back...",
            cancellationToken: cancellationToken);

        // Display previous question
        await DisplayQuestionWithPreviousAnswerAsync(
            chatId,
            questionDto,
            state.CurrentQuestionIndex.Value,
            state.TotalQuestions ?? survey.Questions.Count,
            userId,
            cancellationToken);

        return true;
    }

    /// <summary>
    /// Handles the "Skip" button callback.
    /// Only works for optional questions.
    /// </summary>
    public async Task<bool> HandleSkipAsync(
        CallbackQuery callbackQuery,
        int questionId,
        CancellationToken cancellationToken = default)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message?.Chat.Id ?? userId;

        _logger.LogInformation(
            "User {UserId} pressed Skip on question {QuestionId}",
            userId,
            questionId);

        // Get current state
        var state = await _stateManager.GetStateAsync(userId);
        if (state == null || !state.CurrentQuestionIndex.HasValue || !state.CurrentSurveyId.HasValue)
        {
            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Session expired. Please restart the survey.",
                showAlert: true,
                cancellationToken: cancellationToken);

            return false;
        }

        // Fetch survey with questions
        var survey = await FetchSurveyWithQuestionsAsync(state.CurrentSurveyId.Value, cancellationToken);
        if (survey == null || survey.Questions == null || survey.Questions.Count == 0)
        {
            _logger.LogError("Failed to fetch survey {SurveyId} for user {UserId}", state.CurrentSurveyId, userId);
            return false;
        }

        // Get current question
        var questionDto = survey.Questions.ElementAtOrDefault(state.CurrentQuestionIndex.Value);
        if (questionDto == null)
        {
            _logger.LogError(
                "Question at index {Index} not found in survey {SurveyId}",
                state.CurrentQuestionIndex,
                state.CurrentSurveyId);
            return false;
        }

        // Check if question is required
        if (questionDto.IsRequired)
        {
            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "This question is required and cannot be skipped.",
                showAlert: true,
                cancellationToken: cancellationToken);

            return false;
        }

        // Answer callback query
        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "Question skipped",
            cancellationToken: cancellationToken);

        // Create empty answer for skipped question
        var emptyAnswerJson = CreateEmptyAnswerForQuestionType(questionDto.QuestionType);

        // Submit empty answer to API
        if (state.CurrentResponseId.HasValue)
        {
            var submitSuccess = await SubmitAnswerAsync(
                state.CurrentResponseId.Value,
                questionDto.Id,
                emptyAnswerJson,
                cancellationToken);

            if (!submitSuccess)
            {
                _logger.LogWarning(
                    "Failed to submit skipped answer for question {QuestionId}",
                    questionDto.Id);
            }
        }

        // Record answer in state
        await _stateManager.AnswerQuestionAsync(userId, state.CurrentQuestionIndex.Value, emptyAnswerJson);

        // Move to next question
        var isLastQuestion = state.IsLastQuestion;
        if (!isLastQuestion)
        {
            await _stateManager.NextQuestionAsync(userId);

            // Display next question
            state = await _stateManager.GetStateAsync(userId);
            if (state != null && state.CurrentQuestionIndex.HasValue)
            {
                var nextQuestion = survey.Questions.ElementAtOrDefault(state.CurrentQuestionIndex.Value);
                if (nextQuestion != null)
                {
                    await DisplayQuestionAsync(
                        chatId,
                        nextQuestion,
                        state.CurrentQuestionIndex.Value,
                        state.TotalQuestions ?? survey.Questions.Count,
                        cancellationToken);
                }
            }
        }
        else
        {
            // Last question - complete survey
            await CompleteSurveyAsync(userId, state.CurrentResponseId!.Value, chatId, cancellationToken);
        }

        return true;
    }

    #region Private Helper Methods

    /// <summary>
    /// Fetches survey with questions from API with caching for performance.
    /// </summary>
    private async Task<SurveyDto?> FetchSurveyWithQuestionsAsync(int surveyId, CancellationToken cancellationToken)
    {
        return await _performanceMonitor.TrackOperationAsync(
            "FetchSurveyWithQuestions",
            async () =>
            {
                // Try cache first
                return await _surveyCache.GetOrAddSurveyAsync(
                    surveyId,
                    async () =>
                    {
                        try
                        {
                            var response = await _httpClient.GetAsync($"/api/surveys/{surveyId}", cancellationToken);

                            if (!response.IsSuccessStatusCode)
                            {
                                _logger.LogWarning(
                                    "Failed to fetch survey {SurveyId}: {StatusCode}",
                                    surveyId,
                                    response.StatusCode);
                                return null;
                            }

                            var apiResponse = await response.Content.ReadFromJsonAsync<Models.ApiResponse<SurveyDto>>(cancellationToken);
                            return apiResponse?.Data;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error fetching survey {SurveyId}", surveyId);
                            return null;
                        }
                    },
                    ttl: TimeSpan.FromMinutes(5));
            },
            context: $"SurveyId={surveyId}");
    }

    /// <summary>
    /// Displays question with previous answer pre-filled (if available).
    /// </summary>
    private async Task DisplayQuestionWithPreviousAnswerAsync(
        long chatId,
        QuestionDto question,
        int currentIndex,
        int totalQuestions,
        long userId,
        CancellationToken cancellationToken)
    {
        // Get handler for this question type
        var handler = _questionHandlers.FirstOrDefault(h => h.QuestionType == question.QuestionType);
        if (handler == null)
        {
            _logger.LogError("No handler found for question type {QuestionType}", question.QuestionType);
            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Error: Unable to display this question type.",
                cancellationToken: cancellationToken);
            return;
        }

        // Get cached answer if exists
        var cachedAnswer = await _stateManager.GetCachedAnswerAsync(userId, currentIndex);

        // Display question
        await handler.DisplayQuestionAsync(chatId, question, currentIndex, totalQuestions, cancellationToken);

        // If there's a cached answer, show it
        if (!string.IsNullOrWhiteSpace(cachedAnswer))
        {
            await _botService.Client.SendMessage(
                chatId: chatId,
                text: $"Your previous answer: {FormatPreviousAnswer(cachedAnswer, question.QuestionType)}",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Displays a question using the appropriate handler.
    /// </summary>
    private async Task DisplayQuestionAsync(
        long chatId,
        QuestionDto question,
        int currentIndex,
        int totalQuestions,
        CancellationToken cancellationToken)
    {
        var handler = _questionHandlers.FirstOrDefault(h => h.QuestionType == question.QuestionType);
        if (handler == null)
        {
            _logger.LogError("No handler found for question type {QuestionType}", question.QuestionType);
            return;
        }

        await handler.DisplayQuestionAsync(chatId, question, currentIndex, totalQuestions, cancellationToken);
    }

    /// <summary>
    /// Formats previous answer for display.
    /// </summary>
    private string FormatPreviousAnswer(string answerJson, Core.Entities.QuestionType questionType)
    {
        try
        {
            var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

            return questionType switch
            {
                Core.Entities.QuestionType.Text => answer.GetProperty("text").GetString() ?? "",
                Core.Entities.QuestionType.SingleChoice => answer.GetProperty("selectedOption").GetString() ?? "",
                Core.Entities.QuestionType.MultipleChoice =>
                    string.Join(", ", answer.GetProperty("selectedOptions").EnumerateArray().Select(e => e.GetString())),
                Core.Entities.QuestionType.Rating => answer.GetProperty("rating").GetInt32().ToString(),
                _ => answerJson
            };
        }
        catch
        {
            return "[Previous answer]";
        }
    }

    /// <summary>
    /// Creates empty answer JSON for skipped questions based on type.
    /// </summary>
    private string CreateEmptyAnswerForQuestionType(Core.Entities.QuestionType questionType)
    {
        return questionType switch
        {
            Core.Entities.QuestionType.Text => JsonSerializer.Serialize(new { text = "" }),
            Core.Entities.QuestionType.SingleChoice => JsonSerializer.Serialize(new { selectedOption = "" }),
            Core.Entities.QuestionType.MultipleChoice => JsonSerializer.Serialize(new { selectedOptions = new string[] { } }),
            Core.Entities.QuestionType.Rating => JsonSerializer.Serialize(new { rating = (int?)null }),
            _ => "{}"
        };
    }

    /// <summary>
    /// Submits answer to API.
    /// </summary>
    private async Task<bool> SubmitAnswerAsync(
        int responseId,
        int questionId,
        string answerJson,
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse answer JSON to extract the appropriate field based on answer structure
            var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

            // Create submit DTO with appropriate fields
            var submitDto = new
            {
                questionId = questionId,
                answerText = answer.TryGetProperty("text", out var text) ? text.GetString() : null,
                selectedOptions = answer.TryGetProperty("selectedOptions", out var options)
                    ? options.EnumerateArray().Select(e => e.GetString()).ToList()
                    : (answer.TryGetProperty("selectedOption", out var option)
                        ? new List<string?> { option.GetString() }
                        : null),
                ratingValue = answer.TryGetProperty("rating", out var rating) && rating.ValueKind != JsonValueKind.Null
                    ? rating.GetInt32()
                    : (int?)null
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/responses/{responseId}/answers",
                submitDto,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to submit answer: {StatusCode}",
                    response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer for question {QuestionId}", questionId);
            return false;
        }
    }

    /// <summary>
    /// Completes the survey.
    /// </summary>
    private async Task CompleteSurveyAsync(
        long userId,
        int responseId,
        long chatId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Call API to complete response
            var response = await _httpClient.PostAsync(
                $"/api/responses/{responseId}/complete",
                null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to complete response {ResponseId}", responseId);
            }

            // Update state
            await _stateManager.CompleteSurveyAsync(userId);

            // Send completion message
            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Thank you for completing the survey!\n\nYour responses have been recorded.",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Survey completed for user {UserId}, response {ResponseId}", userId, responseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing survey for user {UserId}", userId);
        }
    }

    #endregion
}
