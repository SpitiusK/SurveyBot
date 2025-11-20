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
    /// <param name="firstQuestionId">Optional first question ID for branching support</param>
    /// <returns>True if successful</returns>
    Task<bool> StartSurveyAsync(long userId, int surveyId, int responseId, int totalQuestions, int? firstQuestionId = null);

    /// <summary>
    /// Records an answer for current question
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <param name="questionIndex">Index of question being answered</param>
    /// <param name="answerJson">JSON-serialized answer</param>
    /// <returns>True if successful</returns>
    Task<bool> AnswerQuestionAsync(long userId, int questionIndex, string answerJson);

    /// <summary>
    /// Moves to next question (index-based - DEPRECATED)
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>True if successful (false if already at last question)</returns>
    Task<bool> NextQuestionAsync(long userId);

    /// <summary>
    /// Moves to next question by ID (for branching support)
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <param name="nextQuestionId">ID of the next question to display</param>
    /// <param name="answerJson">Optional answer JSON for the current question</param>
    /// <returns>True if successful</returns>
    Task<bool> NextQuestionByIdAsync(long userId, int nextQuestionId, string answerJson = null);

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
    /// Skips a question by ID (for branching support)
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <param name="questionId">Question ID to skip</param>
    /// <returns>True if successful</returns>
    Task<bool> SkipQuestionByIdAsync(long userId, int questionId);

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

    /// <summary>
    /// Gets current question ID for user (for branching support)
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <returns>Current question ID or null</returns>
    Task<int?> GetCurrentQuestionIdAsync(long userId);

    /// <summary>
    /// Gets answer for a question by ID
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <param name="questionId">Question ID</param>
    /// <returns>Answer JSON or null</returns>
    Task<string> GetAnswerByIdAsync(long userId, int questionId);

    /// <summary>
    /// Checks if a question has been answered
    /// </summary>
    /// <param name="userId">Telegram user ID</param>
    /// <param name="questionId">Question ID</param>
    /// <returns>True if answered</returns>
    Task<bool> IsQuestionAnsweredAsync(long userId, int questionId);

    #endregion
}
