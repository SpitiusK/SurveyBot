using System.Net;

namespace SurveyBot.API.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : ApiException
{
    public Dictionary<string, string[]> ValidationErrors { get; }

    public ValidationException(Dictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", HttpStatusCode.BadRequest)
    {
        ValidationErrors = errors;
    }

    public ValidationException(string field, string error)
        : base("Validation error occurred.", HttpStatusCode.BadRequest)
    {
        ValidationErrors = new Dictionary<string, string[]>
        {
            { field, new[] { error } }
        };
    }

    public ValidationException(string message, Dictionary<string, string[]> errors)
        : base(message, HttpStatusCode.BadRequest)
    {
        ValidationErrors = errors;
    }
}
