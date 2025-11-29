using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using SurveyBot.Core.ValueObjects.Answers;

namespace SurveyBot.Bot.Validators;

/// <summary>
/// Validates survey answers based on question type and requirements.
/// Provides comprehensive validation for all supported question types.
/// </summary>
public class AnswerValidator : IAnswerValidator
{
    private const int MAX_TEXT_LENGTH = 4000; // Telegram message limit
    private const int MIN_RATING = 1;
    private const int MAX_RATING = 10;

    private readonly ILogger<AnswerValidator> _logger;

    public AnswerValidator(ILogger<AnswerValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates an answer based on question type and requirements.
    /// </summary>
    public ValidationResult ValidateAnswer(string? answerJson, QuestionDto question)
    {
        // Empty answer validation
        if (string.IsNullOrWhiteSpace(answerJson))
        {
            if (question.IsRequired)
            {
                return ValidationResult.Failure("This question is required. Please provide an answer.");
            }
            return ValidationResult.Success();
        }

        // Parse JSON and validate based on question type
        try
        {
            var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

            return question.QuestionType switch
            {
                QuestionType.Text => ValidateTextAnswer(answer, question),
                QuestionType.SingleChoice => ValidateSingleChoiceAnswer(answer, question),
                QuestionType.MultipleChoice => ValidateMultipleChoiceAnswer(answer, question),
                QuestionType.Rating => ValidateRatingAnswer(answer, question),
                QuestionType.Location => ValidateLocationAnswer(answer, question),
                QuestionType.Number => ValidateNumberAnswer(answer, question),
                QuestionType.Date => ValidateDateAnswer(answer, question),
                _ => ValidationResult.Failure($"Unsupported question type: {question.QuestionType}")
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON format for answer validation");
            return ValidationResult.Failure("Invalid answer format. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during answer validation");
            return ValidationResult.Failure("An error occurred while validating your answer. Please try again.");
        }
    }

    #region Question Type Validators

    /// <summary>
    /// Validates text answer.
    /// </summary>
    private ValidationResult ValidateTextAnswer(JsonElement answer, QuestionDto question)
    {
        if (!answer.TryGetProperty("text", out var textElement))
        {
            return ValidationResult.Failure("Answer format is invalid. Please provide a text answer.");
        }

        var text = textElement.GetString();

        // Required question validation
        if (question.IsRequired && string.IsNullOrWhiteSpace(text))
        {
            return ValidationResult.Failure("This question is required. Please provide an answer.");
        }

        // Length validation
        if (!string.IsNullOrWhiteSpace(text) && text.Length > MAX_TEXT_LENGTH)
        {
            return ValidationResult.Failure(
                $"Your answer is too long. Maximum {MAX_TEXT_LENGTH} characters allowed (you entered {text.Length}).");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates single choice answer.
    /// </summary>
    private ValidationResult ValidateSingleChoiceAnswer(JsonElement answer, QuestionDto question)
    {
        if (!answer.TryGetProperty("selectedOption", out var selectedOptionElement))
        {
            return ValidationResult.Failure("Answer format is invalid. Please select an option.");
        }

        var selectedOption = selectedOptionElement.GetString();

        // Required question validation
        if (question.IsRequired && string.IsNullOrWhiteSpace(selectedOption))
        {
            return ValidationResult.Failure("This question is required. Please select an option.");
        }

        // Validate option exists in question options
        if (question.Options == null || question.Options.Count == 0)
        {
            _logger.LogError("Question {QuestionId} has no options configured", question.Id);
            return ValidationResult.Failure("This question is not configured correctly. Please contact support.");
        }

        if (!string.IsNullOrWhiteSpace(selectedOption) && !question.Options.Contains(selectedOption))
        {
            return ValidationResult.Failure(
                "The selected option is not valid. Please choose from the available options.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates multiple choice answer.
    /// </summary>
    private ValidationResult ValidateMultipleChoiceAnswer(JsonElement answer, QuestionDto question)
    {
        if (!answer.TryGetProperty("selectedOptions", out var selectedOptionsElement))
        {
            return ValidationResult.Failure("Answer format is invalid. Please select at least one option.");
        }

        var selectedOptions = selectedOptionsElement.EnumerateArray()
            .Select(e => e.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        // Required question validation - must have at least one selection
        if (question.IsRequired && selectedOptions.Count == 0)
        {
            return ValidationResult.Failure("This question is required. Please select at least one option.");
        }

        // Validate question has options configured
        if (question.Options == null || question.Options.Count == 0)
        {
            _logger.LogError("Question {QuestionId} has no options configured", question.Id);
            return ValidationResult.Failure("This question is not configured correctly. Please contact support.");
        }

        // Validate all selected options exist in question options
        var invalidOptions = selectedOptions.Where(opt => !question.Options.Contains(opt)).ToList();
        if (invalidOptions.Any())
        {
            return ValidationResult.Failure(
                $"Some selected options are not valid: {string.Join(", ", invalidOptions)}. Please select from available options.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates rating answer.
    /// </summary>
    private ValidationResult ValidateRatingAnswer(JsonElement answer, QuestionDto question)
    {
        if (!answer.TryGetProperty("rating", out var ratingElement))
        {
            return ValidationResult.Failure("Answer format is invalid. Please provide a rating.");
        }

        // Check for null rating (optional question skipped)
        if (ratingElement.ValueKind == JsonValueKind.Null)
        {
            if (question.IsRequired)
            {
                return ValidationResult.Failure("This question is required. Please provide a rating.");
            }
            return ValidationResult.Success();
        }

        // Parse rating value
        if (!ratingElement.TryGetInt32(out var rating))
        {
            return ValidationResult.Failure("Rating must be a number. Please select a valid rating.");
        }

        // Get rating range from question
        var (minRating, maxRating) = ParseRatingRange(question);

        // Validate rating is in range
        if (rating < minRating || rating > maxRating)
        {
            return ValidationResult.Failure(
                $"Rating must be between {minRating} and {maxRating}. You provided {rating}.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates location answer.
    /// </summary>
    private ValidationResult ValidateLocationAnswer(JsonElement answer, QuestionDto question)
    {
        if (!answer.TryGetProperty("latitude", out var latElement) ||
            !answer.TryGetProperty("longitude", out var lonElement))
        {
            return ValidationResult.Failure("Answer format is invalid. Please provide a location.");
        }

        // Check for null coordinates (optional question skipped)
        if (latElement.ValueKind == JsonValueKind.Null || lonElement.ValueKind == JsonValueKind.Null)
        {
            if (question.IsRequired)
            {
                return ValidationResult.Failure("This question is required. Please share your location.");
            }
            return ValidationResult.Success();
        }

        // Parse coordinate values
        if (!latElement.TryGetDouble(out var latitude) || !lonElement.TryGetDouble(out var longitude))
        {
            return ValidationResult.Failure("Location coordinates must be numbers. Please try again.");
        }

        // Validate coordinate ranges
        if (latitude < -90 || latitude > 90)
        {
            return ValidationResult.Failure($"Latitude must be between -90 and 90. You provided {latitude}.");
        }

        if (longitude < -180 || longitude > 180)
        {
            return ValidationResult.Failure($"Longitude must be between -180 and 180. You provided {longitude}.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates number answer.
    /// </summary>
    private ValidationResult ValidateNumberAnswer(JsonElement answer, QuestionDto question)
    {
        if (!answer.TryGetProperty("number", out var numberElement))
        {
            return ValidationResult.Failure("Answer format is invalid. Please provide a number.");
        }

        // Check for null number (optional question skipped)
        if (numberElement.ValueKind == JsonValueKind.Null)
        {
            if (question.IsRequired)
            {
                return ValidationResult.Failure("This question is required. Please provide a number.");
            }
            return ValidationResult.Success();
        }

        // Parse number value (should be a decimal directly in JSON)
        if (!numberElement.TryGetDecimal(out var numberValue))
        {
            return ValidationResult.Failure("Invalid number format. Please enter a valid number.");
        }

        // Extract min/max/decimalPlaces from the answer itself (NumberAnswerValue.ToJson includes these)
        decimal? answerMinValue = null;
        decimal? answerMaxValue = null;

        if (answer.TryGetProperty("minValue", out var minElement) && minElement.ValueKind != JsonValueKind.Null)
        {
            answerMinValue = minElement.GetDecimal();
        }

        if (answer.TryGetProperty("maxValue", out var maxElement) && maxElement.ValueKind != JsonValueKind.Null)
        {
            answerMaxValue = maxElement.GetDecimal();
        }

        // Validate range if constraints are present
        if (answerMinValue.HasValue && numberValue < answerMinValue.Value)
        {
            return ValidationResult.Failure($"Number must be at least {answerMinValue.Value}.");
        }

        if (answerMaxValue.HasValue && numberValue > answerMaxValue.Value)
        {
            return ValidationResult.Failure($"Number must be at most {answerMaxValue.Value}.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates date answer.
    /// </summary>
    private ValidationResult ValidateDateAnswer(JsonElement answer, QuestionDto question)
    {
        if (!answer.TryGetProperty("date", out var dateElement))
        {
            return ValidationResult.Failure("Answer format is invalid. Please provide a date.");
        }

        // Check for null date (optional question skipped)
        if (dateElement.ValueKind == JsonValueKind.Null)
        {
            if (question.IsRequired)
            {
                return ValidationResult.Failure("This question is required. Please provide a date.");
            }
            return ValidationResult.Success();
        }

        // Parse date value from DD.MM.YYYY string format (DateAnswerValue.ToJson() uses this format)
        var dateString = dateElement.GetString();
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return ValidationResult.Failure("Date value is missing. Please provide a date in DD.MM.YYYY format.");
        }

        if (!DateTime.TryParseExact(
            dateString,
            DateAnswerValue.DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dateValue))
        {
            return ValidationResult.Failure("Invalid date format. Use DD.MM.YYYY (e.g., 15.06.2024).");
        }

        // Extract min/max dates from the answer itself (DateAnswerValue.ToJson includes these)
        DateTime? answerMinDate = null;
        DateTime? answerMaxDate = null;

        if (answer.TryGetProperty("minDate", out var minElement) && minElement.ValueKind != JsonValueKind.Null)
        {
            var minDateString = minElement.GetString();
            if (!string.IsNullOrWhiteSpace(minDateString) &&
                DateTime.TryParseExact(
                    minDateString,
                    DateAnswerValue.DateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var minDate))
            {
                answerMinDate = minDate;
            }
        }

        if (answer.TryGetProperty("maxDate", out var maxElement) && maxElement.ValueKind != JsonValueKind.Null)
        {
            var maxDateString = maxElement.GetString();
            if (!string.IsNullOrWhiteSpace(maxDateString) &&
                DateTime.TryParseExact(
                    maxDateString,
                    DateAnswerValue.DateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var maxDate))
            {
                answerMaxDate = maxDate;
            }
        }

        // Validate range if constraints are present
        if (answerMinDate.HasValue && dateValue < answerMinDate.Value)
        {
            return ValidationResult.Failure($"Date must be on or after {answerMinDate.Value:dd.MM.yyyy}.");
        }

        if (answerMaxDate.HasValue && dateValue > answerMaxDate.Value)
        {
            return ValidationResult.Failure($"Date must be on or before {answerMaxDate.Value:dd.MM.yyyy}.");
        }

        return ValidationResult.Success();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Parses rating range from question options.
    /// </summary>
    private (int minRating, int maxRating) ParseRatingRange(QuestionDto question)
    {
        if (question.Options != null && question.Options.Count > 0)
        {
            try
            {
                var optionsJson = question.Options.First();
                var options = JsonSerializer.Deserialize<Dictionary<string, int>>(optionsJson);

                if (options != null &&
                    options.TryGetValue("min", out var min) &&
                    options.TryGetValue("max", out var max))
                {
                    return (min, max);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse rating range from options, using defaults");
            }
        }

        // Default to 1-5 scale
        return (MIN_RATING, 5);
    }

    #endregion
}
