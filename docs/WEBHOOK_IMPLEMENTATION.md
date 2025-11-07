# Telegram Bot Webhook Implementation

## Overview

This document describes the webhook implementation for the SurveyBot Telegram integration.

## Architecture

### Components

1. **BotController** (`SurveyBot.API/Controllers/BotController.cs`)
   - Handles incoming webhook requests from Telegram
   - Validates webhook secret token
   - Queues updates for asynchronous processing
   - Returns 200 OK immediately (< 2 seconds)

2. **UpdateHandler** (`SurveyBot.Bot/Services/UpdateHandler.cs`)
   - Processes Telegram updates
   - Routes to appropriate handlers based on update type
   - Handles messages, callback queries, and other update types

3. **BackgroundTaskQueue** (`SurveyBot.API/Services/BackgroundTaskQueue.cs`)
   - Thread-safe queue for background task processing
   - Uses System.Threading.Channels for high performance
   - Configurable capacity (default: 100)

4. **QueuedHostedService** (`SurveyBot.API/Services/QueuedHostedService.cs`)
   - Background service that processes queued tasks
   - Runs continuously as a hosted service
   - Graceful shutdown support

## Request Flow

```
Telegram Server
    |
    | POST /api/bot/webhook
    v
BotController
    |
    | 1. Validate webhook secret
    | 2. Validate update
    | 3. Queue for processing
    | 4. Return 200 OK
    |
    v
BackgroundTaskQueue
    |
    v
QueuedHostedService
    |
    | Dequeue and execute
    v
UpdateHandler
    |
    | Route by update type
    v
CommandRouter / Other Handlers
    |
    v
Send Response to User
```

## Endpoints

### POST /api/bot/webhook

Receives Telegram updates and queues them for processing.

**Headers:**
- `Content-Type: application/json`
- `X-Telegram-Bot-Api-Secret-Token: <webhook-secret>`

**Request Body:**
```json
{
  "update_id": 123456789,
  "message": {
    "message_id": 1,
    "from": { "id": 123, "first_name": "John" },
    "chat": { "id": 123, "type": "private" },
    "date": 1699999999,
    "text": "/start"
  }
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Update received and queued for processing",
  "timestamp": "2025-01-06T12:00:00Z"
}
```

**Response (401 Unauthorized):**
```json
{
  "success": false,
  "error": "Unauthorized",
  "message": "Invalid webhook secret",
  "timestamp": "2025-01-06T12:00:00Z"
}
```

**Response (400 Bad Request):**
```json
{
  "success": false,
  "error": "BadRequest",
  "message": "Update cannot be null",
  "timestamp": "2025-01-06T12:00:00Z"
}
```

### GET /api/bot/status

Returns bot configuration and webhook status.

**Response:**
```json
{
  "success": true,
  "data": {
    "webhookConfigured": true,
    "webhookUrl": "https://example.com/api/bot/webhook",
    "botUsername": "surveybot",
    "apiBaseUrl": "https://api.example.com",
    "timestamp": "2025-01-06T12:00:00Z"
  },
  "timestamp": "2025-01-06T12:00:00Z"
}
```

### GET /api/bot/health

Health check endpoint for bot webhook functionality.

**Response:**
```json
{
  "success": true,
  "healthy": true,
  "service": "Bot Webhook",
  "webhookEnabled": true,
  "timestamp": "2025-01-06T12:00:00Z"
}
```

## Configuration

### appsettings.json

```json
{
  "BotConfiguration": {
    "BotToken": "your-bot-token-from-botfather",
    "UseWebhook": true,
    "WebhookUrl": "https://yourdomain.com",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "your-secret-token",
    "MaxConnections": 40,
    "ApiBaseUrl": "https://api.yourdomain.com",
    "BotUsername": "your_bot",
    "RequestTimeout": 30
  }
}
```

### User Secrets (Development)

```bash
dotnet user-secrets set "BotConfiguration:BotToken" "your-bot-token"
dotnet user-secrets set "BotConfiguration:WebhookSecret" "your-secret-token"
```

## Security

### Webhook Secret Validation

The webhook endpoint validates the `X-Telegram-Bot-Api-Secret-Token` header to ensure requests come from Telegram.

**Setting the webhook with secret:**

```bash
curl -X POST "https://api.telegram.org/bot<TOKEN>/setWebhook" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://yourdomain.com/api/bot/webhook",
    "secret_token": "your-secret-token"
  }'
```

### HTTPS Requirement

Telegram requires webhook URLs to use HTTPS. For local development:

1. Use ngrok: `ngrok http 5000`
2. Or set `UseWebhook: false` to use long polling instead

