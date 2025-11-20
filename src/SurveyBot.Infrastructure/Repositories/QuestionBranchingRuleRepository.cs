using Microsoft.EntityFrameworkCore;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;

namespace SurveyBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for QuestionBranchingRule entity with specific query methods.
/// </summary>
public class QuestionBranchingRuleRepository : GenericRepository<QuestionBranchingRule>, IQuestionBranchingRuleRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionBranchingRuleRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public QuestionBranchingRuleRepository(SurveyBotDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public override async Task<QuestionBranchingRule?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(r => r.SourceQuestion)
            .Include(r => r.TargetQuestion)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<QuestionBranchingRule?> GetByIdWithQuestionsAsync(int id)
    {
        return await _dbSet
            .Include(r => r.SourceQuestion)
                .ThenInclude(q => q.Survey)
            .Include(r => r.TargetQuestion)
                .ThenInclude(q => q.Survey)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<QuestionBranchingRule>> GetBySourceQuestionAsync(int sourceQuestionId)
    {
        return await _dbSet
            .Include(r => r.SourceQuestion)
            .Include(r => r.TargetQuestion)
            .Where(r => r.SourceQuestionId == sourceQuestionId)
            .OrderBy(r => r.TargetQuestionId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<QuestionBranchingRule>> GetByTargetQuestionAsync(int targetQuestionId)
    {
        return await _dbSet
            .Include(r => r.SourceQuestion)
            .Include(r => r.TargetQuestion)
            .Where(r => r.TargetQuestionId == targetQuestionId)
            .OrderBy(r => r.SourceQuestionId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<QuestionBranchingRule>> GetBySurveyIdAsync(int surveyId)
    {
        return await _dbSet
            .Include(r => r.SourceQuestion)
            .Include(r => r.TargetQuestion)
            .Where(r => r.SourceQuestion.SurveyId == surveyId)
            .OrderBy(r => r.SourceQuestion.OrderIndex)
            .ThenBy(r => r.TargetQuestion.OrderIndex)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<QuestionBranchingRule?> GetBySourceAndTargetAsync(int sourceQuestionId, int targetQuestionId)
    {
        return await _dbSet
            .Include(r => r.SourceQuestion)
            .Include(r => r.TargetQuestion)
            .FirstOrDefaultAsync(r =>
                r.SourceQuestionId == sourceQuestionId &&
                r.TargetQuestionId == targetQuestionId);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int sourceQuestionId, int targetQuestionId)
    {
        return await _dbSet
            .AnyAsync(r =>
                r.SourceQuestionId == sourceQuestionId &&
                r.TargetQuestionId == targetQuestionId);
    }

    /// <inheritdoc />
    public async Task<int> DeleteBySourceQuestionAsync(int sourceQuestionId)
    {
        var rules = await _dbSet
            .Where(r => r.SourceQuestionId == sourceQuestionId)
            .ToListAsync();

        if (rules.Count == 0)
        {
            return 0;
        }

        _dbSet.RemoveRange(rules);
        await _context.SaveChangesAsync();

        return rules.Count;
    }

    /// <inheritdoc />
    public async Task<int> DeleteByTargetQuestionAsync(int targetQuestionId)
    {
        var rules = await _dbSet
            .Where(r => r.TargetQuestionId == targetQuestionId)
            .ToListAsync();

        if (rules.Count == 0)
        {
            return 0;
        }

        _dbSet.RemoveRange(rules);
        await _context.SaveChangesAsync();

        return rules.Count;
    }

    /// <inheritdoc />
    public async Task<int> DeleteBySurveyIdAsync(int surveyId)
    {
        var rules = await _dbSet
            .Include(r => r.SourceQuestion)
            .Where(r => r.SourceQuestion.SurveyId == surveyId)
            .ToListAsync();

        if (rules.Count == 0)
        {
            return 0;
        }

        _dbSet.RemoveRange(rules);
        await _context.SaveChangesAsync();

        return rules.Count;
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<QuestionBranchingRule>> GetAllAsync()
    {
        return await _dbSet
            .Include(r => r.SourceQuestion)
            .Include(r => r.TargetQuestion)
            .OrderBy(r => r.SourceQuestion.SurveyId)
            .ThenBy(r => r.SourceQuestion.OrderIndex)
            .ToListAsync();
    }
}
