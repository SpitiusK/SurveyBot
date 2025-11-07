namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when survey validation fails.
/// </summary>
public class SurveyValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyValidationException"/> class.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    public SurveyValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyValidationException"/> class.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    /// <param name="errors">Dictionary of validation errors by field name.</param>
    public SurveyValidationException(string message, Dictionary<string, string[]> errors)
        : base(message)
    {
        ValidationErrors = errors;
    }

    /// <summary>
    /// Gets the validation errors by field name.
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; }
}
