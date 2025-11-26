using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Enums;
using SurveyBot.Core.Interfaces;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Infrastructure.Services;
using Xunit;

namespace SurveyBot.Tests.Unit.Services;

/// <summary>
/// Unit tests for SurveyValidationService - cycle detection and survey structure validation.
/// Tests DFS-based cycle detection algorithm and endpoint validation.
/// </summary>
public class SurveyValidationServiceTests
{
    private readonly Mock<IQuestionRepository> _questionRepositoryMock;
    private readonly Mock<ILogger<SurveyValidationService>> _loggerMock;
    private readonly SurveyValidationService _sut;

    public SurveyValidationServiceTests()
    {
        _questionRepositoryMock = new Mock<IQuestionRepository>();
        _loggerMock = new Mock<ILogger<SurveyValidationService>>();

        _sut = new SurveyValidationService(
            _questionRepositoryMock.Object,
            _loggerMock.Object);
    }

    #region Cycle Detection Tests

    [Fact]
    public async Task DetectCycleAsync_LinearFlow_ReturnsNoCycle()
    {
        // Arrange - Q1 → Q2 → Q3 → End
        var surveyId = 1;
        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(2)),
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(3)),
            CreateQuestion(3, surveyId, QuestionType.Text, NextQuestionDeterminant.End())
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasCycle);
        Assert.Null(result.CyclePath);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task DetectCycleAsync_BranchingFlow_ReturnsNoCycle()
    {
        // Arrange - Q1 → (Option A → Q2, Option B → Q3), Q2 → End, Q3 → End
        var surveyId = 1;
        var q1 = CreateQuestion(1, surveyId, QuestionType.SingleChoice);
        q1.Options = new List<QuestionOption>
        {
            new() { Id = 1, QuestionId = 1, Text = "Option A", OrderIndex = 0, Next = NextQuestionDeterminant.ToQuestion(2) },
            new() { Id = 2, QuestionId = 1, Text = "Option B", OrderIndex = 1, Next = NextQuestionDeterminant.ToQuestion(3) }
        };

        var questions = new List<Question>
        {
            q1,
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.End()),
            CreateQuestion(3, surveyId, QuestionType.Text, NextQuestionDeterminant.End())
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert
        Assert.False(result.HasCycle);
        Assert.Null(result.CyclePath);
    }

    [Fact]
    public async Task DetectCycleAsync_SelfCycle_DetectsCycle()
    {
        // Arrange - Q1 → Q1 (points to itself)
        var surveyId = 1;
        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(1))
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert
        Assert.True(result.HasCycle);
        Assert.NotNull(result.CyclePath);
        Assert.Contains(1, result.CyclePath);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Cycle detected", result.ErrorMessage);
    }

    [Fact]
    public async Task DetectCycleAsync_TwoNodeCycle_DetectsCycle()
    {
        // Arrange - Q1 → Q2 → Q1
        var surveyId = 1;
        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(2)),
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(1))
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert
        Assert.True(result.HasCycle);
        Assert.NotNull(result.CyclePath);
        Assert.True(result.CyclePath.Count >= 2);
        Assert.Contains(1, result.CyclePath);
        Assert.Contains(2, result.CyclePath);
    }

    [Fact]
    public async Task DetectCycleAsync_ThreeNodeCycle_DetectsCycle()
    {
        // Arrange - Q1 → Q2 → Q3 → Q1
        var surveyId = 1;
        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(2)),
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(3)),
            CreateQuestion(3, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(1))
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert
        Assert.True(result.HasCycle);
        Assert.NotNull(result.CyclePath);
        Assert.True(result.CyclePath.Count >= 3);
        Assert.Contains(1, result.CyclePath);
        Assert.Contains(2, result.CyclePath);
        Assert.Contains(3, result.CyclePath);
    }

    [Fact]
    public async Task DetectCycleAsync_CycleWithMultiplePaths_DetectsCycle()
    {
        // Arrange - Q1 → Q2 → Q3, Q2 → Q4 → Q1 (cycle: Q1 → Q2 → Q4 → Q1)
        var surveyId = 1;

        var q2 = CreateQuestion(2, surveyId, QuestionType.SingleChoice);
        q2.Options = new List<QuestionOption>
        {
            new() { Id = 1, QuestionId = 2, Text = "Option A", OrderIndex = 0, Next = NextQuestionDeterminant.ToQuestion(3) },
            new() { Id = 2, QuestionId = 2, Text = "Option B", OrderIndex = 1, Next = NextQuestionDeterminant.ToQuestion(4) }
        };

        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(2)),
            q2,
            CreateQuestion(3, surveyId, QuestionType.Text, NextQuestionDeterminant.End()),
            CreateQuestion(4, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(1))
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert
        Assert.True(result.HasCycle);
        Assert.NotNull(result.CyclePath);
        // The cycle should involve Q1, Q2, Q4
        Assert.Contains(1, result.CyclePath);
        Assert.Contains(4, result.CyclePath);
    }

    [Fact]
    public async Task DetectCycleAsync_OrphanedQuestion_ReturnsNoCycle()
    {
        // Arrange - Q1 → Q2 → End, Q3 orphaned (not reachable)
        var surveyId = 1;
        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(2)),
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.End()),
            CreateQuestion(3, surveyId, QuestionType.Text, NextQuestionDeterminant.End()) // Orphaned
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert
        Assert.False(result.HasCycle);
        Assert.Null(result.CyclePath);
    }

    [Fact]
    public async Task DetectCycleAsync_MultipleSeparateCycles_DetectsFirstCycle()
    {
        // Arrange - Q1 → Q2 → Q1 (cycle 1), Q3 → Q4 → Q3 (cycle 2)
        var surveyId = 1;
        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(2)),
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(1)),
            CreateQuestion(3, surveyId, QuestionType.Text, defaultNext: 4),
            CreateQuestion(4, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(3))
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert
        Assert.True(result.HasCycle);
        Assert.NotNull(result.CyclePath);
        // Should detect at least one cycle
        Assert.True(result.CyclePath.Count >= 2);
    }

    [Fact]
    public async Task DetectCycleAsync_EmptySurvey_ReturnsNoCycle()
    {
        // Arrange
        var surveyId = 1;
        var questions = new List<Question>();

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert
        Assert.False(result.HasCycle);
        Assert.Null(result.CyclePath);
    }

    #endregion

    #region Survey Structure Validation Tests

    [Fact]
    public async Task ValidateSurveyStructureAsync_LinearFlowWithEndpoint_ReturnsTrue()
    {
        // Arrange - Valid linear flow: Q1 → Q2 → Q3 → End
        var surveyId = 1;
        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(2)),
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(3)),
            CreateQuestion(3, surveyId, QuestionType.Text, NextQuestionDeterminant.End())
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.ValidateSurveyStructureAsync(surveyId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateSurveyStructureAsync_BranchingFlowWithEndpoints_ReturnsTrue()
    {
        // Arrange - Valid branching flow with all paths leading to endpoints
        var surveyId = 1;

        var q1 = CreateQuestion(1, surveyId, QuestionType.SingleChoice);
        q1.Options = new List<QuestionOption>
        {
            new() { Id = 1, QuestionId = 1, Text = "Option A", OrderIndex = 0, Next = NextQuestionDeterminant.ToQuestion(2) },
            new() { Id = 2, QuestionId = 1, Text = "Option B", OrderIndex = 1, Next = NextQuestionDeterminant.End() }
        };

        var questions = new List<Question>
        {
            q1,
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.End())
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.ValidateSurveyStructureAsync(surveyId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateSurveyStructureAsync_HasCycle_ReturnsFalse()
    {
        // Arrange - Invalid: Q1 → Q2 → Q1 (cycle)
        var surveyId = 1;
        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(2)),
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(1))
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.ValidateSurveyStructureAsync(surveyId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSurveyStructureAsync_NoEndpoints_ReturnsFalse()
    {
        // Arrange - Invalid: Q1 → Q2 (no endpoint, Q2 doesn't point to end)
        var surveyId = 1;
        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(2)),
            CreateQuestion(2, surveyId, QuestionType.Text, null) // No next question, but not explicitly end
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.ValidateSurveyStructureAsync(surveyId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSurveyStructureAsync_SomeBranchesDeadEnd_ReturnsFalse()
    {
        // Arrange - Invalid: Branching question where one branch doesn't reach endpoint
        var surveyId = 1;

        var q1 = CreateQuestion(1, surveyId, QuestionType.SingleChoice);
        q1.Options = new List<QuestionOption>
        {
            new() { Id = 1, QuestionId = 1, Text = "Option A", OrderIndex = 0, Next = NextQuestionDeterminant.End() },
            new() { Id = 2, QuestionId = 1, Text = "Option B", OrderIndex = 1, Next = NextQuestionDeterminant.ToQuestion(2) }
        };

        var questions = new List<Question>
        {
            q1,
            CreateQuestion(2, surveyId, QuestionType.Text, null) // Dead end
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.ValidateSurveyStructureAsync(surveyId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSurveyStructureAsync_EmptySurvey_ReturnsFalse()
    {
        // Arrange
        var surveyId = 1;
        var questions = new List<Question>();

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.ValidateSurveyStructureAsync(surveyId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Endpoint Finding Tests

    [Fact]
    public async Task FindSurveyEndpointsAsync_SingleEndpoint_ReturnsEndpoint()
    {
        // Arrange - Q1 → Q2 → Q3 → End (Q3 is endpoint)
        var surveyId = 1;
        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(2)),
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(3)),
            CreateQuestion(3, surveyId, QuestionType.Text, NextQuestionDeterminant.End())
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.FindSurveyEndpointsAsync(surveyId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(3, result);
    }

    [Fact]
    public async Task FindSurveyEndpointsAsync_MultipleEndpoints_ReturnsAllEndpoints()
    {
        // Arrange - Q1 → End, Q2 → End (both are endpoints)
        var surveyId = 1;
        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.End()),
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.End())
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.FindSurveyEndpointsAsync(surveyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(1, result);
        Assert.Contains(2, result);
    }

    [Fact]
    public async Task FindSurveyEndpointsAsync_NoEndpoints_ReturnsEmptyList()
    {
        // Arrange - Q1 → Q2 (no endpoints)
        var surveyId = 1;
        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(2)),
            CreateQuestion(2, surveyId, QuestionType.Text, null)
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.FindSurveyEndpointsAsync(surveyId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FindSurveyEndpointsAsync_BranchingWithEndpointOptions_ReturnsEndpoint()
    {
        // Arrange - Q1 with options pointing to End and Q2, Q2 → End (both Q1 and Q2 are endpoints)
        var surveyId = 1;

        var q1 = CreateQuestion(1, surveyId, QuestionType.SingleChoice);
        q1.Options = new List<QuestionOption>
        {
            new() { Id = 1, QuestionId = 1, Text = "Option A", OrderIndex = 0, Next = NextQuestionDeterminant.End() },
            new() { Id = 2, QuestionId = 1, Text = "Option B", OrderIndex = 1, Next = NextQuestionDeterminant.ToQuestion(2) }
        };

        var questions = new List<Question>
        {
            q1,
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.End())
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.FindSurveyEndpointsAsync(surveyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(1, result); // Q1 has option pointing to end
        Assert.Contains(2, result); // Q2 points to end
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task DetectCycleAsync_LargeSurvey100Questions_HandlesEfficiently()
    {
        // Arrange - Create linear flow with 100 questions
        var surveyId = 1;
        var questions = new List<Question>();

        for (int i = 1; i <= 100; i++)
        {
            var nextId = i < 100 ? i + 1 : SurveyConstants.EndOfSurveyMarker;
            questions.Add(CreateQuestion(i, surveyId, QuestionType.Text, defaultNext: nextId));
        }

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert
        Assert.False(result.HasCycle);
        Assert.Null(result.CyclePath);
    }

    [Fact]
    public async Task DetectCycleAsync_DeeplyNestedBranching_HandlesCorrectly()
    {
        // Arrange - 10 levels of branching, all paths lead to end
        var surveyId = 1;
        var questions = new List<Question>();
        int questionId = 1;

        // Create 10 levels of branching
        for (int level = 0; level < 10; level++)
        {
            var q = CreateQuestion(questionId++, surveyId, QuestionType.SingleChoice);

            // Each question branches to next level or end
            var nextQuestionId = questionId;
            q.Options = new List<QuestionOption>
            {
                new() { Id = questionId * 10, QuestionId = q.Id, Text = "Option A", OrderIndex = 0,
                       Next = level < 9 ? NextQuestionDeterminant.ToQuestion(nextQuestionId) : NextQuestionDeterminant.End() },
                new() { Id = questionId * 10 + 1, QuestionId = q.Id, Text = "Option B", OrderIndex = 1,
                       Next = NextQuestionDeterminant.End() }
            };

            questions.Add(q);
        }

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert
        Assert.False(result.HasCycle);
    }

    [Fact]
    public async Task DetectCycleAsync_AllOptionsPointToSameQuestion_NoCycleIfValid()
    {
        // Arrange - Q1 with all options pointing to Q2, Q2 → End
        var surveyId = 1;

        var q1 = CreateQuestion(1, surveyId, QuestionType.SingleChoice);
        q1.Options = new List<QuestionOption>
        {
            new() { Id = 1, QuestionId = 1, Text = "Option A", OrderIndex = 0, Next = NextQuestionDeterminant.ToQuestion(2) },
            new() { Id = 2, QuestionId = 1, Text = "Option B", OrderIndex = 1, Next = NextQuestionDeterminant.ToQuestion(2) },
            new() { Id = 3, QuestionId = 1, Text = "Option C", OrderIndex = 2, Next = NextQuestionDeterminant.ToQuestion(2) }
        };

        var questions = new List<Question>
        {
            q1,
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.End())
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert
        Assert.False(result.HasCycle);
    }

    [Fact]
    public async Task DetectCycleAsync_MixedBranchingAndNonBranching_HandlesCorrectly()
    {
        // Arrange - Q1 (branching) → Q2 (non-branching) → Q3 (branching) → End
        var surveyId = 1;

        var q1 = CreateQuestion(1, surveyId, QuestionType.SingleChoice);
        q1.Options = new List<QuestionOption>
        {
            new() { Id = 1, QuestionId = 1, Text = "Option A", OrderIndex = 0, Next = NextQuestionDeterminant.ToQuestion(2) },
            new() { Id = 2, QuestionId = 1, Text = "Option B", OrderIndex = 1, Next = NextQuestionDeterminant.ToQuestion(3) }
        };

        var q3 = CreateQuestion(3, surveyId, QuestionType.Rating);
        q3.Options = new List<QuestionOption>
        {
            new() { Id = 3, QuestionId = 3, Text = "1", OrderIndex = 0, Next = NextQuestionDeterminant.End() },
            new() { Id = 4, QuestionId = 3, Text = "5", OrderIndex = 1, Next = NextQuestionDeterminant.End() }
        };

        var questions = new List<Question>
        {
            q1,
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(3)),
            q3
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert
        Assert.False(result.HasCycle);
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public async Task GetNextQuestionIds_BranchingQuestion_ReturnsAllOptionNextIds()
    {
        // Arrange - SingleChoice question with 3 options pointing to different questions
        var surveyId = 1;

        var q1 = CreateQuestion(1, surveyId, QuestionType.SingleChoice);
        q1.Options = new List<QuestionOption>
        {
            new() { Id = 1, QuestionId = 1, Text = "Option A", OrderIndex = 0, Next = NextQuestionDeterminant.ToQuestion(2) },
            new() { Id = 2, QuestionId = 1, Text = "Option B", OrderIndex = 1, Next = NextQuestionDeterminant.ToQuestion(3) },
            new() { Id = 3, QuestionId = 1, Text = "Option C", OrderIndex = 2, Next = NextQuestionDeterminant.ToQuestion(4) }
        };

        var questions = new List<Question>
        {
            q1,
            CreateQuestion(2, surveyId, QuestionType.Text, NextQuestionDeterminant.End()),
            CreateQuestion(3, surveyId, QuestionType.Text, NextQuestionDeterminant.End()),
            CreateQuestion(4, surveyId, QuestionType.Text, NextQuestionDeterminant.End())
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert - Should not detect cycle (implicitly tests GetNextQuestionIds)
        Assert.False(result.HasCycle);
    }

    [Fact]
    public async Task GetNextQuestionIds_NonBranchingQuestion_ReturnsDefaultNextId()
    {
        // Arrange - Text question with default next ID
        var surveyId = 1;
        var questions = new List<Question>
        {
            CreateQuestion(1, surveyId, QuestionType.Text, NextQuestionDeterminant.ToQuestion(2)),
            CreateQuestion(2, surveyId, QuestionType.MultipleChoice, NextQuestionDeterminant.End())
        };

        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert - Should not detect cycle (implicitly tests GetNextQuestionIds)
        Assert.False(result.HasCycle);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task DetectCycleAsync_RepositoryThrowsException_ReturnsCycleDetectedForSafety()
    {
        // Arrange
        var surveyId = 1;
        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.DetectCycleAsync(surveyId);

        // Assert - Should return HasCycle = true for safety
        Assert.True(result.HasCycle);
        Assert.Null(result.CyclePath);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Error during cycle detection", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateSurveyStructureAsync_RepositoryThrowsException_ReturnsFalseForSafety()
    {
        // Arrange
        var surveyId = 1;
        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.ValidateSurveyStructureAsync(surveyId);

        // Assert - Should return false for safety
        Assert.False(result);
    }

    [Fact]
    public async Task FindSurveyEndpointsAsync_RepositoryThrowsException_ReturnsEmptyList()
    {
        // Arrange
        var surveyId = 1;
        _questionRepositoryMock
            .Setup(r => r.GetWithFlowConfigurationAsync(surveyId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.FindSurveyEndpointsAsync(surveyId);

        // Assert - Should return empty list for safety
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test question with flow configuration.
    /// </summary>
    private Question CreateQuestion(
        int id,
        int surveyId,
        QuestionType questionType,
        NextQuestionDeterminant? defaultNext = null)
    {
        return new Question
        {
            Id = id,
            SurveyId = surveyId,
            QuestionText = $"Question {id}?",
            Type = questionType,
            OrderIndex = id - 1,
            IsRequired = true,
            DefaultNext = defaultNext,
            Options = new List<QuestionOption>()
        };
    }

    #endregion
}
