namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when a requested response is not found.
/// </summary>
public class ResponseNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseNotFoundException"/> class.
    /// </summary>
    /// <param name="responseId">The ID of the response that was not found.</param>
    public ResponseNotFoundException(int responseId)
        : base($"Response with ID {responseId} was not found.")
    {
        ResponseId = responseId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ResponseNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Gets the ID of the response that was not found.
    /// </summary>
    public int? ResponseId { get; }
}
