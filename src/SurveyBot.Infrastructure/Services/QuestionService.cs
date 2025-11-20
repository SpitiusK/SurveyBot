using AutoMapper;
using Microsoft.Extensions.Logging;
using SurveyBot.Core.DTOs.Media;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using SurveyBot.Core.Models;
using System.Text.Json;

namespace SurveyBot.Infrastructure.Services;

/// <summary>
/// Implementation of question business logic operations.
/// </summary>
public class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ISurveyRepository _surveyRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<QuestionService> _logger;

    // Constants for validation
    private const int MinOptionsCount = 2;
    private const int MaxOptionsCount = 10;
    private const int MaxOptionLength = 200;
    private const int MinRating = 1;
    private const int MaxRating = 5;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionService"/> class.
    /// </summary>
    public QuestionService(
        IQuestionRepository questionRepository,
        ISurveyRepository surveyRepository,
        IMapper mapper,
        ILogger<QuestionService> logger)
    {
        _questionRepository = questionRepository;
        _surveyRepository = surveyRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<QuestionDto> AddQuestionAsync(int surveyId, int userId, CreateQuestionDto dto)
    {
        _logger.LogInformation("Adding question to survey {SurveyId} by user {UserId}", surveyId, userId);

        // Get survey with questions
        var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);
        if (survey == null)
        {
            _logger.LogWarning("Survey {SurveyId} not found", surveyId);
            throw new SurveyNotFoundException(surveyId);
        }

        // Check authorization - user must own the survey
        if (survey.CreatorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to add question to survey {SurveyId} owned by {OwnerId}",
                userId, surveyId, survey.CreatorId);
            throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
        }

        // Check if survey has responses - cannot add questions if survey has responses
        var hasResponses = await _surveyRepository.HasResponsesAsync(surveyId);
        if (hasResponses)
        {
            _logger.LogWarning("Cannot add question to survey {SurveyId} that has responses", surveyId);
            throw new SurveyOperationException(
                "Cannot add questions to a survey that has responses. Create a new survey or deactivate this one first.");
        }

        // Validate question options based on type
        var validationResult = ValidateQuestionOptionsAsync(dto.QuestionType, dto.Options);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Question validation failed for survey {SurveyId}: {Errors}",
                surveyId, string.Join(", ", validationResult.Errors));
            throw new QuestionValidationException(
                "Question validation failed: " + string.Join(", ", validationResult.Errors));
        }

        // Create question entity
        var question = new Question
        {
            SurveyId = surveyId,
            QuestionText = dto.QuestionText,
            QuestionType = dto.QuestionType,
            IsRequired = dto.IsRequired,
            OrderIndex = await _questionRepository.GetNextOrderIndexAsync(surveyId)
        };

        // Serialize options for choice-based questions
        if (dto.QuestionType == QuestionType.SingleChoice || dto.QuestionType == QuestionType.MultipleChoice)
        {
            question.OptionsJson = JsonSerializer.Serialize(dto.Options);
        }

        // Handle media content if provided
        if (!string.IsNullOrWhiteSpace(dto.MediaContent))
        {
            question.MediaContent = dto.MediaContent;
        }

        // Save to database
        var createdQuestion = await _questionRepository.CreateAsync(question);

        _logger.LogInformation("Question {QuestionId} added to survey {SurveyId} successfully",
            createdQuestion.Id, surveyId);

        // Map to DTO
        return MapToDto(createdQuestion);
    }

    /// <inheritdoc/>
    public async Task<QuestionDto> UpdateQuestionAsync(int id, int userId, UpdateQuestionDto dto)
    {
        _logger.LogInformation("Updating question {QuestionId} by user {UserId}", id, userId);

        // Get question with survey
        var question = await _questionRepository.GetByIdAsync(id);
        if (question == null)
        {
            _logger.LogWarning("Question {QuestionId} not found", id);
            throw new QuestionNotFoundException(id);
        }

        // Get survey to check ownership
        var survey = await _surveyRepository.GetByIdAsync(question.SurveyId);
        if (survey == null)
        {
            _logger.LogWarning("Survey {SurveyId} not found for question {QuestionId}",
                question.SurveyId, id);
            throw new SurveyNotFoundException(question.SurveyId);
        }

        // Check authorization
        if (survey.CreatorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to update question {QuestionId} in survey {SurveyId} owned by {OwnerId}",
                userId, id, survey.Id, survey.CreatorId);
            throw new Core.Exceptions.UnauthorizedAccessException(userId, "Question", id);
        }

        // Check if question has answers - cannot modify if it has responses
        var questionWithAnswers = await _questionRepository.GetByIdWithAnswersAsync(id);
        if (questionWithAnswers?.Answers != null && questionWithAnswers.Answers.Any())
        {
            _logger.LogWarning("Cannot modify question {QuestionId} that has answers", id);
            throw new SurveyOperationException(
                "Cannot modify a question that has responses. Consider creating a new question or deactivating the survey.");
        }

        // Validate question options based on type
        var validationResult = ValidateQuestionOptionsAsync(dto.QuestionType, dto.Options);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Question validation failed for question {QuestionId}: {Errors}",
                id, string.Join(", ", validationResult.Errors));
            throw new QuestionValidationException(
                "Question validation failed: " + string.Join(", ", validationResult.Errors));
        }

        // Update question properties
        question.QuestionText = dto.QuestionText;
        question.QuestionType = dto.QuestionType;
        question.IsRequired = dto.IsRequired;
        question.UpdatedAt = DateTime.UtcNow;

        // Update options for choice-based questions
        if (dto.QuestionType == QuestionType.SingleChoice || dto.QuestionType == QuestionType.MultipleChoice)
        {
            question.OptionsJson = JsonSerializer.Serialize(dto.Options);
        }
        else
        {
            question.OptionsJson = null;
        }

        // Update media content (null to clear, string to set)
        question.MediaContent = dto.MediaContent;

        // Save changes
        await _questionRepository.UpdateAsync(question);

        _logger.LogInformation("Question {QuestionId} updated successfully", id);

        // Map to DTO
        return MapToDto(question);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteQuestionAsync(int id, int userId)
    {
        _logger.LogInformation("Deleting question {QuestionId} by user {UserId}", id, userId);

        // Get question
        var question = await _questionRepository.GetByIdAsync(id);
        if (question == null)
        {
            _logger.LogWarning("Question {QuestionId} not found", id);
            throw new QuestionNotFoundException(id);
        }

        // Get survey to check ownership
        var survey = await _surveyRepository.GetByIdAsync(question.SurveyId);
        if (survey == null)
        {
            _logger.LogWarning("Survey {SurveyId} not found for question {QuestionId}",
                question.SurveyId, id);
            throw new SurveyNotFoundException(question.SurveyId);
        }

        // Check authorization
        if (survey.CreatorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to delete question {QuestionId} in survey {SurveyId} owned by {OwnerId}",
                userId, id, survey.Id, survey.CreatorId);
            throw new Core.Exceptions.UnauthorizedAccessException(userId, "Question", id);
        }

        // Check if question has answers - cannot delete if it has responses
        var questionWithAnswers = await _questionRepository.GetByIdWithAnswersAsync(id);
        if (questionWithAnswers?.Answers != null && questionWithAnswers.Answers.Any())
        {
            _logger.LogWarning("Cannot delete question {QuestionId} that has answers", id);
            throw new SurveyOperationException(
                "Cannot delete a question that has responses. Consider deactivating the survey instead.");
        }

        // Delete question
        await _questionRepository.DeleteAsync(id);

        _logger.LogInformation("Question {QuestionId} deleted successfully", id);

        return true;
    }

    /// <inheritdoc/>
    public async Task<QuestionDto> GetQuestionAsync(int id)
    {
        _logger.LogInformation("Getting question {QuestionId}", id);

        var question = await _questionRepository.GetByIdAsync(id);
        if (question == null)
        {
            _logger.LogWarning("Question {QuestionId} not found", id);
            throw new QuestionNotFoundException(id);
        }

        return MapToDto(question);
    }

    /// <inheritdoc/>
    public async Task<List<QuestionDto>> GetBySurveyIdAsync(int surveyId)
    {
        _logger.LogInformation("Getting questions for survey {SurveyId}", surveyId);

        var questions = await _questionRepository.GetBySurveyIdAsync(surveyId);
        return questions.Select(MapToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> ReorderQuestionsAsync(int surveyId, int userId, int[] questionIds)
    {
        _logger.LogInformation("Reordering questions for survey {SurveyId} by user {UserId}", surveyId, userId);

        // Get survey
        var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);
        if (survey == null)
        {
            _logger.LogWarning("Survey {SurveyId} not found", surveyId);
            throw new SurveyNotFoundException(surveyId);
        }

        // Check authorization
        if (survey.CreatorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to reorder questions in survey {SurveyId} owned by {OwnerId}",
                userId, surveyId, survey.CreatorId);
            throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
        }

        // Validate that all question IDs belong to this survey
        var existingQuestions = survey.Questions.Select(q => q.Id).OrderBy(id => id).ToList();
        var providedQuestions = questionIds.Distinct().OrderBy(id => id).ToList();

        if (!existingQuestions.SequenceEqual(providedQuestions))
        {
            _logger.LogWarning("Invalid question IDs provided for reordering survey {SurveyId}", surveyId);
            throw new QuestionValidationException(
                "All question IDs must belong to this survey and no duplicates are allowed.");
        }

        // Create dictionary mapping question ID to new order index
        var questionOrders = new Dictionary<int, int>();
        for (int i = 0; i < questionIds.Length; i++)
        {
            questionOrders[questionIds[i]] = i;
        }

        // Reorder questions
        var result = await _questionRepository.ReorderQuestionsAsync(questionOrders);

        if (result)
        {
            _logger.LogInformation("Questions reordered successfully for survey {SurveyId}", surveyId);
        }
        else
        {
            _logger.LogWarning("Failed to reorder questions for survey {SurveyId}", surveyId);
        }

        return result;
    }

    /// <inheritdoc/>
    public Dictionary<string, object> GetQuestionTypeValidationAsync(QuestionType type)
    {
        var rules = new Dictionary<string, object>();

        switch (type)
        {
            case QuestionType.Text:
                rules["requiresOptions"] = false;
                rules["maxLength"] = 5000;
                rules["description"] = "Free-form text answer";
                break;

            case QuestionType.SingleChoice:
                rules["requiresOptions"] = true;
                rules["minOptions"] = MinOptionsCount;
                rules["maxOptions"] = MaxOptionsCount;
                rules["maxOptionLength"] = MaxOptionLength;
                rules["allowMultiple"] = false;
                rules["description"] = "Single choice from multiple options (radio button)";
                break;

            case QuestionType.MultipleChoice:
                rules["requiresOptions"] = true;
                rules["minOptions"] = MinOptionsCount;
                rules["maxOptions"] = MaxOptionsCount;
                rules["maxOptionLength"] = MaxOptionLength;
                rules["allowMultiple"] = true;
                rules["description"] = "Multiple choices from multiple options (checkboxes)";
                break;

            case QuestionType.Rating:
                rules["requiresOptions"] = false;
                rules["minRating"] = MinRating;
                rules["maxRating"] = MaxRating;
                rules["description"] = $"Numeric rating ({MinRating}-{MaxRating} scale)";
                break;

            default:
                throw new InvalidQuestionTypeException(type);
        }

        return rules;
    }

    /// <inheritdoc/>
    public QuestionValidationResult ValidateQuestionOptionsAsync(QuestionType type, List<string>? options)
    {
        switch (type)
        {
            case QuestionType.Text:
                // Text questions should not have options
                if (options != null && options.Any())
                {
                    return QuestionValidationResult.Failure("Text questions should not have options.");
                }
                return QuestionValidationResult.Success();

            case QuestionType.SingleChoice:
            case QuestionType.MultipleChoice:
                // Choice-based questions must have options
                if (options == null || !options.Any())
                {
                    return QuestionValidationResult.Failure("Choice-based questions must have at least 2 options.");
                }

                if (options.Count < MinOptionsCount)
                {
                    return QuestionValidationResult.Failure($"Choice-based questions must have at least {MinOptionsCount} options.");
                }

                if (options.Count > MaxOptionsCount)
                {
                    return QuestionValidationResult.Failure($"Questions cannot have more than {MaxOptionsCount} options.");
                }

                // Check for empty options
                if (options.Any(string.IsNullOrWhiteSpace))
                {
                    return QuestionValidationResult.Failure("All options must have text.");
                }

                // Check option length
                var longOptions = options.Where(o => o.Length > MaxOptionLength).ToList();
                if (longOptions.Any())
                {
                    return QuestionValidationResult.Failure($"Option text cannot exceed {MaxOptionLength} characters.");
                }

                // Check for duplicate options
                var duplicates = options.GroupBy(o => o.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicates.Any())
                {
                    return QuestionValidationResult.Failure($"Duplicate options are not allowed: {string.Join(", ", duplicates)}");
                }

                return QuestionValidationResult.Success();

            case QuestionType.Rating:
                // Rating questions should not have options
                if (options != null && options.Any())
                {
                    return QuestionValidationResult.Failure("Rating questions should not have options.");
                }
                return QuestionValidationResult.Success();

            default:
                throw new InvalidQuestionTypeException(type);
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Maps a Question entity to QuestionDto.
    /// </summary>
    private QuestionDto MapToDto(Question question)
    {
        var dto = new QuestionDto
        {
            Id = question.Id,
            SurveyId = question.SurveyId,
            QuestionText = question.QuestionText,
            QuestionType = question.QuestionType,
            OrderIndex = question.OrderIndex,
            IsRequired = question.IsRequired,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt
        };

        // Deserialize options for choice-based questions
        if (!string.IsNullOrWhiteSpace(question.OptionsJson))
        {
            try
            {
                dto.Options = JsonSerializer.Deserialize<List<string>>(question.OptionsJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize options for question {QuestionId}", question.Id);
                dto.Options = null;
            }
        }

        // Deserialize media content
        if (!string.IsNullOrWhiteSpace(question.MediaContent))
        {
            try
            {
                dto.MediaContent = JsonSerializer.Deserialize<MediaContentDto>(question.MediaContent);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize media content for question {QuestionId}", question.Id);
                dto.MediaContent = null;
            }
        }

        return dto;
    }

    #endregion
}
