namespace SurveyBot.Core.Exceptions;

/// <summary>
/// Exception thrown when survey structure contains a cycle in the question flow.
/// </summary>
public class SurveyCycleException : Exception
{
    /// <summary>
    /// Gets the sequence of questions forming the cycle.
    /// </summary>
    public List<int> CyclePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyCycleException"/> class.
    /// </summary>
    /// <param name="cyclePath">The sequence of question IDs forming the cycle.</param>
    /// <param name="message">The exception message.</param>
    public SurveyCycleException(List<int> cyclePath, string message)
        : base(message)
    {
        CyclePath = cyclePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyCycleException"/> class.
    /// </summary>
    /// <param name="cyclePath">The sequence of question IDs forming the cycle.</param>
    public SurveyCycleException(List<int> cyclePath)
        : base($"Survey contains a cycle involving {cyclePath.Count} questions")
    {
        CyclePath = cyclePath;
    }
}
