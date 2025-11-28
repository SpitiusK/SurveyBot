namespace SurveyBot.Core.Entities;

/// <summary>
/// Base entity class providing common properties for all entities.
/// Follows DDD principles with private setters for encapsulation.
/// EF Core can still set properties via reflection.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets the unique identifier for the entity.
    /// Set by EF Core during persistence.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Gets the date and time when the entity was created.
    /// Automatically set during entity creation.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the date and time when the entity was last updated.
    /// Automatically updated when entity is modified.
    /// </summary>
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Protected constructor for EF Core.
    /// </summary>
    protected BaseEntity()
    {
    }

    /// <summary>
    /// Marks the entity as modified by updating the UpdatedAt timestamp.
    /// Call this method when making changes to the entity.
    /// </summary>
    protected void MarkAsModified()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes timestamps for a new entity.
    /// Called by derived entity factory methods.
    /// </summary>
    protected void InitializeTimestamps()
    {
        var now = DateTime.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Sets the entity ID. Internal use only for special cases like testing.
    /// </summary>
    /// <param name="id">The ID to set.</param>
    internal void SetId(int id)
    {
        Id = id;
    }

    /// <summary>
    /// Sets the CreatedAt timestamp. Internal use only for testing.
    /// </summary>
    /// <param name="createdAt">The timestamp to set.</param>
    internal void SetCreatedAt(DateTime createdAt)
    {
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Sets the UpdatedAt timestamp. Internal use only for testing.
    /// </summary>
    /// <param name="updatedAt">The timestamp to set.</param>
    internal void SetUpdatedAt(DateTime updatedAt)
    {
        UpdatedAt = updatedAt;
    }
}
