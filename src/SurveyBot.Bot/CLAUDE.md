# SurveyBot.Bot - Telegram Bot Layer Documentation

## Overview

**SurveyBot.Bot** implements the Telegram bot logic and handles all interactions with Telegram's Bot API. This layer manages bot commands, message handling, webhook processing, and conversation flows.

## Purpose

- Handle Telegram bot commands
- Process incoming updates (messages, callbacks)
- Manage conversation state
- Route commands to appropriate handlers
- Support both webhook and polling modes
- Provide bot client abstraction

## Dependencies

```
SurveyBot.Bot
    ├── SurveyBot.Core (domain layer)
    ├── Telegram.Bot 22.7.4 (NuGet)
    ├── Microsoft.Extensions.DependencyInjection (NuGet)
    ├── Microsoft.Extensions.Logging (NuGet)
    └── Microsoft.Extensions.Options (NuGet)
```

**Important**: Bot layer depends ONLY on Core, not on Infrastructure or API.

---

## Project Structure

```
SurveyBot.Bot/
├── Configuration/
│   └── BotConfiguration.cs          # Bot settings model
├── Services/
│   ├── BotService.cs                # Bot client lifecycle
│   ├── UpdateHandler.cs             # Process incoming updates
│   └── CommandRouter.cs             # Route commands to handlers
├── Handlers/
│   └── Commands/                    # Command handler implementations
│       ├── StartCommandHandler.cs
│       ├── HelpCommandHandler.cs
│       ├── MySurveysCommandHandler.cs
│       └── SurveysCommandHandler.cs
├── Interfaces/
│   ├── IBotService.cs               # Bot service contract
│   ├── IUpdateHandler.cs            # Update handler contract
│   └── ICommandHandler.cs           # Command handler contract
└── Extensions/
    ├── ServiceCollectionExtensions.cs    # DI registration
    └── BotServiceExtensions.cs           # Bot initialization helpers
```

---

## Configuration

### BotConfiguration

**Location**: `Configuration/BotConfiguration.cs`

**Properties**:
```csharp
public class BotConfiguration
{
    public const string SectionName = "BotConfiguration";

    public string BotToken { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string WebhookPath { get; set; } = "/api/bot/webhook";
    public string WebhookSecret { get; set; } = string.Empty;
    public int MaxConnections { get; set; } = 40;
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";
    public bool UseWebhook { get; set; } = false;
    public string BotUsername { get; set; } = string.Empty;
    public int RequestTimeout { get; set; } = 30;

    public string FullWebhookUrl => $"{WebhookUrl.TrimEnd('/')}{WebhookPath}";

    public bool IsValid(out List<string> errors)
    {
        // Validation logic
    }
}
```

### appsettings.json Configuration

```json
{
  "BotConfiguration": {
    "BotToken": "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz",
    "WebhookUrl": "https://yourdomain.com",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "your-webhook-secret",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000",
    "MaxConnections": 40,
    "RequestTimeout": 30
  }
}
```

### Development vs Production

**Development (Polling Mode)**:
```json
{
  "UseWebhook": false,
  "BotToken": "your-bot-token"
}
```

**Production (Webhook Mode)**:
```json
{
  "UseWebhook": true,
  "BotToken": "your-bot-token",
  "WebhookUrl": "https://yourdomain.com",
  "WebhookSecret": "secure-random-string"
}
```

---

## Core Services

### BotService

**Location**: `Services/BotService.cs`

**Interface**: `IBotService`

**Responsibilities**:
- Manage Telegram bot client lifecycle
- Initialize bot and validate token
- Set up/remove webhooks
- Provide access to bot client

**Key Methods**:

1. **InitializeAsync**:
   ```csharp
   public async Task<User> InitializeAsync(CancellationToken cancellationToken = default)
   {
       // Validate bot token by getting bot info
       var botInfo = await _botClient.GetMe(cancellationToken);

       _logger.LogInformation(
           "Bot initialized: @{BotUsername} (ID: {BotId})",
           botInfo.Username,
           botInfo.Id);

       return botInfo;
   }
   ```

2. **SetWebhookAsync**:
   ```csharp
   public async Task<bool> SetWebhookAsync(CancellationToken cancellationToken = default)
   {
       if (!_configuration.UseWebhook)
           return false;

       await _botClient.SetWebhook(
           url: _configuration.FullWebhookUrl,
           secretToken: _configuration.WebhookSecret,
           maxConnections: _configuration.MaxConnections,
           cancellationToken: cancellationToken);

       return true;
   }
   ```

