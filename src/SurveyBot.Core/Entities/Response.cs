using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents a user's response to a survey.
/// Follows DDD principles with private setters, encapsulated collections, and factory methods.
/// </summary>
public class Response
{
    private readonly List<Answer> _answers = new();

    /// <summary>
    /// Gets the response ID.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Gets the ID of the survey this response belongs to.
    /// </summary>
    [Required]
    public int SurveyId { get; private set; }

    /// <summary>
    /// Gets the Telegram ID of the user who submitted this response.
    /// Note: This is not a foreign key to allow anonymous responses.
    /// </summary>
    [Required]
    public long RespondentTelegramId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the response is complete.
    /// </summary>
    [Required]
    public bool IsComplete { get; private set; } = false;

    /// <summary>
    /// Gets the timestamp when the user started the response.
    /// </summary>
    public DateTime? StartedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the response was submitted.
    /// </summary>
    public DateTime? SubmittedAt { get; private set; }

    // Conditional flow tracking

    /// <summary>
    /// Gets the list of question IDs visited in this response.
    /// Used to prevent revisiting the same question (runtime cycle prevention).
    /// Stored as PostgreSQL JSON array.
    /// </summary>
    public List<int> VisitedQuestionIds { get; private set; } = new();

    // Navigation properties

    /// <summary>
    /// Gets the survey this response belongs to.
    /// </summary>
    public Survey Survey { get; private set; } = null!;

    /// <summary>
    /// Gets the collection of answers in this response.
    /// Returns a read-only view of the internal collection.
    /// </summary>
    public IReadOnlyCollection<Answer> Answers => _answers.AsReadOnly();

