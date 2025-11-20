using AutoMapper;
using Microsoft.Extensions.Logging;
using SurveyBot.Core.DTOs.Answer;
using SurveyBot.Core.DTOs.Common;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using System.Text.Json;
using ValidationResult = SurveyBot.Core.Models.ValidationResult;

namespace SurveyBot.Infrastructure.Services;

/// <summary>
/// Implementation of response business logic operations.
/// </summary>
public class ResponseService : IResponseService
{
    private readonly IResponseRepository _responseRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly ISurveyRepository _surveyRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IQuestionService _questionService;
    private readonly IMapper _mapper;
    private readonly ILogger<ResponseService> _logger;

    private const int MaxTextAnswerLength = 5000;
    private const int MinRatingValue = 1;
    private const int MaxRatingValue = 5;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseService"/> class.
    /// </summary>
    public ResponseService(
        IResponseRepository responseRepository,
        IAnswerRepository answerRepository,
        ISurveyRepository surveyRepository,
        IQuestionRepository questionRepository,
        IQuestionService questionService,
        IMapper mapper,
        ILogger<ResponseService> logger)
    {
        _responseRepository = responseRepository;
        _answerRepository = answerRepository;
        _surveyRepository = surveyRepository;
        _questionRepository = questionRepository;
        _questionService = questionService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ResponseDto> StartResponseAsync(int surveyId, long telegramUserId, string? username = null, string? firstName = null)
    {
        _logger.LogInformation("Starting response for survey {SurveyId} by Telegram user {TelegramUserId}", surveyId, telegramUserId);

        // Validate survey exists and is active
        var survey = await _surveyRepository.GetByIdAsync(surveyId);
        if (survey == null)
        {
            _logger.LogWarning("Survey {SurveyId} not found", surveyId);
            throw new SurveyNotFoundException(surveyId);
        }

        if (!survey.IsActive)
        {
            _logger.LogWarning("Survey {SurveyId} is not active", surveyId);
            throw new SurveyOperationException("This survey is not currently active.");
        }

        // Check for duplicate completed responses
        var hasCompleted = await _responseRepository.HasUserCompletedSurveyAsync(surveyId, telegramUserId);
        if (hasCompleted && !survey.AllowMultipleResponses)
        {
            _logger.LogWarning("User {TelegramUserId} has already completed survey {SurveyId}", telegramUserId, surveyId);
            throw new DuplicateResponseException(surveyId, telegramUserId);
        }

        // Create new response
        var response = new Response
        {
            SurveyId = surveyId,
            RespondentTelegramId = telegramUserId,
            IsComplete = false,
            StartedAt = DateTime.UtcNow
        };

        var createdResponse = await _responseRepository.CreateAsync(response);

        _logger.LogInformation("Response {ResponseId} started for survey {SurveyId} by user {TelegramUserId}",
            createdResponse.Id, surveyId, telegramUserId);

        return await MapToResponseDtoAsync(createdResponse, username, firstName);
    }

    /// <inheritdoc/>
    public async Task<ResponseDto> SaveAnswerAsync(
        int responseId,
        int questionId,
        string? answerText = null,
        List<string>? selectedOptions = null,
        int? ratingValue = null,
        int? userId = null)
    {
        _logger.LogInformation("Saving answer for response {ResponseId}, question {QuestionId}", responseId, questionId);

        // Get response with survey
        var response = await _responseRepository.GetByIdWithAnswersAsync(responseId);
        if (response == null)
        {
            _logger.LogWarning("Response {ResponseId} not found", responseId);
            throw new ResponseNotFoundException(responseId);
        }

        // Check if response is already completed
        if (response.IsComplete)
        {
            _logger.LogWarning("Cannot save answer to completed response {ResponseId}", responseId);
            throw new SurveyOperationException("Cannot modify a completed response.");
        }

        // Authorize if userId is provided - user must own the survey
        if (userId.HasValue)
        {
            await AuthorizeUserForResponseAsync(response, userId.Value);
        }

        // Validate question exists and belongs to survey
        var question = await _questionRepository.GetByIdAsync(questionId);
        if (question == null)
        {
            _logger.LogWarning("Question {QuestionId} not found", questionId);
            throw new QuestionNotFoundException(questionId);
        }

        if (question.SurveyId != response.SurveyId)
        {
            _logger.LogWarning("Question {QuestionId} does not belong to survey {SurveyId}",
                questionId, response.SurveyId);
            throw new QuestionValidationException("Question does not belong to this survey.");
        }

        // Validate answer format
        var validationResult = await ValidateAnswerFormatAsync(questionId, answerText, selectedOptions, ratingValue);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Invalid answer format for question {QuestionId}: {Error}",
                questionId, validationResult.ErrorMessage);
            throw new InvalidAnswerFormatException(questionId, question.QuestionType, validationResult.ErrorMessage!);
        }

