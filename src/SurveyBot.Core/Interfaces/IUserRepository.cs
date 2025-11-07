using SurveyBot.Core.Entities;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Repository interface for User entity with specific query methods.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Gets a user by their Telegram ID.
    /// </summary>
    /// <param name="telegramId">The Telegram user ID.</param>
    /// <returns>The user if found, otherwise null.</returns>
    Task<User?> GetByTelegramIdAsync(long telegramId);

    /// <summary>
    /// Gets a user by their Telegram username (case-insensitive).
    /// </summary>
    /// <param name="username">The Telegram username (without @ symbol).</param>
    /// <returns>The user if found, otherwise null.</returns>
    Task<User?> GetByUsernameAsync(string username);

    /// <summary>
    /// Gets a user by Telegram ID with all their created surveys included.
    /// </summary>
    /// <param name="telegramId">The Telegram user ID.</param>
    /// <returns>The user with surveys if found, otherwise null.</returns>
    Task<User?> GetByTelegramIdWithSurveysAsync(long telegramId);

    /// <summary>
    /// Checks if a user exists by their Telegram ID.
    /// </summary>
    /// <param name="telegramId">The Telegram user ID.</param>
    /// <returns>True if the user exists, otherwise false.</returns>
    Task<bool> ExistsByTelegramIdAsync(long telegramId);

    /// <summary>
    /// Checks if a username is already taken.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <returns>True if the username is taken, otherwise false.</returns>
    Task<bool> IsUsernameTakenAsync(string username);

    /// <summary>
    /// Creates or updates a user based on their Telegram ID.
    /// If the user exists, updates their information; otherwise, creates a new user.
    /// </summary>
    /// <param name="telegramId">The Telegram user ID.</param>
    /// <param name="username">The Telegram username.</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    /// <returns>The created or updated user.</returns>
    Task<User> CreateOrUpdateAsync(long telegramId, string? username, string? firstName, string? lastName);

    /// <summary>
    /// Gets users who have created surveys (survey creators/admins).
    /// </summary>
    /// <returns>A collection of users who are survey creators.</returns>
    Task<IEnumerable<User>> GetSurveyCreatorsAsync();

    /// <summary>
    /// Gets the count of surveys created by a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The count of surveys created by the user.</returns>
    Task<int> GetSurveyCountAsync(int userId);

    /// <summary>
    /// Searches users by name (first name or last name, case-insensitive).
    /// </summary>
    /// <param name="searchTerm">The search term to match against user names.</param>
    /// <returns>A collection of users matching the search term.</returns>
    Task<IEnumerable<User>> SearchByNameAsync(string searchTerm);
}
