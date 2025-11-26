using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Enums;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Infrastructure.Repositories;
using SurveyBot.Infrastructure.Services;
using Xunit;

namespace SurveyBot.Tests.Unit.Services;

/// <summary>
/// Comprehensive unit tests for ResponseService conditional question flow logic.
/// Tests the DetermineNextQuestionIdAsync and related branching/non-branching flow methods.
/// Uses in-memory database with real repositories for integration-style testing.
/// </summary>
public class ResponseServiceConditionalFlowTests : IDisposable
{
    private readonly SurveyBotDbContext _context;
    private readonly Mock<ILogger<ResponseService>> _loggerMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ResponseService _service;
    private readonly QuestionRepository _questionRepository;
    private readonly ResponseRepository _responseRepository;
    private readonly SurveyRepository _surveyRepository;
    private readonly AnswerRepository _answerRepository;

    public ResponseServiceConditionalFlowTests()
    {
        // Use in-memory database with unique name per test instance
        var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SurveyBotDbContext(options);
        _loggerMock = new Mock<ILogger<ResponseService>>();
        _mapperMock = new Mock<IMapper>();

        // Create real repositories (not mocked)
        _questionRepository = new QuestionRepository(_context);
        _responseRepository = new ResponseRepository(_context);
        _surveyRepository = new SurveyRepository(_context);
        _answerRepository = new AnswerRepository(_context);

        _service = new ResponseService(
            _responseRepository,
            _answerRepository,
            _surveyRepository,
            _questionRepository,
            _context,
            _mapperMock.Object,
            _loggerMock.Object
        );
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Category 1: Branching Flow (SingleChoice)

    [Fact]
    public async Task BranchingFlow_OptionWithNextQuestion_SetsCorrectNextQuestionId()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = CreateSingleChoiceQuestion(survey.Id, "Choose A or B", 0, new List<string> { "Option A", "Option B" });
        var q3 = CreateTextQuestion(survey.Id, "Question 3", 2);
        var q5 = CreateTextQuestion(survey.Id, "Question 5", 4);

        // Configure option flows: Option A -> Q3, Option B -> Q5
        var optionA = q1.Options.First(o => o.Text == "Option A");
        optionA.Next = NextQuestionDeterminant.ToQuestion(q3.Id);

        var optionB = q1.Options.First(o => o.Text == "Option B");
        optionB.Next = NextQuestionDeterminant.ToQuestion(q5.Id);

        await _context.SaveChangesAsync();

        var response = CreateResponse(survey.Id, 123456);

        // Act - Answer Q1 with Option A
        var result = await _service.SaveAnswerAsync(
            response.Id,
            q1.Id,
            answerText: null,
            selectedOptions: new List<string> { "Option A" },
            ratingValue: null,
            userId: null
        );

        // Assert
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q1.Id);

