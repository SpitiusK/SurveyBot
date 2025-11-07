using SurveyBot.Core.Entities;

namespace SurveyBot.Core.DTOs.Answer;

/// <summary>
/// Data transfer object for reading answer details.
/// </summary>
public class AnswerDto
{
    /// <summary>
    /// Gets or sets the answer ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the response this answer belongs to.
    /// </summary>
    public int ResponseId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the question this answer is for.
    /// </summary>
    public int QuestionId { get; set; }

    /// <summary>
    /// Gets or sets the question text for context.
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the question type for context.
    /// </summary>
    public QuestionType QuestionType { get; set; }

    /// <summary>
    /// Gets or sets the text answer for text-based questions.
    /// </summary>
    public string? AnswerText { get; set; }

    /// <summary>
    /// Gets or sets the selected option(s) for choice-based questions.
    /// Single string for SingleChoice, array for MultipleChoice.
    /// </summary>
    public List<string>? SelectedOptions { get; set; }

    /// <summary>
    /// Gets or sets the rating value for rating questions (1-5).
    /// </summary>
    public int? RatingValue { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the answer was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
