using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Core.ValueObjects.Answers;

namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents an individual answer to a question within a response.
/// Follows DDD principles with private setters and factory methods.
/// </summary>
public class Answer
{
    /// <summary>
    /// Gets the answer ID.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Gets the ID of the response this answer belongs to.
    /// </summary>
    [Required]
    public int ResponseId { get; private set; }

    /// <summary>
    /// Gets the ID of the question this answer is for.
    /// </summary>
    [Required]
    public int QuestionId { get; private set; }

    /// <summary>
    /// Gets the text answer for text-based questions.
    /// </summary>
    public string? AnswerText { get; private set; }

    /// <summary>
    /// Gets the JSON answer for complex question types (multiple choice, rating, etc.).
    /// Stored as JSONB in PostgreSQL for efficient querying.
    /// DEPRECATED: Use Value property instead. This property is kept for backward compatibility.
    /// </summary>
    public string? AnswerJson { get; private set; }

    /// <summary>
    /// Gets the polymorphic answer value object.
    /// This is the preferred way to access answer content with type safety.
    /// Stored as JSONB in answer_value_json column.
    /// </summary>
    public AnswerValue? Value { get; private set; }

    /// <summary>
    /// Gets the timestamp when the answer was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Conditional flow navigation

    /// <summary>
    /// Gets the navigation behavior after this answer.
    /// Determines where to go next in the survey flow using type-safe value object.
    /// </summary>
    public NextQuestionDeterminant Next { get; private set; } = NextQuestionDeterminant.End();

    // Navigation properties

    /// <summary>
    /// Gets the response this answer belongs to.
    /// </summary>
    public Response Response { get; private set; } = null!;

    /// <summary>
    /// Gets the question this answer is for.
    /// </summary>
    public Question Question { get; private set; } = null!;

