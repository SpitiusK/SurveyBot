using Microsoft.Extensions.Logging;
using SurveyBot.Core.DTOs.Media;
using SurveyBot.Core.Interfaces;
using System.Text.RegularExpressions;

namespace SurveyBot.Infrastructure.Services;

/// <summary>
/// Service for validating uploaded media files.
/// Validates file type, size, MIME type, dimensions, and security concerns.
/// </summary>
public class MediaValidationService : IMediaValidationService
{
    private readonly ILogger<MediaValidationService> _logger;

    // Unified file extension whitelist by media type (supports all Telegram-compatible formats)
    private static readonly Dictionary<string, HashSet<string>> AllowedExtensions = new()
    {
        // Images
        { "image", new HashSet<string> { "jpg", "jpeg", "png", "gif", "webp", "bmp", "tiff", "tif", "ico", "svg" } },
        // Videos
        { "video", new HashSet<string> { "mp4", "webm", "mov", "avi", "mkv", "flv", "wmv", "3gp", "asf", "mpeg", "mpg", "m3u8" } },
        // Audio
        { "audio", new HashSet<string> { "mp3", "wav", "ogg", "oga", "m4a", "flac", "aac", "wma", "caf", "weba", "aiff", "aif" } },
        // Documents
        { "document", new HashSet<string> { "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt", "rtf", "odt", "ods", "odp", "csv", "json", "xml", "md", "epub" } },
        // Archives
        { "archive", new HashSet<string> { "zip", "rar", "7z", "tar", "gz", "gzip", "bz2", "xz", "lz", "lzma", "snappy" } }
    };

    // Maximum file sizes in megabytes (by media type for backward compatibility, archives get 100MB)
    private static readonly Dictionary<string, int> MaxFileSizeMb = new()
    {
        { "image", 10 },
        { "video", 50 },
        { "audio", 20 },
        { "document", 25 },
        { "archive", 100 }
    };

    // Known dangerous file extensions (blacklist for security)
    private static readonly HashSet<string> DangerousExtensions = new()
    {
        "exe", "bat", "cmd", "com", "pif", "scr", "vbs", "js", "jar",
        "app", "deb", "rpm", "dmg", "pkg", "sh", "bash", "ps1", "msi",
        "dll", "so", "dylib", "class", "apk", "ipa"
    };

