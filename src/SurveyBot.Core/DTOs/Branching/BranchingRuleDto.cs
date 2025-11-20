namespace SurveyBot.Core.DTOs.Branching;

/// <summary>
/// DTO representing a branching rule.
/// </summary>
public class BranchingRuleDto
{
    /// <summary>
    /// The branching rule ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The source question ID
    /// </summary>
    public int SourceQuestionId { get; set; }

    /// <summary>
    /// The target question ID
    /// </summary>
    public int TargetQuestionId { get; set; }

    /// <summary>
    /// The branching condition
    /// </summary>
    public BranchingConditionDto Condition { get; set; } = null!;

    /// <summary>
    /// When the rule was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
