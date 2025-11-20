using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SurveyBot.Bot.Configuration;
using SurveyBot.Bot.Handlers.Questions;
using SurveyBot.Core.DTOs.Media;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit;

namespace SurveyBot.Tests.Unit.Bot;

/// <summary>
/// Comprehensive tests for Telegram bot media handling.
/// Tests media display, error scenarios, and retry logic.
/// </summary>
public class MediaHandlingTests
{
    private readonly Mock<ITelegramMediaService> _mockMediaService;
    private readonly Mock<ITelegramBotClient> _mockBotClient;
    private readonly Mock<ILogger<QuestionMediaHelper>> _mockLogger;
    private readonly BotConfiguration _botConfiguration;
    private readonly QuestionMediaHelper _mediaHelper;

    public MediaHandlingTests()
    {
        _mockMediaService = new Mock<ITelegramMediaService>();
        _mockBotClient = new Mock<ITelegramBotClient>();
        _mockLogger = new Mock<ILogger<QuestionMediaHelper>>();

        _botConfiguration = new BotConfiguration
        {
            BotToken = "test-token",
            ApiBaseUrl = "http://localhost:5000"
        };

        var options = Options.Create(_botConfiguration);

        _mediaHelper = new QuestionMediaHelper(
            _mockMediaService.Object,
            options,
            _mockLogger.Object);
    }

    #region Test Suite 1: Media Display Tests

