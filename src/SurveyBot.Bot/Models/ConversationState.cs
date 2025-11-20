using System;
using System.Collections.Generic;
using System.Linq;

namespace SurveyBot.Bot.Models;

/// <summary>
/// Represents the state of a user's conversation with the bot.
/// Tracks survey progress, current question, and session information.
/// </summary>
public class ConversationState
{
    /// <summary>
    /// Unique session identifier
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Telegram user ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Current state of the conversation
    /// </summary>
    public ConversationStateType CurrentState { get; set; } = ConversationStateType.Idle;

    /// <summary>
    /// When this state was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time user interacted with bot
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current survey being taken (if any)
    /// </summary>
    public int? CurrentSurveyId { get; set; }

    /// <summary>
    /// Current response record being filled (if any)
    /// </summary>
    public int? CurrentResponseId { get; set; }

    /// <summary>
    /// Current question index being displayed (0-based)
    /// DEPRECATED: Use CurrentQuestionId for branching support
    /// </summary>
    [Obsolete("Use CurrentQuestionId instead for branching support")]
    public int? CurrentQuestionIndex { get; set; }

    /// <summary>
    /// Total number of questions in current survey
    /// </summary>
    public int? TotalQuestions { get; set; }

    /// <summary>
    /// Current question ID being displayed (for branching support)
    /// </summary>
    public int? CurrentQuestionId { get; set; }

    /// <summary>
    /// List of question IDs that have been visited during this survey
    /// Tracks the path taken through branching questions
    /// </summary>
    public List<int> VisitedQuestionIds { get; set; } = new();

    /// <summary>
    /// List of question IDs that were skipped due to branching rules
    /// Questions not shown because branching conditions were not met
    /// </summary>
    public List<int> SkippedQuestionIds { get; set; } = new();

    /// <summary>
    /// Indices of questions already answered
    /// DEPRECATED: Use AnsweredQuestions for branching support
    /// </summary>
    [Obsolete("Use AnsweredQuestions instead for branching support")]
    public List<int> AnsweredQuestionIndices { get; set; } = new();

    /// <summary>
    /// Cached answers before saving to database
    /// Key: question index, Value: JSON answer string
    /// DEPRECATED: Use AnsweredQuestions for branching support
    /// </summary>
    [Obsolete("Use AnsweredQuestions instead for branching support")]
    public Dictionary<int, string> CachedAnswers { get; set; } = new();

    /// <summary>
    /// All answered questions during this survey
    /// Key: question ID, Value: JSON answer string
    /// </summary>
    public Dictionary<int, string> AnsweredQuestions { get; set; } = new();

    /// <summary>
    /// Navigation history (stack of previous states)
    /// </summary>
    public Stack<ConversationStateType> StateHistory { get; set; } = new();

    /// <summary>
    /// Additional metadata (survey code, title, etc.)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Checks if this state has expired (30 minutes of inactivity)
    /// </summary>
    public bool IsExpired => DateTime.UtcNow - LastActivityAt > TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets progress percentage through survey
    /// </summary>
    public float ProgressPercent
    {
        get
        {
            if (!TotalQuestions.HasValue || TotalQuestions == 0)
                return 0f;

            // Use new AnsweredQuestions if available, fallback to old AnsweredQuestionIndices
            var answeredCount = AnsweredQuestions.Count > 0
                ? AnsweredQuestions.Count
                : AnsweredQuestionIndices.Count;

            return (answeredCount / (float)TotalQuestions.Value) * 100f;
        }
    }

    /// <summary>
    /// Gets number of questions answered
    /// </summary>
    public int AnsweredCount => AnsweredQuestions.Count > 0
        ? AnsweredQuestions.Count
        : AnsweredQuestionIndices.Count;

    /// <summary>
    /// Checks if all questions have been answered
    /// </summary>
    public bool IsAllAnswered => CurrentQuestionIndex.HasValue &&
                                 TotalQuestions.HasValue &&
                                 AnsweredCount == TotalQuestions;

    /// <summary>
    /// Checks if currently on first question
    /// </summary>
    public bool IsFirstQuestion => CurrentQuestionIndex == 0;

    /// <summary>
    /// Checks if currently on last question
    /// </summary>
    public bool IsLastQuestion => CurrentQuestionIndex.HasValue &&
                                  TotalQuestions.HasValue &&
                                  CurrentQuestionIndex == (TotalQuestions - 1);

