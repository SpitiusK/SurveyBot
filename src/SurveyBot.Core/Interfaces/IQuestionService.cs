using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Models;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Service interface for managing survey questions.
/// Handles business logic for question operations including creation, updates, deletion, and reordering.
/// </summary>
public interface IQuestionService
{
    /// <summary>
    /// Adds a new question to a survey.
    /// </summary>
    /// <param name="surveyId">The ID of the survey to add the question to.</param>
    /// <param name="userId">The ID of the user creating the question.</param>
    /// <param name="dto">The question creation data.</param>
    /// <returns>The created question.</returns>
    /// <exception cref="Exceptions.SurveyNotFoundException">Thrown when the survey is not found.</exception>
    /// <exception cref="Exceptions.UnauthorizedAccessException">Thrown when the user doesn't own the survey.</exception>
    /// <exception cref="Exceptions.QuestionValidationException">Thrown when validation fails.</exception>
    /// <exception cref="Exceptions.SurveyOperationException">Thrown when survey is completed.</exception>
    Task<QuestionDto> AddQuestionAsync(int surveyId, int userId, CreateQuestionDto dto);

    /// <summary>
    /// Updates an existing question.
    /// </summary>
    /// <param name="id">The ID of the question to update.</param>
    /// <param name="userId">The ID of the user updating the question.</param>
    /// <param name="dto">The updated question data.</param>
    /// <returns>The updated question.</returns>
    /// <exception cref="Exceptions.QuestionNotFoundException">Thrown when the question is not found.</exception>
    /// <exception cref="Exceptions.UnauthorizedAccessException">Thrown when the user doesn't own the survey.</exception>
    /// <exception cref="Exceptions.QuestionValidationException">Thrown when validation fails.</exception>
    /// <exception cref="Exceptions.SurveyOperationException">Thrown when the question cannot be modified (has responses).</exception>
    Task<QuestionDto> UpdateQuestionAsync(int id, int userId, UpdateQuestionDto dto);

    /// <summary>
    /// Deletes a question from a survey.
    /// </summary>
    /// <param name="id">The ID of the question to delete.</param>
    /// <param name="userId">The ID of the user deleting the question.</param>
    /// <returns>True if deleted successfully, false otherwise.</returns>
    /// <exception cref="Exceptions.QuestionNotFoundException">Thrown when the question is not found.</exception>
    /// <exception cref="Exceptions.UnauthorizedAccessException">Thrown when the user doesn't own the survey.</exception>
    /// <exception cref="Exceptions.SurveyOperationException">Thrown when the question cannot be deleted (has responses).</exception>
    Task<bool> DeleteQuestionAsync(int id, int userId);

    /// <summary>
    /// Gets a question by ID.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <returns>The question if found.</returns>
    /// <exception cref="Exceptions.QuestionNotFoundException">Thrown when the question is not found.</exception>
    Task<QuestionDto> GetQuestionAsync(int id);

    /// <summary>
    /// Gets all questions for a survey ordered by OrderIndex.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>List of questions ordered by OrderIndex.</returns>
    Task<List<QuestionDto>> GetBySurveyIdAsync(int surveyId);

    /// <summary>
    /// Reorders questions within a survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <param name="userId">The ID of the user reordering the questions.</param>
    /// <param name="questionIds">Array of question IDs in their new order.</param>
    /// <returns>True if reordered successfully.</returns>
    /// <exception cref="Exceptions.SurveyNotFoundException">Thrown when the survey is not found.</exception>
    /// <exception cref="Exceptions.UnauthorizedAccessException">Thrown when the user doesn't own the survey.</exception>
    /// <exception cref="Exceptions.QuestionValidationException">Thrown when question IDs are invalid.</exception>
    Task<bool> ReorderQuestionsAsync(int surveyId, int userId, int[] questionIds);

    /// <summary>
    /// Gets validation rules for a specific question type.
    /// </summary>
    /// <param name="type">The question type.</param>
    /// <returns>Dictionary of validation rules.</returns>
    Dictionary<string, object> GetQuestionTypeValidationAsync(QuestionType type);

    /// <summary>
    /// Validates question options based on question type.
    /// </summary>
    /// <param name="type">The question type.</param>
    /// <param name="options">The options to validate.</param>
    /// <returns>Validation result with any error messages.</returns>
    QuestionValidationResult ValidateQuestionOptionsAsync(QuestionType type, List<string>? options);
}

/// <summary>
/// Result of question options validation.
/// </summary>
public class QuestionValidationResult
{
    /// <summary>
    /// Gets or sets whether validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets validation error messages.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static QuestionValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    public static QuestionValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}
