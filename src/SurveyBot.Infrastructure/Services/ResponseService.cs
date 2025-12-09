using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SurveyBot.Core.DTOs.Answer;
using SurveyBot.Core.DTOs.Common;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Enums;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Core.ValueObjects.Answers;
using SurveyBot.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
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
    private readonly SurveyBotDbContext _context;
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
        SurveyBotDbContext context,
        IMapper mapper,
        ILogger<ResponseService> logger)
    {
        _responseRepository = responseRepository;
        _answerRepository = answerRepository;
        _surveyRepository = surveyRepository;
        _questionRepository = questionRepository;
        _context = context;
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

        // Create new response using factory method
        var response = Response.Start(surveyId, telegramUserId);

        var createdResponse = await _responseRepository.CreateAsync(response);

        _logger.LogInformation("Response {ResponseId} started for survey {SurveyId} by user {TelegramUserId}",
            createdResponse.Id, surveyId, telegramUserId);

        return await MapToResponseDtoAsync(createdResponse, username, firstName);
    }

    /// <inheritdoc/>
    public async Task<AnswerDto> SaveAnswerAsync(
        int responseId,
        int questionId,
        string? answerText = null,
        List<string>? selectedOptions = null,
        int? ratingValue = null,
        int? userId = null,
        string? answerJson = null)
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

        // Validate question exists and belongs to survey (load with flow configuration)
        var question = await _questionRepository.GetByIdWithFlowConfigAsync(questionId);
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
        var validationResult = await ValidateAnswerFormatAsync(questionId, answerText, selectedOptions, ratingValue, answerJson);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Invalid answer format for question {QuestionId}: {Error}",
                questionId, validationResult.ErrorMessage);
            throw new InvalidAnswerFormatException(questionId, question.QuestionType, validationResult.ErrorMessage!);
        }

        // Convert selectedOptions from strings to option indexes
        List<int>? selectedOptionIndexes = null;
        if (selectedOptions != null && selectedOptions.Any())
        {
            selectedOptionIndexes = new List<int>();
            var options = question.Options?.OrderBy(o => o.OrderIndex).ToList();
            if (options != null)
            {
                foreach (var selectedText in selectedOptions)
                {
                    var optionIndex = options.FindIndex(o => o.Text == selectedText);
                    if (optionIndex >= 0)
                    {
                        selectedOptionIndexes.Add(optionIndex);
                    }
                }
            }
        }
        // Convert ratingValue to option index for Rating questions
        // Rating values are 1-5, option indexes are 0-4
        else if (ratingValue.HasValue && question.QuestionType == QuestionType.Rating)
        {
            selectedOptionIndexes = new List<int> { ratingValue.Value - 1 };

            _logger.LogInformation(
                "Converted rating value {RatingValue} to option index {OptionIndex} for question {QuestionId}",
                ratingValue.Value, ratingValue.Value - 1, questionId);
        }

        // Determine next question based on conditional flow
        var nextStep = await DetermineNextStepAsync(
            question,
            selectedOptionIndexes,
            response.SurveyId,
            CancellationToken.None);

        _logger.LogInformation(
            "Determined next step for Response {ResponseId}, Question {QuestionId}: {NextStep}",
            response.Id, question.Id, nextStep);

        // Check if answer already exists for this question
        var existingAnswer = await _answerRepository.GetByResponseAndQuestionAsync(responseId, questionId);

        // Create AnswerValue using factory - this is the FIX for the bug!
        // Previously used CreateAnswerJson() which only set Value for text questions
        AnswerValue answerValue;
        if (question.QuestionType == QuestionType.Location)
        {
            // Check if answer is provided
            if (!string.IsNullOrWhiteSpace(answerJson))
            {
                // Location answers come pre-serialized from the handler
                answerValue = LocationAnswerValue.FromJson(answerJson);
            }
            else if (question.IsRequired)
            {
                // Required location question with no answer - fail validation
                throw new InvalidAnswerFormatException(
                    questionId,
                    QuestionType.Location,
                    "Location answer is required");
            }
            else
            {
                // Optional location question with no answer - allow null value
                answerValue = null!;  // Will be handled by Answer.CreateWithValue
                _logger.LogInformation(
                    "Optional location question {QuestionId} answered with null value",
                    questionId);
            }
        }
        else
        {
            answerValue = AnswerValueFactory.CreateFromInput(
                questionType: question.QuestionType,
                textAnswer: answerText,
                selectedOptions: selectedOptions,
                ratingValue: ratingValue,
                question: question);
        }

        Answer savedAnswer;

        if (existingAnswer != null)
        {
            // Update existing answer using UpdateValue (not legacy SetAnswerText/SetAnswerJson)
            existingAnswer.UpdateValue(answerValue);
            existingAnswer.SetNext(nextStep);
            existingAnswer.SetCreatedAt(DateTime.UtcNow);

            await _answerRepository.UpdateAsync(existingAnswer);
            _logger.LogInformation("Updated existing answer {AnswerId} for response {ResponseId}", existingAnswer.Id, responseId);
            savedAnswer = existingAnswer;
        }
        else
        {
            // Handle null answerValue (optional questions with no answer provided)
            if (answerValue != null)
            {
                // Create new answer using CreateWithValue for non-null values
                savedAnswer = Answer.CreateWithValue(
                    responseId,
                    questionId,
                    answerValue,
                    nextStep);
            }
            else
            {
                // Use legacy Create for optional questions with null/empty answers
                // This handles the case where an optional Location/other question has no answer
                savedAnswer = Answer.Create(
                    responseId,
                    questionId,
                    answerText: null,
                    answerJson: null,
                    next: nextStep);

                _logger.LogInformation(
                    "Created empty answer for optional question {QuestionId} in response {ResponseId}",
                    questionId, responseId);
            }

            await _answerRepository.CreateAsync(savedAnswer);
            _logger.LogInformation("Created new answer for response {ResponseId}, question {QuestionId}", responseId, questionId);
        }

        // Record the question as visited for conditional flow tracking
        response.RecordVisitedQuestion(questionId);
        await _responseRepository.UpdateAsync(response);
        _logger.LogInformation("Recorded question {QuestionId} as visited for response {ResponseId}", questionId, responseId);

        // Return saved answer DTO
        return await MapToAnswerDtoAsync(savedAnswer);
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

        // Validate all required questions are answered
        await ValidateRequiredAnswersAsync(responseId, response.SurveyId);

        // Mark as complete
        response.MarkAsComplete();

        await _responseRepository.UpdateAsync(response);

        _logger.LogInformation("Response {ResponseId} marked as complete", responseId);

        return await MapToResponseDtoAsync(response);
    }

    /// <summary>
    /// Validates that all required questions have been answered for a response.
    /// </summary>
    /// <param name="responseId">The response ID</param>
    /// <param name="surveyId">The survey ID</param>
    /// <exception cref="SurveyValidationException">Thrown when required questions are missing answers</exception>
    private async Task ValidateRequiredAnswersAsync(int responseId, int surveyId)
    {
        _logger.LogDebug("Validating required answers for response {ResponseId}", responseId);

        // Get all required questions for the survey
        var requiredQuestions = await _questionRepository
            .GetBySurveyIdAsync(surveyId);

        var requiredQuestionList = requiredQuestions
            .Where(q => q.IsRequired)
            .ToList();

        if (!requiredQuestionList.Any())
        {
            _logger.LogDebug("No required questions found for survey {SurveyId}", surveyId);
            return; // No required questions - validation passes
        }

        // Get all answered question IDs for this response
        var response = await _responseRepository.GetByIdWithAnswersAsync(responseId);
        var answeredQuestionIds = response.Answers
            .Select(a => a.QuestionId)
            .Distinct()
            .ToList();

        // Get visited question IDs (for conditional flow support)
        var visitedQuestionIds = response.VisitedQuestionIds ?? new List<int>();

        // Fallback: If VisitedQuestionIds is empty but we have answers, use answered questions
        // This handles legacy data and ensures validation works for sequential surveys
        if (!visitedQuestionIds.Any() && answeredQuestionIds.Any())
        {
            _logger.LogWarning(
                "Response {ResponseId} has answers but empty VisitedQuestionIds - using answered questions as fallback",
                responseId);
            visitedQuestionIds = answeredQuestionIds;
        }

        // For surveys without conditional flow, validate ALL required questions (not just visited)
        // This ensures sequential surveys check all required questions
        if (!visitedQuestionIds.Any())
        {
            // No questions visited at all - check ALL required questions
            var missingRequiredQuestions = requiredQuestionList
                .Where(q => !answeredQuestionIds.Contains(q.Id))
                .ToList();

            if (missingRequiredQuestions.Any())
            {
                var missingQuestionTitles = string.Join(", ", missingRequiredQuestions.Select(q => $"\"{q.QuestionText}\""));
                var errorMessage = $"Cannot complete response: {missingRequiredQuestions.Count} required question(s) not answered: {missingQuestionTitles}";

                _logger.LogWarning("Response {ResponseId} validation failed: {ErrorMessage}", responseId, errorMessage);

                throw new SurveyValidationException(errorMessage);
            }

            _logger.LogDebug("All required questions answered for response {ResponseId} (no visited tracking)", responseId);
            return;
        }

        // Find missing required questions (only check VISITED required questions for conditional flow)
        var missingQuestions = requiredQuestionList
            .Where(q => visitedQuestionIds.Contains(q.Id)) // Only validate visited questions
            .Where(q => !answeredQuestionIds.Contains(q.Id))
            .ToList();

        if (missingQuestions.Any())
        {
            var missingQuestionTitles = string.Join(", ", missingQuestions.Select(q => $"\"{q.QuestionText}\""));
            var errorMessage = $"Cannot complete response: {missingQuestions.Count} required question(s) not answered: {missingQuestionTitles}";

            _logger.LogWarning("Response {ResponseId} validation failed: {ErrorMessage}", responseId, errorMessage);

            throw new SurveyValidationException(errorMessage);
        }

        _logger.LogDebug("All visited required questions answered for response {ResponseId}", responseId);
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
        int? ratingValue = null,
        string? answerJson = null)
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
            QuestionType.Location => ValidateLocationAnswer(answerJson, question.IsRequired),
            QuestionType.Number => ValidateNumberAnswer(answerJson, question.IsRequired, question.OptionsJson),
            QuestionType.Date => ValidateDateAnswer(answerJson, question.IsRequired, question.OptionsJson),
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

    /// <inheritdoc/>
    public async Task RecordVisitedQuestionAsync(int responseId, int questionId)
    {
        _logger.LogInformation("Recording visited question {QuestionId} for response {ResponseId}", questionId, responseId);

        var response = await _responseRepository.GetByIdAsync(responseId);
        if (response == null)
        {
            _logger.LogWarning("Response {ResponseId} not found", responseId);
            throw new ResponseNotFoundException(responseId);
        }

        // Check if already visited
        if (response.HasVisitedQuestion(questionId))
        {
            _logger.LogWarning(
                "Question {QuestionId} already visited in response {ResponseId}",
                questionId, responseId);
            return;
        }

        // Record as visited
        response.RecordVisitedQuestion(questionId);
        await _responseRepository.UpdateAsync(response);

        _logger.LogInformation(
            "Question {QuestionId} recorded as visited for response {ResponseId}. Total visited: {VisitedCount}",
            questionId, responseId, response.VisitedQuestionIds.Count);
    }

    /// <inheritdoc/>
    public async Task<int?> GetNextQuestionAsync(int responseId)
    {
        _logger.LogInformation("Getting next question for response {ResponseId}", responseId);

        var response = await _responseRepository.GetByIdWithAnswersAsync(responseId);
        if (response == null)
        {
            _logger.LogWarning("Response {ResponseId} not found", responseId);
            throw new ResponseNotFoundException(responseId);
        }

        // Check if already completed
        if (response.IsComplete)
        {
            _logger.LogInformation("Response {ResponseId} is already complete, no next question", responseId);
            return null;
        }

        // Get last answer to determine next question
        var lastAnswer = response.Answers
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefault();

        if (lastAnswer == null)
        {
            // No answers yet, return first question by OrderIndex
            var survey = await _surveyRepository.GetByIdWithQuestionsAsync(response.SurveyId);
            if (survey == null)
            {
                _logger.LogError("Survey {SurveyId} not found for response {ResponseId}", response.SurveyId, responseId);
                throw new SurveyNotFoundException(response.SurveyId);
            }

            var firstQuestion = survey.Questions
                .OrderBy(q => q.OrderIndex)
                .FirstOrDefault();

            if (firstQuestion == null)
            {
                _logger.LogWarning("Survey {SurveyId} has no questions", response.SurveyId);
                return null;
            }

            _logger.LogInformation(
                "No answers yet for response {ResponseId}, returning first question {QuestionId}",
                responseId, firstQuestion.Id);

            return firstQuestion.Id;
        }

        // Check if last answer indicates end of survey using value object
        if (lastAnswer.Next.Type == NextStepType.EndSurvey)
        {
            _logger.LogInformation(
                "Response {ResponseId} reached end of survey, marking as complete",
                responseId);

            response.MarkAsComplete();
            await _responseRepository.UpdateAsync(response);

            return null;
        }

        // Return NextQuestionId from last answer's value object
        _logger.LogInformation(
            "Next question for response {ResponseId} is {NextQuestionId}",
            responseId, lastAnswer.Next.NextQuestionId);

        return lastAnswer.Next.NextQuestionId;
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
            CreatedAt = answer.CreatedAt
        };

        // Use pattern matching on answer.Value (new approach - no JSON parsing!)
        switch (answer.Value)
        {
            case TextAnswerValue textValue:
                dto.AnswerText = textValue.Text;
                break;

            case SingleChoiceAnswerValue singleChoice:
                dto.SelectedOptions = new List<string> { singleChoice.SelectedOption };
                break;

            case MultipleChoiceAnswerValue multipleChoice:
                dto.SelectedOptions = multipleChoice.SelectedOptions.ToList();
                break;

            case RatingAnswerValue ratingValue:
                dto.RatingValue = ratingValue.Rating;
                break;

            case LocationAnswerValue locationValue:
                dto.Latitude = locationValue.Latitude;
                dto.Longitude = locationValue.Longitude;
                dto.LocationAccuracy = locationValue.Accuracy;
                dto.LocationTimestamp = locationValue.Timestamp;
                break;

            case NumberAnswerValue numberValue:
                dto.NumberValue = numberValue.Value;
                break;

            case DateAnswerValue dateValue:
                dto.DateValue = dateValue.Date;
                break;

            case null:
                // Fallback for legacy data - try legacy AnswerText/AnswerJson
                dto.AnswerText = answer.AnswerText;
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
                        _logger.LogWarning(ex, "Failed to parse legacy answer JSON for answer {AnswerId}", answer.Id);
                    }
                }
                break;
        }

        // Add pre-computed display value for frontend consumption
        dto.DisplayValue = answer.Value?.DisplayValue;

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


    /// <summary>
    /// Validates location answer from JSON.
    /// </summary>
    private ValidationResult ValidateLocationAnswer(string? answerJson, bool isRequired)
    {
        if (isRequired && string.IsNullOrWhiteSpace(answerJson))
        {
            return ValidationResult.Failure("Location answer is required");
        }

        if (string.IsNullOrWhiteSpace(answerJson))
        {
            return ValidationResult.Success(); // Optional question with no answer
        }

        try
        {
            // Use LocationAnswerValue.FromJson() which includes validation
            var locationValue = LocationAnswerValue.FromJson(answerJson);

            _logger.LogInformation(
                "Location coordinates validated: Lat range {LatRange}, Lon range {LonRange}",
                GetCoordinateRange(locationValue.Latitude),
                GetCoordinateRange(locationValue.Longitude));

            return ValidationResult.Success();
        }
        catch (InvalidLocationException ex)
        {
            _logger.LogError(ex, "Invalid location data: {Json}", answerJson);
            return ValidationResult.Failure($"Invalid location data: {ex.Message}");
        }
        catch (InvalidAnswerFormatException ex)
        {
            _logger.LogError(ex, "Invalid answer format for location: {Json}", answerJson);
            return ValidationResult.Failure($"Invalid location answer format: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format for location answer: {Json}", answerJson);
            return ValidationResult.Failure("Invalid JSON format for location answer.");
        }
    }

    /// <summary>
    /// Returns a privacy-preserving coordinate range for logging.
    /// </summary>
    private static string GetCoordinateRange(double coordinate)
    {
        var rounded = Math.Floor(coordinate / 10) * 10;
        return $"{rounded} to {rounded + 10}";
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

    /// <summary>
    /// Validates number answer from JSON.
    /// Supports optional min/max range and decimal places validation.
    /// </summary>
    private ValidationResult ValidateNumberAnswer(string? answerJson, bool isRequired, string? optionsJson)
    {
        if (isRequired && string.IsNullOrWhiteSpace(answerJson))
        {
            return ValidationResult.Failure("Number answer is required");
        }

        if (string.IsNullOrWhiteSpace(answerJson))
        {
            return ValidationResult.Success(); // Optional question with no answer
        }

        try
        {
            // Use NumberAnswerValue.FromJson() which includes validation
            var numberValue = NumberAnswerValue.FromJson(answerJson);

            // If optionsJson contains additional validation rules, apply them
            if (!string.IsNullOrWhiteSpace(optionsJson))
            {
                var options = JsonSerializer.Deserialize<NumberOptions>(optionsJson);
                if (options != null)
                {
                    if (options.MinValue.HasValue && numberValue.Value < options.MinValue.Value)
                    {
                        return ValidationResult.Failure($"Number must be at least {options.MinValue.Value}");
                    }

                    if (options.MaxValue.HasValue && numberValue.Value > options.MaxValue.Value)
                    {
                        return ValidationResult.Failure($"Number must be at most {options.MaxValue.Value}");
                    }

                    if (options.DecimalPlaces.HasValue && options.DecimalPlaces.Value >= 0)
                    {
                        var actualDecimalPlaces = GetDecimalPlaces(numberValue.Value);
                        if (actualDecimalPlaces > options.DecimalPlaces.Value)
                        {
                            return ValidationResult.Failure($"Number cannot have more than {options.DecimalPlaces.Value} decimal place(s)");
                        }
                    }
                }
            }

            _logger.LogDebug("Number answer validated: {Value}", numberValue.Value);
            return ValidationResult.Success();
        }
        catch (InvalidAnswerFormatException ex)
        {
            _logger.LogError(ex, "Invalid number answer format: {Json}", answerJson);
            return ValidationResult.Failure($"Invalid number answer format: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format for number answer: {Json}", answerJson);
            return ValidationResult.Failure("Invalid JSON format for number answer.");
        }
    }

    /// <summary>
    /// Validates date answer from JSON.
    /// Supports optional min/max date range validation.
    /// </summary>
    private ValidationResult ValidateDateAnswer(string? answerJson, bool isRequired, string? optionsJson)
    {
        if (isRequired && string.IsNullOrWhiteSpace(answerJson))
        {
            return ValidationResult.Failure("Date answer is required");
        }

        if (string.IsNullOrWhiteSpace(answerJson))
        {
            return ValidationResult.Success(); // Optional question with no answer
        }

        try
        {
            // Use DateAnswerValue.FromJson() which includes validation
            var dateValue = DateAnswerValue.FromJson(answerJson);

            // If optionsJson contains additional validation rules, apply them
            if (!string.IsNullOrWhiteSpace(optionsJson))
            {
                var options = JsonSerializer.Deserialize<DateOptions>(optionsJson);
                if (options != null)
                {
                    if (options.MinDate.HasValue && dateValue.Date < options.MinDate.Value.Date)
                    {
                        return ValidationResult.Failure($"Date must be on or after {options.MinDate.Value:dd.MM.yyyy}");
                    }

                    if (options.MaxDate.HasValue && dateValue.Date > options.MaxDate.Value.Date)
                    {
                        return ValidationResult.Failure($"Date must be on or before {options.MaxDate.Value:dd.MM.yyyy}");
                    }
                }
            }

            _logger.LogDebug("Date answer validated: {Value}", dateValue.Date.ToString(DateAnswerValue.DateFormat));
            return ValidationResult.Success();
        }
        catch (InvalidAnswerFormatException ex)
        {
            _logger.LogError(ex, "Invalid date answer format: {Json}", answerJson);
            return ValidationResult.Failure($"Invalid date answer format: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format for date answer: {Json}", answerJson);
            return ValidationResult.Failure("Invalid JSON format for date answer.");
        }
    }

    /// <summary>
    /// Gets the number of decimal places in a decimal value.
    /// </summary>
    private static int GetDecimalPlaces(decimal value)
    {
        value = value / 1.000000000000000000000000000000000m;
        var text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var decimalIndex = text.IndexOf('.');
        if (decimalIndex < 0)
            return 0;
        return text.Length - decimalIndex - 1;
    }

    /// <summary>
    /// Number options from question configuration.
    /// </summary>
    private sealed class NumberOptions
    {
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int? DecimalPlaces { get; set; }
    }

    /// <summary>
    /// Date options from question configuration.
    /// </summary>
    private sealed class DateOptions
    {
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }
    }

    // Conditional Flow Logic

    /// <summary>
    /// Determines the next question ID based on the question type and answer.
    /// Implements the conditional flow logic priority: Conditional → Default → Sequential → End.
    /// </summary>
    /// <remarks>
    /// Question type classification for navigation:
    /// - Branching (SingleChoice, Rating): Each option can have individual flow (uses QuestionOption.Next)
    ///   - Rating: Uses rating value 1-5 as implicit option index 0-4
    ///   - SingleChoice: Uses actual QuestionOption entities
    /// - Non-branching (Text, MultipleChoice, Number, Date, Location): All answers use same flow (uses Question.DefaultNext)
    /// </remarks>
    private async Task<NextQuestionDeterminant> DetermineNextStepAsync(
        Question question,
        List<int>? selectedOptions,
        int surveyId,
        CancellationToken cancellationToken)
    {
        // For branching question types (SingleChoice and Rating)
        if (question.QuestionType == QuestionType.SingleChoice || question.QuestionType == QuestionType.Rating)
        {
            return await DetermineBranchingNextStepAsync(question, selectedOptions, cancellationToken);
        }

        // For non-branching question types (Text, MultipleChoice, Number, Date, Location)
        return await DetermineNonBranchingNextStepAsync(question, surveyId, cancellationToken);
    }

    /// <summary>
    /// Determines next question for branching question types (SingleChoice, Rating).
    /// Priority: Option's Next → Question's DefaultNext → Sequential fallback → End.
    /// </summary>
    /// <remarks>
    /// Note: Both SingleChoice and Rating questions support conditional branching.
    /// Rating questions may optionally have QuestionOptions for per-rating flow configuration.
    /// If QuestionOptions are not configured, Rating uses DefaultNext (same as non-branching types).
    /// </remarks>
    private async Task<NextQuestionDeterminant> DetermineBranchingNextStepAsync(
        Question question,
        List<int>? selectedOptions,
        CancellationToken cancellationToken)
    {
        // For Rating questions without QuestionOptions, fall back to DefaultNext
        // This maintains backward compatibility with existing Rating questions
        if (question.QuestionType == QuestionType.Rating && (question.Options == null || !question.Options.Any()))
        {
            _logger.LogInformation(
                "Rating question {QuestionId} has no QuestionOptions, using DefaultNext for all rating values",
                question.Id);

            if (question.DefaultNext != null)
            {
                // Priority 1: Check for explicit EndSurvey configuration
                if (question.DefaultNext.Type == NextStepType.EndSurvey)
                {
                    _logger.LogInformation(
                        "Rating question {QuestionId} configured to end survey (DefaultNext.Type = EndSurvey)",
                        question.Id);
                    return NextQuestionDeterminant.End();
                }

                // Priority 2: Check for explicit GoToQuestion configuration
                if (question.DefaultNext.Type == NextStepType.GoToQuestion)
                {
                    _logger.LogInformation(
                        "Rating question {QuestionId} using DefaultNext flow: GoToQuestion({NextQuestionId})",
                        question.Id, question.DefaultNext.NextQuestionId);
                    return question.DefaultNext;
                }
            }

            // Priority 3: Fall back to sequential navigation (when DefaultNext is null or has unexpected type)
            _logger.LogDebug(
                "Rating question {QuestionId} has no explicit DefaultNext configuration, using sequential flow",
                question.Id);

            var nextId = await GetNextSequentialQuestionIdAsync(
                question.SurveyId,
                question.OrderIndex,
                cancellationToken);

            if (nextId > 0)
            {
                _logger.LogInformation(
                    "Rating question {QuestionId} sequential flow: GoToQuestion({NextQuestionId})",
                    question.Id, nextId);
                return NextQuestionDeterminant.ToQuestion(nextId);
            }
            else
            {
                _logger.LogInformation(
                    "Rating question {QuestionId} is the last question, ending survey",
                    question.Id);
                return NextQuestionDeterminant.End();
            }
        }

        // Get the first selected option (single-choice/rating has only one)
        if (selectedOptions == null || !selectedOptions.Any())
        {
            _logger.LogWarning(
                "No option selected for branching question {QuestionId}, ending survey",
                question.Id);
            return NextQuestionDeterminant.End(); // No selection, end survey
        }

        var selectedOptionIndex = selectedOptions.First();

        // Find the QuestionOption entity for this selection
        var selectedOption = question.Options?
            .OrderBy(o => o.OrderIndex)
            .Skip(selectedOptionIndex)
            .FirstOrDefault();

        if (selectedOption == null)
        {
            _logger.LogWarning(
                "Invalid option index {OptionIndex} for question {QuestionId}, ending survey",
                selectedOptionIndex, question.Id);
            return NextQuestionDeterminant.End(); // Invalid option, end survey
        }

        // Priority 1: Check option's conditional flow (both GoToQuestion and EndSurvey)
        if (selectedOption.Next != null)
        {
            _logger.LogInformation(
                "Using option conditional flow for question {QuestionId}, option {OptionId}: {Next}",
                question.Id, selectedOption.Id, selectedOption.Next);
            return selectedOption.Next;
        }

        // Priority 2: Check question's default flow (both GoToQuestion and EndSurvey)
        if (question.DefaultNext != null)
        {
            _logger.LogInformation(
                "Using question default flow for question {QuestionId}: {DefaultNext}",
                question.Id, question.DefaultNext);
            return question.DefaultNext;
        }

        // Priority 3: Sequential fallback (backward compatibility)
        var sequentialNextId = await GetNextSequentialQuestionIdAsync(
            question.SurveyId,
            question.OrderIndex,
            cancellationToken);

        if (sequentialNextId > 0)
        {
            _logger.LogInformation(
                "Using sequential fallback for question {QuestionId}: NextQuestionId={NextQuestionId}",
                question.Id, sequentialNextId);
            return NextQuestionDeterminant.ToQuestion(sequentialNextId);
        }
        else
        {
            _logger.LogInformation(
                "No next question found for question {QuestionId}, ending survey",
                question.Id);
            return NextQuestionDeterminant.End();
        }
    }

    /// <summary>
    /// Determines next question for non-branching question types (Text, MultipleChoice).
    /// Priority: Question's DefaultNext → Sequential fallback → End.
    /// </summary>
    private async Task<NextQuestionDeterminant> DetermineNonBranchingNextStepAsync(
        Question question,
        int surveyId,
        CancellationToken cancellationToken)
    {
        // Priority 1: Check question's default flow
        if (question.DefaultNext != null)
        {
            // Check for EndSurvey type
            if (question.DefaultNext.Type == NextStepType.EndSurvey)
            {
                _logger.LogInformation(
                    "Question {QuestionId} configured to end survey (DefaultNext = EndSurvey)",
                    question.Id);
                return NextQuestionDeterminant.End();
            }

            // Check for GoToQuestion type
            if (question.DefaultNext.Type == NextStepType.GoToQuestion)
            {
                _logger.LogInformation(
                    "Using question default flow for non-branching question {QuestionId}: GoToQuestion({NextQuestionId})",
                    question.Id, question.DefaultNext.NextQuestionId);
                return question.DefaultNext;
            }
        }

        // Priority 2: Sequential fallback (backward compatibility)
        // Only used when DefaultNext is null (not configured)
        var sequentialNextId = await GetNextSequentialQuestionIdAsync(
            surveyId,
            question.OrderIndex,
            cancellationToken);

        if (sequentialNextId > 0)
        {
            _logger.LogInformation(
                "Using sequential fallback for non-branching question {QuestionId}: NextQuestionId={NextQuestionId}",
                question.Id, sequentialNextId);
            return NextQuestionDeterminant.ToQuestion(sequentialNextId);
        }
        else
        {
            _logger.LogInformation(
                "No next question found for non-branching question {QuestionId}, ending survey",
                question.Id);
            return NextQuestionDeterminant.End();
        }
    }

    /// <summary>
    /// Finds the next question in sequential order (backward compatibility).
    /// Returns 0 if no next question found (end of survey).
    /// </summary>
    private async Task<int> GetNextSequentialQuestionIdAsync(
        int surveyId,
        int currentOrderIndex,
        CancellationToken cancellationToken)
    {
        var nextQuestion = await _context.Questions
            .AsNoTracking()
            .Where(q => q.SurveyId == surveyId && q.OrderIndex > currentOrderIndex)
            .OrderBy(q => q.OrderIndex)
            .FirstOrDefaultAsync(cancellationToken);

        return nextQuestion?.Id ?? 0; // Return 0 if last question
    }
}
