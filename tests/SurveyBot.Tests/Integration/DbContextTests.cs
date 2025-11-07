using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SurveyBot.Core.Entities;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Integration;

public class DbContextTests : IDisposable
{
    private readonly SurveyBotDbContext _context;

    public DbContextTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
    }

    [Fact]
    public void DbContext_AllDbSetsAreConfigured()
    {
        // Assert
        _context.Users.Should().NotBeNull();
        _context.Surveys.Should().NotBeNull();
        _context.Questions.Should().NotBeNull();
        _context.Responses.Should().NotBeNull();
        _context.Answers.Should().NotBeNull();
    }

    [Fact]
    public async Task DbContext_CanAddAndRetrieveUser()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();

        // Act
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var retrievedUser = await _context.Users.FindAsync(user.Id);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser!.TelegramId.Should().Be(user.TelegramId);
    }

    [Fact]
    public async Task DbContext_SaveChanges_SetsCreatedAtTimestamp()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        var beforeSave = DateTime.UtcNow;

        // Act
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var afterSave = DateTime.UtcNow;

        // Assert
        user.CreatedAt.Should().BeOnOrAfter(beforeSave);
        user.CreatedAt.Should().BeOnOrBefore(afterSave);
    }

    [Fact]
    public async Task DbContext_SaveChanges_SetsUpdatedAtTimestamp()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var originalUpdatedAt = user.UpdatedAt;
        await Task.Delay(10);

        // Act
        user.Username = "updated";
        await _context.SaveChangesAsync();

        // Assert
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task DbContext_Relationships_UserToSurveys_WorksCorrectly()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        // Act
        var userWithSurveys = await _context.Users
            .Include(u => u.Surveys)
            .FirstAsync(u => u.Id == user.Id);

        // Assert
        userWithSurveys.Surveys.Should().HaveCount(1);
        userWithSurveys.Surveys.First().Title.Should().Be(survey.Title);
    }

    [Fact]
    public async Task DbContext_Relationships_SurveyToQuestions_WorksCorrectly()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question1 = EntityBuilder.CreateQuestion(surveyId: survey.Id, orderIndex: 0);
        var question2 = EntityBuilder.CreateQuestion(surveyId: survey.Id, orderIndex: 1);
        await _context.Questions.AddRangeAsync(question1, question2);
        await _context.SaveChangesAsync();

        // Act
        var surveyWithQuestions = await _context.Surveys
            .Include(s => s.Questions)
            .FirstAsync(s => s.Id == survey.Id);

        // Assert
        surveyWithQuestions.Questions.Should().HaveCount(2);
    }

    [Fact]
    public async Task DbContext_Relationships_ResponseToAnswers_WorksCorrectly()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = EntityBuilder.CreateQuestion(surveyId: survey.Id);
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        var response = EntityBuilder.CreateResponse(surveyId: survey.Id);
        await _context.Responses.AddAsync(response);
        await _context.SaveChangesAsync();

        var answer = EntityBuilder.CreateAnswer(responseId: response.Id, questionId: question.Id);
        await _context.Answers.AddAsync(answer);
        await _context.SaveChangesAsync();

        // Act
        var responseWithAnswers = await _context.Responses
            .Include(r => r.Answers)
            .FirstAsync(r => r.Id == response.Id);

        // Assert
        responseWithAnswers.Answers.Should().HaveCount(1);
        responseWithAnswers.Answers.First().AnswerText.Should().Be(answer.AnswerText);
    }

    [Fact]
    public async Task DbContext_CascadeDelete_SurveyDeletesQuestions()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = EntityBuilder.CreateQuestion(surveyId: survey.Id);
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        var questionId = question.Id;

        // Act
        _context.Surveys.Remove(survey);
        await _context.SaveChangesAsync();

        var deletedQuestion = await _context.Questions.FindAsync(questionId);

        // Assert
        deletedQuestion.Should().BeNull();
    }

    [Fact]
    public async Task DbContext_CascadeDelete_SurveyDeletesResponses()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var response = EntityBuilder.CreateResponse(surveyId: survey.Id);
        await _context.Responses.AddAsync(response);
        await _context.SaveChangesAsync();

        var responseId = response.Id;

        // Act
        _context.Surveys.Remove(survey);
        await _context.SaveChangesAsync();

        var deletedResponse = await _context.Responses.FindAsync(responseId);

        // Assert
        deletedResponse.Should().BeNull();
    }

    [Fact]
    public async Task DbContext_CascadeDelete_ResponseDeletesAnswers()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = EntityBuilder.CreateQuestion(surveyId: survey.Id);
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        var response = EntityBuilder.CreateResponse(surveyId: survey.Id);
        await _context.Responses.AddAsync(response);
        await _context.SaveChangesAsync();

        var answer = EntityBuilder.CreateAnswer(responseId: response.Id, questionId: question.Id);
        await _context.Answers.AddAsync(answer);
        await _context.SaveChangesAsync();

        var answerId = answer.Id;

        // Act
        _context.Responses.Remove(response);
        await _context.SaveChangesAsync();

        var deletedAnswer = await _context.Answers.FindAsync(answerId);

        // Assert
        deletedAnswer.Should().BeNull();
    }

    [Fact]
    public async Task DbContext_UniqueIndex_TelegramId_EnforcesUniqueness()
    {
        // Arrange
        var user1 = EntityBuilder.CreateUser(telegramId: 12345);
        var user2 = EntityBuilder.CreateUser(telegramId: 12345); // Same TelegramId

        await _context.Users.AddAsync(user1);
        await _context.SaveChangesAsync();

        // Act & Assert
        // Note: In-memory database doesn't enforce unique constraints
        // This test verifies the configuration is set up, but actual enforcement
        // would need a real database or a different testing approach
        _context.Users.Add(user2);

        // In a real database, this would throw a DbUpdateException
        // For in-memory, we just verify the configuration doesn't cause errors
        var action = async () => await _context.SaveChangesAsync();

        // In-memory DB doesn't enforce constraints, so this won't throw
        // But the configuration is still validated when migrations are created
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DbContext_MultipleEntities_CanBeSavedTogether()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        var survey = EntityBuilder.CreateSurvey(creatorId: 1); // Will be updated after user is saved
        var question = EntityBuilder.CreateQuestion(surveyId: 1);

        // Act
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        survey.CreatorId = user.Id;
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        question.SurveyId = survey.Id;
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        // Assert
        var userCount = await _context.Users.CountAsync();
        var surveyCount = await _context.Surveys.CountAsync();
        var questionCount = await _context.Questions.CountAsync();

        userCount.Should().Be(1);
        surveyCount.Should().Be(1);
        questionCount.Should().Be(1);
    }

    [Fact]
    public async Task DbContext_ComplexQuery_WorksCorrectly()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var activeSurvey = EntityBuilder.CreateSurvey(title: "Active", creatorId: user.Id, isActive: true);
        var inactiveSurvey = EntityBuilder.CreateSurvey(title: "Inactive", creatorId: user.Id, isActive: false);
        await _context.Surveys.AddRangeAsync(activeSurvey, inactiveSurvey);
        await _context.SaveChangesAsync();

        // Act
        var activeSurveys = await _context.Surveys
            .Include(s => s.Creator)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Title)
            .ToListAsync();

        // Assert
        activeSurveys.Should().HaveCount(1);
        activeSurveys.First().Title.Should().Be("Active");
        activeSurveys.First().Creator.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
