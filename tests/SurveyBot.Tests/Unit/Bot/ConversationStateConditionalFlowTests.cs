using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SurveyBot.Bot.Models;
using Xunit;

namespace SurveyBot.Tests.Unit.Bot;

/// <summary>
/// Unit tests for ConversationState conditional flow features.
/// Tests the state's ability to:
/// - Track visited questions for cycle prevention
/// - Record and check visited questions
/// - Clear visited questions on survey completion
/// - Maintain state integrity across transitions
/// </summary>
public class ConversationStateConditionalFlowTests
{
    #region Test 8: ConversationState_TrackingVisitedQuestions_WorksCorrectly

    [Fact]
    public void ConversationState_RecordVisitedQuestion_AddsQuestionToList()
    {
        // Arrange
        var state = new ConversationState();

        // Act
        state.RecordVisitedQuestion(1);
        state.RecordVisitedQuestion(2);
        state.RecordVisitedQuestion(3);

        // Assert
        state.VisitedQuestionIds.Should().HaveCount(3);
        state.VisitedQuestionIds.Should().Contain(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ConversationState_RecordVisitedQuestion_PreventsDuplicates()
    {
        // Arrange
        var state = new ConversationState();

        // Act
        state.RecordVisitedQuestion(1);
        state.RecordVisitedQuestion(1); // Duplicate
        state.RecordVisitedQuestion(2);
        state.RecordVisitedQuestion(1); // Another duplicate

        // Assert
        state.VisitedQuestionIds.Should().HaveCount(2);
        state.VisitedQuestionIds.Should().Contain(new[] { 1, 2 });
    }

    [Fact]
    public void ConversationState_HasVisitedQuestion_ReturnsTrueForVisitedQuestion()
    {
        // Arrange
        var state = new ConversationState();
        state.RecordVisitedQuestion(1);
        state.RecordVisitedQuestion(2);

        // Act & Assert
        state.HasVisitedQuestion(1).Should().BeTrue();
        state.HasVisitedQuestion(2).Should().BeTrue();
    }

    [Fact]
    public void ConversationState_HasVisitedQuestion_ReturnsFalseForUnvisitedQuestion()
    {
        // Arrange
        var state = new ConversationState();
        state.RecordVisitedQuestion(1);

        // Act & Assert
        state.HasVisitedQuestion(2).Should().BeFalse();
        state.HasVisitedQuestion(3).Should().BeFalse();
    }

    [Fact]
    public void ConversationState_ClearVisitedQuestions_RemovesAllVisitedQuestions()
    {
        // Arrange
        var state = new ConversationState();
        state.RecordVisitedQuestion(1);
        state.RecordVisitedQuestion(2);
        state.RecordVisitedQuestion(3);

        // Act
        state.ClearVisitedQuestions();

        // Assert
        state.VisitedQuestionIds.Should().BeEmpty();
        state.HasVisitedQuestion(1).Should().BeFalse();
        state.HasVisitedQuestion(2).Should().BeFalse();
        state.HasVisitedQuestion(3).Should().BeFalse();
    }

    [Fact]
    public void ConversationState_ClearSurveyData_ClearsVisitedQuestions()
    {
        // Arrange
        var state = new ConversationState
        {
            CurrentSurveyId = 1,
            CurrentResponseId = 100,
            CurrentQuestionIndex = 2
        };
        state.RecordVisitedQuestion(1);
        state.RecordVisitedQuestion(2);

        // Act
        state.ClearSurveyData();

        // Assert
        state.VisitedQuestionIds.Should().BeEmpty();
        state.CurrentSurveyId.Should().BeNull();
        state.CurrentResponseId.Should().BeNull();
        state.CurrentQuestionIndex.Should().BeNull();
    }

    #endregion

    #region Test 9: ConversationState_StateTransitions_AllPropertiesUpdated

    [Fact]
    public void ConversationState_InitializeWithSurveyData_SetsAllProperties()
    {
        // Arrange
        var state = new ConversationState();

        // Act
        state.CurrentSurveyId = 1;
        state.CurrentResponseId = 100;
        state.CurrentQuestionIndex = 0;
        state.TotalQuestions = 5;

        // Assert
        state.CurrentSurveyId.Should().Be(1);
        state.CurrentResponseId.Should().Be(100);
        state.CurrentQuestionIndex.Should().Be(0);
        state.TotalQuestions.Should().Be(5);
        state.VisitedQuestionIds.Should().BeEmpty(); // Initially empty
    }

    [Fact]
    public void ConversationState_RecordVisitedQuestionsDuringFlow_MaintainsHistory()
    {
        // Arrange
        var state = new ConversationState
        {
            CurrentSurveyId = 1,
            CurrentResponseId = 100,
            CurrentQuestionIndex = 0,
            TotalQuestions = 5
        };

        // Act - Simulate answering questions with branching
        state.RecordVisitedQuestion(1); // Question 1
        state.CurrentQuestionIndex = 1;

        state.RecordVisitedQuestion(3); // Question 3 (skipped 2 due to branching)
        state.CurrentQuestionIndex = 2;

        state.RecordVisitedQuestion(5); // Question 5 (skipped 4 due to branching)
        state.CurrentQuestionIndex = 3;

        // Assert
        state.VisitedQuestionIds.Should().HaveCount(3);
        state.VisitedQuestionIds.Should().Contain(new[] { 1, 3, 5 });
        state.CurrentQuestionIndex.Should().Be(3);
    }

    [Fact]
    public void ConversationState_StateTransitionToComplete_ClearsAllSurveyData()
    {
        // Arrange
        var state = new ConversationState
        {
            CurrentSurveyId = 1,
            CurrentResponseId = 100,
            CurrentQuestionIndex = 4,
            TotalQuestions = 5,
            CurrentState = ConversationStateType.InSurvey
        };
        state.RecordVisitedQuestion(1);
        state.RecordVisitedQuestion(2);
        state.RecordVisitedQuestion(3);
        state.RecordVisitedQuestion(4);
        state.RecordVisitedQuestion(5);

        // Act
        state.CurrentState = ConversationStateType.ResponseComplete;
        state.ClearSurveyData();

        // Assert
        state.CurrentState.Should().Be(ConversationStateType.ResponseComplete);
        state.CurrentSurveyId.Should().BeNull();
        state.CurrentResponseId.Should().BeNull();
        state.CurrentQuestionIndex.Should().BeNull();
        state.TotalQuestions.Should().BeNull();
        state.VisitedQuestionIds.Should().BeEmpty();
    }

    [Fact]
    public void ConversationState_Reset_ClearsEverything()
    {
        // Arrange
        var state = new ConversationState
        {
            CurrentSurveyId = 1,
            CurrentResponseId = 100,
            CurrentQuestionIndex = 2,
            TotalQuestions = 5,
            CurrentState = ConversationStateType.InSurvey
        };
        state.RecordVisitedQuestion(1);
        state.RecordVisitedQuestion(2);
        state.AnsweredQuestionIndices.Add(0);
        state.AnsweredQuestionIndices.Add(1);
        state.CachedAnswers[0] = "{\"text\":\"Answer 1\"}";
        state.StateHistory.Push(ConversationStateType.Idle);

        // Act
        state.Reset();

        // Assert
        state.CurrentState.Should().Be(ConversationStateType.Idle);
        state.CurrentSurveyId.Should().BeNull();
        state.CurrentResponseId.Should().BeNull();
        state.CurrentQuestionIndex.Should().BeNull();
        state.TotalQuestions.Should().BeNull();
        state.VisitedQuestionIds.Should().BeEmpty();
        state.AnsweredQuestionIndices.Should().BeEmpty();
        state.CachedAnswers.Should().BeEmpty();
        state.StateHistory.Should().BeEmpty();
    }

    [Fact]
    public void ConversationState_UpdateActivity_UpdatesTimestamp()
    {
        // Arrange
        var state = new ConversationState();
        var originalTimestamp = state.LastActivityAt;

        // Act
        System.Threading.Thread.Sleep(10); // Small delay to ensure different timestamp
        state.UpdateActivity();

        // Assert
        state.LastActivityAt.Should().BeAfter(originalTimestamp);
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public void ConversationState_RecordVisitedQuestion_HandlesNegativeIds()
    {
        // Arrange
        var state = new ConversationState();

        // Act
        state.RecordVisitedQuestion(-1);
        state.RecordVisitedQuestion(0);
        state.RecordVisitedQuestion(1);

        // Assert - Should handle all IDs
        state.VisitedQuestionIds.Should().HaveCount(3);
        state.HasVisitedQuestion(-1).Should().BeTrue();
        state.HasVisitedQuestion(0).Should().BeTrue();
        state.HasVisitedQuestion(1).Should().BeTrue();
    }

    [Fact]
    public void ConversationState_RecordVisitedQuestion_HandlesLargeIds()
    {
        // Arrange
        var state = new ConversationState();

        // Act
        state.RecordVisitedQuestion(int.MaxValue);
        state.RecordVisitedQuestion(1000000);

        // Assert
        state.VisitedQuestionIds.Should().HaveCount(2);
        state.HasVisitedQuestion(int.MaxValue).Should().BeTrue();
        state.HasVisitedQuestion(1000000).Should().BeTrue();
    }

    [Fact]
    public void ConversationState_RecordVisitedQuestion_MaintainsOrderOfInsertion()
    {
        // Arrange
        var state = new ConversationState();

        // Act
        state.RecordVisitedQuestion(3);
        state.RecordVisitedQuestion(1);
        state.RecordVisitedQuestion(5);
        state.RecordVisitedQuestion(2);

        // Assert
        state.VisitedQuestionIds.Should().HaveCount(4);
        state.VisitedQuestionIds[0].Should().Be(3);
        state.VisitedQuestionIds[1].Should().Be(1);
        state.VisitedQuestionIds[2].Should().Be(5);
        state.VisitedQuestionIds[3].Should().Be(2);
    }

    [Fact]
    public void ConversationState_ClearVisitedQuestions_CanBeCalledMultipleTimes()
    {
        // Arrange
        var state = new ConversationState();
        state.RecordVisitedQuestion(1);

        // Act
        state.ClearVisitedQuestions();
        state.ClearVisitedQuestions(); // Second call
        state.ClearVisitedQuestions(); // Third call

        // Assert - Should not throw
        state.VisitedQuestionIds.Should().BeEmpty();
    }

    [Fact]
    public void ConversationState_VisitedQuestionsIndependentFromAnsweredQuestions()
    {
        // Arrange
        var state = new ConversationState();

        // Act
        state.RecordVisitedQuestion(1);
        state.RecordVisitedQuestion(2);
        state.MarkQuestionAnswered(0); // Different tracking
        state.MarkQuestionAnswered(1);

        // Assert - Both lists are independent
        state.VisitedQuestionIds.Should().HaveCount(2);
        state.VisitedQuestionIds.Should().Contain(new[] { 1, 2 });
        state.AnsweredQuestionIndices.Should().HaveCount(2);
        state.AnsweredQuestionIndices.Should().Contain(new[] { 0, 1 });
    }

    [Fact]
    public void ConversationState_BranchingScenario_TracksNonSequentialQuestions()
    {
        // Arrange - Simulate complex branching flow
        var state = new ConversationState
        {
            CurrentSurveyId = 1,
            CurrentResponseId = 100,
            TotalQuestions = 10
        };

        // Act - Simulate branching path: Q1 → Q3 → Q7 → Q10 (skipped 2,4,5,6,8,9)
        state.RecordVisitedQuestion(1);
        state.RecordVisitedQuestion(3);
        state.RecordVisitedQuestion(7);
        state.RecordVisitedQuestion(10);

        // Assert
        state.VisitedQuestionIds.Should().Equal(new[] { 1, 3, 7, 10 });
        state.HasVisitedQuestion(1).Should().BeTrue();
        state.HasVisitedQuestion(2).Should().BeFalse(); // Skipped
        state.HasVisitedQuestion(3).Should().BeTrue();
        state.HasVisitedQuestion(4).Should().BeFalse(); // Skipped
        state.HasVisitedQuestion(7).Should().BeTrue();
        state.HasVisitedQuestion(10).Should().BeTrue();
    }

    #endregion
}
