namespace SurveyBot.Core.DTOs.Response;

/// <summary>
/// Data transfer object for listing responses with summary information.
/// </summary>
public class ResponseListDto
{
    /// <summary>
    /// Gets or sets the response ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the survey this response belongs to.
    /// </summary>
    public int SurveyId { get; set; }

    /// <summary>
    /// Gets or sets the Telegram ID of the respondent.
    /// </summary>
    public long RespondentTelegramId { get; set; }

    /// <summary>
    /// Gets or sets the respondent's Telegram username (if available).
    /// </summary>
    public string? RespondentUsername { get; set; }

    /// <summary>
    /// Gets or sets the respondent's first name (if available).
    /// </summary>
    public string? RespondentFirstName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the response is complete.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Gets or sets the number of questions answered.
    /// </summary>
    public int AnsweredCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of questions.
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the response was started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the response was submitted.
    /// </summary>
    public DateTime? SubmittedAt { get; set; }
}