    #region Constructors

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// Use factory methods for application code.
    /// </summary>
    private Answer()
    {
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new answer with a type-safe value object.
    /// This is the preferred method for creating answers with full type safety.
    /// </summary>
    /// <param name="responseId">ID of the response this answer belongs to (must be positive)</param>
    /// <param name="questionId">ID of the question being answered (must be positive)</param>
    /// <param name="value">The type-safe answer value object</param>
    /// <param name="next">Navigation behavior after this answer (defaults to end survey)</param>
    /// <returns>New answer instance with value object</returns>
    /// <exception cref="ArgumentException">If responseId or questionId is not positive</exception>
    /// <exception cref="ArgumentNullException">If value is null</exception>
    public static Answer CreateWithValue(
        int responseId,
        int questionId,
        AnswerValue value,
        NextQuestionDeterminant? next = null)
    {
        if (responseId <= 0)
            throw new ArgumentException("Response ID must be positive", nameof(responseId));
        if (questionId <= 0)
            throw new ArgumentException("Question ID must be positive", nameof(questionId));
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Answer value cannot be null");

        var answer = new Answer
        {
            ResponseId = responseId,
            QuestionId = questionId,
            Value = value,
            // Also set legacy properties for backward compatibility
            AnswerText = value is TextAnswerValue textValue ? textValue.Text : null,
            AnswerJson = value is not TextAnswerValue ? value.ToJson() : null,
            CreatedAt = DateTime.UtcNow,
            Next = next ?? NextQuestionDeterminant.End()
        };

        return answer;
    }

    /// <summary>
    /// Creates a new answer with text content (for Text questions).
    /// </summary>
    /// <param name="responseId">ID of the response this answer belongs to (must be positive)</param>
    /// <param name="questionId">ID of the question being answered (must be positive)</param>
    /// <param name="answerText">The text answer</param>
    /// <param name="next">Navigation behavior after this answer (defaults to end survey)</param>
    /// <returns>New text answer instance</returns>
    /// <exception cref="ArgumentException">If responseId or questionId is not positive</exception>
    public static Answer CreateTextAnswer(
        int responseId,
        int questionId,
        string? answerText,
        NextQuestionDeterminant? next = null)
    {
        if (responseId <= 0)
            throw new ArgumentException("Response ID must be positive", nameof(responseId));
        if (questionId <= 0)
            throw new ArgumentException("Question ID must be positive", nameof(questionId));

        // Try to create value object if text is provided
        AnswerValue? value = null;
        if (!string.IsNullOrWhiteSpace(answerText))
        {
            value = TextAnswerValue.CreateOptional(answerText);
        }

        var answer = new Answer
        {
            ResponseId = responseId,
            QuestionId = questionId,
            AnswerText = answerText?.Trim(),
            AnswerJson = null,
            Value = value,
            CreatedAt = DateTime.UtcNow,
            Next = next ?? NextQuestionDeterminant.End()
        };

        return answer;
    }

    /// <summary>
    /// Creates a new answer with JSON content (for SingleChoice, MultipleChoice, Rating questions).
    /// </summary>
    /// <param name="responseId">ID of the response this answer belongs to (must be positive)</param>
    /// <param name="questionId">ID of the question being answered (must be positive)</param>
    /// <param name="answerJson">The JSON answer (serialized options or rating)</param>
    /// <param name="next">Navigation behavior after this answer (defaults to end survey)</param>
    /// <returns>New JSON answer instance</returns>
    /// <exception cref="ArgumentException">If responseId or questionId is not positive</exception>
    public static Answer CreateJsonAnswer(
        int responseId,
        int questionId,
        string? answerJson,
        NextQuestionDeterminant? next = null)
    {
        if (responseId <= 0)
            throw new ArgumentException("Response ID must be positive", nameof(responseId));
        if (questionId <= 0)
            throw new ArgumentException("Question ID must be positive", nameof(questionId));

        var answer = new Answer
        {
            ResponseId = responseId,
            QuestionId = questionId,
            AnswerText = null,
            AnswerJson = answerJson,
            // Value will be set later via SetValue() when question type is known
            Value = null,
            CreatedAt = DateTime.UtcNow,
            Next = next ?? NextQuestionDeterminant.End()
        };

        return answer;
    }

    /// <summary>
    /// Creates a new answer with both text and JSON content (flexible).
    /// DEPRECATED: Use CreateWithValue() instead for type-safe answer handling.
    /// </summary>
    /// <param name="responseId">ID of the response this answer belongs to (must be positive)</param>
    /// <param name="questionId">ID of the question being answered (must be positive)</param>
    /// <param name="answerText">The text answer (optional)</param>
    /// <param name="answerJson">The JSON answer (optional)</param>
    /// <param name="next">Navigation behavior after this answer (defaults to end survey)</param>
    /// <returns>New answer instance</returns>
    /// <exception cref="ArgumentException">If responseId or questionId is not positive</exception>
    [Obsolete("Use CreateWithValue() instead. This method only creates AnswerValue for text questions. Will be removed in v2.0.0")]
    public static Answer Create(
        int responseId,
        int questionId,
        string? answerText = null,
        string? answerJson = null,
        NextQuestionDeterminant? next = null)
    {
        if (responseId <= 0)
            throw new ArgumentException("Response ID must be positive", nameof(responseId));
        if (questionId <= 0)
            throw new ArgumentException("Question ID must be positive", nameof(questionId));

        // Try to create value object if text is provided (for Text questions)
        AnswerValue? value = null;
        if (!string.IsNullOrWhiteSpace(answerText) && string.IsNullOrWhiteSpace(answerJson))
        {
            value = TextAnswerValue.CreateOptional(answerText);
        }

        var answer = new Answer
        {
            ResponseId = responseId,
            QuestionId = questionId,
            AnswerText = answerText?.Trim(),
            AnswerJson = answerJson,
            Value = value,
            CreatedAt = DateTime.UtcNow,
            Next = next ?? NextQuestionDeterminant.End()
        };

        return answer;
    }

    #endregion

    #region Domain Methods

    /// <summary>
    /// Updates the next question determinant for conditional flow.
    /// </summary>
    /// <param name="next">Navigation behavior after this answer</param>
    public void UpdateNext(NextQuestionDeterminant next)
    {
        Next = next ?? NextQuestionDeterminant.End();
    }

    /// <summary>
    /// Sets this answer to end the survey after being submitted.
    /// </summary>
    public void SetEndSurvey()
    {
        Next = NextQuestionDeterminant.End();
    }

    /// <summary>
    /// Sets this answer to navigate to a specific question.
    /// </summary>
    /// <param name="nextQuestionId">ID of the question to navigate to</param>
    public void SetNextQuestion(int nextQuestionId)
    {
        Next = NextQuestionDeterminant.ToQuestion(nextQuestionId);
    }

    /// <summary>
    /// Checks if this answer leads to the end of the survey.
    /// </summary>
    public bool LeadsToEndSurvey() => Next.Type == Enums.NextStepType.EndSurvey;

    /// <summary>
    /// Updates the answer value using a type-safe value object.
    /// Also updates legacy AnswerText/AnswerJson properties for backward compatibility.
    /// </summary>
    /// <param name="value">The new answer value object</param>
    /// <exception cref="ArgumentNullException">If value is null</exception>
    public void UpdateValue(AnswerValue value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Answer value cannot be null");

        Value = value;

        // Update legacy properties for backward compatibility
        if (value is TextAnswerValue textValue)
        {
            AnswerText = textValue.Text;
            AnswerJson = null;
        }
        else
        {
            AnswerText = null;
            AnswerJson = value.ToJson();
        }
    }

    /// <summary>
    /// Gets the answer value, converting from legacy format if necessary.
    /// </summary>
    /// <param name="questionType">The question type for parsing legacy JSON</param>
    /// <returns>The answer value object, or null if no value is available</returns>
    public AnswerValue? GetValue(QuestionType questionType)
    {
        // Return existing value if available
        if (Value != null)
            return Value;

        // Try to convert from legacy format
        return AnswerValueFactory.ConvertFromLegacy(AnswerText, AnswerJson, questionType);
    }

    #endregion

    #region Internal Methods (for testing and EF Core)

    /// <summary>
    /// Sets the answer ID. Used by tests and EF Core mapping.
    /// </summary>
    internal void SetId(int id)
    {
        Id = id;
    }

    /// <summary>
    /// Sets the response ID. Used by tests only.
    /// For normal use, prefer factory methods.
    /// </summary>
    internal void SetResponseId(int responseId)
    {
        if (responseId <= 0)
            throw new ArgumentException("Response ID must be positive", nameof(responseId));
        ResponseId = responseId;
    }

    /// <summary>
    /// Sets the question ID. Used by tests only.
    /// For normal use, prefer factory methods.
    /// </summary>
    internal void SetQuestionId(int questionId)
    {
        if (questionId <= 0)
            throw new ArgumentException("Question ID must be positive", nameof(questionId));
        QuestionId = questionId;
    }

    /// <summary>
    /// Sets the text answer. Used by tests only.
    /// For normal use, prefer factory methods.
    /// DEPRECATED: Use UpdateValue() instead for type-safe answer handling.
    /// </summary>
    [Obsolete("Use UpdateValue() instead. Will be removed in v2.0.0")]
    internal void SetAnswerText(string? answerText)
    {
        AnswerText = answerText;
    }

    /// <summary>
    /// Sets the JSON answer for complex question types. Used by tests only.
    /// For normal use, prefer factory methods.
    /// DEPRECATED: Use UpdateValue() instead for type-safe answer handling.
    /// </summary>
    [Obsolete("Use UpdateValue() instead. Will be removed in v2.0.0")]
    internal void SetAnswerJson(string? answerJson)
    {
        AnswerJson = answerJson;
    }

    /// <summary>
    /// Sets the answer value object. Used by tests and EF Core.
    /// For normal use, prefer factory methods or UpdateValue().
    /// </summary>
    internal void SetValue(AnswerValue? value)
    {
        Value = value;
    }

    /// <summary>
    /// Sets the created timestamp. Used by tests only.
    /// </summary>
    internal void SetCreatedAt(DateTime createdAt)
    {
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Sets the next question determinant for conditional flow. Used by tests only.
    /// For normal use, prefer factory methods or UpdateNext().
    /// </summary>
    internal void SetNext(NextQuestionDeterminant next)
    {
        Next = next ?? NextQuestionDeterminant.End();
    }

    /// <summary>
    /// Sets the response navigation property. Internal use only.
    /// </summary>
    internal void SetResponseInternal(Response response)
    {
        Response = response;
    }

    /// <summary>
    /// Sets the question navigation property. Internal use only.
    /// </summary>
    internal void SetQuestionInternal(Question question)
    {
        Question = question;
    }

    #endregion
}
