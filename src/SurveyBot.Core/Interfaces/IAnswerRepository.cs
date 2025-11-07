using SurveyBot.Core.Entities;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Repository interface for Answer entity with specific query methods.
/// </summary>
public interface IAnswerRepository : IRepository<Answer>
{
    /// <summary>
    /// Gets all answers for a specific response.
    /// </summary>
    /// <param name="responseId">The response ID.</param>
    /// <returns>A collection of answers for the response.</returns>
    Task<IEnumerable<Answer>> GetByResponseIdAsync(int responseId);

    /// <summary>
    /// Gets all answers for a specific question across all responses.
    /// </summary>
    /// <param name="questionId">The question ID.</param>
    /// <returns>A collection of answers for the question.</returns>
    Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId);

    /// <summary>
    /// Gets a specific answer for a response and question combination.
    /// </summary>
    /// <param name="responseId">The response ID.</param>
    /// <param name="questionId">The question ID.</param>
    /// <returns>The answer if found, otherwise null.</returns>
    Task<Answer?> GetByResponseAndQuestionAsync(int responseId, int questionId);

    /// <summary>
    /// Creates multiple answers in a batch operation.
    /// </summary>
    /// <param name="answers">The collection of answers to create.</param>
    /// <returns>The created answers.</returns>
    Task<IEnumerable<Answer>> CreateBatchAsync(IEnumerable<Answer> answers);

    /// <summary>
    /// Deletes all answers for a specific response.
    /// </summary>
    /// <param name="responseId">The response ID.</param>
    /// <returns>The number of answers deleted.</returns>
    Task<int> DeleteByResponseIdAsync(int responseId);

    /// <summary>
    /// Deletes all answers for a specific question.
    /// </summary>
    /// <param name="questionId">The question ID.</param>
    /// <returns>The number of answers deleted.</returns>
    Task<int> DeleteByQuestionIdAsync(int questionId);

    /// <summary>
    /// Gets the count of answers for a specific question.
    /// </summary>
    /// <param name="questionId">The question ID.</param>
    /// <returns>The count of answers.</returns>
    Task<int> GetCountByQuestionIdAsync(int questionId);

    /// <summary>
    /// Checks if a response has an answer for a specific question.
    /// </summary>
    /// <param name="responseId">The response ID.</param>
    /// <param name="questionId">The question ID.</param>
    /// <returns>True if an answer exists, otherwise false.</returns>
    Task<bool> HasAnswerAsync(int responseId, int questionId);
}
