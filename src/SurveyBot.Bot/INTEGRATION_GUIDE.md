# Integration Guide: Adding Telegram Bot to SurveyBot.API

This guide shows how to integrate the Telegram Bot services into your existing SurveyBot.API application.

## Step 1: Add Project Reference

Add a reference to `SurveyBot.Bot` in the `SurveyBot.API` project.

**Edit `SurveyBot.API.csproj`:**
```xml
<ItemGroup>
  <ProjectReference Include="..\SurveyBot.Core\SurveyBot.Core.csproj" />
  <ProjectReference Include="..\SurveyBot.Infrastructure\SurveyBot.Infrastructure.csproj" />
  <ProjectReference Include="..\SurveyBot.Bot\SurveyBot.Bot.csproj" />
</ItemGroup>
```

**Or use CLI:**
```bash
cd src/SurveyBot.API
dotnet add reference ../SurveyBot.Bot/SurveyBot.Bot.csproj
```

## Step 2: Configure Bot Token (User Secrets)

**Using Visual Studio:**
1. Right-click on `SurveyBot.API` project
2. Select "Manage User Secrets"
3. Add bot configuration:

```json
{
  "BotConfiguration": {
    "BotToken": "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz",
    "BotUsername": "your_bot_username"
  }
}
```

**Using CLI:**
```bash
cd src/SurveyBot.API
dotnet user-secrets set "BotConfiguration:BotToken" "YOUR_BOT_TOKEN_HERE"
dotnet user-secrets set "BotConfiguration:BotUsername" "your_bot_username"
```

## Step 3: Update Program.cs

Add bot services registration and initialization to your `Program.cs`:

```csharp
using SurveyBot.Bot.Extensions; // Add this using statement

// ... existing code ...

var builder = WebApplication.CreateBuilder(args);

// ... existing service registrations ...

// Add Telegram Bot Services
builder.Services.AddTelegramBot(builder.Configuration);

// ... rest of your service registrations ...

var app = builder.Build();

// Initialize Telegram Bot (before app.Run())
try
{
    Log.Information("Initializing Telegram Bot...");
    await app.Services.InitializeTelegramBotAsync();
    Log.Information("Telegram Bot initialized successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to initialize Telegram Bot");
    // Decide if you want to continue without bot or fail fast
    // throw; // Uncomment to fail fast
}

// ... existing middleware and app.Run() ...
```

## Step 4: Complete Program.cs Example

Here's a complete example showing where to add bot initialization:

```csharp
using Microsoft.EntityFrameworkCore;
using Serilog;
using SurveyBot.API.Extensions;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Infrastructure.Repositories;
using SurveyBot.Bot.Extensions; // ADD THIS

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddUserSecrets<Program>() // ADD THIS to load user secrets
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "SurveyBot.API")
    .CreateLogger();

try
{
    Log.Information("Starting SurveyBot API application");

    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with Serilog
    builder.Host.UseSerilog();

    // Add Controllers
    builder.Services.AddControllers();

    // Configure Entity Framework Core with PostgreSQL
    builder.Services.AddDbContext<SurveyBotDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString);

        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

    // Register Repository Implementations
    builder.Services.AddScoped<ISurveyRepository, SurveyRepository>();
    builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
    builder.Services.AddScoped<IResponseRepository, ResponseRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();

    // ADD THIS: Register Telegram Bot Services
    builder.Services.AddTelegramBot(builder.Configuration);

    // Add Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<SurveyBotDbContext>(
            name: "database",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
            tags: new[] { "db", "sql", "postgresql" });

    // Swagger configuration
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Verify DI Configuration in Development
    if (app.Environment.IsDevelopment())
    {
        var (success, errors) = SurveyBot.API.DIVerifier.VerifyServiceResolution(app.Services);
        SurveyBot.API.DIVerifier.PrintVerificationResults(success, errors);
    }

    // ADD THIS: Initialize Telegram Bot
    try
    {
        Log.Information("Initializing Telegram Bot...");
        await app.Services.InitializeTelegramBotAsync();
        Log.Information("Telegram Bot initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize Telegram Bot - Bot features will be unavailable");
        // Continue running without bot if initialization fails
        // Remove this and use 'throw;' if you want to fail fast
    }

    // Configure the HTTP request pipeline
    app.UseSerilogRequestLogging();
    app.UseRequestLogging();
    app.UseGlobalExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.MapControllers();
    app.MapHealthChecks("/health/db");

    Log.Information("SurveyBot API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

## Step 5: Verify Installation

### Check Logs

When you run the application, you should see:

```
[12:34:56 INF] Initializing Telegram Bot...
[12:34:56 INF] BotService created successfully
[12:34:56 INF] Initializing bot...
[12:34:57 INF] Bot initialized successfully. Bot: @your_bot_username (ID: 123456789)
[12:34:57 INF] Long polling mode enabled (webhook disabled)
[12:34:57 INF] Telegram Bot initialization completed successfully
[12:34:57 INF] Telegram Bot initialized successfully
```

### Test Bot Service Injection

Create a test controller to verify bot is registered:

**Create `Controllers/BotTestController.cs`:**
```csharp
using Microsoft.AspNetCore.Mvc;
using SurveyBot.Bot.Interfaces;

