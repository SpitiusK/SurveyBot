using System.Net;

namespace SurveyBot.API.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found
/// </summary>
public class NotFoundException : ApiException
{
    public NotFoundException(string message)
        : base(message, HttpStatusCode.NotFound)
    {
    }

    public NotFoundException(string resourceName, object resourceId)
        : base($"{resourceName} with ID '{resourceId}' was not found.", HttpStatusCode.NotFound)
    {
    }
}