3. **RemoveWebhookAsync**:
   ```csharp
   public async Task<bool> RemoveWebhookAsync(CancellationToken cancellationToken = default)
   {
       await _botClient.DeleteWebhook(
           dropPendingUpdates: false,
           cancellationToken: cancellationToken);

       return true;
   }
   ```

4. **ValidateWebhookSecret**:
   ```csharp
   public bool ValidateWebhookSecret(string? secretToken)
   {
       return secretToken != null &&
              secretToken.Equals(_configuration.WebhookSecret, StringComparison.Ordinal);
   }
   ```

### UpdateHandler

**Location**: `Services/UpdateHandler.cs`

**Interface**: `IUpdateHandler`

**Responsibilities**:
- Process all incoming Telegram updates
- Route to appropriate handler based on update type
- Handle errors gracefully

**Update Types**:
- Message (text, commands)
- CallbackQuery (inline keyboard buttons)
- EditedMessage
- ChannelPost
- InlineQuery
- etc.

**Structure**:
```csharp
public class UpdateHandler : IUpdateHandler
{
    private readonly ICommandRouter _commandRouter;
    private readonly ILogger<UpdateHandler> _logger;

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        try
        {
            var handler = update.Type switch
            {
                UpdateType.Message => HandleMessageAsync(update.Message!, cancellationToken),
                UpdateType.CallbackQuery => HandleCallbackQueryAsync(update.CallbackQuery!, cancellationToken),
                UpdateType.EditedMessage => HandleEditedMessageAsync(update.EditedMessage!, cancellationToken),
                _ => HandleUnknownUpdateAsync(update, cancellationToken)
            };

            await handler;
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, cancellationToken);
        }
    }
}
```

### CommandRouter

**Location**: `Services/CommandRouter.cs`

**Responsibilities**:
- Parse command from message
- Route to registered command handler
- Handle unknown commands

**Command Format**: `/command@botusername arguments`

**Routing Logic**:
```csharp
public async Task RouteCommandAsync(Message message, CancellationToken cancellationToken)
{
    if (message.Text == null || !message.Text.StartsWith("/"))
        return;

    // Parse command: /start@botname -> "start"
    var commandText = message.Text.Split(' ')[0];
    var command = commandText.TrimStart('/').Split('@')[0].ToLower();

    // Find and execute handler
    var handler = _commandHandlers.FirstOrDefault(h =>
        h.Command.Equals(command, StringComparison.OrdinalIgnoreCase));

    if (handler != null)
    {
        await handler.HandleAsync(message, cancellationToken);
    }
    else
    {
        await HandleUnknownCommandAsync(message, cancellationToken);
    }
}
```

---

## Command Handlers

### ICommandHandler Interface

**Location**: `Interfaces/ICommandHandler.cs`

```csharp
public interface ICommandHandler
{
    /// <summary>
    /// The command name (without slash).
    /// Example: "start", "help", "mysurveys"
    /// </summary>
    string Command { get; }

    /// <summary>
    /// Command description for help text.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Handles the command execution.
    /// </summary>
    Task HandleAsync(Message message, CancellationToken cancellationToken);
}
```

### StartCommandHandler

**Command**: `/start`

**Purpose**: Welcome new users and provide initial instructions

**Implementation**:
```csharp
public class StartCommandHandler : ICommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<StartCommandHandler> _logger;

    public string Command => "start";
    public string Description => "Start the bot and get welcome message";

    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        var welcomeMessage =
            "Welcome to SurveyBot! 🎉\n\n" +
            "I can help you create and manage surveys.\n\n" +
            "Available commands:\n" +
            "/help - Show available commands\n" +
            "/mysurveys - View your surveys\n" +
            "/surveys - Browse available surveys";

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: welcomeMessage,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "User {UserId} started the bot",
            message.From?.Id);
    }
}
```

### HelpCommandHandler

**Command**: `/help`

**Purpose**: Display available commands and usage information

**Features**:
- List all commands with descriptions
- Provide usage examples
- Link to support resources

### MySurveysCommandHandler

**Command**: `/mysurveys`

**Purpose**: Show surveys created by the user

