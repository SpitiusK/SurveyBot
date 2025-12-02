using System.Threading.Tasks;
using SurveyBot.Bot.Models;

namespace SurveyBot.Bot.Interfaces;

/// <summary>
/// Manages conversation state for Telegram bot users.
/// Tracks survey progress, questions, and session timeouts.
/// </summary>
public interface IConversationStateManager
{
    #region State Access

    /// <summary>
    /// Gets the conversation state for a user
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>ConversationState or null if not found or expired</returns>
    Task<ConversationState> GetStateAsync(long userId);

    /// <summary>
    /// Sets or updates conversation state for a user
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <param name="state">State to set</param>
    Task SetStateAsync(long userId, ConversationState state);

    /// <summary>
    /// Removes conversation state for a user
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    Task ClearStateAsync(long userId);

    /// <summary>
    /// Checks if user has active conversation state
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>True if active and not expired</returns>
    Task<bool> HasActiveStateAsync(long userId);

    #endregion

    #region State Transitions

    /// <summary>
    /// Attempts to transition state to new state with validation
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <param name="targetState">Target state to transition to</param>
    /// <returns>True if transition succeeded</returns>
    Task<bool> TryTransitionAsync(long userId, ConversationStateType targetState);

    #endregion

    #region Survey Operations

    /// <summary>
    /// Initializes survey state when user starts a survey
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <param name="surveyId">Survey ID</param>
    /// <param name="responseId">Response record ID</param>
    /// <param name="totalQuestions">Total number of questions in survey</param>
    /// <param name="surveyVersion">Current version of the survey (for version mismatch detection)</param>
    /// <returns>True if successful</returns>
    Task<bool> StartSurveyAsync(long userId, int surveyId, int responseId, int totalQuestions, int surveyVersion = 1);

    /// <summary>
    /// Records an answer for current question
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <param name="questionIndex">Index of question being answered</param>
    /// <param name="answerJson">JSON-serialized answer</param>
    /// <returns>True if successful</returns>
    Task<bool> AnswerQuestionAsync(long userId, int questionIndex, string answerJson);

    /// <summary>
    /// Moves to next question
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>True if successful (false if already at last question)</returns>
    Task<bool> NextQuestionAsync(long userId);

    /// <summary>
    /// Moves to previous question
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>True if successful (false if at first question)</returns>
    Task<bool> PreviousQuestionAsync(long userId);

    /// <summary>
    /// Skips current question (only for optional questions)
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <param name="isRequired">Whether question is required</param>
    /// <returns>True if successful (false if question is required)</returns>
    Task<bool> SkipQuestionAsync(long userId, bool isRequired);

    /// <summary>
    /// Completes the survey
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>True if successful</returns>
    Task<bool> CompleteSurveyAsync(long userId);

    /// <summary>
    /// Cancels the current survey
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>True if successful</returns>
    Task<bool> CancelSurveyAsync(long userId);

    #endregion

    #region Utilities

    /// <summary>
    /// Gets current question index for user
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>Current question index (0-based) or null</returns>
    Task<int?> GetCurrentQuestionIndexAsync(long userId);

    /// <summary>
    /// Gets current survey ID for user
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>Current survey ID or null</returns>
    Task<int?> GetCurrentSurveyIdAsync(long userId);

    /// <summary>
    /// Gets current response ID for user
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>Current response ID or null</returns>
    Task<int?> GetCurrentResponseIdAsync(long userId);

    /// <summary>
    /// Gets the survey version that was active when the conversation started.
    /// Used to detect if survey was modified during an active session.
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>Survey version or null if no active survey</returns>
    Task<int?> GetCurrentSurveyVersionAsync(long userId);

    /// <summary>
    /// Gets progress percentage through survey
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>Progress percentage (0-100)</returns>
    Task<float> GetProgressPercentAsync(long userId);

    /// <summary>
    /// Gets number of questions answered
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>Number of answered questions</returns>
    Task<int> GetAnsweredCountAsync(long userId);

    /// <summary>
    /// Gets total number of questions
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>Total questions or null</returns>
    Task<int?> GetTotalQuestionsAsync(long userId);

    /// <summary>
    /// Checks if all questions are answered
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>True if all answered</returns>
    Task<bool> IsAllAnsweredAsync(long userId);

    /// <summary>
    /// Gets cached answer for question
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <param name="questionIndex">Question index</param>
    /// <returns>Cached answer JSON or null</returns>
    Task<string> GetCachedAnswerAsync(long userId, int questionIndex);

    #endregion
}
