using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SurveyBot.Core.DTOs.Media;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;

namespace SurveyBot.Infrastructure.Services;

/// <summary>
/// Service for managing media storage on the local file system.
/// Stores files in wwwroot/uploads/media organized by year/month.
/// Generates thumbnails for images and handles file validation.
/// </summary>
public class FileSystemMediaStorageService : IMediaStorageService
{
    private readonly string _webRootPath;
    private readonly ILogger<FileSystemMediaStorageService> _logger;
    private readonly IMediaValidationService _validationService;

    // Configuration constants
    private const string StorageDirectory = "uploads/media";
    private const int ThumbnailWidth = 200;
    private const int ThumbnailHeight = 200;
    private const string ThumbnailSuffix = "_thumb";

    // Image extensions that support thumbnail generation
    private static readonly HashSet<string> ThumbnailSupportedExtensions = new()
    {
        "jpg", "jpeg", "png", "gif", "webp"
    };

    // MIME type mappings for file extensions
    private static readonly Dictionary<string, string> ExtensionToMimeType = new()
    {
        { "jpg", "image/jpeg" },
        { "jpeg", "image/jpeg" },
        { "png", "image/png" },
        { "gif", "image/gif" },
        { "webp", "image/webp" },
        { "mp4", "video/mp4" },
        { "webm", "video/webm" },
        { "mov", "video/quicktime" },
        { "mp3", "audio/mpeg" },
        { "wav", "audio/wav" },
        { "ogg", "audio/ogg" },
        { "m4a", "audio/mp4" },
        { "pdf", "application/pdf" },
        { "doc", "application/msword" },
        { "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { "txt", "text/plain" }
    };

    public FileSystemMediaStorageService(
        string webRootPath,
        ILogger<FileSystemMediaStorageService> logger,
        IMediaValidationService validationService)
    {
        _webRootPath = webRootPath ?? throw new ArgumentNullException(nameof(webRootPath));
        _logger = logger;
        _validationService = validationService;
    }

    /// <summary>
    /// Saves a media file to the file system storage.
    /// Validates, generates unique filename, creates directory structure, saves file, and generates thumbnail if image.
    /// </summary>
    public async Task<MediaItemDto> SaveMediaAsync(
        Stream fileStream,
        string filename,
        string mediaType,
        CancellationToken cancellationToken = default)
    {
        if (fileStream == null)
        {
            throw new ArgumentNullException(nameof(fileStream));
        }

        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentNullException(nameof(filename));
        }

        if (string.IsNullOrWhiteSpace(mediaType))
        {
            throw new ArgumentNullException(nameof(mediaType));
        }

        try
        {
            // 1. Validate the media file
            var validationResult = await _validationService.ValidateMediaAsync(
                fileStream, filename, mediaType, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errorMessage = validationResult.ErrorMessage ?? "Media validation failed";
                _logger.LogWarning(
                    "Media validation failed for {Filename}: {ErrorMessage}",
                    filename, errorMessage);

                // Convert errors dictionary to non-nullable if needed
                var errors = validationResult.Errors ?? new Dictionary<string, string>();
                throw new MediaValidationException(errorMessage, errors);
            }

            // Reset stream position after validation
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            // 2. Generate unique filename with GUID
            var extension = Path.GetExtension(filename).TrimStart('.').ToLowerInvariant();
            var uniqueFilename = $"{Guid.NewGuid()}.{extension}";

            // 3. Create directory structure: /uploads/media/{year}/{month}/
            var now = DateTime.UtcNow;
            var year = now.Year.ToString();
            var month = now.Month.ToString("D2");

            var relativePath = Path.Combine(StorageDirectory, year, month);
            var absolutePath = Path.Combine(_webRootPath, relativePath);

            // Ensure directory exists
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
                _logger.LogDebug("Created storage directory: {Path}", absolutePath);
            }

            // 4. Save file to disk
            var filePath = Path.Combine(absolutePath, uniqueFilename);
            long fileSize;

            try
            {
                using (var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);
                    fileSize = fileStreamOutput.Length;
                }

                _logger.LogInformation(
                    "Media file saved successfully: {FilePath} ({FileSize} bytes)",
                    filePath, fileSize);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error saving media file to {FilePath}", filePath);
                throw new MediaStorageException($"Failed to save media file: {ex.Message}", filePath, ex);
            }
            catch (System.UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied saving media file to {FilePath}", filePath);
                throw new MediaStorageException($"Access denied: {ex.Message}", filePath, ex);
            }

