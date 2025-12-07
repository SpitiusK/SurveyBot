using System.Text.Json;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Bot.Utilities;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SurveyBot.Bot.Handlers.Questions;

/// <summary>
/// Handles text-based questions that accept free-form text input.
/// Includes comprehensive validation and error handling.
/// </summary>
public class TextQuestionHandler : IQuestionHandler
{
    private const int MAX_TEXT_LENGTH = 4000; // Telegram's message limit

    private readonly IBotService _botService;
    private readonly IAnswerValidator _validator;
    private readonly QuestionErrorHandler _errorHandler;
    private readonly QuestionMediaHelper _mediaHelper;
    private readonly ILogger<TextQuestionHandler> _logger;

    public QuestionType QuestionType => QuestionType.Text;

    public TextQuestionHandler(
        IBotService botService,
        IAnswerValidator validator,
        QuestionErrorHandler errorHandler,
        QuestionMediaHelper mediaHelper,
        ILogger<TextQuestionHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _mediaHelper = mediaHelper ?? throw new ArgumentNullException(nameof(mediaHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays the text question to the user.
    /// Sends any attached media first, then the question text.
    /// </summary>
    public async Task<int> DisplayQuestionAsync(
        long chatId,
        QuestionDto question,
        int currentIndex,
        int totalQuestions,
        CancellationToken cancellationToken = default)
    {
        // Send media first if present
        await _mediaHelper.SendQuestionMediaAsync(chatId, question, cancellationToken);

        var progressText = $"Question {currentIndex + 1} of {totalQuestions}";
        var requiredText = question.IsRequired ? "(Required)" : "(Optional - reply /skip to skip)";

        // Build navigation help text
        var navigationHelp = currentIndex > 0 ? "\n\nType /back to go to previous question" : "";
        if (!question.IsRequired)
        {
            navigationHelp += "\nType /skip to skip this question";
        }

        // Convert ReactQuill HTML to Telegram-compatible HTML
        var questionText = HtmlToTelegramConverter.Convert(question.QuestionText);

        var message = $"{progressText}\n\n" +
                      $"<b>{questionText}</b>\n\n" +
                      $"{requiredText}\n\n" +
                      $"Please type your answer below:{navigationHelp}";

        _logger.LogDebug(
            "Displaying text question {QuestionId} in chat {ChatId}",
            question.Id,
            chatId);

        var sentMessage = await _botService.Client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);

        return sentMessage.MessageId;
    }

    /// <summary>
    /// Processes text answer from user's message with comprehensive validation.
    /// </summary>
    public async Task<string?> ProcessAnswerAsync(
        Message? message,
        CallbackQuery? callbackQuery,
        QuestionDto question,
        long userId,
        CancellationToken cancellationToken = default)
    {
        // Text questions only accept message input, not callback queries
        if (message == null || message.Text == null)
        {
            _logger.LogDebug("Text question requires text message input");
            return null;
        }

        var text = message.Text.Trim();
        var chatId = message.Chat.Id;

        // Check if user is trying to skip
        if (text.Equals("/skip", StringComparison.OrdinalIgnoreCase))
        {
            if (question.IsRequired)
            {
                await _errorHandler.ShowValidationErrorAsync(
                    chatId,
                    "This question is required and cannot be skipped. Please provide an answer.",
                    cancellationToken);
                return null;
            }

            // Allow skip for optional questions - return empty answer
            _logger.LogDebug("User {UserId} skipped optional text question {QuestionId}", userId, question.Id);
            return JsonSerializer.Serialize(new { text = "" });
        }

        // Validate text length
        if (text.Length > MAX_TEXT_LENGTH)
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                $"Your answer is too long. Maximum {MAX_TEXT_LENGTH} characters allowed (you entered {text.Length}).\n\n" +
                "Please provide a shorter answer.",
                cancellationToken);
            return null;
        }

        // Validate minimum content for required questions
        if (question.IsRequired && string.IsNullOrWhiteSpace(text))
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                "This question is required. Please provide an answer.",
                cancellationToken);
            return null;
        }

        // Create answer JSON
        var answerJson = JsonSerializer.Serialize(new { text });

        // Final validation using the validator
        var validationResult = _validator.ValidateAnswer(answerJson, question);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Text answer validation failed for question {QuestionId}: {ErrorMessage}",
                question.Id,
                validationResult.ErrorMessage);

            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                validationResult.ErrorMessage!,
                cancellationToken);
            return null;
        }

        _logger.LogDebug(
            "Text answer processed for question {QuestionId} from user {UserId}: length={Length}",
            question.Id,
            userId,
            text.Length);

        return answerJson;
    }

    /// <summary>
    /// Validates text answer format and content.
    /// </summary>
    public bool ValidateAnswer(string? answerJson, QuestionDto question)
    {
        if (string.IsNullOrWhiteSpace(answerJson))
            return !question.IsRequired; // Empty answer OK for optional questions

        try
        {
            var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

            if (!answer.TryGetProperty("text", out var textElement))
                return false;

            var text = textElement.GetString();

            // Required questions must have non-empty text
            if (question.IsRequired && string.IsNullOrWhiteSpace(text))
                return false;

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON format for text answer");
            return false;
        }
    }
}
