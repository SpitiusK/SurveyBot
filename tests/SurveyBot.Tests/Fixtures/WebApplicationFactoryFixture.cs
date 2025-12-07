using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Storage;
using SurveyBot.Infrastructure.Data;

namespace SurveyBot.Tests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Configures in-memory database and test-specific services.
/// </summary>
public class WebApplicationFactoryFixture<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _databaseName;
    private readonly InMemoryDatabaseRoot _databaseRoot;

    public WebApplicationFactoryFixture()
    {
        // Use a unique database name for each test run
        _databaseName = Guid.NewGuid().ToString();
        // Share the in-memory database root to ensure proper isolation
        _databaseRoot = new InMemoryDatabaseRoot();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear existing configuration sources to avoid conflicts
            config.Sources.Clear();

            // Load the base configuration from the API project
            // Calculate correct path from test bin directory to API project
            var testBinDir = Directory.GetCurrentDirectory();

            // From: C:\Users\User\Desktop\SurveyBot\tests\SurveyBot.Tests\bin\Debug\net8.0
            // To:   C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
            // Go up 5 levels: bin -> Debug -> net8.0 -> SurveyBot.Tests -> tests -> SurveyBot
            var solutionRoot = new DirectoryInfo(testBinDir).Parent?.Parent?.Parent?.Parent?.Parent?.FullName;

            if (string.IsNullOrEmpty(solutionRoot) || !Directory.Exists(solutionRoot))
            {
                throw new InvalidOperationException($"Could not find solution root from test bin directory: {testBinDir}");
            }

            var apiProjectPath = Path.Combine(solutionRoot, "src", "SurveyBot.API");

            if (!Directory.Exists(apiProjectPath))
            {
                throw new InvalidOperationException($"API project directory not found at: {apiProjectPath}");
            }

            // Set base path and load appsettings.json from API project
            config.SetBasePath(apiProjectPath);
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            // Override with test configuration using in-memory collection
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Disable Serilog Seq sink to avoid connection issues
                ["Serilog:WriteTo:2:Name"] = "Console", // Override Seq with Console
                ["Serilog:MinimumLevel:Default"] = "Warning", // Reduce log noise in tests

                ["JwtSettings:SecretKey"] = "SurveyBot-Super-Secret-Key-For-JWT-Token-Generation-2025-Change-In-Production",
                ["JwtSettings:Issuer"] = "SurveyBot.API",
                ["JwtSettings:Audience"] = "SurveyBot.Clients",
                ["JwtSettings:TokenLifetimeHours"] = "24",
                ["BotConfiguration:BotToken"] = "8540672675:AAHX9frxfMqVRoGEKspNj5Nxlm9JIodtG1Q",
                ["BotConfiguration:BotUsername"] = "@TestBot",
                ["BotConfiguration:UseWebhook"] = "false",
                ["BotConfiguration:ApiBaseUrl"] = "http://localhost:5000",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            });
        });

        // Set environment to Testing
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Program.cs skips DbContext registration in Testing environment
            // so we can safely register our InMemory database here
            services.AddDbContext<SurveyBotDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName, _databaseRoot);
                options.EnableSensitiveDataLogging();
                // Suppress transaction warning - InMemory database doesn't support transactions
                // but all operations are atomic by default, so this is safe for testing
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            // Remove any existing hosted services (background tasks)
            var hostedServices = services.Where(d =>
                d.ServiceType != null &&
                (d.ServiceType.Name.Contains("HostedService") ||
                 d.ServiceType.Name.Contains("QueuedHostedService"))).ToList();
            foreach (var service in hostedServices)
            {
                services.Remove(service);
            }

            // Remove background task queue
            var queueDescriptor = services.FirstOrDefault(d => d.ServiceType?.Name == "IBackgroundTaskQueue");
            if (queueDescriptor != null)
            {
                services.Remove(queueDescriptor);
            }

            // Remove Telegram bot service for testing
            var botServiceDescriptor = services.FirstOrDefault(d => d.ServiceType?.Name == "IBotService");
            if (botServiceDescriptor != null)
            {
                services.Remove(botServiceDescriptor);
            }

            // Remove all handlers registered by AddBotHandlers
            var updateHandlerDescriptor = services.FirstOrDefault(d => d.ServiceType?.Name == "IUpdateHandler");
            if (updateHandlerDescriptor != null)
            {
                services.Remove(updateHandlerDescriptor);
            }

            var commandRouterDescriptor = services.FirstOrDefault(d => d.ServiceType?.Name == "ICommandRouter");
            if (commandRouterDescriptor != null)
            {
                services.Remove(commandRouterDescriptor);
            }

            var conversationStateDescriptor = services.FirstOrDefault(d => d.ServiceType?.Name == "IConversationStateManager");
            if (conversationStateDescriptor != null)
            {
                services.Remove(conversationStateDescriptor);
            }

            // Register a dummy IBotService for testing
            services.AddScoped<SurveyBot.Bot.Interfaces.IBotService>(sp =>
            {
                return new TestBotService();
            });

            // Replace Serilog with simple console logging for tests
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Warning);
            });

            // Note: Database is created on demand by the InMemory provider
        });
    }

    /// <summary>
    /// Ensures the database is created and seeded after the factory is built.
    /// </summary>
    private void EnsureDatabaseCreated()
    {
        try
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
            db.Database.EnsureCreated();
        }
        catch
        {
            // Ignore errors during database creation - it may already exist
        }
    }

    /// <summary>
    /// Gets a scoped service from the test server.
    /// </summary>
    public T GetScopedService<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Seeds the database with test data.
    /// </summary>
    public void SeedDatabase(Action<SurveyBotDbContext> seedAction)
    {
        // Create a fresh DbContext directly to avoid the dual-provider issue
        var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
            .UseInMemoryDatabase(_databaseName, _databaseRoot)
            .Options;

        using var db = new SurveyBotDbContext(options);
        seedAction(db);
        db.SaveChanges();
    }

    /// <summary>
    /// Clears all data from the database.
    /// </summary>
    public void ClearDatabase()
    {
        // Create a fresh DbContext directly to avoid the dual-provider issue
        var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
            .UseInMemoryDatabase(_databaseName, _databaseRoot)
            .Options;

        using var db = new SurveyBotDbContext(options);

        db.Answers.RemoveRange(db.Answers);
        db.Responses.RemoveRange(db.Responses);
        db.Questions.RemoveRange(db.Questions);
        db.Surveys.RemoveRange(db.Surveys);
        db.Users.RemoveRange(db.Users);

        db.SaveChanges();
    }
}

