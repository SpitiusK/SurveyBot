using Microsoft.EntityFrameworkCore;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;

namespace SurveyBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Answer entity with specific query methods.
/// </summary>
public class AnswerRepository : GenericRepository<Answer>, IAnswerRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnswerRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public AnswerRepository(SurveyBotDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Answer>> GetByResponseIdAsync(int responseId)
    {
        return await _dbSet
            .Include(a => a.Question)
            .Where(a => a.ResponseId == responseId)
            .OrderBy(a => a.Question.OrderIndex)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId)
    {
        return await _dbSet
            .Include(a => a.Response)
            .Where(a => a.QuestionId == questionId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Answer?> GetByResponseAndQuestionAsync(int responseId, int questionId)
    {
        return await _dbSet
            .Include(a => a.Question)
            .Include(a => a.Response)
            .FirstOrDefaultAsync(a => a.ResponseId == responseId && a.QuestionId == questionId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Answer>> CreateBatchAsync(IEnumerable<Answer> answers)
    {
        if (answers == null || !answers.Any())
        {
            return Enumerable.Empty<Answer>();
        }

        await _dbSet.AddRangeAsync(answers);
        await _context.SaveChangesAsync();

        return answers;
    }

    /// <inheritdoc />
    public async Task<int> DeleteByResponseIdAsync(int responseId)
    {
        var answers = await _dbSet
            .Where(a => a.ResponseId == responseId)
            .ToListAsync();

        if (answers.Count == 0)
        {
            return 0;
        }

        _dbSet.RemoveRange(answers);
        await _context.SaveChangesAsync();

        return answers.Count;
    }

    /// <inheritdoc />
    public async Task<int> DeleteByQuestionIdAsync(int questionId)
    {
        var answers = await _dbSet
            .Where(a => a.QuestionId == questionId)
            .ToListAsync();

        if (answers.Count == 0)
        {
            return 0;
        }

        _dbSet.RemoveRange(answers);
        await _context.SaveChangesAsync();

        return answers.Count;
    }

    /// <inheritdoc />
    public async Task<int> GetCountByQuestionIdAsync(int questionId)
    {
        return await _dbSet
            .Where(a => a.QuestionId == questionId)
            .CountAsync();
    }

    /// <inheritdoc />
    public async Task<bool> HasAnswerAsync(int responseId, int questionId)
    {
        return await _dbSet
            .AnyAsync(a => a.ResponseId == responseId && a.QuestionId == questionId);
    }

    /// <inheritdoc />
    public override async Task<Answer?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(a => a.Question)
            .Include(a => a.Response)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<Answer>> GetAllAsync()
    {
        return await _dbSet
            .Include(a => a.Question)
            .Include(a => a.Response)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}
