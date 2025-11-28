using System.Text.Json;
using System.Text.Json.Serialization;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Value object representing a geographic location answer.
/// Immutable with value semantics.
/// </summary>
public sealed class LocationAnswerValue : AnswerValue
{
    /// <summary>
    /// Minimum valid latitude value (South Pole).
    /// </summary>
    public const double MinLatitude = -90.0;

    /// <summary>
    /// Maximum valid latitude value (North Pole).
    /// </summary>
    public const double MaxLatitude = 90.0;

    /// <summary>
    /// Minimum valid longitude value (International Date Line west).
    /// </summary>
    public const double MinLongitude = -180.0;

    /// <summary>
    /// Maximum valid longitude value (International Date Line east).
    /// </summary>
    public const double MaxLongitude = 180.0;

    /// <summary>
    /// Gets the latitude coordinate in decimal degrees.
    /// </summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; private set; }

    /// <summary>
    /// Gets the longitude coordinate in decimal degrees.
    /// </summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; private set; }

    /// <summary>
    /// Gets the accuracy radius in meters (optional).
    /// Indicates the horizontal accuracy of the location.
    /// </summary>
    [JsonPropertyName("accuracy")]
    public double? Accuracy { get; private set; }

    /// <summary>
    /// Gets the timestamp when the location was captured (optional).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; private set; }

    /// <inheritdoc />
    [JsonIgnore]
    public override QuestionType QuestionType => QuestionType.Location;

    /// <inheritdoc />
    [JsonIgnore]
    public override string DisplayValue => $"{Latitude:F6}, {Longitude:F6}";

    /// <summary>
    /// Private constructor - use Create() factory method.
    /// </summary>
    private LocationAnswerValue(double latitude, double longitude, double? accuracy, DateTime? timestamp)
    {
        Latitude = latitude;
        Longitude = longitude;
        Accuracy = accuracy;
        Timestamp = timestamp;
    }

    /// <summary>
    /// JSON constructor for deserialization.
    /// </summary>
    [JsonConstructor]
    private LocationAnswerValue() : this(0.0, 0.0, null, null)
    {
    }

    /// <summary>
    /// Creates a new location answer with validation.
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees (-90 to 90)</param>
    /// <param name="longitude">Longitude in decimal degrees (-180 to 180)</param>
    /// <param name="accuracy">Accuracy radius in meters (optional, must be >= 0 if provided)</param>
    /// <param name="timestamp">Timestamp when location was captured (optional)</param>
    /// <returns>Validated location answer instance</returns>
    /// <exception cref="InvalidLocationException">If coordinates or accuracy are invalid</exception>
    public static LocationAnswerValue Create(
        double latitude,
        double longitude,
        double? accuracy = null,
        DateTime? timestamp = null)
    {
        // Validate latitude
        if (latitude < MinLatitude || latitude > MaxLatitude)
            throw new InvalidLocationException(
                $"Latitude must be between {MinLatitude} and {MaxLatitude}, got {latitude}",
                latitude,
                longitude);

        // Validate longitude
        if (longitude < MinLongitude || longitude > MaxLongitude)
            throw new InvalidLocationException(
                $"Longitude must be between {MinLongitude} and {MaxLongitude}, got {longitude}",
                latitude,
                longitude);

        // Validate accuracy if provided
        if (accuracy.HasValue && accuracy.Value < 0)
            throw new InvalidLocationException(
                $"Accuracy must be >= 0 if provided, got {accuracy.Value}",
                latitude,
                longitude);

        return new LocationAnswerValue(latitude, longitude, accuracy, timestamp);
    }

    /// <summary>
    /// Parses location answer from JSON storage format.
    /// </summary>
    /// <param name="json">JSON string from database</param>
    /// <returns>Parsed location answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If JSON is invalid</exception>
    public static LocationAnswerValue FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAnswerFormatException(0, QuestionType.Location, "JSON cannot be empty");

        try
        {
            var data = JsonSerializer.Deserialize<LocationData>(json);

            if (data == null)
                throw new InvalidAnswerFormatException(0, QuestionType.Location, "Invalid JSON format");

            return Create(data.Latitude, data.Longitude, data.Accuracy, data.Timestamp);
        }
        catch (JsonException ex)
        {
            throw new InvalidAnswerFormatException(
                $"Invalid JSON for location answer: {ex.Message}");
        }
        catch (InvalidLocationException ex)
        {
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Location,
                $"Invalid location data: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public override string ToJson() =>
        JsonSerializer.Serialize(new LocationData
        {
            Latitude = Latitude,
            Longitude = Longitude,
            Accuracy = Accuracy,
            Timestamp = Timestamp
        });

    /// <inheritdoc />
    public override bool IsValidFor(Question question) =>
        question.QuestionType == QuestionType.Location;

    #region Equality

    /// <inheritdoc />
    public override bool Equals(AnswerValue? other)
    {
        if (other is not LocationAnswerValue location)
            return false;

        // Compare coordinates with epsilon for floating-point comparison
        const double epsilon = 0.000001; // ~0.1 meters at equator
        return Math.Abs(Latitude - location.Latitude) < epsilon &&
               Math.Abs(Longitude - location.Longitude) < epsilon;
    }

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(QuestionType, Latitude, Longitude);

    #endregion

    /// <inheritdoc />
    public override string ToString()
    {
        var accuracyStr = Accuracy.HasValue ? $", ±{Accuracy.Value:F1}m" : "";
        var timestampStr = Timestamp.HasValue ? $", {Timestamp.Value:yyyy-MM-dd HH:mm:ss}" : "";
        return $"Location: {Latitude:F6}, {Longitude:F6}{accuracyStr}{timestampStr}";
    }

    /// <summary>
    /// Internal DTO for JSON serialization.
    /// </summary>
    private sealed class LocationData
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("accuracy")]
        public double? Accuracy { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }
    }
}
