using SurveyBot.Core.DTOs.Media;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Service interface for media storage and management operations.
/// Handles file upload, deletion, URL generation, thumbnails, and validation.
/// </summary>
public interface IMediaStorageService
{
    /// <summary>
    /// Saves a media file to storage and returns the media item information.
    /// Creates appropriate directory structure and generates unique filename.
    /// </summary>
    /// <param name="fileStream">The stream of the uploaded file.</param>
    /// <param name="filename">The original filename with extension.</param>
    /// <param name="mediaType">The media type category (image, video, audio, document).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A MediaItemDto containing file path, size, MIME type, and metadata.</returns>
    /// <exception cref="Exceptions.MediaValidationException">Thrown when file validation fails.</exception>
    /// <exception cref="Exceptions.MediaStorageException">Thrown when file storage operation fails.</exception>
    Task<MediaItemDto> SaveMediaAsync(
        Stream fileStream,
        string filename,
        string mediaType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a media file from storage using its relative file path.
    /// Also removes associated thumbnail if one exists.
    /// </summary>
    /// <param name="filePath">The relative path to the media file from storage root.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the file was successfully deleted; false if file was not found.</returns>
    /// <exception cref="Exceptions.MediaStorageException">Thrown when file deletion operation fails.</exception>
    Task<bool> DeleteMediaAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a public URL for accessing a media file.
    /// URL format depends on storage implementation (local disk, cloud storage, etc.).
    /// </summary>
    /// <param name="filePath">The relative path to the media file from storage root.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The public URL for accessing the media file.</returns>
    /// <exception cref="Exceptions.MediaStorageException">Thrown when URL generation fails.</exception>
    Task<string> GetMediaUrlAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a thumbnail image for the specified media file.
    /// Primarily used for images and video files (first frame).
    /// Returns null if thumbnail generation is not supported for the media type.
    /// </summary>
    /// <param name="imagePath">The relative path to the media file from storage root.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The relative path to the generated thumbnail file, or null if thumbnail generation
    /// is not supported or failed.
    /// </returns>
    /// <exception cref="Exceptions.MediaStorageException">Thrown when thumbnail generation fails critically.</exception>
    Task<string?> GenerateThumbnailAsync(
        string imagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a media file against allowed file types, size limits, and content.
    /// Checks MIME type, file extension, file size, and optionally scans for malicious content.
    /// </summary>
    /// <param name="fileStream">The stream of the uploaded file.</param>
    /// <param name="filename">The original filename with extension.</param>
    /// <param name="mediaType">The expected media type category (image, video, audio, document).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A tuple containing validation result:
    /// - IsValid: True if the file passes all validation checks.
    /// - ErrorMessage: Null if valid, otherwise contains a user-friendly error message.
    /// </returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateMediaAsync(
        Stream fileStream,
        string filename,
        string mediaType,
        CancellationToken cancellationToken = default);
}
