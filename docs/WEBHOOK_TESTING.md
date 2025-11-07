# Telegram Bot Webhook Testing Guide

This guide provides instructions for testing the Telegram Bot webhook endpoint.

## Overview

The webhook endpoint is located at: `POST /api/bot/webhook`

This endpoint receives updates from Telegram and processes them asynchronously in the background.

## Testing the Webhook Endpoint

### 1. Local Testing with Postman/cURL

Since Telegram webhook requires HTTPS, local testing can be done by simulating webhook requests.

#### Sample Update JSON (Message)

```json
{
  "update_id": 123456789,
  "message": {
    "message_id": 1,
    "from": {
      "id": 123456789,
      "is_bot": false,
      "first_name": "Test",
      "username": "testuser",
      "language_code": "en"
    },
    "chat": {
      "id": 123456789,
      "first_name": "Test",
      "username": "testuser",
      "type": "private"
    },
    "date": 1699999999,
    "text": "/start"
  }
}
```

#### cURL Command

```bash
curl -X POST https://localhost:5001/api/bot/webhook \
  -H "Content-Type: application/json" \
  -H "X-Telegram-Bot-Api-Secret-Token: your-webhook-secret" \
  -d @sample_update.json
```

#### PowerShell Command

```powershell
$headers = @{
    "Content-Type" = "application/json"
    "X-Telegram-Bot-Api-Secret-Token" = "your-webhook-secret"
}

$body = @{
    update_id = 123456789
    message = @{
        message_id = 1
        from = @{
            id = 123456789
            is_bot = $false
            first_name = "Test"
            username = "testuser"
            language_code = "en"
        }
        chat = @{
            id = 123456789
            first_name = "Test"
            username = "testuser"
            type = "private"
        }
        date = 1699999999
        text = "/start"
    }
} | ConvertTo-Json -Depth 10

Invoke-RestMethod -Uri "https://localhost:5001/api/bot/webhook" -Method Post -Headers $headers -Body $body
```

### 2. Testing with ngrok (for real Telegram integration)

#### Step 1: Install ngrok

Download from https://ngrok.com/download

#### Step 2: Start ngrok tunnel

```bash
ngrok http 5000
```

This will provide an HTTPS URL like: `https://abc123.ngrok.io`

#### Step 3: Configure appsettings.json

```json
{
  "BotConfiguration": {
    "BotToken": "your-bot-token",
    "UseWebhook": true,
    "WebhookUrl": "https://abc123.ngrok.io",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "your-secret-token"
  }
}
```

#### Step 4: Set Telegram Webhook

Use the following endpoint to configure Telegram to send updates to your webhook:

```bash
curl -X POST "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/setWebhook" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://abc123.ngrok.io/api/bot/webhook",
    "secret_token": "your-secret-token"
  }'
```

Or use PowerShell:

```powershell
$botToken = "your-bot-token"
$webhookUrl = "https://abc123.ngrok.io/api/bot/webhook"
$secretToken = "your-secret-token"

$body = @{
    url = $webhookUrl
    secret_token = $secretToken
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://api.telegram.org/bot$botToken/setWebhook" -Method Post -ContentType "application/json" -Body $body
```

#### Step 5: Test with Real Bot

Send messages to your bot on Telegram, and they will be forwarded to your local webhook endpoint.

### 3. Verify Webhook Status

#### Check Bot Status Endpoint

```bash
curl https://localhost:5001/api/bot/status
```

Expected response:

```json
{
  "success": true,
  "data": {
    "webhookConfigured": true,
    "webhookUrl": "https://abc123.ngrok.io/api/bot/webhook",
    "botUsername": "your_bot",
    "apiBaseUrl": "http://localhost:5000",
    "timestamp": "2025-01-06T12:00:00Z"
  },
  "timestamp": "2025-01-06T12:00:00Z"
}
```

#### Check Telegram Webhook Info

```bash
curl https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getWebhookInfo
```

Expected response:

```json
{
  "ok": true,
  "result": {
    "url": "https://abc123.ngrok.io/api/bot/webhook",
    "has_custom_certificate": false,
    "pending_update_count": 0,
    "max_connections": 40
  }
}
```

## Sample Update Types

### 1. Text Message

```json
{
  "update_id": 123456789,
  "message": {
    "message_id": 1,
    "from": {
      "id": 123456789,
      "is_bot": false,
      "first_name": "Test",
      "username": "testuser"
    },
    "chat": {
      "id": 123456789,
      "type": "private"
    },
    "date": 1699999999,
    "text": "Hello, bot!"
  }
}
```

### 2. Command Message

```json
{
  "update_id": 123456790,
  "message": {
    "message_id": 2,
    "from": {
      "id": 123456789,
      "is_bot": false,
      "first_name": "Test",
      "username": "testuser"
    },
    "chat": {
      "id": 123456789,
      "type": "private"
    },
    "date": 1699999999,
    "text": "/surveys",
    "entities": [
      {
        "offset": 0,
        "length": 8,
        "type": "bot_command"
      }
    ]
  }
}
```

