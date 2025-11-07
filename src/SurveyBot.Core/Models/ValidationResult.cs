namespace SurveyBot.Core.Models;

/// <summary>
/// Represents the result of answer validation.
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
    /// Gets or sets additional validation details.
    /// </summary>
    public Dictionary<string, string>? Details { get; set; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result indicating success.</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates a failed validation result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static ValidationResult Failure(string errorMessage)
    {
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates a failed validation result with an error message and details.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="details">Additional validation details.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static ValidationResult Failure(string errorMessage, Dictionary<string, string> details)
    {
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            Details = details
        };
    }
}
