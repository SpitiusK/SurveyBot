# SurveyBot - Complete Testing Guide

**Last Updated**: November 11, 2025
**Version**: 1.0.0

---

## Table of Contents
1. [Quick Start](#quick-start)
2. [Prerequisites](#prerequisites)
3. [Setup Steps](#setup-steps)
4. [Testing Checklist](#testing-checklist)
5. [Troubleshooting](#troubleshooting)

---

## Quick Start

### For Testing WITHOUT Telegram Bot (Recommended for MVP)
```bash
# Terminal 1: Start Backend API
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet run

# Terminal 2: Start Frontend
cd C:\Users\User\Desktop\SurveyBot\frontend
npm run dev

# Open browser: http://localhost:5173
# Login with any Telegram ID (e.g., 123456789)
```

### For Testing WITH Telegram Bot
Follow all steps in this guide (30-45 minutes setup)

---

## Prerequisites

### 1. Telegram Bot Token
You need a bot token from Telegram BotFather:

**Steps**:
1. Open Telegram and search for **@BotFather**
2. Send: `/newbot`
3. Follow prompts to create a new bot
4. Copy the token (looks like: `123456789:ABCDefGHIjklmnop_QRSTuvwxyz`)
5. Save it in `appsettings.json`

### 2. Telegram Bot Link (for Testing)
You can test the bot directly in Telegram by:
- Searching for your bot name in Telegram
- Or getting the link: `t.me/YourBotName`

### 3. ngrok for Webhook Testing (Production-like)
Download from: https://ngrok.com/download

**Why ngrok?**
- Telegram needs a public HTTPS URL to send webhooks
- ngrok creates a secure tunnel to your local machine
- Alternative: Deploy to cloud server (AWS, Azure, Heroku, etc.)

### 4. Development Tools
- Visual Studio Code or Visual Studio
- Postman (for API testing)
- Browser DevTools (Chrome/Firefox)
- Git (for version control)

---

## Setup Steps

### Phase 1: Backend API Setup (15 minutes)

#### Step 1: Configure PostgreSQL
```bash
# Check Docker is running
docker --version

# Start PostgreSQL with docker-compose
cd C:\Users\User\Desktop\SurveyBot
docker-compose up -d

# Verify PostgreSQL is running
docker ps
# Should see: surveybot-postgres and surveybot-pgadmin

# Access pgAdmin (optional)
# URL: http://localhost:5050
# Username: admin@admin.com
# Password: admin
```

#### Step 2: Apply Database Migrations
```bash
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API

# Apply migrations
dotnet ef database update

# Verify: Check pgAdmin or use psql
# Tables should exist: users, surveys, questions, responses, answers
```

#### Step 3: Configure Bot Token
Edit `appsettings.json`:
```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "UseWebhook": false,  // Set to false for polling (testing)
    "WebhookUrl": "https://your-public-url.com",
    "WebhookSecret": "your-secret-key",
    "ApiBaseUrl": "http://localhost:5000"
  }
}
```

#### Step 4: Start Backend
```bash
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet run

# Output should show:
# - Listening on http://localhost:5000
# - Swagger UI: http://localhost:5000/swagger
# - Health check: http://localhost:5000/health
```

**Verify Backend is Running**:
- Open: http://localhost:5000/health
- Should return: `{"status":"Healthy"}`

---

### Phase 2: Frontend Setup (10 minutes)

#### Step 1: Install Dependencies
```bash
cd C:\Users\User\Desktop\SurveyBot\frontend
npm install
```

#### Step 2: Configure Environment
Create `.env.local` (already created, verify):
```env
VITE_API_BASE_URL=http://localhost:5000/api
VITE_APP_NAME=SurveyBot Admin Panel
```

#### Step 3: Start Dev Server
```bash
npm run dev

# Output should show:
# - Local: http://localhost:5173
# - Press 'o' to open in browser
```

**Verify Frontend is Running**:
- Open: http://localhost:5173
- Should see login page

---

### Phase 3: Telegram Bot Setup (Optional, 15-30 minutes)

#### Option A: Polling Mode (Easiest, No Public URL Needed)

```bash
# In appsettings.json, set:
{
  "BotConfiguration": {
    "UseWebhook": false
  }
}

# Restart backend
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet run

# Bot will now poll Telegram API automatically
# Send /start to bot in Telegram to test
```

**Advantages**:
- ✅ No public URL needed
- ✅ Works behind firewalls
- ✅ Perfect for local development

**Disadvantages**:
- ❌ Slight delay in updates (polling interval)
- ❌ More API calls to Telegram

#### Option B: Webhook Mode (Production-like, Requires Public URL)

##### Step 1: Setup ngrok
```bash
# Download ngrok from https://ngrok.com/download
# Or install via package manager

# Windows (chocolatey):
choco install ngrok

# Start ngrok tunnel to port 5000
ngrok http 5000

# Copy the HTTPS URL (looks like: https://abc123def456.ngrok.io)
```

##### Step 2: Configure Webhook in appsettings.json
```json
{
  "BotConfiguration": {
    "UseWebhook": true,
    "WebhookUrl": "https://abc123def456.ngrok.io",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "your-secure-secret-key-min-32-chars",
    "BotToken": "YOUR_BOT_TOKEN"
  }
}
```

##### Step 3: Restart Backend
```bash
# Backend will auto-register webhook with Telegram
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet run

# Should log: "Webhook set successfully"
```

##### Step 4: Test Webhook
```bash
# In another terminal, test endpoint
curl -X POST https://abc123def456.ngrok.io/api/bot/webhook \
  -H "Content-Type: application/json" \
  -d '{"update_id": 1, "message": {"text": "/start", "from": {"id": 123456789}}}'
```

**Advantages**:
- ✅ Real-time updates
- ✅ Production-like
- ✅ Lower API usage

**Disadvantages**:
- ❌ Requires public URL
- ❌ ngrok tunnels are temporary

---

## Testing Checklist

### 1. Backend API Tests

#### Health Check
```bash
curl http://localhost:5000/health
# Expected: {"status":"Healthy"}
```

#### Authentication
```bash
# Login with Telegram ID
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 123456789}'

# Expected: {"success":true,"data":{"token":"eyJhbGc..."}...}
```

#### Swagger UI
- Open: http://localhost:5000/swagger
- Try endpoints manually
- Test all CRUD operations

### 2. Frontend Tests

#### Login Flow
1. Open http://localhost:5173
2. Enter Telegram ID: `123456789`
3. Click Login
4. Should redirect to Dashboard

#### Survey Management
1. Click "Create Survey"
2. Fill in title: "Test Survey"
3. Add questions (Text, SingleChoice, etc.)
4. Publish
5. Copy survey code
6. View statistics

#### Statistics & Export
1. Go to Statistics tab
2. View charts and metrics
3. Click Export CSV
4. Verify CSV downloads

### 3. Telegram Bot Tests

#### Test Commands
Send these to your bot in Telegram:

1. **`/start`**
   - Expected: Bot registers you, sends welcome message
   - Check: User created in database

2. **`/help`**
   - Expected: List of available commands

3. **`/mysurveys`**
   - Expected: List your surveys

4. **`/survey {code}`**
   - Expected: Start survey, show first question
   - Example: `/survey ABC123`

#### Test Survey Flow
1. Create survey in Admin Panel
2. Publish survey (get code, e.g., "ABC123")
3. Send `/survey ABC123` to bot
4. Answer all questions
5. Check: Responses appear in Statistics

#### Test Different Question Types
- **Text**: Send any text response
- **SingleChoice**: Click button option
- **MultipleChoice**: Click multiple buttons, then "Done"
- **Rating**: Click number (1-5)

### 4. Integration Tests

#### Full Flow Test
1. Admin creates survey in web panel
2. Publishes and gets code
3. Shares code with Telegram user
4. User takes survey via bot
5. Admin views responses in Statistics
6. Admin exports to CSV
7. Open CSV in Excel/Google Sheets

#### Multi-User Test
1. Create survey
2. Have 5+ users respond via bot
3. Check completion rate
4. View charts with multiple responses
5. Export and verify all responses

---

## Testing Scenarios

### Scenario 1: Admin Creates and Analyzes Survey (15 min)

```
1. Login to http://localhost:5173 (Telegram ID: 111111111)
2. Create Survey:
   - Title: "Customer Satisfaction"
   - Description: "Please rate our service"
3. Add Questions:
   - Q1 (SingleChoice): "How satisfied are you?"
     - Options: Very Satisfied, Satisfied, Neutral, Unsatisfied
   - Q2 (Rating): "Rate our service 1-5"
   - Q3 (Text): "Any comments?"
4. Publish Survey (copy code: e.g., "ABC123")
5. View Statistics (should be empty)
```

### Scenario 2: User Takes Survey (10 min)

```
1. Open Telegram
2. Search for your bot, tap /start
3. Bot responds with welcome
4. Send: /survey ABC123
5. Bot shows question 1, click option
6. Bot shows question 2, click rating
7. Bot shows question 3, type comment
8. Complete survey
9. Bot confirms: "Thank you! Survey complete"
```

### Scenario 3: Verify Response in Admin Panel (5 min)

```
1. Go back to http://localhost:5173
2. Go to Statistics for the survey
3. See: 1 response, 100% completion
4. View response details
5. Export to CSV
6. Open CSV: Verify all answers
```

---

## Troubleshooting

### Backend Issues

#### "Cannot connect to PostgreSQL"
```bash
# Check Docker is running
docker ps

# Check connection string in appsettings.json
# Should be: "Host=localhost;Port=5432;..."

# Restart containers
docker-compose restart

# Apply migrations again
dotnet ef database update
```

#### "Port 5000 already in use"
```bash
# Change port in launchSettings.json
# Or kill the process using port 5000
netstat -ano | findstr :5000
taskkill /PID <PID> /F
```

#### "Swagger UI not loading"
```bash
# Check backend is running
# Check firewall allows port 5000
# Try: http://localhost:5000/swagger/index.html
```

### Frontend Issues

#### "Cannot connect to API"
```bash
# Check backend is running on http://localhost:5000
# Check .env file has correct API URL
# Check CORS is enabled in backend
# Check browser DevTools (F12) for errors
```

#### "Port 5173 already in use"
```bash
# Kill process or use different port
npm run dev -- --port 3000
```

#### "Login fails with Telegram ID"
```bash
# Verify backend is running
# Check Telegram ID format (should be number)
# Check /api/auth/login endpoint in Swagger
# Check database for users table
```

### Telegram Bot Issues

#### "Bot not responding"
```bash
# Check bot token in appsettings.json
# Check UseWebhook setting (false for polling)
# Check backend logs for errors
# Verify /start command registered with BotFather
```

#### "Webhook not working"
```bash
# Check ngrok is running
# Check WebhookUrl in appsettings.json matches ngrok URL
# Check WebhookSecret is set (min 32 chars)
# Verify HTTPS (not HTTP)
# Check backend logs for webhook errors
```

#### "Responses not saved"
```bash
# Check responses table in database
# Verify user is registered (check users table)
# Check survey code is correct
# Check survey is active (IsActive = true)
# Check response status in database
```

### Database Issues

#### "Migrations won't apply"
```bash
# Check PostgreSQL is running
dotnet ef database update --verbose

# If still failing, reset database:
dotnet ef database drop
dotnet ef database update
```

#### "No test data"
```bash
# Database seeding happens on first run
# Or manually insert test data via pgAdmin
# Or use API to create surveys/questions
```

---

## Performance Testing

### Load Testing
```bash
# Install Apache Bench
choco install apache-httpd

# Test API endpoint (100 requests, 10 concurrent)
ab -n 100 -c 10 http://localhost:5000/api/surveys

# Check response times
```

### Browser Performance
1. Open DevTools (F12)
2. Go to Network tab
3. Reload page
4. Check load times (target: < 3 seconds)
5. Go to Performance tab
6. Record and analyze

---

## Checklist for Go-Live

- [ ] Backend builds without errors
- [ ] Database migrations applied
- [ ] Frontend builds without errors
- [ ] Login works with Telegram ID
- [ ] Survey creation works
- [ ] Bot responds to commands
- [ ] Survey responses saved correctly
- [ ] Statistics display correctly
- [ ] CSV export works
- [ ] All 4 question types working
- [ ] Mobile responsive on phones
- [ ] Error messages helpful
- [ ] No console errors
- [ ] Performance acceptable

---

## Next Steps

### For Full Bot Integration
1. Register bot officially with Telegram
2. Setup webhook on production server
3. Configure SSL certificates
4. Test with real users
5. Monitor bot logs and performance

### For Production Deployment
1. Deploy backend to cloud (AWS, Azure, Heroku)
2. Deploy frontend to CDN (Vercel, Netlify)
3. Configure domain names
4. Setup monitoring and logging
5. Backup database regularly

---

## Support & Debugging

### Useful Commands

```bash
# View backend logs (real-time)
dotnet run

# Check database in psql
psql -U surveybot -d surveybot_db -c "SELECT * FROM surveys;"

# View Telegram bot logs
# Check BotFather: /mybots → select bot → Bot Settings

# Network debugging
tcpdump -i lo0 -n "port 5000"

# Clean build
dotnet clean && dotnet build
```

### Logging
- Backend logs to console and file
- Frontend logs to browser DevTools Console
- Bot logs to backend console
- Database logs in PostgreSQL logs

---

## Resources

- **Telegram Bot API**: https://core.telegram.org/bots/api
- **BotFather Guide**: https://core.telegram.org/bots
- **ngrok Documentation**: https://ngrok.com/docs
- **ASP.NET Core Docs**: https://docs.microsoft.com/aspnet/core
- **React Documentation**: https://react.dev
- **PostgreSQL Docs**: https://www.postgresql.org/docs/

---

**Happy Testing!** 🎉

For issues or questions, check the troubleshooting section or review the logs.