    /// <summary>
    /// Records that a question was answered (by index - DEPRECATED)
    /// </summary>
    [Obsolete("Use MarkQuestionAnsweredById instead")]
    public void MarkQuestionAnswered(int questionIndex)
    {
        if (!AnsweredQuestionIndices.Contains(questionIndex))
        {
            AnsweredQuestionIndices.Add(questionIndex);
            AnsweredQuestionIndices.Sort();
        }
    }

    /// <summary>
    /// Records that a question was answered (by ID - for branching support)
    /// </summary>
    public void MarkQuestionAnsweredById(int questionId, string answerJson)
    {
        AnsweredQuestions[questionId] = answerJson;

        if (!VisitedQuestionIds.Contains(questionId))
        {
            VisitedQuestionIds.Add(questionId);
        }
    }

    /// <summary>
    /// Marks a question as skipped due to branching
    /// </summary>
    public void MarkQuestionSkipped(int questionId)
    {
        if (!SkippedQuestionIds.Contains(questionId))
        {
            SkippedQuestionIds.Add(questionId);
        }
    }

    /// <summary>
    /// Caches an answer before saving to database (by index - DEPRECATED)
    /// </summary>
    [Obsolete("Use MarkQuestionAnsweredById instead")]
    public void CacheAnswer(int questionIndex, string answerJson)
    {
        CachedAnswers[questionIndex] = answerJson;
    }

    /// <summary>
    /// Gets cached answer for a question (by index - DEPRECATED)
    /// </summary>
    [Obsolete("Use GetAnswerById instead")]
    public string GetCachedAnswer(int questionIndex)
    {
        return CachedAnswers.TryGetValue(questionIndex, out var answer) ? answer : null;
    }

    /// <summary>
    /// Gets answer for a question by ID
    /// </summary>
    public string GetAnswerById(int questionId)
    {
        return AnsweredQuestions.TryGetValue(questionId, out var answer) ? answer : null;
    }

    /// <summary>
    /// Checks if a question has been answered
    /// </summary>
    public bool IsQuestionAnswered(int questionId)
    {
        return AnsweredQuestions.ContainsKey(questionId);
    }

    /// <summary>
    /// Updates last activity timestamp (call on every user interaction)
    /// </summary>
    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions to new state with history tracking
    /// </summary>
    public void TransitionTo(ConversationStateType newState)
    {
        StateHistory.Push(CurrentState);
        CurrentState = newState;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clears all survey-related data for new survey
    /// </summary>
    public void ClearSurveyData()
    {
        CurrentSurveyId = null;
        CurrentResponseId = null;
        CurrentQuestionIndex = null;
        CurrentQuestionId = null;
        TotalQuestions = null;
        AnsweredQuestionIndices.Clear();
        CachedAnswers.Clear();
        AnsweredQuestions.Clear();
        VisitedQuestionIds.Clear();
        SkippedQuestionIds.Clear();
        Metadata.Clear();
    }

    /// <summary>
    /// Resets entire state (for session expiration or user logout)
    /// </summary>
    public void Reset()
    {
        CurrentState = ConversationStateType.Idle;
        ClearSurveyData();
        StateHistory.Clear();
        LastActivityAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Enumeration of possible conversation states
/// </summary>
public enum ConversationStateType
{
    /// <summary>
    /// User is registered but not in any survey
    /// Available commands: /surveys, /mysurveys, /help
    /// </summary>
    Idle,

    /// <summary>
    /// User requested survey list and is selecting one
    /// Awaiting button click or /cancel
    /// </summary>
    WaitingSurveySelection,

    /// <summary>
    /// User has started a survey and is answering questions
    /// Can navigate with Back/Skip/Next or /cancel
    /// </summary>
    InSurvey,

    /// <summary>
    /// Bot displayed a question and awaits answer
    /// User should send text or click button
    /// </summary>
    AnsweringQuestion,

    /// <summary>
    /// User has completed all questions in survey
    /// Response is marked complete in database
    /// </summary>
    ResponseComplete,

    /// <summary>
    /// User's session expired due to inactivity (30 minutes)
    /// Must restart with /survey CODE
    /// </summary>
    SessionExpired,

    /// <summary>
    /// User cancelled the survey with /cancel
    /// Response deleted if incomplete
    /// </summary>
    Cancelled
}
