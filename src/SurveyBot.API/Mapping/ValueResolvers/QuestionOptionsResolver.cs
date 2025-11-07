using AutoMapper;
using SurveyBot.Core.Entities;
using SurveyBot.Core.DTOs.Question;
using System.Text.Json;

namespace SurveyBot.API.Mapping.ValueResolvers;

/// <summary>
/// Resolves question options from JSON string to strongly-typed list.
/// </summary>
public class QuestionOptionsResolver : IValueResolver<Question, QuestionDto, List<string>?>
{
    public List<string>? Resolve(Question source, QuestionDto destination, List<string>? destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.OptionsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(source.OptionsJson);
        }
        catch (JsonException)
        {
            // Log error if needed, return null for invalid JSON
            return null;
        }
    }
}
