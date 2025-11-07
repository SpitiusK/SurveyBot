using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SurveyBot.Infrastructure.Data;

namespace SurveyBot.Tests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Configures in-memory database and test-specific services.
/// </summary>
public class WebApplicationFactoryFixture<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _databaseName;

    public WebApplicationFactoryFixture()
    {
        // Use a unique database name for each test run
        _databaseName = Guid.NewGuid().ToString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-key-for-integration-tests-minimum-32-characters-long",
                ["JwtSettings:Issuer"] = "SurveyBot.Test",
                ["JwtSettings:Audience"] = "SurveyBot.Test",
                ["JwtSettings:ExpiryMinutes"] = "60",
                ["TelegramSettings:BotToken"] = "test-bot-token",
                ["TelegramSettings:WebhookUrl"] = "https://test.example.com/webhook",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the app's DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SurveyBotDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext using in-memory database for testing
            services.AddDbContext<SurveyBotDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
            });

            // Remove any existing hosted services (background tasks)
            var hostedServices = services.Where(d => d.ServiceType.Name.Contains("HostedService")).ToList();
            foreach (var service in hostedServices)
            {
                services.Remove(service);
            }

            // Suppress logging noise during tests
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Warning);
            });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<SurveyBotDbContext>();

            // Ensure the database is created
            db.Database.EnsureCreated();
        });
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
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
        seedAction(db);
        db.SaveChanges();
    }

    /// <summary>
    /// Clears all data from the database.
    /// </summary>
    public void ClearDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();

        db.Answers.RemoveRange(db.Answers);
        db.Responses.RemoveRange(db.Responses);
        db.Questions.RemoveRange(db.Questions);
        db.Surveys.RemoveRange(db.Surveys);
        db.Users.RemoveRange(db.Users);

        db.SaveChanges();
    }
}
