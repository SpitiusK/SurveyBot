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
    /// </summary>
    public int? CurrentQuestionIndex { get; set; }

    /// <summary>
    /// Total number of questions in current survey
    /// </summary>
    public int? TotalQuestions { get; set; }

    /// <summary>
    /// Indices of questions already answered
    /// </summary>
    public List<int> AnsweredQuestionIndices { get; set; } = new();

    /// <summary>
    /// Gets or sets the indices of questions that were skipped (optional questions only).
    /// </summary>
    public List<int> SkippedQuestionIndices { get; set; } = new();

    /// <summary>
    /// Cached answers before saving to database
    /// Key: question index, Value: JSON answer string
    /// </summary>
    public Dictionary<int, string> CachedAnswers { get; set; } = new();

    /// <summary>
    /// Navigation history (stack of previous states)
    /// </summary>
    public Stack<ConversationStateType> StateHistory { get; set; } = new();

    /// <summary>
    /// Additional metadata (survey code, title, etc.)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Track visited questions in this conversation.
    /// Used for runtime cycle prevention in conditional question flows.
    /// Stores question IDs that have been displayed/answered.
    /// </summary>
    public List<int> VisitedQuestionIds { get; set; } = new();

    /// <summary>
    /// The version of the survey when the conversation started.
    /// Used to detect if the survey was modified during the session.
    /// If the current survey version differs, the conversation should be reset.
    /// </summary>
    public int? CurrentSurveyVersion { get; set; }

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

            return (AnsweredQuestionIndices.Count / (float)TotalQuestions.Value) * 100f;
        }
    }

    /// <summary>
    /// Gets number of questions answered
    /// </summary>
    public int AnsweredCount => AnsweredQuestionIndices.Count;

    /// <summary>
    /// Gets the count of skipped questions.
    /// </summary>
    public int SkippedCount => SkippedQuestionIndices.Count;

    /// <summary>
    /// Gets whether all questions have been addressed (answered or skipped).
    /// </summary>
    public bool IsAllAnswered => CurrentQuestionIndex.HasValue &&
                                 TotalQuestions.HasValue &&
                                 (AnsweredCount + SkippedCount) == TotalQuestions;

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
    /// Records that a question was answered
    /// </summary>
    public void MarkQuestionAnswered(int questionIndex)
    {
        if (!AnsweredQuestionIndices.Contains(questionIndex))
        {
            AnsweredQuestionIndices.Add(questionIndex);
            AnsweredQuestionIndices.Sort();
        }
    }

    /// <summary>
    /// Marks a question as skipped (for optional questions only).
    /// </summary>
    /// <param name="questionIndex">The 0-based index of the skipped question.</param>
    public void MarkQuestionSkipped(int questionIndex)
    {
        if (!SkippedQuestionIndices.Contains(questionIndex))
        {
            SkippedQuestionIndices.Add(questionIndex);
            SkippedQuestionIndices.Sort();
        }
    }

    /// <summary>
    /// Checks if a specific question has been skipped.
    /// </summary>
    /// <param name="questionIndex">The 0-based index of the question.</param>
    /// <returns>True if the question was skipped; otherwise, false.</returns>
    public bool IsQuestionSkipped(int questionIndex)
    {
        return SkippedQuestionIndices.Contains(questionIndex);
    }

    /// <summary>
    /// Caches an answer before saving to database
    /// </summary>
    public void CacheAnswer(int questionIndex, string answerJson)
    {
        CachedAnswers[questionIndex] = answerJson;
    }

    /// <summary>
    /// Gets cached answer for a question
    /// </summary>
    public string GetCachedAnswer(int questionIndex)
    {
        return CachedAnswers.TryGetValue(questionIndex, out var answer) ? answer : null;
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
    /// Check if a question has been visited in this conversation.
    /// Used for client-side cycle prevention in conditional flows.
    /// </summary>
    public bool HasVisitedQuestion(int questionId)
    {
        return VisitedQuestionIds.Contains(questionId);
    }

    /// <summary>
    /// Record a question as visited.
    /// Call this when displaying a question to the user.
    /// </summary>
    public void RecordVisitedQuestion(int questionId)
    {
        if (!VisitedQuestionIds.Contains(questionId))
        {
            VisitedQuestionIds.Add(questionId);
        }
    }

    /// <summary>
    /// Clear visited questions (when starting new survey).
    /// </summary>
    public void ClearVisitedQuestions()
    {
        VisitedQuestionIds.Clear();
    }

    /// <summary>
    /// Clears all survey-related data for new survey
    /// </summary>
    public void ClearSurveyData()
    {
        CurrentSurveyId = null;
        CurrentResponseId = null;
        CurrentQuestionIndex = null;
        TotalQuestions = null;
        CurrentSurveyVersion = null;
        AnsweredQuestionIndices.Clear();
        SkippedQuestionIndices.Clear();
        CachedAnswers.Clear();
        Metadata.Clear();
        ClearVisitedQuestions();
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
