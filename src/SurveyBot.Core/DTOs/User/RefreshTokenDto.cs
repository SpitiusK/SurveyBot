using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.User;

/// <summary>
/// Data transfer object for refreshing JWT access token.
/// </summary>
public class RefreshTokenDto
{
    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
