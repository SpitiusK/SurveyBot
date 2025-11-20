using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Media;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SurveyBot.API.Controllers;

/// <summary>
/// Controller for managing media uploads and operations.
/// Handles file uploads with validation, storage, and thumbnail generation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly IMediaStorageService _mediaStorageService;
    private readonly IMediaValidationService _mediaValidationService;
    private readonly ILogger<MediaController> _logger;

    /// <summary>
    /// Initializes a new instance of the MediaController.
    /// </summary>
    /// <param name="mediaStorageService">Media storage service for file operations.</param>
    /// <param name="mediaValidationService">Media validation service for file validation.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    public MediaController(
        IMediaStorageService mediaStorageService,
        IMediaValidationService mediaValidationService,
        ILogger<MediaController> logger)
    {
        _mediaStorageService = mediaStorageService;
        _mediaValidationService = mediaValidationService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a media file with unified handling - auto-detects file type.
    /// </summary>
    /// <param name="file">The media file to upload. Supports: images (jpg, png, gif, webp, bmp, tiff, svg), videos (mp4, webm, mov, avi, mkv, flv, wmv), audio (mp3, wav, ogg, m4a, flac, aac), documents (pdf, doc, docx, xls, xlsx, ppt, pptx, txt, rtf, csv, json, xml, md), archives (zip, rar, 7z, tar, gz, bz2).</param>
    /// <param name="mediaType">[OPTIONAL] The type of media for explicit type specification. If omitted, type is auto-detected from file content.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>201 Created with the uploaded MediaItemDto including id, filePath, detectedType, thumbnail, etc.</returns>
    /// <remarks>
    /// File size limits:
    /// - Images: 10 MB
    /// - Videos: 50 MB
    /// - Audio: 20 MB
    /// - Documents: 25 MB
    /// - Archives: 100 MB
    ///
    /// Auto-detection strategy:
    /// 1. File magic bytes (binary signature) - most reliable
    /// 2. MIME type from file
    /// 3. File extension - fallback only
    ///
    /// Requires authentication (Bearer token).
    ///
    /// Example requests:
    /// # Auto-detect file type (RECOMMENDED - NEW)
    /// POST /api/media/upload
    /// Content-Type: multipart/form-data
    /// form-data:
    /// - file: [binary file data]
    ///
    /// # Optional explicit type (backward compatible - OLD)
    /// POST /api/media/upload?mediaType=video
    /// Content-Type: multipart/form-data
    /// form-data:
    /// - file: [binary file data]
    /// </remarks>
    /// <response code="201">Successfully uploaded media file</response>
    /// <response code="400">Invalid request data or validation failure</response>
    /// <response code="401">Unauthorized - invalid or missing token</response>
    /// <response code="413">File exceeds maximum allowed size</response>
    /// <response code="500">Internal server error during upload or storage</response>
    [HttpPost("upload")]
    [SwaggerOperation(
        Summary = "Upload media file with auto-detection",
        Description = "Uploads any file with unified handling. Auto-detects file type from content if mediaType not specified. Supports all Telegram-compatible formats.",
        Tags = new[] { "Media" }
    )]
    [ProducesResponseType(typeof(ApiResponse<MediaItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MediaItemDto>>> UploadMedia(
        IFormFile file,
        [FromQuery] string? mediaType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting media upload. ExplicitMediaType: {MediaType}, FileName: {FileName}",
                mediaType ?? "(auto-detect)", file?.FileName ?? "null");

            // 1. Validate input parameters
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload failed: No file provided");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "File is required",
                    Data = new Dictionary<string, string>
                    {
                        { "file", "File is required and must not be empty" }
                    }
                });
            }

            // 2. Determine media type (auto-detect or use provided)
            string detectedMediaType;
            if (!string.IsNullOrWhiteSpace(mediaType))
            {
                // Explicit media type provided (backward compatibility)
                var validMediaTypes = new[] { "image", "video", "audio", "document", "archive" };
                if (!validMediaTypes.Contains(mediaType.ToLowerInvariant()))
                {
                    _logger.LogWarning("Upload failed: Invalid media type '{MediaType}'", mediaType);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid media type",
                        Data = new Dictionary<string, string>
                        {
                            { "mediaType", $"Invalid media type. Allowed: {string.Join(", ", validMediaTypes)}" }
                        }
                    });
                }
                detectedMediaType = mediaType.ToLowerInvariant();
                _logger.LogDebug("Using explicitly provided media type: {MediaType}", detectedMediaType);
            }
            else
            {
                // Auto-detect media type from file
                _logger.LogDebug("Auto-detecting media type for file: {FileName}", file.FileName);
                using (var stream = file.OpenReadStream())
                {
                    var (validationResult, autoDetectedType) = await _mediaValidationService.ValidateMediaWithAutoDetectionAsync(
                        stream,
                        file.FileName,
                        cancellationToken);

                    if (!validationResult.IsValid || string.IsNullOrEmpty(autoDetectedType))
                    {
                        _logger.LogWarning("File auto-detection failed for {FileName}: {ErrorMessage}",
                            file.FileName, validationResult.ErrorMessage);

                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = validationResult.ErrorMessage ?? "File type not supported",
                            Data = validationResult.Errors
                        });
                    }

                    detectedMediaType = autoDetectedType;
                    _logger.LogInformation("Auto-detected media type '{MediaType}' for file {FileName}",
                        detectedMediaType, file.FileName);
                }
            }

            // 3. Check file size before processing
            var fileSizeMB = file.Length / 1024.0 / 1024.0;
            var maxSizeMB = detectedMediaType switch
            {
                "image" => 10,
                "video" => 50,
                "audio" => 20,
                "document" => 25,
                "archive" => 100,
                _ => 10
            };

            if (fileSizeMB > maxSizeMB)
            {
                _logger.LogWarning("Upload failed: File size {FileSizeMB:F2} MB exceeds limit of {MaxSizeMB} MB for {MediaType}",
                    fileSizeMB, maxSizeMB, detectedMediaType);
                return StatusCode(StatusCodes.Status413PayloadTooLarge, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"File exceeds maximum allowed size of {maxSizeMB} MB for {detectedMediaType} files"
                });
            }

            // 4. Final validation with detected type
            MediaValidationResult finalValidation;
            using (var stream = file.OpenReadStream())
            {
                _logger.LogDebug("Final validation: {FileName}, Size: {FileSize} bytes, Type: {MediaType}",
                    file.FileName, file.Length, detectedMediaType);

                finalValidation = await _mediaValidationService.ValidateMediaAsync(
                    stream,
                    file.FileName,
                    detectedMediaType,
                    cancellationToken);
            }

            if (!finalValidation.IsValid)
            {
                _logger.LogWarning("File validation failed for {FileName}: {ErrorMessage}",
                    file.FileName, finalValidation.ErrorMessage);

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = finalValidation.ErrorMessage ?? "Validation failed",
                    Data = finalValidation.Errors
                });
            }

            // 5. Save media with detected type
            MediaItemDto mediaItem;
            using (var stream = file.OpenReadStream())
            {
                _logger.LogDebug("Saving media file: {FileName} as type {MediaType}", file.FileName, detectedMediaType);

                mediaItem = await _mediaStorageService.SaveMediaAsync(
                    stream,
                    file.FileName,
                    detectedMediaType,
                    cancellationToken);
            }

            _logger.LogInformation(
                "Media upload successful. ID: {MediaId}, DetectedType: {MediaType}, Size: {FileSize} bytes, Path: {FilePath}",
                mediaItem.Id, mediaItem.Type, mediaItem.FileSize, mediaItem.FilePath);

            // 6. Return response
            return CreatedAtAction(
                nameof(UploadMedia),
                new { id = mediaItem.Id },
                ApiResponse<MediaItemDto>.Ok(mediaItem, $"Media uploaded successfully. Type: {detectedMediaType}"));
        }
        catch (MediaValidationException ex)
        {
            _logger.LogWarning(ex, "Media validation exception during upload: {Message}", ex.Message);

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = ex.ValidationErrors
            });
        }
        catch (MediaStorageException ex)
        {
            _logger.LogError(ex, "Media storage exception during upload: {Message}, FilePath: {FilePath}",
                ex.Message, ex.FilePath);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while saving the media file. Please try again."
            });
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO exception during media upload: {Message}", ex.Message);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while reading or writing the file. Please try again."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during media upload for file: {FileName}",
                file?.FileName ?? "unknown");

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An unexpected error occurred while uploading the media file. Please try again."
            });
        }
    }

    /// <summary>
    /// Delete a media file from a question.
    /// </summary>
    /// <param name="mediaId">The ID of the media item to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>204 No Content on success.</returns>
    /// <remarks>
    /// Requires authentication (Bearer token).
    ///
    /// Example request:
    /// DELETE /api/media/{mediaId}
    /// </remarks>
    /// <response code="204">Successfully deleted media file</response>
    /// <response code="400">Invalid media ID</response>
    /// <response code="401">Unauthorized - invalid or missing token</response>
    /// <response code="404">Media file not found</response>
    /// <response code="500">Internal server error during deletion</response>
    [HttpDelete("{mediaId}")]
    [SwaggerOperation(
        Summary = "Delete a media file",
        Description = "Deletes a media file from storage. Requires the media ID.",
        Tags = new[] { "Media" }
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMedia(
        string mediaId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting media deletion. MediaId: {MediaId}", mediaId);

            // Validate mediaId is not empty
            if (string.IsNullOrWhiteSpace(mediaId))
            {
                _logger.LogWarning("Delete media request with empty mediaId");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Media ID is required",
                    Data = new Dictionary<string, string>
                    {
                        { "mediaId", "Media ID is required and must not be empty" }
                    }
                });
            }

            // Delete the media file from storage
            var deleted = await _mediaStorageService.DeleteMediaAsync(mediaId, cancellationToken);

            if (!deleted)
            {
                _logger.LogWarning("Media file not found: {MediaId}", mediaId);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Media file not found"
                });
            }

            _logger.LogInformation("Media deleted successfully: {MediaId}", mediaId);
            return NoContent();
        }
        catch (MediaStorageException ex)
        {
            _logger.LogError(ex, "Media storage exception during deletion: {MediaId}", mediaId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting the media file. Please try again."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during media deletion: {MediaId}", mediaId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An unexpected error occurred while deleting the media file. Please try again."
            });
        }
    }
}
