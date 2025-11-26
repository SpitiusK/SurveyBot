using Microsoft.Extensions.Logging;
using SurveyBot.Core.Constants;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Enums;
using SurveyBot.Core.Interfaces;
using SurveyBot.Core.ValueObjects;

namespace SurveyBot.Infrastructure.Services;

/// <summary>
/// Service for validating survey structure and detecting cycles in question flow.
/// Implements Depth-First Search (DFS) algorithm for cycle detection.
/// </summary>
public class SurveyValidationService : ISurveyValidationService
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ILogger<SurveyValidationService> _logger;

    public SurveyValidationService(
        IQuestionRepository questionRepository,
        ILogger<SurveyValidationService> logger)
    {
        _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CycleDetectionResult> DetectCycleAsync(int surveyId)
    {
        _logger.LogDebug("Starting cycle detection for survey {SurveyId}", surveyId);

        try
        {
            // Get all questions with Options and DefaultNextQuestion eager-loaded
            var questionList = await _questionRepository.GetWithFlowConfigurationAsync(surveyId);

            if (!questionList.Any())
            {
                _logger.LogDebug("Survey {SurveyId} has no questions, no cycle possible", surveyId);
                return new CycleDetectionResult
                {
                    HasCycle = false,
                    CyclePath = null,
                    ErrorMessage = null
                };
            }

            // Convert to dictionary for O(1) lookup
            var questionDict = questionList.ToDictionary(q => q.Id);

            // DFS tracking
            var visited = new HashSet<int>();
            var recursionStack = new HashSet<int>();
            var pathStack = new Stack<int>();

            // Try DFS from each unvisited question (handles disconnected components)
            foreach (var question in questionList)
            {
                if (!visited.Contains(question.Id))
                {
                    _logger.LogDebug("Starting DFS from question {QuestionId}", question.Id);

                    if (HasCycleDFS(question.Id, questionDict, visited, recursionStack, pathStack))
                    {
                        // Cycle found - extract cycle path from stack
                        var cyclePath = pathStack.ToList();
                        cyclePath.Reverse(); // Stack is LIFO, reverse for correct order

                        var cycleMessage = FormatCyclePath(cyclePath, questionDict);
                        _logger.LogWarning("Cycle detected in survey {SurveyId}: {CycleMessage}", surveyId, cycleMessage);

                        return new CycleDetectionResult
                        {
                            HasCycle = true,
                            CyclePath = cyclePath,
                            ErrorMessage = cycleMessage
                        };
                    }
                }
            }

            _logger.LogDebug("No cycles detected in survey {SurveyId}", surveyId);

            return new CycleDetectionResult
            {
                HasCycle = false,
                CyclePath = null,
                ErrorMessage = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting cycle in survey {SurveyId}", surveyId);

            // Return safe default (assume cycle for safety)
            return new CycleDetectionResult
            {
                HasCycle = true,
                CyclePath = null,
                ErrorMessage = $"Error during cycle detection: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Recursive DFS helper for cycle detection.
    /// </summary>
    /// <returns>True if cycle found, false otherwise</returns>
    private bool HasCycleDFS(
        int questionId,
        Dictionary<int, Question> questionDict,
        HashSet<int> visited,
        HashSet<int> recursionStack,
        Stack<int> pathStack)
    {
        // Mark as visited and add to recursion stack
        visited.Add(questionId);
        recursionStack.Add(questionId);
        pathStack.Push(questionId);

        _logger.LogDebug("DFS visiting question {QuestionId}", questionId);

        // Get all next question IDs from this question
        var nextQuestionIds = GetNextQuestionIds(questionDict[questionId], questionDict);

        foreach (var nextId in nextQuestionIds)
        {
            // Skip if next question doesn't exist in survey (invalid reference)
            if (!questionDict.ContainsKey(nextId))
            {
                _logger.LogWarning("Question {QuestionId} references non-existent question {NextId}", questionId, nextId);
                continue;
            }

            // If not visited, recurse
            if (!visited.Contains(nextId))
            {
                if (HasCycleDFS(nextId, questionDict, visited, recursionStack, pathStack))
                {
                    return true; // Cycle found in recursion
                }
            }
            // If in recursion stack, we found a cycle
            else if (recursionStack.Contains(nextId))
            {
                // Add the cycle-closing node to path for clarity
                pathStack.Push(nextId);

                _logger.LogDebug("Cycle detected: Question {CurrentId} → {NextId} (already in stack)", questionId, nextId);
                return true;
            }
        }

        // Backtrack: remove from recursion stack (but keep in visited)
        recursionStack.Remove(questionId);
        pathStack.Pop();

        return false;
    }

    /// <summary>
    /// Extracts all next question IDs from a question (handles branching).
    /// </summary>
    private List<int> GetNextQuestionIds(Question question, Dictionary<int, Question> questionDict)
    {
        var nextIds = new List<int>();

        // Check if question supports branching (SingleChoice, Rating with options)
        if (question.SupportsBranching && question.Options != null && question.Options.Any())
        {
            // Branching: collect NextQuestionId from all options
            foreach (var option in question.Options)
            {
                if (option.Next != null && option.Next.Type == NextStepType.GoToQuestion)
                {
                    nextIds.Add(option.Next.NextQuestionId!.Value);
                }
            }

            _logger.LogDebug("Question {QuestionId} (branching) has {Count} next options: {NextIds}",
                question.Id, nextIds.Count, string.Join(", ", nextIds));
        }
        else
        {
            // Non-branching: use DefaultNext
            if (question.DefaultNext != null && question.DefaultNext.Type == NextStepType.GoToQuestion)
            {
                nextIds.Add(question.DefaultNext.NextQuestionId!.Value);
                _logger.LogDebug("Question {QuestionId} (non-branching) points to {NextId}",
                    question.Id, question.DefaultNext.NextQuestionId.Value);
            }
            else
            {
                _logger.LogDebug("Question {QuestionId} has no next question (end of survey)", question.Id);
            }
        }

        // Remove duplicates and return unique next IDs
        return nextIds.Distinct().ToList();
    }

    /// <summary>
    /// Formats cycle path into human-readable error message.
    /// </summary>
    private string FormatCyclePath(List<int> cyclePath, Dictionary<int, Question> questionDict)
    {
        if (cyclePath == null || cyclePath.Count == 0)
        {
            return "Cycle detected (path unavailable)";
        }

        var pathDescriptions = cyclePath
            .Select(id => questionDict.ContainsKey(id)
                ? $"Q{id} ({TruncateText(questionDict[id].QuestionText, 30)})"
                : $"Q{id} (unknown)")
            .ToList();

        return $"Cycle detected in question flow: {string.Join(" → ", pathDescriptions)}";
    }

    /// <summary>
    /// Truncates text for display in error messages.
    /// </summary>
    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "(empty)";
        }

        return text.Length <= maxLength
            ? text
            : text.Substring(0, maxLength) + "...";
    }

    /// <inheritdoc />
    public async Task<bool> ValidateSurveyStructureAsync(int surveyId)
    {
        _logger.LogDebug("Validating survey structure for survey {SurveyId}", surveyId);

        try
        {
            // Check for cycles
            var cycleResult = await DetectCycleAsync(surveyId);
            if (cycleResult.HasCycle)
            {
                _logger.LogWarning("Survey {SurveyId} validation failed: {ErrorMessage}",
                    surveyId, cycleResult.ErrorMessage);
                return false;
            }

            // Check for at least one endpoint (question pointing to end-of-survey)
            var endpoints = await FindSurveyEndpointsAsync(surveyId);
            if (endpoints.Count == 0)
            {
                _logger.LogWarning("Survey {SurveyId} validation failed: No endpoints found (no question points to end-of-survey)",
                    surveyId);
                return false;
            }

            _logger.LogInformation("Survey {SurveyId} structure is valid: No cycles, {EndpointCount} endpoint(s) found",
                surveyId, endpoints.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating survey structure for survey {SurveyId}", surveyId);
            return false; // Safe default: assume invalid
        }
    }

    /// <inheritdoc />
    public async Task<List<int>> FindSurveyEndpointsAsync(int surveyId)
    {
        _logger.LogDebug("Finding survey endpoints for survey {SurveyId}", surveyId);

        try
        {
            var questionList = await _questionRepository.GetWithFlowConfigurationAsync(surveyId);
            var endpoints = new List<int>();

            foreach (var question in questionList)
            {
                bool isEndpoint = false;

                // Check if question has branching (options with Next)
                if (question.SupportsBranching && question.Options != null && question.Options.Any())
                {
                    // Branching question: check if ANY option points to end-of-survey
                    if (question.Options.Any(opt => opt.Next != null &&
                                                    opt.Next.Type == NextStepType.EndSurvey))
                    {
                        isEndpoint = true;
                        _logger.LogDebug("Question {QuestionId} is an endpoint (branching question with option pointing to end-of-survey)",
                            question.Id);
                    }
                }
                else
                {
                    // Non-branching: check DefaultNext
                    // Treat both NULL and EndSurvey as end-of-survey markers
                    if (question.DefaultNext == null)
                    {
                        // NULL = no next question specified → end of survey
                        isEndpoint = true;
                        _logger.LogDebug("Question {QuestionId} is an endpoint (DefaultNext is NULL, treated as end-of-survey)",
                            question.Id);
                    }
                    else if (question.DefaultNext.Type == NextStepType.EndSurvey)
                    {
                        // EndSurvey = explicit end marker → end of survey
                        isEndpoint = true;
                        _logger.LogDebug("Question {QuestionId} is an endpoint (DefaultNext is EndSurvey, explicit end marker)",
                            question.Id);
                    }
                }

                if (isEndpoint)
                {
                    endpoints.Add(question.Id);
                }
            }

            _logger.LogDebug("Found {Count} endpoint(s) in survey {SurveyId}: {Endpoints}",
                endpoints.Count, surveyId, string.Join(", ", endpoints));

            return endpoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding survey endpoints for survey {SurveyId}", surveyId);
            return new List<int>(); // Safe default: empty list
        }
    }
}
