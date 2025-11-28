using System.Text.Json;
using System.Text.Json.Serialization;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Value object representing a multiple-choice answer.
/// Immutable with value semantics.
/// </summary>
public sealed class MultipleChoiceAnswerValue : AnswerValue
{
    /// <summary>
    /// Gets the selected options (in the order they were selected).
    /// </summary>
    [JsonPropertyName("selectedOptions")]
    public IReadOnlyList<string> SelectedOptions { get; private set; }

    /// <summary>
    /// Gets the zero-based indices of selected options.
    /// May contain -1 values if indices were unknown during parsing from database.
    /// </summary>
    [JsonPropertyName("selectedOptionIndices")]
    public IReadOnlyList<int> SelectedOptionIndices { get; private set; }

    /// <inheritdoc />
    [JsonIgnore]
    public override QuestionType QuestionType => QuestionType.MultipleChoice;

    /// <inheritdoc />
    [JsonIgnore]
    public override string DisplayValue => string.Join(", ", SelectedOptions);

    /// <summary>
    /// Private constructor - use Create() factory method.
    /// </summary>
    private MultipleChoiceAnswerValue(
        IReadOnlyList<string> selectedOptions,
        IReadOnlyList<int> selectedOptionIndices)
    {
        SelectedOptions = selectedOptions;
        SelectedOptionIndices = selectedOptionIndices;
    }

    /// <summary>
    /// JSON constructor for deserialization.
    /// </summary>
    [JsonConstructor]
    private MultipleChoiceAnswerValue() : this(Array.Empty<string>(), Array.Empty<int>())
    {
    }

    /// <summary>
    /// Creates a multiple-choice answer with validation.
    /// </summary>
    /// <param name="selectedOptions">List of selected option texts</param>
    /// <param name="validOptions">All valid options from the question</param>
    /// <returns>Validated multiple-choice answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If any option is invalid</exception>
    public static MultipleChoiceAnswerValue Create(
        IEnumerable<string> selectedOptions,
        IReadOnlyCollection<QuestionOption> validOptions)
    {
        if (selectedOptions == null)
            throw new ArgumentNullException(nameof(selectedOptions));

        var selectedList = selectedOptions.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        if (selectedList.Count == 0)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.MultipleChoice,
                "At least one option must be selected");