    // Comprehensive MIME type mappings to extensions
    private static readonly Dictionary<string, HashSet<string>> MimeTypeToExtensions = new()
    {
        // Images
        { "image/jpeg", new HashSet<string> { "jpg", "jpeg" } },
        { "image/png", new HashSet<string> { "png" } },
        { "image/gif", new HashSet<string> { "gif" } },
        { "image/webp", new HashSet<string> { "webp" } },
        { "image/bmp", new HashSet<string> { "bmp" } },
        { "image/tiff", new HashSet<string> { "tiff", "tif" } },
        { "image/x-icon", new HashSet<string> { "ico" } },
        { "image/svg+xml", new HashSet<string> { "svg" } },
        // Videos
        { "video/mp4", new HashSet<string> { "mp4" } },
        { "video/webm", new HashSet<string> { "webm" } },
        { "video/quicktime", new HashSet<string> { "mov" } },
        { "video/x-msvideo", new HashSet<string> { "avi" } },
        { "video/x-matroska", new HashSet<string> { "mkv" } },
        { "video/x-flv", new HashSet<string> { "flv" } },
        { "video/x-ms-wmv", new HashSet<string> { "wmv" } },
        { "video/3gpp", new HashSet<string> { "3gp" } },
        { "video/x-ms-asf", new HashSet<string> { "asf" } },
        { "video/mpeg", new HashSet<string> { "mpeg", "mpg" } },
        { "application/x-mpegURL", new HashSet<string> { "m3u8" } },
        // Audio
        { "audio/mpeg", new HashSet<string> { "mp3" } },
        { "audio/wav", new HashSet<string> { "wav" } },
        { "audio/ogg", new HashSet<string> { "ogg", "oga" } },
        { "audio/mp4", new HashSet<string> { "m4a" } },
        { "audio/flac", new HashSet<string> { "flac" } },
        { "audio/aac", new HashSet<string> { "aac" } },
        { "audio/x-ms-wma", new HashSet<string> { "wma" } },
        { "audio/x-caf", new HashSet<string> { "caf" } },
        { "audio/webp", new HashSet<string> { "weba" } },
        { "audio/x-wav", new HashSet<string> { "wav" } },
        { "audio/aiff", new HashSet<string> { "aiff", "aif" } },
        // Documents
        { "application/pdf", new HashSet<string> { "pdf" } },
        { "application/msword", new HashSet<string> { "doc" } },
        { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", new HashSet<string> { "docx" } },
        { "application/vnd.ms-excel", new HashSet<string> { "xls" } },
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", new HashSet<string> { "xlsx" } },
        { "application/vnd.ms-powerpoint", new HashSet<string> { "ppt" } },
        { "application/vnd.openxmlformats-officedocument.presentationml.presentation", new HashSet<string> { "pptx" } },
        { "text/plain", new HashSet<string> { "txt" } },
        { "text/rtf", new HashSet<string> { "rtf" } },
        { "application/rtf", new HashSet<string> { "rtf" } },
        { "application/vnd.oasis.opendocument.text", new HashSet<string> { "odt" } },
        { "application/vnd.oasis.opendocument.spreadsheet", new HashSet<string> { "ods" } },
        { "application/vnd.oasis.opendocument.presentation", new HashSet<string> { "odp" } },
        { "text/csv", new HashSet<string> { "csv" } },
        { "application/json", new HashSet<string> { "json" } },
        { "application/xml", new HashSet<string> { "xml" } },
        { "text/xml", new HashSet<string> { "xml" } },
        { "text/markdown", new HashSet<string> { "md" } },
        { "text/x-markdown", new HashSet<string> { "md" } },
        { "application/epub+zip", new HashSet<string> { "epub" } },
        // Archives
        { "application/zip", new HashSet<string> { "zip" } },
        { "application/x-rar-compressed", new HashSet<string> { "rar" } },
        { "application/x-7z-compressed", new HashSet<string> { "7z" } },
        { "application/x-tar", new HashSet<string> { "tar" } },
        { "application/gzip", new HashSet<string> { "gz", "gzip" } },
        { "application/x-bzip2", new HashSet<string> { "bz2" } },
        { "application/x-xz", new HashSet<string> { "xz" } },
        { "application/x-lzip", new HashSet<string> { "lz" } },
        { "application/x-lzma", new HashSet<string> { "lzma" } },
        { "application/x-snappy-framed", new HashSet<string> { "snappy" } }
    };

    // Comprehensive file signature (magic bytes) detection for all supported types
    private static readonly Dictionary<string, byte[][]> FileSignatures = new()
    {
        // Images
        { "jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { "png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { "gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
        { "bmp", new[] { new byte[] { 0x42, 0x4D } } },
        { "ico", new[] { new byte[] { 0x00, 0x00, 0x01, 0x00 } } },
        { "webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } },
        // Videos
        { "mp4", new[] {
            new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 },
            new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 },
            new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 }
        } },
        { "webm", new[] { new byte[] { 0x1A, 0x45, 0xDF, 0xA3 } } },
        { "avi", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } },
        { "mkv", new[] { new byte[] { 0x1A, 0x45, 0xDF, 0xA3 } } },
        { "flv", new[] { new byte[] { 0x46, 0x4C, 0x56 } } },
        { "mov", new[] { new byte[] { 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70 } } },
        // Audio
        { "mp3", new[] {
            new byte[] { 0xFF, 0xFB },
            new byte[] { 0xFF, 0xF3 },
            new byte[] { 0xFF, 0xF2 },
            new byte[] { 0x49, 0x44, 0x33 } // ID3 tag
        } },
        { "wav", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } },
        { "ogg", new[] { new byte[] { 0x4F, 0x67, 0x67, 0x53 } } },
        { "flac", new[] { new byte[] { 0x66, 0x4C, 0x61, 0x43 } } },
        { "m4a", new[] { new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 } } },
        // Documents
        { "pdf", new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
        // Archives
        { "zip", new[] {
            new byte[] { 0x50, 0x4B, 0x03, 0x04 },
            new byte[] { 0x50, 0x4B, 0x05, 0x06 },
            new byte[] { 0x50, 0x4B, 0x07, 0x08 }
        } },
        { "rar", new[] { new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 } } },
        { "7z", new[] { new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C } } },
        { "tar", new[] { new byte[] { 0x75, 0x73, 0x74, 0x61, 0x72 } } },
        { "gz", new[] { new byte[] { 0x1F, 0x8B } } },
        { "bz2", new[] { new byte[] { 0x42, 0x5A } } }
    };

    // Mapping extension to media type for auto-detection
    private static readonly Dictionary<string, string> ExtensionToMediaType = new()
    {
        // Images
        { "jpg", "image" }, { "jpeg", "image" }, { "png", "image" }, { "gif", "image" }, { "webp", "image" },
        { "bmp", "image" }, { "tiff", "image" }, { "tif", "image" }, { "ico", "image" }, { "svg", "image" },
        // Videos
        { "mp4", "video" }, { "webm", "video" }, { "mov", "video" }, { "avi", "video" }, { "mkv", "video" },
        { "flv", "video" }, { "wmv", "video" }, { "3gp", "video" }, { "asf", "video" }, { "mpeg", "video" }, { "mpg", "video" }, { "m3u8", "video" },
        // Audio
        { "mp3", "audio" }, { "wav", "audio" }, { "ogg", "audio" }, { "oga", "audio" }, { "m4a", "audio" },
        { "flac", "audio" }, { "aac", "audio" }, { "wma", "audio" }, { "caf", "audio" }, { "weba", "audio" }, { "aiff", "audio" }, { "aif", "audio" },
        // Documents
        { "pdf", "document" }, { "doc", "document" }, { "docx", "document" }, { "xls", "document" }, { "xlsx", "document" },
        { "ppt", "document" }, { "pptx", "document" }, { "txt", "document" }, { "rtf", "document" }, { "odt", "document" }, { "ods", "document" },
        { "odp", "document" }, { "csv", "document" }, { "json", "document" }, { "xml", "document" }, { "md", "document" }, { "epub", "document" },
        // Archives
        { "zip", "archive" }, { "rar", "archive" }, { "7z", "archive" }, { "tar", "archive" }, { "gz", "archive" }, { "gzip", "archive" },
        { "bz2", "archive" }, { "xz", "archive" }, { "lz", "archive" }, { "lzma", "archive" }, { "snappy", "archive" }
    };

    public MediaValidationService(ILogger<MediaValidationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a media file before upload with auto-detection of media type.
    /// Detects the file type from magic bytes, MIME type, or extension.
    /// This is the new unified method that doesn't require explicit mediaType parameter.
    /// </summary>
    public async Task<(MediaValidationResult validationResult, string detectedMediaType)> ValidateMediaWithAutoDetectionAsync(
        Stream fileStream,
        string filename,
        CancellationToken cancellationToken = default)
    {
        // Detect media type from file
        var detectedMediaType = await DetectMediaTypeAsync(fileStream, filename, cancellationToken);

        if (string.IsNullOrEmpty(detectedMediaType))
        {
            var errors = new Dictionary<string, string>
            {
                { "mediaType", "Could not detect file type. File format is not supported." }
            };
            return (MediaValidationResult.Failure(errors), "");
        }

        // Validate using detected media type
        fileStream.Position = 0;
        var validationResult = await ValidateMediaAsync(fileStream, filename, detectedMediaType, cancellationToken);

        return (validationResult, detectedMediaType);
    }

    /// <summary>
    /// Validates a media file before upload (legacy method that requires explicit mediaType).
    /// Consider using ValidateMediaWithAutoDetectionAsync for new code.
    /// </summary>
    public async Task<MediaValidationResult> ValidateMediaAsync(
        Stream fileStream,
        string filename,
        string mediaType,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string>();

        try
        {
            // 1. Validate filename
            if (string.IsNullOrWhiteSpace(filename))
            {
                errors["filename"] = "Filename is required";
                return MediaValidationResult.Failure(errors);
            }

            if (!IsValidFilename(filename))
            {
                errors["filename"] = "Filename contains invalid characters";
            }

            // 2. Check for path traversal
            if (ContainsPathTraversal(filename))
            {
                errors["path"] = "Filename contains path traversal characters";
                return MediaValidationResult.Failure(errors);
            }

            // 3. Validate extension
            var extension = Path.GetExtension(filename).TrimStart('.').ToLowerInvariant();

            if (string.IsNullOrEmpty(extension))
            {
                errors["extension"] = "File must have an extension";
                return MediaValidationResult.Failure(errors);
            }

            // Check against dangerous extensions
            if (DangerousExtensions.Contains(extension))
            {
                errors["extension"] = $"File extension .{extension} is not allowed for security reasons";
                _logger.LogWarning("Rejected dangerous file extension: {Extension} for file {Filename}",
                    extension, filename);
                return MediaValidationResult.Failure(errors);
            }

            // Check against allowed extensions for media type
            if (!IsAllowedExtension(extension, mediaType))
            {
                var allowed = AllowedExtensions.ContainsKey(mediaType)
                    ? string.Join(", ", AllowedExtensions[mediaType].Select(e => $".{e}"))
                    : "none";
                errors["extension"] = $"File extension .{extension} is not allowed for {mediaType}. Allowed: {allowed}";
            }

            // 4. Validate media type
            if (!AllowedExtensions.ContainsKey(mediaType))
            {
                errors["mediaType"] = $"Invalid media type: {mediaType}. Must be one of: image, video, audio, document";
            }

            // 5. Validate file size
            if (fileStream.CanSeek)
            {
                var fileSize = fileStream.Length;

                if (fileSize == 0)
                {
                    errors["size"] = "File is empty";
                }
                else if (!IsValidFileSize(fileSize, mediaType))
                {
                    var maxSize = GetMaxFileSize(mediaType);
                    var fileSizeMb = fileSize / 1024.0 / 1024.0;
                    errors["size"] = $"File size ({fileSizeMb:F2} MB) exceeds limit of {maxSize} MB for {mediaType}";
                }

                // Reset stream position for MIME type check
                fileStream.Position = 0;
            }
            else
            {
                _logger.LogWarning("Cannot validate file size: stream is not seekable");
            }

            // 6. Validate MIME type (magic bytes)
            if (fileStream.CanRead && fileStream.CanSeek)
            {
                var mimeType = await GetFileMimeTypeAsync(fileStream, extension, cancellationToken);

                if (!string.IsNullOrEmpty(mimeType))
                {
                    if (!IsValidMimeType(mimeType, extension, mediaType))
                    {
                        errors["mimeType"] = $"File content (MIME type: {mimeType}) does not match extension .{extension}";
                        _logger.LogWarning("MIME type mismatch: {MimeType} vs extension {Extension} for file {Filename}",
                            mimeType, extension, filename);
                    }
                }

                // Reset stream position
                fileStream.Position = 0;
            }

            // Return result
            if (errors.Count > 0)
            {
                _logger.LogInformation("Media validation failed for {Filename}: {ErrorCount} errors",
                    filename, errors.Count);
                return MediaValidationResult.Failure(errors);
            }

            _logger.LogDebug("Media validation passed for {Filename}", filename);
            return MediaValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during media validation for {Filename}", filename);
            return MediaValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if the filename is valid (no path separators, null bytes, or control characters).
    /// </summary>
    private bool IsValidFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return false;

        // Check for null bytes
        if (filename.Contains('\0'))
            return false;

        // Check for path separators
        if (filename.Contains('/') || filename.Contains('\\'))
            return false;

        // Check for control characters
        if (filename.Any(c => char.IsControl(c)))
            return false;

        // Check for invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (filename.Any(c => invalidChars.Contains(c)))
            return false;

        // Check filename length
        if (filename.Length > 255)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if the filename contains path traversal patterns.
    /// </summary>
    private bool ContainsPathTraversal(string filename)
    {
        // Check for common path traversal patterns
        var dangerousPatterns = new[]
        {
            "..",
            "~/",
            "%2e%2e",
            "%252e%252e",
            "..%2f",
            "..%5c"
        };

        var lowerFilename = filename.ToLowerInvariant();
        return dangerousPatterns.Any(pattern => lowerFilename.Contains(pattern));
    }

    /// <summary>
    /// Checks if the extension is allowed for the given media type.
    /// </summary>
    private bool IsAllowedExtension(string extension, string mediaType)
    {
        if (!AllowedExtensions.TryGetValue(mediaType, out var allowedExts))
            return false;

        return allowedExts.Contains(extension);
    }

    /// <summary>
    /// Checks if the file size is within the allowed limit for the media type.
    /// </summary>
    private bool IsValidFileSize(long fileSize, string mediaType)
    {
        var maxSizeBytes = GetMaxFileSize(mediaType) * 1024L * 1024L; // Convert MB to bytes
        return fileSize > 0 && fileSize <= maxSizeBytes;
    }

    /// <summary>
    /// Gets the maximum file size in megabytes for the given media type.
    /// </summary>
    private int GetMaxFileSize(string mediaType)
    {
        return MaxFileSizeMb.TryGetValue(mediaType, out var maxSize) ? maxSize : 10; // Default 10 MB
    }

    /// <summary>
    /// Detects the MIME type of the file by reading its magic bytes.
    /// </summary>
    private async Task<string> GetFileMimeTypeAsync(
        Stream stream,
        string extension,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!stream.CanRead || !stream.CanSeek)
                return string.Empty;

            // Read first 8 bytes (enough for most signatures)
            var buffer = new byte[8];
            stream.Position = 0;
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

            if (bytesRead == 0)
                return string.Empty;

            // Check against known file signatures
            foreach (var (ext, signatures) in FileSignatures)
            {
                foreach (var signature in signatures)
                {
                    if (bytesRead >= signature.Length &&
                        buffer.Take(signature.Length).SequenceEqual(signature))
                    {
                        // Find MIME type for this extension
                        var mimeType = MimeTypeToExtensions
                            .FirstOrDefault(kvp => kvp.Value.Contains(ext))
                            .Key;

                        return mimeType ?? string.Empty;
                    }
                }
            }

            // Fallback: map extension to MIME type
            var mimeTypeFromExt = MimeTypeToExtensions
                .FirstOrDefault(kvp => kvp.Value.Contains(extension))
                .Key;

            return mimeTypeFromExt ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting MIME type for extension {Extension}", extension);
            return string.Empty;
        }
        finally
        {
            stream.Position = 0;
        }
    }

    /// <summary>
    /// Validates that the detected MIME type matches the file extension and media type.
    /// </summary>
    private bool IsValidMimeType(string mimeType, string extension, string mediaType)
    {
        // Empty MIME type means detection failed - allow it
        if (string.IsNullOrEmpty(mimeType))
            return true;

        // Check if MIME type matches the extension
        if (MimeTypeToExtensions.TryGetValue(mimeType, out var validExtensions))
        {
            if (!validExtensions.Contains(extension))
                return false;
        }

        // Check if MIME type category matches media type
        var mimeTypeCategory = mimeType.Split('/')[0]; // e.g., "image" from "image/jpeg"

        // Special case: documents can have "application" MIME type
        if (mediaType == "document")
        {
            return mimeTypeCategory == "application" || mimeTypeCategory == "text";
        }

        // For other media types, MIME category should match
        return mimeTypeCategory == mediaType;
    }

    /// <summary>
    /// Detects the media type of a file from magic bytes (primary), MIME type, or extension (fallback).
    /// This enables unified file upload without requiring explicit mediaType parameter.
    /// </summary>
    private async Task<string> DetectMediaTypeAsync(
        Stream fileStream,
        string filename,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!fileStream.CanRead || !fileStream.CanSeek)
            {
                _logger.LogWarning("Cannot detect media type: stream is not readable/seekable");
                return DetectMediaTypeFromExtension(filename);
            }

            // Method 1: Try to detect from magic bytes
            var detectedType = await DetectMediaTypeFromMagicBytesAsync(fileStream, cancellationToken);
            if (!string.IsNullOrEmpty(detectedType))
            {
                fileStream.Position = 0;
                return detectedType;
            }

            // Method 2: Try to detect from MIME type (if available)
            if (fileStream.CanSeek)
                fileStream.Position = 0;
            var mimeType = await GetFileMimeTypeAsync(fileStream, GetFileExtension(filename), cancellationToken);
            var typeFromMime = DetectMediaTypeFromMimeType(mimeType);
            if (!string.IsNullOrEmpty(typeFromMime))
            {
                fileStream.Position = 0;
                return typeFromMime;
            }

            // Method 3: Fallback to extension detection
            fileStream.Position = 0;
            return DetectMediaTypeFromExtension(filename);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting media type for file {Filename}", filename);
            return DetectMediaTypeFromExtension(filename);
        }
    }

    /// <summary>
    /// Detects media type by reading file magic bytes (binary signature).
    /// Most reliable method as it checks actual file content.
    /// </summary>
    private async Task<string> DetectMediaTypeFromMagicBytesAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!stream.CanRead || !stream.CanSeek)
                return string.Empty;

            var buffer = new byte[32]; // Read first 32 bytes for detection
            stream.Position = 0;
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

            if (bytesRead == 0)
                return string.Empty;

            // Check each file signature against actual file bytes
            foreach (var (ext, signatures) in FileSignatures)
            {
                foreach (var signature in signatures)
                {
                    if (bytesRead >= signature.Length &&
                        buffer.Take(signature.Length).SequenceEqual(signature))
                    {
                        // Found a match, convert extension to media type
                        if (ExtensionToMediaType.TryGetValue(ext, out var mediaType))
                        {
                            _logger.LogDebug("Detected media type '{MediaType}' from magic bytes for extension '{Ext}'", mediaType, ext);
                            return mediaType;
                        }
                    }
                }
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting media type from magic bytes");
            return string.Empty;
        }
    }

    /// <summary>
    /// Detects media type from MIME type.
    /// Fallback method when magic bytes detection fails.
    /// </summary>
    private string DetectMediaTypeFromMimeType(string mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
            return string.Empty;

        // Check each media type's MIME types
        foreach (var category in AllowedExtensions.Keys)
        {
            if (category == "archive")
                continue;  // Archives handled separately

            var mimeTypesToCheck = MimeTypeToExtensions
                .Where(kvp => kvp.Value.Count > 0)
                .FirstOrDefault(kvp => kvp.Key == mimeType);

            if (!string.IsNullOrEmpty(mimeTypesToCheck.Key))
            {
                // Determine category from MIME type extensions
                foreach (var (mime, exts) in MimeTypeToExtensions)
                {
                    if (mime == mimeType)
                    {
                        var ext = exts.FirstOrDefault();
                        if (!string.IsNullOrEmpty(ext) &&
                            ExtensionToMediaType.TryGetValue(ext, out var mediaType))
                        {
                            _logger.LogDebug("Detected media type '{MediaType}' from MIME type '{MimeType}'", mediaType, mimeType);
                            return mediaType;
                        }
                    }
                }
            }
        }

        // Fallback: Use MIME type prefix
        var prefix = mimeType.Split('/')[0].ToLowerInvariant();
        if (prefix == "image") return "image";
        if (prefix == "video") return "video";
        if (prefix == "audio") return "audio";
        if (prefix == "application")
        {
            if (mimeType.Contains("zip") || mimeType.Contains("rar") ||
                mimeType.Contains("7z") || mimeType.Contains("tar") ||
                mimeType.Contains("gzip"))
                return "archive";
            return "document";
        }
        if (prefix == "text") return "document";

        return string.Empty;
    }

    /// <summary>
    /// Detects media type from file extension (least reliable, fallback only).
    /// </summary>
    private string DetectMediaTypeFromExtension(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return string.Empty;

        var extension = GetFileExtension(filename);
        if (ExtensionToMediaType.TryGetValue(extension, out var mediaType))
        {
            _logger.LogDebug("Detected media type '{MediaType}' from file extension '{Extension}'", mediaType, extension);
            return mediaType;
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts file extension in lowercase without leading dot.
    /// </summary>
    private string GetFileExtension(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return string.Empty;

        var ext = Path.GetExtension(filename);
        if (string.IsNullOrEmpty(ext))
            return string.Empty;

        return ext.TrimStart('.').ToLowerInvariant();
    }
}
