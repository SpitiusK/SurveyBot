using AutoMapper;
using SurveyBot.Core.Entities;
using SurveyBot.Core.DTOs.Answer;
using System.Text.Json;

namespace SurveyBot.API.Mapping.ValueResolvers;

/// <summary>
/// Resolves answer rating value from JSON string to integer.
/// </summary>
public class AnswerRatingValueResolver : IValueResolver<Answer, AnswerDto, int?>
{
    public int? Resolve(Answer source, AnswerDto destination, int? destMember, ResolutionContext context)
    {
        // Only applicable for rating questions
        if (source.Question?.QuestionType != QuestionType.Rating)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(source.AnswerJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<int>(source.AnswerJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
