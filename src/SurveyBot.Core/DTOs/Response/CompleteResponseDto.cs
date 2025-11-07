using System.ComponentModel.DataAnnotations;
using SurveyBot.Core.DTOs.Answer;

namespace SurveyBot.Core.DTOs.Response;

/// <summary>
/// Data transfer object for completing a survey response with all answers at once.
/// Alternative to submitting answers one by one.
/// </summary>
public class CompleteResponseDto
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

    /// <summary>
    /// Gets or sets the list of all answers in this response.
    /// </summary>
    [Required(ErrorMessage = "Answers are required")]
    [MinLength(1, ErrorMessage = "At least one answer is required")]
    public List<CreateAnswerDto> Answers { get; set; } = new();
}
