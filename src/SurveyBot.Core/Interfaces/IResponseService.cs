using SurveyBot.Core.DTOs.Common;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Models;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Service interface for managing survey responses and answers.
/// </summary>
public interface IResponseService
{
    /// <summary>
    /// Starts a new response for a survey.
    /// Creates a Response record with StartedAt timestamp.
    /// </summary>
    /// <param name="surveyId">The ID of the survey to respond to.</param>
    /// <param name="telegramUserId">The Telegram ID of the respondent.</param>
    /// <param name="username">The Telegram username of the respondent (optional).</param>
    /// <param name="firstName">The first name of the respondent (optional).</param>
    /// <returns>The created response DTO.</returns>
    /// <exception cref="Exceptions.SurveyNotFoundException">Thrown when the survey is not found.</exception>
    /// <exception cref="Exceptions.DuplicateResponseException">Thrown when the user has already completed the survey.</exception>
    /// <exception cref="Exceptions.SurveyOperationException">Thrown when the survey is not active.</exception>
    Task<ResponseDto> StartResponseAsync(int surveyId, long telegramUserId, string? username = null, string? firstName = null);

    /// <summary>
    /// Saves an individual answer to a question within a response.
    /// Validates the answer format based on the question type.
    /// </summary>
    /// <param name="responseId">The ID of the response.</param>
    /// <param name="questionId">The ID of the question being answered.</param>
    /// <param name="answerText">The text answer (for Text questions).</param>
    /// <param name="selectedOptions">The selected options (for SingleChoice/MultipleChoice questions).</param>
    /// <param name="ratingValue">The rating value (for Rating questions).</param>
    /// <param name="userId">The ID of the user making the request (for authorization).</param>
    /// <param name="answerJson">The JSON answer (for Location questions).</param>
    /// <returns>The updated response DTO.</returns>
    /// <exception cref="Exceptions.ResponseNotFoundException">Thrown when the response is not found.</exception>
    /// <exception cref="Exceptions.QuestionNotFoundException">Thrown when the question is not found.</exception>
    /// <exception cref="Exceptions.InvalidAnswerFormatException">Thrown when the answer format is invalid.</exception>
    /// <exception cref="Exceptions.UnauthorizedAccessException">Thrown when the user is not authorized.</exception>
    Task<ResponseDto> SaveAnswerAsync(
        int responseId,
        int questionId,
        string? answerText = null,
        List<string>? selectedOptions = null,
        int? ratingValue = null,
        int? userId = null,
        string? answerJson = null);

    /// <summary>
    /// Marks a response as completed with the current timestamp.
    /// </summary>
    /// <param name="responseId">The ID of the response to complete.</param>
    /// <param name="userId">The ID of the user making the request (for authorization).</param>
    /// <returns>The completed response DTO.</returns>
    /// <exception cref="Exceptions.ResponseNotFoundException">Thrown when the response is not found.</exception>
    /// <exception cref="Exceptions.UnauthorizedAccessException">Thrown when the user is not authorized.</exception>
    Task<ResponseDto> CompleteResponseAsync(int responseId, int? userId = null);

    /// <summary>
    /// Gets a response by ID with all its answers.
    /// </summary>
    /// <param name="responseId">The ID of the response.</param>
    /// <param name="userId">The ID of the user making the request (for authorization).</param>
    /// <returns>The response DTO with all answers.</returns>
    /// <exception cref="Exceptions.ResponseNotFoundException">Thrown when the response is not found.</exception>
    /// <exception cref="Exceptions.UnauthorizedAccessException">Thrown when the user is not authorized.</exception>
    Task<ResponseDto> GetResponseAsync(int responseId, int? userId = null);

