using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.Branching;

/// <summary>
/// DTO for creating a new branching rule.
/// </summary>
public class CreateBranchingRuleDto
{
    /// <summary>
    /// The source question ID (the question that has the branching rule)
    /// </summary>
    [Required(ErrorMessage = "SourceQuestionId is required")]
    public int SourceQuestionId { get; set; }

    /// <summary>
    /// The target question ID (where to branch to if condition matches)
    /// </summary>
    [Required(ErrorMessage = "TargetQuestionId is required")]
    public int TargetQuestionId { get; set; }

    /// <summary>
    /// The branching condition to evaluate
    /// </summary>
    [Required(ErrorMessage = "Condition is required")]
    public BranchingConditionDto Condition { get; set; } = null!;
}
