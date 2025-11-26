using System.ComponentModel.DataAnnotations;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Core.Enums;

namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents an individual option for a choice-based question.
/// </summary>
public class QuestionOption : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the question this option belongs to.
    /// </summary>
    [Required]
    public int QuestionId { get; set; }

    /// <summary>
    /// Gets or sets the text of this option.
    /// </summary>
    [Required]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order index of this option within the question.
    /// </summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int OrderIndex { get; set; }

    // NEW: Conditional flow configuration

    /// <summary>
    /// Gets or sets the navigation behavior when this option is selected.
    /// For branching questions (SingleChoice, Rating), determines where to go if this option is selected.
    /// Ignored for non-branching questions.
    /// Set to null to maintain backward compatibility (no flow defined for this option).
    /// Use NextQuestionDeterminant.End() to end the survey or NextQuestionDeterminant.ToQuestion(id) to navigate.
    /// </summary>
    public NextQuestionDeterminant? Next { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the question this option belongs to.
    /// </summary>
    public Question Question { get; set; } = null!;
}
