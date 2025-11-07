using AutoMapper;
using SurveyBot.Core.Entities;
using SurveyBot.Core.DTOs.Survey;

namespace SurveyBot.API.Mapping.ValueResolvers;

/// <summary>
/// Resolves the number of completed responses for a survey.
/// </summary>
public class SurveyCompletedResponsesResolver : IValueResolver<Survey, SurveyDto, int>
{
    public int Resolve(Survey source, SurveyDto destination, int destMember, ResolutionContext context)
    {
        return source.Responses?.Count(r => r.IsComplete) ?? 0;
    }
}

/// <summary>
/// Resolves the number of completed responses for a survey list item.
/// </summary>
public class SurveyListCompletedResponsesResolver : IValueResolver<Survey, SurveyListDto, int>
{
    public int Resolve(Survey source, SurveyListDto destination, int destMember, ResolutionContext context)
    {
        return source.Responses?.Count(r => r.IsComplete) ?? 0;
    }
}
