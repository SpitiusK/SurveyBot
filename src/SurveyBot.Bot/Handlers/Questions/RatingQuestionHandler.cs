using System.Text.Json;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SurveyBot.Bot.Handlers.Questions;

/// <summary>
/// Handles rating questions with numeric scales (1-5 or 1-10).
/// Displays inline keyboard with rating buttons.
/// Includes comprehensive validation and error handling.
/// </summary>
public class RatingQuestionHandler : IQuestionHandler
{
    private readonly IBotService _botService;
    private readonly IAnswerValidator _validator;
    private readonly QuestionErrorHandler _errorHandler;
    private readonly ILogger<RatingQuestionHandler> _logger;

    private const int DEFAULT_MIN_RATING = 1;
    private const int DEFAULT_MAX_RATING = 5;
    private const int MAX_BUTTONS_PER_ROW = 5;

    public QuestionType QuestionType => QuestionType.Rating;

    public RatingQuestionHandler(
        IBotService botService,
        IAnswerValidator validator,
        QuestionErrorHandler errorHandler,
        ILogger<RatingQuestionHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays rating question with inline keyboard.
    /// </summary>
    public async Task<int> DisplayQuestionAsync(
        long chatId,
        QuestionDto question,
        int currentIndex,
        int totalQuestions,
        CancellationToken cancellationToken = default)
    {
        var (minRating, maxRating) = ParseRatingRange(question);

        var progressText = $"Question {currentIndex + 1} of {totalQuestions}";
        var requiredText = question.IsRequired ? "(Required)" : "(Optional)";

        var message = $"{progressText}\n\n" +
                      $"*{question.QuestionText}*\n\n" +
                      $"{requiredText}\n" +
                      $"Rate from {minRating} to {maxRating}:";

        // Build inline keyboard with rating buttons
        var keyboard = BuildKeyboard(question, minRating, maxRating);

        _logger.LogDebug(
            "Displaying rating question {QuestionId} (range: {MinRating}-{MaxRating}) in chat {ChatId}",
            question.Id,
            minRating,
            maxRating,
            chatId);

        var sentMessage = await _botService.Client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);

        return sentMessage.MessageId;
    }

    /// <summary>
    /// Processes rating answer from callback query (button click).
    /// </summary>
    public async Task<string?> ProcessAnswerAsync(
        Message? message,
        CallbackQuery? callbackQuery,
        QuestionDto question,
        long userId,
        CancellationToken cancellationToken = default)
    {
        // Rating questions require callback query (button click)
        if (callbackQuery == null || string.IsNullOrWhiteSpace(callbackQuery.Data))
        {
            _logger.LogDebug("Rating question requires callback query");
            return null;
        }

        var callbackData = callbackQuery.Data;

        // Check if user clicked "Skip"
        if (callbackData == $"skip_q{question.Id}")
        {
            if (question.IsRequired)
            {
                await _botService.Client.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "This question is required and cannot be skipped.",
                    showAlert: true,
                    cancellationToken: cancellationToken);

                return null;
            }

            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Question skipped",
                cancellationToken: cancellationToken);

