using System.Net;

namespace SurveyBot.API.Exceptions;

/// <summary>
/// Exception thrown when there's a conflict with the current state
/// </summary>
public class ConflictException : ApiException
{
    public ConflictException(string message)
        : base(message, HttpStatusCode.Conflict)
    {
    }

    public ConflictException(string message, string details)
        : base(message, HttpStatusCode.Conflict, details)
    {
    }
}
