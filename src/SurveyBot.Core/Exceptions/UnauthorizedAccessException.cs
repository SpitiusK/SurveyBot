namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to access or modify a resource they don't own.
/// </summary>
public class UnauthorizedAccessException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedAccessException"/> class.
    /// </summary>
    /// <param name="userId">The ID of the user attempting access.</param>
    /// <param name="resourceType">The type of resource being accessed.</param>
    /// <param name="resourceId">The ID of the resource.</param>
    public UnauthorizedAccessException(int userId, string resourceType, int resourceId)
        : base($"User {userId} is not authorized to access {resourceType} with ID {resourceId}.")
    {
        UserId = userId;
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedAccessException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UnauthorizedAccessException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Gets the ID of the user attempting access.
    /// </summary>
    public int? UserId { get; }

    /// <summary>
    /// Gets the type of resource being accessed.
    /// </summary>
    public string? ResourceType { get; }

    /// <summary>
    /// Gets the ID of the resource.
    /// </summary>
    public int? ResourceId { get; }
}
