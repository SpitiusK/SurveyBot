using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SurveyBot.Core.DTOs.Media;

/// <summary>
/// Data transfer object representing a single media item attached to a question.
/// Used for serialization/deserialization of individual items in MediaContent JSON.
/// </summary>
public class MediaItemDto
{
    /// <summary>
    /// Gets or sets the unique identifier for this media item.
    /// Must be a valid UUID v4 format.
    /// </summary>
    [Required(ErrorMessage = "Media item ID is required")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the media type category.
    /// Valid values: image, video, audio, document
    /// </summary>
    [Required(ErrorMessage = "Media type is required")]
    [JsonPropertyName("type")]
    [RegularExpression("^(image|video|audio|document)$", ErrorMessage = "Type must be one of: image, video, audio, document")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative path to the media file from storage root.
    /// Example: /media/surveys/123/questions/456/uuid.ext
    /// </summary>
    [Required(ErrorMessage = "File path is required")]
    [MaxLength(500, ErrorMessage = "File path cannot exceed 500 characters")]
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user-friendly filename displayed in the UI.
    /// Example: product-diagram.png
    /// </summary>
    [Required(ErrorMessage = "Display name is required")]
    [MaxLength(255, ErrorMessage = "Display name cannot exceed 255 characters")]
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// Must be a positive integer.
    /// </summary>
    [Required(ErrorMessage = "File size is required")]
    [Range(1, long.MaxValue, ErrorMessage = "File size must be greater than 0")]
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the media file.
    /// Example: image/png, video/mp4, audio/mpeg, application/pdf
    /// </summary>
    [Required(ErrorMessage = "MIME type is required")]
    [MaxLength(100, ErrorMessage = "MIME type cannot exceed 100 characters")]
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the upload timestamp in ISO 8601 format (UTC).
    /// Example: 2025-11-18T10:30:00Z
    /// </summary>
    [Required(ErrorMessage = "Upload timestamp is required")]
    [JsonPropertyName("uploadedAt")]
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Gets or sets alternative text for accessibility purposes.
    /// Strongly recommended for images, optional for other media types.
    /// Used by screen readers and when media cannot be displayed.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Alt text cannot exceed 500 characters")]
    [JsonPropertyName("altText")]
    public string? AltText { get; set; }

    /// <summary>
    /// Gets or sets the relative path to the thumbnail/preview image.
    /// Used for videos, documents, and optionally for images.
    /// Example: /media/surveys/123/questions/456/thumbs/uuid.jpg
    /// </summary>
    [MaxLength(500, ErrorMessage = "Thumbnail path cannot exceed 500 characters")]
    [JsonPropertyName("thumbnailPath")]
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// Gets or sets the display order of this media item within the question.
    /// Zero-indexed, should be sequential (0, 1, 2, ...) but gaps are allowed.
    /// </summary>
    [Required(ErrorMessage = "Order is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Order must be non-negative")]
    [JsonPropertyName("order")]
    public int Order { get; set; }
}
