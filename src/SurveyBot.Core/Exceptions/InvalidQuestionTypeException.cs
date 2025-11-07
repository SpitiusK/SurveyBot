using SurveyBot.Core.Entities;

namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when an unsupported or invalid question type is specified.
/// </summary>
public class InvalidQuestionTypeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidQuestionTypeException"/> class.
    /// </summary>
    /// <param name="questionType">The invalid question type.</param>
    public InvalidQuestionTypeException(QuestionType questionType)
        : base($"Question type '{questionType}' is not supported or invalid.")
    {
        QuestionType = questionType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidQuestionTypeException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidQuestionTypeException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidQuestionTypeException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvalidQuestionTypeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the invalid question type.
    /// </summary>
    public QuestionType? QuestionType { get; }
}
