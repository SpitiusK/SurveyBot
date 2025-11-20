namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents the condition logic for question branching.
/// This is a value object stored as JSON in the database.
/// </summary>
public class BranchingCondition
{
    /// <summary>
    /// Gets or sets the comparison operator for the branching condition.
    /// </summary>
    public BranchingOperator Operator { get; set; }

    /// <summary>
    /// Gets or sets the value(s) to compare against.
    /// For single values (Equals, GreaterThan, LessThan): use single-element array
    /// For multiple values (In, Contains): use multiple elements
    /// </summary>
    public string[] Values { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the question type this condition applies to.
    /// Used for validation to ensure condition is compatible with question type.
    /// </summary>
    public string QuestionType { get; set; } = string.Empty;
}

/// <summary>
/// Defines the comparison operators available for branching conditions.
/// </summary>
public enum BranchingOperator
{
    /// <summary>
    /// Exact match (for text, single choice, rating)
    /// </summary>
    Equals = 0,

    /// <summary>
    /// Contains text (for text questions only)
    /// </summary>
    Contains = 1,

    /// <summary>
    /// Value is in the provided list (for single/multiple choice)
    /// </summary>
    In = 2,

    /// <summary>
    /// Numeric comparison (for rating questions only)
    /// </summary>
    GreaterThan = 3,

    /// <summary>
    /// Numeric comparison (for rating questions only)
    /// </summary>
    LessThan = 4,

    /// <summary>
    /// Greater than or equal (for rating questions only)
    /// </summary>
    GreaterThanOrEqual = 5,

    /// <summary>
    /// Less than or equal (for rating questions only)
    /// </summary>
    LessThanOrEqual = 6
}
