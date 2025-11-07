# Phase 2 Quick Start Guide

Get started with SurveyBot Phase 2 in 10 minutes.

## Prerequisites

- .NET 8 SDK installed
- SQL Server or SQLite
- Telegram account
- curl or Postman

## Step 1: Clone and Setup (2 minutes)

```bash
# Navigate to project
cd C:\Users\User\Desktop\SurveyBot

# Restore dependencies
dotnet restore

# Run database migrations
cd src\SurveyBot.API
dotnet ef database update
```

## Step 2: Configure Settings (1 minute)

**src/SurveyBot.API/appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=surveybot.db"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-minimum-32-characters-long-for-development",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Client",
    "ExpiresInHours": 24
  },
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN_FROM_BOTFATHER",
    "BotUsername": "YourBotUsername"
  }
}
```

## Step 3: Start API (1 minute)

```bash
# Start API
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet run

# API runs at: http://localhost:5000
# Swagger UI: http://localhost:5000/swagger
```

## Step 4: Test API (3 minutes)

### Login and Get Token

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 123456789}'
```

**Save the token from response:**
```bash
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Create Your First Survey

```bash
curl -X POST http://localhost:5000/api/surveys \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "My First Survey",
    "description": "Quick start test survey"
  }'
```

**Save the survey ID from response:**
```bash
SURVEY_ID=1
```

### Add a Question

```bash
curl -X POST http://localhost:5000/api/surveys/$SURVEY_ID/questions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "How would you rate this quick start guide?",
    "type": "Rating",
    "isRequired": true
  }'
```

### Activate Survey

```bash
curl -X POST http://localhost:5000/api/surveys/$SURVEY_ID/activate \
  -H "Authorization: Bearer $TOKEN"
```

### View Survey

```bash
curl -X GET http://localhost:5000/api/surveys/$SURVEY_ID \
  -H "Authorization: Bearer $TOKEN"
```

## Step 5: Start Bot (Optional - 2 minutes)

```bash
# In new terminal
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot
dotnet run
```

### Test Bot Commands

1. Open Telegram
2. Search for your bot: @YourBotUsername
3. Send `/start` - Bot registers you
4. Send `/surveys` - See your active survey
5. Send `/mysurveys` - Manage your surveys

## Step 6: Explore Swagger UI (1 minute)

Open browser: http://localhost:5000/swagger

- Try authentication endpoints
- Test survey CRUD operations
- View request/response schemas
- Use "Try it out" for interactive testing

## Essential Endpoints

### Authentication
```
POST   /api/auth/login
POST   /api/auth/register
GET    /api/auth/me
```

### Surveys
```
GET    /api/surveys              # List user's surveys
POST   /api/surveys              # Create survey
GET    /api/surveys/{id}         # Get survey details
PUT    /api/surveys/{id}         # Update survey
DELETE /api/surveys/{id}         # Delete survey
POST   /api/surveys/{id}/activate      # Activate
POST   /api/surveys/{id}/deactivate    # Deactivate
GET    /api/surveys/{id}/statistics    # Get stats
```

### Questions
```
GET    /api/surveys/{surveyId}/questions          # List questions
POST   /api/surveys/{surveyId}/questions          # Add question
PUT    /api/questions/{id}                        # Update question
DELETE /api/questions/{id}                        # Delete question
POST   /api/surveys/{surveyId}/questions/reorder  # Reorder
```

## Quick Test Script

Save as `test-api.sh`:

```bash
#!/bin/bash
API="http://localhost:5000"

# Login
echo "1. Logging in..."
TOKEN=$(curl -s -X POST "$API/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 123456789}' | jq -r '.data.accessToken')
echo "Token: ${TOKEN:0:20}..."

# Create Survey
echo "2. Creating survey..."
SURVEY=$(curl -s -X POST "$API/api/surveys" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Quick Test","description":"Test survey"}')
SURVEY_ID=$(echo $SURVEY | jq -r '.data.id')
echo "Survey ID: $SURVEY_ID"

# Add Question
echo "3. Adding question..."
curl -s -X POST "$API/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"text":"Test question?","type":"Rating","isRequired":true}' > /dev/null

# Activate
echo "4. Activating survey..."
curl -s -X POST "$API/api/surveys/$SURVEY_ID/activate" \
  -H "Authorization: Bearer $TOKEN" > /dev/null

# List Surveys
echo "5. Listing surveys..."
curl -s -X GET "$API/api/surveys" \
  -H "Authorization: Bearer $TOKEN" | jq '.data.items[] | {id, title, isActive}'

echo "Done! Survey $SURVEY_ID is active."
```

Run: `chmod +x test-api.sh && ./test-api.sh`

## Common Issues

### Issue: Database not found
```
Solution: Run `dotnet ef database update`
```

### Issue: 401 Unauthorized
```
Solution: Include Authorization header with Bearer token
```

### Issue: Bot not responding
```
Solution:
1. Check bot token in appsettings.json
2. Verify bot is running (dotnet run)
3. Check bot logs for errors
```

### Issue: Port already in use
```
Solution: Change port in launchSettings.json or kill process:
- Windows: netstat -ano | findstr :5000
- Linux: lsof -ti:5000 | xargs kill
```

## Next Steps

1. **Read Full Documentation**
   - API Reference: `documentation/api/PHASE2_API_REFERENCE.md`
   - Auth Flow: `documentation/auth/AUTHENTICATION_FLOW.md`
   - Bot Commands: `documentation/bot/BOT_COMMANDS.md`

2. **Explore Postman Collection**
   - Import: `docs/PostmanCollection-Phase2.json`
   - Contains all endpoints with examples

3. **Run Tests**
   ```bash
   cd tests/SurveyBot.Tests
   dotnet test
   ```

4. **Create More Surveys**
   - Try different question types
   - Test pagination and filtering
   - View statistics

5. **Deploy to Production**
   - Configure production settings
   - Set up database
   - Deploy to Azure/AWS/etc.

## Quick Reference

### Question Types
- `Text` - Free text answer
- `SingleChoice` - Radio buttons (requires options)
- `MultipleChoice` - Checkboxes (requires options)
- `Rating` - 1-5 stars

### Status Codes
- `200 OK` - Success
- `201 Created` - Resource created
- `204 No Content` - Successful delete
- `400 Bad Request` - Validation error
- `401 Unauthorized` - Missing/invalid token
- `403 Forbidden` - No permission
- `404 Not Found` - Resource not found

### Bot Commands
- `/start` - Register and show menu
- `/help` - Show commands
- `/surveys` - Browse active surveys
- `/mysurveys` - Manage your surveys

## Support

- **Documentation:** `C:\Users\User\Desktop\SurveyBot\documentation\`
- **Troubleshooting:** `documentation\PHASE2_TROUBLESHOOTING.md`
- **API Docs:** http://localhost:5000/swagger

## Success Checklist

- [ ] API running at http://localhost:5000
- [ ] Database created with migrations
- [ ] Successfully logged in and got token
- [ ] Created survey via API
- [ ] Added question to survey
- [ ] Activated survey
- [ ] Bot responding to commands (if running)
- [ ] Swagger UI accessible

You're ready to build with SurveyBot! Check the full documentation for advanced features.
