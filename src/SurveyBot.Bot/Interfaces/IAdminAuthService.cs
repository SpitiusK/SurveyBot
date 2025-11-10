namespace SurveyBot.Bot.Interfaces;

/// <summary>
/// Service interface for admin authorization.
/// </summary>
public interface IAdminAuthService
{
    /// <summary>
    /// Checks if a user has admin privileges.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID to check.</param>
    /// <returns>True if user is admin, false otherwise.</returns>
    bool IsAdmin(long telegramUserId);

    /// <summary>
    /// Validates that a user is an admin. Throws exception if not.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID to validate.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not an admin.</exception>
    void RequireAdmin(long telegramUserId);

    /// <summary>
    /// Gets the count of configured admin users.
    /// </summary>
    int AdminCount { get; }

    /// <summary>
    /// Gets all admin user IDs (for debugging/logging purposes).
    /// </summary>
    IReadOnlySet<long> GetAdminUserIds();
}
