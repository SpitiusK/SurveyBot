using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.User;

/// <summary>
/// Data transfer object for updating user information.
/// </summary>
public class UpdateUserDto
{
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
