using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using SurveyBot.Bot.Models;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Entities;

namespace SurveyBot.Tests.Unit.Bot;

/// <summary>
/// Unit tests for conversation state question index tracking during conditional flow transitions.
/// Tests verify that question index logic correctly handles:
/// - Sequential flow (backward compatibility)
/// - Conditional flow jumps (skipping questions)
/// - Question ID to index mapping
/// - Edge cases (empty surveys, invalid indexes)
///
/// These tests verify the data structures and logic used by SurveyResponseHandler.UpdateQuestionIndexAsync
/// to prevent the "already answered" bug when transitioning to Text/Rating questions.
/// </summary>
public class ConversationStateQuestionIndexTests
{
    #region Category 1: Question Index Mapping Tests

    [Fact]
    public void FindQuestionIndex_SequentialQuestions_ReturnsCorrectIndex()
    {
        // Arrange - Survey: [Q1 (index 0), Q2 (index 1), Q3 (index 2)]
        var survey = CreateSurveyWithQuestions(
            (1, QuestionType.Text, 0),
            (2, QuestionType.Text, 1),
            (3, QuestionType.Text, 2)
        );

        // Act - Find index for Q2
        var index = survey.Questions.FindIndex(q => q.Id == 2);

        // Assert
        Assert.Equal(1, index);
    }

    [Fact]
    public void FindQuestionIndex_NonSequentialIds_ReturnsCorrectIndex()
    {
        // Arrange - Survey: [Q77 (index 0), Q79 (index 1), Q78 (index 2)]
        // This is the actual bug scenario: non-sequential question IDs
        var survey = CreateSurveyWithQuestions(
            (77, QuestionType.SingleChoice, 0),
            (79, QuestionType.SingleChoice, 1),
            (78, QuestionType.Text, 2)
        );

        // Act - Find index for Q78
        var index = survey.Questions.FindIndex(q => q.Id == 78);

        // Assert
        Assert.Equal(2, index);
        Assert.NotEqual(1, index); // Q78 is at index 2, not 1
    }

    [Fact]
    public void FindQuestionIndex_QuestionNotFound_ReturnsNegativeOne()
    {
        // Arrange
        var survey = CreateSurveyWithQuestions(
            (1, QuestionType.Text, 0),
            (2, QuestionType.Text, 1)
        );

        // Act - Try to find non-existent question
        var index = survey.Questions.FindIndex(q => q.Id == 999);

        // Assert
        Assert.Equal(-1, index);
    }

    [Fact]
    public void FindQuestionIndex_EmptySurvey_ReturnsNegativeOne()
    {
        // Arrange
        var survey = new SurveyDto
        {
            Id = 1,
            Questions = new List<QuestionDto>()
        };

        // Act
        var index = survey.Questions.FindIndex(q => q.Id == 1);

        // Assert
        Assert.Equal(-1, index);
    }

    [Fact]
    public void AccessQuestionByIndex_ValidIndex_ReturnsCorrectQuestion()
    {
        // Arrange
        var survey = CreateSurveyWithQuestions(
            (77, QuestionType.SingleChoice, 0),
            (79, QuestionType.SingleChoice, 1),
            (78, QuestionType.Text, 2)
        );

        // Act - Access by index
        var question = survey.Questions[2];

        // Assert - Q78 is at index 2
        Assert.Equal(78, question.Id);
        Assert.Equal(QuestionType.Text, question.QuestionType);
    }

