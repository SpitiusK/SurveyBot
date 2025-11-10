using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Infrastructure.Repositories;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = SurveyBot.Core.Entities.User;

namespace SurveyBot.Tests.Fixtures;

/// <summary>
/// Test fixture for bot integration tests.
/// Provides mock Telegram bot client, in-memory database, and required services.
/// </summary>
public class BotTestFixture : IDisposable
{
    public SurveyBotDbContext DbContext { get; }
    public Mock<ITelegramBotClient> MockBotClient { get; }
    public Mock<IBotService> MockBotService { get; }
    public IConversationStateManager StateManager { get; }
    public ISurveyRepository SurveyRepository { get; }
    public IQuestionRepository QuestionRepository { get; }
    public IResponseRepository ResponseRepository { get; }
    public IAnswerRepository AnswerRepository { get; }
    public IUserRepository UserRepository { get; }

    // Test data
    public User TestUser { get; private set; }
    public Survey TestSurvey { get; private set; }
    public List<Question> TestQuestions { get; private set; }

    public BotTestFixture()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
            .UseInMemoryDatabase(databaseName: $"BotTestDb_{Guid.NewGuid()}")
            .Options;
        DbContext = new SurveyBotDbContext(options);

        // Create repositories
        SurveyRepository = new SurveyRepository(DbContext);
        QuestionRepository = new QuestionRepository(DbContext);
        ResponseRepository = new ResponseRepository(DbContext);
        AnswerRepository = new AnswerRepository(DbContext);
        UserRepository = new UserRepository(DbContext);

        // Create mock Telegram bot client
        MockBotClient = new Mock<ITelegramBotClient>();

        // Setup mock to return message IDs
        MockBotClient
            .Setup(x => x.SendTextMessageAsync(
                It.IsAny<ChatId>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<Telegram.Bot.Types.Enums.ParseMode?>(),
                It.IsAny<IEnumerable<Telegram.Bot.Types.MessageEntity>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<int?>(),
                It.IsAny<bool?>(),
                It.IsAny<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatId chatId, string text, int? _, Telegram.Bot.Types.Enums.ParseMode? _,
                IEnumerable<Telegram.Bot.Types.MessageEntity> _, bool? _, bool? _, int? _, bool? _,
                Telegram.Bot.Types.ReplyMarkups.IReplyMarkup _, CancellationToken _) =>
                new Message { MessageId = Random.Shared.Next(1000, 9999), Chat = new Chat { Id = chatId.Identifier ?? 0 } });

        MockBotClient
            .Setup(x => x.AnswerCallbackQueryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Create mock IBotService
        MockBotService = new Mock<IBotService>();
        MockBotService.Setup(x => x.Client).Returns(MockBotClient.Object);

        // Create conversation state manager
        var stateLoggerMock = new Mock<ILogger<ConversationStateManager>>();
        StateManager = new ConversationStateManager(stateLoggerMock.Object);

        // Seed test data
        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        // Create test user
        TestUser = new User
        {
            TelegramId = 123456789,
            Username = "testuser",
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow
        };
        await DbContext.Users.AddAsync(TestUser);
        await DbContext.SaveChangesAsync();

        // Create test survey
        TestSurvey = new Survey
        {
            Title = "Test Survey",
            Description = "A test survey for integration testing",
            CreatorId = TestUser.Id,
            IsActive = true,
            AllowMultipleResponses = false,
            ShowResults = true,
            CreatedAt = DateTime.UtcNow
        };
        await DbContext.Surveys.AddAsync(TestSurvey);
        await DbContext.SaveChangesAsync();

        // Create test questions
        TestQuestions = new List<Question>
        {
            new Question
            {
                SurveyId = TestSurvey.Id,
                QuestionText = "What is your name?",
                QuestionType = QuestionType.Text,
                OrderIndex = 0,
                IsRequired = true,
                CreatedAt = DateTime.UtcNow
            },
            new Question
            {
                SurveyId = TestSurvey.Id,
                QuestionText = "What is your favorite color?",
                QuestionType = QuestionType.SingleChoice,
                OrderIndex = 1,
                IsRequired = true,
                OptionsJson = System.Text.Json.JsonSerializer.Serialize(new[] { "Red", "Blue", "Green", "Yellow" }),
                CreatedAt = DateTime.UtcNow
            },
            new Question
            {
                SurveyId = TestSurvey.Id,
                QuestionText = "Which programming languages do you know?",
                QuestionType = QuestionType.MultipleChoice,
                OrderIndex = 2,
                IsRequired = false,
                OptionsJson = System.Text.Json.JsonSerializer.Serialize(new[] { "C#", "Python", "JavaScript", "Java" }),
                CreatedAt = DateTime.UtcNow
            },
            new Question
            {
                SurveyId = TestSurvey.Id,
                QuestionText = "Rate your experience",
                QuestionType = QuestionType.Rating,
                OrderIndex = 3,
                IsRequired = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await DbContext.Questions.AddRangeAsync(TestQuestions);
        await DbContext.SaveChangesAsync();
    }

    public Message CreateTestMessage(long userId, long chatId, string text)
    {
        return new Message
        {
            MessageId = Random.Shared.Next(1000, 9999),
            From = new Telegram.Bot.Types.User { Id = userId, Username = "testuser" },
            Chat = new Chat { Id = chatId },
            Text = text,
            Date = DateTime.UtcNow
        };
    }

    public CallbackQuery CreateTestCallbackQuery(long userId, long chatId, string data)
    {
        return new CallbackQuery
        {
            Id = Guid.NewGuid().ToString(),
            From = new Telegram.Bot.Types.User { Id = userId, Username = "testuser" },
            Message = new Message
            {
                MessageId = Random.Shared.Next(1000, 9999),
                Chat = new Chat { Id = chatId }
            },
            Data = data
        };
    }

    public void Dispose()
    {
        DbContext?.Dispose();
    }
}
