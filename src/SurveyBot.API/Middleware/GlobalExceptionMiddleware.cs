using System.Net;
using System.Text.Json;
using SurveyBot.API.Exceptions;
using SurveyBot.API.Models;

namespace SurveyBot.API.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions
/// and returns standardized error responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = validationEx.Message;
                errorResponse.Errors = validationEx.ValidationErrors;

                _logger.LogWarning(validationEx,
                    "Validation error occurred. TraceId: {TraceId}. Errors: {@Errors}",
                    context.TraceIdentifier,
                    validationEx.ValidationErrors);
                break;

            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Message = notFoundEx.Message;

                _logger.LogWarning(notFoundEx,
                    "Resource not found. TraceId: {TraceId}. Message: {Message}",
                    context.TraceIdentifier,
                    notFoundEx.Message);
                break;

            case BadRequestException badRequestEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = badRequestEx.Message;
                errorResponse.Details = _environment.IsDevelopment() ? badRequestEx.Details : null;

                _logger.LogWarning(badRequestEx,
                    "Bad request. TraceId: {TraceId}. Message: {Message}",
                    context.TraceIdentifier,
                    badRequestEx.Message);
                break;

            case UnauthorizedException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Message = unauthorizedEx.Message;

                _logger.LogWarning(unauthorizedEx,
                    "Unauthorized access attempt. TraceId: {TraceId}. Path: {Path}",
                    context.TraceIdentifier,
                    context.Request.Path);
                break;

            case ForbiddenException forbiddenEx:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                errorResponse.StatusCode = (int)HttpStatusCode.Forbidden;
                errorResponse.Message = forbiddenEx.Message;

                _logger.LogWarning(forbiddenEx,
                    "Forbidden access attempt. TraceId: {TraceId}. Path: {Path}",
                    context.TraceIdentifier,
                    context.Request.Path);
                break;

            case ConflictException conflictEx:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse.Message = conflictEx.Message;
                errorResponse.Details = _environment.IsDevelopment() ? conflictEx.Details : null;

                _logger.LogWarning(conflictEx,
                    "Conflict occurred. TraceId: {TraceId}. Message: {Message}",
                    context.TraceIdentifier,
                    conflictEx.Message);
                break;

            case ApiException apiEx:
                response.StatusCode = (int)apiEx.StatusCode;
                errorResponse.StatusCode = (int)apiEx.StatusCode;
                errorResponse.Message = apiEx.Message;
                errorResponse.Details = _environment.IsDevelopment() ? apiEx.Details : null;

                _logger.LogError(apiEx,
                    "API exception occurred. TraceId: {TraceId}. StatusCode: {StatusCode}. Message: {Message}",
                    context.TraceIdentifier,
                    apiEx.StatusCode,
                    apiEx.Message);
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = _environment.IsDevelopment()
                    ? exception.Message
                    : "An internal server error occurred.";
                errorResponse.Details = _environment.IsDevelopment()
                    ? exception.StackTrace
                    : null;

                _logger.LogError(exception,
                    "Unhandled exception occurred. TraceId: {TraceId}. Path: {Path}. Method: {Method}",
                    context.TraceIdentifier,
                    context.Request.Path,
                    context.Request.Method);
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var result = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await response.WriteAsync(result);
    }
}
