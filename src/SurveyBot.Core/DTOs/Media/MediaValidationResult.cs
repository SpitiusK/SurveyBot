using System.Text.Json.Serialization;

namespace SurveyBot.Core.DTOs.Media;

/// <summary>
/// Data transfer object representing the result of media file validation.
/// Contains validation status, error messages, and field-specific validation errors.
/// </summary>
public class MediaValidationResult
{
    /// <summary>
    /// Gets or sets whether the media file passed all validation checks.
    /// </summary>
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets a summary error message describing why validation failed.
    /// Null if the file is valid.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets field-level validation errors.
    /// Dictionary key is the field name (filename, extension, size, mimeType, path).
    /// Dictionary value is the error message for that field.
    /// Null if there are no errors.
    /// </summary>
    [JsonPropertyName("errors")]
    public Dictionary<string, string>? Errors { get; set; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static MediaValidationResult Success()
    {
        return new MediaValidationResult
        {
            IsValid = true,
            ErrorMessage = null,
            Errors = null
        };
    }

    /// <summary>
    /// Creates a failed validation result with a single error message.
    /// </summary>
    public static MediaValidationResult Failure(string errorMessage)
    {
        return new MediaValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            Errors = null
        };
    }

    /// <summary>
    /// Creates a failed validation result with field-specific errors.
    /// </summary>
    public static MediaValidationResult Failure(Dictionary<string, string> errors)
    {
        return new MediaValidationResult
        {
            IsValid = false,
            ErrorMessage = string.Join("; ", errors.Values),
            Errors = errors
        };
    }
}