        if (validOptions == null || validOptions.Count == 0)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.MultipleChoice,
                "Question has no valid options");

        var optionsList = validOptions.OrderBy(o => o.OrderIndex).ToList();
        var indices = new List<int>();
        var validatedOptions = new List<string>();

        foreach (var selected in selectedList)
        {
            var index = optionsList.FindIndex(o =>
                string.Equals(o.Text, selected, StringComparison.OrdinalIgnoreCase));

            if (index < 0)
                throw new InvalidAnswerFormatException(
                    0,
                    QuestionType.MultipleChoice,
                    $"Option '{selected}' not found. Valid options: {string.Join(", ", optionsList.Select(o => $"'{o.Text}'"))}");

            // Prevent duplicate selections
            if (!indices.Contains(index))
            {
                indices.Add(index);
                validatedOptions.Add(optionsList[index].Text);
            }
        }

        return new MultipleChoiceAnswerValue(validatedOptions, indices);
    }

    /// <summary>
    /// Creates a multiple-choice answer from simple string options (legacy format).
    /// </summary>
    /// <param name="selectedOptions">List of selected option texts</param>
    /// <param name="validOptions">All valid option texts</param>
    /// <returns>Validated multiple-choice answer</returns>
    public static MultipleChoiceAnswerValue CreateFromStrings(
        IEnumerable<string> selectedOptions,
        IEnumerable<string> validOptions)
    {
        if (selectedOptions == null)
            throw new ArgumentNullException(nameof(selectedOptions));

        var selectedList = selectedOptions.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        if (selectedList.Count == 0)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.MultipleChoice,
                "At least one option must be selected");

        var optionsList = validOptions?.Where(o => !string.IsNullOrWhiteSpace(o)).ToList()
            ?? throw new InvalidAnswerFormatException(0, QuestionType.MultipleChoice, "Question has no valid options");

        if (optionsList.Count == 0)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.MultipleChoice,
                "Question has no valid options");

        var indices = new List<int>();
        var validatedOptions = new List<string>();

        foreach (var selected in selectedList)
        {
            var index = optionsList.FindIndex(o =>
                string.Equals(o, selected, StringComparison.OrdinalIgnoreCase));

            if (index < 0)
                throw new InvalidAnswerFormatException(
                    0,
                    QuestionType.MultipleChoice,
                    $"Option '{selected}' not found. Valid options: {string.Join(", ", optionsList.Select(o => $"'{o}'"))}");

            // Prevent duplicate selections
            if (!indices.Contains(index))
            {
                indices.Add(index);
                validatedOptions.Add(optionsList[index]);
            }
        }

        return new MultipleChoiceAnswerValue(validatedOptions, indices);
    }

    /// <summary>
    /// Creates by option indices (when you already know the indices).
    /// </summary>
    /// <param name="optionIndices">Zero-based option indices</param>
    /// <param name="validOptions">All valid options from the question</param>
    /// <returns>Validated multiple-choice answer</returns>
    public static MultipleChoiceAnswerValue CreateByIndices(
        IEnumerable<int> optionIndices,
        IReadOnlyCollection<QuestionOption> validOptions)
    {
        if (optionIndices == null)
            throw new ArgumentNullException(nameof(optionIndices));

        if (validOptions == null || validOptions.Count == 0)
            throw new InvalidAnswerFormatException(0, QuestionType.MultipleChoice, "No valid options");

        var optionsList = validOptions.OrderBy(o => o.OrderIndex).ToList();
        var indices = optionIndices.Distinct().ToList();

        if (indices.Count == 0)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.MultipleChoice,
                "At least one option must be selected");

        var validatedOptions = new List<string>();

        foreach (var index in indices)
        {
            if (index < 0 || index >= optionsList.Count)
                throw new InvalidAnswerFormatException(
                    0,
                    QuestionType.MultipleChoice,
                    $"Option index {index} out of range (0-{optionsList.Count - 1})");

            validatedOptions.Add(optionsList[index].Text);
        }

        return new MultipleChoiceAnswerValue(validatedOptions, indices);
    }

    /// <summary>
    /// Creates without validation (for parsing from trusted database storage).
    /// </summary>
    /// <param name="selectedOptions">The selected option texts</param>
    /// <param name="selectedOptionIndices">The option indices (empty if unknown)</param>
    /// <returns>Multiple-choice answer</returns>
    internal static MultipleChoiceAnswerValue CreateTrusted(
        IEnumerable<string> selectedOptions,
        IEnumerable<int>? selectedOptionIndices = null)
    {
        var options = selectedOptions?.Where(o => !string.IsNullOrWhiteSpace(o)).ToList()
            ?? throw new InvalidAnswerFormatException(0, QuestionType.MultipleChoice, "Selected options cannot be null");

        if (options.Count == 0)
            throw new InvalidAnswerFormatException(0, QuestionType.MultipleChoice, "At least one option must be selected");

        var indices = selectedOptionIndices?.ToList() ?? Enumerable.Repeat(-1, options.Count).ToList();

        return new MultipleChoiceAnswerValue(options, indices);
    }

    /// <summary>
    /// Parses multiple-choice answer from JSON storage format.
    /// </summary>
    /// <param name="json">JSON string from database</param>
    /// <returns>Parsed multiple-choice answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If JSON is invalid</exception>
    public static MultipleChoiceAnswerValue FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAnswerFormatException(0, QuestionType.MultipleChoice, "JSON cannot be empty");

        try
        {
            var data = JsonSerializer.Deserialize<MultipleChoiceData>(json);

            if (data == null || data.SelectedOptions == null || data.SelectedOptions.Count == 0)
                throw new InvalidAnswerFormatException(0, QuestionType.MultipleChoice, "selectedOptions missing or empty in JSON");

            var validOptions = data.SelectedOptions.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
            if (validOptions.Count == 0)
                throw new InvalidAnswerFormatException(0, QuestionType.MultipleChoice, "selectedOptions contains no valid values");

            var indices = data.SelectedOptionIndices?.ToList()
                ?? Enumerable.Repeat(-1, validOptions.Count).ToList();

            // Ensure indices list matches options list size
            while (indices.Count < validOptions.Count)
                indices.Add(-1);

            return new MultipleChoiceAnswerValue(validOptions, indices.Take(validOptions.Count).ToList());
        }
        catch (JsonException ex)
        {
            throw new InvalidAnswerFormatException(
                $"Invalid JSON for multiple-choice answer: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public override string ToJson() =>
        JsonSerializer.Serialize(new MultipleChoiceData
        {
            SelectedOptions = SelectedOptions.ToList(),
            SelectedOptionIndices = SelectedOptionIndices.Any(i => i >= 0)
                ? SelectedOptionIndices.ToList()
                : null
        });

    /// <inheritdoc />
    public override bool IsValidFor(Question question)
    {
        if (question.QuestionType != QuestionType.MultipleChoice)
            return false;

        // Check all selected options exist in question's options
        return SelectedOptions.All(selected =>
            question.Options.Any(o =>
                string.Equals(o.Text, selected, StringComparison.OrdinalIgnoreCase)));
    }

    #region Equality

    /// <inheritdoc />
    public override bool Equals(AnswerValue? other)
    {
        if (other is not MultipleChoiceAnswerValue multiple)
            return false;

        // Compare sorted option lists for order-independent equality
        var thisOptions = SelectedOptions.OrderBy(o => o).ToList();
        var otherOptions = multiple.SelectedOptions.OrderBy(o => o).ToList();

        return thisOptions.SequenceEqual(otherOptions);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(QuestionType);
        foreach (var option in SelectedOptions.OrderBy(o => o))
        {
            hash.Add(option);
        }
        return hash.ToHashCode();
    }

    #endregion

    /// <inheritdoc />
    public override string ToString() =>
        $"MultipleChoice: [{string.Join(", ", SelectedOptions.Select(o => $"\"{o}\""))}]";

    /// <summary>
    /// Internal DTO for JSON serialization.
    /// </summary>
    private sealed class MultipleChoiceData
    {
        [JsonPropertyName("selectedOptions")]
        public List<string> SelectedOptions { get; set; } = new();

        [JsonPropertyName("selectedOptionIndices")]
        public List<int>? SelectedOptionIndices { get; set; }
    }
}
