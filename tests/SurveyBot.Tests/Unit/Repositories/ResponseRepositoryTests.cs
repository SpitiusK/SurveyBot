using FluentAssertions;
using SurveyBot.Infrastructure.Repositories;
using SurveyBot.Tests.Fixtures;
using SurveyBot.Tests.Helpers;

namespace SurveyBot.Tests.Unit.Repositories;

public class ResponseRepositoryTests : RepositoryTestBase
{
    private readonly ResponseRepository _repository;
    private readonly SurveyRepository _surveyRepository;
    private readonly UserRepository _userRepository;
    private readonly QuestionRepository _questionRepository;

    public ResponseRepositoryTests()
    {
        _repository = new ResponseRepository(_context);
        _surveyRepository = new SurveyRepository(_context);
        _userRepository = new UserRepository(_context);
        _questionRepository = new QuestionRepository(_context);
    }

    [Fact]
    public async Task GetByIdWithAnswersAsync_ResponseWithAnswers_ReturnsResponseWithAnswers()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        var question = EntityBuilder.CreateQuestion(surveyId: survey.Id);
        await _questionRepository.CreateAsync(question);

        var response = EntityBuilder.CreateResponse(surveyId: survey.Id);
        await _repository.CreateAsync(response);

        var answer = EntityBuilder.CreateAnswer(responseId: response.Id, questionId: question.Id);
        await _context.Answers.AddAsync(answer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithAnswersAsync(response.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Answers.Should().HaveCount(1);
        result.Answers.First().Question.Should().NotBeNull();
        result.Survey.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBySurveyIdAsync_ExistingResponses_ReturnsOrderedBySumbissionDate()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        var response1 = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 111);
        var response2 = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 222);
        var response3 = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 333);

        await _repository.CreateAsync(response1);
        await Task.Delay(10);
        await _repository.CreateAsync(response2);
        await Task.Delay(10);
        await _repository.CreateAsync(response3);

        // Act
        var result = await _repository.GetBySurveyIdAsync(survey.Id);

        // Assert
        result.Should().HaveCount(3);
        result.First().RespondentTelegramId.Should().Be(333); // Most recent first
        result.Last().RespondentTelegramId.Should().Be(111);
    }

    [Fact]
    public async Task GetCompletedBySurveyIdAsync_MixedResponses_ReturnsOnlyCompleted()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        var completed1 = EntityBuilder.CreateResponse(surveyId: survey.Id, isComplete: true);
        completed1.SetSubmittedAt(DateTime.UtcNow);
        var incomplete = EntityBuilder.CreateResponse(surveyId: survey.Id, isComplete: false);
        var completed2 = EntityBuilder.CreateResponse(surveyId: survey.Id, isComplete: true);
        completed2.SetSubmittedAt(DateTime.UtcNow);

        await _repository.CreateAsync(completed1);
        await _repository.CreateAsync(incomplete);
        await _repository.CreateAsync(completed2);

        // Act
        var result = await _repository.GetCompletedBySurveyIdAsync(survey.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.IsComplete);
        result.Should().OnlyContain(r => r.SubmittedAt.HasValue);
    }

    [Fact]
    public async Task GetByUserAndSurveyAsync_MultipleResponses_ReturnsUserResponses()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        var userResponse1 = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 12345);
        var userResponse2 = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 12345);
        var otherResponse = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 99999);

        await _repository.CreateAsync(userResponse1);
        await _repository.CreateAsync(userResponse2);
        await _repository.CreateAsync(otherResponse);

        // Act
        var result = await _repository.GetByUserAndSurveyAsync(survey.Id, 12345);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.RespondentTelegramId == 12345);
    }

    [Fact]
    public async Task GetIncompleteResponseAsync_ExistingIncompleteResponse_ReturnsResponse()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        var question = EntityBuilder.CreateQuestion(surveyId: survey.Id);
        await _questionRepository.CreateAsync(question);

        var incompleteResponse = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 12345, isComplete: false);
        await _repository.CreateAsync(incompleteResponse);

        // Act
        var result = await _repository.GetIncompleteResponseAsync(survey.Id, 12345);

        // Assert
        result.Should().NotBeNull();
        result!.IsComplete.Should().BeFalse();
        result.Survey.Should().NotBeNull();
        result.Survey.Questions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetIncompleteResponseAsync_OnlyCompletedResponses_ReturnsNull()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        var completedResponse = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 12345, isComplete: true);
        await _repository.CreateAsync(completedResponse);

        // Act
        var result = await _repository.GetIncompleteResponseAsync(survey.Id, 12345);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task HasUserCompletedSurveyAsync_UserCompletedSurvey_ReturnsTrue()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        var completedResponse = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 12345, isComplete: true);
        await _repository.CreateAsync(completedResponse);

        // Act
        var result = await _repository.HasUserCompletedSurveyAsync(survey.Id, 12345);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasUserCompletedSurveyAsync_UserHasOnlyIncompleteResponse_ReturnsFalse()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        var incompleteResponse = EntityBuilder.CreateResponse(surveyId: survey.Id, respondentTelegramId: 12345, isComplete: false);
        await _repository.CreateAsync(incompleteResponse);

        // Act
        var result = await _repository.HasUserCompletedSurveyAsync(survey.Id, 12345);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetCompletedCountAsync_MixedResponses_ReturnsCompletedCount()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        await _repository.CreateAsync(EntityBuilder.CreateResponse(surveyId: survey.Id, isComplete: true));
        await _repository.CreateAsync(EntityBuilder.CreateResponse(surveyId: survey.Id, isComplete: false));
        await _repository.CreateAsync(EntityBuilder.CreateResponse(surveyId: survey.Id, isComplete: true));
        await _repository.CreateAsync(EntityBuilder.CreateResponse(surveyId: survey.Id, isComplete: true));

        // Act
        var result = await _repository.GetCompletedCountAsync(survey.Id);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ResponsesInRange_ReturnsMatchingResponses()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        var response1 = EntityBuilder.CreateResponse(surveyId: survey.Id);
        response1.SetSubmittedAt(new DateTime(2024, 1, 15));

        var response2 = EntityBuilder.CreateResponse(surveyId: survey.Id);
        response2.SetSubmittedAt(new DateTime(2024, 1, 20));

        var response3 = EntityBuilder.CreateResponse(surveyId: survey.Id);
        response3.SetSubmittedAt(new DateTime(2024, 1, 25));

        await _repository.CreateAsync(response1);
        await _repository.CreateAsync(response2);
        await _repository.CreateAsync(response3);

        // Act
        var result = await _repository.GetByDateRangeAsync(
            survey.Id,
            new DateTime(2024, 1, 18),
            new DateTime(2024, 1, 22));

        // Assert
        result.Should().HaveCount(1);
        result.First().SubmittedAt.Should().Be(new DateTime(2024, 1, 20));
    }

    [Fact]
    public async Task MarkAsCompleteAsync_ExistingResponse_MarksComplete()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        var response = EntityBuilder.CreateResponse(surveyId: survey.Id, isComplete: false);
        await _repository.CreateAsync(response);

        // Act
        var result = await _repository.MarkAsCompleteAsync(response.Id);

        // Assert
        result.Should().BeTrue();

        var updatedResponse = await _repository.GetByIdAsync(response.Id);
        updatedResponse!.IsComplete.Should().BeTrue();
        updatedResponse.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsCompleteAsync_NonExistingResponse_ReturnsFalse()
    {
        // Act
        var result = await _repository.MarkAsCompleteAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBySurveyIdAsync_ExistingResponses_DeletesAllAndReturnsCount()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        await _repository.CreateAsync(EntityBuilder.CreateResponse(surveyId: survey.Id));
        await _repository.CreateAsync(EntityBuilder.CreateResponse(surveyId: survey.Id));
        await _repository.CreateAsync(EntityBuilder.CreateResponse(surveyId: survey.Id));

        // Act
        var result = await _repository.DeleteBySurveyIdAsync(survey.Id);

        // Assert
        result.Should().Be(3);

        var remainingResponses = await _repository.GetBySurveyIdAsync(survey.Id);
        remainingResponses.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBySurveyIdAsync_NoResponses_ReturnsZero()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        // Act
        var result = await _repository.DeleteBySurveyIdAsync(survey.Id);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetByIdAsync_Override_IncludesSurvey()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        var response = EntityBuilder.CreateResponse(surveyId: survey.Id);
        await _repository.CreateAsync(response);

        // Act
        var result = await _repository.GetByIdAsync(response.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Survey.Should().NotBeNull();
        result.Survey.Id.Should().Be(survey.Id);
    }

    [Fact]
    public async Task GetAllAsync_Override_IncludesSurveyAndAnswersOrderedByDate()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _userRepository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _surveyRepository.CreateAsync(survey);

        var response1 = EntityBuilder.CreateResponse(surveyId: survey.Id);
        var response2 = EntityBuilder.CreateResponse(surveyId: survey.Id);

        await _repository.CreateAsync(response1);
        await Task.Delay(10);
        await _repository.CreateAsync(response2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.Survey != null);
        result.First().Id.Should().Be(response2.Id); // Most recent first
    }
}
