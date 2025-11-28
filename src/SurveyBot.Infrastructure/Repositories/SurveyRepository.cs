using Microsoft.EntityFrameworkCore;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;

namespace SurveyBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Survey entity with specific query methods.
/// </summary>
public class SurveyRepository : GenericRepository<Survey>, ISurveyRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SurveyRepository(SurveyBotDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Survey?> GetByIdWithQuestionsAsync(int id)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
            .Include(s => s.Creator)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <inheritdoc />
    public async Task<Survey?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
            .Include(s => s.Creator)
            .Include(s => s.Responses)
                .ThenInclude(r => r.Answers)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Survey>> GetByCreatorIdAsync(int creatorId)
    {
        return await _dbSet
            .Include(s => s.Questions)
            .Include(s => s.Responses)
            .Where(s => s.CreatorId == creatorId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Survey>> GetActiveSurveysAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(s => s.Questions)
            .Include(s => s.Creator)
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ToggleActiveStatusAsync(int id)
    {
        var survey = await GetByIdAsync(id);

        if (survey == null)
        {
            return false;
        }

        if (survey.IsActive)
        {
            survey.Deactivate();
        }
        else
        {
            survey.Activate();
        }

        await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Survey>> SearchByTitleAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllAsync();
        }

        return await _dbSet
            .Include(s => s.Creator)
            .Include(s => s.Questions)
            .Where(s => EF.Functions.ILike(s.Title, $"%{searchTerm}%"))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetResponseCountAsync(int surveyId)
    {
        return await _context.Responses
            .Where(r => r.SurveyId == surveyId)
            .CountAsync();
    }

    /// <inheritdoc />
    public async Task<bool> HasResponsesAsync(int surveyId)
    {
        return await _context.Responses
            .AnyAsync(r => r.SurveyId == surveyId);
    }

    /// <inheritdoc />
    public override async Task<Survey?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(s => s.Creator)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<Survey>> GetAllAsync()
    {
        return await _dbSet
            .Include(s => s.Creator)
            .Include(s => s.Questions)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Survey?> GetByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return await _dbSet
            .Include(s => s.Creator)
            .FirstOrDefaultAsync(s => s.Code == code.ToUpper());
    }

    /// <inheritdoc />
    public async Task<Survey?> GetByCodeWithQuestionsAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return await _dbSet
            .AsNoTracking()
            .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
            .Include(s => s.Creator)
            .FirstOrDefaultAsync(s => s.Code == code.ToUpper());
    }

    /// <inheritdoc />
    public async Task<bool> CodeExistsAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return await _dbSet.AnyAsync(s => s.Code == code.ToUpper());
    }
}
