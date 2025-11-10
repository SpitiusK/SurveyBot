using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveyBot.Bot.Configuration;
using SurveyBot.Bot.Interfaces;

namespace SurveyBot.Bot.Services;

/// <summary>
/// Service for checking if users have admin privileges.
/// Implements simple whitelist-based authorization using configured admin user IDs.
/// </summary>
public class AdminAuthService : IAdminAuthService
{
    private readonly BotConfiguration _configuration;
    private readonly ILogger<AdminAuthService> _logger;
    private readonly HashSet<long> _adminUserIds;

    public AdminAuthService(
        IOptions<BotConfiguration> configuration,
        ILogger<AdminAuthService> logger)
    {
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Load admin user IDs into HashSet for O(1) lookup
        _adminUserIds = new HashSet<long>(_configuration.AdminUserIds ?? Array.Empty<long>());

        _logger.LogInformation(
            "AdminAuthService initialized with {Count} admin users",
            _adminUserIds.Count);
    }

    /// <summary>
    /// Checks if a user has admin privileges.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID to check.</param>
    /// <returns>True if user is admin, false otherwise.</returns>
    public bool IsAdmin(long telegramUserId)
    {
        var isAdmin = _adminUserIds.Contains(telegramUserId);

        _logger.LogDebug(
            "Admin check for user {TelegramUserId}: {IsAdmin}",
            telegramUserId,
            isAdmin);

        return isAdmin;
    }

    /// <summary>
    /// Validates that a user is an admin. Throws exception if not.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID to validate.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not an admin.</exception>
    public void RequireAdmin(long telegramUserId)
    {
        if (!IsAdmin(telegramUserId))
        {
            _logger.LogWarning(
                "Unauthorized admin access attempt by user {TelegramUserId}",
                telegramUserId);

            throw new UnauthorizedAccessException("This command requires admin privileges.");
        }

        _logger.LogDebug(
            "Admin access granted to user {TelegramUserId}",
            telegramUserId);
    }

    /// <summary>
    /// Gets the count of configured admin users.
    /// </summary>
    public int AdminCount => _adminUserIds.Count;

    /// <summary>
    /// Gets all admin user IDs (for debugging/logging purposes).
    /// </summary>
    public IReadOnlySet<long> GetAdminUserIds() => _adminUserIds;
}
