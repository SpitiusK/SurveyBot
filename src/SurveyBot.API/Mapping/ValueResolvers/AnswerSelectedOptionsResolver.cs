using AutoMapper;
using SurveyBot.Core.Entities;
using SurveyBot.Core.DTOs.Answer;
using System.Text.Json;

namespace SurveyBot.API.Mapping.ValueResolvers;

/// <summary>
/// Resolves answer selected options from JSON string to strongly-typed list.
/// </summary>
public class AnswerSelectedOptionsResolver : IValueResolver<Answer, AnswerDto, List<string>?>
{
    public List<string>? Resolve(Answer source, AnswerDto destination, List<string>? destMember, ResolutionContext context)
    {
        // For text questions, return null
        if (!string.IsNullOrWhiteSpace(source.AnswerText))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(source.AnswerJson))
        {
            return null;
        }

        try
        {
            // Try to deserialize as array of strings (for multiple choice)
            var options = JsonSerializer.Deserialize<List<string>>(source.AnswerJson);
            return options;
        }
        catch (JsonException)
        {
            // If it's not an array, it might be a single string
            try
            {
                var singleOption = JsonSerializer.Deserialize<string>(source.AnswerJson);
                return singleOption != null ? new List<string> { singleOption } : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
