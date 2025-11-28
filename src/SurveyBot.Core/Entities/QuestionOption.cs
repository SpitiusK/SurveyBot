using System.ComponentModel.DataAnnotations;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Core.Enums;

namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents an individual option for a choice-based question.
/// Follows DDD principles with private setters and factory methods.
/// </summary>
public class QuestionOption : BaseEntity
{
    /// <summary>
    /// Gets the ID of the question this option belongs to.
    /// </summary>
    [Required]
    public int QuestionId { get; private set; }

    /// <summary>
    /// Gets the text of this option.
    /// </summary>
    [Required]
    public string Text { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the order index of this option within the question.
    /// </summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int OrderIndex { get; private set; }

    // Conditional flow configuration

    /// <summary>
    /// Gets the navigation behavior when this option is selected.
    /// For branching questions (SingleChoice, Rating), determines where to go if this option is selected.
    /// Ignored for non-branching questions.
    /// Set to null to maintain backward compatibility (no flow defined for this option).
    /// Use NextQuestionDeterminant.End() to end the survey or NextQuestionDeterminant.ToQuestion(id) to navigate.
    /// </summary>
    public NextQuestionDeterminant? Next { get; private set; }

    // Navigation properties

    /// <summary>
    /// Gets the question this option belongs to.
    /// </summary>
    public Question Question { get; private set; } = null!;

    #region Constructors

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// Use Create() factory method for application code.
    /// </summary>
    private QuestionOption() : base()
    {
    }

    /// <summary>
    /// Internal constructor for Infrastructure layer when creating options
    /// before the parent Question has been saved and has an ID.
    /// Use Create() factory method for normal application code.
    /// </summary>
    internal QuestionOption(bool forInfrastructure) : base()
    {
        // This constructor allows Infrastructure layer to create instances
        // when the QuestionId is not yet known (will be set via navigation property)
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new question option.
    /// </summary>
    /// <param name="questionId">ID of the question this option belongs to (must be positive)</param>
    /// <param name="text">Option text (required, max 200 characters)</param>
    /// <param name="orderIndex">Order index within the question (non-negative)</param>
    /// <param name="next">Optional navigation behavior when this option is selected</param>
    /// <returns>New question option instance with validated data</returns>
    /// <exception cref="ArgumentException">If questionId is not positive, text is empty/too long, or orderIndex is negative</exception>
    public static QuestionOption Create(
        int questionId,
        string text,
        int orderIndex,
        NextQuestionDeterminant? next = null)
    {
        if (questionId <= 0)
            throw new ArgumentException("Question ID must be positive", nameof(questionId));
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Option text cannot be empty", nameof(text));
        if (text.Length > 200)
            throw new ArgumentException("Option text cannot exceed 200 characters", nameof(text));
        if (orderIndex < 0)
            throw new ArgumentException("Order index cannot be negative", nameof(orderIndex));

        var option = new QuestionOption
        {
            QuestionId = questionId,
            Text = text.Trim(),
            OrderIndex = orderIndex,
            Next = next
        };

        return option;
    }

    /// <summary>
    /// Creates a new question option that ends the survey when selected.
    /// </summary>
    /// <param name="questionId">ID of the question this option belongs to</param>
    /// <param name="text">Option text</param>
    /// <param name="orderIndex">Order index within the question</param>
    /// <returns>New question option that ends the survey</returns>
    public static QuestionOption CreateWithEndSurvey(
        int questionId,
        string text,
        int orderIndex)
    {
        return Create(questionId, text, orderIndex, NextQuestionDeterminant.End());
    }

    /// <summary>
    /// Creates a new question option that navigates to a specific question when selected.
    /// </summary>
    /// <param name="questionId">ID of the question this option belongs to</param>
    /// <param name="text">Option text</param>
    /// <param name="orderIndex">Order index within the question</param>
    /// <param name="nextQuestionId">ID of the question to navigate to</param>
    /// <returns>New question option that navigates to the specified question</returns>
    public static QuestionOption CreateWithNextQuestion(
        int questionId,
        string text,
        int orderIndex,
        int nextQuestionId)
    {
        return Create(questionId, text, orderIndex, NextQuestionDeterminant.ToQuestion(nextQuestionId));
    }

    #endregion

    #region Domain Methods

    /// <summary>
    /// Updates the option text.
    /// </summary>
    /// <param name="text">New option text (required, max 200 characters)</param>
    /// <exception cref="ArgumentException">If text is empty or too long</exception>
    public void UpdateText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Option text cannot be empty", nameof(text));
        if (text.Length > 200)
            throw new ArgumentException("Option text cannot exceed 200 characters", nameof(text));
        Text = text.Trim();
        MarkAsModified();
    }

    /// <summary>
    /// Updates the order index.
    /// </summary>
    /// <param name="orderIndex">New order index (non-negative)</param>
    /// <exception cref="ArgumentException">If orderIndex is negative</exception>
    public void UpdateOrderIndex(int orderIndex)
    {
        if (orderIndex < 0)
            throw new ArgumentException("Order index cannot be negative", nameof(orderIndex));
        OrderIndex = orderIndex;
        MarkAsModified();
    }

    /// <summary>
    /// Updates the next question determinant for conditional flow.
    /// </summary>
    /// <param name="next">Navigation behavior when this option is selected (or null to clear)</param>
    public void UpdateNext(NextQuestionDeterminant? next)
    {
        Next = next;
        MarkAsModified();
    }

    /// <summary>
    /// Sets this option to end the survey when selected.
    /// </summary>
    public void SetEndSurvey()
    {
        Next = NextQuestionDeterminant.End();
        MarkAsModified();
    }

    /// <summary>
    /// Sets this option to navigate to a specific question when selected.
    /// </summary>
    /// <param name="nextQuestionId">ID of the question to navigate to</param>
    public void SetNextQuestion(int nextQuestionId)
    {
        Next = NextQuestionDeterminant.ToQuestion(nextQuestionId);
        MarkAsModified();
    }

    #endregion

    #region Internal Methods (for testing and EF Core)

    /// <summary>
    /// Sets the question ID. Used by tests only.
    /// For normal use, prefer Create() factory method.
    /// </summary>
    internal void SetQuestionId(int questionId)
    {
        if (questionId <= 0)
            throw new ArgumentException("Question ID must be positive", nameof(questionId));
        QuestionId = questionId;
    }

    /// <summary>
    /// Sets the option text. Used by tests only.
    /// For normal use, prefer Create() factory or UpdateText().
    /// </summary>
    internal void SetText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Option text cannot be empty", nameof(text));
        if (text.Length > 200)
            throw new ArgumentException("Option text cannot exceed 200 characters", nameof(text));
        Text = text;
    }

    /// <summary>
    /// Sets the order index. Used by tests only.
    /// For normal use, prefer Create() factory or UpdateOrderIndex().
    /// </summary>
    internal void SetOrderIndex(int orderIndex)
    {
        if (orderIndex < 0)
            throw new ArgumentException("Order index cannot be negative", nameof(orderIndex));
        OrderIndex = orderIndex;
    }

    /// <summary>
    /// Sets the next question determinant for conditional flow. Used by tests only.
    /// For normal use, prefer Create() factory or UpdateNext().
    /// </summary>
    internal void SetNext(NextQuestionDeterminant? next)
    {
        Next = next;
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
