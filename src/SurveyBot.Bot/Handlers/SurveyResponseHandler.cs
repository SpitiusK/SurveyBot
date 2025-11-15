using System.Net.Http.Json;
using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Configuration;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Models;
using SurveyBot.Bot.Services;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SurveyBot.Bot.Handlers;

/// <summary>
/// Handles survey response processing from user messages and callback queries.
/// Routes responses to appropriate question handlers and manages survey flow.
/// </summary>
public class SurveyResponseHandler
{
    private readonly IBotService _botService;
    private readonly IConversationStateManager _stateManager;
    private readonly IEnumerable<IQuestionHandler> _questionHandlers;
    private readonly ISurveyRepository _surveyRepository;
    private readonly IMapper _mapper;
    private readonly HttpClient _httpClient;
    private readonly BotConfiguration _configuration;
    private readonly BotPerformanceMonitor _performanceMonitor;
    private readonly SurveyCache _surveyCache;
    private readonly ILogger<SurveyResponseHandler> _logger;

    public SurveyResponseHandler(
        IBotService botService,
        IConversationStateManager stateManager,
        IEnumerable<IQuestionHandler> questionHandlers,
        ISurveyRepository surveyRepository,
        IMapper mapper,
        HttpClient httpClient,
        Microsoft.Extensions.Options.IOptions<BotConfiguration> configuration,
        BotPerformanceMonitor performanceMonitor,
        SurveyCache surveyCache,
        ILogger<SurveyResponseHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _questionHandlers = questionHandlers ?? throw new ArgumentNullException(nameof(questionHandlers));
        _surveyRepository = surveyRepository ?? throw new ArgumentNullException(nameof(surveyRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
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
    /// Handles text message response during active survey.
    /// </summary>
    public async Task<bool> HandleMessageResponseAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        if (message.From == null)
            return false;

        var userId = message.From.Id;
        var chatId = message.Chat.Id;

        // Get current state
        var state = await _stateManager.GetStateAsync(userId);
        if (state == null || !state.CurrentQuestionIndex.HasValue || !state.CurrentSurveyId.HasValue)
        {
            _logger.LogDebug("No active survey for user {UserId}", userId);
            return false;
        }

        _logger.LogInformation(
            "Processing message response for user {UserId} in survey {SurveyId}, question index {QuestionIndex}",
            userId,
            state.CurrentSurveyId,
            state.CurrentQuestionIndex);

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

        // Get handler for this question type
        var handler = _questionHandlers.FirstOrDefault(h => h.QuestionType == questionDto.QuestionType);
        if (handler == null)
        {
            _logger.LogError("No handler found for question type {QuestionType}", questionDto.QuestionType);
            return false;
        }

        // Process answer
        var answerJson = await handler.ProcessAnswerAsync(message, null, questionDto, userId, cancellationToken);

        if (answerJson == null)
        {
            // Validation failed or user needs to provide different answer
            _logger.LogDebug("Answer processing returned null for user {UserId}", userId);
            return true; // We handled it, just validation failed
        }

        // Submit answer to API
        if (state.CurrentResponseId.HasValue)
        {
            var submitSuccess = await SubmitAnswerAsync(
                state.CurrentResponseId.Value,
                questionDto.Id,
                answerJson,
                cancellationToken);

            if (!submitSuccess)
            {
                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "Failed to save your answer. Please try again.",
                    cancellationToken: cancellationToken);
                return true;
            }
        }

        // Record answer in state
        await _stateManager.AnswerQuestionAsync(userId, state.CurrentQuestionIndex.Value, answerJson);

        // Check if this was the last question
        var isLastQuestion = state.IsLastQuestion;
        if (!isLastQuestion)
        {
            // Move to next question
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

    /// <summary>
    /// Handles callback query response during active survey (for choice-based questions).
    /// </summary>
    public async Task<bool> HandleCallbackResponseAsync(
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken = default)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message?.Chat.Id ?? userId;

        // Get current state
        var state = await _stateManager.GetStateAsync(userId);
        if (state == null || !state.CurrentQuestionIndex.HasValue || !state.CurrentSurveyId.HasValue)
        {
            _logger.LogDebug("No active survey for user {UserId}", userId);
            return false;
        }

        _logger.LogInformation(
            "Processing callback response for user {UserId} in survey {SurveyId}, question index {QuestionIndex}",
            userId,
            state.CurrentSurveyId,
            state.CurrentQuestionIndex);

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

        // Get handler for this question type
        var handler = _questionHandlers.FirstOrDefault(h => h.QuestionType == questionDto.QuestionType);
        if (handler == null)
        {
            _logger.LogError("No handler found for question type {QuestionType}", questionDto.QuestionType);
            return false;
        }

        // Process answer from callback
        var answerJson = await handler.ProcessAnswerAsync(null, callbackQuery, questionDto, userId, cancellationToken);

        if (answerJson == null)
        {
            // Validation failed or user needs to provide different answer
            _logger.LogDebug("Answer processing returned null for user {UserId}", userId);
            return true; // We handled it, just validation failed
        }

        // Answer callback query to remove loading state
        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "Answer recorded",
            cancellationToken: cancellationToken);

        // Submit answer to API
        if (state.CurrentResponseId.HasValue)
        {
            var submitSuccess = await SubmitAnswerAsync(
                state.CurrentResponseId.Value,
                questionDto.Id,
                answerJson,
                cancellationToken);

            if (!submitSuccess)
            {
                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "Failed to save your answer. Please try again.",
                    cancellationToken: cancellationToken);
                return true;
            }
        }

        // Record answer in state
        await _stateManager.AnswerQuestionAsync(userId, state.CurrentQuestionIndex.Value, answerJson);

        // Check if this was the last question
        var isLastQuestion = state.IsLastQuestion;
        if (!isLastQuestion)
        {
            // Move to next question
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
                            // Fetch from repository instead of HTTP API
                            var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);

                            if (survey == null)
                            {
                                _logger.LogWarning("Survey {SurveyId} not found", surveyId);
                                return null;
                            }

                            // Map entity to DTO
                            return _mapper.Map<SurveyDto>(survey);
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

            // Create submit DTO with the correct structure (wrapped in "answer" property)
            var submitDto = new
            {
                answer = new
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
                }
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
                text: "✅ *Survey Completed!*\n\nThank you for completing the survey!\n\nYour responses have been recorded.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
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
