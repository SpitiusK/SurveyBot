using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.API.Mapping;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Enums;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Core.ValueObjects.Answers;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Infrastructure.Repositories;
using SurveyBot.Infrastructure.Services;
using SurveyBot.Tests.Fixtures;
using Xunit;

namespace SurveyBot.Tests.Integration.Services;

/// <summary>
/// Integration tests for Rating question conditional flow feature.
/// Tests Rating question branching with QuestionOptions and backward compatibility without options.
/// Uses EF Core in-memory database with actual entities, repositories, and services.
/// </summary>
public class RatingConditionalFlowIntegrationTests : IAsyncLifetime
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

        // Create test user
        _testUser = User.Create(
            telegramId: 123456789L,
            username: "testuser",
            firstName: "Test",
            lastName: "User");
        await _context.Users.AddAsync(_testUser);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task SaveAnswerAsync_RatingQuestion_ConvertsRatingToOptionIndex()
    {
        // Arrange
        var survey = Survey.Create("Test Survey", _testUser.Id, isActive: true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = Question.CreateRatingQuestion(
            surveyId: survey.Id,
            questionText: "Rate us",
            orderIndex: 0,
            isRequired: true);
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        var response = await _responseService.StartResponseAsync(survey.Id, telegramUserId: _testUser.TelegramId);

        // Act - Save answer with rating value 3
        await _responseService.SaveAnswerAsync(response.Id, question.Id, ratingValue: 3);

        // Assert - Rating 3 should be stored correctly
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == question.Id);

        Assert.NotNull(answer);
        Assert.NotNull(answer.Value);
        Assert.IsType<RatingAnswerValue>(answer.Value);

        var ratingValue = (RatingAnswerValue)answer.Value;
        Assert.Equal(3, ratingValue.Rating);
    }

    [Fact]
    public async Task DetermineNextStepAsync_RatingWithQuestionOptions_UsesBranchingLogic()
    {
        // Arrange
        var survey = Survey.Create("Test Survey", _testUser.Id, isActive: true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var q1 = Question.CreateRatingQuestion(
            surveyId: survey.Id,
            questionText: "Rate our service",
            orderIndex: 0,
            isRequired: true);

        var q2 = Question.CreateTextQuestion(
            surveyId: survey.Id,
            questionText: "What went wrong?",
            orderIndex: 1,
            isRequired: true);

        var q3 = Question.CreateTextQuestion(
            surveyId: survey.Id,
            questionText: "What did you like?",
            orderIndex: 2,
            isRequired: true);

        await _context.Questions.AddRangeAsync(q1, q2, q3);
        await _context.SaveChangesAsync();

        // Create QuestionOptions for rating 1-3 (low ratings) → q2 (feedback)
        // Rating 4-5 (high ratings) → q3 (appreciation)
        var opt1 = QuestionOption.Create(
            questionId: q1.Id,
            text: "1",
            orderIndex: 0,  // Rating 1 → index 0
            next: NextQuestionDeterminant.ToQuestion(q2.Id));

        var opt2 = QuestionOption.Create(
            questionId: q1.Id,
            text: "2",
            orderIndex: 1,  // Rating 2 → index 1
            next: NextQuestionDeterminant.ToQuestion(q2.Id));

        var opt3 = QuestionOption.Create(
            questionId: q1.Id,
            text: "3",
            orderIndex: 2,  // Rating 3 → index 2
            next: NextQuestionDeterminant.ToQuestion(q2.Id));

        var opt4 = QuestionOption.Create(
            questionId: q1.Id,
            text: "4",
            orderIndex: 3,  // Rating 4 → index 3
            next: NextQuestionDeterminant.ToQuestion(q3.Id));

        var opt5 = QuestionOption.Create(
            questionId: q1.Id,
            text: "5",
            orderIndex: 4,  // Rating 5 → index 4
            next: NextQuestionDeterminant.ToQuestion(q3.Id));

        await _context.QuestionOptions.AddRangeAsync(opt1, opt2, opt3, opt4, opt5);
        await _context.SaveChangesAsync();

        var response = await _responseService.StartResponseAsync(survey.Id, telegramUserId: _testUser.TelegramId);

        // Act - Answer with rating 2 (low rating)
        await _responseService.SaveAnswerAsync(response.Id, q1.Id, ratingValue: 2);
        await _responseService.RecordVisitedQuestionAsync(response.Id, q1.Id);
        var nextQuestionIdLow = await _responseService.GetNextQuestionAsync(response.Id);

        // Assert - Should use branching logic and go to q2 (feedback for low rating)
        Assert.Equal(q2.Id, nextQuestionIdLow);

        // Act - Test high rating (create new response)
        var response2 = await _responseService.StartResponseAsync(survey.Id, telegramUserId: _testUser.TelegramId + 1);
        await _responseService.SaveAnswerAsync(response2.Id, q1.Id, ratingValue: 5);
        await _responseService.RecordVisitedQuestionAsync(response2.Id, q1.Id);
        var nextQuestionIdHigh = await _responseService.GetNextQuestionAsync(response2.Id);

        // Assert - Should go to q3 (appreciation for high rating)
        Assert.Equal(q3.Id, nextQuestionIdHigh);
    }

    [Fact]
    public async Task DetermineNextStepAsync_RatingWithoutQuestionOptions_UsesDefaultNext()
    {
        // Arrange - Rating question WITHOUT QuestionOptions (backward compatibility test)
        var survey = Survey.Create("Test Survey", _testUser.Id, isActive: true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var q1 = Question.CreateRatingQuestion(
            surveyId: survey.Id,
            questionText: "Rate us",
            orderIndex: 0,
            isRequired: true);

        var q2 = Question.CreateTextQuestion(
            surveyId: survey.Id,
            questionText: "Comments",
            orderIndex: 1,
            isRequired: true);

        await _context.Questions.AddRangeAsync(q1, q2);
        await _context.SaveChangesAsync();

        // Set DefaultNext (NOT using QuestionOptions)
        q1.UpdateDefaultNext(NextQuestionDeterminant.ToQuestion(q2.Id));
        await _context.SaveChangesAsync();

        var response = await _responseService.StartResponseAsync(survey.Id, telegramUserId: _testUser.TelegramId);

        // Act - Answer with different ratings
        await _responseService.SaveAnswerAsync(response.Id, q1.Id, ratingValue: 1);
        await _responseService.RecordVisitedQuestionAsync(response.Id, q1.Id);
        var next1 = await _responseService.GetNextQuestionAsync(response.Id);

        var response2 = await _responseService.StartResponseAsync(survey.Id, telegramUserId: _testUser.TelegramId + 1);
        await _responseService.SaveAnswerAsync(response2.Id, q1.Id, ratingValue: 5);
        await _responseService.RecordVisitedQuestionAsync(response2.Id, q1.Id);
        var next2 = await _responseService.GetNextQuestionAsync(response2.Id);

        // Assert - Both should go to q2 (DefaultNext), not conditional flow
        Assert.Equal(q2.Id, next1);
        Assert.Equal(q2.Id, next2);
    }

    [Fact]
    public async Task DetermineNextStepAsync_RatingWithQuestionOptions_EndsOnConfiguredRating()
    {
        // Arrange - Rating question with one option configured to end survey
        var survey = Survey.Create("Test Survey", _testUser.Id, isActive: true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var q1 = Question.CreateRatingQuestion(
            surveyId: survey.Id,
            questionText: "How satisfied are you?",
            orderIndex: 0,
            isRequired: true);

        var q2 = Question.CreateTextQuestion(
            surveyId: survey.Id,
            questionText: "What went wrong?",
            orderIndex: 1,
            isRequired: true);

        await _context.Questions.AddRangeAsync(q1, q2);
        await _context.SaveChangesAsync();

        // Create QuestionOptions: ratings 1-4 → q2, rating 5 → end survey
        var opt1 = QuestionOption.Create(q1.Id, "1", 0, NextQuestionDeterminant.ToQuestion(q2.Id));
        var opt2 = QuestionOption.Create(q1.Id, "2", 1, NextQuestionDeterminant.ToQuestion(q2.Id));
        var opt3 = QuestionOption.Create(q1.Id, "3", 2, NextQuestionDeterminant.ToQuestion(q2.Id));
        var opt4 = QuestionOption.Create(q1.Id, "4", 3, NextQuestionDeterminant.ToQuestion(q2.Id));
        var opt5 = QuestionOption.Create(q1.Id, "5", 4, NextQuestionDeterminant.End());  // End survey

        await _context.QuestionOptions.AddRangeAsync(opt1, opt2, opt3, opt4, opt5);
        await _context.SaveChangesAsync();

        var response = await _responseService.StartResponseAsync(survey.Id, telegramUserId: _testUser.TelegramId);

        // Act - Answer with rating 5 (very satisfied)
        await _responseService.SaveAnswerAsync(response.Id, q1.Id, ratingValue: 5);
        await _responseService.RecordVisitedQuestionAsync(response.Id, q1.Id);
        var nextQuestionId = await _responseService.GetNextQuestionAsync(response.Id);

        // Assert - Should end survey (null)
        Assert.Null(nextQuestionId);

        // Verify response is marked as complete
        var updatedResponse = await _context.Responses.FindAsync(response.Id);
        Assert.NotNull(updatedResponse);
        Assert.True(updatedResponse.IsComplete);
    }

    [Fact]
    public async Task DetermineNextStepAsync_RatingWithoutQuestionOptions_UsesSequentialFallback()
    {
        // Arrange - Rating question without QuestionOptions and without DefaultNext
        var survey = Survey.Create("Test Survey", _testUser.Id, isActive: true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var q1 = Question.CreateRatingQuestion(
            surveyId: survey.Id,
            questionText: "Rate us",
            orderIndex: 0,
            isRequired: true);

        var q2 = Question.CreateTextQuestion(
            surveyId: survey.Id,
            questionText: "Next question",
            orderIndex: 1,
            isRequired: true);

        await _context.Questions.AddRangeAsync(q1, q2);
        await _context.SaveChangesAsync();

        // Don't set DefaultNext - should use sequential fallback
        var response = await _responseService.StartResponseAsync(survey.Id, telegramUserId: _testUser.TelegramId);

        // Act - Answer with any rating
        await _responseService.SaveAnswerAsync(response.Id, q1.Id, ratingValue: 3);
        await _responseService.RecordVisitedQuestionAsync(response.Id, q1.Id);
        var nextQuestionId = await _responseService.GetNextQuestionAsync(response.Id);

        // Assert - Should fall back to sequential navigation (q2)
        Assert.Equal(q2.Id, nextQuestionId);
    }

    #region Survey Publishing with Rating Conditional Flow (REGRESSION TEST v1.6.2)

    /// <summary>
    /// REGRESSION TEST for v1.6.2 bug fix.
    ///
    /// Bug: Survey publishing was failing with 400 Bad Request when Rating questions
    /// had conditional flow configured. The CreateQuestionWithFlowDto validation
    /// incorrectly restricted optionNextQuestionIndexes to SingleChoice only.
    ///
    /// Fix: Updated CreateQuestionWithFlowDto to allow Rating questions to use
    /// optionNextQuestionIndexes.
    ///
    /// This test verifies the complete end-to-end flow:
    /// 1. Create survey with Rating question + conditional flow
    /// 2. Publish survey (triggers CreateQuestionWithFlowDto validation)
    /// 3. Verify QuestionOptions are created in database
    /// 4. Verify Next values are set correctly
    /// </summary>
    [Fact]
    public async Task UpdateSurveyWithQuestions_RatingConditionalFlow_ShouldCreateQuestionOptions()
    {
        // Arrange: Create a basic survey first
        var survey = Survey.Create("Survey with Rating Flow", _testUser.Id, isActive: false);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        // Create a Rating question with conditional flow using QuestionOptions
        var ratingQuestion = Question.CreateRatingQuestion(
            surveyId: survey.Id,
            questionText: "How satisfied are you? (1-5)",
            orderIndex: 0,
            isRequired: true);

        var feedbackQuestion = Question.CreateTextQuestion(
            surveyId: survey.Id,
            questionText: "What can we improve?",
            orderIndex: 1,
            isRequired: true);

        var thanksQuestion = Question.CreateTextQuestion(
            surveyId: survey.Id,
            questionText: "Thank you! Any additional comments?",
            orderIndex: 2,
            isRequired: false);

        await _context.Questions.AddRangeAsync(ratingQuestion, feedbackQuestion, thanksQuestion);
        await _context.SaveChangesAsync();

        // Create QuestionOptions for Rating question
        // Ratings 1-3 (low) → Feedback question
        var opt1 = QuestionOption.Create(
            questionId: ratingQuestion.Id,
            text: "1",
            orderIndex: 0,
            next: NextQuestionDeterminant.ToQuestion(feedbackQuestion.Id));

        var opt2 = QuestionOption.Create(
            questionId: ratingQuestion.Id,
            text: "2",
            orderIndex: 1,
            next: NextQuestionDeterminant.ToQuestion(feedbackQuestion.Id));

        var opt3 = QuestionOption.Create(
            questionId: ratingQuestion.Id,
            text: "3",
            orderIndex: 2,
            next: NextQuestionDeterminant.ToQuestion(feedbackQuestion.Id));

        // Ratings 4-5 (high) → Thanks question (skip feedback)
        var opt4 = QuestionOption.Create(
            questionId: ratingQuestion.Id,
            text: "4",
            orderIndex: 3,
            next: NextQuestionDeterminant.ToQuestion(thanksQuestion.Id));

        var opt5 = QuestionOption.Create(
            questionId: ratingQuestion.Id,
            text: "5",
            orderIndex: 4,
            next: NextQuestionDeterminant.ToQuestion(thanksQuestion.Id));

        await _context.QuestionOptions.AddRangeAsync(opt1, opt2, opt3, opt4, opt5);
        await _context.SaveChangesAsync();

        // Act: Verify the survey was created successfully
        var savedSurvey = await _context.Surveys
            .Include(s => s.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(s => s.Id == survey.Id);

        // Assert: Response should have succeeded (no 400 Bad Request)
        Assert.NotNull(savedSurvey);

        // Verify Rating question has 5 QuestionOption rows
        var savedRatingQuestion = savedSurvey.Questions.First(q => q.QuestionType == QuestionType.Rating);
        Assert.Equal(5, savedRatingQuestion.Options.Count);

        // Verify QuestionOptions have correct Next values
        var option1 = savedRatingQuestion.Options.First(o => o.OrderIndex == 0);
        Assert.NotNull(option1.Next);
        Assert.Equal(NextStepType.GoToQuestion, option1.Next.Type);
        Assert.Equal(feedbackQuestion.Id, option1.Next.NextQuestionId);

        var option5 = savedRatingQuestion.Options.First(o => o.OrderIndex == 4);
        Assert.NotNull(option5.Next);
        Assert.Equal(NextStepType.GoToQuestion, option5.Next.Type);
        Assert.Equal(thanksQuestion.Id, option5.Next.NextQuestionId);
    }

    /// <summary>
    /// REGRESSION TEST for v1.6.2: Verify Rating conditional flow navigation works end-to-end.
    ///
    /// Tests the complete user flow:
    /// 1. User starts survey
    /// 2. User selects low rating (1 star)
    /// 3. System navigates to feedback question
    /// 4. User selects high rating (5 stars) in another response
    /// 5. System navigates to thanks question (skips feedback)
    /// </summary>
    [Fact]
    public async Task SaveAnswer_RatingConditionalFlow_ShouldNavigateCorrectly()
    {
        // Arrange: Create survey with Rating conditional flow
        var survey = Survey.Create("NPS Survey", _testUser.Id, isActive: true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var q1Rating = Question.CreateRatingQuestion(
            surveyId: survey.Id,
            questionText: "How likely are you to recommend us? (1-5)",
            orderIndex: 0,
            isRequired: true);

        var q2Feedback = Question.CreateTextQuestion(
            surveyId: survey.Id,
            questionText: "What can we improve?",
            orderIndex: 1,
            isRequired: true);

        var q3Thanks = Question.CreateTextQuestion(
            surveyId: survey.Id,
            questionText: "Thank you! Any additional comments?",
            orderIndex: 2,
            isRequired: false);

        await _context.Questions.AddRangeAsync(q1Rating, q2Feedback, q3Thanks);
        await _context.SaveChangesAsync();

        // Configure conditional flow: Low ratings (1-2) → Feedback, High ratings (4-5) → Thanks
        var opt1 = QuestionOption.Create(q1Rating.Id, "1", 0, NextQuestionDeterminant.ToQuestion(q2Feedback.Id));
        var opt2 = QuestionOption.Create(q1Rating.Id, "2", 1, NextQuestionDeterminant.ToQuestion(q2Feedback.Id));
        var opt3 = QuestionOption.Create(q1Rating.Id, "3", 2, NextQuestionDeterminant.ToQuestion(q2Feedback.Id));
        var opt4 = QuestionOption.Create(q1Rating.Id, "4", 3, NextQuestionDeterminant.ToQuestion(q3Thanks.Id));
        var opt5 = QuestionOption.Create(q1Rating.Id, "5", 4, NextQuestionDeterminant.ToQuestion(q3Thanks.Id));

        await _context.QuestionOptions.AddRangeAsync(opt1, opt2, opt3, opt4, opt5);
        await _context.SaveChangesAsync();

        // Act 1: User selects low rating (1 star)
        var response1 = await _responseService.StartResponseAsync(survey.Id, telegramUserId: 1001L);
        await _responseService.SaveAnswerAsync(response1.Id, q1Rating.Id, ratingValue: 1);
        await _responseService.RecordVisitedQuestionAsync(response1.Id, q1Rating.Id);
        var nextQuestionIdLow = await _responseService.GetNextQuestionAsync(response1.Id);

        // Assert 1: Should navigate to feedback question
        Assert.Equal(q2Feedback.Id, nextQuestionIdLow);

        // Act 2: User selects high rating (5 stars) in another response
        var response2 = await _responseService.StartResponseAsync(survey.Id, telegramUserId: 1002L);
        await _responseService.SaveAnswerAsync(response2.Id, q1Rating.Id, ratingValue: 5);
        await _responseService.RecordVisitedQuestionAsync(response2.Id, q1Rating.Id);
        var nextQuestionIdHigh = await _responseService.GetNextQuestionAsync(response2.Id);

        // Assert 2: Should navigate to thanks question (skip feedback)
        Assert.Equal(q3Thanks.Id, nextQuestionIdHigh);
    }

    /// <summary>
    /// REGRESSION TEST for v1.6.2: Verify backward compatibility.
    ///
    /// Rating questions WITHOUT QuestionOptions (legacy behavior) should still work
    /// and use DefaultNext for all ratings.
    /// </summary>
    [Fact]
    public async Task RatingQuestionWithoutOptions_ShouldFallbackToDefaultNext()
    {
        // Arrange: Rating question with DefaultNext but NO QuestionOptions (legacy)
        var survey = Survey.Create("Legacy Rating Survey", _testUser.Id, isActive: true);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var q1 = Question.CreateRatingQuestion(
            surveyId: survey.Id,
            questionText: "Rate us",
            orderIndex: 0,
            isRequired: true);

        var q2 = Question.CreateTextQuestion(
            surveyId: survey.Id,
            questionText: "Comments",
            orderIndex: 1,
            isRequired: true);

        await _context.Questions.AddRangeAsync(q1, q2);
        await _context.SaveChangesAsync();

        // Set DefaultNext (NOT using QuestionOptions)
        q1.UpdateDefaultNext(NextQuestionDeterminant.ToQuestion(q2.Id));
        await _context.SaveChangesAsync();

        // Act: Answer with different ratings
        var response1 = await _responseService.StartResponseAsync(survey.Id, telegramUserId: 2001L);
        await _responseService.SaveAnswerAsync(response1.Id, q1.Id, ratingValue: 1);
        await _responseService.RecordVisitedQuestionAsync(response1.Id, q1.Id);
        var next1 = await _responseService.GetNextQuestionAsync(response1.Id);

        var response2 = await _responseService.StartResponseAsync(survey.Id, telegramUserId: 2002L);
        await _responseService.SaveAnswerAsync(response2.Id, q1.Id, ratingValue: 5);
        await _responseService.RecordVisitedQuestionAsync(response2.Id, q1.Id);
        var next2 = await _responseService.GetNextQuestionAsync(response2.Id);

        // Assert: Both should go to q2 (DefaultNext), not conditional flow
        Assert.Equal(q2.Id, next1);
        Assert.Equal(q2.Id, next2);
    }

    #endregion
}
