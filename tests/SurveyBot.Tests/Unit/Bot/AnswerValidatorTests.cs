using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Bot.Validators;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using Xunit;

namespace SurveyBot.Tests.Unit.Bot;

/// <summary>
/// Tests for AnswerValidator to ensure comprehensive validation of all question types.
/// </summary>
public class AnswerValidatorTests
{
    private readonly Mock<ILogger<AnswerValidator>> _loggerMock;
    private readonly AnswerValidator _validator;

    public AnswerValidatorTests()
    {
        _loggerMock = new Mock<ILogger<AnswerValidator>>();
        _validator = new AnswerValidator(_loggerMock.Object);
    }

    #region Text Question Validation Tests

    [Fact]
    public void ValidateAnswer_TextQuestion_ValidAnswer_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Text, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { text = "Valid answer" });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateAnswer_TextQuestion_RequiredButEmpty_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Text, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { text = "" });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnswer_TextQuestion_OptionalAndEmpty_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Text, isRequired: false);
        var answerJson = JsonSerializer.Serialize(new { text = "" });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_TextQuestion_TooLong_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Text, isRequired: true);
        var longText = new string('a', 5000); // Exceeds 4000 char limit
        var answerJson = JsonSerializer.Serialize(new { text = longText });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("too long", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnswer_TextQuestion_NullAnswer_RequiredFails()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Text, isRequired: true);

        // Act
        var result = _validator.ValidateAnswer(null, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Single Choice Validation Tests

    [Fact]
    public void ValidateAnswer_SingleChoice_ValidOption_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(
            QuestionType.SingleChoice,
            isRequired: true,
            options: new List<string> { "Option 1", "Option 2", "Option 3" });
        var answerJson = JsonSerializer.Serialize(new { selectedOption = "Option 2" });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_SingleChoice_InvalidOption_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(
            QuestionType.SingleChoice,
            isRequired: true,
            options: new List<string> { "Option 1", "Option 2" });
        var answerJson = JsonSerializer.Serialize(new { selectedOption = "Invalid Option" });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("not valid", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnswer_SingleChoice_RequiredButEmpty_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(
            QuestionType.SingleChoice,
            isRequired: true,
            options: new List<string> { "Yes", "No" });
        var answerJson = JsonSerializer.Serialize(new { selectedOption = "" });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Multiple Choice Validation Tests

    [Fact]
    public void ValidateAnswer_MultipleChoice_ValidSelections_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(
            QuestionType.MultipleChoice,
            isRequired: true,
            options: new List<string> { "A", "B", "C", "D" });
        var answerJson = JsonSerializer.Serialize(new { selectedOptions = new[] { "A", "C" } });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_MultipleChoice_RequiredButEmpty_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(
            QuestionType.MultipleChoice,
            isRequired: true,
            options: new List<string> { "A", "B", "C" });
        var answerJson = JsonSerializer.Serialize(new { selectedOptions = new string[] { } });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("at least one", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnswer_MultipleChoice_InvalidOptions_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(
            QuestionType.MultipleChoice,
            isRequired: true,
            options: new List<string> { "A", "B", "C" });
        var answerJson = JsonSerializer.Serialize(new { selectedOptions = new[] { "A", "X", "Y" } });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("not valid", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnswer_MultipleChoice_OptionalAndEmpty_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(
            QuestionType.MultipleChoice,
            isRequired: false,
            options: new List<string> { "A", "B", "C" });
        var answerJson = JsonSerializer.Serialize(new { selectedOptions = new string[] { } });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Rating Validation Tests

    [Fact]
    public void ValidateAnswer_Rating_ValidRating_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Rating, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { rating = 3 });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_Rating_BelowMinimum_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Rating, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { rating = 0 });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("between", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnswer_Rating_AboveMaximum_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Rating, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { rating = 11 });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("between", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnswer_Rating_CustomRange_Valid_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(
            QuestionType.Rating,
            isRequired: true,
            options: new List<string> { JsonSerializer.Serialize(new { min = 1, max = 10 }) });
        var answerJson = JsonSerializer.Serialize(new { rating = 7 });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_Rating_OptionalAndNull_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Rating, isRequired: false);
        var answerJson = JsonSerializer.Serialize(new { rating = (int?)null });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_Rating_RequiredButNull_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Rating, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { rating = (int?)null });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Number Question Validation Tests

    [Fact]
    public void ValidateAnswer_NumberQuestion_ValidDecimal_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Number, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { number = 42.5 });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateAnswer_NumberQuestion_ValidInteger_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Number, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { number = 100 });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateAnswer_NumberQuestion_NullForOptionalQuestion_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Number, isRequired: false);
        var answerJson = JsonSerializer.Serialize(new { number = (decimal?)null });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_NumberQuestion_NullForRequiredQuestion_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Number, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { number = (decimal?)null });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnswer_NumberQuestion_InvalidJsonFormat_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Number, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { invalid = "format" });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Invalid", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnswer_NumberQuestion_ValueBelowMinimum_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Number, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { number = 5, minValue = 10 });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("at least", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("10", result.ErrorMessage);
    }

    [Fact]
    public void ValidateAnswer_NumberQuestion_ValueAboveMaximum_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Number, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { number = 150, maxValue = 100 });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("at most", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("100", result.ErrorMessage);
    }

    [Fact]
    public void ValidateAnswer_NumberQuestion_ValueInRange_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Number, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { number = 50, minValue = 10, maxValue = 100 });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_NumberQuestion_NegativeNumber_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Number, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { number = -42.5 });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_NumberQuestion_Zero_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Number, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { number = 0 });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Date Question Validation Tests

    [Fact]
    public void ValidateAnswer_DateQuestion_ValidDate_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Date, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { date = new DateTime(2024, 6, 15) });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateAnswer_DateQuestion_NullForOptionalQuestion_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Date, isRequired: false);
        var answerJson = JsonSerializer.Serialize(new { date = (DateTime?)null });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_DateQuestion_NullForRequiredQuestion_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Date, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { date = (DateTime?)null });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnswer_DateQuestion_InvalidJsonFormat_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Date, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { invalid = "format" });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Invalid", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnswer_DateQuestion_DateBeforeMinimum_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Date, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new
        {
            date = new DateTime(2024, 1, 1),
            minDate = new DateTime(2024, 6, 1)
        });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("on or after", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("01.06.2024", result.ErrorMessage);
    }

    [Fact]
    public void ValidateAnswer_DateQuestion_DateAfterMaximum_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Date, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new
        {
            date = new DateTime(2024, 12, 31),
            maxDate = new DateTime(2024, 6, 30)
        });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("on or before", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("30.06.2024", result.ErrorMessage);
    }

    [Fact]
    public void ValidateAnswer_DateQuestion_DateInRange_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Date, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new
        {
            date = new DateTime(2024, 6, 15),
            minDate = new DateTime(2024, 1, 1),
            maxDate = new DateTime(2024, 12, 31)
        });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_DateQuestion_DateAtMinimumBoundary_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Date, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new
        {
            date = new DateTime(2024, 6, 1),
            minDate = new DateTime(2024, 6, 1)
        });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_DateQuestion_DateAtMaximumBoundary_ReturnsSuccess()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Date, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new
        {
            date = new DateTime(2024, 6, 30),
            maxDate = new DateTime(2024, 6, 30)
        });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_DateQuestion_OnlyMinDate_ValidatesCorrectly()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Date, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new
        {
            date = new DateTime(2024, 12, 31),
            minDate = new DateTime(2024, 1, 1)
        });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnswer_DateQuestion_OnlyMaxDate_ValidatesCorrectly()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Date, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new
        {
            date = new DateTime(2024, 1, 1),
            maxDate = new DateTime(2024, 12, 31)
        });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void ValidateAnswer_InvalidJsonFormat_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Text, isRequired: true);
        var invalidJson = "{invalid json}";

        // Act
        var result = _validator.ValidateAnswer(invalidJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Invalid answer format", result.ErrorMessage);
    }

    [Fact]
    public void ValidateAnswer_MissingRequiredProperty_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Text, isRequired: true);
        var answerJson = JsonSerializer.Serialize(new { wrongProperty = "value" });

        // Act
        var result = _validator.ValidateAnswer(answerJson, question);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void ValidateAnswer_EmptyString_RequiredQuestion_ReturnsFailure()
    {
        // Arrange
        var question = CreateQuestion(QuestionType.Text, isRequired: true);

        // Act
        var result = _validator.ValidateAnswer("", question);

        // Assert
        Assert.False(result.IsValid);
    }

    #endregion

    #region Helper Methods

    private QuestionDto CreateQuestion(
        QuestionType type,
        bool isRequired,
        List<string>? options = null)
    {
        return new QuestionDto
        {
            Id = 1,
            SurveyId = 1,
            QuestionText = "Test Question",
            QuestionType = type,
            OrderIndex = 0,
            IsRequired = isRequired,
            Options = options,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
