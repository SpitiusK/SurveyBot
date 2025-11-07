namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when a requested survey is not found.
/// </summary>
public class SurveyNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyNotFoundException"/> class.
    /// </summary>
    /// <param name="surveyId">The ID of the survey that was not found.</param>
    public SurveyNotFoundException(int surveyId)
        : base($"Survey with ID {surveyId} was not found.")
    {
        SurveyId = surveyId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SurveyNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Gets the ID of the survey that was not found.
    /// </summary>
    public int? SurveyId { get; }
}