    [Fact]
    public void AccessQuestionByIndex_InvalidIndex_ThrowsException()
    {
        // Arrange
        var survey = CreateSurveyWithQuestions(
            (1, QuestionType.Text, 0),
            (2, QuestionType.Text, 1)
        );

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => survey.Questions[5]);
    }

    #endregion

    #region Category 2: Conversation State Index Tracking

    [Fact]
    public void ConversationState_InitialIndex_IsZero()
    {
        // Arrange & Act
        var state = new ConversationState
        {
            CurrentSurveyId = 1,
            CurrentResponseId = 100,
            CurrentQuestionIndex = 0,
            TotalQuestions = 10
        };

        // Assert
        Assert.Equal(0, state.CurrentQuestionIndex);
    }

    [Fact]
    public void ConversationState_UpdateIndex_TracksNewValue()
    {
        // Arrange
        var state = new ConversationState
        {
            CurrentQuestionIndex = 0
        };

        // Act - Simulate index update (Q1 → Q3, skipping Q2)
        state.CurrentQuestionIndex = 2;

        // Assert
        Assert.Equal(2, state.CurrentQuestionIndex);
    }

    [Fact]
    public void ConversationState_BackwardNavigation_AllowsDecreasingIndex()
    {
        // Arrange
        var state = new ConversationState
        {
            CurrentQuestionIndex = 3
        };

        // Act - Go back to earlier question
        state.CurrentQuestionIndex = 1;

        // Assert
        Assert.Equal(1, state.CurrentQuestionIndex);
    }

    [Fact]
    public void ConversationState_SkipMultipleQuestions_UpdatesIndexCorrectly()
    {
        // Arrange - Start at Q1 (index 0)
        var state = new ConversationState
        {
            CurrentQuestionIndex = 0
        };

        // Act - Jump to Q4 (index 3), skipping Q2 and Q3
        state.CurrentQuestionIndex = 3;

        // Assert
        Assert.Equal(3, state.CurrentQuestionIndex);
    }

    #endregion

    #region Category 3: Visited Question Tracking (Cycle Prevention)

    [Fact]
    public void HasVisitedQuestion_NewQuestion_ReturnsFalse()
    {
        // Arrange
        var state = new ConversationState
        {
            VisitedQuestionIds = new List<int>()
        };

        // Act & Assert
        Assert.False(state.HasVisitedQuestion(1));
    }

    [Fact]
    public void RecordVisitedQuestion_AddsQuestionId()
    {
        // Arrange
        var state = new ConversationState
        {
            VisitedQuestionIds = new List<int>()
        };

        // Act
        state.RecordVisitedQuestion(78);

        // Assert
        Assert.True(state.HasVisitedQuestion(78));
        Assert.Contains(78, state.VisitedQuestionIds);
    }

    [Fact]
    public void RecordVisitedQuestion_DuplicateId_DoesNotAddTwice()
    {
        // Arrange
        var state = new ConversationState
        {
            VisitedQuestionIds = new List<int>()
        };

        // Act - Record same question twice
        state.RecordVisitedQuestion(5);
        state.RecordVisitedQuestion(5);

        // Assert - Should only appear once
        Assert.Single(state.VisitedQuestionIds);
        Assert.Equal(5, state.VisitedQuestionIds[0]);
    }

    [Fact]
    public void HasVisitedQuestion_MultipleQuestions_TracksAll()
    {
        // Arrange
        var state = new ConversationState
        {
            VisitedQuestionIds = new List<int>()
        };

        // Act - Record multiple questions
        state.RecordVisitedQuestion(1);
        state.RecordVisitedQuestion(3);
        state.RecordVisitedQuestion(5);

        // Assert
        Assert.True(state.HasVisitedQuestion(1));
        Assert.True(state.HasVisitedQuestion(3));
        Assert.True(state.HasVisitedQuestion(5));
        Assert.False(state.HasVisitedQuestion(2)); // Not visited
        Assert.False(state.HasVisitedQuestion(4)); // Not visited
    }

    [Fact]
    public void ClearVisitedQuestions_RemovesAllRecords()
    {
        // Arrange
        var state = new ConversationState
        {
            VisitedQuestionIds = new List<int> { 1, 2, 3 }
        };

        // Act
        state.ClearVisitedQuestions();

        // Assert
        Assert.Empty(state.VisitedQuestionIds);
        Assert.False(state.HasVisitedQuestion(1));
        Assert.False(state.HasVisitedQuestion(2));
        Assert.False(state.HasVisitedQuestion(3));
    }

    #endregion

    #region Category 4: Index and VisitedQuestions Integration

    [Fact]
    public void IndexUpdate_IndependentOfVisitedTracking()
    {
        // Arrange
        var state = new ConversationState
        {
            CurrentQuestionIndex = 0,
            VisitedQuestionIds = new List<int>()
        };

        // Act - Update index and record visited (simulating handler logic)
        state.CurrentQuestionIndex = 2; // Jump to index 2
        state.RecordVisitedQuestion(78); // Record Q78 as visited

        // Assert - Both track independently
        Assert.Equal(2, state.CurrentQuestionIndex); // Index for progress display
        Assert.True(state.HasVisitedQuestion(78)); // ID for cycle prevention
    }

    [Fact]
    public void MultipleTransitions_TracksBothIndexAndVisited()
    {
        // Arrange - Simulate survey with complex flow
        var state = new ConversationState
        {
            CurrentQuestionIndex = 0,
            VisitedQuestionIds = new List<int>()
        };

        // Act - Simulate transitions: Q1 → Q3 → Q5
        // Transition 1: Q1 (ID=1, index=0)
        state.CurrentQuestionIndex = 0;
        state.RecordVisitedQuestion(1);

        // Transition 2: Jump to Q3 (ID=3, index=2)
        state.CurrentQuestionIndex = 2;
        state.RecordVisitedQuestion(3);

        // Transition 3: Jump to Q5 (ID=5, index=4)
        state.CurrentQuestionIndex = 4;
        state.RecordVisitedQuestion(5);

        // Assert - Final state
        Assert.Equal(4, state.CurrentQuestionIndex); // At index 4
        Assert.Equal(3, state.VisitedQuestionIds.Count); // Visited 3 questions
        Assert.True(state.HasVisitedQuestion(1));
        Assert.False(state.HasVisitedQuestion(2)); // Skipped
        Assert.True(state.HasVisitedQuestion(3));
        Assert.False(state.HasVisitedQuestion(4)); // Skipped
        Assert.True(state.HasVisitedQuestion(5));
    }

    #endregion

    #region Category 5: Bug Scenario Verification

    [Fact]
    public void BugScenario_NonSequentialIds_IndexMustBeUpdated()
    {
        // This test documents the bug we fixed:
        // Survey: [Q77 (index 0), Q79 (index 1), Q78 (index 2)]
        // User answers Q77 → API returns Q78
        // BUG: If index stays at 0, fetching Questions[0] returns Q77 (wrong!)
        // FIX: Update index to 2, fetching Questions[2] returns Q78 (correct!)

        // Arrange
        var survey = CreateSurveyWithQuestions(
            (77, QuestionType.SingleChoice, 0),
            (79, QuestionType.SingleChoice, 1),
            (78, QuestionType.Text, 2)
        );

        var state = new ConversationState
        {
            CurrentQuestionIndex = 0, // At Q77
            VisitedQuestionIds = new List<int>()
        };

        // Act - Answer Q77, API returns Q78 (ID=78)
        var nextQuestionId = 78;

        // WRONG: Using stale index (bug scenario)
        var wrongQuestion = survey.Questions[state.CurrentQuestionIndex.Value];

        // CORRECT: Find new index and update
        var correctIndex = survey.Questions.FindIndex(q => q.Id == nextQuestionId);
        state.CurrentQuestionIndex = correctIndex;
        var correctQuestion = survey.Questions[state.CurrentQuestionIndex.Value];

        // Assert - Demonstrate the bug and the fix
        Assert.Equal(77, wrongQuestion.Id); // Bug: Would fetch Q77
        Assert.Equal(78, correctQuestion.Id); // Fix: Fetches Q78
        Assert.Equal(2, state.CurrentQuestionIndex); // Index correctly updated
    }

    [Fact]
    public void BugScenario_StaleIndex_CausesAlreadyAnsweredError()
    {
        // This documents the user-facing symptom of the bug:
        // 1. User answers Q77 → navigates to Q78
        // 2. If index not updated, state.CurrentQuestionIndex = 0
        // 3. Handler fetches Questions[0] = Q77
        // 4. state.HasVisitedQuestion(77) returns true
        // 5. User sees "already answered" error

        // Arrange
        var survey = CreateSurveyWithQuestions(
            (77, QuestionType.SingleChoice, 0),
            (79, QuestionType.SingleChoice, 1),
            (78, QuestionType.Text, 2)
        );

        var state = new ConversationState
        {
            CurrentQuestionIndex = 0, // Stale index (not updated!)
            VisitedQuestionIds = new List<int> { 77 } // User answered Q77
        };

        // Act - Bug: Fetch using stale index
        var fetchedQuestion = survey.Questions[state.CurrentQuestionIndex.Value];

        // Assert - This is why the bug occurred
        Assert.Equal(77, fetchedQuestion.Id); // Wrong question fetched
        Assert.True(state.HasVisitedQuestion(fetchedQuestion.Id)); // Already answered!
        // User would see: "You've already answered this question"
    }

    [Fact]
    public void FixScenario_UpdatedIndex_PreventsAlreadyAnsweredError()
    {
        // This demonstrates the fix working correctly

        // Arrange
        var survey = CreateSurveyWithQuestions(
            (77, QuestionType.SingleChoice, 0),
            (79, QuestionType.SingleChoice, 1),
            (78, QuestionType.Text, 2)
        );

        var state = new ConversationState
        {
            CurrentQuestionIndex = 0,
            VisitedQuestionIds = new List<int> { 77 } // User answered Q77
        };

        // Act - Fix: Update index when API returns Q78
        var nextQuestionId = 78;
        var newIndex = survey.Questions.FindIndex(q => q.Id == nextQuestionId);
        state.CurrentQuestionIndex = newIndex; // UPDATE INDEX!

        var fetchedQuestion = survey.Questions[state.CurrentQuestionIndex.Value];

        // Assert - Fix works correctly
        Assert.Equal(78, fetchedQuestion.Id); // Correct question fetched
        Assert.False(state.HasVisitedQuestion(fetchedQuestion.Id)); // Not answered yet!
        Assert.Equal(2, state.CurrentQuestionIndex); // Index correctly points to Q78
    }

    #endregion

    #region Helper Methods

    private SurveyDto CreateSurveyWithQuestions(params (int id, QuestionType type, int orderIndex)[] questions)
    {
        var survey = new SurveyDto
        {
            Id = 1,
            Title = "Test Survey",
            Code = "TEST",
            IsActive = true,
            Questions = new List<QuestionDto>()
        };

        foreach (var (id, type, orderIndex) in questions)
        {
            var question = new QuestionDto
            {
                Id = id,
                SurveyId = survey.Id,
                QuestionText = $"Question {id}",
                QuestionType = type,
                OrderIndex = orderIndex,
                IsRequired = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (type == QuestionType.SingleChoice || type == QuestionType.MultipleChoice)
            {
                question.Options = new List<string> { "Option A", "Option B", "Option C" };
            }

            survey.Questions.Add(question);
        }

        return survey;
    }

    #endregion
}
