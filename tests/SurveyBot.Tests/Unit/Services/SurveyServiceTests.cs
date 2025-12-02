using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Core.DTOs.Common;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Infrastructure.Services;
using SurveyBot.Tests.Fixtures;
using Xunit;

namespace SurveyBot.Tests.Unit.Services;

/// <summary>
/// Unit tests for SurveyService.
/// </summary>
public class SurveyServiceTests : IDisposable
{
    private readonly Mock<ISurveyRepository> _surveyRepositoryMock;
    private readonly Mock<IQuestionRepository> _questionRepositoryMock;
    private readonly Mock<IResponseRepository> _responseRepositoryMock;
    private readonly Mock<IAnswerRepository> _answerRepositoryMock;
    private readonly Mock<ISurveyValidationService> _validationServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<SurveyService>> _loggerMock;
    private readonly SurveyBotDbContext _context;
    private readonly SurveyService _sut;

    public SurveyServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SurveyBotDbContext(options);

        _surveyRepositoryMock = new Mock<ISurveyRepository>();
        _questionRepositoryMock = new Mock<IQuestionRepository>();
        _responseRepositoryMock = new Mock<IResponseRepository>();
        _answerRepositoryMock = new Mock<IAnswerRepository>();
        _validationServiceMock = new Mock<ISurveyValidationService>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<SurveyService>>();

        // Setup default validation behavior
        _validationServiceMock
            .Setup(v => v.DetectCycleAsync(It.IsAny<int>()))
            .ReturnsAsync(new CycleDetectionResult { HasCycle = false });

        _sut = new SurveyService(
            _surveyRepositoryMock.Object,
            _questionRepositoryMock.Object,
            _responseRepositoryMock.Object,
            _answerRepositoryMock.Object,
            _validationServiceMock.Object,
            _context,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateSurveyAsync Tests

    [Fact]
    public async Task CreateSurveyAsync_WithValidData_CreatesSurveySuccessfully()
    {
        // Arrange
        var userId = 1;
        var dto = new CreateSurveyDto
        {
            Title = "Test Survey",
            Description = "Test Description",
            IsActive = false,
            AllowMultipleResponses = false,
            ShowResults = true
        };

        var survey = EntityBuilder.CreateSurvey(
            title: dto.Title,
            description: dto.Description,
            creatorId: userId,
            isActive: false);
        survey.SetId(1);

        var surveyDto = new SurveyDto
        {
            Id = 1,
            Title = dto.Title,
            Description = dto.Description,
            CreatorId = userId,
            IsActive = false
        };

        _mapperMock.Setup(m => m.Map<Survey>(dto)).Returns(survey);
        _surveyRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Survey>())).ReturnsAsync(survey);
        _mapperMock.Setup(m => m.Map<SurveyDto>(survey)).Returns(surveyDto);

        // Act
        var result = await _sut.CreateSurveyAsync(userId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Title, result.Title);
        Assert.Equal(userId, result.CreatorId);
        Assert.False(result.IsActive);
        Assert.Equal(0, result.TotalResponses);
        Assert.Equal(0, result.CompletedResponses);

