using SurveyBot.Core.Entities;

namespace SurveyBot.Core.DTOs.Statistics;

/// <summary>
/// Data transfer object for question-level statistics.
/// </summary>
public class QuestionStatisticsDto
{
    /// <summary>
    /// Gets or sets the question ID.
    /// </summary>
    public int QuestionId { get; set; }

    /// <summary>
    /// Gets or sets the question text.
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the question type.
    /// </summary>
    public QuestionType QuestionType { get; set; }

    /// <summary>
    /// Gets or sets the total number of answers to this question.
    /// </summary>
    public int TotalAnswers { get; set; }

    /// <summary>
    /// Gets or sets the number of users who skipped this question.
    /// Only applicable for optional questions.
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Gets or sets the response rate as a percentage (0-100).
    /// </summary>
    public double ResponseRate { get; set; }

    /// <summary>
    /// Gets or sets the distribution of choices for choice-based questions.
    /// Key: option text, Value: count and percentage.
    /// </summary>
    public Dictionary<string, ChoiceStatisticsDto>? ChoiceDistribution { get; set; }

    /// <summary>
    /// Gets or sets rating statistics for rating questions.
    /// </summary>
    public RatingStatisticsDto? RatingStatistics { get; set; }

    /// <summary>
    /// Gets or sets text answer statistics for text questions.
    /// </summary>
    public TextStatisticsDto? TextStatistics { get; set; }

    /// <summary>
    /// Gets or sets number statistics for number questions.
    /// Includes min, max, average, median, sum, and standard deviation.
    /// </summary>
    public NumberStatisticsDto? NumberStatistics { get; set; }

    /// <summary>
    /// Gets or sets date statistics for date questions.
    /// Includes earliest, latest, and date distribution.
    /// </summary>
    public DateStatisticsDto? DateStatistics { get; set; }
}
