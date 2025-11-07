using System.Text.Json.Serialization;

namespace SurveyBot.API.Models;

/// <summary>
/// Standardized API response wrapper for successful responses
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    /// <summary>
    /// Response data
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    /// <summary>
    /// Optional message
    /// </summary>
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    /// <summary>
    /// Timestamp of the response
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApiResponse()
    {
    }

    public ApiResponse(T data)
    {
        Data = data;
    }

    public ApiResponse(T data, string message)
    {
        Data = data;
        Message = message;
    }

    /// <summary>
    /// Creates a successful response with data
    /// </summary>
    public static ApiResponse<T> Ok(T data) => new(data);

    /// <summary>
    /// Creates a successful response with data and message
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message) => new(data, message);
}

/// <summary>
/// Non-generic API response for operations that don't return data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public ApiResponse() : base()
    {
    }

    public ApiResponse(string message) : base(null!, message)
    {
    }

    /// <summary>
    /// Creates a successful response with just a message
    /// </summary>
    public static ApiResponse Ok(string message) => new(message);
}