### 3. Callback Query

```json
{
  "update_id": 123456791,
  "callback_query": {
    "id": "123456789",
    "from": {
      "id": 123456789,
      "is_bot": false,
      "first_name": "Test",
      "username": "testuser"
    },
    "message": {
      "message_id": 3,
      "from": {
        "id": 987654321,
        "is_bot": true,
        "first_name": "Bot",
        "username": "your_bot"
      },
      "chat": {
        "id": 123456789,
        "type": "private"
      },
      "date": 1699999999,
      "text": "Select a survey:"
    },
    "chat_instance": "123456789",
    "data": "survey:1"
  }
}
```

## Monitoring and Logging

### Check Application Logs

The webhook controller logs all incoming requests with the following information:

- Update ID
- Update Type (Message, CallbackQuery, etc.)
- Processing status
- Any errors

Look for log entries like:

```
[INF] Webhook received update 123456789 of type Message
[DBG] Processing update 123456789 in background
[DBG] Update 123456789 processed successfully
```

### Check Background Queue

Monitor the background task queue to ensure updates are being processed:

```
[INF] BackgroundTaskQueue initialized with capacity 100
[DBG] Background work item queued successfully
[DBG] Executing background work item
[DBG] Background work item completed successfully
```

## Error Scenarios

### 1. Invalid Webhook Secret

**Request:**
```bash
curl -X POST https://localhost:5001/api/bot/webhook \
  -H "Content-Type: application/json" \
  -H "X-Telegram-Bot-Api-Secret-Token: wrong-secret" \
  -d '{"update_id": 1}'
```

**Expected Response:**
```json
{
  "success": false,
  "error": "Unauthorized",
  "message": "Invalid webhook secret",
  "timestamp": "2025-01-06T12:00:00Z"
}
```

**Status Code:** 401 Unauthorized

### 2. Missing Update Body

**Request:**
```bash
curl -X POST https://localhost:5001/api/bot/webhook \
  -H "Content-Type: application/json" \
  -H "X-Telegram-Bot-Api-Secret-Token: your-secret"
```

**Expected Response:**
```json
{
  "success": false,
  "error": "BadRequest",
  "message": "Update cannot be null",
  "timestamp": "2025-01-06T12:00:00Z"
}
```

**Status Code:** 400 Bad Request

### 3. Processing Error

Even if processing fails, the webhook returns 200 OK to Telegram to avoid retries:

**Response:**
```json
{
  "success": true,
  "message": "Update received and queued for processing",
  "timestamp": "2025-01-06T12:00:00Z"
}
```

**Status Code:** 200 OK

Error details are logged internally for investigation.

## Performance Requirements

- **Response Time:** < 2 seconds (returns immediately after queuing)
- **Telegram Timeout:** 60 seconds (webhook must respond within this time)
- **Queue Capacity:** 100 concurrent updates
- **Processing:** Asynchronous background processing

## Health Checks

### Bot Health Endpoint

```bash
curl https://localhost:5001/api/bot/health
```

Expected response:

```json
{
  "success": true,
  "healthy": true,
  "service": "Bot Webhook",
  "webhookEnabled": true,
  "timestamp": "2025-01-06T12:00:00Z"
}
```

## Security Considerations

1. **Always use HTTPS** in production (Telegram requires it)
2. **Validate webhook secret** from `X-Telegram-Bot-Api-Secret-Token` header
3. **Store secrets securely** (use User Secrets for development, Azure Key Vault for production)
4. **Rate limiting** (consider implementing if needed)
5. **IP whitelisting** (optional - whitelist Telegram's IP ranges)

## Troubleshooting

### Webhook not receiving updates

1. Check webhook is set correctly:
   ```bash
   curl https://api.telegram.org/bot<TOKEN>/getWebhookInfo
   ```

2. Verify ngrok tunnel is active:
   ```bash
   ngrok http 5000
   ```

3. Check application logs for errors

4. Ensure webhook URL is HTTPS

### Updates processing slowly

1. Check background queue capacity
2. Review application logs for bottlenecks
3. Consider increasing queue capacity in `Program.cs`

### Authentication errors

1. Verify `WebhookSecret` matches in both:
   - `appsettings.json`
   - Telegram `setWebhook` call

2. Check `X-Telegram-Bot-Api-Secret-Token` header is present

## Development vs Production

### Development (UseWebhook = false)

- Uses long polling instead of webhooks
- No HTTPS required
- No ngrok needed
- Simpler for local testing

### Production (UseWebhook = true)

- Uses webhooks
- HTTPS required
- Configure proper webhook URL
- Set webhook secret token
- Better performance and reliability
