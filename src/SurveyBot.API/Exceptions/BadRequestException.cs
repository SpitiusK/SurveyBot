using System.Net;

namespace SurveyBot.API.Exceptions;

/// <summary>
/// Exception thrown when the request is invalid
/// </summary>
public class BadRequestException : ApiException
{
    public BadRequestException(string message)
        : base(message, HttpStatusCode.BadRequest)
    {
    }

    public BadRequestException(string message, string details)
        : base(message, HttpStatusCode.BadRequest, details)
    {
    }
}
