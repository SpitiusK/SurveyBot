namespace SurveyBot.Core.DTOs.Statistics;

/// <summary>
/// Data transfer object for number question statistics.
/// Contains statistical analysis of numeric answers.
/// </summary>
public class NumberStatisticsDto
{
    /// <summary>
    /// Gets or sets the minimum value from all answers.
    /// </summary>
    public decimal Minimum { get; set; }

    /// <summary>
    /// Gets or sets the maximum value from all answers.
    /// </summary>
    public decimal Maximum { get; set; }

    /// <summary>
    /// Gets or sets the average (mean) value of all answers.
    /// </summary>
    public decimal Average { get; set; }

    /// <summary>
    /// Gets or sets the median value of all answers.
    /// </summary>
    public decimal Median { get; set; }

    /// <summary>
    /// Gets or sets the standard deviation of all answers.
    /// </summary>
    public decimal StandardDeviation { get; set; }

    /// <summary>
    /// Gets or sets the total count of answers.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the sum of all answer values.
    /// </summary>
    public decimal Sum { get; set; }
}
