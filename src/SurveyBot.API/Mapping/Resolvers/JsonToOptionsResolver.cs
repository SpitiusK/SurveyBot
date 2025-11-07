using AutoMapper;
using SurveyBot.Core.Entities;
using System.Text.Json;

namespace SurveyBot.API.Mapping.Resolvers;

/// <summary>
/// Resolves JSON string options to strongly-typed list of strings
/// </summary>
public class JsonToOptionsResolver : IValueResolver<Question, object, List<string>?>
{
    public List<string>? Resolve(Question source, object destination, List<string>? destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.Options))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(source.Options);
        }
        catch (JsonException)
        {
            // If deserialization fails, return null
            return null;
        }
    }
}
