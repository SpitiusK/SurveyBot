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
                // Telegram.Bot 22.x renamed MessageId to Id
                return new Message
                {
                    Id = Random.Shared.Next(1000, 9999),
                    Chat = new Chat { Id = request.ChatId.Identifier ?? 0, Type = Telegram.Bot.Types.Enums.ChatType.Private },
                    Date = DateTime.UtcNow
                };
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
                // Telegram.Bot 22.x renamed MessageId to Id
                return new Message
                {
                    Id = request.MessageId,
                    Chat = new Chat { Id = request.ChatId.Identifier ?? 0, Type = Telegram.Bot.Types.Enums.ChatType.Private },
                    Text = request.Text,
                    Date = DateTime.UtcNow
                };
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
        // Create test user using EntityBuilder
        TestUser = EntityBuilder.CreateUser(
            telegramId: 123456789,
            username: "testuser",
            firstName: "Test",
            lastName: "User"
        );
        await DbContext.Users.AddAsync(TestUser);
        await DbContext.SaveChangesAsync();

        // Create test survey using EntityBuilder
        TestSurvey = EntityBuilder.CreateSurvey(
            title: "Test Survey",
            description: "A test survey for integration testing",
            creatorId: TestUser.Id,
            isActive: true
        );
        await DbContext.Surveys.AddAsync(TestSurvey);
        await DbContext.SaveChangesAsync();

        // Create test questions
        TestQuestions = new List<Question>();

        // Question 1: Text question
        var question1 = EntityBuilder.CreateQuestion(
            surveyId: TestSurvey.Id,
            questionText: "What is your name?",
            questionType: QuestionType.Text,
            orderIndex: 0,
            isRequired: true
        );
        TestQuestions.Add(question1);

        // Question 2: SingleChoice question
        var question2 = EntityBuilder.CreateQuestion(
            surveyId: TestSurvey.Id,
            questionText: "What is your favorite color?",
            questionType: QuestionType.SingleChoice,
            orderIndex: 1,
            isRequired: true
        );
        question2.SetOptionsJson(System.Text.Json.JsonSerializer.Serialize(new[] { "Red", "Blue", "Green", "Yellow" }));
        TestQuestions.Add(question2);

        // Question 3: MultipleChoice question
        var question3 = EntityBuilder.CreateQuestion(
            surveyId: TestSurvey.Id,
            questionText: "Which programming languages do you know?",
            questionType: QuestionType.MultipleChoice,
            orderIndex: 2,
            isRequired: false
        );
        question3.SetOptionsJson(System.Text.Json.JsonSerializer.Serialize(new[] { "C#", "Python", "JavaScript", "Java" }));
        TestQuestions.Add(question3);

        // Question 4: Rating question
        var question4 = EntityBuilder.CreateQuestion(
            surveyId: TestSurvey.Id,
            questionText: "Rate your experience",
            questionType: QuestionType.Rating,
            orderIndex: 3,
            isRequired: true
        );
        TestQuestions.Add(question4);

        await DbContext.Questions.AddRangeAsync(TestQuestions);
        await DbContext.SaveChangesAsync();
    }

    public Message CreateTestMessage(long userId, long chatId, string text)
    {
        // Telegram.Bot 22.x renamed MessageId to Id
        return new Message
        {
            Id = Random.Shared.Next(1000, 9999),
            From = new Telegram.Bot.Types.User { Id = userId, Username = "testuser", FirstName = "Test", IsBot = false },
            Chat = new Chat { Id = chatId, Type = Telegram.Bot.Types.Enums.ChatType.Private },
            Text = text,
            Date = DateTime.UtcNow
        };
    }

    public CallbackQuery CreateTestCallbackQuery(long userId, long chatId, string data)
    {
        // Telegram.Bot 22.x renamed MessageId to Id
        var innerMessage = new Message
        {
            Id = Random.Shared.Next(1000, 9999),
            Chat = new Chat { Id = chatId, Type = Telegram.Bot.Types.Enums.ChatType.Private },
            Date = DateTime.UtcNow
        };

        return new CallbackQuery
        {
            Id = Guid.NewGuid().ToString(),
            From = new Telegram.Bot.Types.User { Id = userId, Username = "testuser", FirstName = "Test", IsBot = false },
            Message = innerMessage,
            Data = data
        };
    }

    public void Dispose()
    {
        DbContext?.Dispose();
    }
}
