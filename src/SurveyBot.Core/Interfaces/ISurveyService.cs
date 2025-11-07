using SurveyBot.Core.DTOs.Common;
using SurveyBot.Core.DTOs.Statistics;
using SurveyBot.Core.DTOs.Survey;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Service interface for survey business logic operations.
/// </summary>
public interface ISurveyService
{
    /// <summary>
    /// Creates a new survey for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user creating the survey.</param>
    /// <param name="dto">The survey creation data.</param>
    /// <returns>The created survey details.</returns>
    /// <exception cref="SurveyValidationException">Thrown when validation fails.</exception>
    Task<SurveyDto> CreateSurveyAsync(int userId, CreateSurveyDto dto);

    /// <summary>
    /// Updates an existing survey.
    /// </summary>
    /// <param name="surveyId">The ID of the survey to update.</param>
    /// <param name="userId">The ID of the user requesting the update.</param>
    /// <param name="dto">The updated survey data.</param>
    /// <returns>The updated survey details.</returns>
    /// <exception cref="SurveyNotFoundException">Thrown when the survey is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user doesn't own the survey.</exception>
    /// <exception cref="SurveyOperationException">Thrown when the survey cannot be modified (e.g., active survey with responses).</exception>
    Task<SurveyDto> UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto);

    /// <summary>
    /// Deletes a survey (soft delete if it has responses, hard delete otherwise).
    /// </summary>
    /// <param name="surveyId">The ID of the survey to delete.</param>
    /// <param name="userId">The ID of the user requesting the deletion.</param>
    /// <returns>True if the survey was deleted successfully.</returns>
    /// <exception cref="SurveyNotFoundException">Thrown when the survey is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user doesn't own the survey.</exception>
    Task<bool> DeleteSurveyAsync(int surveyId, int userId);

    /// <summary>
    /// Gets a survey by ID with all details including questions.
    /// </summary>
    /// <param name="surveyId">The ID of the survey.</param>
    /// <param name="userId">The ID of the user requesting the survey.</param>
    /// <returns>The survey details with questions.</returns>
    /// <exception cref="SurveyNotFoundException">Thrown when the survey is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user doesn't own the survey.</exception>
    Task<SurveyDto> GetSurveyByIdAsync(int surveyId, int userId);

    /// <summary>
    /// Gets all surveys for a specific user with pagination and filtering.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="query">Pagination and filter parameters.</param>
    /// <returns>Paginated list of surveys.</returns>
    Task<PagedResultDto<SurveyListDto>> GetAllSurveysAsync(int userId, PaginationQueryDto query);

    /// <summary>
    /// Activates a survey (makes it available for responses).
    /// </summary>
    /// <param name="surveyId">The ID of the survey to activate.</param>
    /// <param name="userId">The ID of the user requesting activation.</param>
    /// <returns>The updated survey details.</returns>
    /// <exception cref="SurveyNotFoundException">Thrown when the survey is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user doesn't own the survey.</exception>
    /// <exception cref="SurveyValidationException">Thrown when the survey has no questions.</exception>
    Task<SurveyDto> ActivateSurveyAsync(int surveyId, int userId);

    /// <summary>
    /// Deactivates a survey (stops accepting new responses).
    /// </summary>
    /// <param name="surveyId">The ID of the survey to deactivate.</param>
    /// <param name="userId">The ID of the user requesting deactivation.</param>
    /// <returns>The updated survey details.</returns>
    /// <exception cref="SurveyNotFoundException">Thrown when the survey is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user doesn't own the survey.</exception>
    Task<SurveyDto> DeactivateSurveyAsync(int surveyId, int userId);

    /// <summary>
    /// Gets comprehensive statistics for a survey including question-level data.
    /// </summary>
    /// <param name="surveyId">The ID of the survey.</param>
    /// <param name="userId">The ID of the user requesting statistics.</param>
    /// <returns>Survey statistics including question breakdowns.</returns>
    /// <exception cref="SurveyNotFoundException">Thrown when the survey is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user doesn't own the survey.</exception>
    Task<SurveyStatisticsDto> GetSurveyStatisticsAsync(int surveyId, int userId);

    /// <summary>
    /// Checks if a user owns a specific survey.
    /// </summary>
    /// <param name="surveyId">The ID of the survey.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>True if the user owns the survey.</returns>
    Task<bool> UserOwnsSurveyAsync(int surveyId, int userId);
}
