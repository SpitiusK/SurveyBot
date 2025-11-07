namespace SurveyBot.Core.Configuration;

/// <summary>
/// JWT configuration settings for token generation and validation.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Gets or sets the secret key used to sign JWT tokens.
    /// Must be at least 32 characters for security.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JWT token issuer (API that creates the token).
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JWT token audience (who can use the token).
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token lifetime in hours.
    /// Default is 24 hours for MVP.
    /// </summary>
    public int TokenLifetimeHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the refresh token lifetime in days.
    /// Default is 7 days for MVP.
    /// </summary>
    public int RefreshTokenLifetimeDays { get; set; } = 7;
}
