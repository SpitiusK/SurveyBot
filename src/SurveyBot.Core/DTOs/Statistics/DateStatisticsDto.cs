namespace SurveyBot.Core.DTOs.Statistics;

/// <summary>
/// Data transfer object for date question statistics.
/// Contains statistical analysis of date answers.
/// </summary>
public class DateStatisticsDto
{
    /// <summary>
    /// Gets or sets the earliest date from all answers.
    /// </summary>
    public DateTime? EarliestDate { get; set; }

    /// <summary>
    /// Gets or sets the latest date from all answers.
    /// </summary>
    public DateTime? LatestDate { get; set; }

    /// <summary>
    /// Gets or sets the date distribution showing frequency of each date.
    /// Sorted by date descending (most recent first).
    /// </summary>
    public List<DateFrequency> DateDistribution { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of answers.
    /// </summary>
    public int Count { get; set; }
}
