# SurveyBot.Bot - Telegram Bot Setup

This project contains the Telegram Bot implementation for the SurveyBot application.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Creating Your Telegram Bot](#creating-your-telegram-bot)
3. [Configuration Setup](#configuration-setup)
4. [Dependency Injection Registration](#dependency-injection-registration)
5. [Webhook vs Long Polling](#webhook-vs-long-polling)
6. [Testing the Bot](#testing-the-bot)
7. [Troubleshooting](#troubleshooting)

## Prerequisites

- .NET 8.0 SDK or later
- Telegram account
- Visual Studio 2022 or VS Code

## Creating Your Telegram Bot

1. Open Telegram and search for **@BotFather**
2. Start a chat and send the `/newbot` command
3. Follow the prompts:
   - Choose a name for your bot (e.g., "My Survey Bot")
   - Choose a username for your bot (must end with "bot", e.g., "my_survey_bot")
4. BotFather will provide you with a **Bot Token** that looks like:
   ```
   1234567890:ABCdefGHIjklMNOpqrsTUVwxyz
   ```
5. **IMPORTANT:** Keep this token secret! Never commit it to source control.

## Configuration Setup

### Option 1: User Secrets (Recommended for Development)

This is the most secure way to store your bot token during development.

1. Right-click on the **SurveyBot.API** project in Visual Studio
2. Select **Manage User Secrets**
3. Add your bot token to the `secrets.json` file:

```json
{
  "BotConfiguration": {
    "BotToken": "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"
  }
}
```

4. Save the file

**Using CLI:**
```bash
cd src/SurveyBot.API
dotnet user-secrets init
dotnet user-secrets set "BotConfiguration:BotToken" "YOUR_BOT_TOKEN_HERE"
```

### Option 2: Environment Variables

Set the environment variable:

**Windows (PowerShell):**
```powershell
$env:BotConfiguration__BotToken = "YOUR_BOT_TOKEN_HERE"
```

**Windows (Command Prompt):**
```cmd
set BotConfiguration__BotToken=YOUR_BOT_TOKEN_HERE
```

**Linux/macOS:**
```bash
export BotConfiguration__BotToken="YOUR_BOT_TOKEN_HERE"
```

### Option 3: appsettings.Development.json (Not Recommended)

Only use this for quick testing, and **NEVER** commit the token to git.

Update `src/SurveyBot.API/appsettings.Development.json`:
```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000"
  }
}
```

## Dependency Injection Registration

In your `Program.cs` or startup configuration:

```csharp
using SurveyBot.Bot.Extensions;

// Add bot services
builder.Services.AddTelegramBot(builder.Configuration);

// After building the app
var app = builder.Build();

// Initialize the bot (validates token, sets webhook if configured)
await app.Services.InitializeTelegramBotAsync();
```

### Complete Example

```csharp
using SurveyBot.Bot.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddTelegramBot(builder.Configuration);

var app = builder.Build();

// Initialize bot
try
{
    await app.Services.InitializeTelegramBotAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to initialize bot: {ex.Message}");
    throw;
}

app.MapControllers();
app.Run();
```

## Webhook vs Long Polling

### Long Polling (Development)

Best for local development and testing.

**Configuration:**
```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000"
  }
}
```

**Pros:**
- Easy to set up
- Works locally without public URL
- Good for debugging

**Cons:**
- Less efficient for production
- Constant polling creates load

### Webhook (Production)

Best for production deployments.

**Configuration:**
```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "BotUsername": "your_bot_username",
    "UseWebhook": true,
    "WebhookUrl": "https://yourdomain.com",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "your-secret-token-here",
    "MaxConnections": 40,
    "ApiBaseUrl": "https://yourdomain.com",
    "RequestTimeout": 30
  }
}
```

**Requirements:**
- Public HTTPS URL (Telegram requires SSL)
- Valid SSL certificate
- Webhook secret for security

**Pros:**
- Efficient - Telegram pushes updates
- Scalable for production
- Lower latency

**Cons:**
- Requires public HTTPS endpoint
- More complex setup

## Testing the Bot

### 1. Verify Token

The bot will automatically verify the token on startup. Check the logs:

```
[12:34:56 INF] Bot initialized successfully. Bot: @your_bot_username (ID: 123456789)
```

### 2. Test Basic Commands

Open Telegram and search for your bot by username, then:

1. Send `/start` command
2. Bot should respond (once you implement the update handler)

### 3. Check Bot Info

You can verify bot status programmatically:

```csharp
var botService = serviceProvider.GetRequiredService<IBotService>();
var botInfo = await botService.GetMeAsync();
Console.WriteLine($"Bot: @{botInfo.Username}");
```

## Configuration Reference

| Setting | Description | Required | Default |
|---------|-------------|----------|---------|
| `BotToken` | Bot token from BotFather | Yes | - |
| `BotUsername` | Bot username (without @) | No | Auto-detected |
| `UseWebhook` | Enable webhook mode | No | `false` |
| `WebhookUrl` | Base URL for webhook | If `UseWebhook=true` | - |
| `WebhookPath` | Webhook endpoint path | No | `/api/bot/webhook` |
| `WebhookSecret` | Secret token for webhook validation | If `UseWebhook=true` | - |
| `MaxConnections` | Max webhook connections | No | `40` |
| `ApiBaseUrl` | SurveyBot API base URL | Yes | `http://localhost:5000` |
| `RequestTimeout` | API request timeout (seconds) | No | `30` |

## Troubleshooting

### Bot Token Invalid

**Error:** `Failed to initialize bot. Please check your bot token.`

**Solutions:**
1. Verify token is correct (copy directly from BotFather)
2. Check for extra spaces or quotes
3. Ensure configuration is loaded properly
4. Test with BotFather `/token` command

### Configuration Not Found

**Error:** `Invalid bot configuration: BotToken is required`

**Solutions:**
1. Verify `BotConfiguration` section exists in appsettings
2. Check user secrets are set correctly
3. Ensure environment variables are set
4. Check configuration key names match exactly

### Webhook Setup Failed

**Error:** `Failed to set webhook`

**Solutions:**
1. Verify `WebhookUrl` is a valid HTTPS URL
2. Check SSL certificate is valid
3. Ensure webhook endpoint is publicly accessible
4. Verify `WebhookSecret` is set
5. Test webhook URL manually

### Can't Connect to API

**Error:** Connection refused or timeout

**Solutions:**
1. Verify `ApiBaseUrl` is correct
2. Check API is running
3. Verify network/firewall settings
4. Check API health endpoint

## Security Best Practices

1. **Never commit bot tokens** to source control
2. Use **User Secrets** for development
3. Use **secure configuration** (Azure Key Vault, AWS Secrets Manager) for production
4. Set strong **WebhookSecret** for webhook mode
5. Validate webhook requests using the secret token
6. Use HTTPS for all webhook endpoints
7. Rotate bot token if compromised

## Next Steps

1. Implement update handlers (message, command, callback handlers)
2. Create bot controllers for webhook endpoint
3. Implement survey flow logic
4. Add user state management
5. Create inline keyboards for surveys

## Additional Resources

- [Telegram Bot API Documentation](https://core.telegram.org/bots/api)
- [Telegram.Bot Library Documentation](https://github.com/TelegramBots/Telegram.Bot)
- [BotFather Commands](https://core.telegram.org/bots#6-botfather)
