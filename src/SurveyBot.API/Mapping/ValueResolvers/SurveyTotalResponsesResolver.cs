using AutoMapper;
using SurveyBot.Core.Entities;
using SurveyBot.Core.DTOs.Survey;

namespace SurveyBot.API.Mapping.ValueResolvers;

/// <summary>
/// Resolves the total number of responses for a survey.
/// </summary>
public class SurveyTotalResponsesResolver : IValueResolver<Survey, SurveyDto, int>
{
    public int Resolve(Survey source, SurveyDto destination, int destMember, ResolutionContext context)
    {
        return source.Responses?.Count ?? 0;
    }
}

/// <summary>
/// Resolves the total number of responses for a survey list item.
/// </summary>
public class SurveyListTotalResponsesResolver : IValueResolver<Survey, SurveyListDto, int>
{
    public int Resolve(Survey source, SurveyListDto destination, int destMember, ResolutionContext context)
    {
        return source.Responses?.Count ?? 0;
    }
}
