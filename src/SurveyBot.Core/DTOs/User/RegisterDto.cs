using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.User;

/// <summary>
/// Data transfer object for user registration.
/// Used when a new Telegram user accesses the system.
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// Gets or sets the Telegram user ID.
    /// </summary>
    [Required(ErrorMessage = "Telegram ID is required")]
    [Range(1, long.MaxValue, ErrorMessage = "Invalid Telegram ID")]
    public long TelegramId { get; set; }

    /// <summary>
    /// Gets or sets the Telegram username (without @ symbol).
    /// </summary>
    [MaxLength(255, ErrorMessage = "Username cannot exceed 255 characters")]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    [MaxLength(255, ErrorMessage = "First name cannot exceed 255 characters")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    [MaxLength(255, ErrorMessage = "Last name cannot exceed 255 characters")]
    public string? LastName { get; set; }
}
