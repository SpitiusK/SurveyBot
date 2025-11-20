using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.Branching;

/// <summary>
/// DTO for updating an existing branching rule.
/// </summary>
public class UpdateBranchingRuleDto
{
    /// <summary>
    /// The target question ID (where to branch to if condition matches)
    /// </summary>
    [Required(ErrorMessage = "TargetQuestionId is required")]
    public int TargetQuestionId { get; set; }

    /// <summary>
    /// The updated branching condition to evaluate
    /// </summary>
    [Required(ErrorMessage = "Condition is required")]
    public BranchingConditionDto Condition { get; set; } = null!;
}
