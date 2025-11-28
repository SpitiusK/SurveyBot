using System.ComponentModel.DataAnnotations;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Core.Enums;

namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents a question within a survey.
/// Follows DDD principles with private setters, encapsulated collections, and factory methods.
/// </summary>
public class Question : BaseEntity
{
    private readonly List<Answer> _answers = new();
    private readonly List<QuestionOption> _options = new();

    /// <summary>
    /// Gets the ID of the survey this question belongs to.
    /// </summary>
    [Required]
    public int SurveyId { get; private set; }

    /// <summary>
    /// Gets the question text.
    /// </summary>
    [Required]
    public string QuestionText { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the question type.
    /// </summary>
    [Required]
    public QuestionType QuestionType { get; private set; }

    /// <summary>
    /// Gets the order index of the question within the survey (0-based).
    /// </summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int OrderIndex { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this question is required.
    /// </summary>
    [Required]
    public bool IsRequired { get; private set; } = true;

    /// <summary>
    /// Gets the JSON options for choice-based questions.
    /// Stored as JSONB in PostgreSQL for efficient querying.
    /// </summary>
    public string? OptionsJson { get; private set; }

    /// <summary>
    /// Gets the multimedia content metadata for this question.
    /// Stored as JSONB in PostgreSQL containing file information (type, path, size, etc.).
    /// Null for questions without multimedia content.
    /// </summary>
    public string? MediaContent { get; private set; }

    // Conditional flow configuration

    /// <summary>
    /// Gets a value indicating whether this question type supports conditional branching.
    /// Only SingleChoice and Rating questions support branching.
    /// This is a computed property and not persisted to the database.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool SupportsBranching =>
        QuestionType == QuestionType.SingleChoice || QuestionType == QuestionType.Rating;

    /// <summary>
    /// Gets the default navigation behavior for non-branching questions.
    /// For Text and MultipleChoice questions, all answers navigate according to this determinant.
    /// Ignored for branching questions (SingleChoice, Rating) which use option-specific navigation.
    /// Set to null to maintain backward compatibility (no default flow defined).
    /// Use NextQuestionDeterminant.End() to end the survey or NextQuestionDeterminant.ToQuestion(id) to navigate.
    /// </summary>
    public NextQuestionDeterminant? DefaultNext { get; private set; }

    // Navigation properties

    /// <summary>
    /// Gets the survey this question belongs to.
    /// </summary>
    public Survey Survey { get; private set; } = null!;

    /// <summary>
    /// Gets the collection of answers to this question across all responses.
    /// Returns a read-only view of the internal collection.
    /// </summary>
    public IReadOnlyCollection<Answer> Answers => _answers.AsReadOnly();

    /// <summary>
    /// Gets the collection of options for this question (if applicable).
    /// Returns a read-only view of the internal collection.
    /// </summary>
    public IReadOnlyCollection<QuestionOption> Options => _options.AsReadOnly();

    #region Constructors

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// Use Create() or CreateTextQuestion/CreateChoiceQuestion/CreateRatingQuestion factory methods for application code.
    /// </summary>
    private Question() : base()
    {
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new question with full configuration.
    /// </summary>
    /// <param name="surveyId">ID of the survey this question belongs to (must be positive)</param>
    /// <param name="questionText">The question text (required, non-empty)</param>
    /// <param name="questionType">The type of question</param>
    /// <param name="orderIndex">The order index within the survey (0-based, non-negative)</param>
    /// <param name="isRequired">Whether the question is required (default: true)</param>
    /// <param name="optionsJson">JSON options for choice-based questions (optional)</param>
    /// <param name="mediaContent">Media content metadata (optional)</param>
    /// <param name="defaultNext">Default next question determinant for conditional flow (optional)</param>
    /// <returns>New question instance with validated data</returns>
    /// <exception cref="ArgumentException">If surveyId is not positive, questionText is empty, or orderIndex is negative</exception>
    public static Question Create(
        int surveyId,
        string questionText,
        QuestionType questionType,
        int orderIndex,
        bool isRequired = true,
        string? optionsJson = null,
        string? mediaContent = null,
        NextQuestionDeterminant? defaultNext = null)
    {
        if (surveyId <= 0)
            throw new ArgumentException("Survey ID must be positive", nameof(surveyId));
        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("Question text cannot be empty", nameof(questionText));
        if (orderIndex < 0)
            throw new ArgumentException("Order index cannot be negative", nameof(orderIndex));

        var question = new Question
        {
            SurveyId = surveyId,
            QuestionText = questionText.Trim(),
            QuestionType = questionType,
            OrderIndex = orderIndex,
            IsRequired = isRequired,
            OptionsJson = optionsJson,
            MediaContent = mediaContent,
            DefaultNext = defaultNext
        };

        return question;
    }

    /// <summary>
    /// Creates a new text question (free-form response).
    /// </summary>
    /// <param name="surveyId">ID of the survey this question belongs to</param>
    /// <param name="questionText">The question text</param>
    /// <param name="orderIndex">The order index within the survey</param>
    /// <param name="isRequired">Whether the question is required (default: true)</param>
    /// <returns>New text question instance</returns>
    public static Question CreateTextQuestion(
        int surveyId,
        string questionText,
        int orderIndex,
        bool isRequired = true)
    {
        return Create(
            surveyId: surveyId,
            questionText: questionText,
            questionType: QuestionType.Text,
            orderIndex: orderIndex,
            isRequired: isRequired);
    }

    /// <summary>
    /// Creates a new single-choice question.
    /// </summary>
    /// <param name="surveyId">ID of the survey this question belongs to</param>
    /// <param name="questionText">The question text</param>
    /// <param name="orderIndex">The order index within the survey</param>
    /// <param name="optionsJson">JSON array of options (e.g., ["Option 1", "Option 2"])</param>
    /// <param name="isRequired">Whether the question is required (default: true)</param>
    /// <returns>New single-choice question instance</returns>
    public static Question CreateSingleChoiceQuestion(
        int surveyId,
        string questionText,
        int orderIndex,
        string optionsJson,
        bool isRequired = true)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
            throw new ArgumentException("Options JSON is required for choice questions", nameof(optionsJson));

        return Create(
            surveyId: surveyId,
            questionText: questionText,
            questionType: QuestionType.SingleChoice,
            orderIndex: orderIndex,
            isRequired: isRequired,
            optionsJson: optionsJson);
    }

    /// <summary>
    /// Creates a new multiple-choice question.
    /// </summary>
    /// <param name="surveyId">ID of the survey this question belongs to</param>
    /// <param name="questionText">The question text</param>
    /// <param name="orderIndex">The order index within the survey</param>
    /// <param name="optionsJson">JSON array of options</param>
    /// <param name="isRequired">Whether the question is required (default: true)</param>
    /// <returns>New multiple-choice question instance</returns>
    public static Question CreateMultipleChoiceQuestion(
        int surveyId,
        string questionText,
        int orderIndex,
        string optionsJson,
        bool isRequired = true)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
            throw new ArgumentException("Options JSON is required for choice questions", nameof(optionsJson));

        return Create(
            surveyId: surveyId,
            questionText: questionText,
            questionType: QuestionType.MultipleChoice,
            orderIndex: orderIndex,
            isRequired: isRequired,
            optionsJson: optionsJson);
    }

