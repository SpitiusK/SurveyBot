using AutoMapper;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using System.Text.Json;

namespace SurveyBot.API.Mapping.ValueResolvers;

/// <summary>
/// Resolves media content from JSON string (CreateQuestionDto.MediaContent) to JSONB database field (Question.MediaContent).
/// Validates and passes through JSON string from frontend to database.
/// The frontend sends mediaContent as a JSON string, and the resolver validates it before storage.
/// </summary>
public class QuestionMediaContentJsonResolver : IValueResolver<CreateQuestionDto, Question, string?>
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string? Resolve(CreateQuestionDto source, Question destination, string? destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.MediaContent))
        {
            return null;
        }

        try
        {
            // Validate it's proper JSON by attempting to deserialize
            JsonSerializer.Deserialize<object>(source.MediaContent, _jsonOptions);

            // If validation succeeds, return the JSON string as-is for storage
            return source.MediaContent;
        }
        catch (JsonException)
        {
            // Invalid JSON, return null instead of failing
            return null;
        }
    }
}

/// <summary>
/// Resolves media content from JSON string (UpdateQuestionDto.MediaContent) to JSONB database field (Question.MediaContent) for updates.
/// Validates and passes through JSON string from frontend to database.
/// The frontend sends mediaContent as a JSON string, and the resolver validates it before storage.
/// </summary>
public class UpdateQuestionMediaContentJsonResolver : IValueResolver<UpdateQuestionDto, Question, string?>
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string? Resolve(UpdateQuestionDto source, Question destination, string? destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.MediaContent))
        {
            return null;
        }

        try
        {
            // Validate it's proper JSON by attempting to deserialize
            JsonSerializer.Deserialize<object>(source.MediaContent, _jsonOptions);

            // If validation succeeds, return the JSON string as-is for storage
            return source.MediaContent;
        }
        catch (JsonException)
        {
            // Invalid JSON, return null instead of failing
            return null;
        }
    }
}
