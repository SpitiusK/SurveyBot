using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SurveyBot.Core.DTOs.Media;

/// <summary>
/// Data transfer object representing the complete MediaContent JSON structure.
/// Used for serialization/deserialization of the Question.MediaContent JSONB field.
/// This is the top-level container that holds all media items for a question.
/// </summary>
public class MediaContentDto
{
    /// <summary>
    /// Gets or sets the schema version for this MediaContent structure.
    /// Used to support future schema evolution while maintaining backward compatibility.
    /// Current version: "1.0"
    /// </summary>
    [Required(ErrorMessage = "Version is required")]
    [RegularExpression(@"^\d+\.\d+$", ErrorMessage = "Version must be in format 'major.minor' (e.g., '1.0')")]
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the collection of media items attached to this question.
    /// Can be an empty array if no media is attached.
    /// Each item represents a single media file (image, video, audio, or document).
    /// </summary>
    [Required(ErrorMessage = "Items collection is required")]
    [JsonPropertyName("items")]
    public List<MediaItemDto> Items { get; set; } = new List<MediaItemDto>();
}
