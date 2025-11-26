using SurveyBot.Core.DTOs.Media;
using SurveyBot.Core.Entities;

namespace SurveyBot.Core.DTOs.Question;

/// <summary>
/// Data transfer object for reading question details.
/// </summary>
public class QuestionDto
{
    /// <summary>
    /// Gets or sets the question ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the survey this question belongs to.
    /// </summary>
    public int SurveyId { get; set; }

    /// <summary>
    /// Gets or sets the question text.
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the question type.
    /// </summary>
    public QuestionType QuestionType { get; set; }

    /// <summary>
    /// Gets or sets the order index of the question within the survey.
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this question is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the options for choice-based questions (legacy format).
    /// Null for text and rating questions.
    /// For questions with conditional flow, use OptionDetails instead.
    /// </summary>
    public List<string>? Options { get; set; }

    /// <summary>
    /// Gets or sets the detailed option information for choice-based questions.
    /// Includes option IDs and conditional flow configuration.
    /// Populated for questions with QuestionOption entities.
    /// </summary>
    public List<QuestionOptionDto>? OptionDetails { get; set; }

    /// <summary>
    /// Gets or sets the media content associated with this question.
    /// Deserialized from the Question.MediaContent JSONB field.
    /// Null if no media is attached or if the question was created before multimedia support.
    /// </summary>
    public MediaContentDto? MediaContent { get; set; }

    // NEW: Conditional flow configuration

    /// <summary>
    /// Gets or sets the default navigation behavior for non-branching questions.
    /// For Text and MultipleChoice questions, all answers navigate according to this determinant.
    /// Null means sequential flow (next question by OrderIndex).
    /// For branching questions (SingleChoice, Rating), this is ignored as navigation is per-option.
    /// </summary>
    public NextQuestionDeterminantDto? DefaultNext { get; set; }

    /// <summary>
    /// Gets or sets whether this question type supports conditional branching.
    /// True for SingleChoice and Rating questions.
    /// </summary>
    public bool SupportsBranching { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the question was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the question was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
