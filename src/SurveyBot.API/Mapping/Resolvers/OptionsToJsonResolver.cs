using AutoMapper;
using System.Text.Json;

namespace SurveyBot.API.Mapping.Resolvers;

/// <summary>
/// Resolves list of string options to JSON string for storage
/// </summary>
public class OptionsToJsonResolver : IValueResolver<object, object, string?>
{
    public string? Resolve(object source, object destination, string? destMember, ResolutionContext context)
    {
        // Get the Options property from the source
        var optionsProperty = source.GetType().GetProperty("Options");
        if (optionsProperty == null)
        {
            return null;
        }

        var options = optionsProperty.GetValue(source) as List<string>;

        if (options == null || options.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(options);
    }
}