**Flow**:
1. Get user's Telegram ID
2. Call API to fetch user's surveys
3. Display surveys with inline keyboard
4. Handle survey selection

**Example Response**:
```
Your Surveys (3):

1. Customer Satisfaction Survey
   Status: Active | Responses: 25
   [View] [Edit] [Statistics]

2. Product Feedback
   Status: Inactive | Responses: 0
   [View] [Edit] [Activate]

3. Employee Survey
   Status: Active | Responses: 12
   [View] [Edit] [Statistics]
```

### SurveysCommandHandler

**Command**: `/surveys`

**Purpose**: Browse and respond to available surveys

**Flow**:
1. Fetch active surveys from API
2. Display surveys with "Take Survey" button
3. Handle survey selection
4. Start survey response flow

---

## Conversation Flows (Planned)

### Survey Creation Flow

**Trigger**: Inline keyboard button or `/createsurvey`

**Steps**:
1. Ask for survey title
2. Ask for description (optional, /skip to skip)
3. Ask to add questions
4. For each question:
   - Ask question text
   - Ask question type (inline keyboard)
   - Ask if required
   - Ask for options (if choice-based)
5. Review and confirm
6. Create survey via API

**State Management**: Store in-memory or Redis

### Survey Response Flow

**Trigger**: "Take Survey" button

**Steps**:
1. Start response via API
2. For each question:
   - Display question text
   - Show appropriate input (text/buttons/rating)
   - Validate answer
   - Submit answer via API
3. Show completion message
4. Optionally show results (if allowed)

---

## Integration with API

### HTTP Client Pattern

Bot layer makes HTTP calls to the API layer:

```csharp
public class SurveyApiClient
{
    private readonly HttpClient _httpClient;
    private readonly BotConfiguration _config;

    public SurveyApiClient(HttpClient httpClient, IOptions<BotConfiguration> config)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(config.Value.ApiBaseUrl);
    }

    public async Task<List<SurveyListDto>> GetActiveSurveysAsync()
    {
        var response = await _httpClient.GetAsync("/api/surveys?isActive=true");
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResultDto<SurveyListDto>>>();
        return apiResponse?.Data?.Items ?? new List<SurveyListDto>();
    }
}
```

### Authentication for API Calls

Bot uses service account or bot token for API authentication:

```csharp
_httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", botServiceToken);
```

---

## Webhook vs Polling

### Webhook Mode (Production)

**Configuration**:
```csharp
{
  "UseWebhook": true,
  "WebhookUrl": "https://yourdomain.com",
  "WebhookSecret": "secure-secret"
}
```

**Flow**:
1. Telegram sends updates to webhook URL
2. BotController receives POST request
3. Validates secret token
4. Queues update for background processing
5. Returns 200 OK immediately

**Advantages**:
- Real-time updates
- No polling overhead
- Scalable
- Required for production

**Requirements**:
- HTTPS with valid SSL certificate
- Publicly accessible URL
- Port 80, 88, 443, or 8443

### Polling Mode (Development)

**Configuration**:
```csharp
{
  "UseWebhook": false
}
```

**Flow**:
1. Bot actively polls Telegram API for updates
2. Processes updates as they arrive
3. Requires background service running

**Advantages**:
- Easy for local development
- No HTTPS required
- No public URL needed

**Disadvantages**:
- Higher latency
- More resource intensive
- Not suitable for production

---

## Dependency Injection Registration

### ServiceCollectionExtensions

**Location**: `Extensions/ServiceCollectionExtensions.cs`

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramBot(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<BotConfiguration>(
            configuration.GetSection(BotConfiguration.SectionName));

        // Register bot service
        services.AddSingleton<IBotService, BotService>();

        // Register update handler
        services.AddScoped<IUpdateHandler, UpdateHandler>();

        // Register command router
        services.AddScoped<ICommandRouter, CommandRouter>();

        return services;
    }

    public static IServiceCollection AddBotHandlers(this IServiceCollection services)
    {
        // Register all command handlers
        services.AddScoped<ICommandHandler, StartCommandHandler>();
        services.AddScoped<ICommandHandler, HelpCommandHandler>();
        services.AddScoped<ICommandHandler, MySurveysCommandHandler>();
        services.AddScoped<ICommandHandler, SurveysCommandHandler>();

        return services;
    }
}
```

### BotServiceExtensions

**Location**: `Extensions/BotServiceExtensions.cs`

```csharp
public static class BotServiceExtensions
{
    public static async Task InitializeTelegramBotAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var botService = scope.ServiceProvider.GetRequiredService<IBotService>();

