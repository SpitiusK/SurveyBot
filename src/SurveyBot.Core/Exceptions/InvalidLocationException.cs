namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when a geographic location is invalid.
/// Indicates that latitude or longitude coordinates are out of valid range,
/// or that accuracy values are invalid.
/// </summary>
public class InvalidLocationException : Exception
{
    /// <summary>
    /// Gets the latitude value that was invalid.
    /// </summary>
    public double Latitude { get; }

    /// <summary>
    /// Gets the longitude value that was invalid.
    /// </summary>
    public double Longitude { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidLocationException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public InvalidLocationException(string message)
        : base(message)
    {
        Latitude = 0;
        Longitude = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidLocationException"/> class
    /// with specific latitude and longitude values.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="latitude">The latitude value that was invalid.</param>
    /// <param name="longitude">The longitude value that was invalid.</param>
    public InvalidLocationException(string message, double latitude, double longitude)
        : base(message)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}
