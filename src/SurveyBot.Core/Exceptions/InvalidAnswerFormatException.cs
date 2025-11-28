using SurveyBot.Core.Entities;

namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when an answer format is invalid for the question type.
/// </summary>
public class InvalidAnswerFormatException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidAnswerFormatException"/> class.
    /// </summary>
    /// <param name="questionId">The ID of the question.</param>
    /// <param name="questionType">The type of the question.</param>
    /// <param name="reason">The reason why the answer format is invalid.</param>
    public InvalidAnswerFormatException(int questionId, QuestionType questionType, string reason)
        : base($"Invalid answer format for question {questionId} of type {questionType}: {reason}")
    {
        QuestionId = questionId;
        QuestionType = questionType;
        Reason = reason;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidAnswerFormatException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidAnswerFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidAnswerFormatException"/> class.
    /// </summary>
    /// <param name="questionId">The ID of the question.</param>
    /// <param name="questionType">The type of the question.</param>
    /// <param name="reason">The reason why the answer format is invalid.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvalidAnswerFormatException(int questionId, QuestionType questionType, string reason, Exception innerException)
        : base($"Invalid answer format for question {questionId} of type {questionType}: {reason}", innerException)
    {
        QuestionId = questionId;
        QuestionType = questionType;
        Reason = reason;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidAnswerFormatException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvalidAnswerFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the ID of the question.
    /// </summary>
    public int? QuestionId { get; }

    /// <summary>
    /// Gets the type of the question.
    /// </summary>
    public QuestionType? QuestionType { get; }

    /// <summary>
    /// Gets the reason why the answer format is invalid.
    /// </summary>
    public string? Reason { get; }
}
