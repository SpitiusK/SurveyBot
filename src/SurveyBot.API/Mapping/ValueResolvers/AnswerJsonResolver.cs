using AutoMapper;
using SurveyBot.Core.Entities;
using SurveyBot.Core.DTOs.Answer;
using System.Text.Json;

namespace SurveyBot.API.Mapping.ValueResolvers;

/// <summary>
/// Resolves answer from DTO to JSON string for storage.
/// </summary>
public class AnswerJsonResolver : IValueResolver<CreateAnswerDto, Answer, string?>
{
    public string? Resolve(CreateAnswerDto source, Answer destination, string? destMember, ResolutionContext context)
    {
        // For text answers, don't store in JSON
        if (!string.IsNullOrWhiteSpace(source.AnswerText))
        {
            return null;
        }

        // For choice-based questions, serialize selected options
        if (source.SelectedOptions != null && source.SelectedOptions.Count > 0)
        {
            try
            {
                return JsonSerializer.Serialize(source.SelectedOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // For rating questions, serialize rating value
        if (source.RatingValue.HasValue)
        {
            try
            {
                return JsonSerializer.Serialize(source.RatingValue.Value);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        return null;
    }
}
