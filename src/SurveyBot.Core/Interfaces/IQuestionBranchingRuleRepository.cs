using SurveyBot.Core.Entities;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Repository interface for QuestionBranchingRule entity with specific query methods.
/// </summary>
public interface IQuestionBranchingRuleRepository : IRepository<QuestionBranchingRule>
{
    /// <summary>
    /// Gets a branching rule by its ID with related source and target questions included.
    /// </summary>
    /// <param name="id">The branching rule ID.</param>
    /// <returns>The branching rule if found, otherwise null.</returns>
    Task<QuestionBranchingRule?> GetByIdWithQuestionsAsync(int id);

    /// <summary>
    /// Gets all branching rules where the specified question is the source.
    /// Used to determine where to branch based on answers to this question.
    /// </summary>
    /// <param name="sourceQuestionId">The source question ID.</param>
    /// <returns>A collection of branching rules originating from this question.</returns>
    Task<IEnumerable<QuestionBranchingRule>> GetBySourceQuestionAsync(int sourceQuestionId);

    /// <summary>
    /// Gets all branching rules where the specified question is the target.
    /// Used to determine which questions can branch to this question.
    /// </summary>
    /// <param name="targetQuestionId">The target question ID.</param>
    /// <returns>A collection of branching rules targeting this question.</returns>
    Task<IEnumerable<QuestionBranchingRule>> GetByTargetQuestionAsync(int targetQuestionId);

    /// <summary>
    /// Gets all branching rules for a specific survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>A collection of all branching rules in the survey.</returns>
    Task<IEnumerable<QuestionBranchingRule>> GetBySurveyIdAsync(int surveyId);

    /// <summary>
    /// Gets a specific branching rule by source and target question IDs.
    /// </summary>
    /// <param name="sourceQuestionId">The source question ID.</param>
    /// <param name="targetQuestionId">The target question ID.</param>
    /// <returns>The branching rule if found, otherwise null.</returns>
    Task<QuestionBranchingRule?> GetBySourceAndTargetAsync(int sourceQuestionId, int targetQuestionId);

    /// <summary>
    /// Checks if a branching rule exists between two questions.
    /// </summary>
    /// <param name="sourceQuestionId">The source question ID.</param>
    /// <param name="targetQuestionId">The target question ID.</param>
    /// <returns>True if a rule exists, otherwise false.</returns>
    Task<bool> ExistsAsync(int sourceQuestionId, int targetQuestionId);

    /// <summary>
    /// Deletes all branching rules for a specific source question.
    /// </summary>
    /// <param name="sourceQuestionId">The source question ID.</param>
    /// <returns>The number of rules deleted.</returns>
    Task<int> DeleteBySourceQuestionAsync(int sourceQuestionId);

    /// <summary>
    /// Deletes all branching rules for a specific target question.
    /// </summary>
    /// <param name="targetQuestionId">The target question ID.</param>
    /// <returns>The number of rules deleted.</returns>
    Task<int> DeleteByTargetQuestionAsync(int targetQuestionId);

    /// <summary>
    /// Deletes all branching rules for a specific survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <returns>The number of rules deleted.</returns>
    Task<int> DeleteBySurveyIdAsync(int surveyId);
}
