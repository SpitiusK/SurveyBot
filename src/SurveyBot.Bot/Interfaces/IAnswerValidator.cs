using SurveyBot.Core.DTOs.Question;

namespace SurveyBot.Bot.Interfaces;

/// <summary>
/// Interface for validating survey answers based on question type and requirements.
/// </summary>
public interface IAnswerValidator
{
    /// <summary>
    /// Validates an answer JSON string against question requirements.
    /// </summary>
    /// <param name="answerJson">The answer JSON to validate.</param>
    /// <param name="question">The question being answered.</param>
    /// <returns>Validation result with success flag and optional error message.</returns>
    ValidationResult ValidateAnswer(string? answerJson, QuestionDto question);
}

/// <summary>
/// Represents the result of an answer validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new()
    {
        IsValid = true,
        ErrorMessage = null
    };

    /// <summary>
    /// Creates a failed validation result with an error message.
    /// </summary>
    public static ValidationResult Failure(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}
