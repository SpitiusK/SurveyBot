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
using System.Collections.Concurrent;
// ConditionalWeakTable removed - replaced with ConcurrentDictionary (TEST-FAIL-AUTH-004)

namespace SurveyBot.Tests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Configures in-memory database and test-specific services.
/// </summary>
public class WebApplicationFactoryFixture<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    // Fix TEST-FAIL-AUTH-004: Store database name at instance level instead of AsyncLocal
    // AsyncLocal doesn't reliably flow through CreateScope() calls, causing new database names
    // Each factory instance (one per test) gets its own database name, set during InitializeAsync
    private string? _databaseName;

    // Fix TEST-FAIL-AUTH-004: Use ConcurrentDictionary instead of ConditionalWeakTable
    // ConditionalWeakTable uses reference equality for strings, causing scope mismatch issues
    // ConcurrentDictionary uses value equality (string content), ensuring same root for same DB name
    // Each database name gets its own root for auto-increment isolation
    private readonly ConcurrentDictionary<string, InMemoryDatabaseRoot> _databaseRoots
        = new ConcurrentDictionary<string, InMemoryDatabaseRoot>();

    /// <summary>
    /// Gets or creates a unique database name for the current async context (test method).
    /// Uses AsyncLocal to ensure thread-safe per-test-method isolation.
    /// </summary>
    /// <returns>Unique database name for this test method</returns>
    public string GetOrCreateDatabaseName()
    {
        if (_databaseName == null)
        {
            _databaseName = $"TestDb_{Guid.NewGuid():N}"; // :N for compact format (no hyphens)
        }
        return _databaseName;
    }

    /// <summary>
    /// Resets the database name for the current factory instance.
    /// Called by IntegrationTestBase.InitializeAsync() before each test method.
    /// This ensures each test method gets a fresh unique database.
    /// </summary>
    public void ResetDatabaseName()
    {
        _databaseName = null;
    }

    /// <summary>
    /// Gets or creates the InMemoryDatabaseRoot for the specified database name.
    /// Uses ConcurrentDictionary.GetOrAdd to ensure thread-safe access with value equality.
    /// </summary>
    /// <param name="databaseName">The database name (uses string value equality)</param>
    /// <returns>The InMemoryDatabaseRoot for this database</returns>
    private InMemoryDatabaseRoot GetOrCreateDatabaseRoot(string databaseName)
    {
        return _databaseRoots.GetOrAdd(databaseName, _ => new InMemoryDatabaseRoot());
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

        builder.ConfigureServices((context, services) =>
        {
            // Program.cs skips DbContext registration in Testing environment
            // so we can safely register our InMemory database here
            services.AddDbContext<SurveyBotDbContext>(options =>
            {
                // Get or create unique InMemoryDatabaseRoot for this database name
                // This isolates auto-increment counters between tests
                // Fix TEST-FAIL-AUTH-004: Use GetOrCreateDatabaseRoot (ConcurrentDictionary) for value equality
                var databaseName = GetOrCreateDatabaseName();
                var root = GetOrCreateDatabaseRoot(databaseName);

                options.UseInMemoryDatabase(databaseName, root);
                options.EnableSensitiveDataLogging();
                // Suppress warnings for test scenarios
                options.ConfigureWarnings(warnings =>
                {
                    // InMemory database doesn't support transactions, but operations are atomic by default
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning);
                    // TEST-FLAKY-AUTH-003 (Phase 2): Suppress ManyServiceProvidersCreated warning
                    // Creating multiple factories per test is expected for complete test isolation
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning);
                });
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

            // Fix TEST-FLAKY-AUTH-002: Eager JWT configuration binding (eliminates race condition)
            // Register JwtSettings as singleton with Options.Create to guarantee immediate availability
            // This replaces async IOptions<T> binding that caused 90% test failure rate
            var jwtSettings = new SurveyBot.Core.Configuration.JwtSettings
            {
                SecretKey = "SurveyBot-Super-Secret-Key-For-JWT-Token-Generation-2025-Change-In-Production",
                Issuer = "SurveyBot.API",
                Audience = "SurveyBot.Clients",
                TokenLifetimeHours = 24
            };
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(jwtSettings));

            // Note: Database is created on demand by the InMemory provider
        });
    }

    /// <summary>
    /// Ensures the test server is built and started.
    /// Safe to call multiple times (idempotent).
    /// </summary>
    public void EnsureServerStarted()
    {
        // Accessing Server property triggers lazy build if not already built
        // Subsequent calls are no-ops if server already exists
        _ = Server;
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
        // Get the unique InMemoryDatabaseRoot for this database name
        // Fix TEST-FAIL-AUTH-004: Use GetOrCreateDatabaseRoot (ConcurrentDictionary) for value equality
        var databaseName = GetOrCreateDatabaseName();
        var root = GetOrCreateDatabaseRoot(databaseName);

        // Create a fresh DbContext directly to avoid the dual-provider issue
        var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
            .UseInMemoryDatabase(databaseName, root)
            .ConfigureWarnings(warnings =>
            {
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning);
                // TEST-FLAKY-AUTH-003 (Phase 2): Suppress warning for instance-per-test pattern
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning);
            })
            .Options;

        using var db = new SurveyBotDbContext(options);
        seedAction(db);
        db.SaveChanges();
    }

    /// <summary>
    /// Clears all data from the database and resets auto-increment counters.
    /// </summary>
    /// <remarks>
    /// Uses EnsureDeleted() + EnsureCreated() to reset auto-increment sequences.
    /// Fix TEST-FLAKY-AUTH-002: Now uses unique InMemoryDatabaseRoot per database name,
    /// ensuring true isolation of auto-increment counters between tests.
    /// </remarks>
    public void ClearDatabase()
    {
        // Get the unique InMemoryDatabaseRoot for this database name
        // Fix TEST-FAIL-AUTH-004: Use GetOrCreateDatabaseRoot (ConcurrentDictionary) for value equality
        var databaseName = GetOrCreateDatabaseName();
        var root = GetOrCreateDatabaseRoot(databaseName);

        // Create a fresh DbContext directly to avoid the dual-provider issue
        var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
            .UseInMemoryDatabase(databaseName, root)
            .ConfigureWarnings(warnings =>
            {
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning);
                // TEST-FLAKY-AUTH-003 (Phase 2): Suppress warning for instance-per-test pattern
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning);
            })
            .Options;

        using var db = new SurveyBotDbContext(options);

        // Drop and recreate database to reset auto-increment counters
        // This ensures User.Id, Survey.Id, etc. always start at 1 for each test
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
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
