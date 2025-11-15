using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SurveyBot.Bot.Handlers.Commands;

/// <summary>
/// Handles the /survey command to start taking a survey.
/// Supports both survey ID and survey code formats:
/// - /survey 123 (by ID)
/// - /survey ABCD12 (by code - future feature)
/// </summary>
public class SurveyCommandHandler : ICommandHandler
{
    private readonly IBotService _botService;
    private readonly ISurveyRepository _surveyRepository;
    private readonly IResponseRepository _responseRepository;
    private readonly IConversationStateManager _stateManager;
    private readonly CompletionHandler _completionHandler;
    private readonly ILogger<SurveyCommandHandler> _logger;
    private readonly Dictionary<Core.Entities.QuestionType, IQuestionHandler> _questionHandlers;

    public string Command => "survey";

    public SurveyCommandHandler(
        IBotService botService,
        ISurveyRepository surveyRepository,
        IResponseRepository responseRepository,
        IConversationStateManager stateManager,
        CompletionHandler completionHandler,
        IEnumerable<IQuestionHandler> questionHandlers,
        ILogger<SurveyCommandHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _surveyRepository = surveyRepository ?? throw new ArgumentNullException(nameof(surveyRepository));
        _responseRepository = responseRepository ?? throw new ArgumentNullException(nameof(responseRepository));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _completionHandler = completionHandler ?? throw new ArgumentNullException(nameof(completionHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Build dictionary of question handlers by type
        _questionHandlers = questionHandlers?.ToDictionary(h => h.QuestionType)
            ?? throw new ArgumentNullException(nameof(questionHandlers));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null)
        {
            _logger.LogWarning("Received /survey command with null From user");
            return;
        }

        var telegramUser = message.From;
        var chatId = message.Chat.Id;
        var userId = telegramUser.Id;

        try
        {
            _logger.LogInformation(
                "Processing /survey command from user {TelegramId} (@{Username})",
                userId,
                telegramUser.Username ?? "no_username");

            // Parse survey identifier from command
            var identifier = ParseSurveyIdentifier(message.Text);
            if (identifier == null)
            {
                await SendUsageMessageAsync(chatId, cancellationToken);
                return;
            }

            // Validate survey exists and is active - support both ID and code
            Core.Entities.Survey? survey = null;

            // Try parsing as integer ID first
            if (int.TryParse(identifier, out var surveyId) && surveyId > 0)
            {
                survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);
            }
            else
            {
                // Otherwise treat as survey code
                survey = await _surveyRepository.GetByCodeWithQuestionsAsync(identifier);
            }

            if (survey == null)
            {
                await SendSurveyNotFoundAsync(chatId, identifier, cancellationToken);
                return;
            }

            if (!survey.IsActive)
            {
                await SendSurveyInactiveAsync(chatId, survey.Title, cancellationToken);
                return;
            }

            if (survey.Questions == null || survey.Questions.Count == 0)
            {
                await SendSurveyNoQuestionsAsync(chatId, survey.Title, cancellationToken);
                return;
            }

            // Check for existing responses
            var existingResponse = await _responseRepository.GetIncompleteResponseAsync(survey.Id, userId);
            if (existingResponse != null)
            {
                // Resume existing response
                await ResumeExistingResponseAsync(chatId, userId, survey, existingResponse, cancellationToken);
                return;
            }

            // Check for duplicate responses (if not allowed)
            if (!survey.AllowMultipleResponses)
            {
                var completedResponses = await _responseRepository.GetByUserAndSurveyAsync(survey.Id, userId);
                var completedResponse = completedResponses.FirstOrDefault(r => r.IsComplete);
                if (completedResponse != null)
                {
                    await SendDuplicateResponseErrorAsync(chatId, survey.Title, cancellationToken);
                    return;
                }
            }

            // Create new response record
            var response = await _responseRepository.CreateAsync(new Core.Entities.Response
            {
                SurveyId = survey.Id,
                RespondentTelegramId = userId,
                IsComplete = false,
                StartedAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Created response {ResponseId} for user {TelegramId} on survey {SurveyId}",
                response.Id,
                userId,
                survey.Id);

            // Initialize conversation state
            var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();
            var totalQuestions = questions.Count;

            await _stateManager.StartSurveyAsync(userId, survey.Id, response.Id, totalQuestions);

            // Send survey intro message
            await SendSurveyIntroAsync(chatId, survey.Title, survey.Description, totalQuestions, cancellationToken);

            // Display first question
            var firstQuestion = questions.First();
            await DisplayQuestionAsync(chatId, firstQuestion, 0, totalQuestions, userId, cancellationToken);

            _logger.LogInformation(
                "Survey {SurveyId} started for user {TelegramId}, displayed question 1/{TotalQuestions}",
                surveyId,
                userId,
                totalQuestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing /survey command for user {TelegramId}",
                userId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, an error occurred while starting the survey. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    public string GetDescription()
    {
        return "Start taking a survey by ID: /survey <id>";
    }

    #region Private Methods

    /// <summary>
    /// Parses survey ID from command text.
    /// Supports: /survey 123 or /survey ABCD12 (code - future)
    /// </summary>
    private string? ParseSurveyIdentifier(string? commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
            return null;

        var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return null;

        var identifier = parts[1].Trim();

        // Return identifier as-is - can be either ID (numeric) or code (alphanumeric)
        return string.IsNullOrWhiteSpace(identifier) ? null : identifier;
    }

    /// <summary>
    /// Checks if all questions have been answered and triggers completion if needed.
    /// Should be called after each answer is processed.
    /// </summary>
    public async Task<bool> CheckAndHandleCompletionAsync(
        long chatId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isAllAnswered = await _stateManager.IsAllAnsweredAsync(userId);

            if (isAllAnswered)
            {
                _logger.LogInformation(
                    "All questions answered for user {TelegramId}, triggering completion",
                    userId);

                await _completionHandler.HandleCompletionAsync(chatId, userId, cancellationToken);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking completion status for user {TelegramId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Displays a question using the appropriate handler
    /// </summary>
    private async Task DisplayQuestionAsync(
        long chatId,
        Core.Entities.Question question,
        int currentIndex,
        int totalQuestions,
        long userId,
        CancellationToken cancellationToken)
    {
        var questionDto = MapQuestionToDto(question);

        if (!_questionHandlers.TryGetValue(question.QuestionType, out var handler))
        {
            _logger.LogError("No handler found for question type {QuestionType}", question.QuestionType);
            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, this question type is not supported yet.",
                cancellationToken: cancellationToken);
            return;
        }

        await handler.DisplayQuestionAsync(chatId, questionDto, currentIndex, totalQuestions, cancellationToken);

        // Transition state
        await _stateManager.TryTransitionAsync(userId, Models.ConversationStateType.AnsweringQuestion);
    }

    /// <summary>
    /// Resumes an incomplete survey response
    /// </summary>
    private async Task ResumeExistingResponseAsync(
        long chatId,
        long userId,
        Core.Entities.Survey survey,
        Core.Entities.Response existingResponse,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Resuming incomplete response {ResponseId} for user {TelegramId}",
            existingResponse.Id,
            userId);

        var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();
        var totalQuestions = questions.Count;

        // Get answers already provided
        var responseWithAnswers = await _responseRepository.GetByIdWithAnswersAsync(existingResponse.Id);
        var answeredQuestionIds = responseWithAnswers?.Answers?.Select(a => a.QuestionId).ToHashSet() ?? new HashSet<int>();

        // Find first unanswered question
        var nextQuestionIndex = 0;
        for (int i = 0; i < questions.Count; i++)
        {
            if (!answeredQuestionIds.Contains(questions[i].Id))
            {
                nextQuestionIndex = i;
                break;
            }
        }

        // Initialize state
        await _stateManager.StartSurveyAsync(userId, survey.Id, existingResponse.Id, totalQuestions);

        // Update to correct question index
        for (int i = 0; i < nextQuestionIndex; i++)
        {
            await _stateManager.NextQuestionAsync(userId);
            await _stateManager.AnswerQuestionAsync(userId, i, "{}"); // Mark as answered
        }

        await _botService.Client.SendMessage(
            chatId: chatId,
            text: $"Resuming survey: *{survey.Title}*\n\n" +
                  $"You have answered {answeredQuestionIds.Count} of {totalQuestions} questions.\n" +
                  $"Let's continue...",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: cancellationToken);

        // Display next unanswered question
        if (nextQuestionIndex < questions.Count)
        {
            await DisplayQuestionAsync(chatId, questions[nextQuestionIndex], nextQuestionIndex, totalQuestions, userId, cancellationToken);
        }
    }

    /// <summary>
    /// Maps Question entity to DTO
    /// </summary>
    private Core.DTOs.Question.QuestionDto MapQuestionToDto(Core.Entities.Question question)
    {
        List<string>? options = null;
        if (!string.IsNullOrWhiteSpace(question.OptionsJson))
        {
            try
            {
                options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(question.OptionsJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize options JSON for question {QuestionId}", question.Id);
            }
        }

        return new Core.DTOs.Question.QuestionDto
        {
            Id = question.Id,
            SurveyId = question.SurveyId,
            QuestionText = question.QuestionText,
            QuestionType = question.QuestionType,
            OrderIndex = question.OrderIndex,
            IsRequired = question.IsRequired,
            Options = options,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt
        };
    }

    #endregion

    #region Message Methods

    private async Task SendUsageMessageAsync(long chatId, CancellationToken cancellationToken)
    {
        await _botService.Client.SendMessage(
            chatId: chatId,
            text: "Usage: /survey <survey_id>\n\n" +
                  "Example: /survey 5\n\n" +
                  "To find available surveys, use /surveys command.",
            cancellationToken: cancellationToken);
    }

    private async Task SendSurveyNotFoundAsync(long chatId, string identifier, CancellationToken cancellationToken)
    {
        await _botService.Client.SendMessage(
            chatId: chatId,
            text: $"Survey '{identifier}' not found or is not active.\n\n" +
                  "Use /surveys to browse available surveys.",
            cancellationToken: cancellationToken);
    }

    private async Task SendSurveyInactiveAsync(long chatId, string surveyTitle, CancellationToken cancellationToken)
    {
        await _botService.Client.SendMessage(
            chatId: chatId,
            text: $"Sorry, the survey \"{surveyTitle}\" is not currently accepting responses.\n\n" +
                  "Please check back later or use /surveys to find other surveys.",
            cancellationToken: cancellationToken);
    }

    private async Task SendSurveyNoQuestionsAsync(long chatId, string surveyTitle, CancellationToken cancellationToken)
    {
        await _botService.Client.SendMessage(
            chatId: chatId,
            text: $"The survey \"{surveyTitle}\" has no questions yet.\n\n" +
                  "Please try another survey using /surveys command.",
            cancellationToken: cancellationToken);
    }

    private async Task SendDuplicateResponseErrorAsync(long chatId, string surveyTitle, CancellationToken cancellationToken)
    {
        await _botService.Client.SendMessage(
            chatId: chatId,
            text: $"You have already completed the survey \"{surveyTitle}\".\n\n" +
                  "This survey does not allow multiple responses.\n\n" +
                  "Use /surveys to find other surveys.",
            cancellationToken: cancellationToken);
    }

    private async Task SendSurveyIntroAsync(
        long chatId,
        string title,
        string? description,
        int totalQuestions,
        CancellationToken cancellationToken)
    {
        var message = $"*{title}*\n\n";

        if (!string.IsNullOrWhiteSpace(description))
        {
            message += $"{description}\n\n";
        }

        message += $"This survey has {totalQuestions} question{(totalQuestions != 1 ? "s" : "")}.\n\n";
        message += "You can:\n";
        message += "- Answer each question as it appears\n";
        message += "- Use /cancel to stop at any time\n\n";
        message += "Let's begin...";

        await _botService.Client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: cancellationToken);

        // Small delay before first question
        await Task.Delay(1000, cancellationToken);
    }

    #endregion
}
