using Microsoft.EntityFrameworkCore;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;

namespace SurveyBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Response entity with specific query methods.
/// </summary>
public class ResponseRepository : GenericRepository<Response>, IResponseRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ResponseRepository(SurveyBotDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Response?> GetByIdWithAnswersAsync(int id)
    {
        return await _dbSet
            .Include(r => r.Answers)
                .ThenInclude(a => a.Question)
            .Include(r => r.Survey)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Response>> GetBySurveyIdAsync(int surveyId)
    {
        return await _dbSet
            .Include(r => r.Answers)
            .Where(r => r.SurveyId == surveyId)
            .OrderByDescending(r => r.SubmittedAt ?? r.StartedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Response>> GetCompletedBySurveyIdAsync(int surveyId)
    {
        return await _dbSet
            .Include(r => r.Answers)
                .ThenInclude(a => a.Question)
            .Where(r => r.SurveyId == surveyId && r.IsComplete)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Response>> GetByUserAndSurveyAsync(int surveyId, long telegramId)
    {
        return await _dbSet
            .Include(r => r.Answers)
            .Where(r => r.SurveyId == surveyId && r.RespondentTelegramId == telegramId)
            .OrderByDescending(r => r.SubmittedAt ?? r.StartedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Response?> GetIncompleteResponseAsync(int surveyId, long telegramId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(r => r.Answers)
                .ThenInclude(a => a.Question)
            .Include(r => r.Survey)
                .ThenInclude(s => s.Questions.OrderBy(q => q.OrderIndex))
            .FirstOrDefaultAsync(r => r.SurveyId == surveyId
                && r.RespondentTelegramId == telegramId
                && !r.IsComplete);
    }

    /// <inheritdoc />
    public async Task<bool> HasUserCompletedSurveyAsync(int surveyId, long telegramId)
    {
        return await _dbSet
            .AnyAsync(r => r.SurveyId == surveyId
                && r.RespondentTelegramId == telegramId
                && r.IsComplete);
    }

    /// <inheritdoc />
    public async Task<int> GetCompletedCountAsync(int surveyId)
    {
        return await _dbSet
            .Where(r => r.SurveyId == surveyId && r.IsComplete)
            .CountAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Response>> GetByDateRangeAsync(int surveyId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(r => r.Answers)
            .Where(r => r.SurveyId == surveyId
                && r.SubmittedAt.HasValue
                && r.SubmittedAt >= startDate
                && r.SubmittedAt <= endDate)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsCompleteAsync(int responseId)
    {
        var response = await GetByIdAsync(responseId);

        if (response == null)
        {
            return false;
        }

        response.SetIsComplete(true);
        response.SetSubmittedAt(DateTime.UtcNow);

        await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc />
    public async Task<int> DeleteBySurveyIdAsync(int surveyId)
    {
        var responses = await _dbSet
            .Where(r => r.SurveyId == surveyId)
            .ToListAsync();

        if (responses.Count == 0)
        {
            return 0;
        }

        _dbSet.RemoveRange(responses);
        await _context.SaveChangesAsync();

        return responses.Count;
    }

    /// <inheritdoc />
    public override async Task<Response?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(r => r.Survey)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<Response>> GetAllAsync()
    {
        return await _dbSet
            .Include(r => r.Survey)
            .Include(r => r.Answers)
            .OrderByDescending(r => r.SubmittedAt ?? r.StartedAt)
            .ToListAsync();
    }
}
