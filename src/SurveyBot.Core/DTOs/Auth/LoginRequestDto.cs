using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.Auth;

/// <summary>
/// Request DTO for user login.
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// Gets or sets the Telegram ID for authentication.
    /// </summary>
    [Required(ErrorMessage = "Telegram ID is required")]
    [Range(1, long.MaxValue, ErrorMessage = "Telegram ID must be a positive number")]
    public long TelegramId { get; set; }

    /// <summary>
    /// Gets or sets the username (optional, for logging purposes).
    /// </summary>
    [MaxLength(255)]
    public string? Username { get; set; }
}
