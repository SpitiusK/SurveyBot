using Microsoft.EntityFrameworkCore;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;

namespace SurveyBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Question entity with specific query methods.
/// </summary>
public class QuestionRepository : GenericRepository<Question>, IQuestionRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public QuestionRepository(SurveyBotDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetBySurveyIdAsync(int surveyId)
    {
        return await _dbSet
            .Where(q => q.SurveyId == surveyId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Question?> GetByIdWithAnswersAsync(int id)
    {
        return await _dbSet
            .Include(q => q.Answers)
                .ThenInclude(a => a.Response)
            .Include(q => q.Survey)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    /// <inheritdoc />
    public async Task<bool> ReorderQuestionsAsync(Dictionary<int, int> questionOrders)
    {
        if (questionOrders == null || questionOrders.Count == 0)
        {
            return false;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            foreach (var (questionId, newOrderIndex) in questionOrders)
            {
                var question = await GetByIdAsync(questionId);

                if (question == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                question.OrderIndex = newOrderIndex;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetNextOrderIndexAsync(int surveyId)
    {
        var maxOrderIndex = await _dbSet
            .Where(q => q.SurveyId == surveyId)
            .MaxAsync(q => (int?)q.OrderIndex);

        return (maxOrderIndex ?? -1) + 1;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetRequiredQuestionsBySurveyIdAsync(int surveyId)
    {
        return await _dbSet
            .Where(q => q.SurveyId == surveyId && q.IsRequired)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetByTypeAsync(int surveyId, QuestionType questionType)
    {
        return await _dbSet
            .Where(q => q.SurveyId == surveyId && q.QuestionType == questionType)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> DeleteBySurveyIdAsync(int surveyId)
    {
        var questions = await _dbSet
            .Where(q => q.SurveyId == surveyId)
            .ToListAsync();

        if (questions.Count == 0)
        {
            return 0;
        }

        _dbSet.RemoveRange(questions);
        await _context.SaveChangesAsync();

        return questions.Count;
    }

    /// <inheritdoc />
    public async Task<bool> BelongsToSurveyAsync(int questionId, int surveyId)
    {
        return await _dbSet
            .AnyAsync(q => q.Id == questionId && q.SurveyId == surveyId);
    }

    /// <inheritdoc />
    public override async Task<Question?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(q => q.Survey)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<Question>> GetAllAsync()
    {
        return await _dbSet
            .Include(q => q.Survey)
            .OrderBy(q => q.SurveyId)
            .ThenBy(q => q.OrderIndex)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetWithBranchingRulesAsync(int surveyId, bool includeBranching = true)
    {
        var query = _dbSet
            .Where(q => q.SurveyId == surveyId);

        if (includeBranching)
        {
            query = query
                .Include(q => q.OutgoingRules)
                    .ThenInclude(r => r.TargetQuestion)
                .Include(q => q.IncomingRules)
                    .ThenInclude(r => r.SourceQuestion);
        }

        return await query
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetChildQuestionsAsync(int parentQuestionId)
    {
        return await _dbSet
            .Where(q => q.IncomingRules.Any(r => r.SourceQuestionId == parentQuestionId))
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetParentQuestionsAsync(int childQuestionId)
    {
        return await _dbSet
            .Where(q => q.OutgoingRules.Any(r => r.TargetQuestionId == childQuestionId))
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();
    }
}
