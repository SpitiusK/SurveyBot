using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveyBot.Bot.Configuration;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Interfaces;

namespace SurveyBot.Bot.Handlers.Questions;

/// <summary>
/// Helper class for sending media attached to questions.
/// Shared by all question handlers to provide consistent media handling.
/// </summary>
public class QuestionMediaHelper
{
    private readonly ITelegramMediaService _mediaService;
    private readonly BotConfiguration _botConfiguration;
    private readonly ILogger<QuestionMediaHelper> _logger;

    private const int RETRY_ATTEMPTS = 3;
    private const int RETRY_DELAY_MS = 100;

    public QuestionMediaHelper(
        ITelegramMediaService mediaService,
        IOptions<BotConfiguration> botConfiguration,
        ILogger<QuestionMediaHelper> logger)
    {
        _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
        _botConfiguration = botConfiguration?.Value ?? throw new ArgumentNullException(nameof(botConfiguration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends all media items attached to a question.
    /// Returns true if all media sent successfully, false if some or all failed.
    /// </summary>
    /// <param name="chatId">Telegram chat ID</param>
    /// <param name="question">Question containing media content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all media sent successfully or no media attached, false if some failed</returns>
    public async Task<bool> SendQuestionMediaAsync(
        long chatId,
        QuestionDto question,
        CancellationToken cancellationToken = default)
    {
        // Check if question has media
        if (question.MediaContent == null ||
            question.MediaContent.Items == null ||
            question.MediaContent.Items.Count == 0)
        {
            _logger.LogDebug(
                "Question {QuestionId} has no media content, skipping media send",
                question.Id);
            return true; // No media is considered success
        }

        var items = question.MediaContent.Items.OrderBy(m => m.Order).ToList();
        var successCount = 0;
        var totalItems = items.Count;

        _logger.LogInformation(
            "Sending {Count} media items for question {QuestionId} to chat {ChatId}",
            totalItems,
            question.Id,
            chatId);

        foreach (var (media, index) in items.Select((m, i) => (m, i)))
        {
            var caption = FormatMediaCaption(index + 1, totalItems);
            var success = await SendMediaItemWithRetryAsync(
                chatId,
                media.Type,
                media.FilePath,
                caption,
                cancellationToken);

            if (success)
            {
                successCount++;
                _logger.LogDebug(
                    "Successfully sent media {Index}/{Total} (Type: {Type}, ID: {MediaId}) for question {QuestionId}",
                    index + 1,
                    totalItems,
                    media.Type,
                    media.Id,
                    question.Id);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send media {Index}/{Total} (Type: {Type}, ID: {MediaId}) for question {QuestionId}",
                    index + 1,
                    totalItems,
                    media.Type,
                    media.Id,
                    question.Id);
            }

            // Small delay between media items to avoid rate limiting
            if (index < items.Count - 1)
            {
                await Task.Delay(RETRY_DELAY_MS, cancellationToken);
            }
        }

        var allSuccess = successCount == totalItems;

        if (allSuccess)
        {
            _logger.LogInformation(
                "All {Count} media items sent successfully for question {QuestionId} to chat {ChatId}",
                totalItems,
                question.Id,
                chatId);
        }
        else
        {
            _logger.LogWarning(
                "Only {SuccessCount}/{TotalCount} media items sent successfully for question {QuestionId} to chat {ChatId}",
                successCount,
                totalItems,
                question.Id,
                chatId);
        }

        return allSuccess;
    }

    /// <summary>
    /// Sends a single media item with retry logic.
    /// </summary>
    private async Task<bool> SendMediaItemWithRetryAsync(
        long chatId,
        string mediaType,
        string filePath,
        string caption,
        CancellationToken cancellationToken)
    {
        // Convert file path to full URL
        var mediaUrl = GetMediaUrl(filePath);

        for (int attempt = 1; attempt <= RETRY_ATTEMPTS; attempt++)
        {
            try
            {
                var success = mediaType.ToLowerInvariant() switch
                {
                    "image" => await _mediaService.SendImageAsync(
                        chatId, mediaUrl, caption, cancellationToken),
                    "video" => await _mediaService.SendVideoAsync(
                        chatId, mediaUrl, caption, cancellationToken),
                    "audio" => await _mediaService.SendAudioAsync(
                        chatId, mediaUrl, caption, cancellationToken),
                    "document" => await _mediaService.SendDocumentAsync(
                        chatId, mediaUrl, caption, cancellationToken),
                    _ => false,
                };

                if (success)
                {
                    return true;
                }

                // If not successful and not last attempt, wait before retry
                if (attempt < RETRY_ATTEMPTS)
                {
                    var delay = RETRY_DELAY_MS * attempt; // Exponential backoff
                    _logger.LogDebug(
                        "Media send attempt {Attempt} failed, retrying in {Delay}ms",
                        attempt,
                        delay);
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Attempt {Attempt}/{MaxAttempts} failed to send {MediaType} to chat {ChatId}. URL: {MediaUrl}",
                    attempt,
                    RETRY_ATTEMPTS,
                    mediaType,
                    chatId,
                    mediaUrl);

                if (attempt < RETRY_ATTEMPTS)
                {
                    var delay = RETRY_DELAY_MS * attempt;
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Formats a media caption showing progress.
    /// Example: "Media 1 of 3"
    /// </summary>
    private static string FormatMediaCaption(int mediaIndex, int totalMedia)
    {
        return $"<i>Media {mediaIndex} of {totalMedia}</i>";
    }

    /// <summary>
    /// Converts a file path to a full URL using the API base URL.
    /// If the path is already a full URL, returns it as-is.
    /// </summary>
    private string GetMediaUrl(string filePath)
    {
        // If already a full URL, return as-is
        if (filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return filePath;
        }

        // Use the helper method from media service
        return _mediaService.GetMediaUrl(filePath, _botConfiguration.ApiBaseUrl);
    }
}
