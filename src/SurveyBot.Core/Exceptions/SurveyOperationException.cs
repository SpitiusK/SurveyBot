namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when a survey operation cannot be completed due to business rule violations.
/// </summary>
public class SurveyOperationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyOperationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SurveyOperationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyOperationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SurveyOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
