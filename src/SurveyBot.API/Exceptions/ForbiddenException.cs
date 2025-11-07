using System.Net;

namespace SurveyBot.API.Exceptions;

/// <summary>
/// Exception thrown when user is authenticated but doesn't have permission
/// </summary>
public class ForbiddenException : ApiException
{
    public ForbiddenException(string message = "Access forbidden.")
        : base(message, HttpStatusCode.Forbidden)
    {
    }
}
