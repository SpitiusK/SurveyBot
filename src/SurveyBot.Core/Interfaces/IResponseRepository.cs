using SurveyBot.Core.Entities;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Repository interface for Response entity with specific query methods.
/// </summary>
public interface IResponseRepository : IRepository<Response>
{
    /// <summary>
    /// Gets a response by ID with all related answers included.
    /// </summary>
    /// <param name="id">The response ID.</param>
    /// <returns>The response with answers if found, otherwise null.</returns>
    Task<Response?> GetByIdWithAnswersAsync(int id);

    /// <summary>
    /// Gets all responses for a specific survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>A collection of responses for the survey.</returns>
    Task<IEnumerable<Response>> GetBySurveyIdAsync(int surveyId);

    /// <summary>
    /// Gets all completed responses for a specific survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>A collection of completed responses.</returns>
    Task<IEnumerable<Response>> GetCompletedBySurveyIdAsync(int surveyId);

    /// <summary>
    /// Gets all responses from a specific Telegram user for a survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <param name="telegramId">The Telegram user ID.</param>
    /// <returns>A collection of responses from the user.</returns>
    Task<IEnumerable<Response>> GetByUserAndSurveyAsync(int surveyId, long telegramId);

    /// <summary>
    /// Gets an incomplete (in-progress) response for a user and survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <param name="telegramId">The Telegram user ID.</param>
    /// <returns>The incomplete response if found, otherwise null.</returns>
    Task<Response?> GetIncompleteResponseAsync(int surveyId, long telegramId);

    /// <summary>
    /// Checks if a user has already completed a survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <param name="telegramId">The Telegram user ID.</param>
    /// <returns>True if the user has completed the survey, otherwise false.</returns>
    Task<bool> HasUserCompletedSurveyAsync(int surveyId, long telegramId);

    /// <summary>
    /// Gets the count of completed responses for a survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>The count of completed responses.</returns>
    Task<int> GetCompletedCountAsync(int surveyId);

    /// <summary>
    /// Gets responses by date range for a specific survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <returns>A collection of responses submitted within the date range.</returns>
    Task<IEnumerable<Response>> GetByDateRangeAsync(int surveyId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Marks a response as complete.
    /// </summary>
    /// <param name="responseId">The response ID.</param>
    /// <returns>True if the response was marked as complete, otherwise false.</returns>
    Task<bool> MarkAsCompleteAsync(int responseId);

    /// <summary>
    /// Deletes all responses for a specific survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>The number of responses deleted.</returns>
    Task<int> DeleteBySurveyIdAsync(int surveyId);
}
