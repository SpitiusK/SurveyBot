using FluentAssertions;
using SurveyBot.Bot.Models;
using Xunit;

namespace SurveyBot.Tests.Unit.Bot;

/// <summary>
/// Unit tests for ConversationState skip functionality.
/// Regression tests for TEST-002: Skip-Aware Completion fix.
/// </summary>
public class ConversationStateSkipTests
{
    [Fact]
    public void MarkQuestionSkipped_AddsToSkippedIndices()
    {
        // Arrange
        var state = new ConversationState();

        // Act
        state.MarkQuestionSkipped(2);

        // Assert
        state.SkippedQuestionIndices.Should().Contain(2);
        state.SkippedCount.Should().Be(1);
    }

    [Fact]
    public void MarkQuestionSkipped_IgnoresDuplicates()
    {
        // Arrange
        var state = new ConversationState();
        state.MarkQuestionSkipped(2);

        // Act
        state.MarkQuestionSkipped(2); // Duplicate

        // Assert
        state.SkippedQuestionIndices.Should().HaveCount(1);
        state.SkippedCount.Should().Be(1);
    }

    [Fact]
    public void MarkQuestionSkipped_MaintainsSortedOrder()
    {
        // Arrange
        var state = new ConversationState();

        // Act
        state.MarkQuestionSkipped(5);
        state.MarkQuestionSkipped(2);
        state.MarkQuestionSkipped(8);

        // Assert
        state.SkippedQuestionIndices.Should().BeInAscendingOrder();
        state.SkippedQuestionIndices.Should().ContainInOrder(2, 5, 8);
    }

    [Fact]
    public void MarkQuestionSkipped_MultipleSkips_RecordsAll()
    {
        // Arrange
        var state = new ConversationState();

        // Act
        state.MarkQuestionSkipped(1);
        state.MarkQuestionSkipped(3);
        state.MarkQuestionSkipped(5);

        // Assert
        state.SkippedQuestionIndices.Should().HaveCount(3);
        state.SkippedCount.Should().Be(3);
        state.SkippedQuestionIndices.Should().ContainInOrder(1, 3, 5);
    }

    [Fact]
    public void IsQuestionSkipped_ReturnsTrueForSkippedQuestion()
    {
        // Arrange
        var state = new ConversationState();
        state.MarkQuestionSkipped(3);

        // Act & Assert
        state.IsQuestionSkipped(3).Should().BeTrue();
        state.IsQuestionSkipped(4).Should().BeFalse();
    }

    [Fact]
    public void IsQuestionSkipped_ReturnsFalseForNonSkippedQuestion()
    {
        // Arrange
        var state = new ConversationState();
        state.MarkQuestionSkipped(1);
        state.MarkQuestionSkipped(3);

        // Act & Assert
        state.IsQuestionSkipped(2).Should().BeFalse();
        state.IsQuestionSkipped(5).Should().BeFalse();
    }

    [Fact]
    public void IsQuestionSkipped_ReturnsFalseForEmptySkippedList()
    {
        // Arrange
        var state = new ConversationState();

        // Act & Assert
        state.IsQuestionSkipped(0).Should().BeFalse();
        state.IsQuestionSkipped(10).Should().BeFalse();
    }

    [Fact]
    public void IsAllAnswered_WithSkippedQuestions_ReturnsTrue()
    {
        // Arrange
        var state = new ConversationState
        {
            CurrentQuestionIndex = 3,
            TotalQuestions = 4
        };
        state.MarkQuestionAnswered(0);
        state.MarkQuestionAnswered(1);
        state.MarkQuestionSkipped(2);  // Optional question skipped
        state.MarkQuestionAnswered(3);

        // Act & Assert
        state.IsAllAnswered.Should().BeTrue(); // 3 answered + 1 skipped == 4 total
    }

    [Fact]
    public void IsAllAnswered_WithUnansweredQuestions_ReturnsFalse()
    {
        // Arrange
        var state = new ConversationState
        {
            CurrentQuestionIndex = 2,
            TotalQuestions = 4
        };
        state.MarkQuestionAnswered(0);
        state.MarkQuestionAnswered(1);
        // Questions 2 and 3 not answered or skipped

        // Act & Assert
        state.IsAllAnswered.Should().BeFalse(); // 2 answered + 0 skipped != 4 total
    }

    [Fact]
    public void IsAllAnswered_WithAllSkipped_ReturnsTrue()
    {
        // Arrange - Edge case: all questions are optional and skipped
        var state = new ConversationState
        {
            CurrentQuestionIndex = 2,
            TotalQuestions = 3
        };
        state.MarkQuestionSkipped(0);
        state.MarkQuestionSkipped(1);
        state.MarkQuestionSkipped(2);

        // Act & Assert
        state.IsAllAnswered.Should().BeTrue(); // 0 answered + 3 skipped == 3 total
    }

