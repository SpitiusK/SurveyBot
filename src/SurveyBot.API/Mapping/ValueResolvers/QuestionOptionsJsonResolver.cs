using AutoMapper;
using SurveyBot.Core.Entities;
using SurveyBot.Core.DTOs.Question;
using System.Text.Json;

namespace SurveyBot.API.Mapping.ValueResolvers;

/// <summary>
/// Resolves question options from strongly-typed list to JSON string.
/// </summary>
public class QuestionOptionsJsonResolver : IValueResolver<CreateQuestionDto, Question, string?>
{
    public string? Resolve(CreateQuestionDto source, Question destination, string? destMember, ResolutionContext context)
    {
        if (source.Options == null || source.Options.Count == 0)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Serialize(source.Options);
        }
        catch (JsonException)
        {
            // Log error if needed, return null for serialization failure
            return null;
        }
    }
}

/// <summary>
/// Resolves question options from strongly-typed list to JSON string for updates.
/// </summary>
public class UpdateQuestionOptionsJsonResolver : IValueResolver<UpdateQuestionDto, Question, string?>
{
    public string? Resolve(UpdateQuestionDto source, Question destination, string? destMember, ResolutionContext context)
    {
        if (source.Options == null || source.Options.Count == 0)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Serialize(source.Options);
        }
        catch (JsonException)
        {
            // Log error if needed, return null for serialization failure
            return null;
        }
    }
}
