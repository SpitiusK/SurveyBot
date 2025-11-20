using AutoMapper;
using SurveyBot.Core.DTOs.Media;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using System.Text.Json;

namespace SurveyBot.API.Mapping.ValueResolvers;

/// <summary>
/// Resolves media content from JSON string (Question.MediaContent) to strongly-typed DTO (QuestionDto.MediaContent).
/// Handles deserialization from JSONB database field to MediaContentDto.
/// </summary>
public class QuestionMediaContentResolver : IValueResolver<Question, QuestionDto, MediaContentDto?>
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MediaContentDto? Resolve(Question source, QuestionDto destination, MediaContentDto? destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.MediaContent))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<MediaContentDto>(source.MediaContent, _jsonOptions);
        }
        catch (JsonException)
        {
            // Log error if needed, return null for invalid JSON
            return null;
        }
    }
}
