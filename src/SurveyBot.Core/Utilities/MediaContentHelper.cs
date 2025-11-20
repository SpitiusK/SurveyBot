using System.Text.Json;
using SurveyBot.Core.DTOs.Media;

namespace SurveyBot.Core.Utilities;

/// <summary>
/// Helper class for MediaContent operations including serialization, validation, and manipulation.
/// Provides utilities for managing MediaContentDto and MediaItemDto objects stored in Question.MediaContent JSONB field.
/// </summary>
public static class MediaContentHelper
{
    /// <summary>
    /// Current schema version for MediaContent structure.
    /// Used for schema evolution and backward compatibility.
    /// </summary>
    private const string CurrentVersion = "1.0";

    /// <summary>
    /// JSON serializer options with camelCase naming convention.
    /// Ensures consistent serialization format for database storage.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes MediaContentDto to JSON string for database storage.
    /// Uses camelCase naming convention for consistency with API responses.
    /// </summary>
    /// <param name="mediaContent">The MediaContentDto object to serialize.</param>
    /// <returns>JSON string representation of the MediaContent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when mediaContent is null.</exception>
    /// <exception cref="JsonException">Thrown when serialization fails.</exception>
    /// <example>
    /// <code>
    /// var content = new MediaContentDto
    /// {
    ///     Version = "1.0",
    ///     Items = new List&lt;MediaItemDto&gt; { mediaItem }
    /// };
    /// string json = MediaContentHelper.SerializeMediaContent(content);
    /// // Result: {"version":"1.0","items":[...]}
    /// </code>
    /// </example>
    public static string SerializeMediaContent(MediaContentDto mediaContent)
    {
        if (mediaContent == null)
            throw new ArgumentNullException(nameof(mediaContent));

        return JsonSerializer.Serialize(mediaContent, JsonOptions);
    }

    /// <summary>
    /// Deserializes JSON string to MediaContentDto.
    /// Handles null and invalid JSON gracefully by returning null instead of throwing exceptions.
    /// </summary>
    /// <param name="jsonContent">JSON string to deserialize. Can be null or empty.</param>
    /// <returns>Deserialized MediaContentDto object, or null if input is null/empty/invalid.</returns>
    /// <remarks>
    /// This method never throws exceptions - invalid JSON or parse errors return null.
    /// This is intentional to handle database records with corrupted or legacy data gracefully.
    /// </remarks>
    /// <example>
    /// <code>
    /// string json = "{\"version\":\"1.0\",\"items\":[]}";
    /// var content = MediaContentHelper.DeserializeMediaContent(json);
    /// if (content != null) { /* Use content */ }
    /// </code>
    /// </example>
    public static MediaContentDto? DeserializeMediaContent(string? jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return null;

        try
        {
            return JsonSerializer.Deserialize<MediaContentDto>(jsonContent, JsonOptions);
        }
        catch (JsonException)
        {
            // Invalid JSON - return null gracefully
            // In production, you may want to log this for monitoring
            return null;
        }
    }