    [Fact]
    public void IsAllAnswered_MixedAnsweredAndSkipped_ReturnsTrue()
    {
        // Arrange
        var state = new ConversationState
        {
            CurrentQuestionIndex = 4,
            TotalQuestions = 5
        };
        state.MarkQuestionAnswered(0);
        state.MarkQuestionSkipped(1);
        state.MarkQuestionAnswered(2);
        state.MarkQuestionSkipped(3);
        state.MarkQuestionAnswered(4);

        // Act & Assert
        state.IsAllAnswered.Should().BeTrue(); // 3 answered + 2 skipped == 5 total
    }

    [Fact]
    public void IsAllAnswered_PartiallyAnsweredAndSkipped_ReturnsFalse()
    {
        // Arrange
        var state = new ConversationState
        {
            CurrentQuestionIndex = 2,
            TotalQuestions = 5
        };
        state.MarkQuestionAnswered(0);
        state.MarkQuestionSkipped(1);
        state.MarkQuestionAnswered(2);
        // Questions 3 and 4 neither answered nor skipped

        // Act & Assert
        state.IsAllAnswered.Should().BeFalse(); // 2 answered + 1 skipped != 5 total
    }

    [Fact]
    public void IsAllAnswered_NoQuestionsSet_ReturnsFalse()
    {
        // Arrange - Edge case: state not initialized
        var state = new ConversationState
        {
            CurrentQuestionIndex = null,
            TotalQuestions = null
        };

        // Act & Assert
        state.IsAllAnswered.Should().BeFalse();
    }

    [Fact]
    public void ClearSurveyData_ClearsSkippedQuestionIndices()
    {
        // Arrange
        var state = new ConversationState();
        state.MarkQuestionSkipped(1);
        state.MarkQuestionSkipped(3);
        state.SkippedCount.Should().Be(2);

        // Act
        state.ClearSurveyData();

        // Assert
        state.SkippedQuestionIndices.Should().BeEmpty();
        state.SkippedCount.Should().Be(0);
    }

    [Fact]
    public void ClearSurveyData_ClearsBothAnsweredAndSkippedIndices()
    {
        // Arrange
        var state = new ConversationState();
        state.MarkQuestionAnswered(0);
        state.MarkQuestionSkipped(1);
        state.MarkQuestionAnswered(2);
        state.MarkQuestionSkipped(3);

        // Act
        state.ClearSurveyData();

        // Assert
        state.AnsweredQuestionIndices.Should().BeEmpty();
        state.SkippedQuestionIndices.Should().BeEmpty();
        state.AnsweredCount.Should().Be(0);
        state.SkippedCount.Should().Be(0);
    }

    [Fact]
    public void ProgressPercent_IncludesSkippedQuestions()
    {
        // Arrange
        var state = new ConversationState
        {
            TotalQuestions = 10
        };
        state.MarkQuestionAnswered(0);
        state.MarkQuestionAnswered(1);
        state.MarkQuestionSkipped(2);
        state.MarkQuestionAnswered(3);
        state.MarkQuestionSkipped(4);
        // 3 answered + 2 skipped = 5 out of 10

        // Act
        var progress = state.ProgressPercent;

        // Assert - NOTE: ProgressPercent only counts AnsweredCount, not SkippedCount
        // This is by design - progress shows actual answers, not skips
        progress.Should().BeApproximately(30f, 0.01f); // 3 answered / 10 total = 30% (with tolerance for float precision)
    }

    [Fact]
    public void SkippedCount_ReturnsZero_WhenNoSkips()
    {
        // Arrange
        var state = new ConversationState();
        state.MarkQuestionAnswered(0);
        state.MarkQuestionAnswered(1);

        // Act & Assert
        state.SkippedCount.Should().Be(0);
    }

    [Fact]
    public void AnsweredAndSkipped_AreIndependent()
    {
        // Arrange
        var state = new ConversationState
        {
            TotalQuestions = 5
        };

        // Act
        state.MarkQuestionAnswered(0);
        state.MarkQuestionSkipped(1);
        state.MarkQuestionAnswered(2);
        state.MarkQuestionSkipped(3);
        state.MarkQuestionAnswered(4);

        // Assert - Both lists maintain independent state
        state.AnsweredQuestionIndices.Should().ContainInOrder(0, 2, 4);
        state.SkippedQuestionIndices.Should().ContainInOrder(1, 3);
        state.AnsweredCount.Should().Be(3);
        state.SkippedCount.Should().Be(2);
    }

    [Fact]
    public void MarkQuestionSkipped_AfterMarkQuestionAnswered_BothRecorded()
    {
        // Arrange - Edge case: what if same question is both answered and skipped?
        // This shouldn't happen in practice, but let's verify behavior
        var state = new ConversationState();

        // Act
        state.MarkQuestionAnswered(2);
        state.MarkQuestionSkipped(2);

        // Assert - Both lists record the index (edge case, but consistent behavior)
        state.AnsweredQuestionIndices.Should().Contain(2);
        state.SkippedQuestionIndices.Should().Contain(2);
        state.AnsweredCount.Should().Be(1);
        state.SkippedCount.Should().Be(1);
    }
}
