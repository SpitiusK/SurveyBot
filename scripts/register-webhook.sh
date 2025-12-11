#!/bin/sh
# =============================================================================
# Telegram Webhook Auto-Registration Script
# =============================================================================
# This script automatically registers the Telegram webhook URL after ngrok starts.
# It waits for ngrok to be ready, fetches the public URL, and registers it with Telegram.
#
# Environment Variables Required:
#   - BOT_TOKEN: Telegram bot token
#   - WEBHOOK_SECRET: Secret for webhook verification
#   - NGROK_API_URL: URL to ngrok's local API (default: http://ngrok:4040)
#
# Usage: This script runs as entrypoint in the webhook-registrar container
# =============================================================================

set -e

# Configuration
NGROK_API_URL="${NGROK_API_URL:-http://ngrok:4040}"
MAX_RETRIES=30
RETRY_INTERVAL=2

echo "============================================"
echo "Telegram Webhook Auto-Registration"
echo "============================================"
echo "Ngrok API URL: $NGROK_API_URL"
echo "Bot Token: ${BOT_TOKEN:0:10}...${BOT_TOKEN: -5}"
echo ""

# Validate required environment variables
if [ -z "$BOT_TOKEN" ]; then
    echo "ERROR: BOT_TOKEN environment variable is required"
    exit 1
fi

if [ -z "$WEBHOOK_SECRET" ]; then
    echo "ERROR: WEBHOOK_SECRET environment variable is required"
    exit 1
fi

# Function to get ngrok tunnel URL for the API
get_ngrok_url() {
    # Query ngrok API for the specific "api" tunnel
    # The tunnel name is "api" as defined in ngrok.yml
    curl -s "$NGROK_API_URL/api/tunnels/api" | \
        grep -o '"public_url":"https://[^"]*' | \
        head -1 | \
        sed 's/"public_url":"//'
}

# Wait for ngrok to be ready
echo "Waiting for ngrok to start..."
RETRIES=0
NGROK_URL=""

while [ $RETRIES -lt $MAX_RETRIES ]; do
    NGROK_URL=$(get_ngrok_url 2>/dev/null || echo "")

    if [ -n "$NGROK_URL" ] && [ "$NGROK_URL" != "null" ]; then
        echo "Ngrok is ready!"
        break
    fi

    RETRIES=$((RETRIES + 1))
    echo "Waiting for ngrok... (attempt $RETRIES/$MAX_RETRIES)"
    sleep $RETRY_INTERVAL
done

if [ -z "$NGROK_URL" ] || [ "$NGROK_URL" = "null" ]; then
    echo "ERROR: Failed to get ngrok URL after $MAX_RETRIES attempts"
    exit 1
fi

# Construct webhook URL
WEBHOOK_URL="${NGROK_URL}/api/bot/webhook"

echo ""
echo "============================================"
echo "Ngrok Tunnel Information"
echo "============================================"
echo "Public URL: $NGROK_URL"
echo "Webhook URL: $WEBHOOK_URL"
echo ""

# Delete existing webhook first (clean slate)
echo "Removing existing webhook..."
DELETE_RESPONSE=$(curl -s "https://api.telegram.org/bot${BOT_TOKEN}/deleteWebhook")
echo "Delete response: $DELETE_RESPONSE"

# Small delay to ensure deletion is processed
sleep 1

# Register new webhook with Telegram
echo ""
echo "Registering webhook with Telegram..."
REGISTER_RESPONSE=$(curl -s -X POST "https://api.telegram.org/bot${BOT_TOKEN}/setWebhook" \
    -H "Content-Type: application/json" \
    -d "{
        \"url\": \"${WEBHOOK_URL}\",
        \"secret_token\": \"${WEBHOOK_SECRET}\",
        \"allowed_updates\": [\"message\", \"callback_query\", \"inline_query\"],
        \"drop_pending_updates\": true
    }")

echo "Register response: $REGISTER_RESPONSE"

# Verify webhook was set correctly
echo ""
echo "Verifying webhook configuration..."
WEBHOOK_INFO=$(curl -s "https://api.telegram.org/bot${BOT_TOKEN}/getWebhookInfo")
echo "Webhook info: $WEBHOOK_INFO"

# Check if registration was successful
if echo "$REGISTER_RESPONSE" | grep -q '"ok":true'; then
    echo ""
    echo "============================================"
    echo "SUCCESS! Webhook registered successfully"
    echo "============================================"
    echo ""
    echo "Your bot is now configured to receive updates at:"
    echo "  $WEBHOOK_URL"
    echo ""
    echo "Frontend (if ngrok tunnel enabled):"
    # Try to get frontend URL
    FRONTEND_URL=$(curl -s "$NGROK_API_URL/api/tunnels" | \
        grep -o '"public_url":"https://[^"]*' | \
        grep "frontend" | \
        head -1 | \
        sed 's/"public_url":"//' || echo "Not available")
    echo "  $FRONTEND_URL"
    echo ""
else
    echo ""
    echo "============================================"
    echo "WARNING: Webhook registration may have failed"
    echo "============================================"
    echo "Please check the response above for errors"
    echo ""
fi

# Keep container running to allow viewing logs
echo "Webhook registration complete. Container will exit now."
echo "To view ngrok URLs, run: docker-compose logs ngrok"
