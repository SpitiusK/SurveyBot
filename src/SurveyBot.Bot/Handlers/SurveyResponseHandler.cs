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
    private readonly IQuestionService _questionService;
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
        IQuestionService questionService,
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
        _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));
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
        if (state == null || !state.CurrentSurveyId.HasValue)
        {
            _logger.LogDebug("No active survey for user {UserId}", userId);
            return false;
        }

        // Determine current question ID (prefer new property, fallback to index-based)
        int? currentQuestionId = state.CurrentQuestionId;
        if (!currentQuestionId.HasValue && state.CurrentQuestionIndex.HasValue)
        {
            // Fallback: get question by index
            var survey = await FetchSurveyWithQuestionsAsync(state.CurrentSurveyId.Value, cancellationToken);
            if (survey?.Questions != null && state.CurrentQuestionIndex.Value < survey.Questions.Count)
            {
                currentQuestionId = survey.Questions[state.CurrentQuestionIndex.Value].Id;
            }
        }

        if (!currentQuestionId.HasValue)
        {
            _logger.LogWarning("No current question ID for user {UserId}", userId);
            return false;
        }

        _logger.LogInformation(
            "Processing message response for user {UserId} in survey {SurveyId}, question {QuestionId}",
            userId,
            state.CurrentSurveyId,
            currentQuestionId);

        // Get current question by ID
        QuestionDto questionDto;
        try
        {
            questionDto = await _questionService.GetQuestionAsync(currentQuestionId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch question {QuestionId}", currentQuestionId);
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

        // Extract raw answer value for branching condition evaluation
        var rawAnswerValue = ExtractRawAnswerValue(answerJson, questionDto.QuestionType);

        _logger.LogDebug(
            "Extracted raw answer value for branching: {RawValue} from JSON: {AnswerJson}",
            rawAnswerValue ?? "null",
            answerJson);

        // Get next question using branching logic
        var nextQuestionId = await GetNextQuestionAsync(
            currentQuestionId.Value,
            rawAnswerValue ?? string.Empty,
            state.CurrentSurveyId.Value,
            cancellationToken);

        if (nextQuestionId.HasValue)
        {
            // Update state with next question ID
            await _stateManager.NextQuestionByIdAsync(userId, nextQuestionId.Value, answerJson);

            // Calculate position for display
            var answeredCount = await _stateManager.GetAnsweredCountAsync(userId);
            var totalQuestions = state.TotalQuestions ?? 0;

            // Display next question
            await DisplayQuestionByIdAsync(
                chatId,
                nextQuestionId.Value,
                state.CurrentSurveyId.Value,
                answeredCount + 1,
                totalQuestions,
                cancellationToken);
        }
        else
        {
            // No next question - complete survey
            _logger.LogInformation(
                "Survey {SurveyId} complete for user {UserId}",
                state.CurrentSurveyId,
                userId);
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
        if (state == null || !state.CurrentSurveyId.HasValue)
        {
            _logger.LogDebug("No active survey for user {UserId}", userId);
            return false;
        }

        // Determine current question ID (prefer new property, fallback to index-based)
        int? currentQuestionId = state.CurrentQuestionId;
        if (!currentQuestionId.HasValue && state.CurrentQuestionIndex.HasValue)
        {
            // Fallback: get question by index
            var survey = await FetchSurveyWithQuestionsAsync(state.CurrentSurveyId.Value, cancellationToken);
            if (survey?.Questions != null && state.CurrentQuestionIndex.Value < survey.Questions.Count)
            {
                currentQuestionId = survey.Questions[state.CurrentQuestionIndex.Value].Id;
            }
        }

        if (!currentQuestionId.HasValue)
        {
            _logger.LogWarning("No current question ID for user {UserId}", userId);
            return false;
        }

        _logger.LogInformation(
            "Processing callback response for user {UserId} in survey {SurveyId}, question {QuestionId}",
            userId,
            state.CurrentSurveyId,
            currentQuestionId);

        // Get current question by ID
        QuestionDto questionDto;
        try
        {
            questionDto = await _questionService.GetQuestionAsync(currentQuestionId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch question {QuestionId}", currentQuestionId);
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

        // Extract raw answer value for branching condition evaluation
        var rawAnswerValue = ExtractRawAnswerValue(answerJson, questionDto.QuestionType);

        _logger.LogDebug(
            "Extracted raw answer value for branching: {RawValue} from JSON: {AnswerJson}",
            rawAnswerValue ?? "null",
            answerJson);

        // Get next question using branching logic
        var nextQuestionId = await GetNextQuestionAsync(
            currentQuestionId.Value,
            rawAnswerValue ?? string.Empty,
            state.CurrentSurveyId.Value,
            cancellationToken);

        if (nextQuestionId.HasValue)
        {
            // Update state with next question ID
            await _stateManager.NextQuestionByIdAsync(userId, nextQuestionId.Value, answerJson);

            // Calculate position for display
            var answeredCount = await _stateManager.GetAnsweredCountAsync(userId);
            var totalQuestions = state.TotalQuestions ?? 0;

            // Display next question
            await DisplayQuestionByIdAsync(
                chatId,
                nextQuestionId.Value,
                state.CurrentSurveyId.Value,
                answeredCount + 1,
                totalQuestions,
                cancellationToken);
        }
        else
        {
            // No next question - complete survey
            _logger.LogInformation(
                "Survey {SurveyId} complete for user {UserId}",
                state.CurrentSurveyId,
                userId);
            await CompleteSurveyAsync(userId, state.CurrentResponseId!.Value, chatId, cancellationToken);
        }

        return true;
    }

    #region Private Helper Methods

    /// <summary>
    /// Extracts the raw answer value from answer JSON for branching condition evaluation.
    /// Converts structured JSON answer to the raw value that branching conditions expect.
    /// </summary>
    /// <param name="answerJson">The JSON answer string from question handler</param>
    /// <param name="questionType">The type of question</param>
    /// <returns>Raw answer value (e.g., "Option 1", "5", "Yes") or null if extraction fails</returns>
    private string? ExtractRawAnswerValue(string answerJson, SurveyBot.Core.Entities.QuestionType questionType)
    {
        if (string.IsNullOrWhiteSpace(answerJson))
        {
            _logger.LogWarning("Answer JSON is null or empty");
            return null;
        }

        try
        {
            var answerElement = JsonSerializer.Deserialize<JsonElement>(answerJson);

            switch (questionType)
            {
                case SurveyBot.Core.Entities.QuestionType.Text:
                    // Extract from: {"text": "User's answer"}
                    if (answerElement.TryGetProperty("text", out var textValue))
                    {
                        return textValue.GetString();
                    }
                    break;

                case SurveyBot.Core.Entities.QuestionType.SingleChoice:
                    // Extract from: {"selectedOption": "Option 1"}
                    if (answerElement.TryGetProperty("selectedOption", out var selectedOption))
                    {
                        return selectedOption.GetString();
                    }
                    break;

                case SurveyBot.Core.Entities.QuestionType.MultipleChoice:
                    // Extract from: {"selectedOptions": ["Option A", "Option B"]}
                    // For branching, we'll check if any of the selected options match
                    // Return first selected option for simple matching, or comma-separated for "In" operator
                    if (answerElement.TryGetProperty("selectedOptions", out var selectedOptions) &&
                        selectedOptions.ValueKind == JsonValueKind.Array)
                    {
                        var options = selectedOptions.EnumerateArray()
                            .Select(e => e.GetString())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();

                        if (options.Any())
                        {
                            // Return first option for simple equals comparison
                            // Branching logic can handle checking if value is in the array
                            return options.First();
                        }
                    }
                    break;

                case SurveyBot.Core.Entities.QuestionType.Rating:
                    // Extract from: {"rating": 4}
                    if (answerElement.TryGetProperty("rating", out var rating))
                    {
                        if (rating.ValueKind == JsonValueKind.Number)
                        {
                            return rating.GetInt32().ToString();
                        }
                        else if (rating.ValueKind == JsonValueKind.String)
                        {
                            return rating.GetString();
                        }
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown question type: {QuestionType}", questionType);
                    break;
            }

            _logger.LogWarning(
                "Could not extract raw answer value from JSON: {AnswerJson} for question type: {QuestionType}",
                answerJson,
                questionType);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "JSON deserialization error extracting raw answer from: {AnswerJson}",
                answerJson);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error extracting raw answer from: {AnswerJson}",
                answerJson);
            return null;
        }
    }

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
    /// Determines the next question ID to display based on branching rules or sequential order.
    /// </summary>
    /// <param name="currentQuestionId">Current question ID</param>
    /// <param name="answer">Answer provided for current question</param>
    /// <param name="surveyId">Survey ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next question ID, or null if survey is complete</returns>
    private async Task<int?> GetNextQuestionAsync(
        int currentQuestionId,
        string answer,
        int surveyId,
        CancellationToken cancellationToken)
    {
        try
        {
            // First, try to get next question using branching rules
            var nextQuestionId = await _questionService.GetNextQuestionAsync(
                currentQuestionId,
                answer,
                surveyId);

            if (nextQuestionId.HasValue)
            {
                _logger.LogDebug(
                    "Branching rule matched: question {CurrentQuestionId} -> {NextQuestionId}",
                    currentQuestionId,
                    nextQuestionId.Value);
                return nextQuestionId.Value;
            }

            // No branching rule matched, fallback to sequential navigation
            _logger.LogDebug(
                "No branching rule matched for question {CurrentQuestionId}, using sequential navigation",
                currentQuestionId);

            // Fetch survey with questions to find next sequential question
            var survey = await FetchSurveyWithQuestionsAsync(surveyId, cancellationToken);
            if (survey?.Questions == null || survey.Questions.Count == 0)
            {
                _logger.LogWarning("Survey {SurveyId} has no questions", surveyId);
                return null;
            }

            // Find current question in the list
            var currentIndex = survey.Questions.FindIndex(q => q.Id == currentQuestionId);
            if (currentIndex == -1)
            {
                _logger.LogWarning(
                    "Current question {QuestionId} not found in survey {SurveyId}",
                    currentQuestionId,
                    surveyId);
                return null;
            }

            // Check if there's a next question
            if (currentIndex < survey.Questions.Count - 1)
            {
                var nextQuestion = survey.Questions[currentIndex + 1];
                _logger.LogDebug(
                    "Sequential navigation: question {CurrentQuestionId} -> {NextQuestionId}",
                    currentQuestionId,
                    nextQuestion.Id);
                return nextQuestion.Id;
            }

            // No next question - survey complete
            _logger.LogDebug(
                "No next question after {CurrentQuestionId}, survey {SurveyId} is complete",
                currentQuestionId,
                surveyId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error determining next question after {CurrentQuestionId} in survey {SurveyId}",
                currentQuestionId,
                surveyId);
            return null;
        }
    }

    /// <summary>
    /// Displays a question by ID using the appropriate handler.
    /// </summary>
    /// <param name="chatId">Chat ID to send message to</param>
    /// <param name="questionId">Question ID to display</param>
    /// <param name="surveyId">Survey ID</param>
    /// <param name="currentPosition">Current position in survey (for display)</param>
    /// <param name="totalQuestions">Total questions in survey</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task<bool> DisplayQuestionByIdAsync(
        long chatId,
        int questionId,
        int surveyId,
        int currentPosition,
        int totalQuestions,
        CancellationToken cancellationToken)
    {
        try
        {
            // Fetch the question
            var questionDto = await _questionService.GetQuestionAsync(questionId);
            if (questionDto == null)
            {
                _logger.LogError("Question {QuestionId} not found", questionId);
                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "Error: Question not found. Please contact support.",
                    cancellationToken: cancellationToken);
                return false;
            }

            // Display using appropriate handler
            await DisplayQuestionAsync(
                chatId,
                questionDto,
                currentPosition,
                totalQuestions,
                cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying question {QuestionId}", questionId);
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
