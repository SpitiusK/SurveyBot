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
    Rating = 3,

    /// <summary>
    /// Geographic location question requesting latitude/longitude coordinates.
    /// Displayed via Telegram's location sharing functionality.
    /// </summary>
    Location = 4,

    /// <summary>
    /// Numeric input question accepting integers or decimals.
    /// Supports validation for min/max range and decimal places via OptionsJson.
    /// </summary>
    Number = 5,

    /// <summary>
    /// Date input question accepting dates in DD.MM.YYYY format.
    /// Supports validation for min/max date range via OptionsJson.
    /// </summary>
    Date = 6
}
