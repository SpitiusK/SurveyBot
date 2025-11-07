namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to create a duplicate response for a survey.
/// </summary>
public class DuplicateResponseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateResponseException"/> class.
    /// </summary>
    /// <param name="surveyId">The ID of the survey.</param>
    /// <param name="telegramUserId">The Telegram ID of the user.</param>
    public DuplicateResponseException(int surveyId, long telegramUserId)
        : base($"User {telegramUserId} has already completed survey {surveyId}.")
    {
        SurveyId = surveyId;
        TelegramUserId = telegramUserId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateResponseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DuplicateResponseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Gets the ID of the survey.
    /// </summary>
    public int? SurveyId { get; }

    /// <summary>
    /// Gets the Telegram ID of the user.
    /// </summary>
    public long? TelegramUserId { get; }
}