            // Return null rating for skipped optional question
            return JsonSerializer.Serialize(new { rating = (int?)null });
        }

        // Parse callback data: "rating_q{questionId}_r{ratingValue}"
        if (!TryParseCallbackData(callbackData, question.Id, out var rating))
        {
            _logger.LogWarning(
                "Invalid callback data format: {CallbackData}",
                callbackData);

            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Invalid rating. Please try again.",
                cancellationToken: cancellationToken);

            return null;
        }

        // Validate rating is in valid range
        var (minRating, maxRating) = ParseRatingRange(question);
        if (rating < minRating || rating > maxRating)
        {
            _logger.LogWarning(
                "Rating {Rating} out of range ({MinRating}-{MaxRating}) for question {QuestionId}",
                rating,
                minRating,
                maxRating,
                question.Id);

            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: $"Rating must be between {minRating} and {maxRating}.",
                cancellationToken: cancellationToken);

            return null;
        }

        // Create answer JSON
        var answerJson = JsonSerializer.Serialize(new { rating });

        // Validate the answer
        var validationResult = _validator.ValidateAnswer(answerJson, question);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Rating answer validation failed for question {QuestionId}: {ErrorMessage}",
                question.Id,
                validationResult.ErrorMessage);

            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: validationResult.ErrorMessage!,
                showAlert: true,
                cancellationToken: cancellationToken);

            return null;
        }

        _logger.LogDebug(
            "Rating answer processed for question {QuestionId} from user {UserId}: {Rating}",
            question.Id,
            userId,
            rating);

        // Answer the callback query
        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: $"Rated: {rating}/{maxRating}",
            cancellationToken: cancellationToken);

        // Update the message to show selection
        if (callbackQuery.Message != null)
        {
            try
            {
                var stars = GenerateStarRating(rating, maxRating);
                await _botService.Client.EditMessageText(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: $"{callbackQuery.Message.Text}\n\n✓ Your rating: {stars} ({rating}/{maxRating})",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to edit message after rating selection");
            }
        }

        return answerJson;
    }

    /// <summary>
    /// Validates rating answer format and content.
    /// </summary>
    public bool ValidateAnswer(string? answerJson, QuestionDto question)
    {
        if (string.IsNullOrWhiteSpace(answerJson))
            return !question.IsRequired;

        try
        {
            var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

            if (!answer.TryGetProperty("rating", out var ratingElement))
                return false;

            // Check if rating is null (for optional skipped questions)
            if (ratingElement.ValueKind == JsonValueKind.Null)
                return !question.IsRequired;

            if (!ratingElement.TryGetInt32(out var rating))
                return false;

            // Validate rating is in valid range
            var (minRating, maxRating) = ParseRatingRange(question);
            if (rating < minRating || rating > maxRating)
                return false;

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON format for rating answer");
            return false;
        }
    }

    #region Private Methods

    /// <summary>
    /// Parses rating range from question options.
    /// Defaults to 1-5 if not specified.
    /// </summary>
    private (int minRating, int maxRating) ParseRatingRange(QuestionDto question)
    {
        // Try to parse range from OptionsJson if available
        if (!string.IsNullOrWhiteSpace(question.Options?.FirstOrDefault()))
        {
            try
            {
                var optionsJson = question.Options.First();
                var options = JsonSerializer.Deserialize<Dictionary<string, int>>(optionsJson);

                if (options != null &&
                    options.TryGetValue("min", out var min) &&
                    options.TryGetValue("max", out var max))
                {
                    return (min, max);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse rating range from options, using defaults");
            }
        }

        // Default to 1-5 scale
        return (DEFAULT_MIN_RATING, DEFAULT_MAX_RATING);
    }

    /// <summary>
    /// Builds inline keyboard with rating buttons.
    /// </summary>
    private InlineKeyboardMarkup BuildKeyboard(QuestionDto question, int minRating, int maxRating)
    {
        var buttons = new List<List<InlineKeyboardButton>>();
        var currentRow = new List<InlineKeyboardButton>();

        // Add rating buttons
        for (int rating = minRating; rating <= maxRating; rating++)
        {
            var callbackData = $"rating_q{question.Id}_r{rating}";
            var buttonText = rating.ToString();

            // Add emoji for visual appeal
            if (maxRating <= 5)
            {
                buttonText = $"{rating} {GetRatingEmoji(rating, maxRating)}";
            }

            currentRow.Add(InlineKeyboardButton.WithCallbackData(
                text: buttonText,
                callbackData: callbackData));

            // Start new row after MAX_BUTTONS_PER_ROW buttons
            if (currentRow.Count >= MAX_BUTTONS_PER_ROW)
            {
                buttons.Add(currentRow);
                currentRow = new List<InlineKeyboardButton>();
            }
        }

        // Add remaining buttons in current row
        if (currentRow.Count > 0)
        {
            buttons.Add(currentRow);
        }

        // Add navigation row (Back and Skip buttons)
        var navigationRow = new List<InlineKeyboardButton>();

        // Back button (always show)
        navigationRow.Add(InlineKeyboardButton.WithCallbackData(
            text: "⬅️ Back",
            callbackData: $"nav_back_q{question.Id}"));

        // Skip button for optional questions
        if (!question.IsRequired)
        {
            navigationRow.Add(InlineKeyboardButton.WithCallbackData(
                text: "⏭ Skip",
                callbackData: $"nav_skip_q{question.Id}"));
        }

        if (navigationRow.Count > 0)
        {
            buttons.Add(navigationRow);
        }

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Parses callback data to extract rating value.
    /// Format: "rating_q{questionId}_r{ratingValue}"
    /// </summary>
    private bool TryParseCallbackData(string callbackData, int questionId, out int rating)
    {
        rating = 0;

        if (string.IsNullOrWhiteSpace(callbackData))
            return false;

        var expectedPrefix = $"rating_q{questionId}_r";
        if (!callbackData.StartsWith(expectedPrefix))
            return false;

        var ratingStr = callbackData.Substring(expectedPrefix.Length);
        return int.TryParse(ratingStr, out rating);
    }

    /// <summary>
    /// Gets emoji representation for rating value.
    /// </summary>
    private string GetRatingEmoji(int rating, int maxRating)
    {
        // For 1-5 scale, use star emojis
        if (maxRating == 5)
        {
            return rating switch
            {
                1 => "⭐",
                2 => "⭐⭐",
                3 => "⭐⭐⭐",
                4 => "⭐⭐⭐⭐",
                5 => "⭐⭐⭐⭐⭐",
                _ => ""
            };
        }

        // For other scales, use simple indicators
        return rating >= maxRating * 0.8 ? "😊" :
               rating >= maxRating * 0.6 ? "🙂" :
               rating >= maxRating * 0.4 ? "😐" :
               rating >= maxRating * 0.2 ? "🙁" :
               "☹️";
    }

    /// <summary>
    /// Generates visual star rating display.
    /// </summary>
    private string GenerateStarRating(int rating, int maxRating)
    {
        if (maxRating <= 5)
        {
            var filled = new string('⭐', rating);
            var empty = new string('☆', maxRating - rating);
            return filled + empty;
        }

        return $"{rating}/{maxRating}";
    }

    #endregion
}