        _surveyRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Survey>()), Times.Once);
    }

    [Fact]
    public async Task CreateSurveyAsync_WithEmptyTitle_ThrowsValidationException()
    {
        // Arrange
        var userId = 1;
        var dto = new CreateSurveyDto
        {
            Title = "",
            Description = "Test Description"
        };

        // Act & Assert
        await Assert.ThrowsAsync<SurveyValidationException>(() =>
            _sut.CreateSurveyAsync(userId, dto));
    }

    [Fact]
    public async Task CreateSurveyAsync_WithTitleTooShort_ThrowsValidationException()
    {
        // Arrange
        var userId = 1;
        var dto = new CreateSurveyDto
        {
            Title = "AB",
            Description = "Test Description"
        };

        // Act & Assert
        await Assert.ThrowsAsync<SurveyValidationException>(() =>
            _sut.CreateSurveyAsync(userId, dto));
    }

    #endregion

    #region UpdateSurveyAsync Tests

    [Fact]
    public async Task UpdateSurveyAsync_WithValidData_UpdatesSurveySuccessfully()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;
        var dto = new UpdateSurveyDto
        {
            Title = "Updated Title",
            Description = "Updated Description",
            AllowMultipleResponses = true,
            ShowResults = false
        };

        var survey = EntityBuilder.CreateSurvey(
            title: "Old Title",
            description: "Old Description",
            creatorId: userId,
            isActive: false);
        survey.SetId(surveyId);

        var updatedSurveyDto = new SurveyDto
        {
            Id = surveyId,
            Title = dto.Title,
            Description = dto.Description,
            CreatorId = userId
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId)).ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.HasResponsesAsync(surveyId)).ReturnsAsync(false);
        _surveyRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Survey>())).ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.GetResponseCountAsync(surveyId)).ReturnsAsync(0);
        _responseRepositoryMock.Setup(r => r.GetCompletedCountAsync(surveyId)).ReturnsAsync(0);
        _mapperMock.Setup(m => m.Map<SurveyDto>(survey)).Returns(updatedSurveyDto);

        // Act
        var result = await _sut.UpdateSurveyAsync(surveyId, userId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Title, survey.Title);
        Assert.Equal(dto.Description, survey.Description);

        _surveyRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Survey>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSurveyAsync_WithNonExistentSurvey_ThrowsNotFoundException()
    {
        // Arrange
        var surveyId = 999;
        var userId = 1;
        var dto = new UpdateSurveyDto { Title = "Updated Title" };

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId)).ReturnsAsync((Survey?)null);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyNotFoundException>(() =>
            _sut.UpdateSurveyAsync(surveyId, userId, dto));
    }

    [Fact]
    public async Task UpdateSurveyAsync_WithUnauthorizedUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var surveyId = 1;
        var userId = 2;
        var ownerId = 1;
        var dto = new UpdateSurveyDto { Title = "Updated Title" };

        var survey = EntityBuilder.CreateSurvey(creatorId: ownerId);
        survey.SetId(surveyId);

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId)).ReturnsAsync(survey);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyBot.Core.Exceptions.UnauthorizedAccessException>(() =>
            _sut.UpdateSurveyAsync(surveyId, userId, dto));
    }

    [Fact]
    public async Task UpdateSurveyAsync_WithActiveSurveyWithResponses_ThrowsOperationException()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;
        var dto = new UpdateSurveyDto { Title = "Updated Title" };

        var survey = EntityBuilder.CreateSurvey(
            creatorId: userId,
            isActive: true);
        survey.SetId(surveyId);

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId)).ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.HasResponsesAsync(surveyId)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyOperationException>(() =>
            _sut.UpdateSurveyAsync(surveyId, userId, dto));
    }

    #endregion

    #region DeleteSurveyAsync Tests

    [Fact]
    public async Task DeleteSurveyAsync_WithNoResponses_HardDeletesSurvey()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;

        var survey = EntityBuilder.CreateSurvey(
            title: "Test Survey",
            creatorId: userId);
        survey.SetId(surveyId);

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.HasResponsesAsync(surveyId)).ReturnsAsync(false);
        _surveyRepositoryMock.Setup(r => r.DeleteAsync(surveyId)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteSurveyAsync(surveyId, userId);

        // Assert
        Assert.True(result);
        _surveyRepositoryMock.Verify(r => r.DeleteAsync(surveyId), Times.Once);
    }

    [Fact(Skip = "Implementation changed to always hard delete. Soft delete feature removed.")]
    public async Task DeleteSurveyAsync_WithResponses_SoftDeletesSurvey()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;

        var survey = EntityBuilder.CreateSurvey(
            title: "Test Survey",
            creatorId: userId,
            isActive: true);
        survey.SetId(surveyId);

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.HasResponsesAsync(surveyId)).ReturnsAsync(true);
        _surveyRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Survey>())).ReturnsAsync((Survey s) =>
        {
            // The service should call Deactivate() on the survey
            // After Deactivate() is called, IsActive should be false
            return s;
        });

        // Act
        var result = await _sut.DeleteSurveyAsync(surveyId, userId);

        // Assert
        Assert.True(result);
        // The survey should now be deactivated (IsActive = false)
        Assert.False(survey.IsActive);
        _surveyRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Survey>()), Times.Once);
        _surveyRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteSurveyAsync_WithUnauthorizedUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var surveyId = 1;
        var userId = 2;
        var ownerId = 1;

        var survey = EntityBuilder.CreateSurvey(creatorId: ownerId);
        survey.SetId(surveyId);

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyBot.Core.Exceptions.UnauthorizedAccessException>(() =>
            _sut.DeleteSurveyAsync(surveyId, userId));
    }

    #endregion

    #region GetSurveyByIdAsync Tests

    [Fact]
    public async Task GetSurveyByIdAsync_WithValidId_ReturnsSurvey()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;

        var survey = EntityBuilder.CreateSurvey(
            title: "Test Survey",
            creatorId: userId);
        survey.SetId(surveyId);

        var surveyDto = new SurveyDto
        {
            Id = surveyId,
            Title = "Test Survey",
            CreatorId = userId
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId)).ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.GetResponseCountAsync(surveyId)).ReturnsAsync(5);
        _responseRepositoryMock.Setup(r => r.GetCompletedCountAsync(surveyId)).ReturnsAsync(3);
        _mapperMock.Setup(m => m.Map<SurveyDto>(survey)).Returns(surveyDto);

        // Act
        var result = await _sut.GetSurveyByIdAsync(surveyId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(surveyId, result.Id);
        Assert.Equal(5, result.TotalResponses);
        Assert.Equal(3, result.CompletedResponses);
    }

    [Fact]
    public async Task GetSurveyByIdAsync_WithNonExistentSurvey_ThrowsNotFoundException()
    {
        // Arrange
        var surveyId = 999;
        var userId = 1;

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId)).ReturnsAsync((Survey?)null);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyNotFoundException>(() =>
            _sut.GetSurveyByIdAsync(surveyId, userId));
    }

    #endregion

    #region GetAllSurveysAsync Tests

    [Fact]
    public async Task GetAllSurveysAsync_WithValidQuery_ReturnsPagedResults()
    {
        // Arrange
        var userId = 1;
        var query = new PaginationQueryDto
        {
            PageNumber = 1,
            PageSize = 10
        };

        var survey1 = EntityBuilder.CreateSurvey(title: "Survey 1", creatorId: userId);
        survey1.SetId(1);

        var survey2 = EntityBuilder.CreateSurvey(title: "Survey 2", creatorId: userId);
        survey2.SetId(2);

        var surveys = new List<Survey> { survey1, survey2 };

        _surveyRepositoryMock.Setup(r => r.GetByCreatorIdAsync(userId)).ReturnsAsync(surveys);
        _surveyRepositoryMock.Setup(r => r.GetResponseCountAsync(It.IsAny<int>())).ReturnsAsync(0);
        _responseRepositoryMock.Setup(r => r.GetCompletedCountAsync(It.IsAny<int>())).ReturnsAsync(0);
        _mapperMock.Setup(m => m.Map<SurveyListDto>(It.IsAny<Survey>()))
            .Returns((Survey s) => new SurveyListDto { Id = s.Id, Title = s.Title });

        // Act
        var result = await _sut.GetAllSurveysAsync(userId, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetAllSurveysAsync_WithSearchTerm_FiltersResults()
    {
        // Arrange
        var userId = 1;
        var query = new PaginationQueryDto
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Customer"
        };

        var survey1 = EntityBuilder.CreateSurvey(title: "Customer Satisfaction Survey", creatorId: userId);
        survey1.SetId(1);

        var survey2 = EntityBuilder.CreateSurvey(title: "Employee Survey", creatorId: userId);
        survey2.SetId(2);

        var survey3 = EntityBuilder.CreateSurvey(title: "Customer Feedback", creatorId: userId);
        survey3.SetId(3);

        var surveys = new List<Survey> { survey1, survey2, survey3 };

        _surveyRepositoryMock.Setup(r => r.GetByCreatorIdAsync(userId)).ReturnsAsync(surveys);
        _surveyRepositoryMock.Setup(r => r.GetResponseCountAsync(It.IsAny<int>())).ReturnsAsync(0);
        _responseRepositoryMock.Setup(r => r.GetCompletedCountAsync(It.IsAny<int>())).ReturnsAsync(0);
        _mapperMock.Setup(m => m.Map<SurveyListDto>(It.IsAny<Survey>()))
            .Returns((Survey s) => new SurveyListDto { Id = s.Id, Title = s.Title });

        // Act
        var result = await _sut.GetAllSurveysAsync(userId, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Contains("customer", item.Title.ToLower()));
    }

    #endregion

    #region ActivateSurveyAsync Tests

    [Fact]
    public async Task ActivateSurveyAsync_WithQuestionsPresent_ActivatesSurvey()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;

        var survey = EntityBuilder.CreateSurvey(
            title: "Test Survey",
            creatorId: userId,
            isActive: false);
        survey.SetId(surveyId);

        var question = EntityBuilder.CreateQuestion(
            surveyId: surveyId,
            questionText: "Question 1",
            questionType: QuestionType.Text);
        question.SetId(1);

        survey.AddQuestionInternal(question);

        var surveyDto = new SurveyDto
        {
            Id = surveyId,
            IsActive = true
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId)).ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Survey>())).ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.GetResponseCountAsync(surveyId)).ReturnsAsync(0);
        _responseRepositoryMock.Setup(r => r.GetCompletedCountAsync(surveyId)).ReturnsAsync(0);
        _mapperMock.Setup(m => m.Map<SurveyDto>(survey)).Returns(surveyDto);

        // Mock validation service to pass validation
        _validationServiceMock
            .Setup(v => v.DetectCycleAsync(surveyId))
            .ReturnsAsync(new CycleDetectionResult { HasCycle = false });
        _validationServiceMock
            .Setup(v => v.FindSurveyEndpointsAsync(surveyId))
            .ReturnsAsync(new List<int> { 1 }); // At least one endpoint

        // Act
        var result = await _sut.ActivateSurveyAsync(surveyId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(survey.IsActive);
        _surveyRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Survey>()), Times.Once);
    }

    [Fact]
    public async Task ActivateSurveyAsync_WithNoQuestions_ThrowsValidationException()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;

        var survey = EntityBuilder.CreateSurvey(
            creatorId: userId,
            isActive: false);
        survey.SetId(surveyId);

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId)).ReturnsAsync(survey);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyValidationException>(() =>
            _sut.ActivateSurveyAsync(surveyId, userId));
    }

    [Fact]
    public async Task ActivateSurveyAsync_WithUnauthorizedUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var surveyId = 1;
        var userId = 2;
        var ownerId = 1;

        var survey = EntityBuilder.CreateSurvey(creatorId: ownerId);
        survey.SetId(surveyId);

        var question = EntityBuilder.CreateQuestion(surveyId: surveyId);
        question.SetId(1);
        survey.AddQuestionInternal(question);

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId)).ReturnsAsync(survey);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyBot.Core.Exceptions.UnauthorizedAccessException>(() =>
            _sut.ActivateSurveyAsync(surveyId, userId));
    }

    #endregion

    #region DeactivateSurveyAsync Tests

    [Fact]
    public async Task DeactivateSurveyAsync_WithActiveSurvey_DeactivatesSurvey()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;

        var survey = EntityBuilder.CreateSurvey(
            title: "Test Survey",
            creatorId: userId,
            isActive: true);
        survey.SetId(surveyId);

        var surveyDto = new SurveyDto
        {
            Id = surveyId,
            IsActive = false
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId)).ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Survey>())).ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.GetResponseCountAsync(surveyId)).ReturnsAsync(0);
        _responseRepositoryMock.Setup(r => r.GetCompletedCountAsync(surveyId)).ReturnsAsync(0);
        _mapperMock.Setup(m => m.Map<SurveyDto>(survey)).Returns(surveyDto);

        // Act
        var result = await _sut.DeactivateSurveyAsync(surveyId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.False(survey.IsActive);
        _surveyRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Survey>()), Times.Once);
    }

    #endregion

    #region UserOwnsSurveyAsync Tests

    [Fact]
    public async Task UserOwnsSurveyAsync_WhenUserOwns_ReturnsTrue()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;

        var survey = EntityBuilder.CreateSurvey(creatorId: userId);
        survey.SetId(surveyId);

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);

        // Act
        var result = await _sut.UserOwnsSurveyAsync(surveyId, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserOwnsSurveyAsync_WhenUserDoesNotOwn_ReturnsFalse()
    {
        // Arrange
        var surveyId = 1;
        var userId = 2;
        var ownerId = 1;

        var survey = EntityBuilder.CreateSurvey(creatorId: ownerId);
        survey.SetId(surveyId);

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);

        // Act
        var result = await _sut.UserOwnsSurveyAsync(surveyId, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UserOwnsSurveyAsync_WhenSurveyNotFound_ReturnsFalse()
    {
        // Arrange
        var surveyId = 999;
        var userId = 1;

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync((Survey?)null);

        // Act
        var result = await _sut.UserOwnsSurveyAsync(surveyId, userId);

        // Assert
        Assert.False(result);
    }

    #endregion
}
