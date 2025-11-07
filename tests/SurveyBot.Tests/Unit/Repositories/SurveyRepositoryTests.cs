using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SurveyBot.Core.Entities;
using SurveyBot.Infrastructure.Repositories;
using SurveyBot.Tests.Fixtures;
using SurveyBot.Tests.Helpers;

namespace SurveyBot.Tests.Unit.Repositories;

public class SurveyRepositoryTests : RepositoryTestBase
{
    private readonly SurveyRepository _repository;
    private readonly UserRepository _userRepository;

    public SurveyRepositoryTests()
    {
        _repository = new SurveyRepository(_context);
        _userRepository = new UserRepository(_context);
    }

    [Fact]
    public async Task GetByIdWithQuestionsAsync_ExistingSurvey_ReturnsSurveyWithQuestions()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _repository.CreateAsync(survey);

        var question1 = EntityBuilder.CreateQuestion(surveyId: survey.Id, orderIndex: 0);
        var question2 = EntityBuilder.CreateQuestion(surveyId: survey.Id, orderIndex: 1);
        await _context.Questions.AddRangeAsync(question1, question2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithQuestionsAsync(survey.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Questions.Should().HaveCount(2);
        result.Questions.Should().BeInAscendingOrder(q => q.OrderIndex);
        result.Creator.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_ExistingSurvey_ReturnsSurveyWithAllDetails()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _repository.CreateAsync(survey);

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
        var result = await _repository.GetByIdWithDetailsAsync(survey.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Questions.Should().HaveCount(1);
        result.Responses.Should().HaveCount(1);
        result.Responses.First().Answers.Should().HaveCount(1);
        result.Creator.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByCreatorIdAsync_ExistingSurveys_ReturnsSurveysOrderedByCreationDate()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey1 = EntityBuilder.CreateSurvey(title: "Survey 1", creatorId: user.Id);
        var survey2 = EntityBuilder.CreateSurvey(title: "Survey 2", creatorId: user.Id);
        var survey3 = EntityBuilder.CreateSurvey(title: "Survey 3", creatorId: user.Id);

        await _repository.CreateAsync(survey1);
        await Task.Delay(10); // Ensure different timestamps
        await _repository.CreateAsync(survey2);
        await Task.Delay(10);
        await _repository.CreateAsync(survey3);

        // Act
        var result = await _repository.GetByCreatorIdAsync(user.Id);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(s => s.CreatedAt);
        result.First().Title.Should().Be("Survey 3");
    }

    [Fact]
    public async Task GetActiveSurveysAsync_MixedSurveys_ReturnsOnlyActiveSurveys()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var activeSurvey1 = EntityBuilder.CreateSurvey(title: "Active 1", creatorId: user.Id, isActive: true);
        var activeSurvey2 = EntityBuilder.CreateSurvey(title: "Active 2", creatorId: user.Id, isActive: true);
        var inactiveSurvey = EntityBuilder.CreateSurvey(title: "Inactive", creatorId: user.Id, isActive: false);

        await _repository.CreateAsync(activeSurvey1);
        await _repository.CreateAsync(activeSurvey2);
        await _repository.CreateAsync(inactiveSurvey);

        // Act
        var result = await _repository.GetActiveSurveysAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.IsActive);
        result.Should().NotContain(s => s.Title == "Inactive");
    }

    [Fact]
    public async Task ToggleActiveStatusAsync_ExistingSurvey_TogglesStatus()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
        await _repository.CreateAsync(survey);

        // Act
        var result = await _repository.ToggleActiveStatusAsync(survey.Id);

        // Assert
        result.Should().BeTrue();

        var updatedSurvey = await _repository.GetByIdAsync(survey.Id);
        updatedSurvey!.IsActive.Should().BeFalse();

        // Toggle again
        await _repository.ToggleActiveStatusAsync(survey.Id);
        updatedSurvey = await _repository.GetByIdAsync(survey.Id);
        updatedSurvey!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleActiveStatusAsync_NonExistingSurvey_ReturnsFalse()
    {
        // Act
        var result = await _repository.ToggleActiveStatusAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Skip = "InMemory DB doesn't support EF.Functions.ILike")]
    public async Task SearchByTitleAsync_MatchingTitle_ReturnsSurveys()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey1 = EntityBuilder.CreateSurvey(title: "Customer Satisfaction Survey", creatorId: user.Id);
        var survey2 = EntityBuilder.CreateSurvey(title: "Employee Feedback", creatorId: user.Id);
        var survey3 = EntityBuilder.CreateSurvey(title: "Customer Experience", creatorId: user.Id);

        await _repository.CreateAsync(survey1);
        await _repository.CreateAsync(survey2);
        await _repository.CreateAsync(survey3);

        // Act
        var result = await _repository.SearchByTitleAsync("Customer");

        // Assert
        // Note: This test requires PostgreSQL-specific ILike function
        // In real PostgreSQL, this would return 2 surveys
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Title.Contains("Customer"));
    }

    [Fact]
    public async Task SearchByTitleAsync_EmptySearchTerm_ReturnsAllSurveys()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        await _repository.CreateAsync(EntityBuilder.CreateSurvey(creatorId: user.Id));
        await _repository.CreateAsync(EntityBuilder.CreateSurvey(creatorId: user.Id));

        // Act
        var result = await _repository.SearchByTitleAsync("");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetResponseCountAsync_SurveyWithResponses_ReturnsCorrectCount()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _repository.CreateAsync(survey);

        await _context.Responses.AddRangeAsync(
            EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 111),
            EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 222),
            EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 333)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetResponseCountAsync(survey.Id);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task HasResponsesAsync_SurveyWithResponses_ReturnsTrue()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _repository.CreateAsync(survey);

        await _context.Responses.AddAsync(EntityBuilder.CreateResponse(surveyId: survey.Id));
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.HasResponsesAsync(survey.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasResponsesAsync_SurveyWithoutResponses_ReturnsFalse()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _repository.CreateAsync(survey);

        // Act
        var result = await _repository.HasResponsesAsync(survey.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_Override_IncludesCreator()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _repository.CreateAsync(survey);

        // Act
        var result = await _repository.GetByIdAsync(survey.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Creator.Should().NotBeNull();
        result.Creator.TelegramId.Should().Be(user.TelegramId);
    }

    [Fact]
    public async Task GetAllAsync_Override_IncludesCreatorAndQuestions()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _repository.CreateAsync(survey);

        var question = EntityBuilder.CreateQuestion(surveyId: survey.Id);
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Creator.Should().NotBeNull();
        result.First().Questions.Should().HaveCount(1);
    }
}
