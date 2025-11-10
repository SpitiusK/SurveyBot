using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents a survey with metadata and configuration.
/// </summary>
public class Survey : BaseEntity
{
    /// <summary>
    /// Gets or sets the survey title.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the survey description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the unique survey code for easy sharing.
    /// </summary>
    [MaxLength(10)]
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this survey.
    /// </summary>
    [Required]
    public int CreatorId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the survey is active and accepting responses.
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether users can submit multiple responses.
    /// </summary>
    [Required]
    public bool AllowMultipleResponses { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether results should be shown to respondents.
    /// </summary>
    [Required]
    public bool ShowResults { get; set; } = true;

    // Navigation properties

    /// <summary>
    /// Gets or sets the user who created this survey.
    /// </summary>
    public User Creator { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of questions in this survey.
    /// </summary>
    public ICollection<Question> Questions { get; set; } = new List<Question>();

    /// <summary>
    /// Gets or sets the collection of responses to this survey.
    /// </summary>
    public ICollection<Response> Responses { get; set; } = new List<Response>();
}
