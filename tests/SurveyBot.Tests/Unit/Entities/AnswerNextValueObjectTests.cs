using SurveyBot.Core.Entities;
using SurveyBot.Core.Enums;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Tests.Fixtures;
using Xunit;

namespace SurveyBot.Tests.Unit.Entities;

/// <summary>
/// Unit tests for Answer entity's Next value object property.
/// Tests the migration from int? NextQuestionId to NextQuestionDeterminant Next.
/// </summary>
public class AnswerNextValueObjectTests
{
    #region Answer Creation Tests

    [Fact]
    public void Answer_DefaultNext_ShouldBeEndSurvey()
    {
        // Arrange & Act
        var answer = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 1,
            answerText: "Test answer");

        // Assert
        Assert.NotNull(answer.Next);
        Assert.Equal(NextStepType.EndSurvey, answer.Next.Type);
        Assert.Null(answer.Next.NextQuestionId);
    }

    [Fact]
    public void Answer_WithEndSurveyNext_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var answer = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 1,
            answerText: "Test answer",
            next: NextQuestionDeterminant.End());

        // Assert
        Assert.NotNull(answer.Next);
        Assert.Equal(NextStepType.EndSurvey, answer.Next.Type);
        Assert.Null(answer.Next.NextQuestionId);
    }

    [Fact]
    public void Answer_WithGoToQuestionNext_ShouldHaveCorrectType()
    {
        // Arrange
        var nextQuestionId = 5;

        // Act
        var answer = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 1,
            answerText: "Test answer",
            next: NextQuestionDeterminant.ToQuestion(nextQuestionId));

        // Assert
        Assert.NotNull(answer.Next);
        Assert.Equal(NextStepType.GoToQuestion, answer.Next.Type);
        Assert.Equal(nextQuestionId, answer.Next.NextQuestionId);
    }

    #endregion

    #region Next Property Assignment Tests

    [Fact]
    public void Answer_AssignEndSurvey_ShouldUpdateNext()
    {
        // Arrange
        var answer = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 1,
            answerText: "Test answer",
            next: NextQuestionDeterminant.ToQuestion(5)); // Initially go to question 5

        // Act - Change to end survey
        answer.SetNext(NextQuestionDeterminant.End());

        // Assert
        Assert.NotNull(answer.Next);
        Assert.Equal(NextStepType.EndSurvey, answer.Next.Type);
        Assert.Null(answer.Next.NextQuestionId);
    }

    [Fact]
    public void Answer_AssignGoToQuestion_ShouldUpdateNext()
    {
        // Arrange
        var answer = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 1,
            answerText: "Test answer",
            next: NextQuestionDeterminant.End()); // Initially end survey

        // Act - Change to go to question 10
        answer.SetNext(NextQuestionDeterminant.ToQuestion(10));

        // Assert
        Assert.NotNull(answer.Next);
        Assert.Equal(NextStepType.GoToQuestion, answer.Next.Type);
        Assert.Equal(10, answer.Next.NextQuestionId);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Answer_WithSameNextValue_ShouldBeEqual()
    {
        // Arrange
        var answer1 = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 1,
            next: NextQuestionDeterminant.ToQuestion(5));

        var answer2 = EntityBuilder.CreateAnswer(
            responseId: 2,
            questionId: 2,
            next: NextQuestionDeterminant.ToQuestion(5));

        // Act & Assert - Next values should be equal (value equality)
        Assert.Equal(answer1.Next, answer2.Next);
    }

    [Fact]
    public void Answer_WithDifferentNextValue_ShouldNotBeEqual()
    {
        // Arrange
        var answer1 = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 1,
            next: NextQuestionDeterminant.ToQuestion(5));

        var answer2 = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 1,
            next: NextQuestionDeterminant.ToQuestion(10));

        // Act & Assert - Next values should not be equal
        Assert.NotEqual(answer1.Next, answer2.Next);
    }

    [Fact]
    public void Answer_EndSurveyVsGoToQuestion_ShouldNotBeEqual()
    {
        // Arrange
        var answer1 = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 1,
            next: NextQuestionDeterminant.End());

        var answer2 = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 1,
            next: NextQuestionDeterminant.ToQuestion(5));

        // Act & Assert - Different types should not be equal
        Assert.NotEqual(answer1.Next, answer2.Next);
    }

    #endregion

    #region Integration with Question Types Tests

    [Fact]
    public void Answer_ForTextQuestion_ShouldUseDefaultNext()
    {
        // Arrange - Text question (non-branching)
        var answer = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 1,
            answerText: "User typed this",
            next: NextQuestionDeterminant.ToQuestion(2)); // Determined by Question.DefaultNextQuestionId

        // Assert
        Assert.Equal(NextStepType.GoToQuestion, answer.Next.Type);
        Assert.Equal(2, answer.Next.NextQuestionId);
    }

    [Fact]
    public void Answer_ForSingleChoiceQuestion_ShouldUseBranchingLogic()
    {
        // Arrange - Single choice question (branching)
        var answer = Answer.CreateJsonAnswer(
            responseId: 1,
            questionId: 1,
            answerJson: "[\"Option A\"]",
            next: NextQuestionDeterminant.ToQuestion(3)); // Determined by QuestionOption.NextQuestionDeterminant

        // Assert
        Assert.Equal(NextStepType.GoToQuestion, answer.Next.Type);
        Assert.Equal(3, answer.Next.NextQuestionId);
    }

    [Fact]
    public void Answer_ForLastQuestion_ShouldEndSurvey()
    {
        // Arrange - Last question in survey
        var answer = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 10,
            answerText: "Final answer",
            next: NextQuestionDeterminant.End());

        // Assert
        Assert.Equal(NextStepType.EndSurvey, answer.Next.Type);
        Assert.Null(answer.Next.NextQuestionId);
    }

    #endregion

    #region Boundary Tests

    [Fact]
    public void Answer_NextToQuestionOne_ShouldWork()
    {
        // Arrange & Act - Go to first question (valid edge case)
        var answer = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 5,
            next: NextQuestionDeterminant.ToQuestion(1));

        // Assert
        Assert.Equal(NextStepType.GoToQuestion, answer.Next.Type);
        Assert.Equal(1, answer.Next.NextQuestionId);
    }

    [Fact]
    public void Answer_NextToHighQuestionId_ShouldWork()
    {
        // Arrange & Act - Go to question with high ID
        var answer = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 1,
            next: NextQuestionDeterminant.ToQuestion(999999));

        // Assert
        Assert.Equal(NextStepType.GoToQuestion, answer.Next.Type);
        Assert.Equal(999999, answer.Next.NextQuestionId);
    }

    #endregion

    #region Migration Validation Tests

    [Fact]
    public void Answer_NoLongerHasNextQuestionIdProperty()
    {
        // Arrange
        var answer = EntityBuilder.CreateAnswer();

        // Assert - Verify old property doesn't exist
        var property = answer.GetType().GetProperty("NextQuestionId");
        Assert.Null(property); // Property should not exist
    }

    [Fact]
    public void Answer_HasNextProperty()
    {
        // Arrange
        var answer = EntityBuilder.CreateAnswer();

        // Assert - Verify new property exists
        var property = answer.GetType().GetProperty("Next");
        Assert.NotNull(property);
        Assert.Equal(typeof(NextQuestionDeterminant), property.PropertyType);
    }

    [Fact]
    public void Answer_NextPropertyNotNullable()
    {
        // Arrange & Act
        var answer = EntityBuilder.CreateAnswer();

        // Assert - Next should always have a value (not nullable)
        Assert.NotNull(answer.Next);
    }

    #endregion

    #region Multiple Answer Scenarios Tests

    [Fact]
    public void MultipleAnswers_WithDifferentNext_ShouldMaintainIndependence()
    {
        // Arrange
        var answer1 = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 1,
            next: NextQuestionDeterminant.ToQuestion(2));

        var answer2 = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 2,
            next: NextQuestionDeterminant.ToQuestion(5));

        var answer3 = EntityBuilder.CreateAnswer(
            responseId: 1,
            questionId: 5,
            next: NextQuestionDeterminant.End());

        // Assert - Each answer maintains its own Next value
        Assert.Equal(2, answer1.Next.NextQuestionId);
        Assert.Equal(5, answer2.Next.NextQuestionId);
        Assert.Equal(NextStepType.EndSurvey, answer3.Next.Type);
    }

    [Fact]
    public void MultipleAnswers_InResponse_CanHaveDifferentFlows()
    {
        // Arrange - Simulate different users taking different paths
        var userAAnswer = Answer.CreateJsonAnswer(
            responseId: 1,
            questionId: 1,
            answerJson: "[\"Option A\"]", // User A chose Option A
            next: NextQuestionDeterminant.ToQuestion(3)); // Leads to Q3

        var userBAnswer = Answer.CreateJsonAnswer(
            responseId: 2,
            questionId: 1,
            answerJson: "[\"Option B\"]", // User B chose Option B
            next: NextQuestionDeterminant.ToQuestion(5)); // Leads to Q5

        // Assert - Different answers to same question can have different next steps
        Assert.Equal(3, userAAnswer.Next.NextQuestionId);
        Assert.Equal(5, userBAnswer.Next.NextQuestionId);
    }

    #endregion
}
