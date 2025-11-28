using Microsoft.EntityFrameworkCore;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;

namespace SurveyBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity with specific query methods.
/// </summary>
public class UserRepository : GenericRepository<User>, IUserRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public UserRepository(SurveyBotDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<User?> GetByTelegramIdAsync(long telegramId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
    }

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return await _dbSet
            .FirstOrDefaultAsync(u => u.Username != null && u.Username.ToLower() == username.ToLower());
    }

    /// <inheritdoc />
    public async Task<User?> GetByTelegramIdWithSurveysAsync(long telegramId)
    {
        return await _dbSet
            .Include(u => u.Surveys)
                .ThenInclude(s => s.Questions)
            .Include(u => u.Surveys)
                .ThenInclude(s => s.Responses)
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByTelegramIdAsync(long telegramId)
    {
        return await _dbSet
            .AnyAsync(u => u.TelegramId == telegramId);
    }

    /// <inheritdoc />
    public async Task<bool> IsUsernameTakenAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        return await _dbSet
            .AnyAsync(u => u.Username != null && u.Username.ToLower() == username.ToLower());
    }

    /// <inheritdoc />
    public async Task<User> CreateOrUpdateAsync(long telegramId, string? username, string? firstName, string? lastName)
    {
        var existingUser = await GetByTelegramIdAsync(telegramId);

        if (existingUser != null)
        {
            // Update existing user information using domain method
            existingUser.UpdateFromTelegram(username, firstName, lastName);

            await _context.SaveChangesAsync();
            return existingUser;
        }

        // Create new user using factory method
        var newUser = User.Create(telegramId, username, firstName, lastName);

        await _dbSet.AddAsync(newUser);
        await _context.SaveChangesAsync();

        return newUser;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetSurveyCreatorsAsync()
    {
        return await _dbSet
            .Include(u => u.Surveys)
            .Where(u => u.Surveys.Any())
            .OrderBy(u => u.Username ?? u.FirstName ?? u.LastName)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetSurveyCountAsync(int userId)
    {
        return await _context.Surveys
            .Where(s => s.CreatorId == userId)
            .CountAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> SearchByNameAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllAsync();
        }

        var lowerSearchTerm = searchTerm.ToLower();

        return await _dbSet
            .Where(u =>
                (u.FirstName != null && u.FirstName.ToLower().Contains(lowerSearchTerm)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(lowerSearchTerm)) ||
                (u.Username != null && u.Username.ToLower().Contains(lowerSearchTerm)))
            .OrderBy(u => u.Username ?? u.FirstName ?? u.LastName)
            .ToListAsync();
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _dbSet
            .OrderBy(u => u.Username ?? u.FirstName ?? u.LastName)
            .ToListAsync();
    }
}
