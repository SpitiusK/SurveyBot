using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using SurveyBot.Core.ValueObjects.Answers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SurveyBot.Bot.Handlers.Questions;

/// <summary>
/// Handles date questions that accept dates in DD.MM.YYYY format.
/// Supports optional date range validation.
/// IMPORTANT: Automatically appends format hint to question text.
/// </summary>
public class DateQuestionHandler : IQuestionHandler
{
    private readonly IBotService _botService;
    private readonly IAnswerValidator _validator;
    private readonly QuestionErrorHandler _errorHandler;
    private readonly QuestionMediaHelper _mediaHelper;
    private readonly ILogger<DateQuestionHandler> _logger;

    public QuestionType QuestionType => QuestionType.Date;

    public DateQuestionHandler(
        IBotService botService,
        IAnswerValidator validator,
        QuestionErrorHandler errorHandler,
        QuestionMediaHelper mediaHelper,
        ILogger<DateQuestionHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _mediaHelper = mediaHelper ?? throw new ArgumentNullException(nameof(mediaHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays the date question to the user.
    /// IMPORTANT: Appends format hint "(format: DD.MM.YYYY)" to question text.
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

        // Parse date configuration for validation hints
        var (minDate, maxDate) = ParseDateConfig(question);
        var validationHint = BuildValidationHint(minDate, maxDate);

        // Build navigation help text
        var navigationHelp = currentIndex > 0 ? "\n\nType /back to go to previous question" : "";
        if (!question.IsRequired)
        {
            navigationHelp += "\nType /skip to skip this question";
        }

        // IMPORTANT: Format hint is APPENDED to question text
        var todayExample = DateTime.Today.ToString(DateAnswerValue.DateFormat, CultureInfo.InvariantCulture);
        var formatHint = $"(Please answer in DD.MM.YYYY format, e.g., {todayExample})";

        var message = $"{progressText}\n\n" +
                      $"*{question.QuestionText}*\n" +
                      $"{formatHint}\n\n" +
                      $"{requiredText}\n" +
                      $"{validationHint}" +
                      $"{navigationHelp}";

        _logger.LogDebug(
            "Displaying date question {QuestionId} in chat {ChatId}",
            question.Id,
            chatId);

        var sentMessage = await _botService.Client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);

        return sentMessage.MessageId;
    }

    /// <summary>
    /// Processes date answer from user's message with comprehensive validation.
    /// Only accepts DD.MM.YYYY format.
    /// </summary>
    public async Task<string?> ProcessAnswerAsync(
        Message? message,
        CallbackQuery? callbackQuery,
        QuestionDto question,
        long userId,
        CancellationToken cancellationToken = default)
    {
        // Date questions only accept message input, not callback queries
        if (message == null || string.IsNullOrWhiteSpace(message.Text))
        {
            _logger.LogDebug("Date question requires text message input");
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
                    "This question is required and cannot be skipped. Please enter a date.",
                    cancellationToken);
                return null;
            }

            // Allow skip for optional questions - return empty answer
            _logger.LogDebug("User {UserId} skipped optional date question {QuestionId}", userId, question.Id);
            return JsonSerializer.Serialize(new { date = (DateTime?)null });
        }

        // Check if user wants to go back
        if (text.Equals("/back", StringComparison.OrdinalIgnoreCase))
        {
            // Return null to signal back navigation (handled by caller)
            return null;
        }

        // Parse date configuration
        var (minDate, maxDate) = ParseDateConfig(question);

        // Try to parse the date using EXACT format DD.MM.YYYY
        var todayExample = DateTime.Today.ToString(DateAnswerValue.DateFormat, CultureInfo.InvariantCulture);

