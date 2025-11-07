namespace SurveyBot.Core.DTOs.Survey;

/// <summary>
/// Data transfer object for listing surveys with summary information.
/// Used in list views where full details are not needed.
/// </summary>
public class SurveyListDto
{
    /// <summary>
    /// Gets or sets the survey ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the survey title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the survey description (truncated if needed).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the survey is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the number of questions in this survey.
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of responses.
    /// </summary>
    public int TotalResponses { get; set; }

    /// <summary>
    /// Gets or sets the number of completed responses.
    /// </summary>
    public int CompletedResponses { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the survey was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the survey was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
