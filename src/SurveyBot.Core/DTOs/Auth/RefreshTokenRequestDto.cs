using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.Auth;

/// <summary>
/// Request DTO for refreshing an access token.
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