        if (!DateTime.TryParseExact(text, DateAnswerValue.DateFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var dateValue))
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                $"'{text}' is not a valid date format.\n\n" +
                $"Please enter the date in DD.MM.YYYY format (e.g., {todayExample}).",
                cancellationToken);
            return null;
        }

        // Strip time component
        dateValue = dateValue.Date;

        // Validate range
        if (minDate.HasValue && dateValue < minDate.Value.Date)
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                $"The date must be on or after {minDate.Value:dd.MM.yyyy}. You entered {dateValue:dd.MM.yyyy}.",
                cancellationToken);
            return null;
        }

        if (maxDate.HasValue && dateValue > maxDate.Value.Date)
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                $"The date must be on or before {maxDate.Value:dd.MM.yyyy}. You entered {dateValue:dd.MM.yyyy}.",
                cancellationToken);
            return null;
        }

        // Create DateAnswerValue and serialize to JSON
        try
        {
            var answerValue = DateAnswerValue.Create(dateValue, minDate, maxDate);
            var answerJson = answerValue.ToJson();

            // Final validation using the validator
            var validationResult = _validator.ValidateAnswer(answerJson, question);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Date answer validation failed for question {QuestionId}: {ErrorMessage}",
                    question.Id,
                    validationResult.ErrorMessage);

                await _errorHandler.ShowValidationErrorAsync(
                    chatId,
                    validationResult.ErrorMessage!,
                    cancellationToken);
                return null;
            }

            _logger.LogDebug(
                "Date answer processed for question {QuestionId} from user {UserId}: {Value}",
                question.Id,
                userId,
                dateValue.ToString(DateAnswerValue.DateFormat));

            return answerJson;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create DateAnswerValue for question {QuestionId}", question.Id);
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                "Failed to process your answer. Please try again.",
                cancellationToken);
            return null;
        }
    }

    /// <summary>
    /// Validates date answer format and content.
    /// </summary>
    public bool ValidateAnswer(string? answerJson, QuestionDto question)
    {
        if (string.IsNullOrWhiteSpace(answerJson))
            return !question.IsRequired; // Empty answer OK for optional questions

        try
        {
            var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

            if (!answer.TryGetProperty("date", out var dateElement))
                return false;

            // Check if date is null (for optional skipped questions)
            if (dateElement.ValueKind == JsonValueKind.Null)
                return !question.IsRequired;

            if (!dateElement.TryGetDateTime(out var dateValue))
                return false;

            // Strip time component
            dateValue = dateValue.Date;

            // Validate against question configuration
            var (minDate, maxDate) = ParseDateConfig(question);

            if (minDate.HasValue && dateValue < minDate.Value.Date)
                return false;

            if (maxDate.HasValue && dateValue > maxDate.Value.Date)
                return false;

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON format for date answer");
            return false;
        }
    }

    #region Private Methods

    /// <summary>
    /// Parses date configuration from question options.
    /// </summary>
    private (DateTime? minDate, DateTime? maxDate) ParseDateConfig(QuestionDto question)
    {
        if (question.Options == null || !question.Options.Any())
            return (null, null);

        try
        {
            var optionsJson = question.Options.First();
            if (string.IsNullOrWhiteSpace(optionsJson))
                return (null, null);

            var options = JsonSerializer.Deserialize<DateOptions>(optionsJson);
            if (options != null)
            {
                return (options.MinDate, options.MaxDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse date config from options, using defaults");
        }

        return (null, null);
    }

    /// <summary>
    /// Builds a validation hint message for the user.
    /// </summary>
    private string BuildValidationHint(DateTime? minDate, DateTime? maxDate)
    {
        var hints = new List<string>();

        if (minDate.HasValue && maxDate.HasValue)
        {
            hints.Add($"Date range: {minDate.Value:dd.MM.yyyy} to {maxDate.Value:dd.MM.yyyy}");
        }
        else if (minDate.HasValue)
        {
            hints.Add($"Earliest date: {minDate.Value:dd.MM.yyyy}");
        }
        else if (maxDate.HasValue)
        {
            hints.Add($"Latest date: {maxDate.Value:dd.MM.yyyy}");
        }

        return hints.Count > 0 ? $"({string.Join(", ", hints)})\n" : "";
    }

    #endregion

    /// <summary>
    /// Date options from question configuration.
    /// </summary>
    private sealed class DateOptions
    {
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }
    }
}
