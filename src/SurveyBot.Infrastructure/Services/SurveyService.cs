using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SurveyBot.Core.DTOs.Common;
using SurveyBot.Core.DTOs.Statistics;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using SurveyBot.Core.Utilities;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Core.ValueObjects.Answers;
using SurveyBot.Infrastructure.Data;
using System.Text.Json;

namespace SurveyBot.Infrastructure.Services;

/// <summary>
/// Implementation of survey business logic operations.
/// </summary>
public class SurveyService : ISurveyService
{
    private readonly ISurveyRepository _surveyRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IResponseRepository _responseRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly ISurveyValidationService _validationService;
    private readonly SurveyBotDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<SurveyService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyService"/> class.
    /// </summary>
    public SurveyService(
        ISurveyRepository surveyRepository,
        IQuestionRepository questionRepository,
        IResponseRepository responseRepository,
        IAnswerRepository answerRepository,
        ISurveyValidationService validationService,
        SurveyBotDbContext context,
        IMapper mapper,
        ILogger<SurveyService> logger)
    {
        _surveyRepository = surveyRepository;
        _questionRepository = questionRepository;
        _responseRepository = responseRepository;
        _answerRepository = answerRepository;
        _validationService = validationService;
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SurveyDto> CreateSurveyAsync(int userId, CreateSurveyDto dto)
    {
        _logger.LogInformation("Creating new survey for user {UserId}: {Title}", userId, dto.Title);

        // Validate the DTO
        ValidateCreateSurveyDto(dto);

        // Create survey entity
        var survey = _mapper.Map<Survey>(dto);
        survey.SetCreatorId(userId);
        survey.SetIsActive(false); // Always create as inactive, user must explicitly activate

        // Generate unique survey code
        var code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
            _surveyRepository.CodeExistsAsync);
        survey.SetCode(code);

        _logger.LogInformation("Generated unique code {Code} for survey", survey.Code);

        // Save to database
        var createdSurvey = await _surveyRepository.CreateAsync(survey);

        _logger.LogInformation("Survey {SurveyId} created successfully by user {UserId} with code {Code}",
            createdSurvey.Id, userId, createdSurvey.Code);

        // Map to DTO
        var result = _mapper.Map<SurveyDto>(createdSurvey);
        result.TotalResponses = 0;
        result.CompletedResponses = 0;

        return result;
    }

