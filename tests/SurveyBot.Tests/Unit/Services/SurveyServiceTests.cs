using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Core.DTOs.Common;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Services;
using Xunit;

namespace SurveyBot.Tests.Unit.Services;

/// <summary>
/// Unit tests for SurveyService.
/// </summary>
public class SurveyServiceTests
{
    private readonly Mock<ISurveyRepository> _surveyRepositoryMock;
    private readonly Mock<IResponseRepository> _responseRepositoryMock;
    private readonly Mock<IAnswerRepository> _answerRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<SurveyService>> _loggerMock;
    private readonly SurveyService _sut;

    public SurveyServiceTests()
    {
        _surveyRepositoryMock = new Mock<ISurveyRepository>();
        _responseRepositoryMock = new Mock<IResponseRepository>();
        _answerRepositoryMock = new Mock<IAnswerRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<SurveyService>>();

        _sut = new SurveyService(
            _surveyRepositoryMock.Object,
            _responseRepositoryMock.Object,
            _answerRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
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

        var survey = new Survey
        {
            Id = 1,
            Title = dto.Title,
            Description = dto.Description,
            CreatorId = userId,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

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

        var survey = new Survey
        {
            Id = surveyId,
            Title = "Old Title",
            Description = "Old Description",
            CreatorId = userId,
            IsActive = false,
            Questions = new List<Question>()
        };

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

        var survey = new Survey
        {
            Id = surveyId,
            CreatorId = ownerId,
            Questions = new List<Question>()
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId)).ReturnsAsync(survey);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.UpdateSurveyAsync(surveyId, userId, dto));
    }

    [Fact]
    public async Task UpdateSurveyAsync_WithActiveSurveyWithResponses_ThrowsOperationException()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;
        var dto = new UpdateSurveyDto { Title = "Updated Title" };

        var survey = new Survey
        {
            Id = surveyId,
            CreatorId = userId,
            IsActive = true,
            Questions = new List<Question>()
        };

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

        var survey = new Survey
        {
            Id = surveyId,
            CreatorId = userId,
            Title = "Test Survey"
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.HasResponsesAsync(surveyId)).ReturnsAsync(false);
        _surveyRepositoryMock.Setup(r => r.DeleteAsync(surveyId)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteSurveyAsync(surveyId, userId);

        // Assert
        Assert.True(result);
        _surveyRepositoryMock.Verify(r => r.DeleteAsync(surveyId), Times.Once);
    }

    [Fact]
    public async Task DeleteSurveyAsync_WithResponses_SoftDeletesSurvey()
    {
        // Arrange
        var surveyId = 1;
        var userId = 1;

        var survey = new Survey
        {
            Id = surveyId,
            CreatorId = userId,
            Title = "Test Survey",
            IsActive = true
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);
        _surveyRepositoryMock.Setup(r => r.HasResponsesAsync(surveyId)).ReturnsAsync(true);
        _surveyRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Survey>())).ReturnsAsync(survey);

        // Act
        var result = await _sut.DeleteSurveyAsync(surveyId, userId);

        // Assert
        Assert.True(result);
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

        var survey = new Survey
        {
            Id = surveyId,
            CreatorId = ownerId
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
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

        var survey = new Survey
        {
            Id = surveyId,
            Title = "Test Survey",
            CreatorId = userId,
            Questions = new List<Question>()
        };

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

        var surveys = new List<Survey>
        {
            new() { Id = 1, Title = "Survey 1", CreatorId = userId, Questions = new List<Question>(), CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Title = "Survey 2", CreatorId = userId, Questions = new List<Question>(), CreatedAt = DateTime.UtcNow }
        };

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

        var surveys = new List<Survey>
        {
            new() { Id = 1, Title = "Customer Satisfaction Survey", CreatorId = userId, Questions = new List<Question>(), CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Title = "Employee Survey", CreatorId = userId, Questions = new List<Question>(), CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Title = "Customer Feedback", CreatorId = userId, Questions = new List<Question>(), CreatedAt = DateTime.UtcNow }
        };

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

        var survey = new Survey
        {
            Id = surveyId,
            Title = "Test Survey",
            CreatorId = userId,
            IsActive = false,
            Questions = new List<Question>
            {
                new() { Id = 1, QuestionText = "Question 1", QuestionType = QuestionType.Text }
            }
        };

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

        var survey = new Survey
        {
            Id = surveyId,
            CreatorId = userId,
            IsActive = false,
            Questions = new List<Question>()
        };

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

        var survey = new Survey
        {
            Id = surveyId,
            CreatorId = ownerId,
            Questions = new List<Question> { new() { Id = 1 } }
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(surveyId)).ReturnsAsync(survey);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
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

        var survey = new Survey
        {
            Id = surveyId,
            Title = "Test Survey",
            CreatorId = userId,
            IsActive = true,
            Questions = new List<Question>()
        };

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

        var survey = new Survey
        {
            Id = surveyId,
            CreatorId = userId
        };

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

        var survey = new Survey
        {
            Id = surveyId,
            CreatorId = ownerId
        };

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
