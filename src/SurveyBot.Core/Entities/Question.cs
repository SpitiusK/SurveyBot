using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents a question within a survey.
/// </summary>
public class Question : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the survey this question belongs to.
    /// </summary>
    [Required]
    public int SurveyId { get; set; }

    /// <summary>
    /// Gets or sets the question text.
    /// </summary>
    [Required]
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the question type.
    /// </summary>
    [Required]
    public QuestionType QuestionType { get; set; }

    /// <summary>
    /// Gets or sets the order index of the question within the survey (0-based).
    /// </summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int OrderIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this question is required.
    /// </summary>
    [Required]
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Gets or sets the JSON options for choice-based questions.
    /// Stored as JSONB in PostgreSQL for efficient querying.
    /// </summary>
    public string? OptionsJson { get; set; }

    /// <summary>
    /// Gets or sets the multimedia content metadata for this question.
    /// Stored as JSONB in PostgreSQL containing file information (type, path, size, etc.).
    /// Null for questions without multimedia content.
    /// </summary>
    public string? MediaContent { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the survey this question belongs to.
    /// </summary>
    public Survey Survey { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of answers to this question across all responses.
    /// </summary>
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
