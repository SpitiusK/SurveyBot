using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Models;
using SurveyBot.Bot.Services;
using Xunit;

namespace SurveyBot.Tests.Unit.Bot;

/// <summary>
/// Unit tests for ConversationStateManager
/// </summary>
public class ConversationStateManagerTests
{
    private readonly ConversationStateManager _manager;
    private readonly Mock<ILogger<ConversationStateManager>> _loggerMock;
    private const long TestUserId = 123456789;

    public ConversationStateManagerTests()
    {
        _loggerMock = new Mock<ILogger<ConversationStateManager>>();
        _manager = new ConversationStateManager(_loggerMock.Object);
    }

    #region State Access Tests

    [Fact]
    public async Task GetState_ReturnsNull_WhenStateNotFound()
    {
        // Act
        var result = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetState_StoresState_Successfully()
    {
        // Arrange
        var state = new ConversationState { UserId = TestUserId, CurrentState = ConversationStateType.Idle };

        // Act
        await _manager.SetStateAsync(TestUserId, state);
        var result = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestUserId, result.UserId);
        Assert.Equal(ConversationStateType.Idle, result.CurrentState);
    }

    [Fact]
    public async Task GetState_UpdatesLastActivity_OnAccess()
    {
        // Arrange
        var state = new ConversationState { UserId = TestUserId };
        var originalActivity = state.LastActivityAt;
        await _manager.SetStateAsync(TestUserId, state);

        // Act
        await Task.Delay(100);
        var result = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.True(result.LastActivityAt > originalActivity);
    }

    [Fact]
    public async Task ClearState_RemovesState_Successfully()
    {
        // Arrange
        var state = new ConversationState { UserId = TestUserId };
        await _manager.SetStateAsync(TestUserId, state);

        // Act
        await _manager.ClearStateAsync(TestUserId);
        var result = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HasActiveState_ReturnsTrue_WhenStateExists()
    {
        // Arrange
        var state = new ConversationState { UserId = TestUserId };
        await _manager.SetStateAsync(TestUserId, state);

        // Act
        var result = await _manager.HasActiveStateAsync(TestUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasActiveState_ReturnsFalse_WhenStateNotFound()
    {
        // Act
        var result = await _manager.HasActiveStateAsync(TestUserId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Expiration Tests

    [Fact]
    public async Task GetState_ReturnsNull_WhenStateExpired()
    {
        // Arrange
        var state = new ConversationState
        {
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow.AddMinutes(-31) // 31 minutes old
        };
        await _manager.SetStateAsync(TestUserId, state);

        // Act
        var result = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task IsExpired_ReturnsTrue_After30MinutesInactivity()
    {
        // Arrange
        var state = new ConversationState
        {
            UserId = TestUserId,
            LastActivityAt = DateTime.UtcNow.AddMinutes(-31)
        };

        // Act
        var result = state.IsExpired;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsExpired_ReturnsFalse_WithinInactivityWindow()
    {
        // Arrange
        var state = new ConversationState
        {
            UserId = TestUserId,
            LastActivityAt = DateTime.UtcNow.AddMinutes(-15)
        };

        // Act
        var result = state.IsExpired;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Survey Operations Tests

    [Fact]
    public async Task StartSurvey_CreatesNewState_Successfully()
    {
        // Arrange
        const int surveyId = 1;
        const int responseId = 100;
        const int totalQuestions = 10;

        // Act
        var result = await _manager.StartSurveyAsync(TestUserId, surveyId, responseId, totalQuestions);
        var state = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.True(result);
        Assert.NotNull(state);
        Assert.Equal(surveyId, state.CurrentSurveyId);
        Assert.Equal(responseId, state.CurrentResponseId);
        Assert.Equal(0, state.CurrentQuestionIndex);
        Assert.Equal(totalQuestions, state.TotalQuestions);
        Assert.Equal(ConversationStateType.InSurvey, state.CurrentState);
    }

    [Fact]
    public async Task StartSurvey_UpdatesExistingState_WhenAlreadyInSurvey()
    {
        // Arrange
        var state = new ConversationState { UserId = TestUserId, CurrentState = ConversationStateType.Idle };
        await _manager.SetStateAsync(TestUserId, state);

        // Act
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 5);
        var updated = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.Equal(ConversationStateType.InSurvey, updated.CurrentState);
        Assert.Equal(1, updated.CurrentSurveyId);
    }

    [Fact]
    public async Task AnswerQuestion_CachesAnswer_Successfully()
    {
        // Arrange
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 5);
        const string answerJson = "{\"text\":\"My Answer\"}";

        // Act
        var result = await _manager.AnswerQuestionAsync(TestUserId, 0, answerJson);
        var state = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.True(result);
        Assert.Contains(0, state.AnsweredQuestionIndices);
        Assert.Equal(answerJson, state.GetCachedAnswer(0));
    }

    [Fact]
    public async Task NextQuestion_MovesForward_Successfully()
    {
        // Arrange
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 5);
        await _manager.AnswerQuestionAsync(TestUserId, 0, "{}");

        // Act
        var result = await _manager.NextQuestionAsync(TestUserId);
        var state = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.True(result);
        Assert.Equal(1, state.CurrentQuestionIndex);
    }

    [Fact]
    public async Task NextQuestion_ReturnsFalse_WhenAtLastQuestion()
    {
        // Arrange
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 3);
        // Set to last question
        var state = await _manager.GetStateAsync(TestUserId);
        state.CurrentQuestionIndex = 2; // 0-based, so 2 is last in 3-question survey
        await _manager.SetStateAsync(TestUserId, state);

        // Act
        var result = await _manager.NextQuestionAsync(TestUserId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PreviousQuestion_MovesBackward_Successfully()
    {
        // Arrange
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 5);
        var state = await _manager.GetStateAsync(TestUserId);
        state.CurrentQuestionIndex = 2;
        await _manager.SetStateAsync(TestUserId, state);

        // Act
        var result = await _manager.PreviousQuestionAsync(TestUserId);
        var updated = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.True(result);
        Assert.Equal(1, updated.CurrentQuestionIndex);
    }

    [Fact]
    public async Task PreviousQuestion_ReturnsFalse_OnFirstQuestion()
    {
        // Arrange
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 5);

        // Act
        var result = await _manager.PreviousQuestionAsync(TestUserId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SkipQuestion_MovesForward_WhenOptional()
    {
        // Arrange
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 5);

        // Act
        var result = await _manager.SkipQuestionAsync(TestUserId, isRequired: false);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SkipQuestion_ReturnsFalse_WhenRequired()
    {
        // Arrange
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 5);

        // Act
        var result = await _manager.SkipQuestionAsync(TestUserId, isRequired: true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CompleteSurvey_TransitionsToComplete_Successfully()
    {
        // Arrange
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 5);

        // Act
        var result = await _manager.CompleteSurveyAsync(TestUserId);
        var state = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.True(result);
        Assert.Equal(ConversationStateType.ResponseComplete, state.CurrentState);
    }

    [Fact]
    public async Task CancelSurvey_TransitionsAndClearState_Successfully()
    {
        // Arrange
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 5);

        // Act
        var result = await _manager.CancelSurveyAsync(TestUserId);
        var state = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.True(result);
        Assert.Equal(ConversationStateType.Cancelled, state.CurrentState);
        Assert.Null(state.CurrentSurveyId);
        Assert.Null(state.CurrentResponseId);
    }

    #endregion

    #region Utility Tests

    [Fact]
    public async Task GetProgressPercent_CalculatesCorrectly()
    {
        // Arrange
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 10);
        var state = await _manager.GetStateAsync(TestUserId);
        // Mark 3 questions answered
        state.MarkQuestionAnswered(0);
        state.MarkQuestionAnswered(1);
        state.MarkQuestionAnswered(2);
        await _manager.SetStateAsync(TestUserId, state);

        // Act
        var progress = await _manager.GetProgressPercentAsync(TestUserId);

        // Assert
        Assert.Equal(30f, progress);
    }

    [Fact]
    public async Task GetAnsweredCount_ReturnsCorrectCount()
    {
        // Arrange
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 5);
        await _manager.AnswerQuestionAsync(TestUserId, 0, "{}");
        await _manager.AnswerQuestionAsync(TestUserId, 1, "{}");

        // Act
        var count = await _manager.GetAnsweredCountAsync(TestUserId);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task IsAllAnswered_ReturnsTrue_WhenAllAnswered()
    {
        // Arrange
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 2);
        var state = await _manager.GetStateAsync(TestUserId);
        state.MarkQuestionAnswered(0);
        state.MarkQuestionAnswered(1);
        state.CurrentQuestionIndex = 1; // At last question
        await _manager.SetStateAsync(TestUserId, state);

        // Act
        var result = await _manager.IsAllAnsweredAsync(TestUserId);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ConcurrentUpdates_AreThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var state = new ConversationState { UserId = TestUserId };
                await _manager.SetStateAsync(TestUserId, state);
            }));
        }

