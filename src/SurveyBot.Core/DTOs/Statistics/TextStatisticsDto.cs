namespace SurveyBot.Core.DTOs.Statistics;

/// <summary>
/// Data transfer object for text question statistics.
/// </summary>
public class TextStatisticsDto
{
    /// <summary>
    /// Gets or sets the total number of text answers.
    /// </summary>
    public int TotalAnswers { get; set; }

    /// <summary>
    /// Gets or sets the average length of text answers in characters.
    /// </summary>
    public double AverageLength { get; set; }

    /// <summary>
    /// Gets or sets the minimum length of text answers in characters.
    /// </summary>
    public int MinLength { get; set; }

    /// <summary>
    /// Gets or sets the maximum length of text answers in characters.
    /// </summary>
    public int MaxLength { get; set; }

    /// <summary>
    /// Gets or sets a sample of recent text answers (limited to 10).
    /// </summary>
    public List<string> SampleAnswers { get; set; } = new();
}
