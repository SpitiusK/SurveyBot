using System.Text.Json.Serialization;
using SurveyBot.Core.Enums;

namespace SurveyBot.Core.DTOs;

/// <summary>
/// DTO representing the conditional flow configuration for a question.
/// </summary>
public class ConditionalFlowDto
{
    /// <summary>
    /// Question ID this flow belongs to.
    /// </summary>
    public int QuestionId { get; set; }

    /// <summary>
    /// Whether this question supports branching logic.
    /// </summary>
    public bool SupportsBranching { get; set; }

    /// <summary>
    /// For non-branching questions, the default navigation behavior.
    /// All answers navigate according to this determinant.
    /// Null means sequential flow.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NextQuestionDeterminantDto? DefaultNext { get; set; }

    /// <summary>
    /// For branching questions, the navigation for each option.
    /// </summary>
    public List<OptionFlowDto> OptionFlows { get; set; } = new();
}

/// <summary>
/// DTO representing the navigation for a specific question option.
/// </summary>
public class OptionFlowDto
{
    /// <summary>
    /// The option ID.
    /// </summary>
    public int OptionId { get; set; }

    /// <summary>
    /// The option text.
    /// </summary>
    public string OptionText { get; set; } = string.Empty;

    /// <summary>
    /// The navigation behavior for this option.
    /// Determines where selecting this option leads.
    /// </summary>
    public NextQuestionDeterminantDto Next { get; set; } = null!;

    /// <summary>
    /// Whether this option leads to end of survey.
    /// Convenience property for UI display.
    /// </summary>
    [JsonIgnore]
    public bool IsEndOfSurvey => Next?.Type == NextStepType.EndSurvey;
}
