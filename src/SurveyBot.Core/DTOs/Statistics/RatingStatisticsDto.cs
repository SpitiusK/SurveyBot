namespace SurveyBot.Core.DTOs.Statistics;

/// <summary>
/// Data transfer object for rating question statistics.
/// </summary>
public class RatingStatisticsDto
{
    /// <summary>
    /// Gets or sets the average rating value.
    /// </summary>
    public double AverageRating { get; set; }

    /// <summary>
    /// Gets or sets the median rating value.
    /// </summary>
    public double MedianRating { get; set; }

    /// <summary>
    /// Gets or sets the mode (most common) rating value.
    /// </summary>
    public int ModeRating { get; set; }

    /// <summary>
    /// Gets or sets the minimum rating given.
    /// </summary>
    public int MinRating { get; set; }

    /// <summary>
    /// Gets or sets the maximum rating given.
    /// </summary>
    public int MaxRating { get; set; }

    /// <summary>
    /// Gets or sets the distribution of ratings.
    /// Key: rating value (1-5), Value: count and percentage.
    /// </summary>
    public Dictionary<int, RatingDistributionDto> Distribution { get; set; } = new();
}