        // Act
        await Task.WhenAll(tasks);
        var final = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.NotNull(final);
        Assert.Equal(TestUserId, final.UserId);
    }

    [Fact]
    public async Task ConcurrentStateTransitions_AreThreadSafe()
    {
        // Arrange
        await _manager.StartSurveyAsync(TestUserId, 1, 100, 10);

        var tasks = new List<Task>();
        for (int i = 0; i < 50; i++)
        {
            if (i % 2 == 0)
                tasks.Add(_manager.NextQuestionAsync(TestUserId));
            else
                tasks.Add(_manager.AnswerQuestionAsync(TestUserId, i / 2, "{}"));
        }

        // Act
        await Task.WhenAll(tasks);
        var state = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.NotNull(state);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task StateHistoryTracking_RecordsTransitions()
    {
        // Arrange
        var state = new ConversationState { UserId = TestUserId, CurrentState = ConversationStateType.Idle };
        await _manager.SetStateAsync(TestUserId, state);

        // Act
        await _manager.TryTransitionAsync(TestUserId, ConversationStateType.WaitingSurveySelection);
        var updated = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.Equal(ConversationStateType.WaitingSurveySelection, updated.CurrentState);
        Assert.NotEmpty(updated.StateHistory);
    }

    [Fact]
    public async Task ClearSurveyData_OnlyRemovesSurveyContext()
    {
        // Arrange
        var state = new ConversationState { UserId = TestUserId };
        state.CurrentSurveyId = 1;
        state.CurrentResponseId = 100;
        state.Metadata["test"] = "value";
        await _manager.SetStateAsync(TestUserId, state);

        // Act
        var updated = await _manager.GetStateAsync(TestUserId);
        updated.ClearSurveyData();
        await _manager.SetStateAsync(TestUserId, updated);

        var result = await _manager.GetStateAsync(TestUserId);

        // Assert
        Assert.Null(result.CurrentSurveyId);
        Assert.Null(result.CurrentResponseId);
    }

    #endregion
}