        await botService.InitializeAsync();

        var config = scope.ServiceProvider
            .GetRequiredService<IOptions<BotConfiguration>>().Value;

        if (config.UseWebhook)
        {
            await botService.SetWebhookAsync();
        }
        else
        {
            await botService.RemoveWebhookAsync();
        }
    }
}
```

---

## Message Formatting

### Markdown Formatting

Telegram supports Markdown and HTML formatting:

```csharp
// Markdown
await _botClient.SendTextMessageAsync(
    chatId: chatId,
    text: "*Bold* _italic_ `code` [link](http://example.com)",
    parseMode: ParseMode.Markdown,
    cancellationToken: cancellationToken);

// HTML
await _botClient.SendTextMessageAsync(
    chatId: chatId,
    text: "<b>Bold</b> <i>italic</i> <code>code</code>",
    parseMode: ParseMode.Html,
    cancellationToken: cancellationToken);
```

### Inline Keyboards

Create interactive buttons:

```csharp
var keyboard = new InlineKeyboardMarkup(new[]
{
    new[]
    {
        InlineKeyboardButton.WithCallbackData("View", $"view_survey_{surveyId}"),
        InlineKeyboardButton.WithCallbackData("Edit", $"edit_survey_{surveyId}")
    },
    new[]
    {
        InlineKeyboardButton.WithCallbackData("Statistics", $"stats_survey_{surveyId}")
    }
});

await _botClient.SendTextMessageAsync(
    chatId: chatId,
    text: "Select an action:",
    replyMarkup: keyboard,
    cancellationToken: cancellationToken);
```

### Reply Keyboards

Create custom keyboards:

```csharp
var keyboard = new ReplyKeyboardMarkup(new[]
{
    new KeyboardButton[] { "My Surveys", "Browse Surveys" },
    new KeyboardButton[] { "Create Survey", "Help" }
})
{
    ResizeKeyboard = true,
    OneTimeKeyboard = false
};

await _botClient.SendTextMessageAsync(
    chatId: chatId,
    text: "Choose an option:",
    replyMarkup: keyboard,
    cancellationToken: cancellationToken);
