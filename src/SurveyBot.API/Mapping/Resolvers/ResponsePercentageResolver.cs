using AutoMapper;
using SurveyBot.Core.Entities;

namespace SurveyBot.API.Mapping.Resolvers;

/// <summary>
/// Calculates completion percentage for a response
/// </summary>
public class ResponsePercentageResolver : IValueResolver<Response, object, decimal>
{
    public decimal Resolve(Response source, object destination, decimal destMember, ResolutionContext context)
    {
        if (source.Survey?.Questions == null || source.Survey.Questions.Count == 0)
        {
            return 0;
        }

        var answeredCount = source.Answers?.Count ?? 0;
        var totalQuestions = source.Survey.Questions.Count;

        return totalQuestions > 0
            ? Math.Round((decimal)answeredCount / totalQuestions * 100, 2)
            : 0;
    }
}
