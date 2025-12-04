namespace SurveyBot.Core.DTOs.Statistics;

/// <summary>
/// Data transfer object for survey-level statistics.
/// </summary>
public class SurveyStatisticsDto
{
    /// <summary>
    /// Gets or sets the survey ID.
    /// </summary>
    public int SurveyId { get; set; }

    /// <summary>
    /// Gets or sets the survey title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of responses started.
    /// </summary>
    public int TotalResponses { get; set; }

    /// <summary>
    /// Gets or sets the number of completed responses.
    /// </summary>
    public int CompletedResponses { get; set; }

    /// <summary>
    /// Gets or sets the number of incomplete/abandoned responses.
    /// </summary>
    public int IncompleteResponses { get; set; }

    /// <summary>
    /// Gets or sets the completion rate as a percentage (0-100).
    /// </summary>
    public double CompletionRate { get; set; }

    /// <summary>
    /// Gets or sets the average completion time in minutes for completed responses.
    /// </summary>
    public double? AverageCompletionTimeMinutes { get; set; }

    /// <summary>
    /// Gets or sets the number of unique respondents.
    /// </summary>
    public int UniqueRespondents { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the first response.
    /// </summary>
    public DateTime? FirstResponseAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the most recent response.
    /// </summary>
    public DateTime? LastResponseAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the survey was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the statistics for each question in the survey.
    /// </summary>
    public List<QuestionStatisticsDto> QuestionStatistics { get; set; } = new();
}
