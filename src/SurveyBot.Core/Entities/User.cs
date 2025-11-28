using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents a Telegram user in the system.
/// Follows DDD principles with private setters, encapsulated collections, and factory methods.
/// </summary>
public class User : BaseEntity
{
    private readonly List<Survey> _surveys = new();

    /// <summary>
    /// Gets the Telegram user ID (from Telegram API).
    /// This is the unique identifier from Telegram.
    /// </summary>
    [Required]
    public long TelegramId { get; private set; }

    /// <summary>
    /// Gets the Telegram username (without @ symbol).
    /// </summary>
    [MaxLength(255)]
    public string? Username { get; private set; }

    /// <summary>
    /// Gets the user's first name.
    /// </summary>
    [MaxLength(255)]
    public string? FirstName { get; private set; }

    /// <summary>
    /// Gets the user's last name.
    /// </summary>
    [MaxLength(255)]
    public string? LastName { get; private set; }

    /// <summary>
    /// Gets the timestamp when the user last logged in.
    /// Updated automatically during login/registration.
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    // Navigation properties

    /// <summary>
    /// Gets the collection of surveys created by this user.
    /// Returns a read-only view of the internal collection.
    /// </summary>
    public IReadOnlyCollection<Survey> Surveys => _surveys.AsReadOnly();

    #region Constructors

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// Use Create() factory method for application code.
    /// </summary>
    private User() : base()
    {
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new user from Telegram data.
    /// </summary>
    /// <param name="telegramId">Telegram user ID (must be positive)</param>
    /// <param name="username">Telegram username (optional, @ prefix will be removed)</param>
    /// <param name="firstName">User's first name (optional)</param>
    /// <param name="lastName">User's last name (optional)</param>
    /// <returns>New user instance with validated data</returns>
    /// <exception cref="ArgumentException">If telegramId is not positive</exception>
    public static User Create(
        long telegramId,
        string? username = null,
        string? firstName = null,
        string? lastName = null)
    {
        if (telegramId <= 0)
            throw new ArgumentException("Telegram ID must be positive", nameof(telegramId));

        // Normalize username (remove @ prefix if present)
        var normalizedUsername = string.IsNullOrWhiteSpace(username)
            ? null
            : username.Trim().TrimStart('@');

        var user = new User
        {
            TelegramId = telegramId,
            Username = normalizedUsername,
            FirstName = firstName?.Trim(),
            LastName = lastName?.Trim(),
            LastLoginAt = DateTime.UtcNow
        };

        return user;
    }

    #endregion

    #region Domain Methods

    /// <summary>
    /// Updates user profile information from Telegram (upsert pattern).
    /// Call this when user data may have changed.
    /// </summary>
    /// <param name="username">Telegram username (optional, @ prefix will be removed)</param>
    /// <param name="firstName">User's first name (optional)</param>
    /// <param name="lastName">User's last name (optional)</param>
    public void UpdateFromTelegram(
        string? username,
        string? firstName,
        string? lastName)
    {
        Username = string.IsNullOrWhiteSpace(username)
            ? null
            : username.Trim().TrimStart('@');
        FirstName = firstName?.Trim();
        LastName = lastName?.Trim();
        LastLoginAt = DateTime.UtcNow;
        MarkAsModified();
    }

    /// <summary>
    /// Updates the last login timestamp to the current UTC time.
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        MarkAsModified();
    }

    #endregion

    #region Internal Methods (for testing and EF Core)

    /// <summary>
    /// Adds a survey to the user's collection. Internal use only.
    /// </summary>
    internal void AddSurveyInternal(Survey survey)
    {
        _surveys.Add(survey);
    }

    /// <summary>
    /// Sets the Telegram ID. Used by tests only.
    /// For normal use, prefer the Create() factory method.
    /// </summary>
    internal void SetTelegramId(long telegramId)
    {
        if (telegramId <= 0)
            throw new ArgumentException("Telegram ID must be positive", nameof(telegramId));
        TelegramId = telegramId;
    }

    /// <summary>
    /// Sets the username. Used by tests only.
    /// For normal use, prefer UpdateFromTelegram().
    /// </summary>
    internal void SetUsername(string? username)
    {
        Username = username?.TrimStart('@');
    }

    /// <summary>
    /// Sets the first name. Used by tests only.
    /// For normal use, prefer UpdateFromTelegram().
    /// </summary>
    internal void SetFirstName(string? firstName)
    {
        FirstName = firstName;
    }

    /// <summary>
    /// Sets the last name. Used by tests only.
    /// For normal use, prefer UpdateFromTelegram().
    /// </summary>
    internal void SetLastName(string? lastName)
    {
        LastName = lastName;
    }

    #endregion
}
