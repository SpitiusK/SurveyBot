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
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
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

        // Setup mock to return message IDs for SendRequest (core method used by SendMessage)
        MockBotClient
            .Setup(x => x.SendRequest(
                It.IsAny<SendMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((SendMessageRequest request, CancellationToken ct) =>
            {
                var message = new Message();
                var messageIdProperty = typeof(Message).GetProperty("MessageId");
                var chatProperty = typeof(Message).GetProperty("Chat");
                messageIdProperty?.SetValue(message, Random.Shared.Next(1000, 9999));
                chatProperty?.SetValue(message, new Chat { Id = request.ChatId.Identifier ?? 0 });
                return message;
            });

        // Setup mock for AnswerCallbackQueryRequest
        MockBotClient
            .Setup(x => x.SendRequest(
                It.IsAny<AnswerCallbackQueryRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Setup mock for EditMessageTextRequest
        MockBotClient
            .Setup(x => x.SendRequest(
                It.IsAny<EditMessageTextRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((EditMessageTextRequest request, CancellationToken ct) =>
            {
                var message = new Message();
                var messageIdProperty = typeof(Message).GetProperty("MessageId");
                var chatProperty = typeof(Message).GetProperty("Chat");
                var textProperty = typeof(Message).GetProperty("Text");

                messageIdProperty?.SetValue(message, request.MessageId);
                chatProperty?.SetValue(message, new Chat { Id = request.ChatId.Identifier ?? 0 });
                textProperty?.SetValue(message, request.Text);

                return message;
            });

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
        var message = new Message();
        var messageIdProperty = typeof(Message).GetProperty("MessageId");
        var fromProperty = typeof(Message).GetProperty("From");
        var chatProperty = typeof(Message).GetProperty("Chat");
        var textProperty = typeof(Message).GetProperty("Text");
        var dateProperty = typeof(Message).GetProperty("Date");

        messageIdProperty?.SetValue(message, Random.Shared.Next(1000, 9999));
        fromProperty?.SetValue(message, new Telegram.Bot.Types.User { Id = userId, Username = "testuser" });
        chatProperty?.SetValue(message, new Chat { Id = chatId });
        textProperty?.SetValue(message, text);
        dateProperty?.SetValue(message, DateTime.UtcNow);

        return message;
    }

    public CallbackQuery CreateTestCallbackQuery(long userId, long chatId, string data)
    {
        var innerMessage = new Message();
        var messageIdProperty = typeof(Message).GetProperty("MessageId");
        var chatProperty = typeof(Message).GetProperty("Chat");
        messageIdProperty?.SetValue(innerMessage, Random.Shared.Next(1000, 9999));
        chatProperty?.SetValue(innerMessage, new Chat { Id = chatId });

        var callbackQuery = new CallbackQuery();
        var idProperty = typeof(CallbackQuery).GetProperty("Id");
        var fromProperty = typeof(CallbackQuery).GetProperty("From");
        var messageProperty = typeof(CallbackQuery).GetProperty("Message");
        var dataProperty = typeof(CallbackQuery).GetProperty("Data");

        idProperty?.SetValue(callbackQuery, Guid.NewGuid().ToString());
        fromProperty?.SetValue(callbackQuery, new Telegram.Bot.Types.User { Id = userId, Username = "testuser" });
        messageProperty?.SetValue(callbackQuery, innerMessage);
        dataProperty?.SetValue(callbackQuery, data);

        return callbackQuery;
    }

    public void Dispose()
    {
        DbContext?.Dispose();
    }
}