    /// <inheritdoc/>
    public async Task<SurveyDto> UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto)
    {
        _logger.LogInformation("Updating survey {SurveyId} by user {UserId}", surveyId, userId);

        // Get survey with questions
        var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);
        if (survey == null)
        {
            _logger.LogWarning("Survey {SurveyId} not found", surveyId);
            throw new SurveyNotFoundException(surveyId);
        }

        // Check authorization
        if (survey.CreatorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to update survey {SurveyId} owned by {OwnerId}",
                userId, surveyId, survey.CreatorId);
            throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
        }

        // Check if survey is active - cannot modify active surveys
        if (survey.IsActive)
        {
            _logger.LogWarning("Cannot modify active survey {SurveyId}", surveyId);
            throw new SurveyOperationException(
                "Cannot modify an active survey. Deactivate the survey first.");
        }

        // Update survey properties
        survey.SetTitle(dto.Title);
        survey.SetDescription(dto.Description);
        survey.SetAllowMultipleResponses(dto.AllowMultipleResponses);
        survey.SetShowResults(dto.ShowResults);

        // Save changes
        await _surveyRepository.UpdateAsync(survey);

        _logger.LogInformation("Survey {SurveyId} updated successfully", surveyId);

        // Get response counts
        var responseCount = await _surveyRepository.GetResponseCountAsync(surveyId);
        var completedCount = await _responseRepository.GetCompletedCountAsync(surveyId);

        // Map to DTO
        var result = _mapper.Map<SurveyDto>(survey);
        result.TotalResponses = responseCount;
        result.CompletedResponses = completedCount;

        return result;
    }

    /// <inheritdoc/>
    public async Task<SurveyDto> UpdateSurveyWithQuestionsAsync(
        int surveyId,
        int userId,
        UpdateSurveyWithQuestionsDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "🔄 Starting UpdateSurveyWithQuestionsAsync - SurveyId: {SurveyId}, UserId: {UserId}, QuestionCount: {QuestionCount}",
            surveyId, userId, dto.Questions.Count);
        _logger.LogDebug("DTO: {Dto}", JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true }));

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _logger.LogInformation("PASS 1: Deleting existing questions and creating new ones for survey {SurveyId}", surveyId);

            // Step 1: Validate ownership
            var survey = await _surveyRepository.GetByIdAsync(surveyId);
            if (survey == null)
            {
                _logger.LogWarning("Survey {SurveyId} not found", surveyId);
                throw new SurveyNotFoundException(surveyId);
            }

            if (survey.CreatorId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update survey {SurveyId} owned by {OwnerId}",
                    userId, surveyId, survey.CreatorId);
                throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
            }

            // Step 2: Update survey metadata and increment version
            survey.UpdateMetadata(
                dto.Title,
                dto.Description,
                dto.AllowMultipleResponses,
                dto.ShowResults);

            // Increment version to invalidate cached data in bot conversations
            survey.IncrementVersion();
            _logger.LogInformation("📊 Survey {SurveyId} version incremented to {Version}", surveyId, survey.Version);

            await _surveyRepository.UpdateAsync(survey);

            _logger.LogInformation("✅ Survey {SurveyId} metadata updated", surveyId);

            // Step 3: Delete existing questions (CASCADE deletes responses/answers)
            var deletedCount = await _questionRepository.DeleteBySurveyIdAsync(surveyId);
            _logger.LogInformation(
                "✅ Deleted {DeletedCount} existing questions from survey {SurveyId}",
                deletedCount, surveyId);

            // Step 4: Create new questions WITHOUT flow configuration
            var indexToIdMap = new Dictionary<int, int>(); // orderIndex → database ID
            var createdQuestions = new List<Question>();

            for (int i = 0; i < dto.Questions.Count; i++)
            {
                var questionDto = dto.Questions[i];

                // Create question without flow configuration
                var question = Question.Create(
                    surveyId: surveyId,
                    questionText: questionDto.QuestionText,
                    questionType: questionDto.QuestionType,
                    orderIndex: questionDto.OrderIndex,
                    isRequired: questionDto.IsRequired,
                    optionsJson: questionDto.Options != null ? JsonSerializer.Serialize(questionDto.Options) : null,
                    mediaContent: questionDto.MediaContent != null ? JsonSerializer.Serialize(questionDto.MediaContent) : null,
                    defaultNext: null); // No flow yet

                // Save to get database ID
                var createdQuestion = await _questionRepository.CreateAsync(question);
                createdQuestions.Add(createdQuestion);

                // Build index map
                indexToIdMap[i] = createdQuestion.Id;

                // Create QuestionOptions for SingleChoice questions
                if (questionDto.QuestionType == QuestionType.SingleChoice && questionDto.Options != null)
                {
                    for (int optIdx = 0; optIdx < questionDto.Options.Count; optIdx++)
                    {
                        var option = QuestionOption.Create(
                            questionId: createdQuestion.Id,
                            text: questionDto.Options[optIdx],
                            orderIndex: optIdx);

                        // Add option to question (using internal method for EF Core)
                        createdQuestion.AddOptionInternal(option);
                    }

                    // Save options
                    await _questionRepository.UpdateAsync(createdQuestion);
                }
            }

            _logger.LogInformation(
                "✅ PASS 1 complete: Created {QuestionCount} new questions for survey {SurveyId}",
                createdQuestions.Count, surveyId);

            _logger.LogInformation("PASS 2: Transforming index-based flow to ID-based flow for survey {SurveyId}", surveyId);

            // Log the complete index→ID mapping for debugging
            _logger.LogDebug(
                "Index→ID Mapping for survey {SurveyId}: {Mapping}",
                surveyId,
                string.Join(", ", indexToIdMap.Select(kvp => $"[{kvp.Key}→{kvp.Value}]")));

            // Load questions with options for flow configuration
            var questionsForFlow = await _questionRepository.GetBySurveyIdAsync(surveyId);
            var questionMap = questionsForFlow.ToDictionary(q => q.OrderIndex, q => q);

            for (int i = 0; i < dto.Questions.Count; i++)
            {
                var questionDto = dto.Questions[i];
                var question = questionMap[i];

                _logger.LogDebug(
                    "📝 Q{Index} (ID:{QuestionId}, Type:{QuestionType}): Processing flow transformation",
                    i, question.Id, questionDto.QuestionType);

                // Transform DefaultNextQuestionIndex
                if (questionDto.DefaultNextQuestionIndex.HasValue)
                {
                    var nextIndex = questionDto.DefaultNextQuestionIndex.Value;

                    if (nextIndex == -1)
                    {
                        // Sequential flow: null (use next question by order)
                        question.UpdateDefaultNext(null);
                        _logger.LogDebug(
                            "  ➡️ Q{Index}: DefaultNextQuestionIndex={InputValue} → Sequential flow (null)",
                            i, nextIndex);
                    }
                    else if (nextIndex >= 0 && indexToIdMap.ContainsKey(nextIndex))
                    {
                        // Go to specific question
                        var targetQuestionId = indexToIdMap[nextIndex];
                        question.UpdateDefaultNext(NextQuestionDeterminant.ToQuestion(targetQuestionId));
                        _logger.LogDebug(
                            "  ➡️ Q{Index}: DefaultNextQuestionIndex={InputValue} → GoToQuestion(ID:{TargetId})",
                            i, nextIndex, targetQuestionId);
                    }
                    else if (nextIndex >= 0)
                    {
                        // Index not found in map
                        _logger.LogWarning(
                            "  ⚠️ Q{Index}: DefaultNextQuestionIndex={InputValue} NOT FOUND in indexToIdMap (max index: {MaxIndex}). No transformation applied.",
                            i, nextIndex, indexToIdMap.Count - 1);
                    }
                }
                else
                {
                    // null means end survey
                    question.UpdateDefaultNext(NextQuestionDeterminant.End());
                    _logger.LogDebug(
                        "  ➡️ Q{Index}: DefaultNextQuestionIndex=null → EndSurvey",
                        i);
                }

                // Transform OptionNextQuestionIndexes for SingleChoice questions
                if (questionDto.QuestionType == QuestionType.SingleChoice &&
                    questionDto.OptionNextQuestionIndexes != null)
                {
                    _logger.LogDebug(
                        "  🔀 Q{Index}: SingleChoice question with {OptionCount} option flow configurations",
                        i, questionDto.OptionNextQuestionIndexes.Count);

                    // Reload question with options
                    var questionWithOptions = await _questionRepository.GetByIdWithOptionsAsync(question.Id);
                    if (questionWithOptions != null)
                    {
                        var options = questionWithOptions.Options.OrderBy(o => o.OrderIndex).ToList();

                        foreach (var kvp in questionDto.OptionNextQuestionIndexes)
                        {
                            var optionIndex = kvp.Key;
                            var nextQuestionIndex = kvp.Value;

                            if (optionIndex >= 0 && optionIndex < options.Count)
                            {
                                var option = options[optionIndex];

                                if (nextQuestionIndex.HasValue)
                                {
                                    if (nextQuestionIndex.Value == -1)
                                    {
                                        // Sequential flow
                                        option.UpdateNext(null);
                                        _logger.LogDebug(
                                            "    ↪️ Option[{OptionIndex}]: NextQuestionIndex={InputValue} → Sequential flow (null)",
                                            optionIndex, nextQuestionIndex.Value);
                                    }
                                    else if (nextQuestionIndex.Value >= 0 && indexToIdMap.ContainsKey(nextQuestionIndex.Value))
                                    {
                                        // Go to specific question
                                        var targetQuestionId = indexToIdMap[nextQuestionIndex.Value];
                                        option.UpdateNext(NextQuestionDeterminant.ToQuestion(targetQuestionId));
                                        _logger.LogDebug(
                                            "    ↪️ Option[{OptionIndex}]: NextQuestionIndex={InputValue} → GoToQuestion(ID:{TargetId})",
                                            optionIndex, nextQuestionIndex.Value, targetQuestionId);
                                    }
                                    else if (nextQuestionIndex.Value >= 0)
                                    {
                                        _logger.LogWarning(
                                            "    ⚠️ Option[{OptionIndex}]: NextQuestionIndex={InputValue} NOT FOUND in indexToIdMap (max index: {MaxIndex})",
                                            optionIndex, nextQuestionIndex.Value, indexToIdMap.Count - 1);
                                    }
                                }
                                else
                                {
                                    // null means end survey
                                    option.UpdateNext(NextQuestionDeterminant.End());
                                    _logger.LogDebug(
                                        "    ↪️ Option[{OptionIndex}]: NextQuestionIndex=null → EndSurvey",
                                        optionIndex);
                                }
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "    ⚠️ Option[{OptionIndex}]: Invalid option index (max: {MaxOptionIndex})",
                                    optionIndex, options.Count - 1);
                            }
                        }

                        await _questionRepository.UpdateAsync(questionWithOptions);
                    }
                    else
                    {
                        _logger.LogError(
                            "  ❌ Q{Index}: Failed to reload question with options for flow transformation",
                            i);
                    }
                }
                else if (questionDto.OptionNextQuestionIndexes != null && questionDto.OptionNextQuestionIndexes.Any())
                {
                    _logger.LogWarning(
                        "  ⚠️ Q{Index}: OptionNextQuestionIndexes provided for non-SingleChoice question (Type:{QuestionType}). This will be ignored.",
                        i, questionDto.QuestionType);
                }

                // Save flow configuration
                await _questionRepository.UpdateAsync(question);
            }

            _logger.LogInformation("✅ PASS 2 complete: Flow configuration updated for survey {SurveyId}", surveyId);

            _logger.LogInformation("PASS 3: Validating survey structure for survey {SurveyId}", surveyId);

            // Run cycle detection
            var cycleResult = await _validationService.DetectCycleAsync(surveyId);
            if (cycleResult.HasCycle)
            {
                _logger.LogWarning(
                    "Survey {SurveyId} has cycle: {CyclePath}",
                    surveyId, string.Join(" -> ", cycleResult.CyclePath!));

                await transaction.RollbackAsync();
                throw new SurveyCycleException(
                    cycleResult.CyclePath!,
                    $"Survey contains a cycle: {cycleResult.ErrorMessage}");
            }

            // Check survey has at least one endpoint
            var endpoints = await _validationService.FindSurveyEndpointsAsync(surveyId);
            if (!endpoints.Any())
            {
                _logger.LogWarning("Survey {SurveyId} has no endpoints", surveyId);

                await transaction.RollbackAsync();
                throw new SurveyValidationException(
                    "Survey must have at least one path that leads to completion (end survey).");
            }

            // Activate survey if requested
            if (dto.ActivateAfterUpdate)
            {
                survey.Activate();
                await _surveyRepository.UpdateAsync(survey);
                _logger.LogInformation("Survey {SurveyId} activated after update", surveyId);
            }

            // Commit transaction
            await transaction.CommitAsync();
            _logger.LogInformation(
                "✅ PASS 3 complete: Validation passed for survey {SurveyId}");

            _logger.LogInformation(
                "✅✅✅ Survey {SurveyId} update completed successfully. Questions: {QuestionCount}, Endpoints: {EndpointCount}, IsActive: {IsActive}",
                surveyId, dto.Questions.Count, endpoints.Count, survey.IsActive);

            // Return updated survey
            var updatedSurvey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);
            var responseCount = await _surveyRepository.GetResponseCountAsync(surveyId);
            var completedCount = await _responseRepository.GetCompletedCountAsync(surveyId);

            var result = _mapper.Map<SurveyDto>(updatedSurvey);
            result.TotalResponses = responseCount;
            result.CompletedResponses = completedCount;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed to update survey {SurveyId} with questions. Rolling back transaction. Error: {ErrorMessage}",
                surveyId, ex.Message);

            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSurveyAsync(int surveyId, int userId)
    {
        _logger.LogInformation("Deleting survey {SurveyId} by user {UserId}", surveyId, userId);

        // Get survey
        var survey = await _surveyRepository.GetByIdAsync(surveyId);
        if (survey == null)
        {
            _logger.LogWarning("Survey {SurveyId} not found", surveyId);
            throw new SurveyNotFoundException(surveyId);
        }

        // Check authorization
        if (survey.CreatorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to delete survey {SurveyId} owned by {OwnerId}",
                userId, surveyId, survey.CreatorId);
            throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
        }

        // Hard delete - remove survey and all related data (questions, responses, answers)
        // Note: Database cascade delete handles removal of related entities
        await _surveyRepository.DeleteAsync(surveyId);

        _logger.LogInformation("Survey {SurveyId} permanently deleted by user {UserId}", surveyId, userId);

        return true;
    }

    /// <inheritdoc/>
    public async Task<SurveyDto> GetSurveyByIdAsync(int surveyId, int userId)
    {
        _logger.LogInformation("Getting survey {SurveyId} for user {UserId}", surveyId, userId);

        // Get survey with questions
        var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);
        if (survey == null)
        {
            _logger.LogWarning("Survey {SurveyId} not found", surveyId);
            throw new SurveyNotFoundException(surveyId);
        }

        // Check authorization
        if (survey.CreatorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to access survey {SurveyId} owned by {OwnerId}",
                userId, surveyId, survey.CreatorId);
            throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
        }

        // Get response counts
        var responseCount = await _surveyRepository.GetResponseCountAsync(surveyId);
        var completedCount = await _responseRepository.GetCompletedCountAsync(surveyId);

        // Map to DTO
        var result = _mapper.Map<SurveyDto>(survey);
        result.TotalResponses = responseCount;
        result.CompletedResponses = completedCount;

        return result;
    }

    /// <inheritdoc/>
    public async Task<PagedResultDto<SurveyListDto>> GetAllSurveysAsync(int userId, PaginationQueryDto query)
    {
        _logger.LogInformation("Getting surveys for user {UserId} with pagination", userId);

        // Get surveys for the user
        var surveysQuery = (await _surveyRepository.GetByCreatorIdAsync(userId)).AsQueryable();

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            surveysQuery = surveysQuery.Where(s =>
                s.Title.ToLower().Contains(searchTerm) ||
                (s.Description != null && s.Description.ToLower().Contains(searchTerm)));
        }

        // Apply sorting
        surveysQuery = ApplySorting(surveysQuery, query.SortBy, query.SortDescending);

        // Get total count
        var totalCount = surveysQuery.Count();

        // Apply pagination
        var surveys = surveysQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        // Map to DTOs and add counts
        var surveyDtos = new List<SurveyListDto>();
        foreach (var survey in surveys)
        {
            var dto = _mapper.Map<SurveyListDto>(survey);
            dto.QuestionCount = survey.Questions.Count;
            dto.TotalResponses = await _surveyRepository.GetResponseCountAsync(survey.Id);
            dto.CompletedResponses = await _responseRepository.GetCompletedCountAsync(survey.Id);
            surveyDtos.Add(dto);
        }

        // Create paged result
        var result = new PagedResultDto<SurveyListDto>
        {
            Items = surveyDtos,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };

        return result;
    }

    /// <inheritdoc/>
    public async Task<SurveyDto> ActivateSurveyAsync(int surveyId, int userId)
    {
        _logger.LogInformation("Activating survey {SurveyId} by user {UserId}", surveyId, userId);

        // Get survey with questions
        var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);
        if (survey == null)
        {
            _logger.LogWarning("Survey {SurveyId} not found", surveyId);
            throw new SurveyNotFoundException(surveyId);
        }

        // Check authorization
        if (survey.CreatorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to activate survey {SurveyId} owned by {OwnerId}",
                userId, surveyId, survey.CreatorId);
            throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
        }

        // Validate survey has at least one question
        if (survey.Questions == null || survey.Questions.Count == 0)
        {
            _logger.LogWarning("Cannot activate survey {SurveyId} with no questions", surveyId);
            throw new SurveyValidationException(
                "Cannot activate a survey with no questions. Please add at least one question before activating.");
        }

        // Validate survey flow before activation (conditional flow validation)
        var cycleResult = await _validationService.DetectCycleAsync(surveyId);
        if (cycleResult.HasCycle)
        {
            _logger.LogWarning(
                "Cannot activate survey {SurveyId}: Cycle detected in question flow. Cycle path: {CyclePath}",
                surveyId, string.Join(" -> ", cycleResult.CyclePath!));
            throw new SurveyCycleException(
                cycleResult.CyclePath!,
                $"Cannot activate survey: {cycleResult.ErrorMessage}");
        }

        // Check that survey has at least one endpoint (at least one path to completion)
        var endpoints = await _validationService.FindSurveyEndpointsAsync(surveyId);
        if (!endpoints.Any())
        {
            _logger.LogWarning("Cannot activate survey {SurveyId}: No questions lead to survey completion", surveyId);
            throw new InvalidOperationException(
                "Cannot activate survey: No questions lead to survey completion. " +
                "At least one question must point to end of survey.");
        }

        // Activate survey
        survey.Activate();

        await _surveyRepository.UpdateAsync(survey);

        _logger.LogInformation(
            "Survey {SurveyId} activated successfully after validation. Endpoints: {EndpointCount}",
            surveyId, endpoints.Count);

        // Get response counts
        var responseCount = await _surveyRepository.GetResponseCountAsync(surveyId);
        var completedCount = await _responseRepository.GetCompletedCountAsync(surveyId);

        // Map to DTO
        var result = _mapper.Map<SurveyDto>(survey);
        result.TotalResponses = responseCount;
        result.CompletedResponses = completedCount;

        return result;
    }

    /// <inheritdoc/>
    public async Task<SurveyDto> DeactivateSurveyAsync(int surveyId, int userId)
    {
        _logger.LogInformation("Deactivating survey {SurveyId} by user {UserId}", surveyId, userId);

        // Get survey with questions
        var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);
        if (survey == null)
        {
            _logger.LogWarning("Survey {SurveyId} not found", surveyId);
            throw new SurveyNotFoundException(surveyId);
        }

        // Check authorization
        if (survey.CreatorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to deactivate survey {SurveyId} owned by {OwnerId}",
                userId, surveyId, survey.CreatorId);
            throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
        }

        // Deactivate survey
        survey.Deactivate();

        await _surveyRepository.UpdateAsync(survey);

        _logger.LogInformation("Survey {SurveyId} deactivated successfully", surveyId);

        // Get response counts
        var responseCount = await _surveyRepository.GetResponseCountAsync(surveyId);
        var completedCount = await _responseRepository.GetCompletedCountAsync(surveyId);

        // Map to DTO
        var result = _mapper.Map<SurveyDto>(survey);
        result.TotalResponses = responseCount;
        result.CompletedResponses = completedCount;

        return result;
    }

    /// <inheritdoc/>
    public async Task<SurveyStatisticsDto> GetSurveyStatisticsAsync(int surveyId, int userId)
    {
        _logger.LogInformation("Getting statistics for survey {SurveyId} requested by user {UserId}", surveyId, userId);

        // Get survey with questions and responses
        var survey = await _surveyRepository.GetByIdWithDetailsAsync(surveyId);
        if (survey == null)
        {
            _logger.LogWarning("Survey {SurveyId} not found", surveyId);
            throw new SurveyNotFoundException(surveyId);
        }

        // Check authorization
        if (survey.CreatorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to access statistics for survey {SurveyId} owned by {OwnerId}",
                userId, surveyId, survey.CreatorId);
            throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
        }

        // Get all responses
        var allResponses = survey.Responses.ToList();
        var completedResponses = allResponses.Where(r => r.IsComplete).ToList();

        // Calculate basic statistics
        var statistics = new SurveyStatisticsDto
        {
            SurveyId = surveyId,
            Title = survey.Title,
            TotalResponses = allResponses.Count,
            CompletedResponses = completedResponses.Count,
            IncompleteResponses = allResponses.Count - completedResponses.Count,
            CompletionRate = allResponses.Count > 0
                ? Math.Round((double)completedResponses.Count / allResponses.Count * 100, 2)
                : 0,
            UniqueRespondents = allResponses.Select(r => r.RespondentTelegramId).Distinct().Count(),
            FirstResponseAt = allResponses.OrderBy(r => r.StartedAt).FirstOrDefault()?.StartedAt,
            LastResponseAt = allResponses.OrderByDescending(r => r.StartedAt).FirstOrDefault()?.StartedAt,
            CreatedAt = survey.CreatedAt
        };

        // Calculate average completion time for completed responses
        if (completedResponses.Any())
        {
            var completionTimes = completedResponses
                .Where(r => r.StartedAt.HasValue && r.SubmittedAt.HasValue)
                .Select(r => (r.SubmittedAt!.Value - r.StartedAt!.Value).TotalSeconds)
                .ToList();

            if (completionTimes.Any())
            {
                // Convert seconds to minutes for frontend compatibility
                statistics.AverageCompletionTimeMinutes = Math.Round(completionTimes.Average() / 60, 2);
            }
        }

        // Calculate question-level statistics
        statistics.QuestionStatistics = await CalculateQuestionStatisticsAsync(survey.Questions.ToList(), completedResponses);

        _logger.LogInformation("Statistics calculated for survey {SurveyId}", surveyId);

        return statistics;
    }

    /// <inheritdoc/>
    public async Task<bool> UserOwnsSurveyAsync(int surveyId, int userId)
    {
        var survey = await _surveyRepository.GetByIdAsync(surveyId);
        return survey != null && survey.CreatorId == userId;
    }

    /// <inheritdoc/>
    public async Task<SurveyDto> GetSurveyByCodeAsync(string code)
    {
        _logger.LogInformation("Getting survey by code: {Code}", code);

        // Validate code format
        if (!SurveyCodeGenerator.IsValidCode(code))
        {
            _logger.LogWarning("Invalid survey code format: {Code}", code);
            throw new SurveyNotFoundException($"Survey with code '{code}' not found");
        }

        // Get survey with questions
        var survey = await _surveyRepository.GetByCodeWithQuestionsAsync(code);
        if (survey == null)
        {
            _logger.LogWarning("Survey with code {Code} not found", code);
            throw new SurveyNotFoundException($"Survey with code '{code}' not found");
        }

        // Only return active surveys for public access
        if (!survey.IsActive)
        {
            _logger.LogWarning("Survey {SurveyId} with code {Code} is not active", survey.Id, code);
            throw new SurveyNotFoundException($"Survey with code '{code}' is not available");
        }

        // Get response counts
        var responseCount = await _surveyRepository.GetResponseCountAsync(survey.Id);
        var completedCount = await _responseRepository.GetCompletedCountAsync(survey.Id);

        // Map to DTO
        var result = _mapper.Map<SurveyDto>(survey);
        result.TotalResponses = responseCount;
        result.CompletedResponses = completedCount;

        _logger.LogInformation("Survey {SurveyId} retrieved by code {Code}", survey.Id, code);

        return result;
    }

    #region Private Helper Methods

    /// <summary>
    /// Validates the CreateSurveyDto.
    /// </summary>
    private void ValidateCreateSurveyDto(CreateSurveyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new SurveyValidationException("Survey title is required.");
        }

        if (dto.Title.Length < 3)
        {
            throw new SurveyValidationException("Survey title must be at least 3 characters long.");
        }

        if (dto.Title.Length > 500)
        {
            throw new SurveyValidationException("Survey title cannot exceed 500 characters.");
        }

        if (dto.Description != null && dto.Description.Length > 2000)
        {
            throw new SurveyValidationException("Survey description cannot exceed 2000 characters.");
        }
    }

    /// <summary>
    /// Applies sorting to survey query.
    /// </summary>
    private IQueryable<Survey> ApplySorting(IQueryable<Survey> query, string? sortBy, bool descending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            // Default sorting: newest first
            return query.OrderByDescending(s => s.CreatedAt);
        }

        return sortBy.ToLower() switch
        {
            "title" => descending ? query.OrderByDescending(s => s.Title) : query.OrderBy(s => s.Title),
            "createdat" => descending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
            "updatedat" => descending ? query.OrderByDescending(s => s.UpdatedAt) : query.OrderBy(s => s.UpdatedAt),
            "isactive" => descending ? query.OrderByDescending(s => s.IsActive) : query.OrderBy(s => s.IsActive),
            _ => query.OrderByDescending(s => s.CreatedAt) // Default
        };
    }

    /// <summary>
    /// Calculates statistics for each question in the survey.
    /// </summary>
    private async Task<List<QuestionStatisticsDto>> CalculateQuestionStatisticsAsync(
        List<Question> questions,
        List<Response> completedResponses)
    {
        var statistics = new List<QuestionStatisticsDto>();

        foreach (var question in questions.OrderBy(q => q.OrderIndex))
        {
            var questionStat = new QuestionStatisticsDto
            {
                QuestionId = question.Id,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType
            };

            // Get all answers for this question from completed responses
            var responseIds = completedResponses.Select(r => r.Id).ToList();
            var answers = question.Answers
                .Where(a => responseIds.Contains(a.ResponseId))
                .ToList();

            questionStat.TotalAnswers = answers.Count;
            questionStat.SkippedCount = completedResponses.Count - answers.Count;
            questionStat.ResponseRate = completedResponses.Count > 0
                ? Math.Round((double)answers.Count / completedResponses.Count * 100, 2)
                : 0;

            // Calculate type-specific statistics
            switch (question.QuestionType)
            {
                case QuestionType.MultipleChoice:
                case QuestionType.SingleChoice:
                    questionStat.ChoiceDistribution = CalculateChoiceDistribution(answers, question.OptionsJson);
                    break;

                case QuestionType.Rating:
                    questionStat.RatingStatistics = CalculateRatingStatistics(answers);
                    break;

                case QuestionType.Text:
                    questionStat.TextStatistics = CalculateTextStatistics(answers);
                    break;

                case QuestionType.Number:
                    questionStat.NumberStatistics = CalculateNumberStatistics(answers);
                    break;

                case QuestionType.Date:
                    questionStat.DateStatistics = CalculateDateStatistics(answers);
                    break;

                case QuestionType.Location:
                    questionStat.LocationStatistics = CalculateLocationStatistics(answers);
                    break;
            }

            statistics.Add(questionStat);
        }

        return statistics;
    }

    /// <summary>
    /// Calculates distribution of choices for choice-based questions.
    /// </summary>
    private Dictionary<string, ChoiceStatisticsDto> CalculateChoiceDistribution(List<Answer> answers, string? optionsJson)
    {
        var distribution = new Dictionary<string, ChoiceStatisticsDto>();

        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return distribution;
        }

        try
        {
            var options = JsonSerializer.Deserialize<List<string>>(optionsJson);
            if (options == null) return distribution;

            // Initialize counts for each option
            var choiceCounts = new Dictionary<string, int>();
            foreach (var option in options)
            {
                choiceCounts[option] = 0;
            }

            // Count selections using answer.Value pattern matching (no JSON parsing!)
            foreach (var answer in answers)
            {
                switch (answer.Value)
                {
                    case SingleChoiceAnswerValue singleChoice:
                        if (choiceCounts.ContainsKey(singleChoice.SelectedOption))
                        {
                            choiceCounts[singleChoice.SelectedOption]++;
                        }
                        break;

                    case MultipleChoiceAnswerValue multipleChoice:
                        foreach (var selectedOption in multipleChoice.SelectedOptions)
                        {
                            if (choiceCounts.ContainsKey(selectedOption))
                            {
                                choiceCounts[selectedOption]++;
                            }
                        }
                        break;

                    case null:
                        // Legacy fallback: try to parse AnswerJson
                        if (!string.IsNullOrWhiteSpace(answer.AnswerJson))
                        {
                            try
                            {
                                var answerData = JsonSerializer.Deserialize<JsonElement>(answer.AnswerJson);
                                if (answerData.TryGetProperty("selectedOptions", out var selectedOptionsElement) &&
                                    selectedOptionsElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var item in selectedOptionsElement.EnumerateArray())
                                    {
                                        var choice = item.GetString();
                                        if (choice != null && choiceCounts.ContainsKey(choice))
                                        {
                                            choiceCounts[choice]++;
                                        }
                                    }
                                }
                                else if (answerData.TryGetProperty("selectedOption", out var selectedOptionElement))
                                {
                                    var choice = selectedOptionElement.GetString();
                                    if (choice != null && choiceCounts.ContainsKey(choice))
                                    {
                                        choiceCounts[choice]++;
                                    }
                                }
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogWarning(ex, "Failed to parse legacy answer JSON for answer {AnswerId}", answer.Id);
                            }
                        }
                        break;

                    default:
                        _logger.LogWarning("Answer {AnswerId} has unexpected Value type: {Type}",
                            answer.Id, answer.Value?.GetType().Name);
                        break;
                }
            }

            // Build distribution with percentages
            var totalAnswers = answers.Count;
            foreach (var kvp in choiceCounts)
            {
                distribution[kvp.Key] = new ChoiceStatisticsDto
                {
                    Option = kvp.Key,
                    Count = kvp.Value,
                    Percentage = totalAnswers > 0
                        ? Math.Round((double)kvp.Value / totalAnswers * 100, 2)
                        : 0
                };
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse options JSON");
        }

        return distribution;
    }

    /// <summary>
    /// Calculates statistics for rating questions.
    /// </summary>
    private RatingStatisticsDto CalculateRatingStatistics(List<Answer> answers)
    {
        var ratings = new List<int>();

        foreach (var answer in answers)
        {
            // Use pattern matching on answer.Value (no JSON parsing!)
            switch (answer.Value)
            {
                case RatingAnswerValue ratingValue:
                    ratings.Add(ratingValue.Rating);
                    break;

                case null:
                    // Legacy fallback: try to parse AnswerJson
                    if (!string.IsNullOrWhiteSpace(answer.AnswerJson))
                    {
                        try
                        {
                            var answerData = JsonSerializer.Deserialize<JsonElement>(answer.AnswerJson);
                            if (answerData.TryGetProperty("rating", out var ratingElement))
                            {
                                ratings.Add(ratingElement.GetInt32());
                            }
                            else if (answerData.TryGetProperty("ratingValue", out var ratingValueElement))
                            {
                                ratings.Add(ratingValueElement.GetInt32());
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse legacy rating for answer {AnswerId}", answer.Id);
                        }
                    }
                    break;

                default:
                    _logger.LogWarning("Answer {AnswerId} has unexpected Value type for rating: {Type}",
                        answer.Id, answer.Value?.GetType().Name);
                    break;
            }
        }

        if (!ratings.Any())
        {
            return new RatingStatisticsDto();
        }

        var stats = new RatingStatisticsDto
        {
            AverageRating = Math.Round(ratings.Average(), 2),
            MinRating = ratings.Min(),
            MaxRating = ratings.Max()
        };

        // Calculate distribution
        var distribution = ratings
            .GroupBy(r => r)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => new RatingDistributionDto
                {
                    Rating = g.Key,
                    Count = g.Count(),
                    Percentage = Math.Round((double)g.Count() / ratings.Count * 100, 2)
                });

        stats.Distribution = distribution;

        return stats;
    }

    /// <summary>
    /// Calculates statistics for text questions.
    /// </summary>
    private TextStatisticsDto CalculateTextStatistics(List<Answer> answers)
    {
        // Use answer.Value pattern matching with fallback to legacy AnswerText
        var textAnswers = answers
            .Select(a => a.Value is TextAnswerValue textValue ? textValue.Text : a.AnswerText)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Cast<string>()
            .ToList();

        if (!textAnswers.Any())
        {
            return new TextStatisticsDto();
        }

        return new TextStatisticsDto
        {
            TotalAnswers = textAnswers.Count,
            AverageLength = Math.Round(textAnswers.Average(t => t.Length), 2),
            MinLength = textAnswers.Min(t => t.Length),
            MaxLength = textAnswers.Max(t => t.Length)
        };
    }

    /// <summary>
    /// Calculates statistics for number questions.
    /// </summary>
    private NumberStatisticsDto CalculateNumberStatistics(List<Answer> answers)
    {
        var numbers = new List<decimal>();

        foreach (var answer in answers)
        {
            // Use pattern matching on answer.Value (no JSON parsing!)
            switch (answer.Value)
            {
                case NumberAnswerValue numberValue:
                    numbers.Add(numberValue.Value);
                    break;

                case null:
                    // Legacy fallback: try to parse AnswerJson
                    if (!string.IsNullOrWhiteSpace(answer.AnswerJson))
                    {
                        try
                        {
                            var answerData = JsonSerializer.Deserialize<JsonElement>(answer.AnswerJson);
                            if (answerData.TryGetProperty("number", out var numberElement))
                            {
                                numbers.Add(numberElement.GetDecimal());
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse legacy number for answer {AnswerId}", answer.Id);
                        }
                    }
                    break;

                default:
                    _logger.LogWarning("Answer {AnswerId} has unexpected Value type for number: {Type}",
                        answer.Id, answer.Value?.GetType().Name);
                    break;
            }
        }

        if (!numbers.Any())
        {
            return new NumberStatisticsDto();
        }

        var sortedNumbers = numbers.OrderBy(n => n).ToList();
        var count = sortedNumbers.Count;
        var sum = sortedNumbers.Sum();
        var average = sum / count;

        // Calculate median
        decimal median;
        if (count % 2 == 0)
        {
            median = (sortedNumbers[count / 2 - 1] + sortedNumbers[count / 2]) / 2;
        }
        else
        {
            median = sortedNumbers[count / 2];
        }

        // Calculate standard deviation
        var squaredDifferences = sortedNumbers.Sum(n => (n - average) * (n - average));
        var standardDeviation = (decimal)Math.Sqrt((double)(squaredDifferences / count));

        return new NumberStatisticsDto
        {
            Minimum = sortedNumbers.First(),
            Maximum = sortedNumbers.Last(),
            Average = Math.Round(average, 2),
            Median = Math.Round(median, 2),
            StandardDeviation = Math.Round(standardDeviation, 2),
            Count = count,
            Sum = Math.Round(sum, 2)
        };
    }

    /// <summary>
    /// Calculates statistics for date questions.
    /// </summary>
    private DateStatisticsDto CalculateDateStatistics(List<Answer> answers)
    {
        var dates = new List<DateTime>();

        foreach (var answer in answers)
        {
            // Use pattern matching on answer.Value (no JSON parsing!)
            switch (answer.Value)
            {
                case DateAnswerValue dateValue:
                    dates.Add(dateValue.Date);
                    break;

                case null:
                    // Legacy fallback: try to parse AnswerJson
                    if (!string.IsNullOrWhiteSpace(answer.AnswerJson))
                    {
                        try
                        {
                            var answerData = JsonSerializer.Deserialize<JsonElement>(answer.AnswerJson);
                            if (answerData.TryGetProperty("date", out var dateElement))
                            {
                                if (dateElement.TryGetDateTime(out var parsedDate))
                                {
                                    dates.Add(parsedDate.Date);
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse legacy date for answer {AnswerId}", answer.Id);
                        }
                    }
                    break;

                default:
                    _logger.LogWarning("Answer {AnswerId} has unexpected Value type for date: {Type}",
                        answer.Id, answer.Value?.GetType().Name);
                    break;
            }
        }

        if (!dates.Any())
        {
            return new DateStatisticsDto();
        }

        var sortedDates = dates.OrderBy(d => d).ToList();
        var count = sortedDates.Count;

        // Calculate date distribution (grouped by date, sorted by most recent first)
        var distribution = dates
            .GroupBy(d => d.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new DateFrequency
            {
                Date = g.Key,
                Count = g.Count(),
                Percentage = Math.Round((double)g.Count() / count * 100, 2)
            })
            .ToList();

        return new DateStatisticsDto
        {
            EarliestDate = sortedDates.First(),
            LatestDate = sortedDates.Last(),
            DateDistribution = distribution,
            Count = count
        };
    }

    /// <summary>
    /// Calculates statistics for location-type questions.
    /// Extracts location data points and calculates geographic bounds.
    /// </summary>
    /// <param name="answers">Answers for the location question</param>
    /// <returns>Location statistics with bounds and data points</returns>
    private LocationStatisticsDto CalculateLocationStatistics(List<Answer> answers)
    {
        var locations = new List<LocationDataPointDto>();

        foreach (var answer in answers)
        {
            switch (answer.Value)
            {
                case LocationAnswerValue locationValue:
                    locations.Add(new LocationDataPointDto
                    {
                        Latitude = locationValue.Latitude,
                        Longitude = locationValue.Longitude,
                        Accuracy = locationValue.Accuracy,
                        Timestamp = locationValue.Timestamp,
                        ResponseId = answer.ResponseId
                    });
                    break;

                case null:
                    // Legacy fallback
                    if (!string.IsNullOrWhiteSpace(answer.AnswerJson))
                    {
                        try
                        {
                            var data = JsonSerializer.Deserialize<JsonElement>(answer.AnswerJson);
                            if (data.TryGetProperty("latitude", out var lat) &&
                                data.TryGetProperty("longitude", out var lon))
                            {
                                locations.Add(new LocationDataPointDto
                                {
                                    Latitude = lat.GetDouble(),
                                    Longitude = lon.GetDouble(),
                                    Accuracy = data.TryGetProperty("accuracy", out var acc)
                                        ? acc.GetDouble()
                                        : null,
                                    Timestamp = data.TryGetProperty("timestamp", out var ts)
                                        ? ts.GetDateTime()
                                        : null,
                                    ResponseId = answer.ResponseId
                                });
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex,
                                "Failed to parse location answer JSON for answer ID {AnswerId}",
                                answer.Id);
                        }
                    }
                    break;
            }
        }

        if (!locations.Any())
        {
            return new LocationStatisticsDto
            {
                TotalLocations = 0,
                Locations = new List<LocationDataPointDto>()
            };
        }

        return new LocationStatisticsDto
        {
            TotalLocations = locations.Count,
            MinLatitude = locations.Min(l => l.Latitude),
            MaxLatitude = locations.Max(l => l.Latitude),
            MinLongitude = locations.Min(l => l.Longitude),
            MaxLongitude = locations.Max(l => l.Longitude),
            CenterLatitude = locations.Average(l => l.Latitude),
            CenterLongitude = locations.Average(l => l.Longitude),
            Locations = locations
        };
    }

    /// <inheritdoc/>
    public async Task<string> ExportSurveyToCSVAsync(int surveyId, int userId, string filter = "completed",
        bool includeMetadata = true, bool includeTimestamps = true)
    {
        _logger.LogInformation("Exporting survey {SurveyId} to CSV for user {UserId} with filter '{Filter}'",
            surveyId, userId, filter);

        // Get survey with all details
        var survey = await _surveyRepository.GetByIdWithDetailsAsync(surveyId);
        if (survey == null)
        {
            _logger.LogWarning("Survey {SurveyId} not found", surveyId);
            throw new SurveyNotFoundException(surveyId);
        }

        // Check authorization
        if (survey.CreatorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to export survey {SurveyId} owned by {OwnerId}",
                userId, surveyId, survey.CreatorId);
            throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
        }

        // Validate filter parameter
        filter = filter.ToLower();
        if (filter != "all" && filter != "completed" && filter != "incomplete")
        {
            _logger.LogWarning("Invalid filter parameter: {Filter}", filter);
            throw new SurveyValidationException("Filter must be 'all', 'completed', or 'incomplete'");
        }

        // Filter responses based on parameter
        var responses = survey.Responses.AsEnumerable();
        switch (filter)
        {
            case "completed":
                responses = responses.Where(r => r.IsComplete);
                break;
            case "incomplete":
                responses = responses.Where(r => !r.IsComplete);
                break;
            // "all" - no filtering
        }

        var responsesList = responses.OrderBy(r => r.StartedAt ?? DateTime.MinValue).ToList();

        _logger.LogInformation("Found {Count} responses matching filter '{Filter}'", responsesList.Count, filter);

        // Generate CSV content
        var csv = GenerateCSVContent(survey, responsesList, includeMetadata, includeTimestamps);

        _logger.LogInformation("CSV export completed for survey {SurveyId}. Size: {Size} bytes",
            surveyId, csv.Length);

        return csv;
    }

    /// <summary>
    /// Generates CSV content from survey responses.
    /// </summary>
    private string GenerateCSVContent(Survey survey, List<Response> responses,
        bool includeMetadata, bool includeTimestamps)
    {
        var csv = new System.Text.StringBuilder();

        // Get ordered questions
        var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

        // Build header row
        var headers = new List<string>();

        // Metadata columns
        if (includeMetadata)
        {
            headers.Add("Response ID");
            headers.Add("Respondent Telegram ID");
            headers.Add("Status");
        }

        // Timestamp columns
        if (includeTimestamps)
        {
            headers.Add("Started At");
            headers.Add("Submitted At");
        }

        // Question columns
        for (int i = 0; i < questions.Count; i++)
        {
            var question = questions[i];
            var columnHeader = $"Q{i + 1}: {question.QuestionText}";
            headers.Add(columnHeader);
        }

        // Write header row
        csv.AppendLine(EscapeCSVRow(headers));

        // Handle no responses case
        if (responses.Count == 0)
        {
            _logger.LogInformation("No responses to export, returning CSV with headers only");
            return csv.ToString();
        }

        // Write data rows
        foreach (var response in responses)
        {
            var row = new List<string>();

            // Metadata columns
            if (includeMetadata)
            {
                row.Add(response.Id.ToString());
                row.Add(response.RespondentTelegramId.ToString());
                row.Add(response.IsComplete ? "Completed" : "Incomplete");
            }

            // Timestamp columns
            if (includeTimestamps)
            {
                row.Add(response.StartedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "");
                row.Add(response.SubmittedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "");
            }

            // Answer columns
            foreach (var question in questions)
            {
                var answer = response.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                var answerText = FormatAnswerForCSV(answer, question);
                row.Add(answerText);
            }

            csv.AppendLine(EscapeCSVRow(row));
        }

        return csv.ToString();
    }

    /// <summary>
    /// Formats an answer for CSV export based on question type.
    /// Uses answer.Value pattern matching with legacy fallback.
    /// </summary>
    private string FormatAnswerForCSV(Answer? answer, Question question)
    {
        if (answer == null)
        {
            return ""; // No answer provided
        }

        // Use answer.Value.DisplayValue if available - simplest approach!
        if (answer.Value != null)
        {
            return answer.Value.DisplayValue;
        }

        // Legacy fallback for old data without Value
        try
        {
            switch (question.QuestionType)
            {
                case QuestionType.Text:
                    return answer.AnswerText ?? "";

                case QuestionType.SingleChoice:
                    if (!string.IsNullOrWhiteSpace(answer.AnswerJson))
                    {
                        var singleChoice = JsonSerializer.Deserialize<JsonElement>(answer.AnswerJson);
                        if (singleChoice.TryGetProperty("selectedOption", out var option))
                        {
                            return option.GetString() ?? "";
                        }
                    }
                    return "";

                case QuestionType.MultipleChoice:
                    if (!string.IsNullOrWhiteSpace(answer.AnswerJson))
                    {
                        var multipleChoice = JsonSerializer.Deserialize<JsonElement>(answer.AnswerJson);
                        if (multipleChoice.TryGetProperty("selectedOptions", out var options) &&
                            options.ValueKind == JsonValueKind.Array)
                        {
                            var selectedOptions = new List<string>();
                            foreach (var opt in options.EnumerateArray())
                            {
                                var optValue = opt.GetString();
                                if (optValue != null)
                                {
                                    selectedOptions.Add(optValue);
                                }
                            }
                            return string.Join(", ", selectedOptions);
                        }
                    }
                    return "";

                case QuestionType.Rating:
                    if (!string.IsNullOrWhiteSpace(answer.AnswerJson))
                    {
                        var rating = JsonSerializer.Deserialize<JsonElement>(answer.AnswerJson);
                        if (rating.TryGetProperty("rating", out var ratingValue))
                        {
                            return ratingValue.GetInt32().ToString();
                        }
                        if (rating.TryGetProperty("ratingValue", out var ratingVal))
                        {
                            return ratingVal.GetInt32().ToString();
                        }
                    }
                    return "";

                case QuestionType.Location:
                    if (!string.IsNullOrWhiteSpace(answer.AnswerJson))
                    {
                        var location = JsonSerializer.Deserialize<JsonElement>(answer.AnswerJson);
                        if (location.TryGetProperty("latitude", out var lat) &&
                            location.TryGetProperty("longitude", out var lon))
                        {
                            return $"{lat.GetDouble():F6}, {lon.GetDouble():F6}";
                        }
                    }
                    return "";

                default:
                    _logger.LogWarning("Unknown question type {QuestionType} for question {QuestionId}",
                        question.QuestionType, question.Id);
                    return "";
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse legacy answer JSON for answer {AnswerId}", answer.Id);
            return "";
        }
    }

    /// <summary>
    /// Escapes a CSV row by properly handling quotes, commas, and newlines.
    /// </summary>
    private string EscapeCSVRow(List<string> values)
    {
        var escapedValues = new List<string>();

        foreach (var value in values)
        {
            if (string.IsNullOrEmpty(value))
            {
                escapedValues.Add("");
                continue;
            }

            // Check if value needs escaping (contains comma, quote, or newline)
            if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            {
                // Escape quotes by doubling them
                var escaped = value.Replace("\"", "\"\"");
                // Wrap in quotes
                escapedValues.Add($"\"{escaped}\"");
            }
            else
            {
                escapedValues.Add(value);
            }
        }

        return string.Join(",", escapedValues);
    }

    #endregion
}
