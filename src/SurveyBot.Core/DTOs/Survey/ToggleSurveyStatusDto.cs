using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.Survey;

/// <summary>
/// Data transfer object for toggling survey active status.
/// </summary>
public class ToggleSurveyStatusDto
{
    /// <summary>
    /// Gets or sets a value indicating whether the survey should be active.
    /// </summary>
    [Required]
    public bool IsActive { get; set; }
}
