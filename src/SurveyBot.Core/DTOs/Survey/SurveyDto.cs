using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.DTOs.User;

namespace SurveyBot.Core.DTOs.Survey;

/// <summary>
/// Data transfer object for reading survey details with full information.
/// </summary>
public class SurveyDto
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
    /// Gets or sets the survey description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this survey.
    /// </summary>
    public int CreatorId { get; set; }

    /// <summary>
    /// Gets or sets the creator information.
    /// </summary>
    public UserDto? Creator { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the survey is active and accepting responses.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether users can submit multiple responses.
    /// </summary>
    public bool AllowMultipleResponses { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether results should be shown to respondents.
    /// </summary>
    public bool ShowResults { get; set; }

    /// <summary>
    /// Gets or sets the list of questions in this survey.
    /// </summary>
    public List<QuestionDto> Questions { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of responses to this survey.
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