    /// <summary>
    /// Validates MediaContent structure and schema version.
    /// Checks version compatibility, items array existence, and each item's required fields.
    /// </summary>
    /// <param name="mediaContent">The MediaContentDto to validate.</param>
    /// <param name="errorMessage">Outputs detailed error message if validation fails.</param>
    /// <returns>True if MediaContent is valid, otherwise false.</returns>
    /// <remarks>
    /// Validation includes:
    /// - Non-null mediaContent
    /// - Version is "1.0" (current schema version)
    /// - Items array exists (can be empty)
    /// - Each item has ID, Type, FilePath, DisplayName, MimeType
    /// - Each item has valid FileSize > 0
    /// - Each item has valid Order >= 0
    /// </remarks>
    /// <example>
    /// <code>
    /// if (!MediaContentHelper.ValidateMediaContent(content, out string? error))
    /// {
    ///     throw new ValidationException(error);
    /// }
    /// </code>
    /// </example>
    public static bool ValidateMediaContent(MediaContentDto? mediaContent, out string? errorMessage)
    {
        errorMessage = null;

        if (mediaContent == null)
        {
            errorMessage = "MediaContent cannot be null.";
            return false;
        }

        // Check version
        if (string.IsNullOrWhiteSpace(mediaContent.Version))
        {
            errorMessage = "MediaContent version is required.";
            return false;
        }

        if (mediaContent.Version != CurrentVersion)
        {
            errorMessage = $"MediaContent version '{mediaContent.Version}' is not supported. Expected version: '{CurrentVersion}'.";
            return false;
        }

        // Check items array exists
        if (mediaContent.Items == null)
        {
            errorMessage = "MediaContent items collection cannot be null.";
            return false;
        }

        // Validate each media item
        for (int i = 0; i < mediaContent.Items.Count; i++)
        {
            var item = mediaContent.Items[i];

            if (string.IsNullOrWhiteSpace(item.Id))
            {
                errorMessage = $"MediaItem at index {i} has missing or empty ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.Type))
            {
                errorMessage = $"MediaItem at index {i} has missing or empty Type.";
                return false;
            }

            if (!IsValidMediaType(item.Type))
            {
                errorMessage = $"MediaItem at index {i} has invalid Type '{item.Type}'. Expected: image, video, audio, or document.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.FilePath))
            {
                errorMessage = $"MediaItem at index {i} has missing or empty FilePath.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.DisplayName))
            {
                errorMessage = $"MediaItem at index {i} has missing or empty DisplayName.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.MimeType))
            {
                errorMessage = $"MediaItem at index {i} has missing or empty MimeType.";
                return false;
            }

            if (item.FileSize <= 0)
            {
                errorMessage = $"MediaItem at index {i} has invalid FileSize ({item.FileSize}). FileSize must be greater than 0.";
                return false;
            }

            if (item.Order < 0)
            {
                errorMessage = $"MediaItem at index {i} has invalid Order ({item.Order}). Order must be non-negative.";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Adds a new media item to existing MediaContent.
    /// If existingContent is null, creates new MediaContentDto with version "1.0".
    /// Automatically sets the order number for the new item.
    /// </summary>
    /// <param name="existingContent">Existing MediaContentDto, or null to create new.</param>
    /// <param name="newItem">New MediaItemDto to add.</param>
    /// <returns>Updated MediaContentDto with new item appended.</returns>
    /// <exception cref="ArgumentNullException">Thrown when newItem is null.</exception>
    /// <remarks>
    /// The new item's Order property will be automatically set to the next available order number
    /// (0 if items list is empty, or max existing order + 1).
    /// </remarks>
    /// <example>
    /// <code>
    /// var newItem = new MediaItemDto
    /// {
    ///     Id = Guid.NewGuid().ToString(),
    ///     Type = "image",
    ///     FilePath = "/media/image.jpg",
    ///     DisplayName = "Sample Image",
    ///     FileSize = 12345,
    ///     MimeType = "image/jpeg",
    ///     UploadedAt = DateTime.UtcNow
    /// };
    /// var updatedContent = MediaContentHelper.AddMediaItem(existingContent, newItem);
    /// </code>
    /// </example>
    public static MediaContentDto AddMediaItem(MediaContentDto? existingContent, MediaItemDto newItem)
    {
        if (newItem == null)
            throw new ArgumentNullException(nameof(newItem));

        // Create new MediaContentDto if null
        if (existingContent == null)
        {
            existingContent = new MediaContentDto
            {
                Version = CurrentVersion,
                Items = new List<MediaItemDto>()
            };
        }

        // Ensure Items collection exists
        if (existingContent.Items == null)
        {
            existingContent.Items = new List<MediaItemDto>();
        }

        // Set order for new item (next available order number)
        if (existingContent.Items.Count == 0)
        {
            newItem.Order = 0;
        }
        else
        {
            // Get max order and add 1
            var maxOrder = existingContent.Items.Max(i => i.Order);
            newItem.Order = maxOrder + 1;
        }

        // Add new item
        existingContent.Items.Add(newItem);

        return existingContent;
    }

    /// <summary>
    /// Removes a media item from MediaContent by its ID.
    /// Returns null if no items remain after removal.
    /// </summary>
    /// <param name="mediaContent">The MediaContentDto containing items.</param>
    /// <param name="mediaItemId">The ID of the media item to remove.</param>
    /// <returns>Updated MediaContentDto without the removed item, or null if no items remain.</returns>
    /// <remarks>
    /// This method does NOT reorder remaining items - order numbers may have gaps after removal.
    /// If you need sequential order numbers, call ReorderMediaItems after removal.
    /// </remarks>
    /// <example>
    /// <code>
    /// var updatedContent = MediaContentHelper.RemoveMediaItem(content, "item-id-to-remove");
    /// if (updatedContent == null)
    /// {
    ///     // No media items left - you may want to set Question.MediaContent to null
    /// }
    /// </code>
    /// </example>
    public static MediaContentDto? RemoveMediaItem(MediaContentDto? mediaContent, string mediaItemId)
    {
        if (mediaContent == null)
            return null;

        if (string.IsNullOrWhiteSpace(mediaItemId))
            return mediaContent;

        if (mediaContent.Items == null || mediaContent.Items.Count == 0)
            return null;

        // Remove item by ID
        var itemToRemove = mediaContent.Items.FirstOrDefault(i => i.Id == mediaItemId);
        if (itemToRemove != null)
        {
            mediaContent.Items.Remove(itemToRemove);
        }

        // If no items remain, return null
        if (mediaContent.Items.Count == 0)
            return null;

        return mediaContent;
    }

    /// <summary>
    /// Reorders media items sequentially starting from 0.
    /// Useful after removing items to eliminate gaps in order numbers.
    /// </summary>
    /// <param name="mediaContent">The MediaContentDto to reorder.</param>
    /// <returns>The same MediaContentDto with updated order numbers.</returns>
    /// <remarks>
    /// Items are reordered in their current list order.
    /// If you need custom ordering, reorder the Items list first, then call this method.
    /// </remarks>
    /// <example>
    /// <code>
    /// // After removing items, reorder to eliminate gaps
    /// mediaContent = MediaContentHelper.ReorderMediaItems(mediaContent);
    /// // Orders will now be 0, 1, 2, 3... with no gaps
    /// </code>
    /// </example>
    public static MediaContentDto? ReorderMediaItems(MediaContentDto? mediaContent)
    {
        if (mediaContent == null || mediaContent.Items == null || mediaContent.Items.Count == 0)
            return mediaContent;

        for (int i = 0; i < mediaContent.Items.Count; i++)
        {
            mediaContent.Items[i].Order = i;
        }

        return mediaContent;
    }

    /// <summary>
    /// Validates if a media type string is one of the supported types.
    /// </summary>
    /// <param name="type">The media type string to validate.</param>
    /// <returns>True if type is image, video, audio, or document (case-sensitive).</returns>
    private static bool IsValidMediaType(string type)
    {
        return type == "image" || type == "video" || type == "audio" || type == "document";
    }
}
