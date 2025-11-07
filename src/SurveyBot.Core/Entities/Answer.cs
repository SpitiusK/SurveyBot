using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents an individual answer to a question within a response.
/// </summary>
public class Answer
{
    /// <summary>
    /// Gets or sets the answer ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the response this answer belongs to.
    /// </summary>
    [Required]
    public int ResponseId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the question this answer is for.
    /// </summary>
    [Required]
    public int QuestionId { get; set; }

    /// <summary>
    /// Gets or sets the text answer for text-based questions.
    /// </summary>
    public string? AnswerText { get; set; }

    /// <summary>
    /// Gets or sets the JSON answer for complex question types (multiple choice, rating, etc.).
    /// Stored as JSONB in PostgreSQL for efficient querying.
    /// </summary>
    public string? AnswerJson { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the answer was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Gets or sets the response this answer belongs to.
    /// </summary>
    public Response Response { get; set; } = null!;

    /// <summary>
    /// Gets or sets the question this answer is for.
    /// </summary>
    public Question Question { get; set; } = null!;
}
