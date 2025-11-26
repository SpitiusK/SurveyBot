using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents a user's response to a survey.
/// </summary>
public class Response
{
    /// <summary>
    /// Gets or sets the response ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the survey this response belongs to.
    /// </summary>
    [Required]
    public int SurveyId { get; set; }

    /// <summary>
    /// Gets or sets the Telegram ID of the user who submitted this response.
    /// Note: This is not a foreign key to allow anonymous responses.
    /// </summary>
    [Required]
    public long RespondentTelegramId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the response is complete.
    /// </summary>
    [Required]
    public bool IsComplete { get; set; } = false;

    /// <summary>
    /// Gets or sets the timestamp when the user started the response.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the response was submitted.
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    // NEW: Conditional flow tracking

    /// <summary>
    /// Gets or sets the list of question IDs visited in this response.
    /// Used to prevent revisiting the same question (runtime cycle prevention).
    /// Stored as PostgreSQL JSON array.
    /// </summary>
    public List<int> VisitedQuestionIds { get; set; } = new();

    /// <summary>
    /// Helper method to check if a question has been visited in this response.
    /// </summary>
    public bool HasVisitedQuestion(int questionId) =>
        VisitedQuestionIds.Contains(questionId);

    /// <summary>
    /// Helper method to record a question as visited in this response.
    /// </summary>
    public void RecordVisitedQuestion(int questionId)
    {
        if (!VisitedQuestionIds.Contains(questionId))
            VisitedQuestionIds.Add(questionId);
    }

    // Navigation properties

    /// <summary>
    /// Gets or sets the survey this response belongs to.
    /// </summary>
    public Survey Survey { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of answers in this response.
    /// </summary>
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
