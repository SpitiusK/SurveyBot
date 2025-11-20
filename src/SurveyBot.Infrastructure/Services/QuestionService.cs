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
    private readonly IQuestionBranchingRuleRepository _branchingRuleRepository;
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
        IQuestionBranchingRuleRepository branchingRuleRepository,
        IMapper mapper,
        ILogger<QuestionService> logger)
    {
        _questionRepository = questionRepository;
        _surveyRepository = surveyRepository;
        _branchingRuleRepository = branchingRuleRepository;
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

    /// <inheritdoc/>
    public async Task<int?> EvaluateBranchingRuleAsync(int sourceQuestionId, string answerValue)
    {
        _logger.LogInformation("Evaluating branching rules for question {QuestionId} with answer: {AnswerValue}",
            sourceQuestionId, answerValue);

        // Get the source question to validate
        var sourceQuestion = await _questionRepository.GetByIdAsync(sourceQuestionId);
        if (sourceQuestion == null)
        {
            _logger.LogWarning("Source question {QuestionId} not found", sourceQuestionId);
            return null;
        }

        // Get all branching rules for this source question
        var rules = await _branchingRuleRepository.GetBySourceQuestionAsync(sourceQuestionId);

        foreach (var rule in rules)
        {
            try
            {
                // Deserialize the condition
                var condition = JsonSerializer.Deserialize<BranchingCondition>(rule.ConditionJson);
                if (condition == null) continue;

                // Evaluate the condition
                if (await EvaluateConditionAsync(condition, answerValue))
                {
                    _logger.LogInformation("Branching rule matched for question {SourceId} -> {TargetId}",
                        sourceQuestionId, rule.TargetQuestionId);
                    return rule.TargetQuestionId;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize branching condition for rule {RuleId}", rule.Id);
            }
        }

        _logger.LogInformation("No branching rule matched for question {QuestionId}", sourceQuestionId);
        return null;
    }

    /// <inheritdoc/>
    public async Task<int?> GetNextQuestionAsync(int currentQuestionId, string answerValue, int surveyId)
    {
        _logger.LogInformation("Getting next question after {QuestionId} in survey {SurveyId}",
            currentQuestionId, surveyId);

        // Try to find a matching branching rule
        var branchTarget = await EvaluateBranchingRuleAsync(currentQuestionId, answerValue);
        if (branchTarget.HasValue)
        {
            _logger.LogInformation("Branching to question {TargetId}", branchTarget.Value);
            return branchTarget.Value;
        }

        // No branching rule matched, get next sequential question
        var currentQuestion = await _questionRepository.GetByIdAsync(currentQuestionId);
        if (currentQuestion == null)
        {
            _logger.LogWarning("Current question {QuestionId} not found", currentQuestionId);
            return null;
        }

        // Get all questions in the survey
        var questions = await _questionRepository.GetBySurveyIdAsync(surveyId);
        var questionList = questions.OrderBy(q => q.OrderIndex).ToList();

        // Find the current question's index
        var currentIndex = questionList.FindIndex(q => q.Id == currentQuestionId);
        if (currentIndex == -1 || currentIndex == questionList.Count - 1)
        {
            _logger.LogInformation("Survey complete - no more questions");
            return null; // Survey complete
        }

        var nextQuestion = questionList[currentIndex + 1];
        _logger.LogInformation("Next sequential question is {QuestionId}", nextQuestion.Id);
        return nextQuestion.Id;
    }

    /// <inheritdoc/>
    public async Task<bool> SupportsConditionAsync(int questionId)
    {
        var question = await _questionRepository.GetByIdAsync(questionId);
        if (question == null)
        {
            return false;
        }

        // Initially, only SingleChoice questions support branching
        // This can be expanded in the future to support other types
        return question.QuestionType == QuestionType.SingleChoice;
    }

    /// <inheritdoc/>
    public async Task<bool> HasCyclicDependencyAsync(int sourceQuestionId, int targetQuestionId)
    {
        _logger.LogInformation("Checking for cyclic dependency: {SourceId} -> {TargetId}",
            sourceQuestionId, targetQuestionId);

        // If source and target are the same, it's a cycle
        if (sourceQuestionId == targetQuestionId)
        {
            _logger.LogWarning("Self-reference detected: {QuestionId}", sourceQuestionId);
            return true;
        }

        var visited = new HashSet<int>();
        var hasCycle = await DetectCycleAsync(targetQuestionId, sourceQuestionId, visited);

        if (hasCycle)
        {
            _logger.LogWarning("Cycle detected: adding rule {SourceId} -> {TargetId} would create a cycle",
                sourceQuestionId, targetQuestionId);
        }

        return hasCycle;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> DetectAllCyclesAsync(int surveyId)
    {
        _logger.LogInformation("Detecting all cycles in survey {SurveyId}", surveyId);

        var cycles = new List<string>();
        var rules = await _branchingRuleRepository.GetBySurveyIdAsync(surveyId);
        var questions = await _questionRepository.GetBySurveyIdAsync(surveyId);
        var questionDict = questions.ToDictionary(q => q.Id);

        foreach (var question in questions)
        {
            var visited = new HashSet<int>();
            var path = new List<int> { question.Id };

            await DetectCyclesRecursive(question.Id, visited, path, cycles, questionDict);
        }

        return cycles.Distinct();
    }

    /// <inheritdoc/>
    public async Task ValidateBranchingRuleAsync(QuestionBranchingRule rule)
    {
        _logger.LogInformation("Validating branching rule: {SourceId} -> {TargetId}",
            rule.SourceQuestionId, rule.TargetQuestionId);

        // 1. Check for self-reference
        if (rule.SourceQuestionId == rule.TargetQuestionId)
        {
            throw new QuestionValidationException(
                "A question cannot branch to itself.");
        }

        // 2. Check that both questions exist
        var sourceQuestion = await _questionRepository.GetByIdAsync(rule.SourceQuestionId);
        if (sourceQuestion == null)
        {
            throw new QuestionNotFoundException(rule.SourceQuestionId);
        }

        var targetQuestion = await _questionRepository.GetByIdAsync(rule.TargetQuestionId);
        if (targetQuestion == null)
        {
            throw new QuestionNotFoundException(rule.TargetQuestionId);
        }

        // 3. Check that both questions are in the same survey
        if (sourceQuestion.SurveyId != targetQuestion.SurveyId)
        {
            throw new QuestionValidationException(
                "Source and target questions must be in the same survey.");
        }

        // 4. Check that source question type supports branching
        if (!await SupportsConditionAsync(rule.SourceQuestionId))
        {
            throw new QuestionValidationException(
                $"Question type '{sourceQuestion.QuestionType}' does not support branching conditions.");
        }

        // 5. Check for existing rule
        var existingRule = await _branchingRuleRepository.GetBySourceAndTargetAsync(
            rule.SourceQuestionId, rule.TargetQuestionId);
        if (existingRule != null && existingRule.Id != rule.Id)
        {
            throw new QuestionValidationException(
                "A branching rule already exists between these questions.");
        }

        // 6. Check for circular dependency
        if (await HasCyclicDependencyAsync(rule.SourceQuestionId, rule.TargetQuestionId))
        {
            throw new QuestionValidationException(
                "Adding this branching rule would create a circular dependency.");
        }

        _logger.LogInformation("Branching rule validation passed");
    }

    #region Private Helper Methods

    /// <summary>
    /// Evaluates a single branching condition against an answer value.
    /// </summary>
    private Task<bool> EvaluateConditionAsync(BranchingCondition condition, string answerValue)
    {
        if (string.IsNullOrWhiteSpace(answerValue))
        {
            _logger.LogDebug("Condition evaluation: answer value is null or empty");
            return Task.FromResult(false);
        }

        try
        {
            _logger.LogDebug(
                "Evaluating condition: operator={Operator}, answerValue='{AnswerValue}', conditionValues=[{ConditionValues}]",
                condition.Operator,
                answerValue,
                string.Join(", ", condition.Values.Select(v => $"'{v}'")));

            bool result = condition.Operator switch
            {
                BranchingOperator.Equals => condition.Values.Length > 0 &&
                    string.Equals(answerValue, condition.Values[0], StringComparison.OrdinalIgnoreCase),

                BranchingOperator.Contains => condition.Values.Length > 0 &&
                    answerValue.Contains(condition.Values[0], StringComparison.OrdinalIgnoreCase),

                BranchingOperator.In => condition.Values.Any(v =>
                    string.Equals(answerValue, v, StringComparison.OrdinalIgnoreCase)),

                BranchingOperator.GreaterThan => condition.Values.Length > 0 &&
                    int.TryParse(answerValue, out int gtValue) &&
                    int.TryParse(condition.Values[0], out int gtThreshold) &&
                    gtValue > gtThreshold,

                BranchingOperator.LessThan => condition.Values.Length > 0 &&
                    int.TryParse(answerValue, out int ltValue) &&
                    int.TryParse(condition.Values[0], out int ltThreshold) &&
                    ltValue < ltThreshold,

                BranchingOperator.GreaterThanOrEqual => condition.Values.Length > 0 &&
                    int.TryParse(answerValue, out int gteValue) &&
                    int.TryParse(condition.Values[0], out int gteThreshold) &&
                    gteValue >= gteThreshold,

                BranchingOperator.LessThanOrEqual => condition.Values.Length > 0 &&
                    int.TryParse(answerValue, out int lteValue) &&
                    int.TryParse(condition.Values[0], out int lteThreshold) &&
                    lteValue <= lteThreshold,

                _ => false
            };

            _logger.LogDebug("Condition evaluation result: {Result}", result);

            if (!result && condition.Operator > BranchingOperator.In)
            {
                // Log warning for unknown operators only
                _logger.LogWarning("Unknown or unhandled branching operator: {Operator}", condition.Operator);
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating condition with operator {Operator}", condition.Operator);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Recursive cycle detection helper using depth-first search.
    /// </summary>
    private async Task<bool> DetectCycleAsync(int currentId, int targetId, HashSet<int> visited)
    {
        // Already checked this path
        if (visited.Contains(currentId))
        {
            return false;
        }

        visited.Add(currentId);

        // Found the target - cycle exists
        if (currentId == targetId)
        {
            return true;
        }

        // Get all questions that branch FROM currentId
        var outgoingRules = await _branchingRuleRepository.GetBySourceQuestionAsync(currentId);

        foreach (var rule in outgoingRules)
        {
            if (await DetectCycleAsync(rule.TargetQuestionId, targetId, visited))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Recursive helper to detect all cycles in a survey.
    /// </summary>
    private async Task DetectCyclesRecursive(
        int currentId,
        HashSet<int> visited,
        List<int> path,
        List<string> cycles,
        Dictionary<int, Question> questionDict)
    {
        if (visited.Contains(currentId))
        {
            // Check if this completes a cycle
            var cycleStartIndex = path.IndexOf(currentId);
            if (cycleStartIndex >= 0)
            {
                var cyclePath = path.Skip(cycleStartIndex).Append(currentId);
                var cycleDescription = string.Join(" -> ", cyclePath.Select(id =>
                    questionDict.ContainsKey(id) ? $"Q{questionDict[id].OrderIndex + 1}" : $"Q?"));
                cycles.Add(cycleDescription);
            }
            return;
        }

        visited.Add(currentId);

        var outgoingRules = await _branchingRuleRepository.GetBySourceQuestionAsync(currentId);
        foreach (var rule in outgoingRules)
        {
            path.Add(rule.TargetQuestionId);
            await DetectCyclesRecursive(rule.TargetQuestionId, new HashSet<int>(visited), path, cycles, questionDict);
            path.RemoveAt(path.Count - 1);
        }
    }

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
