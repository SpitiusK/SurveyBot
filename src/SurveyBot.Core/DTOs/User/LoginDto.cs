using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.User;

/// <summary>
/// Data transfer object for user login/authentication.
/// Uses Telegram Web App authentication data.
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Gets or sets the Telegram Web App init data string.
    /// This contains the authentication data from Telegram.
    /// </summary>
    [Required(ErrorMessage = "Telegram init data is required")]
    public string InitData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Telegram user ID (for simple authentication).
    /// Alternative to InitData for development/testing.
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "Invalid Telegram ID")]
    public long? TelegramId { get; set; }

    /// <summary>
    /// Gets or sets the username (for simple authentication).
    /// Alternative to InitData for development/testing.
    /// </summary>
    [MaxLength(255, ErrorMessage = "Username cannot exceed 255 characters")]
    public string? Username { get; set; }
}
