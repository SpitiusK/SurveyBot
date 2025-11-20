using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using SurveyBot.Core.DTOs.Media;
using SurveyBot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace SurveyBot.Bot.Services;

/// <summary>
/// Service for handling media operations in Telegram bot context.
/// Implements sending of images, videos, audio, and documents via Telegram Bot API.
/// </summary>
public class TelegramMediaService : ITelegramMediaService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<TelegramMediaService> _logger;

    public TelegramMediaService(
        ITelegramBotClient botClient,
        ILogger<TelegramMediaService> logger)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Send image to Telegram user with retry logic.
    /// </summary>
    public async Task<bool> SendImageAsync(
        long chatId,
        string imageUrl,
        string? caption = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Sending image to chat {ChatId}: {ImageUrl}",
                chatId,
                imageUrl);

            await _botClient.SendPhoto(
                chatId: chatId,
                photo: InputFile.FromUri(imageUrl),
                caption: caption,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Image sent successfully to chat {ChatId}", chatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending image to chat {ChatId}. URL: {ImageUrl}",
                chatId,
                imageUrl);
            return false;
        }
    }

    /// <summary>
    /// Send video to Telegram user.
    /// </summary>
    public async Task<bool> SendVideoAsync(
        long chatId,
        string videoUrl,
        string? caption = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Sending video to chat {ChatId}: {VideoUrl}",
                chatId,
                videoUrl);

            await _botClient.SendVideo(
                chatId: chatId,
                video: InputFile.FromUri(videoUrl),
                caption: caption,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Video sent successfully to chat {ChatId}", chatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending video to chat {ChatId}. URL: {VideoUrl}",
                chatId,
                videoUrl);
            return false;
        }
    }

    /// <summary>
    /// Send audio to Telegram user.
    /// </summary>
    public async Task<bool> SendAudioAsync(
        long chatId,
        string audioUrl,
        string? caption = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Sending audio to chat {ChatId}: {AudioUrl}",
                chatId,
                audioUrl);

            await _botClient.SendAudio(
                chatId: chatId,
                audio: InputFile.FromUri(audioUrl),
                caption: caption,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Audio sent successfully to chat {ChatId}", chatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending audio to chat {ChatId}. URL: {AudioUrl}",
                chatId,
                audioUrl);
            return false;
        }
    }

    /// <summary>
    /// Send document to Telegram user.
    /// </summary>
    public async Task<bool> SendDocumentAsync(
        long chatId,
        string documentUrl,
        string? caption = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Sending document to chat {ChatId}: {DocumentUrl}",
                chatId,
                documentUrl);

            await _botClient.SendDocument(
                chatId: chatId,
                document: InputFile.FromUri(documentUrl),
                caption: caption,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Document sent successfully to chat {ChatId}", chatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending document to chat {ChatId}. URL: {DocumentUrl}",
                chatId,
                documentUrl);
            return false;
        }
    }

    /// <summary>
    /// Send multiple media items in order.
    /// Sends each media item sequentially based on their order property.
    /// </summary>
    public async Task<bool> SendMediaItemsAsync(
        long chatId,
        IEnumerable<MediaItemDto> mediaItems,
        CancellationToken cancellationToken = default)
    {
        var items = mediaItems.OrderBy(m => m.Order).ToList();

        if (!items.Any())
        {
            _logger.LogWarning("No media items to send to chat {ChatId}", chatId);
            return true; // No items is considered success
        }

        var successCount = 0;
        var totalItems = items.Count;

        _logger.LogInformation(
            "Sending {Count} media items to chat {ChatId}",
            totalItems,
            chatId);

        foreach (var (media, index) in items.Select((m, i) => (m, i)))
        {
            var caption = FormatMediaCaption("", index + 1, totalItems);

            var success = media.Type.ToLowerInvariant() switch
            {
                "image" => await SendImageAsync(chatId, media.FilePath, caption, cancellationToken),
                "video" => await SendVideoAsync(chatId, media.FilePath, caption, cancellationToken),
                "audio" => await SendAudioAsync(chatId, media.FilePath, caption, cancellationToken),
                "document" => await SendDocumentAsync(chatId, media.FilePath, caption, cancellationToken),
                _ => false,
            };

            if (success)
            {
                successCount++;
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send media item {Index}/{Total} (Type: {Type}) to chat {ChatId}",
                    index + 1,
                    totalItems,
                    media.Type,
                    chatId);
            }
        }

        var allSuccess = successCount == totalItems;

        if (allSuccess)
        {
            _logger.LogInformation(
                "All {Count} media items sent successfully to chat {ChatId}",
                totalItems,
                chatId);
        }
        else
        {
            _logger.LogWarning(
                "Only {SuccessCount}/{TotalCount} media items sent successfully to chat {ChatId}",
                successCount,
                totalItems,
                chatId);
        }

        return allSuccess;
    }

    /// <summary>
    /// Format caption for media message.
    /// Example: "Image 1 of 3" or with question text.
    /// </summary>
    public string FormatMediaCaption(string questionText, int mediaIndex, int totalMedia)
    {
        var caption = $"<b>Media {mediaIndex} of {totalMedia}</b>";

        if (!string.IsNullOrWhiteSpace(questionText))
        {
            caption = $"{questionText}\n\n{caption}";
        }

        return caption;
    }

    /// <summary>
    /// Convert local file path to public URL.
    /// Example: "/uploads/media/2025/11/file.jpg" → "https://api.example.com/uploads/media/2025/11/file.jpg"
    /// </summary>
    public string GetMediaUrl(string filePath, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));
        }

        // If already a full URL, return as-is
        if (filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return filePath;
        }

        // Normalize paths
        var cleanPath = filePath.TrimStart('/');
        var cleanBaseUrl = baseUrl.TrimEnd('/');

        return $"{cleanBaseUrl}/{cleanPath}";
    }
}
