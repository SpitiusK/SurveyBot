using FluentAssertions;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Unit.Features;

/// <summary>
/// Unit tests for the IncludeInStatistics feature on Question entity.
/// Tests the entity property, factory method, and setter behavior.
/// </summary>
public class IncludeInStatisticsTests
{
    #region Factory Method Tests

    [Fact]
    public void Question_Create_SetsIncludeInStatisticsToTrue_ByDefault()
    {
        // Arrange & Act
        var question = EntityBuilder.CreateQuestion();

        // Assert
        question.IncludeInStatistics.Should().BeTrue(
            because: "new questions should be included in statistics by default");
    }

    [Fact]
    public void Question_Create_SetsIncludeInStatisticsToFalse_WhenSpecified()
    {
        // Arrange & Act
        var question = EntityBuilder.CreateQuestion(includeInStatistics: false);

        // Assert
        question.IncludeInStatistics.Should().BeFalse(
            because: "the factory method should respect the includeInStatistics parameter");
    }

    [Fact]
    public void Question_Create_SetsIncludeInStatisticsToTrue_WhenExplicitlySpecified()
    {
        // Arrange & Act
        var question = EntityBuilder.CreateQuestion(includeInStatistics: true);

        // Assert
        question.IncludeInStatistics.Should().BeTrue();
    }

    [Theory]
    [InlineData(QuestionType.Text)]
    [InlineData(QuestionType.SingleChoice)]
    [InlineData(QuestionType.MultipleChoice)]
    [InlineData(QuestionType.Rating)]
    [InlineData(QuestionType.Location)]
    [InlineData(QuestionType.Number)]
    [InlineData(QuestionType.Date)]
    public void Question_Create_DefaultsToIncludeInStatistics_ForAllQuestionTypes(QuestionType questionType)
    {
        // Arrange & Act
        var question = EntityBuilder.CreateQuestion(questionType: questionType);

        // Assert
        question.IncludeInStatistics.Should().BeTrue(
            because: $"all question types ({questionType}) should default to being included in statistics");
    }

    #endregion

    #region Setter Method Tests

    [Fact]
    public void SetIncludeInStatistics_ToFalse_UpdatesPropertyAndTimestamp()
    {
        // Arrange
        var question = EntityBuilder.CreateQuestion(includeInStatistics: true);
        var originalUpdatedAt = question.UpdatedAt;

        // Small delay to ensure timestamp difference
        Thread.Sleep(10);

        // Act
        question.SetIncludeInStatistics(false);

        // Assert
        question.IncludeInStatistics.Should().BeFalse();
        question.UpdatedAt.Should().BeAfter(originalUpdatedAt,
            because: "modifying IncludeInStatistics should update the timestamp");
    }

    [Fact]
    public void SetIncludeInStatistics_ToTrue_UpdatesPropertyAndTimestamp()
    {
        // Arrange
        var question = EntityBuilder.CreateQuestion(includeInStatistics: false);
        var originalUpdatedAt = question.UpdatedAt;

        // Small delay to ensure timestamp difference
        Thread.Sleep(10);

        // Act
        question.SetIncludeInStatistics(true);

        // Assert
        question.IncludeInStatistics.Should().BeTrue();
        question.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void SetIncludeInStatistics_ToSameValue_StillUpdatesTimestamp()
    {
        // Arrange
        var question = EntityBuilder.CreateQuestion(includeInStatistics: true);
        var originalUpdatedAt = question.UpdatedAt;

        // Small delay to ensure timestamp difference
        Thread.Sleep(10);

        // Act
        question.SetIncludeInStatistics(true);

        // Assert
        question.IncludeInStatistics.Should().BeTrue();
        // The setter should still update timestamp even if value doesn't change
        // (following DDD pattern of tracking all modifications)
        question.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    #endregion

    #region Property Encapsulation Tests

    [Fact]
    public void IncludeInStatistics_HasPrivateSetter()
    {
        // Arrange
        var propertyInfo = typeof(Question).GetProperty(nameof(Question.IncludeInStatistics));

        // Assert
        propertyInfo.Should().NotBeNull();
        propertyInfo!.SetMethod.Should().NotBeNull();
        propertyInfo.SetMethod!.IsPrivate.Should().BeTrue(
            because: "IncludeInStatistics should have a private setter following DDD encapsulation pattern");
    }

    [Fact]
    public void IncludeInStatistics_CanOnlyBeModifiedViaSetterMethod()
    {
        // Arrange
        var question = EntityBuilder.CreateQuestion(includeInStatistics: true);

        // Act - Can only change via setter method
        question.SetIncludeInStatistics(false);

        // Assert
        question.IncludeInStatistics.Should().BeFalse(
            because: "the only way to modify IncludeInStatistics is through SetIncludeInStatistics()");
    }

    #endregion

    #region Integration with Other Properties Tests

    [Fact]
    public void Question_WithIncludeInStatisticsFalse_OtherPropertiesUnaffected()
    {
        // Arrange
        const string questionText = "What is your feedback?";
        const int orderIndex = 2;
        const bool isRequired = true;

        // Act
        var question = EntityBuilder.CreateQuestion(
            questionText: questionText,
            orderIndex: orderIndex,
            isRequired: isRequired,
            includeInStatistics: false);

        // Assert
        question.QuestionText.Should().Be(questionText);
        question.OrderIndex.Should().Be(orderIndex);
        question.IsRequired.Should().Be(isRequired);
        question.IncludeInStatistics.Should().BeFalse(
            because: "IncludeInStatistics should not affect other properties");
    }

    [Fact]
    public void Question_ChangingIncludeInStatistics_DoesNotAffectOtherProperties()
    {
        // Arrange
        var question = EntityBuilder.CreateQuestion(
            questionText: "Original Question",
            orderIndex: 5,
            isRequired: true,
            includeInStatistics: true);

        var originalText = question.QuestionText;
        var originalOrderIndex = question.OrderIndex;
        var originalIsRequired = question.IsRequired;

        // Act
        question.SetIncludeInStatistics(false);

        // Assert
        question.QuestionText.Should().Be(originalText);
        question.OrderIndex.Should().Be(originalOrderIndex);
        question.IsRequired.Should().Be(originalIsRequired);
        question.IncludeInStatistics.Should().BeFalse();
    }

    #endregion
}
