using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Services;
using SurveyBot.Tests.Fixtures;
using Xunit;

namespace SurveyBot.Tests.Unit.Services;

/// <summary>
/// Unit tests for QuestionService.
/// </summary>
public class QuestionServiceTests
{
    private readonly Mock<IQuestionRepository> _questionRepositoryMock;
    private readonly Mock<ISurveyRepository> _surveyRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<QuestionService>> _loggerMock;
    private readonly QuestionService _sut;

    public QuestionServiceTests()
    {
        _questionRepositoryMock = new Mock<IQuestionRepository>();
        _surveyRepositoryMock = new Mock<ISurveyRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<QuestionService>>();

        _sut = new QuestionService(
            _questionRepositoryMock.Object,
            _surveyRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    #region AddQuestionAsync Tests

    [Fact]
    public async Task AddQuestionAsync_WithValidTextQuestion_CreatesQuestionSuccessfully()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;
        var dto = new CreateQuestionDto
        {
            QuestionText = "What is your name?",
            QuestionType = QuestionType.Text,
            IsRequired = true,
            Options = null
        };

        var survey = EntityBuilder.CreateSurvey(creatorId: userId);
        survey.SetId(surveyId);

        var question = EntityBuilder.CreateQuestion(
            surveyId: surveyId,
            questionText: dto.QuestionText,
            questionType: dto.QuestionType,
            orderIndex: 0,
            isRequired: dto.IsRequired);
        question.SetId(1);

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId))
            .ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.HasResponsesAsync(surveyId))
            .ReturnsAsync(false);
        _questionRepositoryMock.Setup(r => r.GetNextOrderIndexAsync(surveyId))
            .ReturnsAsync(0);
        _questionRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Question>()))
            .ReturnsAsync(question);

        // Act
        var result = await _sut.AddQuestionAsync(surveyId, userId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.QuestionText, result.QuestionText);
        Assert.Equal(QuestionType.Text, result.QuestionType);
        Assert.True(result.IsRequired);
        Assert.Null(result.Options);

        _questionRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Question>()), Times.Once);
    }

    [Fact]
    public async Task AddQuestionAsync_WithValidSingleChoiceQuestion_CreatesQuestionSuccessfully()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;
        var dto = new CreateQuestionDto
        {
            QuestionText = "What is your favorite color?",
            QuestionType = QuestionType.SingleChoice,
            IsRequired = true,
            Options = new List<string> { "Red", "Blue", "Green" }
        };

        var survey = EntityBuilder.CreateSurvey(creatorId: userId);
        survey.SetId(surveyId);

        var question = EntityBuilder.CreateQuestion(
            surveyId: surveyId,
            questionText: dto.QuestionText,
            questionType: dto.QuestionType,
            orderIndex: 0,
            isRequired: dto.IsRequired);
        question.SetId(1);
        question.SetOptionsJson("[\"Red\",\"Blue\",\"Green\"]");

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId))
            .ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.HasResponsesAsync(surveyId))
            .ReturnsAsync(false);
        _questionRepositoryMock.Setup(r => r.GetNextOrderIndexAsync(surveyId))
            .ReturnsAsync(0);
        _questionRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Question>()))
            .ReturnsAsync(question);

        // Act
        var result = await _sut.AddQuestionAsync(surveyId, userId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.QuestionText, result.QuestionText);
        Assert.Equal(QuestionType.SingleChoice, result.QuestionType);
        Assert.NotNull(result.Options);
        Assert.Equal(3, result.Options.Count);

        _questionRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Question>()), Times.Once);
    }

    [Fact]
    public async Task AddQuestionAsync_WithValidRatingQuestion_CreatesQuestionSuccessfully()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;
        var dto = new CreateQuestionDto
        {
            QuestionText = "Rate our service",
            QuestionType = QuestionType.Rating,
            IsRequired = true,
            Options = null
        };

        var survey = EntityBuilder.CreateSurvey(creatorId: userId);
        survey.SetId(surveyId);

        var question = EntityBuilder.CreateQuestion(
            surveyId: surveyId,
            questionText: dto.QuestionText,
            questionType: dto.QuestionType,
            orderIndex: 0,
            isRequired: dto.IsRequired);
        question.SetId(1);

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId))
            .ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.HasResponsesAsync(surveyId))
            .ReturnsAsync(false);
        _questionRepositoryMock.Setup(r => r.GetNextOrderIndexAsync(surveyId))
            .ReturnsAsync(0);
        _questionRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Question>()))
            .ReturnsAsync(question);

        // Act
        var result = await _sut.AddQuestionAsync(surveyId, userId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.QuestionText, result.QuestionText);
        Assert.Equal(QuestionType.Rating, result.QuestionType);
        Assert.Null(result.Options);

        _questionRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Question>()), Times.Once);
    }

    [Fact]
    public async Task AddQuestionAsync_WithNonExistentSurvey_ThrowsSurveyNotFoundException()
    {
        // Arrange
        var surveyId = 999;
        var userId = 1;
        var dto = new CreateQuestionDto
        {
            QuestionText = "Test question",
            QuestionType = QuestionType.Text,
            IsRequired = true
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId))
            .ReturnsAsync((Survey?)null);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyNotFoundException>(
            () => _sut.AddQuestionAsync(surveyId, userId, dto));
    }

    [Fact]
    public async Task AddQuestionAsync_WithUnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var surveyId = 1;
        var userId = 2; // Different user
        var dto = new CreateQuestionDto
        {
            QuestionText = "Test question",
            QuestionType = QuestionType.Text,
            IsRequired = true
        };

        var survey = EntityBuilder.CreateSurvey(creatorId: 1); // Owned by user 1
        survey.SetId(surveyId);

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId))
            .ReturnsAsync(survey);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyBot.Core.Exceptions.UnauthorizedAccessException>(
            () => _sut.AddQuestionAsync(surveyId, userId, dto));
    }

    [Fact]
    public async Task AddQuestionAsync_WhenSurveyHasResponses_ThrowsSurveyOperationException()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;
        var dto = new CreateQuestionDto
        {
            QuestionText = "Test question",
            QuestionType = QuestionType.Text,
            IsRequired = true
        };

        var survey = EntityBuilder.CreateSurvey(creatorId: userId);
        survey.SetId(surveyId);

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId))
            .ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.HasResponsesAsync(surveyId))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyOperationException>(
            () => _sut.AddQuestionAsync(surveyId, userId, dto));
    }

    [Fact]
    public async Task AddQuestionAsync_WithSingleChoiceAndNoOptions_ThrowsQuestionValidationException()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;
        var dto = new CreateQuestionDto
        {
            QuestionText = "Test question",
            QuestionType = QuestionType.SingleChoice,
            IsRequired = true,
            Options = null
        };

        var survey = EntityBuilder.CreateSurvey(creatorId: userId);
        survey.SetId(surveyId);

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId))
            .ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.HasResponsesAsync(surveyId))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<QuestionValidationException>(
            () => _sut.AddQuestionAsync(surveyId, userId, dto));
    }

    [Fact]
    public async Task AddQuestionAsync_WithTextQuestionAndOptions_ThrowsQuestionValidationException()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;
        var dto = new CreateQuestionDto
        {
            QuestionText = "Test question",
            QuestionType = QuestionType.Text,
            IsRequired = true,
            Options = new List<string> { "Option1", "Option2" }
        };

        var survey = EntityBuilder.CreateSurvey(creatorId: userId);
        survey.SetId(surveyId);

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId))
            .ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.HasResponsesAsync(surveyId))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<QuestionValidationException>(
            () => _sut.AddQuestionAsync(surveyId, userId, dto));
    }

    #endregion

    #region UpdateQuestionAsync Tests

    [Fact]
    public async Task UpdateQuestionAsync_WithValidData_UpdatesQuestionSuccessfully()
    {
        // Arrange
        var questionId = 1;
        var userId = 1;
        var dto = new UpdateQuestionDto
        {
            QuestionText = "Updated question text",
            QuestionType = QuestionType.Text,
            IsRequired = false
        };

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Old question text",
            questionType: QuestionType.Text,
            isRequired: true);
        question.SetId(questionId);

        var survey = EntityBuilder.CreateSurvey(creatorId: userId);
        survey.SetId(1);

        var questionWithAnswers = EntityBuilder.CreateQuestion(surveyId: 1);
        questionWithAnswers.SetId(questionId);

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId))
            .ReturnsAsync(question);
        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(survey);
        _questionRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(questionId))
            .ReturnsAsync(questionWithAnswers);
        _questionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Question>()))
            .ReturnsAsync((Question q) => q);

        // Act
        var result = await _sut.UpdateQuestionAsync(questionId, userId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.QuestionText, result.QuestionText);
        Assert.False(result.IsRequired);

        _questionRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Question>()), Times.Once);
    }

    [Fact]
    public async Task UpdateQuestionAsync_WithNonExistentQuestion_ThrowsQuestionNotFoundException()
    {
        // Arrange
        var questionId = 999;
        var userId = 1;
        var dto = new UpdateQuestionDto
        {
            QuestionText = "Updated question",
            QuestionType = QuestionType.Text,
            IsRequired = true
        };

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId))
            .ReturnsAsync((Question?)null);

        // Act & Assert
        await Assert.ThrowsAsync<QuestionNotFoundException>(
            () => _sut.UpdateQuestionAsync(questionId, userId, dto));
    }

    [Fact]
    public async Task UpdateQuestionAsync_WhenQuestionHasAnswers_ThrowsSurveyOperationException()
    {
        // Arrange
        var questionId = 1;
        var userId = 1;
        var dto = new UpdateQuestionDto
        {
            QuestionText = "Updated question",
            QuestionType = QuestionType.Text,
            IsRequired = true
        };

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Old question",
            questionType: QuestionType.Text);
        question.SetId(questionId);

        var survey = EntityBuilder.CreateSurvey(creatorId: userId);
        survey.SetId(1);

        var questionWithAnswers = EntityBuilder.CreateQuestion(surveyId: 1);
        questionWithAnswers.SetId(questionId);
        var answer = EntityBuilder.CreateAnswer(questionId: questionId);
        answer.SetId(1);
        questionWithAnswers.AddAnswerInternal(answer);

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId))
            .ReturnsAsync(question);
        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(survey);
        _questionRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(questionId))
            .ReturnsAsync(questionWithAnswers);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyOperationException>(
            () => _sut.UpdateQuestionAsync(questionId, userId, dto));
    }

    #endregion

    #region DeleteQuestionAsync Tests

    [Fact]
    public async Task DeleteQuestionAsync_WithValidData_DeletesQuestionSuccessfully()
    {
        // Arrange
        var questionId = 1;
        var userId = 1;

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Test question",
            questionType: QuestionType.Text);
        question.SetId(questionId);

        var survey = EntityBuilder.CreateSurvey(creatorId: userId);
        survey.SetId(1);

        var questionWithAnswers = EntityBuilder.CreateQuestion(surveyId: 1);
        questionWithAnswers.SetId(questionId);

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId))
            .ReturnsAsync(question);
        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(survey);
        _questionRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(questionId))
            .ReturnsAsync(questionWithAnswers);
        _questionRepositoryMock.Setup(r => r.DeleteAsync(questionId))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteQuestionAsync(questionId, userId);

        // Assert
        Assert.True(result);
        _questionRepositoryMock.Verify(r => r.DeleteAsync(questionId), Times.Once);
    }

    [Fact]
    public async Task DeleteQuestionAsync_WithNonExistentQuestion_ThrowsQuestionNotFoundException()
    {
        // Arrange
        var questionId = 999;
        var userId = 1;

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId))
            .ReturnsAsync((Question?)null);

        // Act & Assert
        await Assert.ThrowsAsync<QuestionNotFoundException>(
            () => _sut.DeleteQuestionAsync(questionId, userId));
    }

    [Fact]
    public async Task DeleteQuestionAsync_WhenQuestionHasAnswers_ThrowsSurveyOperationException()
    {
        // Arrange
        var questionId = 1;
        var userId = 1;

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Test question",
            questionType: QuestionType.Text);
        question.SetId(questionId);

        var survey = EntityBuilder.CreateSurvey(creatorId: userId);
        survey.SetId(1);

        var questionWithAnswers = EntityBuilder.CreateQuestion(surveyId: 1);
        questionWithAnswers.SetId(questionId);
        var answer = EntityBuilder.CreateAnswer(questionId: questionId);
        answer.SetId(1);
        questionWithAnswers.AddAnswerInternal(answer);

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId))
            .ReturnsAsync(question);
        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(survey);
        _questionRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(questionId))
            .ReturnsAsync(questionWithAnswers);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyOperationException>(
            () => _sut.DeleteQuestionAsync(questionId, userId));
    }

    #endregion

    #region GetQuestionAsync Tests

    [Fact]
    public async Task GetQuestionAsync_WithValidId_ReturnsQuestion()
    {
        // Arrange
        var questionId = 1;
        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Test question",
            questionType: QuestionType.Text,
            orderIndex: 0,
            isRequired: true);
        question.SetId(questionId);

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId))
            .ReturnsAsync(question);

        // Act
        var result = await _sut.GetQuestionAsync(questionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(questionId, result.Id);
        Assert.Equal("Test question", result.QuestionText);
    }

    [Fact]
    public async Task GetQuestionAsync_WithNonExistentId_ThrowsQuestionNotFoundException()
    {
        // Arrange
        var questionId = 999;

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId))
            .ReturnsAsync((Question?)null);

        // Act & Assert
        await Assert.ThrowsAsync<QuestionNotFoundException>(
            () => _sut.GetQuestionAsync(questionId));
    }

    #endregion

    #region GetBySurveyIdAsync Tests

    [Fact]
    public async Task GetBySurveyIdAsync_ReturnsQuestionsOrderedByIndex()
    {
        // Arrange
        var surveyId = 1;

        var q1 = EntityBuilder.CreateQuestion(surveyId: surveyId, questionText: "Q1", orderIndex: 0);
        q1.SetId(1);

        var q2 = EntityBuilder.CreateQuestion(surveyId: surveyId, questionText: "Q2", orderIndex: 1);
        q2.SetId(2);

        var q3 = EntityBuilder.CreateQuestion(surveyId: surveyId, questionText: "Q3", orderIndex: 2);
        q3.SetId(3);

        var questions = new List<Question> { q1, q2, q3 };

        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(surveyId))
            .ReturnsAsync(questions);

        // Act
        var result = await _sut.GetBySurveyIdAsync(surveyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Q1", result[0].QuestionText);
        Assert.Equal("Q2", result[1].QuestionText);
        Assert.Equal("Q3", result[2].QuestionText);
    }

    #endregion

    #region ReorderQuestionsAsync Tests

    [Fact]
    public async Task ReorderQuestionsAsync_WithValidData_ReordersSuccessfully()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;
        var questionIds = new int[] { 3, 1, 2 };

        var survey = EntityBuilder.CreateSurvey(creatorId: userId);
        survey.SetId(surveyId);

        var q1 = EntityBuilder.CreateQuestion(surveyId: surveyId, orderIndex: 0);
        q1.SetId(1);

        var q2 = EntityBuilder.CreateQuestion(surveyId: surveyId, orderIndex: 1);
        q2.SetId(2);

        var q3 = EntityBuilder.CreateQuestion(surveyId: surveyId, orderIndex: 2);
        q3.SetId(3);

        survey.AddQuestionInternal(q1);
        survey.AddQuestionInternal(q2);
        survey.AddQuestionInternal(q3);

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId))
            .ReturnsAsync(survey);
        _questionRepositoryMock.Setup(r => r.ReorderQuestionsAsync(It.IsAny<Dictionary<int, int>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ReorderQuestionsAsync(surveyId, userId, questionIds);

        // Assert
        Assert.True(result);
        _questionRepositoryMock.Verify(
            r => r.ReorderQuestionsAsync(It.IsAny<Dictionary<int, int>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReorderQuestionsAsync_WithInvalidQuestionIds_ThrowsQuestionValidationException()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;
        var questionIds = new int[] { 1, 2, 999 }; // 999 doesn't belong to survey

        var survey = EntityBuilder.CreateSurvey(creatorId: userId);
        survey.SetId(surveyId);

        var q1 = EntityBuilder.CreateQuestion(surveyId: surveyId, orderIndex: 0);
        q1.SetId(1);

        var q2 = EntityBuilder.CreateQuestion(surveyId: surveyId, orderIndex: 1);
        q2.SetId(2);

        survey.AddQuestionInternal(q1);
        survey.AddQuestionInternal(q2);

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId))
            .ReturnsAsync(survey);

        // Act & Assert
        await Assert.ThrowsAsync<QuestionValidationException>(
            () => _sut.ReorderQuestionsAsync(surveyId, userId, questionIds));
    }

    #endregion

    #region ValidateQuestionOptionsAsync Tests

    [Fact]
    public void ValidateQuestionOptionsAsync_TextQuestionWithoutOptions_ReturnsValid()
    {
        // Act
        var result = _sut.ValidateQuestionOptionsAsync(QuestionType.Text, null);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateQuestionOptionsAsync_TextQuestionWithOptions_ReturnsInvalid()
    {
        // Arrange
        var options = new List<string> { "Option1", "Option2" };

        // Act
        var result = _sut.ValidateQuestionOptionsAsync(QuestionType.Text, options);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ValidateQuestionOptionsAsync_SingleChoiceWithValidOptions_ReturnsValid()
    {
        // Arrange
        var options = new List<string> { "Option1", "Option2", "Option3" };

        // Act
        var result = _sut.ValidateQuestionOptionsAsync(QuestionType.SingleChoice, options);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateQuestionOptionsAsync_SingleChoiceWithoutOptions_ReturnsInvalid()
    {
        // Act
        var result = _sut.ValidateQuestionOptionsAsync(QuestionType.SingleChoice, null);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ValidateQuestionOptionsAsync_SingleChoiceWithDuplicateOptions_ReturnsInvalid()
    {
        // Arrange
        var options = new List<string> { "Option1", "Option2", "Option1" };

        // Act
        var result = _sut.ValidateQuestionOptionsAsync(QuestionType.SingleChoice, options);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Duplicate", result.Errors[0]);
    }

    [Fact]
    public void ValidateQuestionOptionsAsync_RatingQuestionWithoutOptions_ReturnsValid()
    {
        // Act
        var result = _sut.ValidateQuestionOptionsAsync(QuestionType.Rating, null);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region GetQuestionTypeValidationAsync Tests

    [Fact]
    public void GetQuestionTypeValidationAsync_ForTextType_ReturnsCorrectRules()
    {
        // Act
        var result = _sut.GetQuestionTypeValidationAsync(QuestionType.Text);

        // Assert
        Assert.NotNull(result);
        Assert.False((bool)result["requiresOptions"]);
        Assert.Contains("description", result.Keys);
    }

    [Fact]
    public void GetQuestionTypeValidationAsync_ForSingleChoiceType_ReturnsCorrectRules()
    {
        // Act
        var result = _sut.GetQuestionTypeValidationAsync(QuestionType.SingleChoice);

        // Assert
        Assert.NotNull(result);
        Assert.True((bool)result["requiresOptions"]);
        Assert.Equal(2, result["minOptions"]);
        Assert.Equal(10, result["maxOptions"]);
        Assert.False((bool)result["allowMultiple"]);
    }

    [Fact]
    public void GetQuestionTypeValidationAsync_ForMultipleChoiceType_ReturnsCorrectRules()
    {
        // Act
        var result = _sut.GetQuestionTypeValidationAsync(QuestionType.MultipleChoice);

        // Assert
        Assert.NotNull(result);
        Assert.True((bool)result["requiresOptions"]);
        Assert.True((bool)result["allowMultiple"]);
    }

    [Fact]
    public void GetQuestionTypeValidationAsync_ForRatingType_ReturnsCorrectRules()
    {
        // Act
        var result = _sut.GetQuestionTypeValidationAsync(QuestionType.Rating);

        // Assert
        Assert.NotNull(result);
        Assert.False((bool)result["requiresOptions"]);
        Assert.Equal(1, result["minRating"]);
        Assert.Equal(5, result["maxRating"]);
    }

    #endregion
}
