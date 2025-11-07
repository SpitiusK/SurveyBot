using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.Survey;

/// <summary>
/// Data transfer object for updating an existing survey.
/// </summary>
public class UpdateSurveyDto
{
    /// <summary>
    /// Gets or sets the survey title.
    /// </summary>
    [Required(ErrorMessage = "Survey title is required")]
    [MaxLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
    [MinLength(3, ErrorMessage = "Title must be at least 3 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the survey description.
    /// </summary>
    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether users can submit multiple responses.
    /// </summary>
    public bool AllowMultipleResponses { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether results should be shown to respondents.
    /// </summary>
    public bool ShowResults { get; set; }
}
