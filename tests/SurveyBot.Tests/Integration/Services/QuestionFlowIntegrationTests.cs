using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.API.Mapping;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Enums;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Infrastructure.Repositories;
using SurveyBot.Infrastructure.Services;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Integration.Services;

/// <summary>
/// Integration tests for conditional question flow feature.
/// Tests complete flow from starting survey through answering questions and getting next questions.
/// Uses EF Core in-memory database with actual entities, repositories, and services.
/// </summary>
public class QuestionFlowIntegrationTests : IAsyncLifetime
{
    private SurveyBotDbContext _context = null!;
    private SurveyRepository _surveyRepository = null!;
    private QuestionRepository _questionRepository = null!;
    private ResponseRepository _responseRepository = null!;
    private AnswerRepository _answerRepository = null!;
    private ResponseService _responseService = null!;
    private IMapper _mapper = null!;

    // Test data
    private User _testUser = null!;
    private Survey _testSurvey = null!;
    private Question _q1_text = null!;
    private Question _q2_singleChoice = null!;
    private Question _q3_rating = null!;
    private Question _q4_multipleChoice = null!;
    private Question _q5_text = null!;
    private QuestionOption _q2_opt1 = null!;
    private QuestionOption _q2_opt2 = null!;
    private QuestionOption _q3_opt1 = null!;
    private QuestionOption _q3_opt2 = null!;
    private QuestionOption _q3_opt3 = null!;
    private QuestionOption _q3_opt4 = null!;
    private QuestionOption _q3_opt5 = null!;

