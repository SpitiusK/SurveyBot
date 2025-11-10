namespace SurveyBot.Bot.Models;

/// <summary>
/// Represents a standard API response wrapper.
/// Used for parsing responses from the SurveyBot API.
/// </summary>
/// <typeparam name="T">The type of data in the response.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the API call was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The response data.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Additional metadata about the response.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
