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
/// Handles number questions that accept numeric input.
/// Supports integers and decimals with optional range and decimal places validation.
/// Accepts both comma and period as decimal separators.
/// </summary>
public class NumberQuestionHandler : IQuestionHandler
{
    private readonly IBotService _botService;
    private readonly IAnswerValidator _validator;
    private readonly QuestionErrorHandler _errorHandler;
    private readonly QuestionMediaHelper _mediaHelper;
    private readonly ILogger<NumberQuestionHandler> _logger;

    public QuestionType QuestionType => QuestionType.Number;

    public NumberQuestionHandler(
        IBotService botService,
        IAnswerValidator validator,
        QuestionErrorHandler errorHandler,
        QuestionMediaHelper mediaHelper,
        ILogger<NumberQuestionHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _mediaHelper = mediaHelper ?? throw new ArgumentNullException(nameof(mediaHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays the number question to the user.
    /// Sends any attached media first, then the question text with validation hints.
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

        // Parse number configuration for validation hints
        var (minValue, maxValue, decimalPlaces) = ParseNumberConfig(question);
        var validationHint = BuildValidationHint(minValue, maxValue, decimalPlaces);

        // Build navigation help text
        var navigationHelp = currentIndex > 0 ? "\n\nType /back to go to previous question" : "";
        if (!question.IsRequired)
        {
            navigationHelp += "\nType /skip to skip this question";
        }

        var message = $"{progressText}\n\n" +
                      $"*{question.QuestionText}*\n\n" +
                      $"{requiredText}\n" +
                      $"{validationHint}\n" +
                      $"Please enter a number:{navigationHelp}";

        _logger.LogDebug(
            "Displaying number question {QuestionId} in chat {ChatId}",
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
    /// Processes numeric answer from user's message with comprehensive validation.
    /// </summary>
    public async Task<string?> ProcessAnswerAsync(
        Message? message,
        CallbackQuery? callbackQuery,
        QuestionDto question,
        long userId,
        CancellationToken cancellationToken = default)
    {
        // Number questions only accept message input, not callback queries
        if (message == null || string.IsNullOrWhiteSpace(message.Text))
        {
            _logger.LogDebug("Number question requires text message input");
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
                    "This question is required and cannot be skipped. Please enter a number.",
                    cancellationToken);
                return null;
            }

            // Allow skip for optional questions - return empty answer
            _logger.LogDebug("User {UserId} skipped optional number question {QuestionId}", userId, question.Id);
            return JsonSerializer.Serialize(new { number = (decimal?)null });
        }

        // Check if user wants to go back
        if (text.Equals("/back", StringComparison.OrdinalIgnoreCase))
        {
            // Return null to signal back navigation (handled by caller)
            return null;
        }

        // Parse number configuration
        var (minValue, maxValue, decimalPlaces) = ParseNumberConfig(question);

        // Try to parse the number (accept both comma and period as decimal separator)
        var normalizedText = text.Replace(',', '.');
        if (!decimal.TryParse(normalizedText, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var numberValue))
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                $"'{text}' is not a valid number. Please enter a valid number (e.g., 42 or 3.14).",
                cancellationToken);
            return null;
        }

        // Validate range
        if (minValue.HasValue && numberValue < minValue.Value)
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                $"The number must be at least {minValue.Value}. You entered {numberValue}.",
                cancellationToken);
            return null;
        }

        if (maxValue.HasValue && numberValue > maxValue.Value)
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                $"The number must be at most {maxValue.Value}. You entered {numberValue}.",
                cancellationToken);
            return null;
        }

        // Validate decimal places
        if (decimalPlaces.HasValue && decimalPlaces.Value >= 0)
        {
            var actualDecimalPlaces = GetDecimalPlaces(numberValue);
            if (actualDecimalPlaces > decimalPlaces.Value)
            {
                var placesText = decimalPlaces.Value == 0
                    ? "whole numbers only (no decimals)"
                    : $"up to {decimalPlaces.Value} decimal place(s)";
                await _errorHandler.ShowValidationErrorAsync(
                    chatId,
                    $"Please enter {placesText}. You entered {actualDecimalPlaces} decimal place(s).",
                    cancellationToken);
                return null;
            }
        }

        // Create NumberAnswerValue and serialize to JSON
        try
        {
            var answerValue = NumberAnswerValue.Create(numberValue, minValue, maxValue, decimalPlaces);
            var answerJson = answerValue.ToJson();

            // Final validation using the validator
            var validationResult = _validator.ValidateAnswer(answerJson, question);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Number answer validation failed for question {QuestionId}: {ErrorMessage}",
                    question.Id,
                    validationResult.ErrorMessage);

                await _errorHandler.ShowValidationErrorAsync(
                    chatId,
                    validationResult.ErrorMessage!,
                    cancellationToken);
                return null;
            }

            _logger.LogDebug(
                "Number answer processed for question {QuestionId} from user {UserId}: {Value}",
                question.Id,
                userId,
                numberValue);

            return answerJson;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create NumberAnswerValue for question {QuestionId}", question.Id);
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                "Failed to process your answer. Please try again.",
                cancellationToken);
            return null;
        }
    }

    /// <summary>
    /// Validates number answer format and content.
    /// </summary>
    public bool ValidateAnswer(string? answerJson, QuestionDto question)
    {
        if (string.IsNullOrWhiteSpace(answerJson))
            return !question.IsRequired; // Empty answer OK for optional questions

        try
        {
            var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

            if (!answer.TryGetProperty("number", out var numberElement))
                return false;

            // Check if number is null (for optional skipped questions)
            if (numberElement.ValueKind == JsonValueKind.Null)
                return !question.IsRequired;

            if (!numberElement.TryGetDecimal(out var numberValue))
                return false;

            // Validate against question configuration
            var (minValue, maxValue, decimalPlaces) = ParseNumberConfig(question);

            if (minValue.HasValue && numberValue < minValue.Value)
                return false;

            if (maxValue.HasValue && numberValue > maxValue.Value)
                return false;

            if (decimalPlaces.HasValue && decimalPlaces.Value >= 0)
            {
                var actualDecimalPlaces = GetDecimalPlaces(numberValue);
                if (actualDecimalPlaces > decimalPlaces.Value)
                    return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON format for number answer");
            return false;
        }
    }

    #region Private Methods

    /// <summary>
    /// Parses number configuration from question options.
    /// </summary>
    private (decimal? minValue, decimal? maxValue, int? decimalPlaces) ParseNumberConfig(QuestionDto question)
    {
        if (question.Options == null || !question.Options.Any())
            return (null, null, null);

        try
        {
            var optionsJson = question.Options.First();
            if (string.IsNullOrWhiteSpace(optionsJson))
                return (null, null, null);

            var options = JsonSerializer.Deserialize<NumberOptions>(optionsJson);
            if (options != null)
            {
                return (options.MinValue, options.MaxValue, options.DecimalPlaces);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse number config from options, using defaults");
        }

        return (null, null, null);
    }

    /// <summary>
    /// Builds a validation hint message for the user.
    /// </summary>
    private string BuildValidationHint(decimal? minValue, decimal? maxValue, int? decimalPlaces)
    {
        var hints = new List<string>();

        if (minValue.HasValue && maxValue.HasValue)
        {
            hints.Add($"Range: {minValue.Value} to {maxValue.Value}");
        }
        else if (minValue.HasValue)
        {
            hints.Add($"Minimum: {minValue.Value}");
        }
        else if (maxValue.HasValue)
        {
            hints.Add($"Maximum: {maxValue.Value}");
        }

        if (decimalPlaces.HasValue && decimalPlaces.Value >= 0)
        {
            if (decimalPlaces.Value == 0)
            {
                hints.Add("Whole numbers only");
            }
            else
            {
                hints.Add($"Up to {decimalPlaces.Value} decimal place(s)");
            }
        }

        return hints.Count > 0 ? $"({string.Join(", ", hints)})" : "";
    }

    /// <summary>
    /// Gets the number of decimal places in a decimal value.
    /// </summary>
    private int GetDecimalPlaces(decimal value)
    {
        // Remove trailing zeros and count decimal places
        value = value / 1.000000000000000000000000000000000m;
        var text = value.ToString(CultureInfo.InvariantCulture);
        var decimalIndex = text.IndexOf('.');
        if (decimalIndex < 0)
            return 0;
        return text.Length - decimalIndex - 1;
    }

    #endregion

    /// <summary>
    /// Number options from question configuration.
    /// </summary>
    private sealed class NumberOptions
    {
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int? DecimalPlaces { get; set; }
    }
}
