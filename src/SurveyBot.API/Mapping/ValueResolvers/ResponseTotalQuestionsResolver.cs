using AutoMapper;
using SurveyBot.Core.Entities;
using SurveyBot.Core.DTOs.Response;

namespace SurveyBot.API.Mapping.ValueResolvers;

/// <summary>
/// Resolves the total number of questions in the survey for a response.
/// </summary>
public class ResponseTotalQuestionsResolver : IValueResolver<Response, ResponseDto, int>
{
    public int Resolve(Response source, ResponseDto destination, int destMember, ResolutionContext context)
    {
        return source.Survey?.Questions?.Count ?? 0;
    }
}