/// <summary>
/// Dummy Telegram bot service for testing.
/// </summary>
public class TestBotService : SurveyBot.Bot.Interfaces.IBotService
{
    public Telegram.Bot.ITelegramBotClient Client => throw new NotImplementedException();

    public Task<Telegram.Bot.Types.User> InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Return a mock user
        return Task.FromResult(new Telegram.Bot.Types.User
        {
            Id = 123456789,
            IsBot = true,
            FirstName = "TestBot"
        });
    }

    public Task<bool> SetWebhookAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<bool> RemoveWebhookAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<Telegram.Bot.Types.WebhookInfo> GetWebhookInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Telegram.Bot.Types.WebhookInfo());
    }

    public Task<Telegram.Bot.Types.User> GetMeAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Telegram.Bot.Types.User
        {
            Id = 123456789,
            IsBot = true,
            FirstName = "TestBot"
        });
    }

    public bool ValidateWebhookSecret(string? secretToken) => true;

    public Task<Telegram.Bot.Types.Message> SendMessageAsync(
        Telegram.Bot.Types.ChatId chatId,
        string text,
        Telegram.Bot.Types.Enums.ParseMode? parseMode = null,
        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Telegram.Bot.Types.Message
        {
            Id = Random.Shared.Next(1000, 9999),
            Chat = new Telegram.Bot.Types.Chat { Id = chatId.Identifier ?? 0 },
            Text = text,
            Date = DateTime.UtcNow
        });
    }

    public Task<Telegram.Bot.Types.Message> EditMessageTextAsync(
        Telegram.Bot.Types.ChatId chatId,
        int messageId,
        string text,
        Telegram.Bot.Types.Enums.ParseMode? parseMode = null,
        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Telegram.Bot.Types.Message
        {
            Id = messageId,
            Chat = new Telegram.Bot.Types.Chat { Id = chatId.Identifier ?? 0 },
            Text = text,
            Date = DateTime.UtcNow
        });
    }

    public Task AnswerCallbackQueryAsync(
        string callbackQueryId,
        string? text = null,
        bool showAlert = false,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task DeleteMessageAsync(
        Telegram.Bot.Types.ChatId chatId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
