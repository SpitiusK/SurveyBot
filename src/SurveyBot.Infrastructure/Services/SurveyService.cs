using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SurveyBot.Core.DTOs.Common;
using SurveyBot.Core.DTOs.Statistics;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using SurveyBot.Core.Utilities;
using SurveyBot.Core.ValueObjects.Answers;
using System.Text.Json;

namespace SurveyBot.Infrastructure.Services;

/// <summary>
/// Implementation of survey business logic operations.
/// </summary>
public class SurveyService : ISurveyService
{
    private readonly ISurveyRepository _surveyRepository;
    private readonly IResponseRepository _responseRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly ISurveyValidationService _validationService;
    private readonly IMapper _mapper;
    private readonly ILogger<SurveyService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyService"/> class.
    /// </summary>
    public SurveyService(
        ISurveyRepository surveyRepository,
        IResponseRepository responseRepository,
        IAnswerRepository answerRepository,
        ISurveyValidationService validationService,
        IMapper mapper,
        ILogger<SurveyService> logger)
    {
        _surveyRepository = surveyRepository;
        _responseRepository = responseRepository;
        _answerRepository = answerRepository;
        _validationService = validationService;
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

        // Check if survey can be modified
        if (survey.IsActive && await _surveyRepository.HasResponsesAsync(surveyId))
        {
            _logger.LogWarning("Cannot modify active survey {SurveyId} that has responses", surveyId);
            throw new SurveyOperationException(
                "Cannot modify an active survey that has responses. Deactivate the survey first or create a new version.");
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
            LastResponseAt = allResponses.OrderByDescending(r => r.StartedAt).FirstOrDefault()?.StartedAt
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
                statistics.AverageCompletionTime = Math.Round(completionTimes.Average(), 2);
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
