namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when a requested question is not found.
/// </summary>
public class QuestionNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionNotFoundException"/> class.
    /// </summary>
    /// <param name="questionId">The ID of the question that was not found.</param>
    public QuestionNotFoundException(int questionId)
        : base($"Question with ID {questionId} was not found.")
    {
        QuestionId = questionId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public QuestionNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public QuestionNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the ID of the question that was not found.
    /// </summary>
    public int? QuestionId { get; }
}
