namespace SurveyBot.Core.DTOs.Statistics;

/// <summary>
/// Represents a date and its frequency in answers.
/// Used for date distribution statistics.
/// </summary>
public class DateFrequency
{
    /// <summary>
    /// Gets or sets the date value.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the count of answers with this date.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total answers.
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Gets the formatted date string in DD.MM.YYYY format.
    /// </summary>
    public string FormattedDate => Date.ToString("dd.MM.yyyy");
}