    #region Constructors

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// Use Start() or Create() factory methods for application code.
    /// </summary>
    private Response()
    {
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates and starts a new response to a survey.
    /// This is the primary factory method for starting a new survey response.
    /// </summary>
    /// <param name="surveyId">ID of the survey being responded to (must be positive)</param>
    /// <param name="respondentTelegramId">Telegram ID of the respondent (must be positive)</param>
    /// <returns>New response instance with StartedAt timestamp set</returns>
    /// <exception cref="ArgumentException">If surveyId or respondentTelegramId is not positive</exception>
    public static Response Start(int surveyId, long respondentTelegramId)
    {
        if (surveyId <= 0)
            throw new ArgumentException("Survey ID must be positive", nameof(surveyId));
        if (respondentTelegramId <= 0)
            throw new ArgumentException("Telegram ID must be positive", nameof(respondentTelegramId));

        var response = new Response
        {
            SurveyId = surveyId,
            RespondentTelegramId = respondentTelegramId,
            IsComplete = false,
            StartedAt = DateTime.UtcNow,
            VisitedQuestionIds = new List<int>()
        };

        return response;
    }

    /// <summary>
    /// Creates a new response with explicit timestamps (for testing or data import).
    /// </summary>
    /// <param name="surveyId">ID of the survey being responded to (must be positive)</param>
    /// <param name="respondentTelegramId">Telegram ID of the respondent (must be positive)</param>
    /// <param name="startedAt">When the response was started (optional)</param>
    /// <param name="isComplete">Whether the response is complete</param>
    /// <param name="submittedAt">When the response was submitted (optional, required if isComplete is true)</param>
    /// <returns>New response instance with specified state</returns>
    public static Response Create(
        int surveyId,
        long respondentTelegramId,
        DateTime? startedAt = null,
        bool isComplete = false,
        DateTime? submittedAt = null)
    {
        if (surveyId <= 0)
            throw new ArgumentException("Survey ID must be positive", nameof(surveyId));
        if (respondentTelegramId <= 0)
            throw new ArgumentException("Telegram ID must be positive", nameof(respondentTelegramId));

        var response = new Response
        {
            SurveyId = surveyId,
            RespondentTelegramId = respondentTelegramId,
            IsComplete = isComplete,
            StartedAt = startedAt,
            SubmittedAt = submittedAt,
            VisitedQuestionIds = new List<int>()
        };

        return response;
    }

    #endregion

    #region Domain Methods

    /// <summary>
    /// Marks the response as started with current timestamp.
    /// </summary>
    public void MarkAsStarted()
    {
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the response as complete with current timestamp.
    /// </summary>
    public void MarkAsComplete()
    {
        IsComplete = true;
        SubmittedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a question has been visited in this response.
    /// </summary>
    /// <param name="questionId">ID of the question to check</param>
    /// <returns>True if the question has been visited, false otherwise</returns>
    public bool HasVisitedQuestion(int questionId) =>
        VisitedQuestionIds.Contains(questionId);

    /// <summary>
    /// Records a question as visited in this response.
    /// Prevents the same question from being answered multiple times.
    /// </summary>
    /// <param name="questionId">ID of the question to record</param>
    public void RecordVisitedQuestion(int questionId)
    {
        if (!VisitedQuestionIds.Contains(questionId))
            VisitedQuestionIds.Add(questionId);
    }

    /// <summary>
    /// Gets the count of questions that have been visited/answered.
    /// </summary>
    public int GetAnsweredQuestionCount() => VisitedQuestionIds.Count;

    /// <summary>
    /// Validates that VisitedQuestionIds is consistent with Answers collection.
    /// For defensive programming - catches inconsistent state before completing response.
    /// </summary>
    /// <returns>True if invariants are valid, false otherwise with error message</returns>
    public (bool IsValid, string? ErrorMessage) ValidateTrackingConsistency()
    {
        if (Answers == null || !Answers.Any())
        {
            // No answers yet - invariant is trivially satisfied
            return (true, null);
        }

        var answeredQuestionIds = Answers.Select(a => a.QuestionId).Distinct().ToList();

        // Check if any answered questions are missing from visited list
        var missingFromVisited = answeredQuestionIds.Except(VisitedQuestionIds).ToList();

        if (missingFromVisited.Any())
        {
            return (false, $"Questions {string.Join(", ", missingFromVisited)} have answers but are not in VisitedQuestionIds");
        }

        return (true, null);
    }

    #endregion

    #region Internal Methods (for testing and EF Core)

    /// <summary>
    /// Sets the response ID. Used by tests and EF Core mapping.
    /// </summary>
    internal void SetId(int id)
    {
        Id = id;
    }

    /// <summary>
    /// Sets the survey ID. Used by tests only.
    /// For normal use, prefer Start() or Create() factory methods.
    /// </summary>
    internal void SetSurveyId(int surveyId)
    {
        if (surveyId <= 0)
            throw new ArgumentException("Survey ID must be positive", nameof(surveyId));
        SurveyId = surveyId;
    }

    /// <summary>
    /// Sets the respondent's Telegram ID. Used by tests only.
    /// For normal use, prefer Start() or Create() factory methods.
    /// </summary>
    internal void SetRespondentTelegramId(long telegramId)
    {
        if (telegramId <= 0)
            throw new ArgumentException("Telegram ID must be positive", nameof(telegramId));
        RespondentTelegramId = telegramId;
    }

    /// <summary>
    /// Sets the started timestamp. Used by tests only.
    /// For normal use, prefer Start() factory or MarkAsStarted().
    /// </summary>
    internal void SetStartedAt(DateTime? startedAt)
    {
        StartedAt = startedAt;
    }

    /// <summary>
    /// Sets the completion status. Used by tests only.
    /// For normal use, prefer MarkAsComplete().
    /// </summary>
    internal void SetIsComplete(bool isComplete)
    {
        IsComplete = isComplete;
    }

    /// <summary>
    /// Sets the submitted timestamp. Used by tests only.
    /// For normal use, prefer MarkAsComplete().
    /// </summary>
    internal void SetSubmittedAt(DateTime? submittedAt)
    {
        SubmittedAt = submittedAt;
    }

    /// <summary>
    /// Sets the visited question IDs. Used by tests and during database loading.
    /// </summary>
    internal void SetVisitedQuestionIds(List<int> visitedQuestionIds)
    {
        VisitedQuestionIds = visitedQuestionIds ?? new List<int>();
    }

    /// <summary>
    /// Adds an answer to the response's collection. Internal use only.
    /// </summary>
    internal void AddAnswerInternal(Answer answer)
    {
        _answers.Add(answer);
    }

    /// <summary>
    /// Sets the survey navigation property. Internal use only.
    /// </summary>
    internal void SetSurveyInternal(Survey survey)
    {
        Survey = survey;
    }

    #endregion
}
