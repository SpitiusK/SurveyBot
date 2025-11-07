using System.Net;

namespace SurveyBot.API.Exceptions;

/// <summary>
/// Exception thrown when user is not authorized
/// </summary>
public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message = "Unauthorized access.")
        : base(message, HttpStatusCode.Unauthorized)
    {
    }
}
