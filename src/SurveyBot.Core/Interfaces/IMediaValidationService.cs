using SurveyBot.Core.DTOs.Media;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Service interface for validating uploaded media files.
/// Validates file type, size, MIME type, dimensions, duration, and security concerns.
/// </summary>
public interface IMediaValidationService
{
    /// <summary>
    /// Validates a media file before upload.
    /// Performs comprehensive checks including:
    /// - File extension validation against allowed types
    /// - File size validation against media type limits
    /// - MIME type validation (magic byte detection)
    /// - Path traversal prevention
    /// - Malicious file detection
    /// - Filename validation
    /// </summary>
    /// <param name="fileStream">The stream of the uploaded file to validate.</param>
    /// <param name="filename">The original filename with extension.</param>
    /// <param name="mediaType">The expected media type category (image, video, audio, document).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A MediaValidationResult containing:
    /// - IsValid: True if file passes all validations
    /// - ErrorMessage: Summary of all errors (if any)
    /// - Errors: Field-specific validation errors (if any)
    /// </returns>
    Task<MediaValidationResult> ValidateMediaAsync(
        Stream fileStream,
        string filename,
        string mediaType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a media file with automatic type detection.
    /// Performs the same validations as ValidateMediaAsync but auto-detects the media type.
    /// </summary>
    /// <param name="fileStream">The stream of the uploaded file to validate.</param>
    /// <param name="filename">The original filename with extension.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A tuple containing:
    /// - validationResult: MediaValidationResult with validation status and errors
    /// - detectedMediaType: The auto-detected media type (image, video, audio, document, archive)
    /// Returns tuple where detectedMediaType is null/empty if detection failed.
    /// </returns>
    Task<(MediaValidationResult validationResult, string detectedMediaType)> ValidateMediaWithAutoDetectionAsync(
        Stream fileStream,
        string filename,
        CancellationToken cancellationToken = default);
}