    [Fact]
    public async Task SendQuestionMedia_WithImageMedia_DisplaysImage()
    {
        // Arrange: Question with 1 image
        var question = CreateQuestionWithMedia(new[]
        {
            CreateMediaItem("image", "/uploads/media/2025/11/photo.jpg", "photo.jpg", 0)
        });

        _mockMediaService.Setup(x => x.SendImageAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act: Send media
        var result = await _mediaHelper.SendQuestionMediaAsync(
            chatId: 123456789,
            question: question,
            cancellationToken: CancellationToken.None);

        // Assert: Verify image was sent
        Assert.True(result);
        _mockMediaService.Verify(x => x.SendImageAsync(
            It.Is<long>(c => c == 123456789),
            It.Is<string>(url => url.Contains("photo.jpg")),
            It.Is<string>(caption => caption.Contains("Media 1 of 1")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendQuestionMedia_WithMultipleMediaTypes_DisplaysAllInOrder()
    {
        // Arrange: Question with image, video, audio
        var mediaItems = new[]
        {
            CreateMediaItem("image", "/uploads/media/2025/11/photo.jpg", "photo.jpg", 0),
            CreateMediaItem("video", "/uploads/media/2025/11/video.mp4", "video.mp4", 1),
            CreateMediaItem("audio", "/uploads/media/2025/11/audio.mp3", "audio.mp3", 2)
        };

        var question = CreateQuestionWithMedia(mediaItems);

        _mockMediaService.Setup(x => x.SendImageAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockMediaService.Setup(x => x.SendVideoAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockMediaService.Setup(x => x.SendAudioAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mediaHelper.SendQuestionMediaAsync(
            chatId: 123456789,
            question: question,
            cancellationToken: CancellationToken.None);

        // Assert: Verify all media sent in order
        Assert.True(result);
        _mockMediaService.Verify(x => x.SendImageAsync(
            It.IsAny<long>(),
            It.Is<string>(url => url.Contains("photo.jpg")),
            It.Is<string>(caption => caption.Contains("Media 1 of 3")),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockMediaService.Verify(x => x.SendVideoAsync(
            It.IsAny<long>(),
            It.Is<string>(url => url.Contains("video.mp4")),
            It.Is<string>(caption => caption.Contains("Media 2 of 3")),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockMediaService.Verify(x => x.SendAudioAsync(
            It.IsAny<long>(),
            It.Is<string>(url => url.Contains("audio.mp3")),
            It.Is<string>(caption => caption.Contains("Media 3 of 3")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendQuestionMedia_WithoutMedia_ReturnsTrue()
    {
        // Arrange: Text-only question
        var question = new QuestionDto
        {
            Id = 1,
            QuestionText = "What is your name?",
            MediaContent = null // No media
        };

        // Act
        var result = await _mediaHelper.SendQuestionMediaAsync(
            chatId: 123456789,
            question: question,
            cancellationToken: CancellationToken.None);

        // Assert: Returns true (no media is considered success)
        Assert.True(result);

        // Verify no media methods were called
        _mockMediaService.Verify(x => x.SendImageAsync(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendQuestionMedia_WithEmptyMediaList_ReturnsTrue()
    {
        // Arrange: Question with empty media list
        var question = new QuestionDto
        {
            Id = 1,
            QuestionText = "What is your favorite color?",
            MediaContent = new MediaContentDto
            {
                Version = "1.0",
                Items = new List<MediaItemDto>() // Empty list
            }
        };

        // Act
        var result = await _mediaHelper.SendQuestionMediaAsync(
            chatId: 123456789,
            question: question,
            cancellationToken: CancellationToken.None);

        // Assert: Returns true
        Assert.True(result);

        // Verify no media methods were called
        _mockMediaService.Verify(x => x.SendImageAsync(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendQuestionMedia_WithDocumentType_SendsDocument()
    {
        // Arrange: Question with PDF document
        var question = CreateQuestionWithMedia(new[]
        {
            CreateMediaItem("document", "/uploads/media/2025/11/report.pdf", "report.pdf", 0)
        });

        _mockMediaService.Setup(x => x.SendDocumentAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mediaHelper.SendQuestionMediaAsync(
            chatId: 123456789,
            question: question,
            cancellationToken: CancellationToken.None);

        // Assert: Verify document was sent
        Assert.True(result);
        _mockMediaService.Verify(x => x.SendDocumentAsync(
            It.Is<long>(c => c == 123456789),
            It.Is<string>(url => url.Contains("report.pdf")),
            It.Is<string>(caption => caption.Contains("Media 1 of 1")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Test Suite 2: Media Error Handling

    [Fact]
    public async Task SendQuestionMedia_WithInvalidMediaUrl_ContinuesAndReturnsFalse()
    {
        // Arrange: Question with invalid image URL
        var question = CreateQuestionWithMedia(new[]
        {
            CreateMediaItem("image", "/invalid/path/photo.jpg", "photo.jpg", 0)
        });

        // Mock: Image send fails after retries
        _mockMediaService.Setup(x => x.SendImageAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _mediaHelper.SendQuestionMediaAsync(
            chatId: 123456789,
            question: question,
            cancellationToken: CancellationToken.None);

        // Assert: Returns false due to media failure
        Assert.False(result);

        // Verify image send was attempted with retries (3 attempts)
        _mockMediaService.Verify(x => x.SendImageAsync(
            It.IsAny<long>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task SendQuestionMedia_WithPartialFailure_ReturnsFalse()
    {
        // Arrange: Question with 3 media items, middle one fails
        var question = CreateQuestionWithMedia(new[]
        {
            CreateMediaItem("image", "/uploads/media/2025/11/photo1.jpg", "photo1.jpg", 0),
            CreateMediaItem("video", "/uploads/media/2025/11/video.mp4", "video.mp4", 1),
            CreateMediaItem("image", "/uploads/media/2025/11/photo2.jpg", "photo2.jpg", 2)
        });

        // Mock: First image succeeds, video fails, second image succeeds
        _mockMediaService.SetupSequence(x => x.SendImageAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)   // photo1.jpg succeeds
            .ReturnsAsync(false)  // photo1.jpg retry (shouldn't happen)
            .ReturnsAsync(false)  // photo1.jpg retry (shouldn't happen)
            .ReturnsAsync(true);  // photo2.jpg succeeds

        _mockMediaService.Setup(x => x.SendVideoAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Video fails all retries

        // Act
        var result = await _mediaHelper.SendQuestionMediaAsync(
            chatId: 123456789,
            question: question,
            cancellationToken: CancellationToken.None);

        // Assert: Returns false due to partial failure
        Assert.False(result);
    }

    [Fact]
    public async Task SendQuestionMedia_WithExceptionThrown_RetriesAndLogWarning()
    {
        // Arrange: Question with image
        var question = CreateQuestionWithMedia(new[]
        {
            CreateMediaItem("image", "/uploads/media/2025/11/photo.jpg", "photo.jpg", 0)
        });

        // Mock: Throw exception on all attempts
        _mockMediaService.Setup(x => x.SendImageAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _mediaHelper.SendQuestionMediaAsync(
            chatId: 123456789,
            question: question,
            cancellationToken: CancellationToken.None);

        // Assert: Returns false due to exception
        Assert.False(result);

        // Verify retries happened (3 attempts)
        _mockMediaService.Verify(x => x.SendImageAsync(
            It.IsAny<long>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));

        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendQuestionMedia_FailsFirstAttempt_RetriesAndSucceeds()
    {
        // Arrange: Question with image
        var question = CreateQuestionWithMedia(new[]
        {
            CreateMediaItem("image", "/uploads/media/2025/11/photo.jpg", "photo.jpg", 0)
        });

        // Mock: Fails 2 times, succeeds on 3rd
        var callCount = 0;
        _mockMediaService.Setup(x => x.SendImageAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount >= 3; // Succeed on 3rd attempt
            });

        // Act
        var result = await _mediaHelper.SendQuestionMediaAsync(
            chatId: 123456789,
            question: question,
            cancellationToken: CancellationToken.None);

        // Assert: Eventually succeeds
        Assert.True(result);

        // Verify exactly 3 attempts
        _mockMediaService.Verify(x => x.SendImageAsync(
            It.IsAny<long>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task SendQuestionMedia_WithUnsupportedMediaType_SkipsItem()
    {
        // Arrange: Question with unsupported media type
        var question = CreateQuestionWithMedia(new[]
        {
            CreateMediaItem("unsupported", "/uploads/media/2025/11/file.xyz", "file.xyz", 0)
        });

        // Act
        var result = await _mediaHelper.SendQuestionMediaAsync(
            chatId: 123456789,
            question: question,
            cancellationToken: CancellationToken.None);

        // Assert: Returns false (unsupported type = failure)
        Assert.False(result);

        // Verify no media methods were called
        _mockMediaService.Verify(x => x.SendImageAsync(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockMediaService.Verify(x => x.SendVideoAsync(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Test Suite 3: TelegramMediaService Integration Tests

    /// <summary>
    /// Note: These tests verify the TelegramMediaService behavior.
    /// Actual Telegram Bot API calls should be tested via integration tests with a test bot.
    /// </summary>

    [Fact]
    public void TelegramMediaService_GetMediaUrl_ConvertsRelativeToAbsolute()
    {
        // Arrange
        var service = new SurveyBot.Bot.Services.TelegramMediaService(
            _mockBotClient.Object,
            Mock.Of<ILogger<SurveyBot.Bot.Services.TelegramMediaService>>());

        var filePath = "/uploads/media/2025/11/photo.jpg";
        var baseUrl = "http://localhost:5000";

        // Act
        var result = service.GetMediaUrl(filePath, baseUrl);

        // Assert
        Assert.Equal("http://localhost:5000/uploads/media/2025/11/photo.jpg", result);
    }

    [Fact]
    public void TelegramMediaService_GetMediaUrl_ReturnsAbsoluteUrlAsIs()
    {
        // Arrange
        var service = new SurveyBot.Bot.Services.TelegramMediaService(
            _mockBotClient.Object,
            Mock.Of<ILogger<SurveyBot.Bot.Services.TelegramMediaService>>());

        var absoluteUrl = "https://example.com/media/photo.jpg";
        var baseUrl = "http://localhost:5000";

        // Act
        var result = service.GetMediaUrl(absoluteUrl, baseUrl);

        // Assert
        Assert.Equal(absoluteUrl, result);
    }

    [Fact]
    public void TelegramMediaService_FormatMediaCaption_FormatsCorrectly()
    {
        // Arrange
        var service = new SurveyBot.Bot.Services.TelegramMediaService(
            _mockBotClient.Object,
            Mock.Of<ILogger<SurveyBot.Bot.Services.TelegramMediaService>>());

        // Act
        var result = service.FormatMediaCaption("What is your favorite color?", 2, 5);

        // Assert
        Assert.Contains("Media 2 of 5", result);
        Assert.Contains("What is your favorite color?", result);
    }

    [Fact]
    public void TelegramMediaService_FormatMediaCaption_WithEmptyText_OnlyShowsMediaCount()
    {
        // Arrange
        var service = new SurveyBot.Bot.Services.TelegramMediaService(
            _mockBotClient.Object,
            Mock.Of<ILogger<SurveyBot.Bot.Services.TelegramMediaService>>());

        // Act
        var result = service.FormatMediaCaption("", 1, 3);

        // Assert
        Assert.Contains("Media 1 of 3", result);
        Assert.DoesNotContain("What is your favorite color?", result);
    }

    #endregion

    #region Helper Methods

    private QuestionDto CreateQuestionWithMedia(MediaItemDto[] mediaItems)
    {
        return new QuestionDto
        {
            Id = 1,
            QuestionText = "Review this content",
            QuestionType = QuestionType.Text,
            MediaContent = new MediaContentDto
            {
                Version = "1.0",
                Items = mediaItems.ToList()
            }
        };
    }

    private MediaItemDto CreateMediaItem(string type, string filePath, string displayName, int order)
    {
        return new MediaItemDto
        {
            Id = Guid.NewGuid().ToString(),
            Type = type,
            FilePath = filePath,
            DisplayName = displayName,
            FileSize = 1024000, // 1MB
            MimeType = GetMimeType(type),
            UploadedAt = DateTime.UtcNow,
            Order = order
        };
    }

    private string GetMimeType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "image" => "image/jpeg",
            "video" => "video/mp4",
            "audio" => "audio/mpeg",
            "document" => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    #endregion
}