    /// <summary>
    /// Creates a new rating question (typically 1-5 scale).
    /// </summary>
    /// <param name="surveyId">ID of the survey this question belongs to</param>
    /// <param name="questionText">The question text</param>
    /// <param name="orderIndex">The order index within the survey</param>
    /// <param name="isRequired">Whether the question is required (default: true)</param>
    /// <returns>New rating question instance</returns>
    public static Question CreateRatingQuestion(
        int surveyId,
        string questionText,
        int orderIndex,
        bool isRequired = true)
    {
        return Create(
            surveyId: surveyId,
            questionText: questionText,
            questionType: QuestionType.Rating,
            orderIndex: orderIndex,
            isRequired: isRequired);
    }

    #endregion

    #region Domain Methods

    /// <summary>
    /// Updates the question text.
    /// </summary>
    /// <param name="questionText">New question text (required, non-empty)</param>
    /// <exception cref="ArgumentException">If questionText is empty</exception>
    public void UpdateText(string questionText)
    {
        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("Question text cannot be empty", nameof(questionText));
        QuestionText = questionText.Trim();
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
    /// Updates whether the question is required.
    /// </summary>
    /// <param name="isRequired">Whether the question is required</param>
    public void UpdateIsRequired(bool isRequired)
    {
        IsRequired = isRequired;
        MarkAsModified();
    }

    /// <summary>
    /// Updates the options JSON for choice-based questions.
    /// </summary>
    /// <param name="optionsJson">JSON array of options (or null to clear)</param>
    public void UpdateOptionsJson(string? optionsJson)
    {
        OptionsJson = optionsJson;
        MarkAsModified();
    }

    /// <summary>
    /// Attaches media content to this question.
    /// </summary>
    /// <param name="mediaContent">Media content metadata JSON (or null to clear)</param>
    public void AttachMedia(string? mediaContent)
    {
        MediaContent = mediaContent;
        MarkAsModified();
    }

    /// <summary>
    /// Updates the default next question determinant for conditional flow.
    /// </summary>
    /// <param name="defaultNext">Default next question determinant (or null to clear)</param>
    public void UpdateDefaultNext(NextQuestionDeterminant? defaultNext)
    {
        DefaultNext = defaultNext;
        MarkAsModified();
    }

    #endregion

    #region Internal Methods (for testing and EF Core)

    /// <summary>
    /// Sets the survey ID. Used by tests only.
    /// For normal use, prefer Create() factory method.
    /// </summary>
    internal void SetSurveyId(int surveyId)
    {
        if (surveyId <= 0)
            throw new ArgumentException("Survey ID must be positive", nameof(surveyId));
        SurveyId = surveyId;
    }

    /// <summary>
    /// Sets the question text. Used by tests only.
    /// For normal use, prefer Create() factory or UpdateText().
    /// </summary>
    internal void SetQuestionText(string questionText)
    {
        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("Question text cannot be empty", nameof(questionText));
        QuestionText = questionText;
    }

    /// <summary>
    /// Sets the question type. Used by tests only.
    /// For normal use, prefer Create() factory method.
    /// </summary>
    internal void SetQuestionType(QuestionType questionType)
    {
        QuestionType = questionType;
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
    /// Sets whether the question is required. Used by tests only.
    /// For normal use, prefer Create() factory or UpdateIsRequired().
    /// </summary>
    internal void SetIsRequired(bool isRequired)
    {
        IsRequired = isRequired;
    }

    /// <summary>
    /// Sets the options JSON for choice-based questions. Used by tests only.
    /// For normal use, prefer Create() factory or UpdateOptionsJson().
    /// </summary>
    internal void SetOptionsJson(string? optionsJson)
    {
        OptionsJson = optionsJson;
    }

    /// <summary>
    /// Sets the media content metadata. Used by tests only.
    /// For normal use, prefer AttachMedia().
    /// </summary>
    internal void SetMediaContent(string? mediaContent)
    {
        MediaContent = mediaContent;
    }

    /// <summary>
    /// Sets the default next question determinant for conditional flow. Used by tests only.
    /// For normal use, prefer UpdateDefaultNext().
    /// </summary>
    internal void SetDefaultNext(NextQuestionDeterminant? defaultNext)
    {
        DefaultNext = defaultNext;
    }

    /// <summary>
    /// Adds an answer to the question's collection. Internal use only.
    /// </summary>
    internal void AddAnswerInternal(Answer answer)
    {
        _answers.Add(answer);
    }

    /// <summary>
    /// Adds an option to the question's collection. Internal use only.
    /// </summary>
    internal void AddOptionInternal(QuestionOption option)
    {
        _options.Add(option);
    }

    /// <summary>
    /// Clears and replaces all options. Internal use only.
    /// </summary>
    internal void SetOptionsInternal(IEnumerable<QuestionOption> options)
    {
        _options.Clear();
        _options.AddRange(options);
    }

    /// <summary>
    /// Sets the survey navigation property. Internal use only.
    /// </summary>
    internal void SetSurveyInternal(Survey survey)
    {
        Survey = survey;
    }

    /// <summary>
    /// Sets the question navigation for an answer. Internal use only.
    /// </summary>
    internal void SetQuestionInternal(Answer answer)
    {
        // This method exists for test setup compatibility
        // It doesn't modify Question but allows setting up answer relationships
    }

    #endregion
}
