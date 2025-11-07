namespace SurveyBot.Core.Entities;

/// <summary>
/// Defines the types of questions that can be asked in a survey.
/// </summary>
public enum QuestionType
{
    /// <summary>
    /// Free-form text answer.
    /// </summary>
    Text = 0,

    /// <summary>
    /// Single choice from multiple options (radio button).
    /// </summary>
    SingleChoice = 1,

    /// <summary>
    /// Multiple choices from multiple options (checkboxes).
    /// </summary>
    MultipleChoice = 2,

    /// <summary>
    /// Numeric rating (1-5 scale).
    /// </summary>
    Rating = 3
}
