using System.Net;

namespace SurveyBot.API.Exceptions;

/// <summary>
/// Base exception class for API-specific errors
/// </summary>
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? Details { get; }

    public ApiException(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        string? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        Details = details;
    }

    public ApiException(
        string message,
        Exception innerException,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        string? details = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Details = details;
    }
}
