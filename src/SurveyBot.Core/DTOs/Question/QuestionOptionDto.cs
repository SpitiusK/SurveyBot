namespace SurveyBot.Core.DTOs.Question;

/// <summary>
/// Data transfer object for question option details.
/// Used in responses to provide option information including conditional flow.
/// </summary>
public class QuestionOptionDto
{
    /// <summary>
    /// Gets or sets the option ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the option text displayed to users.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order index of the option within the question (0-based).
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// Gets or sets the navigation behavior when this option is selected.
    /// For SingleChoice questions with branching, this determines where selecting this option leads.
    /// Null means sequential flow (next question by OrderIndex).
    /// </summary>
    public NextQuestionDeterminantDto? Next { get; set; }
}
