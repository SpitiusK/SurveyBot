using SurveyBot.Core.Entities;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Repository interface for Survey entity with specific query methods.
/// </summary>
public interface ISurveyRepository : IRepository<Survey>
{
    /// <summary>
    /// Gets a survey by ID with all related questions included.
    /// </summary>
    /// <param name="id">The survey ID.</param>
    /// <returns>The survey with questions if found, otherwise null.</returns>
    Task<Survey?> GetByIdWithQuestionsAsync(int id);

    /// <summary>
    /// Gets a survey by ID with all related questions and responses included.
    /// </summary>
    /// <param name="id">The survey ID.</param>
    /// <returns>The survey with questions and responses if found, otherwise null.</returns>
    Task<Survey?> GetByIdWithDetailsAsync(int id);

    /// <summary>
    /// Gets all surveys created by a specific user.
    /// </summary>
    /// <param name="creatorId">The creator user ID.</param>
    /// <returns>A collection of surveys created by the user.</returns>
    Task<IEnumerable<Survey>> GetByCreatorIdAsync(int creatorId);

    /// <summary>
    /// Gets all active surveys.
    /// </summary>
    /// <returns>A collection of active surveys.</returns>
    Task<IEnumerable<Survey>> GetActiveSurveysAsync();

    /// <summary>
    /// Toggles the IsActive status of a survey.
    /// </summary>
    /// <param name="id">The survey ID.</param>
    /// <returns>True if the status was toggled successfully, otherwise false.</returns>
    Task<bool> ToggleActiveStatusAsync(int id);

    /// <summary>
    /// Searches surveys by title (case-insensitive).
    /// </summary>
    /// <param name="searchTerm">The search term to match against survey titles.</param>
    /// <returns>A collection of surveys matching the search term.</returns>
    Task<IEnumerable<Survey>> SearchByTitleAsync(string searchTerm);

    /// <summary>
    /// Gets the total number of responses for a survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>The total number of responses.</returns>
    Task<int> GetResponseCountAsync(int surveyId);

    /// <summary>
    /// Checks if a survey has any responses.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>True if the survey has responses, otherwise false.</returns>
    Task<bool> HasResponsesAsync(int surveyId);
}
