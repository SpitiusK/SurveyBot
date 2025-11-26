using SurveyBot.Core.DTOs;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Core.Enums;

namespace SurveyBot.Core.Extensions;

/// <summary>
/// Extension methods for converting between NextQuestionDeterminant (value object)
/// and NextQuestionDeterminantDto (DTO) representations.
/// Enables clean separation between domain layer and API layer.
/// </summary>
public static class NextQuestionDeterminantExtensions
{
    /// <summary>
    /// Converts a NextQuestionDeterminantDto to a NextQuestionDeterminant value object.
    /// </summary>
    /// <param name="dto">The DTO to convert. Can be null.</param>
    /// <returns>
    /// NextQuestionDeterminant value object, or null if dto is null.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the DTO contains invalid data (e.g., GoToQuestion with invalid ID).
    /// </exception>
    public static NextQuestionDeterminant? ToValueObject(this NextQuestionDeterminantDto? dto)
    {
        if (dto == null)
        {
            return null;
        }

        // Validate DTO before conversion
        dto.Validate();

        return dto.Type switch
        {
            NextStepType.GoToQuestion => NextQuestionDeterminant.ToQuestion(dto.NextQuestionId!.Value),
            NextStepType.EndSurvey => NextQuestionDeterminant.End(),
            _ => throw new ArgumentException($"Unknown NextStepType: {dto.Type}", nameof(dto))
        };
    }

    /// <summary>
    /// Converts a NextQuestionDeterminant value object to a NextQuestionDeterminantDto.
    /// </summary>
    /// <param name="valueObject">The value object to convert. Can be null.</param>
    /// <returns>
    /// NextQuestionDeterminantDto, or null if valueObject is null.
    /// </returns>
    public static NextQuestionDeterminantDto? ToDto(this NextQuestionDeterminant? valueObject)
    {
        if (valueObject == null)
        {
            return null;
        }

        return new NextQuestionDeterminantDto
        {
            Type = valueObject.Type,
            NextQuestionId = valueObject.NextQuestionId
        };
    }

    /// <summary>
    /// Converts a dictionary of option indices to NextQuestionDeterminantDto
    /// to a dictionary of option indices to NextQuestionDeterminant value objects.
    /// Useful for mapping CreateQuestionDto.OptionNextDeterminants to domain entities.
    /// </summary>
    /// <param name="dtoMap">Dictionary mapping option index to DTO. Can be null.</param>
    /// <returns>
    /// Dictionary mapping option index to value object, or null if input is null.
    /// </returns>
    public static Dictionary<int, NextQuestionDeterminant>? ToValueObjectMap(
        this Dictionary<int, NextQuestionDeterminantDto>? dtoMap)
    {
        if (dtoMap == null || !dtoMap.Any())
        {
            return null;
        }

        return dtoMap.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToValueObject()!
        );
    }

    /// <summary>
    /// Converts a dictionary of option IDs to NextQuestionDeterminant value objects
    /// to a dictionary of option IDs to NextQuestionDeterminantDto.
    /// Useful for mapping domain entities to response DTOs.
    /// </summary>
    /// <param name="valueObjectMap">Dictionary mapping option ID to value object. Can be null.</param>
    /// <returns>
    /// Dictionary mapping option ID to DTO, or null if input is null.
    /// </returns>
    public static Dictionary<int, NextQuestionDeterminantDto>? ToDtoMap(
        this Dictionary<int, NextQuestionDeterminant>? valueObjectMap)
    {
        if (valueObjectMap == null || !valueObjectMap.Any())
        {
            return null;
        }

        return valueObjectMap.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToDto()!
        );
    }
}
