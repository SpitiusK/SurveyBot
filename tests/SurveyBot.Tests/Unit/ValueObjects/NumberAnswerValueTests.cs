using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.ValueObjects.Answers;
using System.Text.Json;
using Xunit;

namespace SurveyBot.Tests.Unit.ValueObjects;

/// <summary>
/// Unit tests for NumberAnswerValue value object.
/// Tests factory methods, validation, parsing, JSON serialization, and equality.
/// </summary>
public class NumberAnswerValueTests
{
    #region Factory Method Tests

    [Fact]
    public void Create_ValidValue_CreatesNumberAnswer()
    {
        // Arrange
        const decimal value = 42.5m;

        // Act
        var result = NumberAnswerValue.Create(value);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value, result.Value);
        Assert.Equal(QuestionType.Number, result.QuestionType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100.5)]
    [InlineData(999999.99)]
    [InlineData(0.00001)]
    public void Create_VariousValidValues_CreatesCorrectly(decimal value)
    {
        // Act
        var result = NumberAnswerValue.Create(value);

        // Assert
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Create_WithinRange_CreatesNumberAnswer()
    {
        // Arrange
        const decimal value = 50m;
        const decimal minValue = 0m;
        const decimal maxValue = 100m;

        // Act
        var result = NumberAnswerValue.Create(value, minValue, maxValue);

        // Assert
        Assert.Equal(value, result.Value);
        Assert.Equal(minValue, result.MinValue);
        Assert.Equal(maxValue, result.MaxValue);
    }

    [Fact]
    public void Create_BelowMinimum_ThrowsInvalidAnswerFormatException()
    {
        // Arrange
        const decimal value = -10m;
        const decimal minValue = 0m;

        // Act & Assert
        var exception = Assert.Throws<InvalidAnswerFormatException>(() =>
            NumberAnswerValue.Create(value, minValue));

        Assert.Contains("less than minimum", exception.Message);
    }

    [Fact]
    public void Create_AboveMaximum_ThrowsInvalidAnswerFormatException()
    {
        // Arrange
        const decimal value = 150m;
        const decimal maxValue = 100m;

        // Act & Assert
        var exception = Assert.Throws<InvalidAnswerFormatException>(() =>
            NumberAnswerValue.Create(value, maxValue: maxValue));

        Assert.Contains("greater than maximum", exception.Message);
    }

    [Theory]
    [InlineData(3.14159, 2)] // 5 decimal places but max is 2
    [InlineData(1.1234, 3)]  // 4 decimal places but max is 3
    public void Create_TooManyDecimalPlaces_ThrowsInvalidAnswerFormatException(decimal value, int maxDecimalPlaces)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidAnswerFormatException>(() =>
            NumberAnswerValue.Create(value, decimalPlaces: maxDecimalPlaces));

        Assert.Contains("decimal places", exception.Message);
    }

    [Theory]
    [InlineData(3.14, 2)]    // 2 decimal places, max is 2
    [InlineData(1.123, 3)]   // 3 decimal places, max is 3
    [InlineData(42, 0)]      // 0 decimal places, max is 0
    [InlineData(100.5, -1)]  // Any decimal places (unlimited)
    public void Create_ValidDecimalPlaces_CreatesCorrectly(decimal value, int maxDecimalPlaces)
    {
        // Act
        var result = NumberAnswerValue.Create(value, decimalPlaces: maxDecimalPlaces);

        // Assert
        Assert.Equal(value, result.Value);
    }

    #endregion

    #region Parse Tests

    [Theory]
    [InlineData("42", 42)]
    [InlineData("3.14", 3.14)]
    [InlineData("-100", -100)]
    [InlineData("  50.5  ", 50.5)] // Whitespace trimmed
    [InlineData("3,14", 3.14)]     // Comma as decimal separator
    public void Parse_ValidInput_CreatesNumberAnswer(string input, decimal expected)
    {
        // Act
        var result = NumberAnswerValue.Parse(input);

        // Assert
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_EmptyOrNull_ThrowsInvalidAnswerFormatException(string? input)
    {
        // Act & Assert
        Assert.Throws<InvalidAnswerFormatException>(() =>
            NumberAnswerValue.Parse(input!));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.34.56")]
    [InlineData("1e5")]  // Scientific notation not supported
    [InlineData("$100")]
    public void Parse_InvalidFormat_ThrowsInvalidAnswerFormatException(string input)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidAnswerFormatException>(() =>
            NumberAnswerValue.Parse(input));

        Assert.Contains("not a valid number", exception.Message);
    }

    [Fact]
    public void TryParse_ValidInput_ReturnsTrueAndResult()
    {
        // Arrange
        const string input = "42.5";

        // Act
        var success = NumberAnswerValue.TryParse(input, out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(42.5m, result.Value);
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalseAndNull()
    {
        // Arrange
        const string input = "not a number";

        // Act
        var success = NumberAnswerValue.TryParse(input, out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public void ToJson_CreatesValidJson()
    {
        // Arrange
        var answer = NumberAnswerValue.Create(42.5m, 0, 100, 2);

        // Act
        var json = answer.ToJson();

        // Assert
        var doc = JsonDocument.Parse(json);
        Assert.Equal(42.5m, doc.RootElement.GetProperty("number").GetDecimal());
        Assert.Equal(0m, doc.RootElement.GetProperty("minValue").GetDecimal());
        Assert.Equal(100m, doc.RootElement.GetProperty("maxValue").GetDecimal());
        Assert.Equal(2, doc.RootElement.GetProperty("decimalPlaces").GetInt32());
    }

    [Fact]
    public void FromJson_ValidJson_CreatesNumberAnswer()
    {
        // Arrange
        var json = """{"number": 42.5, "minValue": 0, "maxValue": 100, "decimalPlaces": 2}""";

        // Act
        var result = NumberAnswerValue.FromJson(json);

        // Assert
        Assert.Equal(42.5m, result.Value);
        Assert.Equal(0m, result.MinValue);
        Assert.Equal(100m, result.MaxValue);
        Assert.Equal(2, result.DecimalPlaces);
    }

    [Fact]
    public void FromJson_EmptyJson_ThrowsInvalidAnswerFormatException()
    {
        // Arrange
        var json = "";

        // Act & Assert
        Assert.Throws<InvalidAnswerFormatException>(() =>
            NumberAnswerValue.FromJson(json));
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsInvalidAnswerFormatException()
    {
        // Arrange
        var json = "not valid json";

        // Act & Assert
        Assert.Throws<InvalidAnswerFormatException>(() =>
            NumberAnswerValue.FromJson(json));
    }

    [Fact]
    public void RoundTripSerialization_PreservesValue()
    {
        // Arrange
        var original = NumberAnswerValue.Create(123.456m, 0, 1000, 3);

        // Act
        var json = original.ToJson();
        var restored = NumberAnswerValue.FromJson(json);

        // Assert
        Assert.Equal(original.Value, restored.Value);
        Assert.Equal(original.MinValue, restored.MinValue);
        Assert.Equal(original.MaxValue, restored.MaxValue);
        Assert.Equal(original.DecimalPlaces, restored.DecimalPlaces);
    }

    #endregion

    #region DisplayValue Tests

    [Theory]
    [InlineData(42, null, "42")]
    [InlineData(3.14, null, "3.14")]
    [InlineData(100.00, 2, "100.00")]
    [InlineData(3.14, 2, "3.14")]
    public void DisplayValue_FormatsCorrectly(decimal value, int? decimalPlaces, string expected)
    {
        // Arrange
        var answer = NumberAnswerValue.Create(value, decimalPlaces: decimalPlaces);

        // Act
        var display = answer.DisplayValue;

        // Assert
        Assert.Equal(expected, display);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var answer1 = NumberAnswerValue.Create(42.5m);
        var answer2 = NumberAnswerValue.Create(42.5m);

        // Act & Assert
        Assert.True(answer1.Equals(answer2));
        Assert.True(answer2.Equals(answer1));
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var answer1 = NumberAnswerValue.Create(42.5m);
        var answer2 = NumberAnswerValue.Create(100m);

        // Act & Assert
        Assert.False(answer1.Equals(answer2));
    }

    [Fact]
    public void Equals_SameValueDifferentConstraints_ReturnsTrue()
    {
        // Value equality is based on Value only, not constraints
        var answer1 = NumberAnswerValue.Create(42.5m, 0, 100);
        var answer2 = NumberAnswerValue.Create(42.5m, -100, 200);

        // Act & Assert
        Assert.True(answer1.Equals(answer2));
    }

    [Fact]
    public void GetHashCode_SameValue_ReturnsSameHash()
    {
        // Arrange
        var answer1 = NumberAnswerValue.Create(42.5m);
        var answer2 = NumberAnswerValue.Create(42.5m);

        // Act & Assert
        Assert.Equal(answer1.GetHashCode(), answer2.GetHashCode());
    }

    #endregion

    #region IsValidFor Tests

    [Fact]
    public void IsValidFor_CorrectQuestionType_ReturnsTrue()
    {
        // Arrange
        var answer = NumberAnswerValue.Create(50m);
        var question = Question.Create(1, "Enter a number", QuestionType.Number, 1);

        // Act
        var result = answer.IsValidFor(question);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidFor_WrongQuestionType_ReturnsFalse()
    {
        // Arrange
        var answer = NumberAnswerValue.Create(50m);
        var question = Question.Create(1, "Enter text", QuestionType.Text, 1);

        // Act
        var result = answer.IsValidFor(question);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsReadableFormat()
    {
        // Arrange
        var answer = NumberAnswerValue.Create(42.5m);

        // Act
        var result = answer.ToString();

        // Assert
        Assert.Contains("Number", result);
        Assert.Contains("42.5", result);
    }

    #endregion
}
