namespace SurveyBot.Core.DTOs.Statistics;

/// <summary>
/// Data transfer object for rating value distribution.
/// </summary>
public class RatingDistributionDto
{
    /// <summary>
    /// Gets or sets the rating value (1-5).
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Gets or sets the number of times this rating was given.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the percentage of respondents who gave this rating (0-100).
    /// </summary>
    public double Percentage { get; set; }
}
