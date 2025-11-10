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
    private readonly IMapper _mapper;
    private readonly ILogger<SurveyService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyService"/> class.
    /// </summary>
    public SurveyService(
        ISurveyRepository surveyRepository,
        IResponseRepository responseRepository,
        IAnswerRepository answerRepository,
        IMapper mapper,
        ILogger<SurveyService> logger)
    {
        _surveyRepository = surveyRepository;
        _responseRepository = responseRepository;
        _answerRepository = answerRepository;
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
        survey.CreatorId = userId;
        survey.IsActive = false; // Always create as inactive, user must explicitly activate

        // Generate unique survey code
        survey.Code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
            _surveyRepository.CodeExistsAsync);

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
        survey.Title = dto.Title;
        survey.Description = dto.Description;
        survey.AllowMultipleResponses = dto.AllowMultipleResponses;
        survey.ShowResults = dto.ShowResults;
        survey.UpdatedAt = DateTime.UtcNow;

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

        // Check if survey has responses
        var hasResponses = await _surveyRepository.HasResponsesAsync(surveyId);

        if (hasResponses)
        {
            // Soft delete - just deactivate
            survey.IsActive = false;
            survey.UpdatedAt = DateTime.UtcNow;
            await _surveyRepository.UpdateAsync(survey);

            _logger.LogInformation("Survey {SurveyId} soft deleted (deactivated)", surveyId);
        }
        else
        {
            // Hard delete - survey has no responses
            await _surveyRepository.DeleteAsync(surveyId);

            _logger.LogInformation("Survey {SurveyId} hard deleted", surveyId);
        }

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

        // Activate survey
        survey.IsActive = true;
        survey.UpdatedAt = DateTime.UtcNow;

        await _surveyRepository.UpdateAsync(survey);

        _logger.LogInformation("Survey {SurveyId} activated successfully", surveyId);

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
        survey.IsActive = false;
        survey.UpdatedAt = DateTime.UtcNow;

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

            // Count selections
            foreach (var answer in answers)
            {
                if (string.IsNullOrWhiteSpace(answer.AnswerJson)) continue;

                try
                {
                    // Parse answer JSON - could be single value or array for multiple choice
                    var answerData = JsonSerializer.Deserialize<JsonElement>(answer.AnswerJson);

                    if (answerData.ValueKind == JsonValueKind.Array)
                    {
                        // Multiple choice
                        foreach (var item in answerData.EnumerateArray())
                        {
                            var choice = item.GetString();
                            if (choice != null && choiceCounts.ContainsKey(choice))
                            {
                                choiceCounts[choice]++;
                            }
                        }
                    }
                    else if (answerData.ValueKind == JsonValueKind.String)
                    {
                        // Single choice
                        var choice = answerData.GetString();
                        if (choice != null && choiceCounts.ContainsKey(choice))
                        {
                            choiceCounts[choice]++;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse answer JSON for answer {AnswerId}", answer.Id);
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
            if (string.IsNullOrWhiteSpace(answer.AnswerJson)) continue;

            try
            {
                var rating = JsonSerializer.Deserialize<int>(answer.AnswerJson);
                ratings.Add(rating);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse rating for answer {AnswerId}", answer.Id);
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
        var textAnswers = answers
            .Where(a => !string.IsNullOrWhiteSpace(a.AnswerText))
            .Select(a => a.AnswerText!)
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

    #endregion
}
