using SurveyBot.Core.DTOs.User;

namespace SurveyBot.Core.DTOs.Auth;

/// <summary>
/// Response DTO for successful login containing JWT tokens.
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// Gets or sets the JWT access token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user information.
    /// </summary>
    public UserDto User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the token expiration time in UTC.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the refresh token (optional for MVP).
    /// </summary>
    public string? RefreshToken { get; set; }
}
