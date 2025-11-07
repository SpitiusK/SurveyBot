using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.Question;

/// <summary>
/// Data transfer object for reordering questions within a survey.
/// </summary>
public class ReorderQuestionsDto
{
    /// <summary>
    /// Gets or sets the list of question IDs in their new order.
    /// </summary>
    [Required(ErrorMessage = "Question order is required")]
    [MinLength(1, ErrorMessage = "At least one question ID is required")]
    public List<int> QuestionIds { get; set; } = new();
}
