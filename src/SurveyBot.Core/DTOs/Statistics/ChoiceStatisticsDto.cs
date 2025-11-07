namespace SurveyBot.Core.DTOs.Statistics;

/// <summary>
/// Data transfer object for choice option statistics.
/// </summary>
public class ChoiceStatisticsDto
{
    /// <summary>
    /// Gets or sets the option text.
    /// </summary>
    public string Option { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of times this option was selected.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the percentage of respondents who selected this option (0-100).
    /// </summary>
    public double Percentage { get; set; }
}
