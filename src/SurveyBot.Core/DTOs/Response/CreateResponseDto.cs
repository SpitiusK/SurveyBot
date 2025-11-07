using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.Response;

/// <summary>
/// Data transfer object for starting a new survey response.
/// </summary>
public class CreateResponseDto
{
    /// <summary>
    /// Gets or sets the Telegram ID of the respondent.
    /// </summary>
    [Required(ErrorMessage = "Respondent Telegram ID is required")]
    [Range(1, long.MaxValue, ErrorMessage = "Invalid Telegram ID")]
    public long RespondentTelegramId { get; set; }

    /// <summary>
    /// Gets or sets the respondent's Telegram username (optional).
    /// </summary>
    [MaxLength(255, ErrorMessage = "Username cannot exceed 255 characters")]
    public string? RespondentUsername { get; set; }

    /// <summary>
    /// Gets or sets the respondent's first name (optional).
    /// </summary>
    [MaxLength(255, ErrorMessage = "First name cannot exceed 255 characters")]
    public string? RespondentFirstName { get; set; }
}
