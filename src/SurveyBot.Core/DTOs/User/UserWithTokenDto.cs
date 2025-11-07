namespace SurveyBot.Core.DTOs.User;

/// <summary>
/// Data transfer object for user information with authentication token.
/// Returned after successful registration or login.
/// </summary>
public class UserWithTokenDto
{
    /// <summary>
    /// Gets or sets the user information.
    /// </summary>
    public UserDto User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the JWT access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token (optional for MVP).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the token expiration timestamp.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
