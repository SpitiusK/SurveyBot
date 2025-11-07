using AutoMapper;
using SurveyBot.Core.DTOs.Statistics;

namespace SurveyBot.API.Mapping;

/// <summary>
/// AutoMapper profile for statistics DTOs.
/// Note: Statistics DTOs are computed from aggregated data rather than direct entity mappings.
/// This profile is primarily for documentation and potential future mappings.
/// Statistics are typically calculated in service layer using LINQ queries.
/// </summary>
public class StatisticsMappingProfile : Profile
{
    public StatisticsMappingProfile()
    {
        // Statistics DTOs don't have direct entity mappings
        // They are computed from aggregated data in the service layer

        // Example: SurveyStatisticsDto is built by:
        // - Counting responses from Response entity
        // - Calculating completion rates
        // - Aggregating QuestionStatisticsDto for each question

        // Example: QuestionStatisticsDto is built by:
        // - Analyzing Answer entities for a specific question
        // - Computing choice distributions, rating statistics, or text statistics
        // - Calculating response rates and skip counts

        // Example: RatingStatisticsDto is built by:
        // - Parsing AnswerJson from Answer entities
        // - Computing average, median, mode, min, max
        // - Building rating distribution histogram

        // Example: TextStatisticsDto is built by:
        // - Extracting AnswerText from Answer entities
        // - Computing length statistics
        // - Sampling recent answers

        // Example: ChoiceStatisticsDto is built by:
        // - Parsing AnswerJson for choice-based questions
        // - Counting option selections
        // - Computing percentages

        // If direct mappings are needed in the future, they can be added here
        // For now, statistics calculation logic resides in the StatisticsService
    }
}