        Assert.NotNull(answer);
        Assert.Equal(q3.Id, answer.NextQuestionId);
    }

    [Fact]
    public async Task BranchingFlow_OptionWithEndSurvey_SetsNextQuestionIdToZero()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = CreateSingleChoiceQuestion(survey.Id, "End after A?", 0, new List<string> { "Option A", "Option B" });

        // Configure option flow: Option A -> End Survey
        var optionA = q1.Options.First(o => o.Text == "Option A");
        optionA.Next = NextQuestionDeterminant.End();

        await _context.SaveChangesAsync();

        var response = CreateResponse(survey.Id, 123456);

        // Act - Answer Q1 with Option A (leads to end)
        var result = await _service.SaveAnswerAsync(
            response.Id,
            q1.Id,
            answerText: null,
            selectedOptions: new List<string> { "Option A" },
            ratingValue: null,
            userId: null
        );

        // Assert
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q1.Id);

        Assert.NotNull(answer);
        Assert.Equal(0, answer.NextQuestionId); // 0 = end of survey
    }

    [Fact]
    public async Task BranchingFlow_OptionWithNullFlow_FallsBackToDefaultNext()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = CreateSingleChoiceQuestion(survey.Id, "Question 1", 0, new List<string> { "Option A", "Option B" });
        var q4 = CreateTextQuestion(survey.Id, "Question 4", 3);

        // Option A has no flow configured (null), Q1 has DefaultNext = Q4
        q1.DefaultNext = NextQuestionDeterminant.ToQuestion(q4.Id);
        await _context.SaveChangesAsync();

        var response = CreateResponse(survey.Id, 123456);

        // Act - Answer Q1 with Option A (no option flow, uses default)
        var result = await _service.SaveAnswerAsync(
            response.Id,
            q1.Id,
            answerText: null,
            selectedOptions: new List<string> { "Option A" },
            ratingValue: null,
            userId: null
        );

        // Assert
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q1.Id);

        Assert.NotNull(answer);
        Assert.Equal(q4.Id, answer.NextQuestionId);
    }

    [Fact]
    public async Task BranchingFlow_NoFlowConfigured_FallsBackToSequential()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = CreateSingleChoiceQuestion(survey.Id, "Question 1", 0, new List<string> { "Option A" });
        var q2 = CreateTextQuestion(survey.Id, "Question 2", 1);
        var q3 = CreateTextQuestion(survey.Id, "Question 3", 2);

        // No flow configured at all
        await _context.SaveChangesAsync();

        var response = CreateResponse(survey.Id, 123456);

        // Act - Answer Q1 (no flow, should go sequential to Q2)
        var result = await _service.SaveAnswerAsync(
            response.Id,
            q1.Id,
            answerText: null,
            selectedOptions: new List<string> { "Option A" },
            ratingValue: null,
            userId: null
        );

        // Assert
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q1.Id);

        Assert.NotNull(answer);
        Assert.Equal(q2.Id, answer.NextQuestionId);
    }

    [Fact]
    public async Task BranchingFlow_LastQuestionNoFlow_SetsNextQuestionIdToZero()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = CreateTextQuestion(survey.Id, "Question 1", 0);
        var q2 = CreateTextQuestion(survey.Id, "Question 2", 1);
        var q3 = CreateSingleChoiceQuestion(survey.Id, "Question 3", 2, new List<string> { "Final" });

        // Q3 is last, no flow
        await _context.SaveChangesAsync();

        var response = CreateResponse(survey.Id, 123456);

        // Act - Answer Q3 (last question)
        var result = await _service.SaveAnswerAsync(
            response.Id,
            q3.Id,
            answerText: null,
            selectedOptions: new List<string> { "Final" },
            ratingValue: null,
            userId: null
        );

        // Assert
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q3.Id);

        Assert.NotNull(answer);
        Assert.Equal(0, answer.NextQuestionId); // Last question
    }

    #endregion

    #region Category 2: Non-Branching Flow (Text)

    [Fact]
    public async Task NonBranchingFlow_QuestionWithDefaultNext_SetsCorrectNextQuestionId()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = CreateTextQuestion(survey.Id, "Text Question 1", 0);
        var q5 = CreateTextQuestion(survey.Id, "Question 5", 4);

        // Q1 has DefaultNext = Q5
        q1.DefaultNext = NextQuestionDeterminant.ToQuestion(q5.Id);
        await _context.SaveChangesAsync();

        var response = CreateResponse(survey.Id, 123456);

        // Act - Answer Q1 (text question with DefaultNext)
        var result = await _service.SaveAnswerAsync(
            response.Id,
            q1.Id,
            answerText: "User's answer",
            selectedOptions: null,
            ratingValue: null,
            userId: null
        );

        // Assert
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q1.Id);

        Assert.NotNull(answer);
        Assert.Equal(q5.Id, answer.NextQuestionId);
    }

    [Fact]
    public async Task NonBranchingFlow_NoDefaultNext_FallsBackToSequential()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = CreateTextQuestion(survey.Id, "Question 1", 0);
        var q2 = CreateTextQuestion(survey.Id, "Question 2", 1);
        var q3 = CreateTextQuestion(survey.Id, "Question 3", 2);

        // No DefaultNext configured
        await _context.SaveChangesAsync();

        var response = CreateResponse(survey.Id, 123456);

        // Act - Answer Q1 (no DefaultNext, sequential fallback)
        var result = await _service.SaveAnswerAsync(
            response.Id,
            q1.Id,
            answerText: "User's answer",
            selectedOptions: null,
            ratingValue: null,
            userId: null
        );

        // Assert
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q1.Id);

        Assert.NotNull(answer);
        Assert.Equal(q2.Id, answer.NextQuestionId);
    }

    [Fact]
    public async Task NonBranchingFlow_LastQuestionNoDefaultNext_SetsNextQuestionIdToZero()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = CreateTextQuestion(survey.Id, "Question 1", 0);

        // Q1 is last, no DefaultNext
        await _context.SaveChangesAsync();

        var response = CreateResponse(survey.Id, 123456);

        // Act - Answer Q1 (last question)
        var result = await _service.SaveAnswerAsync(
            response.Id,
            q1.Id,
            answerText: "User's answer",
            selectedOptions: null,
            ratingValue: null,
            userId: null
        );

        // Assert
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q1.Id);

        Assert.NotNull(answer);
        Assert.Equal(0, answer.NextQuestionId);
    }

    #endregion

    #region Category 3: Edge Cases

    [Fact]
    public async Task EdgeCase_InvalidOptionIndex_SetsNextQuestionIdToZero()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = CreateSingleChoiceQuestion(survey.Id, "Question 1", 0, new List<string> { "A", "B", "C" });

        await _context.SaveChangesAsync();

        var response = CreateResponse(survey.Id, 123456);

        // Act - Answer Q1 with invalid option (not in list)
        var result = await _service.SaveAnswerAsync(
            response.Id,
            q1.Id,
            answerText: null,
            selectedOptions: new List<string> { "Invalid Option X" }, // Not in A, B, C
            ratingValue: null,
            userId: null
        );

        // Assert
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q1.Id);

        Assert.NotNull(answer);
        Assert.Equal(0, answer.NextQuestionId); // Graceful failure
    }

    [Fact]
    public async Task EdgeCase_EmptySelectedOptions_SetsNextQuestionIdToZero()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = CreateSingleChoiceQuestion(survey.Id, "Question 1", 0, new List<string> { "A", "B" });

        await _context.SaveChangesAsync();

        var response = CreateResponse(survey.Id, 123456);

        // Act - Answer Q1 with empty options list
        var result = await _service.SaveAnswerAsync(
            response.Id,
            q1.Id,
            answerText: null,
            selectedOptions: new List<string>(), // Empty!
            ratingValue: null,
            userId: null
        );

        // Assert
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q1.Id);

        Assert.NotNull(answer);
        Assert.Equal(0, answer.NextQuestionId);
    }

    [Fact]
    public async Task EdgeCase_QuestionWithoutOptions_FallsBackToDefaultNext()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = new Question
        {
            SurveyId = survey.Id,
            QuestionText = "Question 1",
            QuestionType = QuestionType.SingleChoice,
            IsRequired = true,
            OrderIndex = 0,
            Options = new List<QuestionOption>() // Empty options!
        };
        _context.Questions.Add(q1);

        var q2 = CreateTextQuestion(survey.Id, "Question 2", 1);

        // Set DefaultNext as fallback
        q1.DefaultNext = NextQuestionDeterminant.ToQuestion(q2.Id);
        await _context.SaveChangesAsync();

        var response = CreateResponse(survey.Id, 123456);

        // Act - Answer Q1 (single choice with no options, uses DefaultNext)
        var result = await _service.SaveAnswerAsync(
            response.Id,
            q1.Id,
            answerText: null,
            selectedOptions: new List<string> { "Something" },
            ratingValue: null,
            userId: null
        );

        // Assert
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q1.Id);

        Assert.NotNull(answer);
        Assert.Equal(q2.Id, answer.NextQuestionId);
    }

    #endregion

    #region Category 4: Complex Flow Scenarios

    [Fact]
    public async Task ComplexFlow_MultipleBranchesConverging_WorksCorrectly()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = CreateSingleChoiceQuestion(survey.Id, "Question 1", 0, new List<string> { "A", "B" });
        var q3 = CreateTextQuestion(survey.Id, "Question 3", 2);

        // Both options lead to same question Q3
        var optionA = q1.Options.First(o => o.Text == "A");
        optionA.Next = NextQuestionDeterminant.ToQuestion(q3.Id);

        var optionB = q1.Options.First(o => o.Text == "B");
        optionB.Next = NextQuestionDeterminant.ToQuestion(q3.Id);

        await _context.SaveChangesAsync();

        var responseA = CreateResponse(survey.Id, 111111);
        var responseB = CreateResponse(survey.Id, 222222);

        // Act - Answer Q1 with Option A
        await _service.SaveAnswerAsync(
            responseA.Id,
            q1.Id,
            answerText: null,
            selectedOptions: new List<string> { "A" },
            ratingValue: null,
            userId: null
        );

        // Act - Answer Q1 with Option B in separate response
        await _service.SaveAnswerAsync(
            responseB.Id,
            q1.Id,
            answerText: null,
            selectedOptions: new List<string> { "B" },
            ratingValue: null,
            userId: null
        );

        // Assert
        var answerA = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == responseA.Id && a.QuestionId == q1.Id);
        var answerB = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == responseB.Id && a.QuestionId == q1.Id);

        Assert.NotNull(answerA);
        Assert.Equal(q3.Id, answerA.NextQuestionId);

        Assert.NotNull(answerB);
        Assert.Equal(q3.Id, answerB.NextQuestionId);
    }

    [Fact]
    public async Task ComplexFlow_ChainedConditionalFlow_FollowsMultipleBranches()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = CreateSingleChoiceQuestion(survey.Id, "Question 1", 0, new List<string> { "A", "B" });
        var q2 = CreateSingleChoiceQuestion(survey.Id, "Question 2", 1, new List<string> { "X", "Y" });
        var q5 = CreateTextQuestion(survey.Id, "Question 5", 4);

        // Q1 Option A -> Q2
        var optionA = q1.Options.First(o => o.Text == "A");
        optionA.Next = NextQuestionDeterminant.ToQuestion(q2.Id);

        // Q2 Option X -> Q5
        var optionX = q2.Options.First(o => o.Text == "X");
        optionX.Next = NextQuestionDeterminant.ToQuestion(q5.Id);

        await _context.SaveChangesAsync();

        var response = CreateResponse(survey.Id, 123456);

        // Act - Answer Q1 with Option A
        await _service.SaveAnswerAsync(
            response.Id,
            q1.Id,
            answerText: null,
            selectedOptions: new List<string> { "A" },
            ratingValue: null,
            userId: null
        );

        // Act - Answer Q2 with Option X
        await _service.SaveAnswerAsync(
            response.Id,
            q2.Id,
            answerText: null,
            selectedOptions: new List<string> { "X" },
            ratingValue: null,
            userId: null
        );

        // Assert
        var answer1 = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q1.Id);
        var answer2 = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q2.Id);

        Assert.NotNull(answer1);
        Assert.Equal(q2.Id, answer1.NextQuestionId);

        Assert.NotNull(answer2);
        Assert.Equal(q5.Id, answer2.NextQuestionId);
    }

    [Fact]
    public async Task ComplexFlow_MixedFlowTypes_WorksCorrectly()
    {
        // Arrange
        var survey = CreateSurvey();
        var q1 = CreateSingleChoiceQuestion(survey.Id, "Question 1", 0, new List<string> { "A" });
        var q2 = CreateTextQuestion(survey.Id, "Question 2", 1);
        var q3 = CreateTextQuestion(survey.Id, "Question 3", 2);

        // Q1 (branching) -> Q2
        var optionA = q1.Options.First(o => o.Text == "A");
        optionA.Next = NextQuestionDeterminant.ToQuestion(q2.Id);

        // Q2 (non-branching) -> Q3
        q2.DefaultNext = NextQuestionDeterminant.ToQuestion(q3.Id);

        await _context.SaveChangesAsync();

        var response = CreateResponse(survey.Id, 123456);

        // Act - Answer Q1 (uses conditional flow)
        await _service.SaveAnswerAsync(
            response.Id,
            q1.Id,
            answerText: null,
            selectedOptions: new List<string> { "A" },
            ratingValue: null,
            userId: null
        );

        // Act - Answer Q2 (uses DefaultNext)
        await _service.SaveAnswerAsync(
            response.Id,
            q2.Id,
            answerText: "User's text",
            selectedOptions: null,
            ratingValue: null,
            userId: null
        );

        // Assert
        var answer1 = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q1.Id);
        var answer2 = await _context.Answers
            .FirstOrDefaultAsync(a => a.ResponseId == response.Id && a.QuestionId == q2.Id);

        Assert.NotNull(answer1);
        Assert.Equal(q2.Id, answer1.NextQuestionId);

        Assert.NotNull(answer2);
        Assert.Equal(q3.Id, answer2.NextQuestionId);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a survey with default values.
    /// </summary>
    private Survey CreateSurvey()
    {
        var survey = new Survey
        {
            Title = "Test Survey",
            Code = "TST001",
            IsActive = true,
            CreatorId = 1,
            AllowMultipleResponses = true
        };
        _context.Surveys.Add(survey);
        _context.SaveChanges();
        return survey;
    }

    /// <summary>
    /// Creates a single-choice question with options.
    /// </summary>
    private Question CreateSingleChoiceQuestion(int surveyId, string text, int orderIndex, List<string> options)
    {
        var question = new Question
        {
            SurveyId = surveyId,
            QuestionText = text,
            QuestionType = QuestionType.SingleChoice,
            IsRequired = true,
            OrderIndex = orderIndex
        };
        _context.Questions.Add(question);
        _context.SaveChanges();

        // Add options
        for (int i = 0; i < options.Count; i++)
        {
            var option = new QuestionOption
            {
                QuestionId = question.Id,
                Text = options[i],
                OrderIndex = i
            };
            _context.QuestionOptions.Add(option);
        }
        _context.SaveChanges();

        // Reload question with options
        return _context.Questions
            .Include(q => q.Options)
            .First(q => q.Id == question.Id);
    }

    /// <summary>
    /// Creates a text question.
    /// </summary>
    private Question CreateTextQuestion(int surveyId, string text, int orderIndex)
    {
        var question = new Question
        {
            SurveyId = surveyId,
            QuestionText = text,
            QuestionType = QuestionType.Text,
            IsRequired = true,
            OrderIndex = orderIndex
        };
        _context.Questions.Add(question);
        _context.SaveChanges();
        return question;
    }

    /// <summary>
    /// Creates a response for testing.
    /// </summary>
    private Response CreateResponse(int surveyId, long telegramUserId)
    {
        var response = new Response
        {
            SurveyId = surveyId,
            RespondentTelegramId = telegramUserId,
            IsComplete = false,
            StartedAt = DateTime.UtcNow
        };
        _context.Responses.Add(response);
        _context.SaveChanges();
        return response;
    }

    #endregion
}
