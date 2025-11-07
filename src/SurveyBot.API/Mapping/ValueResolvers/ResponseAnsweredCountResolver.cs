using AutoMapper;
using SurveyBot.Core.Entities;
using SurveyBot.Core.DTOs.Response;

namespace SurveyBot.API.Mapping.ValueResolvers;

/// <summary>
/// Resolves the number of questions answered in a response.
/// </summary>
public class ResponseAnsweredCountResolver : IValueResolver<Response, ResponseDto, int>
{
    public int Resolve(Response source, ResponseDto destination, int destMember, ResolutionContext context)
    {
        return source.Answers?.Count ?? 0;
    }
}