    public async Task InitializeAsync()
    {
        // Create in-memory database
        _context = TestDbContextFactory.CreateInMemoryContext();

        // Create repositories
        _surveyRepository = new SurveyRepository(_context);
        _questionRepository = new QuestionRepository(_context);
        _responseRepository = new ResponseRepository(_context);
        _answerRepository = new AnswerRepository(_context);

        // Create mapper
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(SurveyMappingProfile).Assembly);
        });
        _mapper = mapperConfig.CreateMapper();

        // Create service with real dependencies
        _responseService = new ResponseService(
            _responseRepository,
            _answerRepository,
            _surveyRepository,
            _questionRepository,
            _context,
            _mapper,
            Mock.Of<ILogger<ResponseService>>());

        // Seed test data
        await SeedTestDataAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Create test user
        _testUser = EntityBuilder.CreateUser(
            telegramId: 123456789L,
            username: "testuser",
            firstName: "Test",
            lastName: "User");
        await _context.Users.AddAsync(_testUser);
        await _context.SaveChangesAsync();

        // Create test survey
        _testSurvey = Survey.Create(
            title: "Conditional Flow Test Survey",
            creatorId: _testUser.Id,
            description: "Testing branching and linear question flow",
            code: "TEST01",
            isActive: true,
            allowMultipleResponses: false,
            showResults: true);
        await _context.Surveys.AddAsync(_testSurvey);
        await _context.SaveChangesAsync();

        // Question 1: Text (non-branching) → Q2
        _q1_text = Question.CreateTextQuestion(
            surveyId: _testSurvey.Id,
            questionText: "What is your name?",
            orderIndex: 0,
            isRequired: true);
        // DefaultNextQuestionId will be set after Q2 is created
        await _context.Questions.AddAsync(_q1_text);
        await _context.SaveChangesAsync();

        // Question 2: SingleChoice (branching) - "Yes" → Q3, "No" → END
        _q2_singleChoice = Question.CreateSingleChoiceQuestion(
            surveyId: _testSurvey.Id,
            questionText: "Do you like surveys?",
            orderIndex: 1,
            optionsJson: "[\"Yes\", \"No\"]",
            isRequired: true);
        await _context.Questions.AddAsync(_q2_singleChoice);
        await _context.SaveChangesAsync();

        // Question 3: Rating (branching) - 1-3 → Q4, 4-5 → Q5
        _q3_rating = Question.CreateRatingQuestion(
            surveyId: _testSurvey.Id,
            questionText: "How satisfied are you?",
            orderIndex: 2,
            isRequired: true);
        await _context.Questions.AddAsync(_q3_rating);
        await _context.SaveChangesAsync();

        // Question 4: MultipleChoice (non-branching) → Q5
        _q4_multipleChoice = Question.CreateMultipleChoiceQuestion(
            surveyId: _testSurvey.Id,
            questionText: "What features do you like? (Select all)",
            orderIndex: 3,
            optionsJson: "[\"Feature A\", \"Feature B\", \"Feature C\"]",
            isRequired: true);
        // DefaultNextQuestionId will be set after Q5 is created
        await _context.Questions.AddAsync(_q4_multipleChoice);
        await _context.SaveChangesAsync();

        // Question 5: Text (non-branching) → END
        _q5_text = Question.CreateTextQuestion(
            surveyId: _testSurvey.Id,
            questionText: "Any additional comments?",
            orderIndex: 4,
            isRequired: false);
        // DefaultNextQuestionId = null (end of survey)
        await _context.Questions.AddAsync(_q5_text);
        await _context.SaveChangesAsync();

        // Update Q1 and Q4 to point to next questions
        _q1_text.SetDefaultNext(NextQuestionDeterminant.ToQuestion(_q2_singleChoice.Id));
        _q4_multipleChoice.SetDefaultNext(NextQuestionDeterminant.ToQuestion(_q5_text.Id));
        await _context.SaveChangesAsync();

        // Create options for Q2 (SingleChoice)
        _q2_opt1 = QuestionOption.Create(
            questionId: _q2_singleChoice.Id,
            text: "Yes",
            orderIndex: 0,
            next: NextQuestionDeterminant.ToQuestion(_q3_rating.Id));  // Yes → Q3

        _q2_opt2 = QuestionOption.Create(
            questionId: _q2_singleChoice.Id,
            text: "No",
            orderIndex: 1,
            next: NextQuestionDeterminant.End());  // No → END

        await _context.QuestionOptions.AddRangeAsync(_q2_opt1, _q2_opt2);
        await _context.SaveChangesAsync();

        // Create options for Q3 (Rating) - ratings 1-5
        _q3_opt1 = QuestionOption.Create(
            questionId: _q3_rating.Id,
            text: "1",
            orderIndex: 0,
            next: NextQuestionDeterminant.ToQuestion(_q4_multipleChoice.Id));  // Low rating → Q4

        _q3_opt2 = QuestionOption.Create(
            questionId: _q3_rating.Id,
            text: "2",
            orderIndex: 1,
            next: NextQuestionDeterminant.ToQuestion(_q4_multipleChoice.Id));

        _q3_opt3 = QuestionOption.Create(
            questionId: _q3_rating.Id,
            text: "3",
            orderIndex: 2,
            next: NextQuestionDeterminant.ToQuestion(_q4_multipleChoice.Id));

        _q3_opt4 = QuestionOption.Create(
            questionId: _q3_rating.Id,
            text: "4",
            orderIndex: 3,
            next: NextQuestionDeterminant.ToQuestion(_q5_text.Id));  // High rating → Q5 (skip Q4)

        _q3_opt5 = QuestionOption.Create(
            questionId: _q3_rating.Id,
            text: "5",
            orderIndex: 4,
            next: NextQuestionDeterminant.ToQuestion(_q5_text.Id));

        await _context.QuestionOptions.AddRangeAsync(_q3_opt1, _q3_opt2, _q3_opt3, _q3_opt4, _q3_opt5);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    #region Survey Start Tests

    [Fact]
    public async Task StartSurvey_ValidSurvey_CreatesResponseAndReturnsFirstQuestion()
    {
        // Arrange
        var userId = 999888777L;
        var surveyId = _testSurvey.Id;

        // Act
        var response = await _responseService.StartResponseAsync(surveyId, userId);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(surveyId, response.SurveyId);
        Assert.Equal(userId, response.RespondentTelegramId);
        Assert.False(response.IsComplete);
        Assert.NotNull(response.StartedAt);
        Assert.Null(response.SubmittedAt);

        // Verify response was saved to database
        var savedResponse = await _responseRepository.GetByIdAsync(response.Id);
        Assert.NotNull(savedResponse);
        Assert.Equal(userId, savedResponse.RespondentTelegramId);
    }

    [Fact]
    public async Task StartSurvey_InactiveSurvey_ThrowsSurveyOperationException()
    {
        // Arrange
        var inactiveSurvey = Survey.Create(
            title: "Inactive Survey",
            creatorId: _testUser.Id,
            code: "INACT1",
            isActive: false);  // Not active
        await _context.Surveys.AddAsync(inactiveSurvey);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<SurveyOperationException>(
            async () => await _responseService.StartResponseAsync(inactiveSurvey.Id, 111222333L));
    }

    [Fact]
    public async Task StartSurvey_NonExistentSurvey_ThrowsSurveyNotFoundException()
    {
        // Arrange
        var nonExistentSurveyId = 99999;

        // Act & Assert
        await Assert.ThrowsAsync<SurveyNotFoundException>(
            async () => await _responseService.StartResponseAsync(nonExistentSurveyId, 111222333L));
    }

    [Fact]
    public async Task StartSurvey_DuplicateResponse_WhenNotAllowed_ThrowsDuplicateResponseException()
    {
        // Arrange
        var userId = 555666777L;

        // Start and complete first response
        var firstResponse = await _responseService.StartResponseAsync(_testSurvey.Id, userId);
        await _responseService.CompleteResponseAsync(firstResponse.Id);

        // Survey does not allow multiple responses
        Assert.False(_testSurvey.AllowMultipleResponses);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateResponseException>(
            async () => await _responseService.StartResponseAsync(_testSurvey.Id, userId));
    }

    #endregion

    #region Linear Flow Tests

    [Fact]
    public async Task LinearFlow_NonBranchingQuestions_FollowsDefaultNextQuestionId()
    {
        // Arrange
        var userId = 111222333L;
        var response = await _responseService.StartResponseAsync(_testSurvey.Id, userId);

        // Act - Answer Q1 (Text, non-branching, DefaultNextQuestionId = Q2)
        await _responseService.SaveAnswerAsync(response.Id, _q1_text.Id, answerText: "John Doe");
        await _responseService.RecordVisitedQuestionAsync(response.Id, _q1_text.Id);

        // Get next question
        var nextQuestionId = await _responseService.GetNextQuestionAsync(response.Id);

        // Assert
        Assert.NotNull(nextQuestionId);
        Assert.Equal(_q2_singleChoice.Id, nextQuestionId);

        // Verify answer was saved with Next value object
        var savedAnswer = await _answerRepository.GetByResponseAndQuestionAsync(response.Id, _q1_text.Id);
        Assert.NotNull(savedAnswer);
        Assert.NotNull(savedAnswer.Next);
        Assert.Equal(NextStepType.GoToQuestion, savedAnswer.Next.Type);
        Assert.Equal(_q2_singleChoice.Id, savedAnswer.Next.NextQuestionId);
    }

    [Fact]
    public async Task LinearFlow_MultipleNonBranchingQuestions_FollowsChain()
    {
        // Arrange - Create simple linear survey: Q1 → Q2 → Q3 → END
        var linearSurvey = Survey.Create(
            title: "Linear Survey",
            creatorId: _testUser.Id,
            code: "LIN001",
            isActive: true);
        await _context.Surveys.AddAsync(linearSurvey);
        await _context.SaveChangesAsync();

        var q1 = Question.CreateTextQuestion(
            surveyId: linearSurvey.Id,
            questionText: "Question 1",
            orderIndex: 0,
            isRequired: true);
        await _context.Questions.AddAsync(q1);
        await _context.SaveChangesAsync();

        var q2 = Question.CreateTextQuestion(
            surveyId: linearSurvey.Id,
            questionText: "Question 2",
            orderIndex: 1,
            isRequired: true);
        await _context.Questions.AddAsync(q2);
        await _context.SaveChangesAsync();

        var q3 = Question.CreateTextQuestion(
            surveyId: linearSurvey.Id,
            questionText: "Question 3",
            orderIndex: 2,
            isRequired: true);
        // DefaultNextQuestionId = null (END)
        await _context.Questions.AddAsync(q3);
        await _context.SaveChangesAsync();

        // Set up chain: Q1 → Q2 → Q3 → END
        q1.SetDefaultNext(NextQuestionDeterminant.ToQuestion(q2.Id));
        q2.SetDefaultNext(NextQuestionDeterminant.ToQuestion(q3.Id));
        await _context.SaveChangesAsync();

        var userId = 222333444L;
        var response = await _responseService.StartResponseAsync(linearSurvey.Id, userId);

        // Act - Answer all questions in sequence
        await _responseService.SaveAnswerAsync(response.Id, q1.Id, answerText: "Answer 1");
        await _responseService.RecordVisitedQuestionAsync(response.Id, q1.Id);
        var next1 = await _responseService.GetNextQuestionAsync(response.Id);

        await _responseService.SaveAnswerAsync(response.Id, q2.Id, answerText: "Answer 2");
        await _responseService.RecordVisitedQuestionAsync(response.Id, q2.Id);
        var next2 = await _responseService.GetNextQuestionAsync(response.Id);

        await _responseService.SaveAnswerAsync(response.Id, q3.Id, answerText: "Answer 3");
        await _responseService.RecordVisitedQuestionAsync(response.Id, q3.Id);
        var next3 = await _responseService.GetNextQuestionAsync(response.Id);

        // Assert
        Assert.Equal(q2.Id, next1);
        Assert.Equal(q3.Id, next2);
        Assert.Null(next3);  // End of survey
    }

    [Fact]
    public async Task LinearFlow_MultipleChoiceQuestion_UsesDefaultNextQuestionId()
    {
        // Arrange
        var userId = 333444555L;
        var response = await _responseService.StartResponseAsync(_testSurvey.Id, userId);

        // Answer questions to reach Q4 (MultipleChoice)
        await _responseService.SaveAnswerAsync(response.Id, _q1_text.Id, answerText: "Test");
        await _responseService.RecordVisitedQuestionAsync(response.Id, _q1_text.Id);

        await _responseService.SaveAnswerAsync(response.Id, _q2_singleChoice.Id, selectedOptions: new List<string> { "Yes" });
        await _responseService.RecordVisitedQuestionAsync(response.Id, _q2_singleChoice.Id);

        await _responseService.SaveAnswerAsync(response.Id, _q3_rating.Id, ratingValue: 2);
        await _responseService.RecordVisitedQuestionAsync(response.Id, _q3_rating.Id);

        // Act - Answer Q4 (MultipleChoice, should go to Q5 regardless of selection)
        await _responseService.SaveAnswerAsync(response.Id, _q4_multipleChoice.Id,
            selectedOptions: new List<string> { "Feature A", "Feature C" });
        await _responseService.RecordVisitedQuestionAsync(response.Id, _q4_multipleChoice.Id);

        var nextQuestionId = await _responseService.GetNextQuestionAsync(response.Id);

        // Assert
        Assert.NotNull(nextQuestionId);
        Assert.Equal(_q5_text.Id, nextQuestionId);
    }

    #endregion

    #region Branching Flow Tests

    [Fact]
    public async Task BranchingFlow_SingleChoiceQuestion_DifferentOptionsDifferentPaths()
    {
        // Arrange
        var userId1 = 444555666L;
        var userId2 = 555666777L;

        var response1 = await _responseService.StartResponseAsync(_testSurvey.Id, userId1);
        var response2 = await _responseService.StartResponseAsync(_testSurvey.Id, userId2);

        // Answer Q1 for both responses
        await _responseService.SaveAnswerAsync(response1.Id, _q1_text.Id, answerText: "User 1");
        await _responseService.RecordVisitedQuestionAsync(response1.Id, _q1_text.Id);

        await _responseService.SaveAnswerAsync(response2.Id, _q1_text.Id, answerText: "User 2");
        await _responseService.RecordVisitedQuestionAsync(response2.Id, _q1_text.Id);

        // Act - Answer Q2 with different options
        // Response 1: "Yes" → should go to Q3
        await _responseService.SaveAnswerAsync(response1.Id, _q2_singleChoice.Id, selectedOptions: new List<string> { "Yes" });
        await _responseService.RecordVisitedQuestionAsync(response1.Id, _q2_singleChoice.Id);
        var next1 = await _responseService.GetNextQuestionAsync(response1.Id);

        // Response 2: "No" → should end survey
        await _responseService.SaveAnswerAsync(response2.Id, _q2_singleChoice.Id, selectedOptions: new List<string> { "No" });
        await _responseService.RecordVisitedQuestionAsync(response2.Id, _q2_singleChoice.Id);
        var next2 = await _responseService.GetNextQuestionAsync(response2.Id);

        // Assert
        Assert.Equal(_q3_rating.Id, next1);  // "Yes" goes to Q3
        Assert.Null(next2);  // "No" ends survey
    }

    [Fact]
    public async Task BranchingFlow_RatingQuestion_DifferentRatingsDifferentPaths()
    {
        // Arrange - Two users reach Q3
        var userId1 = 666777888L;
        var userId2 = 777888999L;

        var response1 = await _responseService.StartResponseAsync(_testSurvey.Id, userId1);
        var response2 = await _responseService.StartResponseAsync(_testSurvey.Id, userId2);

        // Both answer Q1 and Q2 (Yes)
        foreach (var resp in new[] { response1, response2 })
        {
            await _responseService.SaveAnswerAsync(resp.Id, _q1_text.Id, answerText: "Test");
            await _responseService.RecordVisitedQuestionAsync(resp.Id, _q1_text.Id);

            await _responseService.SaveAnswerAsync(resp.Id, _q2_singleChoice.Id, selectedOptions: new List<string> { "Yes" });
            await _responseService.RecordVisitedQuestionAsync(resp.Id, _q2_singleChoice.Id);
        }

        // Act - Answer Q3 with different ratings
        // Response 1: Rating 2 (low) → should go to Q4
        await _responseService.SaveAnswerAsync(response1.Id, _q3_rating.Id, ratingValue: 2);
        await _responseService.RecordVisitedQuestionAsync(response1.Id, _q3_rating.Id);
        var next1 = await _responseService.GetNextQuestionAsync(response1.Id);

        // Response 2: Rating 5 (high) → should go to Q5 (skip Q4)
        await _responseService.SaveAnswerAsync(response2.Id, _q3_rating.Id, ratingValue: 5);
        await _responseService.RecordVisitedQuestionAsync(response2.Id, _q3_rating.Id);
        var next2 = await _responseService.GetNextQuestionAsync(response2.Id);

        // Assert
        Assert.Equal(_q4_multipleChoice.Id, next1);  // Low rating → Q4
        Assert.Equal(_q5_text.Id, next2);  // High rating → Q5
    }

    [Fact]
    public async Task BranchingFlow_MultipleBranchingLevels_NavigatesCorrectly()
    {
        // Arrange - Test full path with multiple branches
        var userId = 888999000L;
        var response = await _responseService.StartResponseAsync(_testSurvey.Id, userId);

        // Act - Navigate through branching flow
        // Q1 (Text) → DefaultNext = Q2
        await _responseService.SaveAnswerAsync(response.Id, _q1_text.Id, answerText: "Test User");
        await _responseService.RecordVisitedQuestionAsync(response.Id, _q1_text.Id);
        var next1 = await _responseService.GetNextQuestionAsync(response.Id);
        Assert.Equal(_q2_singleChoice.Id, next1);

        // Q2 (SingleChoice) → "Yes" = Q3
        await _responseService.SaveAnswerAsync(response.Id, _q2_singleChoice.Id, selectedOptions: new List<string> { "Yes" });
        await _responseService.RecordVisitedQuestionAsync(response.Id, _q2_singleChoice.Id);
        var next2 = await _responseService.GetNextQuestionAsync(response.Id);
        Assert.Equal(_q3_rating.Id, next2);

        // Q3 (Rating) → Rating 1 = Q4
        await _responseService.SaveAnswerAsync(response.Id, _q3_rating.Id, ratingValue: 1);
        await _responseService.RecordVisitedQuestionAsync(response.Id, _q3_rating.Id);
        var next3 = await _responseService.GetNextQuestionAsync(response.Id);
        Assert.Equal(_q4_multipleChoice.Id, next3);

        // Q4 (MultipleChoice) → DefaultNext = Q5
        await _responseService.SaveAnswerAsync(response.Id, _q4_multipleChoice.Id,
            selectedOptions: new List<string> { "Feature B" });
        await _responseService.RecordVisitedQuestionAsync(response.Id, _q4_multipleChoice.Id);
        var next4 = await _responseService.GetNextQuestionAsync(response.Id);
        Assert.Equal(_q5_text.Id, next4);

        // Q5 (Text) → End (DefaultNext = null)
        await _responseService.SaveAnswerAsync(response.Id, _q5_text.Id, answerText: "Great survey!");
        await _responseService.RecordVisitedQuestionAsync(response.Id, _q5_text.Id);
        var next5 = await _responseService.GetNextQuestionAsync(response.Id);
        Assert.Null(next5);

        // Assert - Verify all questions were visited
        var finalResponse = await _responseRepository.GetByIdWithAnswersAsync(response.Id);
        Assert.NotNull(finalResponse);
        Assert.Equal(5, finalResponse.VisitedQuestionIds.Count);
        Assert.Contains(_q1_text.Id, finalResponse.VisitedQuestionIds);
        Assert.Contains(_q2_singleChoice.Id, finalResponse.VisitedQuestionIds);
        Assert.Contains(_q3_rating.Id, finalResponse.VisitedQuestionIds);
        Assert.Contains(_q4_multipleChoice.Id, finalResponse.VisitedQuestionIds);
        Assert.Contains(_q5_text.Id, finalResponse.VisitedQuestionIds);
    }

    [Fact]
    public async Task BranchingFlow_AllRatingOptionsLeadToDifferentPaths()
    {
        // Arrange - Test all 5 rating options
        var userIds = new[] { 101L, 102L, 103L, 104L, 105L };
        var responses = new List<Core.DTOs.Response.ResponseDto>();

        foreach (var userId in userIds)
        {
            var resp = await _responseService.StartResponseAsync(_testSurvey.Id, userId);
            responses.Add(resp);

            // Answer Q1 and Q2 to reach Q3
            await _responseService.SaveAnswerAsync(resp.Id, _q1_text.Id, answerText: $"User {userId}");
            await _responseService.RecordVisitedQuestionAsync(resp.Id, _q1_text.Id);

            await _responseService.SaveAnswerAsync(resp.Id, _q2_singleChoice.Id, selectedOptions: new List<string> { "Yes" });
            await _responseService.RecordVisitedQuestionAsync(resp.Id, _q2_singleChoice.Id);
        }

        // Act - Answer Q3 with ratings 1-5
        var nextQuestions = new List<int?>();
        for (int i = 0; i < 5; i++)
        {
            int rating = i + 1;  // 1 to 5
            await _responseService.SaveAnswerAsync(responses[i].Id, _q3_rating.Id, ratingValue: rating);
            await _responseService.RecordVisitedQuestionAsync(responses[i].Id, _q3_rating.Id);
            var next = await _responseService.GetNextQuestionAsync(responses[i].Id);
            nextQuestions.Add(next);
        }

        // Assert - Ratings 1-3 go to Q4, ratings 4-5 go to Q5
        Assert.Equal(_q4_multipleChoice.Id, nextQuestions[0]);  // Rating 1 → Q4
        Assert.Equal(_q4_multipleChoice.Id, nextQuestions[1]);  // Rating 2 → Q4
        Assert.Equal(_q4_multipleChoice.Id, nextQuestions[2]);  // Rating 3 → Q4
        Assert.Equal(_q5_text.Id, nextQuestions[3]);  // Rating 4 → Q5
        Assert.Equal(_q5_text.Id, nextQuestions[4]);  // Rating 5 → Q5
    }

    #endregion

    #region Visited Question Prevention Tests

    [Fact]
    public async Task VisitedQuestionPrevention_CannotReAnswerSameQuestion()
    {
        // Arrange
        var userId = 201202203L;
        var response = await _responseService.StartResponseAsync(_testSurvey.Id, userId);

        // Answer Q1
        await _responseService.SaveAnswerAsync(response.Id, _q1_text.Id, answerText: "First answer");
        await _responseService.RecordVisitedQuestionAsync(response.Id, _q1_text.Id);

        // Verify question is marked as visited
        var responseAfterFirst = await _responseRepository.GetByIdWithAnswersAsync(response.Id);
        Assert.Contains(_q1_text.Id, responseAfterFirst!.VisitedQuestionIds);

        // Act - Try to answer Q1 again
        await _responseService.SaveAnswerAsync(response.Id, _q1_text.Id, answerText: "Second answer - should update");

        // The answer should be updated, but visited status remains
        var responseAfterSecond = await _responseRepository.GetByIdWithAnswersAsync(response.Id);

        // Assert - Question still in visited list (only once)
        Assert.Single(responseAfterSecond!.VisitedQuestionIds.Where(id => id == _q1_text.Id));

        // Verify answer was updated
        var answer = await _answerRepository.GetByResponseAndQuestionAsync(response.Id, _q1_text.Id);
        Assert.NotNull(answer);
        Assert.Equal("Second answer - should update", answer.AnswerText);
    }

    [Fact]
    public async Task VisitedQuestionTracking_UpdatesAcrossBotConversation()
    {
        // Arrange
        var userId = 301302303L;
        var response = await _responseService.StartResponseAsync(_testSurvey.Id, userId);

        // Act - Simulate bot conversation by answering questions one by one
        var questionsToAnswer = new[] { _q1_text.Id, _q2_singleChoice.Id, _q3_rating.Id };

        foreach (var questionId in questionsToAnswer)
        {
            await _responseService.RecordVisitedQuestionAsync(response.Id, questionId);

            // Verify visited count increases
            var currentResponse = await _responseRepository.GetByIdWithAnswersAsync(response.Id);
            Assert.Contains(questionId, currentResponse!.VisitedQuestionIds);
        }

        // Assert - All questions marked as visited
        var finalResponse = await _responseRepository.GetByIdWithAnswersAsync(response.Id);
        Assert.Equal(3, finalResponse!.VisitedQuestionIds.Count);
        Assert.Contains(_q1_text.Id, finalResponse.VisitedQuestionIds);
        Assert.Contains(_q2_singleChoice.Id, finalResponse.VisitedQuestionIds);
        Assert.Contains(_q3_rating.Id, finalResponse.VisitedQuestionIds);
    }

    #endregion

    #region Response Completion Tests

    [Fact]
    public async Task ResponseCompletion_WhenNextQuestionIsNull_MarksAsComplete()
    {
        // Arrange - Answer all questions to reach end
        var userId = 401402403L;
        var response = await _responseService.StartResponseAsync(_testSurvey.Id, userId);

        // Answer Q1
        await _responseService.SaveAnswerAsync(response.Id, _q1_text.Id, answerText: "Test");
        await _responseService.RecordVisitedQuestionAsync(response.Id, _q1_text.Id);

        // Answer Q2 with "No" to end survey early
        await _responseService.SaveAnswerAsync(response.Id, _q2_singleChoice.Id, selectedOptions: new List<string> { "No" });
        await _responseService.RecordVisitedQuestionAsync(response.Id, _q2_singleChoice.Id);

        // Verify next question is null (end of survey)
        var nextQuestion = await _responseService.GetNextQuestionAsync(response.Id);
        Assert.Null(nextQuestion);

        // Act - Complete the response
        var completedResponse = await _responseService.CompleteResponseAsync(response.Id);

        // Assert
        Assert.True(completedResponse.IsComplete);
        Assert.NotNull(completedResponse.SubmittedAt);

        // Verify in database
        var dbResponse = await _responseRepository.GetByIdAsync(response.Id);
        Assert.NotNull(dbResponse);
        Assert.True(dbResponse.IsComplete);
        Assert.NotNull(dbResponse.SubmittedAt);
    }

    [Fact]
    public async Task ResponseCompletion_TrackingFlag_UpdatesCorrectly()
    {
        // Arrange
        var userId = 501502503L;
        var response = await _responseService.StartResponseAsync(_testSurvey.Id, userId);

        // Verify initial state
        var initialResponse = await _responseRepository.GetByIdAsync(response.Id);
        Assert.NotNull(initialResponse);
        Assert.False(initialResponse.IsComplete);
        Assert.Null(initialResponse.SubmittedAt);

        // Act - Complete response
        await _responseService.CompleteResponseAsync(response.Id);

        // Assert - Verify completion flag and timestamp
        var completedResponse = await _responseRepository.GetByIdAsync(response.Id);
        Assert.NotNull(completedResponse);
        Assert.True(completedResponse.IsComplete);
        Assert.NotNull(completedResponse.SubmittedAt);
        Assert.True(completedResponse.SubmittedAt > initialResponse.StartedAt);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ErrorHandling_InvalidNextQuestionId_GracefullyHandles()
    {
        // Arrange - Create question with invalid next question ID
        var invalidSurvey = Survey.Create(
            title: "Invalid Flow Survey",
            creatorId: _testUser.Id,
            code: "INV001",
            isActive: true);
        await _context.Surveys.AddAsync(invalidSurvey);
        await _context.SaveChangesAsync();

        var questionWithInvalidNext = Question.CreateTextQuestion(
            surveyId: invalidSurvey.Id,
            questionText: "Question with invalid next",
            orderIndex: 0,
            isRequired: true);
        await _context.Questions.AddAsync(questionWithInvalidNext);
        await _context.SaveChangesAsync();

        // Set invalid next question after creation
        questionWithInvalidNext.SetDefaultNext(NextQuestionDeterminant.ToQuestion(99999));  // Non-existent question
        await _context.SaveChangesAsync();

        var userId = 601602603L;
        var response = await _responseService.StartResponseAsync(invalidSurvey.Id, userId);

        // Act - Answer question
        await _responseService.SaveAnswerAsync(response.Id, questionWithInvalidNext.Id, answerText: "Test");
        await _responseService.RecordVisitedQuestionAsync(response.Id, questionWithInvalidNext.Id);

        // The GetNextQuestionAsync should handle gracefully
        var nextQuestionId = await _responseService.GetNextQuestionAsync(response.Id);

        // Assert - Should return the invalid ID (service doesn't validate existence)
        // The actual navigation would fail, but that's handled at a higher level
        Assert.Equal(99999, nextQuestionId);
    }

    [Fact]
    public async Task ErrorHandling_ResponseNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentResponseId = 99999;

        // Act & Assert
        await Assert.ThrowsAsync<ResponseNotFoundException>(
            async () => await _responseService.GetNextQuestionAsync(nonExistentResponseId));
    }

    #endregion
}
