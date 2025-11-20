using SurveyBot.Core.Entities;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Repository interface for Question entity with specific query methods.
/// </summary>
public interface IQuestionRepository : IRepository<Question>
{
    /// <summary>
    /// Gets all questions for a specific survey ordered by OrderIndex.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>A collection of questions ordered by OrderIndex.</returns>
    Task<IEnumerable<Question>> GetBySurveyIdAsync(int surveyId);

    /// <summary>
    /// Gets a question by ID with all related answers included.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <returns>The question with answers if found, otherwise null.</returns>
    Task<Question?> GetByIdWithAnswersAsync(int id);

    /// <summary>
    /// Reorders questions within a survey.
    /// </summary>
    /// <param name="questionOrders">Dictionary mapping question IDs to their new OrderIndex values.</param>
    /// <returns>True if the reordering was successful, otherwise false.</returns>
    Task<bool> ReorderQuestionsAsync(Dictionary<int, int> questionOrders);

    /// <summary>
    /// Gets the next available OrderIndex for a survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>The next available OrderIndex value.</returns>
    Task<int> GetNextOrderIndexAsync(int surveyId);

    /// <summary>
    /// Gets all required questions for a specific survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>A collection of required questions.</returns>
    Task<IEnumerable<Question>> GetRequiredQuestionsBySurveyIdAsync(int surveyId);

    /// <summary>
    /// Gets questions by type for a specific survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <param name="questionType">The question type to filter by.</param>
    /// <returns>A collection of questions of the specified type.</returns>
    Task<IEnumerable<Question>> GetByTypeAsync(int surveyId, QuestionType questionType);

    /// <summary>
    /// Deletes all questions for a specific survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>The number of questions deleted.</returns>
    Task<int> DeleteBySurveyIdAsync(int surveyId);

    /// <summary>
    /// Checks if a question belongs to a specific survey.
    /// </summary>
    /// <param name="questionId">The question ID.</param>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>True if the question belongs to the survey, otherwise false.</returns>
    Task<bool> BelongsToSurveyAsync(int questionId, int surveyId);

    /// <summary>
    /// Gets all questions for a survey with their branching rules included.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <param name="includeBranching">Whether to include branching rules (default: true).</param>
    /// <returns>A collection of questions with branching information.</returns>
    Task<IEnumerable<Question>> GetWithBranchingRulesAsync(int surveyId, bool includeBranching = true);

    /// <summary>
    /// Gets all questions that can be branched to from a specific question.
    /// </summary>
    /// <param name="parentQuestionId">The parent question ID.</param>
    /// <returns>A collection of child questions (branching targets).</returns>
    Task<IEnumerable<Question>> GetChildQuestionsAsync(int parentQuestionId);

    /// <summary>
    /// Gets all questions that can branch to a specific question.
    /// </summary>
    /// <param name="childQuestionId">The child question ID.</param>
    /// <returns>A collection of parent questions (branching sources).</returns>
    Task<IEnumerable<Question>> GetParentQuestionsAsync(int childQuestionId);
}