```

---

## Error Handling

### Telegram API Errors

Common errors and handling:

```csharp
try
{
    await _botClient.SendTextMessageAsync(chatId, text);
}
catch (ApiRequestException ex) when (ex.ErrorCode == 403)
{
    // User blocked the bot
    _logger.LogWarning("User {ChatId} blocked the bot", chatId);
}
catch (ApiRequestException ex) when (ex.ErrorCode == 400)
{
    // Bad request (invalid parameters)
    _logger.LogError(ex, "Invalid request to Telegram API");
}
catch (ApiRequestException ex)
{
    // Other API errors
    _logger.LogError(ex, "Telegram API error: {Message}", ex.Message);
}
```

### Graceful Degradation

```csharp
public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
{
    try
    {
        // Process update
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing update {UpdateId}", update.Id);

        // Try to notify user
        try
        {
            await _botClient.SendTextMessageAsync(
                chatId: update.Message?.Chat.Id ?? 0,
                text: "Sorry, an error occurred. Please try again later.",
                cancellationToken: cancellationToken);
        }
        catch
        {
            // Silently fail - don't crash the bot
        }
    }
}
```

---

## Testing

### Unit Testing Command Handlers

```csharp
public class StartCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_SendsWelcomeMessage()
    {
        // Arrange
        var botClientMock = new Mock<ITelegramBotClient>();
        var loggerMock = new Mock<ILogger<StartCommandHandler>>();
        var handler = new StartCommandHandler(botClientMock.Object, loggerMock.Object);

        var message = new Message
        {
            Chat = new Chat { Id = 123 },
            From = new User { Id = 456 }
        };

        // Act
        await handler.HandleAsync(message, CancellationToken.None);

        // Assert
        botClientMock.Verify(
            x => x.SendTextMessageAsync(
                It.Is<ChatId>(c => c.Identifier == 123),
                It.IsAny<string>(),
                It.IsAny<ParseMode>(),
                It.IsAny<IEnumerable<MessageEntity>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<IReplyMarkup>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

### Integration Testing Bot Flow

```csharp
public class SurveyFlowIntegrationTests
{
    [Fact]
    public async Task CreateSurvey_CompleteFlow_CreatesSurvey()
    {
        // Arrange - set up test bot, API client, database

        // Act - simulate user commands
        await SimulateUserMessage("/createsurvey");
        await SimulateUserMessage("Test Survey");
        await SimulateUserMessage("Test Description");
        // ... more steps

        // Assert - verify survey created in database
    }
}
```

---

## Best Practices

### 1. Command Design

- **Keep commands simple**: One action per command
- **Use inline keyboards**: For multi-step flows
- **Provide feedback**: Confirm actions with messages
- **Handle cancellation**: Allow users to exit flows

### 2. Message Design

- **Be concise**: Telegram users expect quick interactions
- **Use formatting**: Make messages readable
- **Use emojis**: Add personality (but don't overuse)
- **Provide context**: Users may have forgotten what they were doing

### 3. Error Handling

- **Never crash**: Catch all exceptions
- **Log everything**: Include context for debugging
- **Inform users**: When errors occur, explain clearly
- **Retry intelligently**: Use exponential backoff

### 4. Performance

- **Respond quickly**: Telegram has 60-second timeout
- **Use webhooks**: In production for real-time updates
- **Queue long tasks**: Don't block update processing
- **Cache when possible**: Reduce API calls

### 5. Security

- **Validate webhook secret**: Prevent unauthorized updates
- **Sanitize inputs**: Never trust user input
- **Rate limit**: Prevent abuse
- **Use HTTPS**: For webhooks (required by Telegram)

---

## Common Issues

### Issue: Webhook not receiving updates
**Causes**:
- Invalid SSL certificate
- Webhook URL not publicly accessible
- Secret token mismatch
- Firewall blocking Telegram IPs

**Solutions**:
- Verify SSL with `curl -I https://yourdomain.com`
- Check webhook info: `GET /api/bot/webhook/info`
- Verify secret token matches config
- Allow Telegram IP ranges

### Issue: Bot responding slowly
**Causes**:
- Synchronous processing
- Long-running operations
- Database queries in handler

**Solutions**:
- Use async/await consistently
- Queue long operations
- Optimize database queries
- Add caching layer

### Issue: Messages not sending
**Causes**:
- User blocked bot
- Chat not found
- Message too long
- Invalid parameters

**Solutions**:
- Handle ApiRequestException
- Check message length (4096 chars max)
- Validate parameters before sending
- Log errors with context

---

## Future Enhancements

### Planned Features

1. **Conversation State Management**:
   - Redis-based state storage
   - Support for multi-step flows
   - Session timeout handling

2. **Rich Media Support**:
   - Send survey results as charts
   - Support image questions
   - Export survey as PDF

3. **Inline Mode**:
   - Share surveys via inline queries
   - Quick survey creation

4. **Bot Commands Menu**:
   - Register commands with BotFather
   - Show command hints in Telegram

5. **Localization**:
   - Multi-language support
   - User language preferences

---

## Quick Reference

### Sending Messages

```csharp
// Simple text
await _botClient.SendTextMessageAsync(chatId, "Hello!");

// With formatting
await _botClient.SendTextMessageAsync(chatId, "*Bold* text", ParseMode.Markdown);

// With inline keyboard
await _botClient.SendTextMessageAsync(chatId, "Choose:", replyMarkup: keyboard);
```

### Handling Callbacks

```csharp
private async Task HandleCallbackQueryAsync(CallbackQuery query, CancellationToken ct)
{
    // Answer callback to remove loading state
    await _botClient.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);

    // Parse callback data
    var data = query.Data; // e.g., "view_survey_123"

    // Process and respond
    await _botClient.SendTextMessageAsync(query.Message!.Chat.Id, "Processing...", cancellationToken: ct);
}
```

### Editing Messages

```csharp
await _botClient.EditMessageTextAsync(
    chatId: chatId,
    messageId: messageId,
    text: "Updated text",
    cancellationToken: cancellationToken);
```

---

**Key Takeaway**: The Bot layer bridges Telegram and your application. Keep handlers focused, use async patterns, handle errors gracefully, and always think about the user experience.
