using SurveyBot.Core.DTOs.Media;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Service for handling media operations in Telegram bot context.
/// Sends images, videos, audio, and documents via Telegram Bot API.
/// </summary>
public interface ITelegramMediaService
{
    /// <summary>
    /// Send image to Telegram user.
    /// </summary>
    /// <param name="chatId">Telegram chat ID</param>
    /// <param name="imageUrl">URL or file path to the image</param>
    /// <param name="caption">Optional caption text (supports HTML)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendImageAsync(
        long chatId,
        string imageUrl,
        string? caption = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send video to Telegram user.
    /// </summary>
    /// <param name="chatId">Telegram chat ID</param>
    /// <param name="videoUrl">URL or file path to the video</param>
    /// <param name="caption">Optional caption text (supports HTML)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendVideoAsync(
        long chatId,
        string videoUrl,
        string? caption = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send audio to Telegram user.
    /// </summary>
    /// <param name="chatId">Telegram chat ID</param>
    /// <param name="audioUrl">URL or file path to the audio</param>
    /// <param name="caption">Optional caption text (supports HTML)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendAudioAsync(
        long chatId,
        string audioUrl,
        string? caption = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send document to Telegram user.
    /// </summary>
    /// <param name="chatId">Telegram chat ID</param>
    /// <param name="documentUrl">URL or file path to the document</param>
    /// <param name="caption">Optional caption text (supports HTML)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendDocumentAsync(
        long chatId,
        string documentUrl,
        string? caption = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send multiple media items in order.
    /// </summary>
    /// <param name="chatId">Telegram chat ID</param>
    /// <param name="mediaItems">Collection of media items to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all items sent successfully, false otherwise</returns>
    Task<bool> SendMediaItemsAsync(
        long chatId,
        IEnumerable<MediaItemDto> mediaItems,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Format media caption from question text and media metadata.
    /// </summary>
    /// <param name="questionText">Question text to include (optional)</param>
    /// <param name="mediaIndex">Current media index (1-based)</param>
    /// <param name="totalMedia">Total number of media items</param>
    /// <returns>Formatted caption with HTML markup</returns>
    string FormatMediaCaption(string questionText, int mediaIndex, int totalMedia);

    /// <summary>
    /// Get media file URL from local path.
    /// Converts local file path to publicly accessible URL.
    /// </summary>
    /// <param name="filePath">Local file path or existing URL</param>
    /// <param name="baseUrl">Base URL for the API server</param>
    /// <returns>Full URL to the media file</returns>
    string GetMediaUrl(string filePath, string baseUrl);
}
