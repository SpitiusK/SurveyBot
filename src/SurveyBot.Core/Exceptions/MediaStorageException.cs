namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when a media storage operation fails.
/// Includes file path context for debugging and logging.
/// </summary>
public class MediaStorageException : Exception
{
    /// <summary>
    /// Gets the file path related to the storage operation that failed.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Initializes a new instance of the MediaStorageException class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public MediaStorageException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the MediaStorageException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MediaStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the MediaStorageException class with a specified error message,
    /// file path context, and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="filePath">The file path related to the failed operation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MediaStorageException(string message, string filePath, Exception innerException)
        : base(message, innerException)
    {
        FilePath = filePath;
    }
}
