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
    /// Gets a question by ID with the Options collection included (eager loaded).
    /// Used after creating a question to retrieve database-generated option IDs.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <returns>The question with Options collection if found, otherwise null.</returns>
    Task<Question?> GetByIdWithOptionsAsync(int id);

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
    /// Gets all questions for a survey with flow configuration (Options, DefaultNextQuestion).
    /// Used for cycle detection and flow validation.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>A collection of questions with Options and DefaultNextQuestion navigation properties loaded.</returns>
    Task<List<Question>> GetWithFlowConfigurationAsync(int surveyId);

    /// <summary>
    /// Determines the next question ID based on the current question and selected option.
    /// For branching questions: looks up the option's NextQuestionId.
    /// For non-branching questions: returns DefaultNextQuestionId.
    /// </summary>
    /// <param name="questionId">The current question ID.</param>
    /// <param name="selectedOptionText">The selected option text (for branching questions, null for non-branching).</param>
    /// <returns>The next question ID, or null if no next question configured.</returns>
    Task<int?> GetNextQuestionIdAsync(int questionId, string? selectedOptionText);

    /// <summary>
    /// Gets a question by ID with Options collection for conditional flow determination.
    /// Uses AsNoTracking for read-only queries during flow logic execution.
    /// </summary>
    /// <param name="questionId">The question ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The question with Options collection if found, otherwise null.</returns>
    Task<Question?> GetByIdWithFlowConfigAsync(int questionId, CancellationToken cancellationToken = default);
}
