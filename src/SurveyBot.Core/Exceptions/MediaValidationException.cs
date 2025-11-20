namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when media file validation fails.
/// Contains detailed validation error messages for user feedback.
/// </summary>
public class MediaValidationException : Exception
{
    /// <summary>
    /// Gets the dictionary of field-specific validation errors.
    /// Key: field name (e.g., "extension", "size", "mimeType")
    /// Value: error message for that field
    /// </summary>
    public IReadOnlyDictionary<string, string>? ValidationErrors { get; }

    /// <summary>
    /// Initializes a new instance of the MediaValidationException class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public MediaValidationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the MediaValidationException class with validation errors.
    /// </summary>
    /// <param name="message">The summary error message.</param>
    /// <param name="validationErrors">Field-specific validation errors.</param>
    public MediaValidationException(string message, Dictionary<string, string> validationErrors)
        : base(message)
    {
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// Initializes a new instance of the MediaValidationException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MediaValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
