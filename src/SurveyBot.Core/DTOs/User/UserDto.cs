namespace SurveyBot.Core.DTOs.User;

/// <summary>
/// Data transfer object for reading user information.
/// </summary>
public class UserDto
{
    /// <summary>
    /// Gets or sets the user ID (internal database ID).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Telegram user ID.
    /// </summary>
    public long TelegramId { get; set; }

    /// <summary>
    /// Gets or sets the Telegram username (without @ symbol).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the user was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the user last logged in.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}
