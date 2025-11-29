using System.Text.Json;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Bot.Utilities;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using SurveyBot.Core.ValueObjects.Answers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SurveyBot.Bot.Handlers.Questions;

/// <summary>
/// Handles location-based questions that use Telegram's location sharing functionality.
/// Uses ReplyKeyboardMarkup with location request button (NOT InlineKeyboardMarkup).
/// Includes privacy-preserving logging and comprehensive validation.
/// </summary>
public class LocationQuestionHandler : IQuestionHandler
{
    private readonly IBotService _botService;
    private readonly IAnswerValidator _validator;
    private readonly QuestionErrorHandler _errorHandler;
    private readonly QuestionMediaHelper _mediaHelper;
    private readonly ILogger<LocationQuestionHandler> _logger;

    public QuestionType QuestionType => QuestionType.Location;

    public LocationQuestionHandler(
        IBotService botService,
        IAnswerValidator validator,
        QuestionErrorHandler errorHandler,
        QuestionMediaHelper mediaHelper,
        ILogger<LocationQuestionHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _mediaHelper = mediaHelper ?? throw new ArgumentNullException(nameof(mediaHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays the location question to the user with a location sharing keyboard.
    /// CRITICAL: Uses ReplyKeyboardMarkup (NOT InlineKeyboardMarkup) for location sharing.
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
                      $"Please share your location using the button below:{navigationHelp}";

        // Create location keyboard (ReplyKeyboardMarkup required for location sharing)
        var keyboard = CreateLocationKeyboard(question.IsRequired);

        _logger.LogDebug(
            "Displaying location question {QuestionId} in chat {ChatId}",
            question.Id,
            chatId);

        var sentMessage = await _botService.Client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);

        return sentMessage.MessageId;
    }

    /// <summary>
    /// Processes location answer from user's message with comprehensive validation.
    /// Handles both location sharing and skip commands.
    /// </summary>
    public async Task<string?> ProcessAnswerAsync(
        Message? message,
        CallbackQuery? callbackQuery,
        QuestionDto question,
        long userId,
        CancellationToken cancellationToken = default)
    {
        // Location questions only accept message input, not callback queries
        if (message == null)
        {
            _logger.LogDebug("Location question requires message input");
            return null;
        }

        var chatId = message.Chat.Id;

        // Check if user is trying to skip
        if (message.Text?.Equals("/skip", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (question.IsRequired)
            {
                await _errorHandler.ShowValidationErrorAsync(
                    chatId,
                    "This question is required and cannot be skipped. Please share your location using the button.",
                    cancellationToken);
                return null;
            }

            // Allow skip for optional questions - remove keyboard and return empty answer
            _logger.LogDebug("User {UserId} skipped optional location question {QuestionId}", userId, question.Id);

            await RemoveLocationKeyboardAsync(chatId, "Question skipped.", cancellationToken);

            // Return empty location answer
            return JsonSerializer.Serialize(new { latitude = (double?)null, longitude = (double?)null });
        }

        // Check if user is trying to go back
        if (message.Text?.Equals("/back", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Let NavigationHandler handle this
            return null;
        }

        // Check if user sent text instead of location
        if (message.Location == null && !string.IsNullOrWhiteSpace(message.Text))
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                "Please share your location using the button above, not by typing text.\n\n" +
                "Tap the \"📍 Share Location\" button to share your current location.",
                cancellationToken);
            return null;
        }

        // Validate location was received
        if (message.Location == null)
        {
            _logger.LogDebug("Location question requires location message, received message type: {MessageType}", message.Type);
            return null;
        }

        // Extract location data
        var latitude = message.Location.Latitude;
        var longitude = message.Location.Longitude;
        var accuracy = message.Location.HorizontalAccuracy;

        // Validate coordinates
        if (latitude < LocationAnswerValue.MinLatitude || latitude > LocationAnswerValue.MaxLatitude)
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                $"Invalid latitude: {latitude}. Must be between {LocationAnswerValue.MinLatitude} and {LocationAnswerValue.MaxLatitude}.",
                cancellationToken);
            return null;
        }

