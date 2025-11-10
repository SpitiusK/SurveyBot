using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SurveyBot.Bot.Services;

/// <summary>
/// Handles API errors and provides consistent error handling for HTTP requests.
/// Wraps HTTP client calls with retry logic, timeout handling, and error logging.
/// </summary>
public class ApiErrorHandler
{
    private readonly ILogger<ApiErrorHandler> _logger;

    public ApiErrorHandler(ILogger<ApiErrorHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an HTTP request with comprehensive error handling.
    /// </summary>
    public async Task<ApiResult<T>> ExecuteAsync<T>(
        Func<Task<HttpResponseMessage>> request,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Executing API request: {OperationName}", operationName);

            var response = await request();

            // Success case
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);

                    _logger.LogDebug("API request successful: {OperationName}", operationName);

                    return ApiResult<T>.Success(data!);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize API response for {OperationName}", operationName);
                    return ApiResult<T>.Failure(
                        "Failed to process server response.",
                        (int)response.StatusCode);
                }
            }

            // Handle error responses
            return await HandleErrorResponseAsync<T>(response, operationName, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for {OperationName}: {Message}",
                operationName, ex.Message);

            return ApiResult<T>.Failure(
                "Failed to connect to the server. Please check your connection.",
                errorType: ApiErrorType.NetworkError);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "API request timed out for {OperationName}", operationName);

            return ApiResult<T>.Failure(
                "The request timed out. Please try again.",
                errorType: ApiErrorType.Timeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during API request for {OperationName}", operationName);

            return ApiResult<T>.Failure(
                "An unexpected error occurred. Please try again.",
                errorType: ApiErrorType.UnknownError);
        }
    }

    /// <summary>
    /// Handles HTTP error responses with detailed logging and user-friendly messages.
    /// </summary>
    private async Task<ApiResult<T>> HandleErrorResponseAsync<T>(
        HttpResponseMessage response,
        string operationName,
        CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        string? errorDetails = null;

        try
        {
            errorDetails = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            // Ignore if we can't read error details
        }

        _logger.LogWarning(
            "API request failed for {OperationName}: StatusCode={StatusCode}, Details={Details}",
            operationName,
            statusCode,
            errorDetails);

        // Map status codes to user-friendly messages
        var (message, errorType) = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => (
                "Invalid request. Please try again.",
                ApiErrorType.ValidationError
            ),
            HttpStatusCode.Unauthorized => (
                "Authentication failed. Please restart the bot.",
                ApiErrorType.AuthenticationError
            ),
            HttpStatusCode.Forbidden => (
                "You don't have permission to perform this action.",
                ApiErrorType.AuthorizationError
            ),
            HttpStatusCode.NotFound => (
                "The requested resource was not found. The survey may have been deleted.",
                ApiErrorType.NotFoundError
            ),
            HttpStatusCode.Conflict => (
                "This action conflicts with existing data. You may have already submitted a response.",
                ApiErrorType.ConflictError
            ),
            HttpStatusCode.InternalServerError => (
                "Server error occurred. Please try again later.",
                ApiErrorType.ServerError
            ),
            HttpStatusCode.ServiceUnavailable => (
                "Service temporarily unavailable. Please try again later.",
                ApiErrorType.ServerError
            ),
            _ => (
                $"Request failed with status {statusCode}. Please try again.",
                ApiErrorType.UnknownError
            )
        };

        return ApiResult<T>.Failure(message, statusCode, errorType, errorDetails);
    }

    /// <summary>
    /// Validates that a response exists and handles consistency errors.
    /// </summary>
    public async Task<bool> ValidateResponseExistsAsync(
        long chatId,
        int? responseId,
        QuestionErrorHandler errorHandler,
        CancellationToken cancellationToken = default)
    {
        if (!responseId.HasValue)
        {
            await errorHandler.ShowDataConsistencyErrorAsync(
                chatId,
                "No active response found. The survey session may have expired.",
                cancellationToken);

            _logger.LogWarning("Response validation failed: No response ID for chat {ChatId}", chatId);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that a survey/question still exists and belongs to the correct survey.
    /// </summary>
    public async Task<bool> ValidateDataConsistencyAsync(
        long chatId,
        int? surveyId,
        int? currentSurveyId,
        QuestionErrorHandler errorHandler,
        CancellationToken cancellationToken = default)
    {
        if (!surveyId.HasValue || !currentSurveyId.HasValue)
        {
            await errorHandler.ShowDataConsistencyErrorAsync(
                chatId,
                "Survey data is missing. Please restart the survey.",
                cancellationToken);

            _logger.LogWarning("Data consistency validation failed: Missing survey IDs");
            return false;
        }

        if (surveyId.Value != currentSurveyId.Value)
        {
            await errorHandler.ShowDataConsistencyErrorAsync(
                chatId,
                "The survey data has changed. This question may belong to a different survey.",
                cancellationToken);

            _logger.LogWarning(
                "Data consistency validation failed: Survey ID mismatch ({SurveyId} != {CurrentSurveyId})",
                surveyId,
                currentSurveyId);

            return false;
        }

        return true;
    }
}

/// <summary>
/// Represents the result of an API call with success/failure status.
/// </summary>
public class ApiResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int? StatusCode { get; set; }
    public ApiErrorType ErrorType { get; set; }
    public string? ErrorDetails { get; set; }

    public static ApiResult<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data,
        ErrorType = ApiErrorType.None
    };

    public static ApiResult<T> Failure(
        string errorMessage,
        int? statusCode = null,
        ApiErrorType errorType = ApiErrorType.UnknownError,
        string? errorDetails = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        StatusCode = statusCode,
        ErrorType = errorType,
        ErrorDetails = errorDetails
    };
}

/// <summary>
/// Types of API errors for categorization and handling.
/// </summary>
public enum ApiErrorType
{
    None = 0,
    NetworkError = 1,
    Timeout = 2,
    ValidationError = 3,
    AuthenticationError = 4,
    AuthorizationError = 5,
    NotFoundError = 6,
    ConflictError = 7,
    ServerError = 8,
    UnknownError = 9
}
