using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Models;

namespace SurveyBot.Bot.Services;

/// <summary>
/// Manages conversation state for Telegram bot users.
/// Tracks survey progress, questions, and session timeouts.
/// Thread-safe in-memory storage with automatic expiration.
/// </summary>
public class ConversationStateManager : IConversationStateManager
{
    private const int EXPIRATION_MINUTES = 30;
    private const int CLEANUP_INTERVAL_MINUTES = 5;

    private readonly ILogger<ConversationStateManager> _logger;
    private readonly ConcurrentDictionary<long, ConversationState> _states;
    private readonly SemaphoreSlim _transitionLock;
    private Timer _cleanupTimer;

    public ConversationStateManager(ILogger<ConversationStateManager> logger)
    {
        _logger = logger;
        _states = new ConcurrentDictionary<long, ConversationState>();
        _transitionLock = new SemaphoreSlim(1, 1);

        // Start cleanup timer
        StartCleanupTimer();

        _logger.LogInformation("ConversationStateManager initialized");
    }

    #region State Access

    /// <summary>
    /// Gets the conversation state for a user
    /// </summary>
    public async Task<ConversationState> GetStateAsync(long userId)
    {
        if (_states.TryGetValue(userId, out var state))
        {
            // Check if expired
            if (state.IsExpired)
            {
                _logger.LogInformation($"State expired for user {userId}, removing");
                _states.TryRemove(userId, out _);
                return null;
            }

            // Update activity
            state.UpdateActivity();
            return state;
        }

        return null;
    }

    /// <summary>
    /// Sets or updates conversation state for a user
    /// </summary>
    public async Task SetStateAsync(long userId, ConversationState state)
    {
        state.UserId = userId;
        state.UpdateActivity();

        _states.AddOrUpdate(userId, state, (_, _) => state);

        _logger.LogDebug($"State set for user {userId}: {state.CurrentState}");
    }

    /// <summary>
    /// Removes conversation state for a user
    /// </summary>
    public async Task ClearStateAsync(long userId)
    {
        _states.TryRemove(userId, out _);
        _logger.LogInformation($"State cleared for user {userId}");
    }

    /// <summary>
    /// Checks if user has active conversation state
    /// </summary>
    public async Task<bool> HasActiveStateAsync(long userId)
    {
        var state = await GetStateAsync(userId);
        return state != null && !state.IsExpired;
    }

    #endregion

    #region State Transitions

