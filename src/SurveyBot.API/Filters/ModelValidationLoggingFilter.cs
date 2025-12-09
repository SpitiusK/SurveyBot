using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text;
using System.Text.Json;

namespace SurveyBot.API.Filters;

/// <summary>
/// Action filter that logs detailed model validation errors before ASP.NET Core's
/// [ApiController] attribute returns 400 Bad Request.
///
/// This filter runs AFTER model binding but BEFORE controller action execution,
/// allowing us to inspect ModelState validation errors that cause 400 responses.
/// </summary>
public class ModelValidationLoggingFilter : IActionFilter
{
    private readonly ILogger<ModelValidationLoggingFilter> _logger;

    public ModelValidationLoggingFilter(ILogger<ModelValidationLoggingFilter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes AFTER action method completes. Not used in this filter.
    /// </summary>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Not needed - we only log before action execution
    }

    /// <summary>
    /// Executes BEFORE action method runs. Logs ModelState validation errors if present.
    /// </summary>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Only log if ModelState is invalid (validation failed)
        if (!context.ModelState.IsValid)
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();
            var httpMethod = context.HttpContext.Request.Method;
            var path = context.HttpContext.Request.Path;

            _logger.LogWarning(
                "❌ MODEL VALIDATION FAILED | {HttpMethod} {Path} | Controller: {Controller}, Action: {Action}",
                httpMethod, path, controllerName, actionName);

            // Log all validation errors with details
            LogValidationErrors(context.ModelState, context);

            // Log action parameters (DTOs) for debugging
            LogActionParameters(context);

            // Log the raw request body if available
            LogRawRequestBody(context.HttpContext);

            _logger.LogWarning(
                "⚠️ Returning 400 Bad Request with ApiResponse format");

            // Since we suppressed automatic 400 response, we need to return it manually
            // Return ApiResponse<object> format to match other endpoints
            context.Result = new BadRequestObjectResult(new Models.ApiResponse<object>
            {
                Success = false,
                Message = "Invalid request data",
                Data = context.ModelState
            });
        }
    }

    /// <summary>
    /// Logs all ModelState validation errors with property paths and error messages.
    /// </summary>
    private void LogValidationErrors(ModelStateDictionary modelState, ActionExecutingContext context)
    {
        var errors = modelState
            .Where(ms => ms.Value?.Errors.Count > 0)
            .Select(ms => new
            {
                PropertyPath = ms.Key,
                Errors = ms.Value!.Errors.Select(e => new
                {
                    ErrorMessage = e.ErrorMessage,
                    Exception = e.Exception?.Message,
                    ExceptionType = e.Exception?.GetType().Name
                }).ToList(),
                AttemptedValue = ms.Value.AttemptedValue,
                RawValue = ms.Value.RawValue
            })
            .ToList();

        _logger.LogError(
            "📋 VALIDATION ERRORS ({Count} properties failed):\n{Errors}",
            errors.Count,
            JsonSerializer.Serialize(errors, new JsonSerializerOptions { WriteIndented = true }));

        // Log each error individually for better readability
        foreach (var error in errors)
        {
            _logger.LogError(
                "  🔴 Property: '{PropertyPath}' | Attempted Value: {AttemptedValue} | Raw Value: {RawValue}",
                error.PropertyPath,
                error.AttemptedValue ?? "null",
                error.RawValue ?? "null");

            foreach (var err in error.Errors)
            {
                _logger.LogError(
                    "     └─ Error: {ErrorMessage} {Exception}",
                    err.ErrorMessage,
                    err.Exception != null ? $"| Exception: {err.ExceptionType} - {err.Exception}" : "");
            }
        }
    }

    /// <summary>
    /// Logs the action parameters (DTOs) that were bound from the request.
    /// </summary>
    private void LogActionParameters(ActionExecutingContext context)
    {
        if (context.ActionArguments.Count == 0)
        {
            _logger.LogInformation("📦 No action parameters (empty body or query)");
            return;
        }

        _logger.LogInformation(
            "📦 ACTION PARAMETERS ({Count} parameters bound):",
            context.ActionArguments.Count);

        foreach (var param in context.ActionArguments)
        {
            try
            {
                var paramJson = JsonSerializer.Serialize(param.Value, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    MaxDepth = 10,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                });

                _logger.LogInformation(
                    "  📄 Parameter '{Name}' (Type: {Type}):\n{Json}",
                    param.Key,
                    param.Value?.GetType().Name ?? "null",
                    paramJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "  ⚠️ Could not serialize parameter '{Name}': {Error}",
                    param.Key, ex.Message);
            }
        }
    }

    /// <summary>
    /// Logs the raw request body for debugging (enables buffering if needed).
    /// </summary>
    private void LogRawRequestBody(HttpContext httpContext)
    {
        try
        {
            // Request body can only be read once unless buffering is enabled
            if (!httpContext.Request.Body.CanSeek)
            {
                _logger.LogInformation(
                    "📭 Raw request body: (Not available - buffering not enabled)");
                return;
            }

            httpContext.Request.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = reader.ReadToEndAsync().GetAwaiter().GetResult();
            httpContext.Request.Body.Seek(0, SeekOrigin.Begin);

            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogInformation("📭 Raw request body: (empty)");
            }
            else
            {
                _logger.LogInformation(
                    "📭 RAW REQUEST BODY ({Length} chars):\n{Body}",
                    body.Length, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "⚠️ Could not read raw request body: {Error}",
                ex.Message);
        }
    }
}
