using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents a branching rule that determines conditional question flow based on answers.
/// Defines the relationship between a source question and a target question with conditional logic.
/// </summary>
public class QuestionBranchingRule : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the source question (the question being answered).
    /// </summary>
    [Required]
    public int SourceQuestionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the target question (the question to branch to if condition is met).
    /// </summary>
    [Required]
    public int TargetQuestionId { get; set; }

    /// <summary>
    /// Gets or sets the branching condition as JSON.
    /// Stored as JSONB in PostgreSQL for efficient querying.
    /// Contains the logic that determines when this branching rule applies.
    /// </summary>
    [Required]
    public string ConditionJson { get; set; } = string.Empty;

    // Navigation properties

    /// <summary>
    /// Gets or sets the source question that triggers the branching rule.
    /// </summary>
    public Question SourceQuestion { get; set; } = null!;

    /// <summary>
    /// Gets or sets the target question to branch to when the condition is met.
    /// </summary>
    public Question TargetQuestion { get; set; } = null!;
}
