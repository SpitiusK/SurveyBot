using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.ValueObjects.Answers;
using System.Text.Json;
using Xunit;

namespace SurveyBot.Tests.Unit.ValueObjects;

/// <summary>
/// Unit tests for DateAnswerValue value object.
/// Tests factory methods, validation, parsing, JSON serialization, and equality.
/// </summary>
public class DateAnswerValueTests
{
    #region Factory Method Tests

    [Fact]
    public void Create_ValidDate_CreatesDateAnswer()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);

        // Act
        var result = DateAnswerValue.Create(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(date.Date, result.Date);
        Assert.Equal(QuestionType.Date, result.QuestionType);
    }

    [Fact]
    public void Create_DateWithTime_StripsTimeComponent()
    {
        // Arrange
        var dateWithTime = new DateTime(2024, 6, 15, 14, 30, 45);

        // Act
        var result = DateAnswerValue.Create(dateWithTime);

        // Assert
        Assert.Equal(new DateTime(2024, 6, 15), result.Date);
        Assert.Equal(TimeSpan.Zero, result.Date.TimeOfDay);
    }

    [Fact]
    public void Create_WithinRange_CreatesDateAnswer()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);
        var minDate = new DateTime(2024, 1, 1);
        var maxDate = new DateTime(2024, 12, 31);

        // Act
        var result = DateAnswerValue.Create(date, minDate, maxDate);

        // Assert
        Assert.Equal(date.Date, result.Date);
        Assert.Equal(minDate.Date, result.MinDate);
        Assert.Equal(maxDate.Date, result.MaxDate);
    }

    [Fact]
    public void Create_BeforeMinDate_ThrowsInvalidAnswerFormatException()
    {
        // Arrange
        var date = new DateTime(2023, 6, 15);
        var minDate = new DateTime(2024, 1, 1);

        // Act & Assert
        var exception = Assert.Throws<InvalidAnswerFormatException>(() =>
            DateAnswerValue.Create(date, minDate));

        Assert.Contains("before minimum", exception.Message);
    }

    [Fact]
    public void Create_AfterMaxDate_ThrowsInvalidAnswerFormatException()
    {
        // Arrange
        var date = new DateTime(2025, 6, 15);
        var maxDate = new DateTime(2024, 12, 31);

        // Act & Assert
        var exception = Assert.Throws<InvalidAnswerFormatException>(() =>
            DateAnswerValue.Create(date, maxDate: maxDate));

        Assert.Contains("after maximum", exception.Message);
    }

    [Theory]
    [InlineData(2020, 1, 1)]
    [InlineData(2024, 2, 29)]  // Leap year
    [InlineData(2099, 12, 31)]
    public void Create_VariousValidDates_CreatesCorrectly(int year, int month, int day)
    {
        // Arrange
        var date = new DateTime(year, month, day);

        // Act
        var result = DateAnswerValue.Create(date);

        // Assert
        Assert.Equal(date.Date, result.Date);
    }

    #endregion

    #region Parse Tests

    [Theory]
    [InlineData("15.06.2024", 2024, 6, 15)]
    [InlineData("01.01.2020", 2020, 1, 1)]
    [InlineData("31.12.2025", 2025, 12, 31)]
    [InlineData("  15.06.2024  ", 2024, 6, 15)] // Whitespace trimmed
    public void Parse_ValidDDMMYYYY_CreatesDateAnswer(string input, int year, int month, int day)
    {
        // Act
        var result = DateAnswerValue.Parse(input);

        // Assert
        Assert.Equal(new DateTime(year, month, day), result.Date);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_EmptyOrNull_ThrowsInvalidAnswerFormatException(string? input)
    {
        // Act & Assert
        Assert.Throws<InvalidAnswerFormatException>(() =>
            DateAnswerValue.Parse(input!));
    }

    [Theory]
    [InlineData("2024-06-15")]    // ISO format not supported
    [InlineData("06/15/2024")]    // US format not supported
    [InlineData("15/06/2024")]    // Slash separator not supported
    [InlineData("June 15, 2024")] // Text month not supported
    [InlineData("not a date")]
    [InlineData("32.13.2024")]    // Invalid day/month
    public void Parse_InvalidFormat_ThrowsInvalidAnswerFormatException(string input)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidAnswerFormatException>(() =>
            DateAnswerValue.Parse(input));

        Assert.Contains("not a valid date", exception.Message);
        Assert.Contains("DD.MM.YYYY", exception.Message);
    }

    [Fact]
    public void TryParse_ValidInput_ReturnsTrueAndResult()
    {
        // Arrange
        const string input = "15.06.2024";

        // Act
        var success = DateAnswerValue.TryParse(input, out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 6, 15), result.Date);
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalseAndNull()
    {
        // Arrange
        const string input = "not a date";

        // Act
        var success = DateAnswerValue.TryParse(input, out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithValidRange_CreatesDateAnswer()
    {
        // Arrange
        const string input = "15.06.2024";
        var minDate = new DateTime(2024, 1, 1);
        var maxDate = new DateTime(2024, 12, 31);

        // Act
        var result = DateAnswerValue.Parse(input, minDate, maxDate);

        // Assert
        Assert.Equal(new DateTime(2024, 6, 15), result.Date);
    }

    [Fact]
    public void Parse_OutOfRange_ThrowsInvalidAnswerFormatException()
    {
        // Arrange
        const string input = "15.06.2023"; // Year before allowed range
        var minDate = new DateTime(2024, 1, 1);

        // Act & Assert
        Assert.Throws<InvalidAnswerFormatException>(() =>
            DateAnswerValue.Parse(input, minDate));
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public void ToJson_CreatesValidJson()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);
        var minDate = new DateTime(2024, 1, 1);
        var maxDate = new DateTime(2024, 12, 31);
        var answer = DateAnswerValue.Create(date, minDate, maxDate);

        // Act
        var json = answer.ToJson();

        // Assert
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("date", out var dateElement));
        Assert.True(doc.RootElement.TryGetProperty("minDate", out var minDateElement));
        Assert.True(doc.RootElement.TryGetProperty("maxDate", out var maxDateElement));
    }

    [Fact]
    public void FromJson_ValidJson_CreatesDateAnswer()
    {
        // Arrange
        var json = """{"date": "15.06.2024", "minDate": "01.01.2024", "maxDate": "31.12.2024"}""";

        // Act
        var result = DateAnswerValue.FromJson(json);

        // Assert
        Assert.Equal(new DateTime(2024, 6, 15), result.Date);
        Assert.Equal(new DateTime(2024, 1, 1), result.MinDate);
        Assert.Equal(new DateTime(2024, 12, 31), result.MaxDate);
    }

    [Fact]
    public void FromJson_EmptyJson_ThrowsInvalidAnswerFormatException()
    {
        // Arrange
        var json = "";

        // Act & Assert
        Assert.Throws<InvalidAnswerFormatException>(() =>
            DateAnswerValue.FromJson(json));
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsInvalidAnswerFormatException()
    {
        // Arrange
        var json = "not valid json";

        // Act & Assert
        Assert.Throws<InvalidAnswerFormatException>(() =>
            DateAnswerValue.FromJson(json));
    }

    [Fact]
    public void RoundTripSerialization_PreservesValue()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);
        var minDate = new DateTime(2024, 1, 1);
        var maxDate = new DateTime(2024, 12, 31);
        var original = DateAnswerValue.Create(date, minDate, maxDate);

        // Act
        var json = original.ToJson();
        var restored = DateAnswerValue.FromJson(json);

        // Assert
        Assert.Equal(original.Date, restored.Date);
        Assert.Equal(original.MinDate, restored.MinDate);
        Assert.Equal(original.MaxDate, restored.MaxDate);
    }

    #endregion

    #region DisplayValue Tests

    [Fact]
    public void DisplayValue_FormatsAsDDMMYYYY()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);
        var answer = DateAnswerValue.Create(date);

        // Act
        var display = answer.DisplayValue;

        // Assert
        Assert.Equal("15.06.2024", display);
    }

    [Theory]
    [InlineData(2024, 1, 1, "01.01.2024")]
    [InlineData(2024, 12, 31, "31.12.2024")]
    [InlineData(2020, 2, 29, "29.02.2020")] // Leap year
    public void DisplayValue_FormatsCorrectly(int year, int month, int day, string expected)
    {
        // Arrange
        var answer = DateAnswerValue.Create(new DateTime(year, month, day));

        // Act
        var display = answer.DisplayValue;

        // Assert
        Assert.Equal(expected, display);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameDate_ReturnsTrue()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);
        var answer1 = DateAnswerValue.Create(date);
        var answer2 = DateAnswerValue.Create(date);

        // Act & Assert
        Assert.True(answer1.Equals(answer2));
        Assert.True(answer2.Equals(answer1));
    }

    [Fact]
    public void Equals_DifferentDate_ReturnsFalse()
    {
        // Arrange
        var answer1 = DateAnswerValue.Create(new DateTime(2024, 6, 15));
        var answer2 = DateAnswerValue.Create(new DateTime(2024, 6, 16));

        // Act & Assert
        Assert.False(answer1.Equals(answer2));
    }

    [Fact]
    public void Equals_SameDateDifferentConstraints_ReturnsTrue()
    {
        // Value equality is based on Date only, not constraints
        var date = new DateTime(2024, 6, 15);
        var answer1 = DateAnswerValue.Create(date, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        var answer2 = DateAnswerValue.Create(date, new DateTime(2020, 1, 1), new DateTime(2030, 12, 31));

        // Act & Assert
        Assert.True(answer1.Equals(answer2));
    }

    [Fact]
    public void Equals_SameDateDifferentTime_ReturnsTrue()
    {
        // Time component should be stripped
        var answer1 = DateAnswerValue.Create(new DateTime(2024, 6, 15, 10, 30, 0));
        var answer2 = DateAnswerValue.Create(new DateTime(2024, 6, 15, 23, 59, 59));

        // Act & Assert
        Assert.True(answer1.Equals(answer2));
    }

    [Fact]
    public void GetHashCode_SameDate_ReturnsSameHash()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);
        var answer1 = DateAnswerValue.Create(date);
        var answer2 = DateAnswerValue.Create(date);

        // Act & Assert
        Assert.Equal(answer1.GetHashCode(), answer2.GetHashCode());
    }

    #endregion

    #region IsValidFor Tests

    [Fact]
    public void IsValidFor_CorrectQuestionType_ReturnsTrue()
    {
        // Arrange
        var answer = DateAnswerValue.Create(new DateTime(2024, 6, 15));
        var question = Question.Create(1, "Enter a date", QuestionType.Date, 1);

        // Act
        var result = answer.IsValidFor(question);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidFor_WrongQuestionType_ReturnsFalse()
    {
        // Arrange
        var answer = DateAnswerValue.Create(new DateTime(2024, 6, 15));
        var question = Question.Create(1, "Enter text", QuestionType.Text, 1);

        // Act
        var result = answer.IsValidFor(question);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DateFormat Constant Tests

    [Fact]
    public void DateFormat_IsDDMMYYYY()
    {
        // Assert
        Assert.Equal("dd.MM.yyyy", DateAnswerValue.DateFormat);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsReadableFormat()
    {
        // Arrange
        var answer = DateAnswerValue.Create(new DateTime(2024, 6, 15));

        // Act
        var result = answer.ToString();

        // Assert
        Assert.Contains("Date", result);
        Assert.Contains("15.06.2024", result);
    }

    #endregion
}