        // Check if answer already exists for this question
        var existingAnswer = await _answerRepository.GetByResponseAndQuestionAsync(responseId, questionId);

        if (existingAnswer != null)
        {
            // Update existing answer
            existingAnswer.AnswerText = answerText;
            existingAnswer.AnswerJson = CreateAnswerJson(question.QuestionType, selectedOptions, ratingValue);
            existingAnswer.CreatedAt = DateTime.UtcNow;

            await _answerRepository.UpdateAsync(existingAnswer);
            _logger.LogInformation("Updated existing answer {AnswerId} for response {ResponseId}", existingAnswer.Id, responseId);
        }
        else
        {
            // Create new answer
            var answer = new Answer
            {
                ResponseId = responseId,
                QuestionId = questionId,
                AnswerText = answerText,
                AnswerJson = CreateAnswerJson(question.QuestionType, selectedOptions, ratingValue),
                CreatedAt = DateTime.UtcNow
            };

            await _answerRepository.CreateAsync(answer);
            _logger.LogInformation("Created new answer for response {ResponseId}, question {QuestionId}", responseId, questionId);
        }

        // Return updated response
        var updatedResponse = await _responseRepository.GetByIdWithAnswersAsync(responseId);
        return await MapToResponseDtoAsync(updatedResponse!);
    }

    /// <inheritdoc/>
    public async Task<ResponseDto> CompleteResponseAsync(int responseId, int? userId = null)
    {
        _logger.LogInformation("Completing response {ResponseId}", responseId);

        var response = await _responseRepository.GetByIdWithAnswersAsync(responseId);
        if (response == null)
        {
            _logger.LogWarning("Response {ResponseId} not found", responseId);
            throw new ResponseNotFoundException(responseId);
        }

        // Authorize if userId is provided
        if (userId.HasValue)
        {
            await AuthorizeUserForResponseAsync(response, userId.Value);
        }

        // Check if already completed
        if (response.IsComplete)
        {
            _logger.LogInformation("Response {ResponseId} is already completed", responseId);
            return await MapToResponseDtoAsync(response);
        }

        // Mark as complete
        response.IsComplete = true;
        response.SubmittedAt = DateTime.UtcNow;

        await _responseRepository.UpdateAsync(response);

        _logger.LogInformation("Response {ResponseId} marked as complete", responseId);

        return await MapToResponseDtoAsync(response);
    }

    /// <inheritdoc/>
    public async Task<ResponseDto> GetResponseAsync(int responseId, int? userId = null)
    {
        _logger.LogInformation("Getting response {ResponseId}", responseId);

        var response = await _responseRepository.GetByIdWithAnswersAsync(responseId);
        if (response == null)
        {
            _logger.LogWarning("Response {ResponseId} not found", responseId);
            throw new ResponseNotFoundException(responseId);
        }

        // Authorize if userId is provided
        if (userId.HasValue)
        {
            await AuthorizeUserForResponseAsync(response, userId.Value);
        }

        return await MapToResponseDtoAsync(response);
    }

    /// <inheritdoc/>
    public async Task<PagedResultDto<ResponseDto>> GetSurveyResponsesAsync(
        int surveyId,
        int userId,
        int pageNumber = 1,
        int pageSize = 20,
        bool? isCompleteFilter = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        _logger.LogInformation("Getting responses for survey {SurveyId} by user {UserId}", surveyId, userId);

        // Validate survey exists and user is the creator
        var survey = await _surveyRepository.GetByIdAsync(surveyId);
        if (survey == null)
        {
            _logger.LogWarning("Survey {SurveyId} not found", surveyId);
            throw new SurveyNotFoundException(surveyId);
        }

        if (survey.CreatorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to access responses for survey {SurveyId} owned by {CreatorId}",
                userId, surveyId, survey.CreatorId);
            throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
        }

        // Get responses based on filters
        IEnumerable<Response> responses;

        if (startDate.HasValue || endDate.HasValue)
        {
            var start = startDate ?? DateTime.MinValue;
            var end = endDate ?? DateTime.MaxValue;
            responses = await _responseRepository.GetByDateRangeAsync(surveyId, start, end);
        }
        else if (isCompleteFilter == true)
        {
            responses = await _responseRepository.GetCompletedBySurveyIdAsync(surveyId);
        }
        else
        {
            responses = await _responseRepository.GetBySurveyIdAsync(surveyId);
        }

        // Apply completion filter if needed
        if (isCompleteFilter.HasValue && !startDate.HasValue && !endDate.HasValue)
        {
            responses = responses.Where(r => r.IsComplete == isCompleteFilter.Value);
        }

        // Calculate total count
        var totalCount = responses.Count();

        // Apply pagination
        var pagedResponses = responses
            .OrderByDescending(r => r.StartedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Map to DTOs
        var responseDtos = new List<ResponseDto>();
        foreach (var response in pagedResponses)
        {
            responseDtos.Add(await MapToResponseDtoAsync(response));
        }

        return new PagedResultDto<ResponseDto>
        {
            Items = responseDtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAnswerFormatAsync(
        int questionId,
        string? answerText = null,
        List<string>? selectedOptions = null,
        int? ratingValue = null)
    {
        var question = await _questionRepository.GetByIdAsync(questionId);
        if (question == null)
        {
            _logger.LogWarning("Question {QuestionId} not found", questionId);
            throw new QuestionNotFoundException(questionId);
        }

        return question.QuestionType switch
        {
            QuestionType.Text => ValidateTextAnswer(answerText, question.IsRequired),
            QuestionType.SingleChoice => ValidateSingleChoiceAnswer(selectedOptions, question.OptionsJson, question.IsRequired),
            QuestionType.MultipleChoice => ValidateMultipleChoiceAnswer(selectedOptions, question.OptionsJson, question.IsRequired),
            QuestionType.Rating => ValidateRatingAnswer(ratingValue, question.IsRequired),
            _ => ValidationResult.Failure("Unknown question type")
        };
    }

    /// <inheritdoc/>
    public async Task<ResponseDto> ResumeResponseAsync(int surveyId, long telegramUserId, string? username = null, string? firstName = null)
    {
        _logger.LogInformation("Resuming response for survey {SurveyId} by Telegram user {TelegramUserId}", surveyId, telegramUserId);

        // Check for incomplete response
        var incompleteResponse = await _responseRepository.GetIncompleteResponseAsync(surveyId, telegramUserId);
        if (incompleteResponse != null)
        {
            _logger.LogInformation("Found incomplete response {ResponseId} for user {TelegramUserId}",
                incompleteResponse.Id, telegramUserId);
            return await MapToResponseDtoAsync(incompleteResponse, username, firstName);
        }

        // No incomplete response, start a new one
        return await StartResponseAsync(surveyId, telegramUserId, username, firstName);
    }

    /// <inheritdoc/>
    public async Task DeleteResponseAsync(int responseId, int userId)
    {
        _logger.LogInformation("Deleting response {ResponseId} by user {UserId}", responseId, userId);

        var response = await _responseRepository.GetByIdAsync(responseId);
        if (response == null)
        {
            _logger.LogWarning("Response {ResponseId} not found", responseId);
            throw new ResponseNotFoundException(responseId);
        }

        // Authorize - user must own the survey
        await AuthorizeUserForResponseAsync(response, userId);

        // Delete response (cascade will delete answers)
        await _responseRepository.DeleteAsync(responseId);

        _logger.LogInformation("Response {ResponseId} deleted successfully", responseId);
    }

    /// <inheritdoc/>
    public async Task<int> GetCompletedResponseCountAsync(int surveyId)
    {
        return await _responseRepository.GetCompletedCountAsync(surveyId);
    }

    // Private helper methods

    private async Task<ResponseDto> MapToResponseDtoAsync(Response response, string? username = null, string? firstName = null)
    {
        var dto = new ResponseDto
        {
            Id = response.Id,
            SurveyId = response.SurveyId,
            RespondentTelegramId = response.RespondentTelegramId,
            RespondentUsername = username,
            RespondentFirstName = firstName,
            IsComplete = response.IsComplete,
            StartedAt = response.StartedAt,
            SubmittedAt = response.SubmittedAt,
            Answers = new List<AnswerDto>(),
            AnsweredCount = 0,
            TotalQuestions = 0
        };

        // Get survey questions for total count
        var questions = await _questionRepository.GetBySurveyIdAsync(response.SurveyId);
        dto.TotalQuestions = questions.Count();

        // Load answers if exists
        if (response.Answers != null && response.Answers.Any())
        {
            foreach (var answer in response.Answers)
            {
                var answerDto = await MapToAnswerDtoAsync(answer);
                dto.Answers.Add(answerDto);
            }
            dto.AnsweredCount = dto.Answers.Count;
        }
        else
        {
            // Load answers separately if not included
            var answers = await _answerRepository.GetByResponseIdAsync(response.Id);
            foreach (var answer in answers)
            {
                var answerDto = await MapToAnswerDtoAsync(answer);
                dto.Answers.Add(answerDto);
            }
            dto.AnsweredCount = dto.Answers.Count;
        }

        return dto;
    }

    private async Task<AnswerDto> MapToAnswerDtoAsync(Answer answer)
    {
        var question = await _questionRepository.GetByIdAsync(answer.QuestionId);

        var dto = new AnswerDto
        {
            Id = answer.Id,
            ResponseId = answer.ResponseId,
            QuestionId = answer.QuestionId,
            QuestionText = question?.QuestionText ?? "",
            QuestionType = question?.QuestionType ?? QuestionType.Text,
            AnswerText = answer.AnswerText,
            CreatedAt = answer.CreatedAt
        };

        // Parse JSON answer for choice and rating questions
        if (!string.IsNullOrEmpty(answer.AnswerJson))
        {
            try
            {
                var answerData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(answer.AnswerJson);
                if (answerData != null)
                {
                    if (answerData.ContainsKey("selectedOptions"))
                    {
                        dto.SelectedOptions = JsonSerializer.Deserialize<List<string>>(answerData["selectedOptions"].GetRawText());
                    }
                    if (answerData.ContainsKey("ratingValue"))
                    {
                        dto.RatingValue = answerData["ratingValue"].GetInt32();
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse answer JSON for answer {AnswerId}", answer.Id);
            }
        }

        return dto;
    }

    private async Task AuthorizeUserForResponseAsync(Response response, int userId)
    {
        var survey = await _surveyRepository.GetByIdAsync(response.SurveyId);
        if (survey == null)
        {
            throw new SurveyNotFoundException(response.SurveyId);
        }

        if (survey.CreatorId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to access response {ResponseId} for survey {SurveyId} owned by {CreatorId}",
                userId, response.Id, response.SurveyId, survey.CreatorId);
            throw new Core.Exceptions.UnauthorizedAccessException(userId, "Response", response.Id);
        }
    }

    private string? CreateAnswerJson(QuestionType questionType, List<string>? selectedOptions, int? ratingValue)
    {
        if (questionType == QuestionType.Text)
        {
            return null;
        }

        var answerData = new Dictionary<string, object?>();

        if (questionType == QuestionType.SingleChoice || questionType == QuestionType.MultipleChoice)
        {
            answerData["selectedOptions"] = selectedOptions;
        }

        if (questionType == QuestionType.Rating)
        {
            answerData["ratingValue"] = ratingValue;
        }

        return JsonSerializer.Serialize(answerData);
    }

    private ValidationResult ValidateTextAnswer(string? answerText, bool isRequired)
    {
        if (isRequired && string.IsNullOrWhiteSpace(answerText))
        {
            return ValidationResult.Failure("Text answer is required");
        }

        if (!string.IsNullOrEmpty(answerText) && answerText.Length > MaxTextAnswerLength)
        {
            return ValidationResult.Failure($"Text answer cannot exceed {MaxTextAnswerLength} characters");
        }

        return ValidationResult.Success();
    }

    private ValidationResult ValidateSingleChoiceAnswer(List<string>? selectedOptions, string? optionsJson, bool isRequired)
    {
        if (isRequired && (selectedOptions == null || !selectedOptions.Any()))
        {
            return ValidationResult.Failure("An option must be selected");
        }

        if (selectedOptions == null || !selectedOptions.Any())
        {
            return ValidationResult.Success(); // Optional question with no answer
        }

        if (selectedOptions.Count > 1)
        {
            return ValidationResult.Failure("Only one option can be selected for single choice questions");
        }

        // Validate option exists in question options
        if (!string.IsNullOrEmpty(optionsJson))
        {
            try
            {
                var validOptions = JsonSerializer.Deserialize<List<string>>(optionsJson);
                if (validOptions != null && !validOptions.Contains(selectedOptions[0]))
                {
                    return ValidationResult.Failure("Selected option is not valid for this question");
                }
            }
            catch (JsonException)
            {
                _logger.LogWarning("Failed to parse options JSON");
            }
        }

        return ValidationResult.Success();
    }

    private ValidationResult ValidateMultipleChoiceAnswer(List<string>? selectedOptions, string? optionsJson, bool isRequired)
    {
        if (isRequired && (selectedOptions == null || !selectedOptions.Any()))
        {
            return ValidationResult.Failure("At least one option must be selected");
        }

        if (selectedOptions == null || !selectedOptions.Any())
        {
            return ValidationResult.Success(); // Optional question with no answer
        }

        // Validate all options exist in question options
        if (!string.IsNullOrEmpty(optionsJson))
        {
            try
            {
                var validOptions = JsonSerializer.Deserialize<List<string>>(optionsJson);
                if (validOptions != null)
                {
                    var invalidOptions = selectedOptions.Where(o => !validOptions.Contains(o)).ToList();
                    if (invalidOptions.Any())
                    {
                        return ValidationResult.Failure($"Invalid options selected: {string.Join(", ", invalidOptions)}");
                    }
                }
            }
            catch (JsonException)
            {
                _logger.LogWarning("Failed to parse options JSON");
            }
        }

        return ValidationResult.Success();
    }

    private ValidationResult ValidateRatingAnswer(int? ratingValue, bool isRequired)
    {
        if (isRequired && !ratingValue.HasValue)
        {
            return ValidationResult.Failure("Rating is required");
        }

        if (!ratingValue.HasValue)
        {
            return ValidationResult.Success(); // Optional question with no answer
        }

        if (ratingValue < MinRatingValue || ratingValue > MaxRatingValue)
        {
            return ValidationResult.Failure($"Rating must be between {MinRatingValue} and {MaxRatingValue}");
        }

        return ValidationResult.Success();
    }

    /// <inheritdoc/>
    public async Task<(int answerId, int? nextQuestionId)> SaveAnswerWithBranchingAsync(
        int responseId, int questionId, string answerValue)
    {
        _logger.LogInformation("Saving answer with branching for response {ResponseId}, question {QuestionId}",
            responseId, questionId);

        // 1. Validate response exists
        var response = await _responseRepository.GetByIdAsync(responseId);
        if (response == null)
        {
            _logger.LogWarning("Response {ResponseId} not found", responseId);
            throw new ResponseNotFoundException(responseId);
        }

        // 2. Validate question exists and belongs to the survey
        var question = await _questionRepository.GetByIdAsync(questionId);
        if (question == null)
        {
            _logger.LogWarning("Question {QuestionId} not found", questionId);
            throw new QuestionNotFoundException(questionId);
        }

        if (question.SurveyId != response.SurveyId)
        {
            _logger.LogWarning("Question {QuestionId} does not belong to survey {SurveyId}",
                questionId, response.SurveyId);
            throw new QuestionValidationException(
                "Question does not belong to the response's survey.");
        }

        // 3. Save the answer (create or update existing)
        var existingAnswer = await _answerRepository.GetByResponseAndQuestionAsync(responseId, questionId);

        Answer answer;
        if (existingAnswer != null)
        {
            // Update existing answer
            existingAnswer.AnswerText = answerValue;
            existingAnswer.AnswerJson = null; // Simple text-based for branching evaluation
            answer = await _answerRepository.UpdateAsync(existingAnswer);
            _logger.LogInformation("Updated existing answer {AnswerId}", answer.Id);
        }
        else
        {
            // Create new answer
            answer = new Answer
            {
                ResponseId = responseId,
                QuestionId = questionId,
                AnswerText = answerValue,
                CreatedAt = DateTime.UtcNow
            };
            answer = await _answerRepository.CreateAsync(answer);
            _logger.LogInformation("Created new answer {AnswerId}", answer.Id);
        }

        // 4. Evaluate branching rules to get next question
        var nextQuestionId = await _questionService.GetNextQuestionAsync(
            questionId, answerValue, response.SurveyId);

        if (nextQuestionId.HasValue)
        {
            _logger.LogInformation("Next question determined: {NextQuestionId}", nextQuestionId.Value);
        }
        else
        {
            _logger.LogInformation("No next question - survey complete");
        }

        return (answer.Id, nextQuestionId);
    }
}