        if (longitude < LocationAnswerValue.MinLongitude || longitude > LocationAnswerValue.MaxLongitude)
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                $"Invalid longitude: {longitude}. Must be between {LocationAnswerValue.MinLongitude} and {LocationAnswerValue.MaxLongitude}.",
                cancellationToken);
            return null;
        }

        // Create LocationAnswerValue using factory method
        LocationAnswerValue locationValue;
        try
        {
            locationValue = LocationAnswerValue.Create(
                latitude: latitude,
                longitude: longitude,
                accuracy: accuracy,
                timestamp: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create LocationAnswerValue for question {QuestionId}", question.Id);
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                "Invalid location data. Please try sharing your location again.",
                cancellationToken);
            return null;
        }

        // Get JSON representation
        var answerJson = locationValue.ToJson();

        // Final validation using the validator
        var validationResult = _validator.ValidateAnswer(answerJson, question);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Location answer validation failed for question {QuestionId}: {ErrorMessage}",
                question.Id,
                validationResult.ErrorMessage);

            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                validationResult.ErrorMessage!,
                cancellationToken);
            return null;
        }

        // Log location received (privacy-preserving - use coordinate ranges)
        _logger.LogDebug(
            "Location answer processed for question {QuestionId} from user {UserId}: lat={LatRange}, lon={LonRange}, accuracy={Accuracy}m",
            question.Id,
            userId,
            GetCoordinateRange(latitude),
            GetCoordinateRange(longitude),
            accuracy?.ToString("F1") ?? "N/A");

        // Remove keyboard and show confirmation
        await RemoveLocationKeyboardAsync(
            chatId,
            $"📍 Location received! ({latitude:F6}, {longitude:F6})\nThank you.",
            cancellationToken);

        return answerJson;
    }

    /// <summary>
    /// Validates location answer format and content.
    /// </summary>
    public bool ValidateAnswer(string? answerJson, QuestionDto question)
    {
        if (string.IsNullOrWhiteSpace(answerJson))
            return !question.IsRequired; // Empty answer OK for optional questions

        try
        {
            var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

            // Check for latitude and longitude properties
            if (!answer.TryGetProperty("latitude", out var latElement) ||
                !answer.TryGetProperty("longitude", out var lonElement))
            {
                return false;
            }

            // For required questions, coordinates must be non-null
            if (question.IsRequired)
            {
                if (latElement.ValueKind == JsonValueKind.Null ||
                    lonElement.ValueKind == JsonValueKind.Null)
                {
                    return false;
                }
            }

            // If coordinates are provided, validate ranges
            if (latElement.ValueKind != JsonValueKind.Null &&
                lonElement.ValueKind != JsonValueKind.Null)
            {
                var lat = latElement.GetDouble();
                var lon = lonElement.GetDouble();

                if (lat < LocationAnswerValue.MinLatitude || lat > LocationAnswerValue.MaxLatitude)
                    return false;

                if (lon < LocationAnswerValue.MinLongitude || lon > LocationAnswerValue.MaxLongitude)
                    return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON format for location answer");
            return false;
        }
    }

    #region Private Methods

    /// <summary>
    /// Creates a location sharing keyboard.
    /// CRITICAL: Must use ReplyKeyboardMarkup (NOT InlineKeyboardMarkup) for location sharing.
    /// </summary>
    private ReplyKeyboardMarkup CreateLocationKeyboard(bool isRequired)
    {
        var buttons = new List<KeyboardButton[]>
        {
            // Location sharing button (only works with ReplyKeyboardMarkup)
            new[] { KeyboardButton.WithRequestLocation("📍 Share Location") }
        };

        // Add skip button for optional questions
        if (!isRequired)
        {
            buttons.Add(new[] { new KeyboardButton("/skip") });
        }

        return new ReplyKeyboardMarkup(buttons)
        {
            ResizeKeyboard = true,      // Make keyboard smaller
            OneTimeKeyboard = true      // Hide keyboard after use
        };
    }

    /// <summary>
    /// Removes the location keyboard and shows a confirmation message.
    /// </summary>
    private async Task RemoveLocationKeyboardAsync(
        long chatId,
        string confirmationMessage,
        CancellationToken cancellationToken)
    {
        await _botService.Client.SendMessage(
            chatId: chatId,
            text: confirmationMessage,
            replyMarkup: new ReplyKeyboardRemove(),  // Remove keyboard
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Converts coordinate to privacy-preserving range string.
    /// Example: 37.7749 -> "30 to 40"
    /// </summary>
    private static string GetCoordinateRange(double coordinate)
    {
        var rounded = Math.Floor(coordinate / 10) * 10;
        return $"{rounded} to {rounded + 10}";
    }

    #endregion
}
