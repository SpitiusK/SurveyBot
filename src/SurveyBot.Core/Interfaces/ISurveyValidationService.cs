namespace SurveyBot.Core.Interfaces;

/// <summary>
/// Service for validating survey structure and detecting cycles.
/// </summary>
public interface ISurveyValidationService
{
    /// <summary>
    /// Detects if survey contains cycles in question flow.
    /// </summary>
    /// <param name="surveyId">Survey to validate</param>
    /// <returns>Cycle detection result with details</returns>
    Task<CycleDetectionResult> DetectCycleAsync(int surveyId);

    /// <summary>
    /// Validates that all questions form a valid DAG and have proper endpoints.
    /// </summary>
    /// <param name="surveyId">Survey to validate</param>
    /// <returns>True if survey is valid, false otherwise</returns>
    Task<bool> ValidateSurveyStructureAsync(int surveyId);

    /// <summary>
    /// Finds all questions that end the survey (point to EndOfSurveyMarker).
    /// </summary>
    /// <param name="surveyId">Survey ID</param>
    /// <returns>List of endpoint question IDs</returns>
    Task<List<int>> FindSurveyEndpointsAsync(int surveyId);
}

/// <summary>
/// Result of cycle detection operation.
/// </summary>
public class CycleDetectionResult
{
    /// <summary>
    /// Whether a cycle was detected.
    /// </summary>
    public bool HasCycle { get; set; }

    /// <summary>
    /// The sequence of questions forming the cycle (if detected).
    /// Example: [Q1, Q3, Q5, Q3] shows Q3 → Q5 → Q3 cycle
    /// </summary>
    public List<int>? CyclePath { get; set; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