            // 5. Get MIME type
            var mimeType = ExtensionToMimeType.TryGetValue(extension, out var mime)
                ? mime
                : "application/octet-stream";

            // 6. Generate thumbnail for images
            string? thumbnailPath = null;
            if (mediaType == "image" && ThumbnailSupportedExtensions.Contains(extension))
            {
                try
                {
                    var relativeFilePath = Path.Combine(relativePath, uniqueFilename).Replace('\\', '/');
                    thumbnailPath = await GenerateThumbnailAsync(relativeFilePath, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the upload if thumbnail generation fails
                    _logger.LogWarning(ex, "Failed to generate thumbnail for {FilePath}", filePath);
                }
            }

            // 7. Build and return MediaItemDto
            var relativeFilePathResult = Path.Combine(relativePath, uniqueFilename).Replace('\\', '/');
            var mediaItem = new MediaItemDto
            {
                Id = Guid.NewGuid().ToString(),
                Type = mediaType,
                FilePath = $"/{relativeFilePathResult}",
                DisplayName = filename,
                FileSize = fileSize,
                MimeType = mimeType,
                UploadedAt = DateTime.UtcNow,
                ThumbnailPath = thumbnailPath,
                Order = 0
            };

            _logger.LogInformation(
                "Media item created: ID={Id}, Type={Type}, Size={Size} bytes, Thumbnail={HasThumbnail}",
                mediaItem.Id, mediaItem.Type, mediaItem.FileSize, thumbnailPath != null);

            return mediaItem;
        }
        catch (MediaValidationException)
        {
            throw; // Re-throw validation exceptions as-is
        }
        catch (MediaStorageException)
        {
            throw; // Re-throw storage exceptions as-is
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error saving media file {Filename}", filename);
            throw new MediaStorageException($"Unexpected error saving media: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes a media file from storage using its relative file path.
    /// Also removes associated thumbnail if one exists.
    /// </summary>
    public async Task<bool> DeleteMediaAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        try
        {
            // Normalize path separators and remove leading slash
            var normalizedPath = filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

            // Validate path to prevent path traversal
            if (normalizedPath.Contains("..") || Path.IsPathRooted(normalizedPath))
            {
                _logger.LogWarning("Attempted path traversal in DeleteMediaAsync: {FilePath}", filePath);
                throw new MediaStorageException($"Invalid file path: {filePath}");
            }

            // Construct full path
            var absolutePath = Path.Combine(_webRootPath, normalizedPath);

            // Check if file exists
            if (!File.Exists(absolutePath))
            {
                _logger.LogWarning("File not found for deletion: {FilePath}", absolutePath);
                return false;
            }

            // Delete main file
            try
            {
                await Task.Run(() => File.Delete(absolutePath), cancellationToken);
                _logger.LogInformation("Media file deleted: {FilePath}", absolutePath);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error deleting media file: {FilePath}", absolutePath);
                throw new MediaStorageException($"Failed to delete media file: {ex.Message}", filePath, ex);
            }
            catch (System.UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied deleting media file: {FilePath}", absolutePath);
                throw new MediaStorageException($"Access denied: {ex.Message}", filePath, ex);
            }

            // Delete thumbnail if exists
            var thumbnailPath = GetThumbnailPath(absolutePath);
            if (File.Exists(thumbnailPath))
            {
                try
                {
                    await Task.Run(() => File.Delete(thumbnailPath), cancellationToken);
                    _logger.LogInformation("Thumbnail deleted: {ThumbnailPath}", thumbnailPath);
                }
                catch (Exception ex)
                {
                    // Log warning but don't fail if thumbnail deletion fails
                    _logger.LogWarning(ex, "Failed to delete thumbnail: {ThumbnailPath}", thumbnailPath);
                }
            }

            return true;
        }
        catch (MediaStorageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting media file: {FilePath}", filePath);
            throw new MediaStorageException($"Unexpected error deleting media: {ex.Message}", filePath, ex);
        }
    }

    /// <summary>
    /// Generates a public URL for accessing a media file.
    /// </summary>
    public Task<string> GetMediaUrlAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        try
        {
            // Validate path to prevent path traversal
            var normalizedPath = filePath.TrimStart('/');
            if (normalizedPath.Contains(".."))
            {
                _logger.LogWarning("Attempted path traversal in GetMediaUrlAsync: {FilePath}", filePath);
                throw new MediaStorageException($"Invalid file path: {filePath}");
            }

            // Construct full path to check if file exists
            var absolutePath = Path.Combine(
                _webRootPath,
                normalizedPath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(absolutePath))
            {
                _logger.LogWarning("File not found for URL generation: {FilePath}", absolutePath);
                throw new MediaStorageException($"File not found: {filePath}");
            }

            // Return URL with forward slashes (web standard)
            var url = filePath.StartsWith('/') ? filePath : $"/{filePath}";
            url = url.Replace('\\', '/');

            _logger.LogDebug("Generated media URL: {Url}", url);
            return Task.FromResult(url);
        }
        catch (MediaStorageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating media URL for {FilePath}", filePath);
            throw new MediaStorageException($"Error generating URL: {ex.Message}", filePath, ex);
        }
    }

