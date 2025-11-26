namespace SurveyBot.Core.Constants;

/// <summary>
/// Constants used throughout the survey system.
/// </summary>
public static class SurveyConstants
{
    /// <summary>
    /// Maximum number of questions in a single survey.
    /// Used for validation and performance optimization.
    /// </summary>
    public const int MaxQuestionsPerSurvey = 100;

    /// <summary>
    /// Maximum number of options per question.
    /// Used for validation and UI constraints.
    /// </summary>
    public const int MaxOptionsPerQuestion = 50;

    /// <summary>
    /// Maximum length of a survey code (6 alphanumeric characters).
    /// </summary>
    public const int SurveyCodeLength = 6;

    /// <summary>
    /// Minimum length of a survey title.
    /// </summary>
    public const int SurveyTitleMinLength = 3;

    /// <summary>
    /// Maximum length of a survey title.
    /// </summary>
    public const int SurveyTitleMaxLength = 500;

    /// <summary>
    /// Minimum value for a rating question (inclusive).
    /// </summary>
    public const int RatingMinValue = 1;

    /// <summary>
    /// Maximum value for a rating question (inclusive).
    /// </summary>
    public const int RatingMaxValue = 5;

    /// <summary>
    /// Pagination default page size.
    /// </summary>
    public const int PaginationDefaultPageSize = 20;

    /// <summary>
    /// Pagination maximum page size.
    /// </summary>
    public const int PaginationMaxPageSize = 100;
}
