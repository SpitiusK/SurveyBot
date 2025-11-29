using AutoMapper;
using Microsoft.Extensions.Logging;
using SurveyBot.Core.DTOs.Media;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Extensions;
using SurveyBot.Core.Interfaces;
using SurveyBot.Core.Models;
using SurveyBot.Core.ValueObjects;
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

        // Create question entity using factory method
        var question = Question.Create(
            surveyId,
            dto.QuestionText,
            dto.QuestionType,
            await _questionRepository.GetNextOrderIndexAsync(surveyId),
            dto.IsRequired,
            optionsJson: null,  // Will be set below for choice questions
            mediaContent: null, // Will be set below if provided
            dto.DefaultNext.ToValueObject());

        // Handle options for choice-based questions
        if (dto.QuestionType == QuestionType.SingleChoice || dto.QuestionType == QuestionType.MultipleChoice)
        {
            if (dto.Options != null && dto.Options.Any())
            {
                // Create QuestionOption entities (NEW approach with flow support)
                var options = new List<QuestionOption>();

                for (int i = 0; i < dto.Options.Count; i++)
                {
                    // Create option using internal constructor (factory requires positive QuestionId,
                    // but the Question doesn't have an ID yet until saved)
                    // EF Core will set the FK automatically via the navigation property
                    var option = new QuestionOption(forInfrastructure: true);
                    option.SetText(dto.Options[i]);
                    option.SetOrderIndex(i);
                    option.SetQuestionInternal(question);
                    option.SetNext(dto.OptionNextDeterminants?.ContainsKey(i) == true
                        ? dto.OptionNextDeterminants[i].ToValueObject()
                        : null);

                    options.Add(option);
                }
                question.SetOptionsInternal(options);

                // Keep legacy OptionsJson for backwards compatibility
                question.SetOptionsJson(JsonSerializer.Serialize(dto.Options));
            }
        }

        // Handle media content if provided
        if (!string.IsNullOrWhiteSpace(dto.MediaContent))
        {
            question.SetMediaContent(dto.MediaContent);
        }

        // Save to database
        var createdQuestion = await _questionRepository.CreateAsync(question);

        _logger.LogInformation("Question {QuestionId} added to survey {SurveyId} successfully",
            createdQuestion.Id, surveyId);

        // Reload question with Options collection to get database-generated IDs
        var questionWithOptions = await _questionRepository.GetByIdWithOptionsAsync(createdQuestion.Id);
        if (questionWithOptions == null)
        {
            // Fallback: return created question even if reload fails
            _logger.LogWarning("Failed to reload question {QuestionId} with options after creation", createdQuestion.Id);
            return MapToDto(createdQuestion);
        }

        // Map to DTO (now includes OptionDetails with database IDs)
        return MapToDto(questionWithOptions);
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
        question.SetQuestionText(dto.QuestionText);
        question.SetQuestionType(dto.QuestionType);
        question.SetIsRequired(dto.IsRequired);

        // Update options for choice-based questions
        if (dto.QuestionType == QuestionType.SingleChoice || dto.QuestionType == QuestionType.MultipleChoice)
        {
            question.SetOptionsJson(JsonSerializer.Serialize(dto.Options));
        }
        else
        {
            question.SetOptionsJson(null);
        }

        // Update media content (null to clear, string to set)
        question.SetMediaContent(dto.MediaContent);

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

            case QuestionType.Location:
                rules["requiresOptions"] = false;
                rules["latitudeRange"] = new { min = -90.0, max = 90.0 };
                rules["longitudeRange"] = new { min = -180.0, max = 180.0 };
                rules["description"] = "Geographic location (latitude/longitude coordinates)";
                break;

            case QuestionType.Number:
                rules["requiresOptions"] = false;
                rules["supportsRange"] = true;
                rules["supportsDecimalPlaces"] = true;
                rules["description"] = "Numeric input (integer or decimal)";
                break;

            case QuestionType.Date:
                rules["requiresOptions"] = false;
                rules["supportsDateRange"] = true;
                rules["dateFormat"] = "DD.MM.YYYY";
                rules["description"] = "Date input in DD.MM.YYYY format";
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

            case QuestionType.Location:
                // Location questions should not have options
                if (options != null && options.Any())
                {
                    return QuestionValidationResult.Failure("Location questions should not have options.");
                }
                return QuestionValidationResult.Success();

            case QuestionType.Number:
                // Number questions should not have options
                if (options != null && options.Any())
                {
                    return QuestionValidationResult.Failure("Number questions should not have options.");
                }
                return QuestionValidationResult.Success();

            case QuestionType.Date:
                // Date questions should not have options
                if (options != null && options.Any())
                {
                    return QuestionValidationResult.Failure("Date questions should not have options.");
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

        // Map QuestionOption entities to OptionDetails (includes database IDs)
        if (question.Options != null && question.Options.Any())
        {
            dto.OptionDetails = question.Options
                .OrderBy(o => o.OrderIndex)
                .Select(o => new QuestionOptionDto
                {
                    Id = o.Id,  // Database-generated ID
                    Text = o.Text,
                    OrderIndex = o.OrderIndex
                })
                .ToList();
        }

        // Set SupportsBranching flag
        dto.SupportsBranching = question.SupportsBranching;

        return dto;
    }

    /// <inheritdoc/>
    public async Task<Question?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Getting question entity {QuestionId}", id);

        var question = await _questionRepository.GetByIdAsync(id);
        if (question == null)
        {
            _logger.LogWarning("Question {QuestionId} not found", id);
        }

        return question;
    }

    /// <inheritdoc/>
    public async Task<Question?> GetByIdWithOptionsAsync(int id)
    {
        _logger.LogInformation("Getting question entity {QuestionId} with Options", id);

        var question = await _questionRepository.GetByIdWithOptionsAsync(id);
        if (question == null)
        {
            _logger.LogWarning("Question {QuestionId} not found", id);
        }
        else
        {
            _logger.LogInformation("Question {QuestionId} loaded with {OptionCount} options",
                id, question.Options?.Count ?? 0);
        }

        return question;
    }

    /// <inheritdoc/>
    public async Task<Question> UpdateQuestionFlowAsync(int id, Core.DTOs.UpdateQuestionFlowDto dto)
    {
        _logger.LogInformation("═══════════════════════════════════════════════════");
        _logger.LogInformation("🔧 SERVICE LAYER: UpdateQuestionFlowAsync");
        _logger.LogInformation("═══════════════════════════════════════════════════");
        _logger.LogInformation("Question ID: {QuestionId}", id);

        // Get question WITH OPTIONS for flow configuration (critical for AutoMapper)
        var question = await _questionRepository.GetByIdWithOptionsAsync(id);
        if (question == null)
        {
            _logger.LogWarning("❌ Question {QuestionId} not found", id);
            throw new QuestionNotFoundException(id);
        }

        _logger.LogInformation("📋 Current Question State (BEFORE update):");
        _logger.LogInformation("  Question ID: {QuestionId}", question.Id);
        _logger.LogInformation("  Question Text: {Text}", question.QuestionText);
        _logger.LogInformation("  Question Type: {Type}", question.QuestionType);
        _logger.LogInformation("  Survey ID: {SurveyId}", question.SurveyId);
        _logger.LogInformation("  CURRENT DefaultNext: {DefaultNext}",
            question.DefaultNext?.ToString() ?? "NULL");
        _logger.LogInformation("  Options Count: {Count}", question.Options?.Count ?? 0);

        if (question.Options != null && question.Options.Any())
        {
            _logger.LogInformation("  Available Options:");
            foreach (var opt in question.Options)
            {
                _logger.LogInformation("    Option {OptionId}: '{Text}' (Current Next: {Next})",
                    opt.Id, opt.Text, opt.Next?.ToString() ?? "NULL");
            }
        }

        _logger.LogInformation("───────────────────────────────────────────────────");
        _logger.LogInformation("🔄 DTO → Entity Transformation:");

        try
        {
            // Validate and set DefaultNext using value object
            if (dto.DefaultNext != null)
            {
                _logger.LogInformation("📌 Processing DefaultNext:");
                _logger.LogInformation("   DTO Value: {Value}", dto.DefaultNext);

                if (dto.DefaultNext.Type == Core.Enums.NextStepType.EndSurvey)
                {
                    // EndSurvey type
                    _logger.LogInformation("   ✅ END SURVEY type → Creating EndSurvey determinant");
                    question.SetDefaultNext(NextQuestionDeterminant.End());
                    _logger.LogInformation("   NEW Value: {Value}", question.DefaultNext);
                }
                else if (dto.DefaultNext.Type == Core.Enums.NextStepType.GoToQuestion)
                {
                    // Validate that the target question exists
                    _logger.LogInformation("   🔍 Validating question ID exists...");
                    var targetQuestionId = dto.DefaultNext.NextQuestionId!.Value;
                    var targetQuestion = await _questionRepository.GetByIdAsync(targetQuestionId);

                    if (targetQuestion == null)
                    {
                        _logger.LogError("   ❌ Target question {TargetId} NOT FOUND", targetQuestionId);
                        throw new QuestionNotFoundException(targetQuestionId);
                    }

                    _logger.LogInformation("   ✅ Target question found: '{Text}' (ID: {TargetId})",
                        targetQuestion.QuestionText, targetQuestion.Id);

                    if (targetQuestionId == id)
                    {
                        _logger.LogError("   ❌ SELF-REFERENCE detected!");
                        throw new InvalidOperationException($"Question {id} cannot reference itself");
                    }

                    question.SetDefaultNext(NextQuestionDeterminant.ToQuestion(targetQuestionId));
                    _logger.LogInformation("   ✅ NEW Value: {Value}", question.DefaultNext);
                }
            }
            else
            {
                // null = clear flow configuration (sequential flow)
                _logger.LogInformation("📌 DefaultNext is NULL → Sequential flow");
                question.SetDefaultNext(null);
                _logger.LogInformation("   NEW Value: NULL");
            }

            // Update option-specific flows (for branching questions)
            if (dto.OptionNextDeterminants != null && dto.OptionNextDeterminants.Any())
            {
                _logger.LogInformation("───────────────────────────────────────────────────");
                _logger.LogInformation("📌 Processing OptionNextDeterminants: {Count} mappings",
                    dto.OptionNextDeterminants.Count);

                foreach (var optionFlow in dto.OptionNextDeterminants)
                {
                    var optionId = optionFlow.Key;
                    var determinant = optionFlow.Value;

                    _logger.LogInformation("  🔹 Option {OptionId} → {Determinant}:", optionId, determinant);

                    // Find the option by ID
                    var option = question.Options.FirstOrDefault(o => o.Id == optionId);
                    if (option == null)
                    {
                        _logger.LogError("    ❌ OPTION NOT FOUND!");
                        _logger.LogError("       Requested Option ID: {OptionId}", optionId);
                        _logger.LogError("       Available Option IDs: {AvailableIds}",
                            string.Join(", ", question.Options.Select(o => o.Id)));
                        throw new InvalidOperationException($"Option {optionId} does not exist for question {id}");
                    }

                    _logger.LogInformation("    ✅ Option found: '{Text}' (ID: {OptionId})", option.Text, option.Id);
                    _logger.LogInformation("       CURRENT Next: {Current}",
                        option.Next?.ToString() ?? "NULL");

                    // Validate next question determinant and create value object
                    if (determinant.Type == Core.Enums.NextStepType.EndSurvey)
                    {
                        // End of survey marker
                        _logger.LogInformation("       ✅ END SURVEY type → Creating EndSurvey determinant");
                        option.SetNext(NextQuestionDeterminant.End());
                        _logger.LogInformation("       NEW Next: {Value}", option.Next);
                    }
                    else if (determinant.Type == Core.Enums.NextStepType.GoToQuestion)
                    {
                        var targetQuestionId = determinant.NextQuestionId!.Value;

                        // Validate target question exists
                        _logger.LogInformation("       🔍 Validating next question {NextId} exists...", targetQuestionId);
                        var targetQuestion = await _questionRepository.GetByIdAsync(targetQuestionId);

                        if (targetQuestion == null)
                        {
                            _logger.LogError("       ❌ Target question {TargetId} NOT FOUND", targetQuestionId);
                            throw new QuestionNotFoundException(targetQuestionId);
                        }

                        _logger.LogInformation("       ✅ Target found: '{Text}' (ID: {TargetId})",
                            targetQuestion.QuestionText, targetQuestion.Id);

                        // Prevent self-reference
                        if (targetQuestionId == id)
                        {
                            _logger.LogError("       ❌ SELF-REFERENCE detected!");
                            throw new InvalidOperationException($"Option {optionId} cannot reference question {id}");
                        }

                        option.SetNext(NextQuestionDeterminant.ToQuestion(targetQuestionId));
                        _logger.LogInformation("       ✅ NEW Next: {Value}", option.Next);
                    }
                }
            }

            _logger.LogInformation("───────────────────────────────────────────────────");
            _logger.LogInformation("💾 Saving changes to database...");

            try
            {
                await _questionRepository.UpdateAsync(question);
                _logger.LogInformation("✅ Database update SUCCESSFUL");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "❌ DATABASE UPDATE FAILED");
                _logger.LogError("   Question ID: {QuestionId}", question.Id);
                _logger.LogError("   DefaultNext (entity value): {Value}", question.DefaultNext?.ToString() ?? "NULL");
                _logger.LogError("   Inner Exception: {InnerException}", ex.InnerException?.Message);
                throw;
            }

            _logger.LogInformation("═══════════════════════════════════════════════════");

            return question;
        }
        catch (Exception ex) when (ex is not QuestionNotFoundException && ex is not InvalidOperationException)
        {
            // Log and re-throw unexpected exceptions
            _logger.LogError(ex, "❌ Failed to update flow for question {QuestionId}: {ErrorMessage}", id, ex.Message);
            throw;
        }
    }

    #endregion
}