    /// <summary>
    /// Attempts to transition state to new state with validation
    /// </summary>
    public async Task<bool> TryTransitionAsync(long userId, ConversationStateType targetState)
    {
        await _transitionLock.WaitAsync();
        try
        {
            var state = await GetStateAsync(userId);
            if (state == null)
            {
                _logger.LogWarning($"Cannot transition: no state for user {userId}");
                return false;
            }

            // Validate transition
            if (!IsValidTransition(state.CurrentState, targetState))
            {
                _logger.LogWarning(
                    $"Invalid transition for user {userId}: {state.CurrentState} -> {targetState}");
                return false;
            }

            state.TransitionTo(targetState);
            return true;
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    #endregion

    #region Survey Operations

    /// <summary>
    /// Initializes survey state when user starts a survey
    /// </summary>
    public async Task<bool> StartSurveyAsync(long userId, int surveyId, int responseId, int totalQuestions, int surveyVersion = 1)
    {
        await _transitionLock.WaitAsync();
        try
        {
            var state = await GetStateAsync(userId);
            if (state == null)
            {
                // Create new state
                state = new ConversationState
                {
                    UserId = userId,
                    CurrentState = ConversationStateType.InSurvey,
                    CurrentSurveyId = surveyId,
                    CurrentResponseId = responseId,
                    CurrentQuestionIndex = 0,
                    TotalQuestions = totalQuestions,
                    CurrentSurveyVersion = surveyVersion
                };
            }
            else
            {
                // Update existing state
                state.ClearSurveyData();
                state.CurrentSurveyId = surveyId;
                state.CurrentResponseId = responseId;
                state.CurrentQuestionIndex = 0;
                state.TotalQuestions = totalQuestions;
                state.CurrentSurveyVersion = surveyVersion;
                state.TransitionTo(ConversationStateType.InSurvey);
            }

            await SetStateAsync(userId, state);

            _logger.LogInformation(
                $"Survey started for user {userId}: survey={surveyId}, response={responseId}, questions={totalQuestions}, version={surveyVersion}");

            return true;
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    /// <summary>
    /// Records an answer for current question
    /// </summary>
    public async Task<bool> AnswerQuestionAsync(long userId, int questionIndex, string answerJson)
    {
        var state = await GetStateAsync(userId);
        if (state == null)
            return false;

        await _transitionLock.WaitAsync();
        try
        {
            // Validate current question index
            if (state.CurrentQuestionIndex != questionIndex)
            {
                _logger.LogWarning(
                    $"Answer question mismatch for user {userId}: expected={state.CurrentQuestionIndex}, got={questionIndex}");
                return false;
            }

            // Cache answer
            state.CacheAnswer(questionIndex, answerJson);
            state.MarkQuestionAnswered(questionIndex);
            state.UpdateActivity();

            _logger.LogDebug($"Answer recorded for user {userId} question {questionIndex}");

            return true;
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    /// <summary>
    /// Moves to next question
    /// </summary>
    public async Task<bool> NextQuestionAsync(long userId)
    {
        var state = await GetStateAsync(userId);
        if (state == null)
            return false;

        await _transitionLock.WaitAsync();
        try
        {
            if (!state.CurrentQuestionIndex.HasValue || !state.TotalQuestions.HasValue)
                return false;

            // Check if already at last question
            if (state.CurrentQuestionIndex >= state.TotalQuestions - 1)
            {
                _logger.LogWarning($"Already at last question for user {userId}");
                return false;
            }

            state.CurrentQuestionIndex++;
            state.UpdateActivity();

            _logger.LogDebug($"Moved to next question for user {userId}: {state.CurrentQuestionIndex}");

            return true;
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    /// <summary>
    /// Moves to previous question
    /// </summary>
    public async Task<bool> PreviousQuestionAsync(long userId)
    {
        var state = await GetStateAsync(userId);
        if (state == null)
            return false;

        await _transitionLock.WaitAsync();
        try
        {
            // Cannot go back from first question
            if (state.IsFirstQuestion)
            {
                _logger.LogWarning($"Cannot go back from first question for user {userId}");
                return false;
            }

            if (!state.CurrentQuestionIndex.HasValue)
                return false;

            state.CurrentQuestionIndex--;
            state.UpdateActivity();

            _logger.LogDebug($"Moved to previous question for user {userId}: {state.CurrentQuestionIndex}");

            return true;
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    /// <summary>
    /// Skips current question (only for optional questions)
    /// </summary>
    public async Task<bool> SkipQuestionAsync(long userId, bool isRequired)
    {
        var state = await GetStateAsync(userId);
        if (state == null)
        {
            _logger.LogWarning("No active conversation state for user {UserId}", userId);
            return false;
        }

        if (isRequired)
        {
            _logger.LogWarning("Cannot skip required question for user {UserId}", userId);
            return false;
        }

        // Record the skip action for tracking purposes
        if (state.CurrentQuestionIndex.HasValue)
        {
            state.MarkQuestionSkipped(state.CurrentQuestionIndex.Value);
            state.UpdateActivity();
            _logger.LogInformation("User {UserId} skipped optional question at index {QuestionIndex}",
                userId, state.CurrentQuestionIndex.Value);
        }

        return await NextQuestionAsync(userId);
    }

    /// <summary>
    /// Completes the survey
    /// </summary>
    public async Task<bool> CompleteSurveyAsync(long userId)
    {
        await _transitionLock.WaitAsync();
        try
        {
            var state = await GetStateAsync(userId);
            if (state == null)
                return false;

            state.TransitionTo(ConversationStateType.ResponseComplete);

            _logger.LogInformation($"Survey completed for user {userId}: response={state.CurrentResponseId}");

            return true;
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    /// <summary>
    /// Cancels the current survey
    /// </summary>
    public async Task<bool> CancelSurveyAsync(long userId)
    {
        await _transitionLock.WaitAsync();
        try
        {
            var state = await GetStateAsync(userId);
            if (state == null)
                return false;

            var responseId = state.CurrentResponseId;
            state.TransitionTo(ConversationStateType.Cancelled);
            state.ClearSurveyData();

            _logger.LogInformation($"Survey cancelled for user {userId}: response={responseId}");

            return true;
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    #endregion

    #region Session Management

    /// <summary>
    /// Checks if user's session has expired and handles cleanup.
    /// </summary>
    public async Task<bool> CheckSessionTimeoutAsync(long userId)
    {
        var state = await GetStateAsync(userId);

        if (state == null)
            return false; // No session to timeout

        if (state.IsExpired)
        {
            _logger.LogInformation(
                $"Session expired for user {userId}. Last activity: {state.LastActivityAt:yyyy-MM-dd HH:mm:ss UTC}, " +
                $"Age: {(DateTime.UtcNow - state.LastActivityAt).TotalMinutes:F1} minutes");

            // Mark state as expired
            state.TransitionTo(ConversationStateType.SessionExpired);

            return true; // Session has expired
        }

        return false; // Session is still active
    }

    /// <summary>
    /// Gets the remaining time before session expires.
    /// </summary>
    public async Task<TimeSpan?> GetSessionTimeRemainingAsync(long userId)
    {
        var state = await GetStateAsync(userId);

        if (state == null)
            return null;

        var sessionAge = DateTime.UtcNow - state.LastActivityAt;
        var remainingTime = TimeSpan.FromMinutes(EXPIRATION_MINUTES) - sessionAge;

        return remainingTime > TimeSpan.Zero ? remainingTime : TimeSpan.Zero;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Gets current question index for user
    /// </summary>
    public async Task<int?> GetCurrentQuestionIndexAsync(long userId)
    {
        var state = await GetStateAsync(userId);
        return state?.CurrentQuestionIndex;
    }

    /// <summary>
    /// Gets current survey ID for user
    /// </summary>
    public async Task<int?> GetCurrentSurveyIdAsync(long userId)
    {
        var state = await GetStateAsync(userId);
        return state?.CurrentSurveyId;
    }

    /// <summary>
    /// Gets current response ID for user
    /// </summary>
    public async Task<int?> GetCurrentResponseIdAsync(long userId)
    {
        var state = await GetStateAsync(userId);
        return state?.CurrentResponseId;
    }

    /// <summary>
    /// Gets the survey version that was active when the conversation started.
    /// Used to detect if survey was modified during an active session.
    /// </summary>
    public async Task<int?> GetCurrentSurveyVersionAsync(long userId)
    {
        var state = await GetStateAsync(userId);
        return state?.CurrentSurveyVersion;
    }

    /// <summary>
    /// Gets progress percentage through survey
    /// </summary>
    public async Task<float> GetProgressPercentAsync(long userId)
    {
        var state = await GetStateAsync(userId);
        return state?.ProgressPercent ?? 0f;
    }

    /// <summary>
    /// Gets number of questions answered
    /// </summary>
    public async Task<int> GetAnsweredCountAsync(long userId)
    {
        var state = await GetStateAsync(userId);
        return state?.AnsweredCount ?? 0;
    }

    /// <summary>
    /// Gets total number of questions
    /// </summary>
    public async Task<int?> GetTotalQuestionsAsync(long userId)
    {
        var state = await GetStateAsync(userId);
        return state?.TotalQuestions;
    }

    /// <summary>
    /// Checks if all questions are answered
    /// </summary>
    public async Task<bool> IsAllAnsweredAsync(long userId)
    {
        var state = await GetStateAsync(userId);
        return state?.IsAllAnswered ?? false;
    }

    /// <summary>
    /// Gets cached answer for question
    /// </summary>
    public async Task<string> GetCachedAnswerAsync(long userId, int questionIndex)
    {
        var state = await GetStateAsync(userId);
        return state?.GetCachedAnswer(questionIndex);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Validates if transition is allowed
    /// </summary>
    private bool IsValidTransition(ConversationStateType from, ConversationStateType to)
    {
        // Most transitions are valid except a few specific cases
        return from != to; // Cannot transition to same state
    }

    /// <summary>
    /// Starts background cleanup timer for expired states
    /// </summary>
    private void StartCleanupTimer()
    {
        _cleanupTimer = new Timer(
            async _ => await CleanupExpiredStatesAsync(),
            null,
            TimeSpan.FromMinutes(CLEANUP_INTERVAL_MINUTES),
            TimeSpan.FromMinutes(CLEANUP_INTERVAL_MINUTES));

        _logger.LogInformation(
            $"Cleanup timer started: interval={CLEANUP_INTERVAL_MINUTES} minutes, expiration={EXPIRATION_MINUTES} minutes");
    }

    /// <summary>
    /// Removes expired states from memory
    /// </summary>
    private async Task CleanupExpiredStatesAsync()
    {
        var expired = _states
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var userId in expired)
        {
            _states.TryRemove(userId, out _);
            _logger.LogInformation($"Expired state removed for user {userId}");
        }

        if (expired.Count > 0)
        {
            _logger.LogInformation($"Cleanup: removed {expired.Count} expired states. Active states: {_states.Count}");
        }
    }

    #endregion

    #region Diagnostics

    /// <summary>
    /// Gets count of active states (for monitoring)
    /// </summary>
    public int GetActiveStateCount() => _states.Count;

    /// <summary>
    /// Gets list of active user IDs (for diagnostics)
    /// </summary>
    public List<long> GetActiveUserIds() => _states.Keys.ToList();

    #endregion
}