## Performance

### Response Time

- **Target:** < 2 seconds
- **Telegram Timeout:** 60 seconds
- **Implementation:** Returns 200 OK immediately after queuing

### Queue Capacity

- **Default:** 100 concurrent updates
- **Configuration:** Set in `Program.cs` via `AddBackgroundTaskQueue(capacity)`
- **Behavior:** Waits when full (BoundedChannelFullMode.Wait)

### Background Processing

Updates are processed asynchronously in the background, allowing the webhook to respond quickly.

## Error Handling

### Controller Level

- Invalid webhook secret → 401 Unauthorized
- Null update → 400 Bad Request
- Any other error → 200 OK (to avoid Telegram retries) + logged

### Processing Level

- Errors during update processing are logged
- `UpdateHandler.HandleErrorAsync()` is called
- Original exception details preserved in logs

## Logging

### Webhook Requests

```
[INF] Webhook received update 123456789 of type Message
```

### Background Processing

```
[DBG] Background work item queued successfully
[DBG] Executing background work item
[DBG] Update 123456789 processed successfully
```

### Errors

```
[ERR] Error processing update 123456789 in background
[ERR] Error occurred executing background work item
```

## Dependencies

### NuGet Packages

- `Telegram.Bot` - Telegram Bot API client
- No additional packages required (uses built-in System.Threading.Channels)

### Project References

- `SurveyBot.Bot` - Bot services and handlers
- `SurveyBot.Core` - Core models and interfaces

## Files Created

### Controllers
- `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\Controllers\BotController.cs`

### Services
- `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\Services\IBackgroundTaskQueue.cs`
- `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\Services\BackgroundTaskQueue.cs`
- `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\Services\QueuedHostedService.cs`

### Extensions
- `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\Extensions\BackgroundServiceExtensions.cs`

### Documentation
- `C:\Users\User\Desktop\SurveyBot\docs\WEBHOOK_IMPLEMENTATION.md`
- `C:\Users\User\Desktop\SurveyBot\docs\WEBHOOK_TESTING.md`

### Test Files
- `C:\Users\User\Desktop\SurveyBot\test_data\sample_webhook_updates.json`
- `C:\Users\User\Desktop\SurveyBot\scripts\Test-Webhook.ps1`

## Testing

### Unit Testing

Test the following components:

1. **BotController**
   - Webhook secret validation
   - Update queueing
   - Error handling

2. **BackgroundTaskQueue**
   - Queue/dequeue operations
   - Capacity limits
   - Thread safety

3. **QueuedHostedService**
   - Background processing
   - Error handling
   - Graceful shutdown

### Integration Testing

Use the provided PowerShell script:

```powershell
.\scripts\Test-Webhook.ps1 -UpdateType start_command
```

Or use ngrok for real Telegram integration:

```bash
ngrok http 5000
# Then set webhook in Telegram
# Send messages to bot
```

## Monitoring

### Health Checks

- `/api/bot/health` - Bot webhook health
- `/health` - General API health
- `/health/db` - Database health

### Metrics to Monitor

1. Webhook response time (should be < 2 seconds)
2. Queue size (should not reach capacity)
3. Background processing time
4. Error rate
5. Update throughput

## Troubleshooting

### Issue: Webhook not receiving updates

**Solution:**
1. Verify webhook is set: `curl https://api.telegram.org/bot<TOKEN>/getWebhookInfo`
2. Check HTTPS is working
3. Verify webhook secret matches
4. Check application logs

### Issue: Updates processing slowly

**Solution:**
1. Increase queue capacity in `Program.cs`
2. Optimize update handlers
3. Check database performance
4. Review application logs for bottlenecks

### Issue: 401 Unauthorized errors

**Solution:**
1. Verify `WebhookSecret` in `appsettings.json`
2. Ensure secret was set when calling `setWebhook`
3. Check `X-Telegram-Bot-Api-Secret-Token` header

## Future Enhancements

1. **Rate Limiting**
   - Implement per-user rate limiting
   - Prevent abuse

2. **Metrics Collection**
   - Track webhook performance
   - Monitor queue health

3. **IP Whitelisting**
   - Whitelist Telegram IP ranges
   - Additional security layer

4. **Retry Logic**
   - Retry failed update processing
   - Dead letter queue for failed updates

5. **Distributed Queue**
   - Redis-based queue for horizontal scaling
   - Multiple API instances

## References

- [Telegram Bot API - Webhooks](https://core.telegram.org/bots/api#setwebhook)
- [ASP.NET Core Background Services](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
- [System.Threading.Channels](https://docs.microsoft.com/en-us/dotnet/api/system.threading.channels)
