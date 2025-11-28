using System.Text.Json;
using System.Text.Json.Serialization;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Value object representing a single-choice answer.
/// Immutable with value semantics.
/// </summary>
public sealed class SingleChoiceAnswerValue : AnswerValue
{
    /// <summary>
    /// Gets the selected option text.
    /// </summary>
    [JsonPropertyName("selectedOption")]
    public string SelectedOption { get; private set; }

    /// <summary>
    /// Gets the zero-based index of the selected option.
    /// May be -1 if index was unknown during parsing from database.
    /// </summary>
    [JsonPropertyName("selectedOptionIndex")]
    public int SelectedOptionIndex { get; private set; }

    /// <inheritdoc />
    [JsonIgnore]
    public override QuestionType QuestionType => QuestionType.SingleChoice;

    /// <inheritdoc />
    [JsonIgnore]
    public override string DisplayValue => SelectedOption;

    /// <summary>
    /// Private constructor - use Create() factory method.
    /// </summary>
    private SingleChoiceAnswerValue(string selectedOption, int selectedOptionIndex)
    {
        SelectedOption = selectedOption;
        SelectedOptionIndex = selectedOptionIndex;
    }

    /// <summary>
    /// JSON constructor for deserialization.
    /// </summary>
    [JsonConstructor]
    private SingleChoiceAnswerValue() : this(string.Empty, -1)
    {
    }

    /// <summary>
    /// Creates a new single-choice answer with validation.
    /// </summary>
    /// <param name="selectedOption">The selected option text</param>
    /// <param name="validOptions">All valid options from the question</param>
    /// <returns>Validated single-choice answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If option is invalid</exception>
    public static SingleChoiceAnswerValue Create(
        string selectedOption,
        IReadOnlyCollection<QuestionOption> validOptions)
    {
        if (string.IsNullOrWhiteSpace(selectedOption))
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.SingleChoice,
                "Selected option cannot be empty");

        if (validOptions == null || validOptions.Count == 0)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.SingleChoice,
                "Question has no valid options");

        var optionsList = validOptions.OrderBy(o => o.OrderIndex).ToList();
        var index = optionsList.FindIndex(o =>
            string.Equals(o.Text, selectedOption, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.SingleChoice,
                $"Option '{selectedOption}' not found. Valid options: {string.Join(", ", optionsList.Select(o => $"'{o.Text}'"))}");

        return new SingleChoiceAnswerValue(optionsList[index].Text, index);
    }

    /// <summary>
    /// Creates a single-choice answer from simple string options (legacy format).
    /// </summary>
    /// <param name="selectedOption">The selected option text</param>
    /// <param name="validOptions">All valid option texts</param>
    /// <returns>Validated single-choice answer</returns>
    public static SingleChoiceAnswerValue CreateFromStrings(
        string selectedOption,
        IEnumerable<string> validOptions)
    {
        if (string.IsNullOrWhiteSpace(selectedOption))
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.SingleChoice,
                "Selected option cannot be empty");

        var optionsList = validOptions?.Where(o => !string.IsNullOrWhiteSpace(o)).ToList()
            ?? throw new InvalidAnswerFormatException(0, QuestionType.SingleChoice, "Question has no valid options");

        if (optionsList.Count == 0)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.SingleChoice,
                "Question has no valid options");

        var index = optionsList.FindIndex(o =>
            string.Equals(o, selectedOption, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.SingleChoice,
                $"Option '{selectedOption}' not found. Valid options: {string.Join(", ", optionsList.Select(o => $"'{o}'"))}");

        return new SingleChoiceAnswerValue(optionsList[index], index);
    }

    /// <summary>
    /// Creates from option index (when you already know the index).
    /// </summary>
    /// <param name="optionIndex">Zero-based option index</param>
    /// <param name="validOptions">All valid options from the question</param>
    /// <returns>Validated single-choice answer</returns>
    public static SingleChoiceAnswerValue CreateByIndex(
        int optionIndex,
        IReadOnlyCollection<QuestionOption> validOptions)
    {
        if (validOptions == null || validOptions.Count == 0)
            throw new InvalidAnswerFormatException(0, QuestionType.SingleChoice, "No valid options");

        var optionsList = validOptions.OrderBy(o => o.OrderIndex).ToList();

        if (optionIndex < 0 || optionIndex >= optionsList.Count)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.SingleChoice,
                $"Option index {optionIndex} out of range (0-{optionsList.Count - 1})");

        return new SingleChoiceAnswerValue(optionsList[optionIndex].Text, optionIndex);
    }

    /// <summary>
    /// Creates without validation (for parsing from trusted database storage).
    /// </summary>
    /// <param name="selectedOption">The selected option text</param>
    /// <param name="selectedOptionIndex">The option index (-1 if unknown)</param>
    /// <returns>Single-choice answer</returns>
    internal static SingleChoiceAnswerValue CreateTrusted(string selectedOption, int selectedOptionIndex = -1)
    {
        if (string.IsNullOrWhiteSpace(selectedOption))
            throw new InvalidAnswerFormatException(0, QuestionType.SingleChoice, "Selected option cannot be empty");

        return new SingleChoiceAnswerValue(selectedOption, selectedOptionIndex);
    }

    /// <summary>
    /// Parses single-choice answer from JSON storage format.
    /// </summary>
    /// <param name="json">JSON string from database</param>
    /// <returns>Parsed single-choice answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If JSON is invalid</exception>
    public static SingleChoiceAnswerValue FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAnswerFormatException(0, QuestionType.SingleChoice, "JSON cannot be empty");

        try
        {
            var data = JsonSerializer.Deserialize<SingleChoiceData>(json);

            if (data == null || string.IsNullOrWhiteSpace(data.SelectedOption))
                throw new InvalidAnswerFormatException(0, QuestionType.SingleChoice, "selectedOption missing in JSON");

            // When parsing from DB, we trust the stored data (it was validated on creation)
            return new SingleChoiceAnswerValue(data.SelectedOption, data.SelectedOptionIndex ?? -1);
        }
        catch (JsonException ex)
        {
            throw new InvalidAnswerFormatException(
                $"Invalid JSON for single-choice answer: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public override string ToJson() =>
        JsonSerializer.Serialize(new SingleChoiceData
        {
            SelectedOption = SelectedOption,
            SelectedOptionIndex = SelectedOptionIndex >= 0 ? SelectedOptionIndex : null
        });

    /// <inheritdoc />
    public override bool IsValidFor(Question question)
    {
        if (question.QuestionType != QuestionType.SingleChoice)
            return false;

        // Check option exists in question's options
        return question.Options.Any(o =>
            string.Equals(o.Text, SelectedOption, StringComparison.OrdinalIgnoreCase));
    }

    #region Equality

    /// <inheritdoc />
    public override bool Equals(AnswerValue? other) =>
        other is SingleChoiceAnswerValue single &&
        string.Equals(SelectedOption, single.SelectedOption, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(QuestionType, SelectedOption);

    #endregion

    /// <inheritdoc />
    public override string ToString() => $"SingleChoice: \"{SelectedOption}\" (index {SelectedOptionIndex})";

    /// <summary>
    /// Internal DTO for JSON serialization.
    /// </summary>
    private sealed class SingleChoiceData
    {
        [JsonPropertyName("selectedOption")]
        public string SelectedOption { get; set; } = string.Empty;

        [JsonPropertyName("selectedOptionIndex")]
        public int? SelectedOptionIndex { get; set; }
    }
}
