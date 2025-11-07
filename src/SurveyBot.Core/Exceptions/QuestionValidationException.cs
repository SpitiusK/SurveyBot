namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when question validation fails.
/// </summary>
public class QuestionValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionValidationException"/> class.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    public QuestionValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionValidationException"/> class.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    /// <param name="errors">Dictionary of validation errors by field name.</param>
    public QuestionValidationException(string message, Dictionary<string, string[]> errors)
        : base(message)
    {
        ValidationErrors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionValidationException"/> class.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public QuestionValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the validation errors by field name.
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; }
}
