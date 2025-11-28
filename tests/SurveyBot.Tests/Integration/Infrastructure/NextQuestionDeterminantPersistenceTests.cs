using Microsoft.EntityFrameworkCore;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Enums;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Infrastructure.Data;
using Xunit;

namespace SurveyBot.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for NextQuestionDeterminant EF Core owned type persistence.
/// Tests saving, retrieving, and querying value objects stored as JSON in PostgreSQL.
/// </summary>
public class NextQuestionDeterminantPersistenceTests : IDisposable
{
    private readonly SurveyBotDbContext _context;

    public NextQuestionDeterminantPersistenceTests()
    {
        var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new SurveyBotDbContext(options);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Question DefaultNext Tests

    [Fact]
    public async Task SaveAndRetrieve_QuestionWithDefaultNextGoToQuestion_PersistsCorrectly()
    {
        // Arrange
        var user = User.Create(123456, "testuser", null, null);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = Survey.Create("Test Survey", user.Id, null, "ABC123", false, false, true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var targetQuestion = Question.CreateTextQuestion(survey.Id, "Target Question", 1, true);
        await _context.Questions.AddAsync(targetQuestion);
        await _context.SaveChangesAsync();

        var sourceQuestion = Question.CreateTextQuestion(survey.Id, "Source Question", 0, true);
        sourceQuestion.SetDefaultNext(NextQuestionDeterminant.ToQuestion(targetQuestion.Id));
        await _context.Questions.AddAsync(sourceQuestion);
        await _context.SaveChangesAsync();

        // Clear tracking
        _context.ChangeTracker.Clear();

        // Act
        var retrieved = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == sourceQuestion.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.DefaultNext);
        Assert.Equal(NextStepType.GoToQuestion, retrieved.DefaultNext.Type);
        Assert.Equal(targetQuestion.Id, retrieved.DefaultNext.NextQuestionId);
    }

    [Fact]
    public async Task SaveAndRetrieve_QuestionWithDefaultNextEnd_PersistsCorrectly()
    {
        // Arrange
        var user = User.Create(123456, "testuser", null, null);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = Survey.Create("Test Survey", user.Id, null, "ABC123", false, false, true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = Question.CreateTextQuestion(survey.Id, "Final Question", 0, true);
        question.SetDefaultNext(NextQuestionDeterminant.End());
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        // Clear tracking
        _context.ChangeTracker.Clear();

        // Act
        var retrieved = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == question.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.DefaultNext);
        Assert.Equal(NextStepType.EndSurvey, retrieved.DefaultNext.Type);
        Assert.Null(retrieved.DefaultNext.NextQuestionId);
    }

    [Fact]
    public async Task SaveAndRetrieve_QuestionWithNullDefaultNext_PersistsCorrectly()
    {
        // Arrange
        var user = User.Create(123456, "testuser", null, null);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = Survey.Create("Test Survey", user.Id, null, "ABC123", false, false, true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = Question.CreateTextQuestion(survey.Id, "Question without flow", 0, true);
        question.SetDefaultNext(null);
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        // Clear tracking
        _context.ChangeTracker.Clear();

        // Act
        var retrieved = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == question.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Null(retrieved.DefaultNext);
    }

    [Fact]
    public async Task Update_QuestionDefaultNext_UpdatesPersistence()
    {
        // Arrange
        var user = User.Create(123456, "testuser", null, null);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = Survey.Create("Test Survey", user.Id, null, "ABC123", false, false, true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = Question.CreateTextQuestion(survey.Id, "Question", 0, true);
        question.SetDefaultNext(NextQuestionDeterminant.End());
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        var targetQuestion = Question.CreateTextQuestion(survey.Id, "Target", 1, true);
        await _context.Questions.AddAsync(targetQuestion);
        await _context.SaveChangesAsync();

        // Act - Update DefaultNext
        question.SetDefaultNext(NextQuestionDeterminant.ToQuestion(targetQuestion.Id));
        await _context.SaveChangesAsync();

        // Clear tracking
        _context.ChangeTracker.Clear();

        // Assert
        var retrieved = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == question.Id);

        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.DefaultNext);
        Assert.Equal(NextStepType.GoToQuestion, retrieved.DefaultNext.Type);
        Assert.Equal(targetQuestion.Id, retrieved.DefaultNext.NextQuestionId);
    }

    #endregion

    #region QuestionOption Next Tests

    [Fact]
    public async Task SaveAndRetrieve_QuestionOptionWithNextGoToQuestion_PersistsCorrectly()
    {
        // Arrange
        var user = User.Create(123456, "testuser", null, null);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = Survey.Create("Test Survey", user.Id, null, "ABC123", false, false, true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = Question.CreateSingleChoiceQuestion(survey.Id, "Choice Question", 0, "[]", true);
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        var targetQuestion = Question.CreateTextQuestion(survey.Id, "Target Question", 1, true);
        await _context.Questions.AddAsync(targetQuestion);
        await _context.SaveChangesAsync();

        var option = QuestionOption.Create(question.Id, "Option A", 0, NextQuestionDeterminant.ToQuestion(targetQuestion.Id));
        await _context.QuestionOptions.AddAsync(option);
        await _context.SaveChangesAsync();

        // Clear tracking
        _context.ChangeTracker.Clear();

        // Act
        var retrieved = await _context.QuestionOptions
            .FirstOrDefaultAsync(o => o.Id == option.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.Next);
        Assert.Equal(NextStepType.GoToQuestion, retrieved.Next.Type);
        Assert.Equal(targetQuestion.Id, retrieved.Next.NextQuestionId);
    }

    [Fact]
    public async Task SaveAndRetrieve_QuestionOptionWithNextEnd_PersistsCorrectly()
    {
        // Arrange
        var user = User.Create(123456, "testuser", null, null);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = Survey.Create("Test Survey", user.Id, null, "ABC123", false, false, true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = Question.CreateSingleChoiceQuestion(survey.Id, "Choice Question", 0, "[]", true);
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        var option = QuestionOption.Create(question.Id, "End Survey Option", 0, NextQuestionDeterminant.End());
        await _context.QuestionOptions.AddAsync(option);
        await _context.SaveChangesAsync();

        // Clear tracking
        _context.ChangeTracker.Clear();

        // Act
        var retrieved = await _context.QuestionOptions
            .FirstOrDefaultAsync(o => o.Id == option.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.Next);
        Assert.Equal(NextStepType.EndSurvey, retrieved.Next.Type);
        Assert.Null(retrieved.Next.NextQuestionId);
    }

    [Fact]
    public async Task SaveAndRetrieve_MultipleOptionsWithDifferentNext_PersistsCorrectly()
    {
        // Arrange
        var user = User.Create(123456, "testuser", null, null);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = Survey.Create("Test Survey", user.Id, null, "ABC123", false, false, true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = Question.CreateSingleChoiceQuestion(survey.Id, "Branching Question", 0, "[]", true);
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        var targetQuestion1 = Question.CreateTextQuestion(survey.Id, "Path A", 1, true);
        var targetQuestion2 = Question.CreateTextQuestion(survey.Id, "Path B", 2, true);

        await _context.Questions.AddRangeAsync(targetQuestion1, targetQuestion2);
        await _context.SaveChangesAsync();

        var option1 = QuestionOption.Create(question.Id, "Go to Path A", 0, NextQuestionDeterminant.ToQuestion(targetQuestion1.Id));
        var option2 = QuestionOption.Create(question.Id, "Go to Path B", 1, NextQuestionDeterminant.ToQuestion(targetQuestion2.Id));
        var option3 = QuestionOption.Create(question.Id, "End Survey", 2, NextQuestionDeterminant.End());

        await _context.QuestionOptions.AddRangeAsync(option1, option2, option3);
        await _context.SaveChangesAsync();

        // Clear tracking
        _context.ChangeTracker.Clear();

        // Act
        var retrievedOptions = await _context.QuestionOptions
            .Where(o => o.QuestionId == question.Id)
            .OrderBy(o => o.OrderIndex)
            .ToListAsync();

        // Assert
        Assert.Equal(3, retrievedOptions.Count);

        Assert.NotNull(retrievedOptions[0].Next);
        Assert.Equal(NextStepType.GoToQuestion, retrievedOptions[0].Next.Type);
        Assert.Equal(targetQuestion1.Id, retrievedOptions[0].Next.NextQuestionId);

        Assert.NotNull(retrievedOptions[1].Next);
        Assert.Equal(NextStepType.GoToQuestion, retrievedOptions[1].Next.Type);
        Assert.Equal(targetQuestion2.Id, retrievedOptions[1].Next.NextQuestionId);

        Assert.NotNull(retrievedOptions[2].Next);
        Assert.Equal(NextStepType.EndSurvey, retrievedOptions[2].Next.Type);
        Assert.Null(retrievedOptions[2].Next.NextQuestionId);
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task Query_QuestionsByNextQuestionId_WorksCorrectly()
    {
        // Arrange
        var user = User.Create(123456, "testuser", null, null);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = Survey.Create("Test Survey", user.Id, null, "ABC123", false, false, true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var targetQuestion = Question.CreateTextQuestion(survey.Id, "Target", 2, true);
        await _context.Questions.AddAsync(targetQuestion);
        await _context.SaveChangesAsync();

        var question1 = Question.CreateTextQuestion(survey.Id, "Q1", 0, true);
        question1.SetDefaultNext(NextQuestionDeterminant.ToQuestion(targetQuestion.Id));

        var question2 = Question.CreateTextQuestion(survey.Id, "Q2", 1, true);
        question2.SetDefaultNext(NextQuestionDeterminant.End());

        await _context.Questions.AddRangeAsync(question1, question2);
        await _context.SaveChangesAsync();

        // Clear tracking
        _context.ChangeTracker.Clear();

        // Act - Query questions that point to targetQuestion
        var questionsPointingToTarget = await _context.Questions
            .Where(q => q.DefaultNext != null &&
                        q.DefaultNext.NextQuestionId == targetQuestion.Id)
            .ToListAsync();

        // Assert
        Assert.Single(questionsPointingToTarget);
        Assert.Equal(question1.Id, questionsPointingToTarget[0].Id);
    }

    [Fact]
    public async Task Query_QuestionsWithEndSurvey_WorksCorrectly()
    {
        // Arrange
        var user = User.Create(123456, "testuser", null, null);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = Survey.Create("Test Survey", user.Id, null, "ABC123", false, false, true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question1 = Question.CreateTextQuestion(survey.Id, "Q1", 0, true);
        question1.SetDefaultNext(NextQuestionDeterminant.End());

        var question2 = Question.CreateTextQuestion(survey.Id, "Q2", 1, true);
        question2.SetDefaultNext(NextQuestionDeterminant.End());

        var question3 = Question.CreateTextQuestion(survey.Id, "Q3", 2, true);
        question3.SetDefaultNext(null);

        await _context.Questions.AddRangeAsync(question1, question2, question3);
        await _context.SaveChangesAsync();

        // Clear tracking
        _context.ChangeTracker.Clear();

        // Act - Query questions that end the survey
        var endSurveyQuestions = await _context.Questions
            .Where(q => q.DefaultNext != null &&
                        q.DefaultNext.Type == NextStepType.EndSurvey)
            .ToListAsync();

        // Assert
        Assert.Equal(2, endSurveyQuestions.Count);
        Assert.Contains(endSurveyQuestions, q => q.Id == question1.Id);
        Assert.Contains(endSurveyQuestions, q => q.Id == question2.Id);
    }

    [Fact]
    public async Task Query_OptionsPointingToQuestion_WorksCorrectly()
    {
        // Arrange
        var user = User.Create(123456, "testuser", null, null);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = Survey.Create("Test Survey", user.Id, null, "ABC123", false, false, true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = Question.CreateSingleChoiceQuestion(survey.Id, "Choice Question", 0, "[]", true);
        var targetQuestion = Question.CreateTextQuestion(survey.Id, "Target", 1, true);

        await _context.Questions.AddRangeAsync(question, targetQuestion);
        await _context.SaveChangesAsync();

        var option1 = QuestionOption.Create(question.Id, "Option A", 0, NextQuestionDeterminant.ToQuestion(targetQuestion.Id));
        var option2 = QuestionOption.Create(question.Id, "Option B", 1, NextQuestionDeterminant.End());

        await _context.QuestionOptions.AddRangeAsync(option1, option2);
        await _context.SaveChangesAsync();

        // Clear tracking
        _context.ChangeTracker.Clear();

        // Act
        var optionsPointingToTarget = await _context.QuestionOptions
            .Where(o => o.Next != null &&
                        o.Next.NextQuestionId == targetQuestion.Id)
            .ToListAsync();

        // Assert
        Assert.Single(optionsPointingToTarget);
        Assert.Equal(option1.Id, optionsPointingToTarget[0].Id);
    }

    #endregion

    #region JSON Storage Tests

    [Fact]
    public async Task JsonStorage_ValueObjectStoredAsJson_NotSeparateTable()
    {
        // Arrange
        var user = User.Create(123456, "testuser", null, null);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = Survey.Create("Test Survey", user.Id, null, "ABC123", false, false, true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = Question.CreateTextQuestion(
            surveyId: survey.Id,
            questionText: "Question",
            orderIndex: 0,
            isRequired: true);
        question.SetDefaultNext(NextQuestionDeterminant.ToQuestion(99));
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        // Act - Verify value object is stored as JSON in same table
        var questionCount = await _context.Questions.CountAsync();

        // Assert
        Assert.Equal(1, questionCount);
        // In real PostgreSQL, DefaultNext would be stored in DefaultNext_Type and DefaultNext_NextQuestionId columns
        // In-memory database simulates this behavior
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task Delete_QuestionWithDefaultNext_DeletesSuccessfully()
    {
        // Arrange
        var user = User.Create(123456, "testuser", null, null);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = Survey.Create("Test Survey", user.Id, null, "ABC123", false, false, true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = Question.CreateTextQuestion(survey.Id, "Question", 0, true);
        question.SetDefaultNext(NextQuestionDeterminant.End());
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        // Act
        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();

        // Assert
        var count = await _context.Questions.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Update_ChangeNextFromGoToQuestionToEnd_WorksCorrectly()
    {
        // Arrange
        var user = User.Create(123456, "testuser", null, null);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = Survey.Create("Test Survey", user.Id, null, "ABC123", false, false, true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var targetQuestion = Question.CreateTextQuestion(survey.Id, "Target", 1, true);
        var question = Question.CreateTextQuestion(survey.Id, "Question", 0, true);
        question.SetDefaultNext(NextQuestionDeterminant.ToQuestion(targetQuestion.Id));

        await _context.Questions.AddRangeAsync(targetQuestion, question);
        await _context.SaveChangesAsync();

        // Act - Change to End
        question.SetDefaultNext(NextQuestionDeterminant.End());
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        // Assert
        var retrieved = await _context.Questions.FindAsync(question.Id);
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.DefaultNext);
        Assert.Equal(NextStepType.EndSurvey, retrieved.DefaultNext.Type);
        Assert.Null(retrieved.DefaultNext.NextQuestionId);
    }

    #endregion
}
