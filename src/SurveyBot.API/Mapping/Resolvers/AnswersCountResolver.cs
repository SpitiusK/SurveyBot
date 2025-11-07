using AutoMapper;
using SurveyBot.Core.Entities;

namespace SurveyBot.API.Mapping.Resolvers;

/// <summary>
/// Resolves the count of answers for a response
/// </summary>
public class AnswersCountResolver : IValueResolver<Response, object, int>
{
    public int Resolve(Response source, object destination, int destMember, ResolutionContext context)
    {
        return source.Answers?.Count ?? 0;
    }
}
