using SurveyBot.Core.DTOs.Auth;

namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Service interface for authentication and JWT token management.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and generates JWT tokens.
    /// </summary>
    /// <param name="request">Login request containing Telegram ID and optional username.</param>
    /// <returns>Login response with access token and user information.</returns>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// Validates a JWT token and returns user claims.
    /// </summary>
    /// <param name="token">JWT token to validate.</param>
    /// <returns>True if token is valid, false otherwise.</returns>
    bool ValidateToken(string token);

    /// <summary>
    /// Refreshes an access token using a refresh token (optional for MVP).
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <returns>New login response with fresh tokens.</returns>
    Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);

    /// <summary>
    /// Generates a JWT token for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="telegramId">Telegram ID.</param>
    /// <param name="username">Username.</param>
    /// <returns>JWT token string.</returns>
    string GenerateAccessToken(int userId, long telegramId, string? username);

    /// <summary>
    /// Generates a refresh token (optional for MVP).
    /// </summary>
    /// <returns>Refresh token string.</returns>
    string GenerateRefreshToken();
}
