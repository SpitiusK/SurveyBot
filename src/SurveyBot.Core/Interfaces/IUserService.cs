using System.Security.Claims;
using SurveyBot.Core.DTOs.User;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Service interface for user management and authentication.
/// Provides user registration, login, and profile management functionality.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Registers a new user or updates an existing user on login (upsert pattern).
    /// This method is designed for Telegram bot integration where users are automatically
    /// registered on first interaction (/start command).
    /// </summary>
    /// <param name="registerDto">Registration data containing Telegram user information.</param>
    /// <returns>UserDto with JWT token for authentication.</returns>
    Task<UserWithTokenDto> RegisterAsync(RegisterDto registerDto);

    /// <summary>
    /// Gets a user by their Telegram ID.
    /// </summary>
    /// <param name="telegramId">The Telegram user ID.</param>
    /// <returns>UserDto if found, otherwise null.</returns>
    Task<UserDto?> GetUserByTelegramIdAsync(long telegramId);

    /// <summary>
    /// Gets a user by their internal database ID.
    /// </summary>
    /// <param name="userId">The internal user ID.</param>
    /// <returns>UserDto if found, otherwise null.</returns>
    Task<UserDto?> GetUserByIdAsync(int userId);

    /// <summary>
    /// Updates user information.
    /// </summary>
    /// <param name="userId">The user ID to update.</param>
    /// <param name="updateDto">Updated user information.</param>
    /// <returns>Updated UserDto.</returns>
    Task<UserDto> UpdateUserAsync(int userId, UpdateUserDto updateDto);

    /// <summary>
    /// Gets the current authenticated user by their ID.
    /// This method is typically called from authenticated endpoints using the user ID from JWT claims.
    /// </summary>
    /// <param name="userId">The user ID from JWT claims.</param>
    /// <returns>UserDto of the current user.</returns>
    Task<UserDto> GetCurrentUserAsync(int userId);

    /// <summary>
    /// Validates a JWT token and returns the claims principal.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>ClaimsPrincipal if token is valid, otherwise null.</returns>
    ClaimsPrincipal? ValidateTokenAsync(string token);

    /// <summary>
    /// Updates the last login timestamp for a user.
    /// Called automatically during the login/registration process.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    Task UpdateLastLoginAsync(int userId);

    /// <summary>
    /// Checks if a user exists by their Telegram ID.
    /// </summary>
    /// <param name="telegramId">The Telegram user ID.</param>
    /// <returns>True if user exists, otherwise false.</returns>
    Task<bool> UserExistsAsync(long telegramId);

    /// <summary>
    /// Gets all users (for admin purposes).
    /// </summary>
    /// <returns>Collection of all users.</returns>
    Task<IEnumerable<UserDto>> GetAllUsersAsync();

    /// <summary>
    /// Searches users by name (first name or last name).
    /// </summary>
    /// <param name="searchTerm">Search term to match against user names.</param>
    /// <returns>Collection of matching users.</returns>
    Task<IEnumerable<UserDto>> SearchUsersAsync(string searchTerm);
}
