using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents a Telegram user in the system.
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Gets or sets the Telegram user ID (from Telegram API).
    /// This is the unique identifier from Telegram.
    /// </summary>
    [Required]
    public long TelegramId { get; set; }

    /// <summary>
    /// Gets or sets the Telegram username (without @ symbol).
    /// </summary>
    [MaxLength(255)]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    [MaxLength(255)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    [MaxLength(255)]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the user last logged in.
    /// Updated automatically during login/registration.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the collection of surveys created by this user.
    /// </summary>
    public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
}
