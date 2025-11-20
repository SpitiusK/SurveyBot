using SurveyBot.Core.DTOs.Media;
using SurveyBot.Core.DTOs.Branching;
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
    /// Gets or sets the options for choice-based questions.
    /// Null for text and rating questions.
    /// </summary>
    public List<string>? Options { get; set; }

    /// <summary>
    /// Gets or sets the media content associated with this question.
    /// Deserialized from the Question.MediaContent JSONB field.
    /// Null if no media is attached or if the question was created before multimedia support.
    /// </summary>
    public MediaContentDto? MediaContent { get; set; }

    /// <summary>
    /// Gets or sets the branching rules that originate from this question.
    /// These rules determine which question to show next based on the user's answer.
    /// </summary>
    public List<BranchingRuleDto>? OutgoingRules { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the question was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the question was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