    /// <summary>
    /// Generates a thumbnail image for the specified media file.
    /// Only generates for supported image formats (jpg, png, gif, webp).
    /// Returns null if thumbnail generation is not supported or fails.
    /// </summary>
    public async Task<string?> GenerateThumbnailAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        try
        {
            // Check if file extension supports thumbnail generation
            var extension = Path.GetExtension(imagePath).TrimStart('.').ToLowerInvariant();
            if (!ThumbnailSupportedExtensions.Contains(extension))
            {
                _logger.LogDebug("Thumbnail not supported for extension: {Extension}", extension);
                return null;
            }

            // Normalize path
            var normalizedPath = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

            // Validate path
            if (normalizedPath.Contains("..") || Path.IsPathRooted(normalizedPath))
            {
                _logger.LogWarning("Invalid path in GenerateThumbnailAsync: {ImagePath}", imagePath);
                return null;
            }

            // Construct paths
            var sourceAbsolutePath = Path.Combine(_webRootPath, normalizedPath);
            if (!File.Exists(sourceAbsolutePath))
            {
                _logger.LogWarning("Source image not found for thumbnail: {Path}", sourceAbsolutePath);
                return null;
            }

            var thumbnailAbsolutePath = GetThumbnailPath(sourceAbsolutePath);
            var thumbnailRelativePath = GetThumbnailPath(imagePath);

            // Generate thumbnail using ImageSharp
            try
            {
                using var image = await Image.LoadAsync(sourceAbsolutePath, cancellationToken);

                // Resize to fit within thumbnail dimensions while maintaining aspect ratio
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(ThumbnailWidth, ThumbnailHeight),
                    Mode = ResizeMode.Max // Maintain aspect ratio, fit within bounds
                }));

                // Save thumbnail
                await image.SaveAsync(thumbnailAbsolutePath, cancellationToken);

                _logger.LogInformation(
                    "Thumbnail generated: {ThumbnailPath} (Original: {OriginalPath})",
                    thumbnailAbsolutePath, sourceAbsolutePath);

                // Return relative path with forward slashes
                var result = thumbnailRelativePath.StartsWith('/')
                    ? thumbnailRelativePath
                    : $"/{thumbnailRelativePath}";
                return result.Replace('\\', '/');
            }
            catch (UnknownImageFormatException ex)
            {
                _logger.LogWarning(ex, "Unknown image format for thumbnail: {Path}", sourceAbsolutePath);
                return null;
            }
            catch (InvalidImageContentException ex)
            {
                _logger.LogWarning(ex, "Invalid image content for thumbnail: {Path}", sourceAbsolutePath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating thumbnail for {Path}", sourceAbsolutePath);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GenerateThumbnailAsync for {ImagePath}", imagePath);
            return null;
        }
    }

    /// <summary>
    /// Validates a media file before upload.
    /// Delegates to IMediaValidationService and returns result as tuple.
    /// </summary>
    public async Task<(bool IsValid, string? ErrorMessage)> ValidateMediaAsync(
        Stream fileStream,
        string filename,
        string mediaType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = await _validationService.ValidateMediaAsync(
                fileStream, filename, mediaType, cancellationToken);

            return (validationResult.IsValid, validationResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during media validation for {Filename}", filename);
            return (false, $"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the thumbnail path for a given file path.
    /// Inserts "_thumb" suffix before the file extension.
    /// Example: /path/file.jpg -> /path/file_thumb.jpg
    /// </summary>
    private string GetThumbnailPath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        var thumbnailFilename = $"{filenameWithoutExtension}{ThumbnailSuffix}{extension}";
        return Path.Combine(directory, thumbnailFilename);
    }
}