    /// <summary>
    /// Gets all responses for a specific survey with pagination and filtering.
    /// Only accessible by the survey creator.
    /// </summary>
    /// <param name="surveyId">The ID of the survey.</param>
    /// <param name="userId">The ID of the user making the request (must be survey creator).</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="isCompleteFilter">Filter by completion status (optional).</param>
    /// <param name="startDate">Filter by start date (optional).</param>
    /// <param name="endDate">Filter by end date (optional).</param>
    /// <returns>A paged result of response DTOs.</returns>
    /// <exception cref="Exceptions.SurveyNotFoundException">Thrown when the survey is not found.</exception>
    /// <exception cref="Exceptions.UnauthorizedAccessException">Thrown when the user is not the survey creator.</exception>
    Task<PagedResultDto<ResponseDto>> GetSurveyResponsesAsync(
        int surveyId,
        int userId,
        int pageNumber = 1,
        int pageSize = 20,
        bool? isCompleteFilter = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Validates an answer format based on the question type.
    /// </summary>
    /// <param name="questionId">The ID of the question.</param>
    /// <param name="answerText">The text answer (for Text questions).</param>
    /// <param name="selectedOptions">The selected options (for SingleChoice/MultipleChoice questions).</param>
    /// <param name="ratingValue">The rating value (for Rating questions).</param>
    /// <param name="answerJson">The JSON answer (for Location questions).</param>
    /// <returns>A validation result indicating success or failure with error message.</returns>
    /// <exception cref="Exceptions.QuestionNotFoundException">Thrown when the question is not found.</exception>
    Task<ValidationResult> ValidateAnswerFormatAsync(
        int questionId,
        string? answerText = null,
        List<string>? selectedOptions = null,
        int? ratingValue = null,
        string? answerJson = null);

    /// <summary>
    /// Resumes an incomplete response for a user and survey.
    /// Returns the existing incomplete response if found, or starts a new one.
    /// </summary>
    /// <param name="surveyId">The ID of the survey.</param>
    /// <param name="telegramUserId">The Telegram ID of the respondent.</param>
    /// <param name="username">The Telegram username of the respondent (optional).</param>
    /// <param name="firstName">The first name of the respondent (optional).</param>
    /// <returns>The incomplete response DTO or a newly created response.</returns>
    /// <exception cref="Exceptions.SurveyNotFoundException">Thrown when the survey is not found.</exception>
    /// <exception cref="Exceptions.DuplicateResponseException">Thrown when the user has already completed the survey.</exception>
    Task<ResponseDto> ResumeResponseAsync(int surveyId, long telegramUserId, string? username = null, string? firstName = null);

    /// <summary>
    /// Deletes a response and all its answers.
    /// Only accessible by the survey creator.
    /// </summary>
    /// <param name="responseId">The ID of the response to delete.</param>
    /// <param name="userId">The ID of the user making the request (must be survey creator).</param>
    /// <exception cref="Exceptions.ResponseNotFoundException">Thrown when the response is not found.</exception>
    /// <exception cref="Exceptions.UnauthorizedAccessException">Thrown when the user is not the survey creator.</exception>
    Task DeleteResponseAsync(int responseId, int userId);

    /// <summary>
    /// Gets the count of completed responses for a survey.
    /// </summary>
    /// <param name="surveyId">The ID of the survey.</param>
    /// <returns>The count of completed responses.</returns>
    Task<int> GetCompletedResponseCountAsync(int surveyId);

    /// <summary>
    /// Records a question as visited in a response (for cycle prevention).
    /// </summary>
    /// <param name="responseId">The ID of the response.</param>
    /// <param name="questionId">The ID of the visited question.</param>
    /// <exception cref="Exceptions.ResponseNotFoundException">Thrown when the response is not found.</exception>
    Task RecordVisitedQuestionAsync(int responseId, int questionId);

    /// <summary>
    /// Gets the next question ID in the survey flow for a response.
    /// </summary>
    /// <param name="responseId">The ID of the response.</param>
    /// <returns>The next question ID, or null if survey is complete.</returns>
    /// <exception cref="Exceptions.ResponseNotFoundException">Thrown when the response is not found.</exception>
    Task<int?> GetNextQuestionAsync(int responseId);
}
