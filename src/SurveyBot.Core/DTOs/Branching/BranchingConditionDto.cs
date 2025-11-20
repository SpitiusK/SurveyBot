using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.Branching;

/// <summary>
/// DTO representing a branching condition.
/// </summary>
public class BranchingConditionDto
{
    /// <summary>
    /// The comparison operator.
    /// Supported values: Equals, Contains, In, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual
    /// </summary>
    [Required(ErrorMessage = "Operator is required")]
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// Single value for comparison (used with Equals, Contains, GreaterThan, etc.)
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Multiple values for "In" operator (e.g., answer matches any of these values)
    /// </summary>
    public string[]? Values { get; set; }

    /// <summary>
    /// The question type this condition is for (used for validation)
    /// </summary>
    [Required(ErrorMessage = "QuestionType is required")]
    public string QuestionType { get; set; } = string.Empty;
}
