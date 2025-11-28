using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SurveyBot.Bot.Configuration;
using SurveyBot.Bot.Handlers.Questions;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using SurveyBot.Core.ValueObjects.Answers;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit;

namespace SurveyBot.Tests.Unit.Bot;

/// <summary>
/// Unit tests for LocationQuestionHandler.
/// Tests location question display, answer processing, validation, and privacy-preserving logging.
/// </summary>
public class LocationQuestionHandlerTests
{
    private readonly Mock<IBotService> _mockBotService;
    private readonly Mock<ITelegramBotClient> _mockBotClient;
    private readonly Mock<IAnswerValidator> _mockValidator;
    private readonly QuestionErrorHandler _errorHandler;
    private readonly QuestionMediaHelper _mediaHelper;
    private readonly Mock<ILogger<LocationQuestionHandler>> _mockLogger;
    private readonly LocationQuestionHandler _handler;

    public LocationQuestionHandlerTests()
    {
        _mockBotService = new Mock<IBotService>();
        _mockBotClient = new Mock<ITelegramBotClient>();
        _mockBotService.Setup(s => s.Client).Returns(_mockBotClient.Object);

        _mockValidator = new Mock<IAnswerValidator>();
        _mockLogger = new Mock<ILogger<LocationQuestionHandler>>();

        // Create real instances of helper classes with mocked dependencies
        var mockErrorHandlerLogger = new Mock<ILogger<QuestionErrorHandler>>();
        _errorHandler = new QuestionErrorHandler(
            _mockBotService.Object,
            mockErrorHandlerLogger.Object);

        var mockMediaServiceLogger = new Mock<ITelegramMediaService>();
        var mockMediaHelperLogger = new Mock<ILogger<QuestionMediaHelper>>();
        var botConfig = Options.Create(new BotConfiguration { BotToken = "test-token" });
        _mediaHelper = new QuestionMediaHelper(
            mockMediaServiceLogger.Object,
            botConfig,
            mockMediaHelperLogger.Object);

        _handler = new LocationQuestionHandler(
            _mockBotService.Object,
            _mockValidator.Object,
            _errorHandler,
            _mediaHelper,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullBotService_ThrowsArgumentNullException()
    {
        // Arrange
        var mockErrorHandlerLogger = new Mock<ILogger<QuestionErrorHandler>>();
        var errorHandler = new QuestionErrorHandler(
            _mockBotService.Object,
            mockErrorHandlerLogger.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LocationQuestionHandler(
            null!,
            _mockValidator.Object,
            errorHandler,
            _mediaHelper,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullValidator_ThrowsArgumentNullException()
    {
        // Arrange
        var mockErrorHandlerLogger = new Mock<ILogger<QuestionErrorHandler>>();
        var errorHandler = new QuestionErrorHandler(
            _mockBotService.Object,
            mockErrorHandlerLogger.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LocationQuestionHandler(
            _mockBotService.Object,
            null!,
            errorHandler,
            _mediaHelper,
            _mockLogger.Object));
    }

    [Fact]
    public void QuestionType_ReturnsLocation()
    {
        // Act & Assert
        _handler.QuestionType.Should().Be(QuestionType.Location);
    }

    #endregion

    #region DisplayQuestionAsync Tests

    [Fact]
    public async Task DisplayQuestionAsync_SendsMessageWithLocationKeyboard()
    {
        // Arrange
        var chatId = 12345L;
        var question = CreateQuestionDto(1, "Share your location", true);

        SetupSendMessageMock(100);

        // Act
        var result = await _handler.DisplayQuestionAsync(chatId, question, 0, 5);

        // Assert
        result.Should().Be(100);
        VerifySendMessageCalled(chatId, s =>
            s.Contains("Share your location") && s.Contains("Question 1 of 5"));
    }

    [Fact]
    public async Task DisplayQuestionAsync_ForOptionalQuestion_IncludesSkipInfo()
    {
        // Arrange
        var chatId = 12345L;
        var question = CreateQuestionDto(1, "Optional location", false);

        SetupSendMessageMock(100);

        // Act
        await _handler.DisplayQuestionAsync(chatId, question, 0, 5);

        // Assert
        VerifySendMessageCalled(chatId, s => s.Contains("Optional"));
    }

    #endregion

    #region ProcessAnswerAsync Tests

    [Fact]
    public async Task ProcessAnswerAsync_WithValidLocation_ReturnsJsonAnswer()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var message = CreateLocationMessage(40.7128, -74.0060, null);

        _mockValidator
            .Setup(v => v.ValidateAnswer(It.IsAny<string>(), question))
            .Returns(ValidationResult.Success());

        SetupSendMessageMock(100);

        // Act
        var result = await _handler.ProcessAnswerAsync(message, null, question, 12345L);

        // Assert
        result.Should().NotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement>(result!);
        parsed.GetProperty("latitude").GetDouble().Should().BeApproximately(40.7128, 0.0001);
        parsed.GetProperty("longitude").GetDouble().Should().BeApproximately(-74.0060, 0.0001);
    }

    [Fact]
    public async Task ProcessAnswerAsync_WithInvalidLatitude_ReturnsNull()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var message = CreateLocationMessage(95.0, -74.0060, null); // Invalid latitude (> 90)

        SetupSendMessageMock(100); // For error message

        // Act
        var result = await _handler.ProcessAnswerAsync(message, null, question, 12345L);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAnswerAsync_WithInvalidLongitude_ReturnsNull()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var message = CreateLocationMessage(40.7128, 200.0, null); // Invalid longitude (> 180)

        SetupSendMessageMock(100); // For error message

        // Act
        var result = await _handler.ProcessAnswerAsync(message, null, question, 12345L);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAnswerAsync_WithTextInsteadOfLocation_ReturnsNull()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var message = new Message
        {
            Chat = new Chat { Id = 12345 },
            Text = "I'm at home",
            Location = null
        };

        SetupSendMessageMock(100); // For error message

        // Act
        var result = await _handler.ProcessAnswerAsync(message, null, question, 12345L);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAnswerAsync_SkipOptionalQuestion_ReturnsEmptyLocationAnswer()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Optional location", false);
        var message = new Message
        {
            Chat = new Chat { Id = 12345 },
            Text = "/skip"
        };

        SetupSendMessageMock(100);

        // Act
        var result = await _handler.ProcessAnswerAsync(message, null, question, 12345L);

        // Assert
        result.Should().NotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement>(result!);
        parsed.GetProperty("latitude").ValueKind.Should().Be(JsonValueKind.Null);
        parsed.GetProperty("longitude").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ProcessAnswerAsync_SkipRequiredQuestion_ReturnsNull()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Required location", true);
        var message = new Message
        {
            Chat = new Chat { Id = 12345 },
            Text = "/skip"
        };

        SetupSendMessageMock(100); // For error message

        // Act
        var result = await _handler.ProcessAnswerAsync(message, null, question, 12345L);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAnswerAsync_WithNullMessage_ReturnsNull()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);

        // Act
        var result = await _handler.ProcessAnswerAsync(null, null, question, 12345L);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAnswerAsync_WithCallbackQuery_ReturnsNull()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var callbackQuery = new CallbackQuery
        {
            Id = "callback123",
            Data = "some_callback_data"
        };

        // Act
        var result = await _handler.ProcessAnswerAsync(null, callbackQuery, question, 12345L);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ValidateAnswer Tests

    [Fact]
    public void ValidateAnswer_WithValidCoordinates_ReturnsTrue()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var answerJson = JsonSerializer.Serialize(new { latitude = 40.7128, longitude = -74.0060 });

        // Act
        var result = _handler.ValidateAnswer(answerJson, question);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateAnswer_WithNullCoordinatesForRequired_ReturnsFalse()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var answerJson = JsonSerializer.Serialize(new { latitude = (double?)null, longitude = (double?)null });

        // Act
        var result = _handler.ValidateAnswer(answerJson, question);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAnswer_WithNullCoordinatesForOptional_ReturnsTrue()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", false);
        var answerJson = JsonSerializer.Serialize(new { latitude = (double?)null, longitude = (double?)null });

        // Act
        var result = _handler.ValidateAnswer(answerJson, question);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateAnswer_WithLatitudeOutOfRange_ReturnsFalse()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var answerJson = JsonSerializer.Serialize(new { latitude = 95.0, longitude = -74.0060 });

        // Act
        var result = _handler.ValidateAnswer(answerJson, question);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAnswer_WithLongitudeOutOfRange_ReturnsFalse()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var answerJson = JsonSerializer.Serialize(new { latitude = 40.7128, longitude = 200.0 });

        // Act
        var result = _handler.ValidateAnswer(answerJson, question);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAnswer_WithNullAnswer_ReturnsFalseForRequired()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);

        // Act
        var result = _handler.ValidateAnswer(null, question);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAnswer_WithNullAnswer_ReturnsTrueForOptional()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", false);

        // Act
        var result = _handler.ValidateAnswer(null, question);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateAnswer_WithInvalidJson_ReturnsFalse()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);

        // Act
        var result = _handler.ValidateAnswer("not valid json {{{", question);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAnswer_WithMissingLatitude_ReturnsFalse()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var answerJson = JsonSerializer.Serialize(new { longitude = -74.0060 });

        // Act
        var result = _handler.ValidateAnswer(answerJson, question);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAnswer_WithMissingLongitude_ReturnsFalse()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var answerJson = JsonSerializer.Serialize(new { latitude = 40.7128 });

        // Act
        var result = _handler.ValidateAnswer(answerJson, question);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAnswer_WithBoundaryLatitude90_ReturnsTrue()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var answerJson = JsonSerializer.Serialize(new { latitude = 90.0, longitude = 0.0 });

        // Act
        var result = _handler.ValidateAnswer(answerJson, question);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateAnswer_WithBoundaryLatitudeMinus90_ReturnsTrue()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var answerJson = JsonSerializer.Serialize(new { latitude = -90.0, longitude = 0.0 });

        // Act
        var result = _handler.ValidateAnswer(answerJson, question);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateAnswer_WithBoundaryLongitude180_ReturnsTrue()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var answerJson = JsonSerializer.Serialize(new { latitude = 0.0, longitude = 180.0 });

        // Act
        var result = _handler.ValidateAnswer(answerJson, question);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateAnswer_WithBoundaryLongitudeMinus180_ReturnsTrue()
    {
        // Arrange
        var question = CreateQuestionDto(1, "Share location", true);
        var answerJson = JsonSerializer.Serialize(new { latitude = 0.0, longitude = -180.0 });

        // Act
        var result = _handler.ValidateAnswer(answerJson, question);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static QuestionDto CreateQuestionDto(int id, string text, bool isRequired)
    {
        return new QuestionDto
        {
            Id = id,
            QuestionText = text,
            QuestionType = QuestionType.Location,
            IsRequired = isRequired,
            OrderIndex = 0
        };
    }

    private static Message CreateLocationMessage(double latitude, double longitude, float? accuracy)
    {
        return new Message
        {
            Chat = new Chat { Id = 12345 },
            Location = new Location
            {
                Latitude = latitude,
                Longitude = longitude,
                HorizontalAccuracy = accuracy
            }
        };
    }

    /// <summary>
    /// Sets up the SendRequest mock for SendMessage to return a message with the specified ID.
    /// Telegram.Bot 22.x uses SendRequest with request objects.
    /// </summary>
    private void SetupSendMessageMock(int messageId)
    {
        _mockBotClient
            .Setup(c => c.SendRequest(
                It.IsAny<SendMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = messageId, Chat = new Chat { Id = 12345 } });
    }

    /// <summary>
    /// Verifies SendRequest was called with a SendMessageRequest containing expected properties.
    /// </summary>
    private void VerifySendMessageCalled(long chatId, Func<string, bool> textPredicate, Times? times = null)
    {
        _mockBotClient.Verify(
            c => c.SendRequest(
                It.Is<SendMessageRequest>(req =>
                    req.ChatId.Identifier == chatId &&
                    textPredicate(req.Text)),
                It.IsAny<CancellationToken>()),
            times ?? Times.Once());
    }

    #endregion
}
