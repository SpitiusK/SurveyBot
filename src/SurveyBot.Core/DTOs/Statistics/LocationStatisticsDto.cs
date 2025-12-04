using System;
using System.Collections.Generic;

namespace SurveyBot.Core.DTOs.Statistics;

/// <summary>
/// Statistics for location-type questions.
/// Contains all location data points and geographic bounds for map visualization.
/// </summary>
public class LocationStatisticsDto
{
    /// <summary>
    /// Total number of location responses.
    /// </summary>
    public int TotalLocations { get; set; }

    /// <summary>
    /// Minimum latitude across all locations (south bound).
    /// </summary>
    public double? MinLatitude { get; set; }

    /// <summary>
    /// Maximum latitude across all locations (north bound).
    /// </summary>
    public double? MaxLatitude { get; set; }

    /// <summary>
    /// Minimum longitude across all locations (west bound).
    /// </summary>
    public double? MinLongitude { get; set; }

    /// <summary>
    /// Maximum longitude across all locations (east bound).
    /// </summary>
    public double? MaxLongitude { get; set; }

    /// <summary>
    /// Geographic center latitude (average of all locations).
    /// </summary>
    public double? CenterLatitude { get; set; }

    /// <summary>
    /// Geographic center longitude (average of all locations).
    /// </summary>
    public double? CenterLongitude { get; set; }

    /// <summary>
    /// Individual location data points for map markers.
    /// </summary>
    public List<LocationDataPointDto> Locations { get; set; } = new();
}

/// <summary>
/// Individual location data point for a single response.
/// </summary>
public class LocationDataPointDto
{
    /// <summary>
    /// Latitude in decimal degrees (-90 to 90).
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude in decimal degrees (-180 to 180).
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Accuracy radius in meters (optional).
    /// </summary>
    public double? Accuracy { get; set; }

    /// <summary>
    /// Timestamp when location was captured (optional).
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Response ID for linking to response details.
    /// </summary>
    public int ResponseId { get; set; }
}
