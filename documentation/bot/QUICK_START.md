# Quick Start Guide - Telegram Bot Setup

## 1. Get Your Bot Token (2 minutes)

1. Open Telegram and search for `@BotFather`
2. Send `/newbot` command
3. Follow prompts to name your bot
4. Copy the bot token (looks like: `1234567890:ABCdefGHIjklMNOpqrsTUVwxyz`)

## 2. Configure Bot Token (1 minute)

### Using User Secrets (Recommended)

```bash
cd src/SurveyBot.API
dotnet user-secrets set "BotConfiguration:BotToken" "YOUR_BOT_TOKEN_HERE"
```

## 3. Add to Program.cs (2 minutes)

Add to the top of `Program.cs`:
```csharp
using SurveyBot.Bot.Extensions;
```

Add after service registration (before `builder.Build()`):
```csharp
// Add Telegram Bot Services
builder.Services.AddTelegramBot(builder.Configuration);
```

Add after `app.Build()` and before `app.Run()`:
```csharp
// Initialize Telegram Bot
await app.Services.InitializeTelegramBotAsync();
```

## 4. Add Project Reference (1 minute)

In `SurveyBot.API.csproj`, add:
```xml
<ItemGroup>
  <ProjectReference Include="..\SurveyBot.Bot\SurveyBot.Bot.csproj" />
</ItemGroup>
```

Or via CLI:
```bash
cd src/SurveyBot.API
dotnet add reference ../SurveyBot.Bot/SurveyBot.Bot.csproj
```

## 5. Run and Verify

```bash
cd src/SurveyBot.API
dotnet run
```

Look for:
```
[INF] Bot initialized successfully. Bot: @your_bot_username (ID: 123456789)
[INF] Telegram Bot initialized successfully
```

## Configuration Summary

Your bot is now configured with these defaults:
- Mode: Long Polling (good for development)
- API URL: `http://localhost:5000`
- Timeout: 30 seconds

## Next Steps

1. Create webhook controller
2. Implement update handlers
3. Add command handlers (/start, /surveys, /help)
4. Implement survey flow

## Need Help?

See full documentation:
- `README.md` - Complete setup guide
- `INTEGRATION_GUIDE.md` - Detailed integration steps
