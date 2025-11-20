namespace SurveyBot.API.Services;

/// <summary>
/// Maps detected media types to appropriate Telegram Bot API methods.
/// Ensures correct handling of different file types when sending to Telegram.
/// </summary>
public class TelegramMediaService
{
    /// <summary>
    /// Telegram media type determines which API method to use for sending files.
    /// </summary>
    public enum TelegramMediaType
    {
        /// <summary>Photo - uses sendPhoto() API</summary>
        Photo,
        /// <summary>Video - uses sendVideo() API</summary>
        Video,
        /// <summary>Audio - uses sendAudio() API</summary>
        Audio,
        /// <summary>General document - uses sendDocument() API</summary>
        Document,
        /// <summary>Animation/GIF - uses sendAnimation() API</summary>
        Animation,
    }

    /// <summary>
    /// Maps application media type to Telegram media type.
    /// </summary>
    public static TelegramMediaType MapToTelegramMediaType(string mediaType)
    {
        return mediaType.ToLowerInvariant() switch
        {
            "image" => TelegramMediaType.Photo,
            "video" => TelegramMediaType.Video,
            "audio" => TelegramMediaType.Audio,
            "document" => TelegramMediaType.Document,
            "archive" => TelegramMediaType.Document,
            _ => TelegramMediaType.Document
        };
    }

    /// <summary>
    /// Maps file extension to Telegram media type for optimal handling.
    /// Some formats can be sent as specific types (e.g., GIF as animation).
    /// </summary>
    public static TelegramMediaType MapFileExtensionToTelegramType(string extension)
    {
        var ext = extension.ToLowerInvariant().TrimStart('.');

        return ext switch
        {
            // Photos
            "jpg" or "jpeg" or "png" or "webp" or "bmp" => TelegramMediaType.Photo,

            // Videos
            "mp4" or "webm" or "mov" or "avi" or "mkv" or "flv" or "wmv" or "3gp" or "mpeg" or "mpg" => TelegramMediaType.Video,

            // Animations (GIF-like)
            "gif" => TelegramMediaType.Animation,

            // Audio
            "mp3" or "wav" or "ogg" or "m4a" or "flac" or "aac" or "wma" or "aif" or "aiff" => TelegramMediaType.Audio,

            // Everything else as document
            _ => TelegramMediaType.Document
        };
    }

    /// <summary>
    /// Gets the maximum file size for a Telegram media type (in bytes).
    /// Telegram has limits: 50MB for most files, but some are higher.
    /// </summary>
    public static long GetMaxFileSizeForTelegramType(TelegramMediaType telegramType)
    {
        return telegramType switch
        {
            TelegramMediaType.Photo => 10 * 1024 * 1024,      // 10 MB
            TelegramMediaType.Video => 50 * 1024 * 1024,      // 50 MB
            TelegramMediaType.Audio => 50 * 1024 * 1024,      // 50 MB
            TelegramMediaType.Document => 50 * 1024 * 1024,   // 50 MB
            TelegramMediaType.Animation => 50 * 1024 * 1024,  // 50 MB
            _ => 50 * 1024 * 1024
        };
    }

    /// <summary>
    /// Determines if a file should be sent as a document or specific media type.
    /// Some formats (like large videos) are better sent as documents.
    /// </summary>
    public static TelegramMediaType OptimizeForTelegramSending(
        string mediaType,
        string fileExtension,
        long fileSizeBytes)
    {
        var baseType = MapToTelegramMediaType(mediaType);
        var extensionType = MapFileExtensionToTelegramType(fileExtension);

        // For videos larger than a threshold, consider sending as document
        if (extensionType == TelegramMediaType.Video && fileSizeBytes > 40 * 1024 * 1024)
        {
            return TelegramMediaType.Document;
        }

        // Use more specific type if available
        return extensionType != TelegramMediaType.Document ? extensionType : baseType;
    }

    /// <summary>
    /// Gets a human-readable name for the Telegram media type.
    /// </summary>
    public static string GetTelegramMediaTypeName(TelegramMediaType telegramType)
    {
        return telegramType switch
        {
            TelegramMediaType.Photo => "Photo",
            TelegramMediaType.Video => "Video",
            TelegramMediaType.Audio => "Audio",
            TelegramMediaType.Document => "Document",
            TelegramMediaType.Animation => "Animation",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Checks if a file type is natively supported as a specific Telegram media type
    /// (not just as a generic document).
    /// </summary>
    public static bool IsNativelySupported(string mediaType)
    {
        var type = MapToTelegramMediaType(mediaType);
        return type != TelegramMediaType.Document;
    }

    /// <summary>
    /// Gets recommended caption for different media types when sending to Telegram.
    /// </summary>
    public static string GetRecommendedCaption(string mediaType, string displayName)
    {
        var prefix = mediaType.ToLowerInvariant() switch
        {
            "image" => "📷",
            "video" => "🎥",
            "audio" => "🎵",
            "document" => "📄",
            "archive" => "📦",
            _ => "📎"
        };

        return $"{prefix} {displayName}";
    }

    /// <summary>
    /// Validates if a file can be sent via Telegram with optimal settings.
    /// Returns error message if validation fails, null if OK.
    /// </summary>
    public static string? ValidateForTelegramSending(
        string mediaType,
        string fileExtension,
        long fileSizeBytes)
    {
        var telegramType = OptimizeForTelegramSending(mediaType, fileExtension, fileSizeBytes);
        var maxSize = GetMaxFileSizeForTelegramType(telegramType);

        if (fileSizeBytes > maxSize)
        {
            return $"File size exceeds Telegram limit of {maxSize / 1024 / 1024}MB for {GetTelegramMediaTypeName(telegramType)} files";
        }

        return null;
    }
}