namespace SurveyBot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BotTestController : ControllerBase
{
    private readonly IBotService _botService;
    private readonly ILogger<BotTestController> _logger;

    public BotTestController(IBotService botService, ILogger<BotTestController> logger)
    {
        _botService = botService;
        _logger = logger;
    }

    [HttpGet("info")]
    public async Task<IActionResult> GetBotInfo()
    {
        try
        {
            var botInfo = await _botService.GetMeAsync();
            return Ok(new
            {
                Username = botInfo.Username,
                Id = botInfo.Id,
                FirstName = botInfo.FirstName,
                CanJoinGroups = botInfo.CanJoinGroups,
                CanReadAllGroupMessages = botInfo.CanReadAllGroupMessages,
                SupportsInlineQueries = botInfo.SupportsInlineQueries
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bot info");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("webhook")]
    public async Task<IActionResult> GetWebhookInfo()
    {
        try
        {
            var webhookInfo = await _botService.GetWebhookInfoAsync();
            return Ok(new
            {
                Url = webhookInfo.Url,
                HasCustomCertificate = webhookInfo.HasCustomCertificate,
                PendingUpdateCount = webhookInfo.PendingUpdateCount,
                LastErrorMessage = webhookInfo.LastErrorMessage,
                LastErrorDate = webhookInfo.LastErrorDate,
                MaxConnections = webhookInfo.MaxConnections,
                AllowedUpdates = webhookInfo.AllowedUpdates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get webhook info");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
```

Test the endpoints:
- `GET https://localhost:5001/api/bottest/info`
- `GET https://localhost:5001/api/bottest/webhook`

## Step 6: Update appsettings (Production)

For production deployment, update `appsettings.json` with webhook configuration:

```json
{
  "BotConfiguration": {
    "BotToken": "",
    "BotUsername": "",
    "UseWebhook": true,
    "WebhookUrl": "https://yourdomain.com",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "",
    "MaxConnections": 40,
    "ApiBaseUrl": "https://yourdomain.com",
    "RequestTimeout": 30
  }
}
```

Configure actual values via environment variables or secure configuration providers:
- Azure: Key Vault
- AWS: Secrets Manager
- Docker: Environment variables or secrets

## Troubleshooting

### Bot Token Not Found

If you see: `Invalid bot configuration: BotToken is required`

1. Verify user secrets are configured
2. Check `BotConfiguration` section in appsettings
3. Ensure environment variables are set
4. Add `.AddUserSecrets<Program>()` to configuration builder

### DI Resolution Failed

If bot service can't be resolved:

1. Verify `AddTelegramBot()` is called before `builder.Build()`
2. Check project reference to `SurveyBot.Bot` is added
3. Ensure `using SurveyBot.Bot.Extensions;` is included

### Bot Initialization Failed

Check the exception message:
- Invalid token: Verify token from BotFather
- Network error: Check internet connection
- Timeout: Increase `RequestTimeout` in configuration

## Next Steps

1. Create webhook controller for receiving updates
2. Implement update handlers (commands, messages, callbacks)
3. Create bot command handlers
4. Implement survey flow logic
5. Add user state management

## Additional Configuration Options

### Add Logging Override for Bot

In `appsettings.Development.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "SurveyBot.Bot": "Debug",
        "Telegram.Bot": "Information"
      }
    }
  }
}
```

### Configure Request Timeout

Increase timeout for slow connections:

```json
{
  "BotConfiguration": {
    "RequestTimeout": 60
  }
}
```

### Multiple Environments

Create environment-specific settings:

- `appsettings.Development.json` - Development (long polling)
- `appsettings.Staging.json` - Staging (webhook)
- `appsettings.Production.json` - Production (webhook)
