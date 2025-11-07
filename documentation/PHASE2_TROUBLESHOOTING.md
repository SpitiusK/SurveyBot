# Phase 2 Troubleshooting Guide

Solutions to common issues in SurveyBot Phase 2.

## Table of Contents

1. [API Issues](#api-issues)
2. [Authentication Problems](#authentication-problems)
3. [Database Issues](#database-issues)
4. [Bot Issues](#bot-issues)
5. [Common HTTP Errors](#common-http-errors)
6. [Performance Issues](#performance-issues)
7. [Debug Logging](#debug-logging)

---

## API Issues

### API Won't Start

**Symptoms:**
- Application crashes on startup
- Error: "Port already in use"
- Exception during startup

**Solutions:**

1. **Port Conflict**
   ```bash
   # Windows - Find process using port 5000
   netstat -ano | findstr :5000
   # Kill process (replace PID)
   taskkill /PID <PID> /F

   # Linux/Mac
   lsof -ti:5000 | xargs kill
   ```

2. **Missing Configuration**
   - Check `appsettings.json` exists
   - Verify all required settings present
   - Check JWT secret key is set

3. **Database Connection**
   - Verify connection string
   - Ensure database file/server accessible
   - Run migrations: `dotnet ef database update`

4. **Check Logs**
   ```bash
   # View startup logs
   dotnet run --verbosity detailed
   ```

### API Returns 500 Internal Server Error

**Symptoms:**
- All endpoints return 500
- Generic error message

**Solutions:**

1. **Check Application Logs**
   - Location: `logs/surveybot-{date}.log`
   - Look for exception stack traces

2. **Enable Developer Exception Page**
   ```json
   // appsettings.Development.json
   {
     "DetailedErrors": true,
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   ```

3. **Common Causes**
   - Database connection failed
   - Null reference exceptions
   - Configuration missing
   - Dependency injection error

### Swagger UI Not Working

**Symptoms:**
- 404 when accessing /swagger
- Swagger page blank

**Solutions:**

1. **Check Environment**
   - Swagger only enabled in Development
   - Set: `ASPNETCORE_ENVIRONMENT=Development`

2. **Verify Configuration**
   ```csharp
   // Program.cs should have:
   if (app.Environment.IsDevelopment())
   {
       app.UseSwagger();
       app.UseSwaggerUI();
   }
   ```

3. **Clear Browser Cache**
   - Hard refresh: Ctrl+F5
   - Clear cache and reload

---

## Authentication Problems

### 401 Unauthorized on All Requests

**Symptoms:**
- Every authenticated endpoint returns 401
- Token appears valid

**Solutions:**

1. **Check Authorization Header**
   ```bash
   # Correct format
   Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

   # Common mistakes:
   # ❌ Authorization: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   # ❌ Bearer: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   # ✅ Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```

2. **Verify Token Not Expired**
   - Decode JWT at https://jwt.io
   - Check `exp` claim
   - Re-login if expired

3. **Check JWT Configuration**
   ```json
   // appsettings.json
   {
     "JwtSettings": {
       "SecretKey": "must-be-at-least-32-characters-long",
       "Issuer": "SurveyBot.API",
       "Audience": "SurveyBot.Client",
       "ExpiresInHours": 24
     }
   }
   ```

4. **Verify Secret Key Matches**
   - Secret key must be same for token generation and validation
   - Minimum 32 characters
   - No special characters that might cause encoding issues

### Cannot Login - 400 Bad Request

**Symptoms:**
- POST /api/auth/login returns 400
- Error: "Invalid request data"

**Solutions:**

1. **Check Request Format**
   ```json
   // Correct
   {
     "telegramId": 123456789
   }

   // Wrong
   {
     "telegram_id": 123456789
   }
   {
     "telegramId": "123456789"  // Should be number, not string
   }
   ```

2. **Verify Content-Type Header**
   ```
   Content-Type: application/json
   ```

3. **Check Telegram ID**
   - Must be valid integer
   - Positive number
   - Not zero

### 403 Forbidden When Accessing Survey

**Symptoms:**
- Can see own surveys
- Get 403 when accessing other users' surveys

**Solution:**
- **This is expected behavior**
- Users can only access surveys they created
- For testing, use the survey creator's token

---

## Database Issues

### Database Not Found

**Symptoms:**
- Error: "Unable to open the database file"
- Error: "A network-related or instance-specific error"

**Solutions:**

1. **SQLite**
   ```bash
   # Check database file exists
   ls surveybot.db

   # If missing, run migrations
   dotnet ef database update

   # Verify connection string
   # appsettings.json
   "ConnectionStrings": {
     "DefaultConnection": "Data Source=surveybot.db"
   }
   ```

2. **SQL Server**
   ```bash
   # Test connection
   sqlcmd -S localhost -U sa -P YourPassword

   # Verify server is running
   # Windows: Services -> SQL Server
   # Linux: systemctl status mssql-server
   ```

### Migration Errors

**Symptoms:**
- Error when running `dotnet ef database update`
- "No migrations found"
- "Build failed"

**Solutions:**

1. **Install EF Tools**
   ```bash
   dotnet tool install --global dotnet-ef
   dotnet tool update --global dotnet-ef
   ```

2. **Build First**
   ```bash
   dotnet build
   dotnet ef database update
   ```

3. **Specify Project**
   ```bash
   cd src/SurveyBot.API
   dotnet ef database update --project ../SurveyBot.Infrastructure
   ```

4. **Reset Database**
   ```bash
   # Drop database
   dotnet ef database drop

   # Recreate
   dotnet ef database update
   ```

### Database Locked (SQLite)

**Symptoms:**
- Error: "database is locked"
- Operations timeout

**Solutions:**

1. **Close Other Connections**
   - Close DB Browser for SQLite
   - Stop other API instances
   - Close Visual Studio database connections

2. **Restart Application**
   ```bash
   # Kill all dotnet processes
   # Windows
   taskkill /IM dotnet.exe /F

   # Linux/Mac
   killall dotnet
   ```

---

## Bot Issues

### Bot Not Responding

**Symptoms:**
- Messages sent to bot, no response
- Commands do nothing

**Solutions:**

1. **Check Bot is Running**
   ```bash
   # Should see bot polling log
   dotnet run
   # Expected: "Bot started polling..."
   ```

2. **Verify Bot Token**
   ```json
   // appsettings.json
   {
     "BotConfiguration": {
       "BotToken": "YOUR_BOT_TOKEN_FROM_BOTFATHER",
       "BotUsername": "YourBotUsername"
     }
   }
   ```

3. **Test Bot Token**
   ```bash
   # Test with API
   curl https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getMe

   # Should return bot info
   ```

4. **Check Logs**
   - Look for exceptions in bot console
   - Check for authentication errors
   - Verify database connection

### Bot Commands Don't Work

**Symptoms:**
- /start works
- Other commands don't work

**Solutions:**

1. **Verify Command Handlers Registered**
   ```csharp
   // Should be in ServiceCollectionExtensions
   services.AddScoped<ICommandHandler, StartCommandHandler>();
   services.AddScoped<ICommandHandler, HelpCommandHandler>();
   services.AddScoped<ICommandHandler, SurveysCommandHandler>();
   services.AddScoped<ICommandHandler, MySurveysCommandHandler>();
   ```

2. **Check Command Format**
   ```
   ✅ /start
   ✅ /help
   ❌ / start (space)
   ❌ /Start (case matters if not configured)
   ```

3. **Check Logs for Handler Errors**

### /start Doesn't Register User

**Symptoms:**
- /start runs but user not in database
- Later commands fail

**Solutions:**

1. **Check API Connection**
   - Verify API is running
   - Check API URL in bot configuration
   - Test API endpoint directly

2. **Check User Repository**
   - Verify CreateOrUpdateAsync method works
   - Check database permissions
   - Look for exceptions in logs

3. **Test API Endpoint**
   ```bash
   curl -X POST http://localhost:5000/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "telegramId": 123456789,
       "username": "test",
       "firstName": "Test",
       "lastName": "User"
     }'
   ```

---

## Common HTTP Errors

### 400 Bad Request

**Meaning:** Invalid request data

**Common Causes:**

1. **Missing Required Field**
   ```json
   // Missing title
   {
     "description": "Test"
   }
   ```
   **Solution:** Include all required fields

2. **Invalid Data Type**
   ```json
   // telegramId should be number
   {
     "telegramId": "123"
   }
   ```
   **Solution:** Use correct data types

3. **Validation Error**
   - Title too long (>200 chars)
   - Options missing for choice questions
   - Invalid question type

**Debug:**
- Check error response `data` field for specific validation errors
- Review API documentation for field requirements

### 401 Unauthorized

**Meaning:** Authentication required or token invalid

**Solutions:**
- Include Authorization header
- Check token format: `Bearer <token>`
- Verify token not expired
- Re-login to get fresh token

### 403 Forbidden

**Meaning:** User doesn't have permission

**Common Causes:**
- Trying to access another user's survey
- Trying to modify survey you don't own

**Solution:**
- Verify you're using correct user token
- Check resource ownership

### 404 Not Found

**Meaning:** Resource doesn't exist

**Common Causes:**
- Wrong ID
- Resource deleted
- Typo in URL

**Solutions:**
- Verify ID is correct
- Check resource exists: GET /api/surveys/{id}
- Check URL spelling

### 500 Internal Server Error

**Meaning:** Server-side error

**Solutions:**
- Check server logs
- Enable detailed errors in Development
- Report with logs if bug

---

## Performance Issues

### Slow API Responses

**Symptoms:**
- Requests take >1 second
- Timeouts on large datasets

**Solutions:**

1. **Enable Pagination**
   ```bash
   # Use smaller page sizes
   GET /api/surveys?pageSize=10
   ```

2. **Add Database Indexes**
   - Check EF Core migrations include indexes
   - Add indexes for frequently queried fields

3. **Enable Response Caching**
   - Consider caching survey lists
   - Cache statistics

4. **Profile Database Queries**
   ```json
   // Enable query logging
   {
     "Logging": {
       "LogLevel": {
         "Microsoft.EntityFrameworkCore.Database.Command": "Information"
       }
     }
   }
   ```

### High Memory Usage

**Symptoms:**
- Application memory grows over time
- Out of memory exceptions

**Solutions:**

1. **Check for Memory Leaks**
   - Dispose DbContext properly
   - Don't cache large objects
   - Use pagination for lists

2. **Enable GC Monitoring**
   ```bash
   # Run with GC logging
   dotnet run --property:GCEELogFilePath=gc.log
   ```

---

## Debug Logging

### Enable Detailed Logging

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore": "Information",
      "SurveyBot": "Debug"
    }
  }
}
```

### View Logs

**Console Output:**
- Shown in terminal where app runs
- Includes all log levels based on configuration

**File Logs:**
```bash
# Check log file location in Program.cs
# Typical: logs/surveybot-{date}.log
tail -f logs/surveybot-20251107.log
```

### Log Database Queries

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### Add Custom Logging

```csharp
// In controller or service
_logger.LogDebug("Processing survey {SurveyId} for user {UserId}", surveyId, userId);
_logger.LogWarning("Survey {SurveyId} not found", surveyId);
_logger.LogError(ex, "Error processing survey {SurveyId}", surveyId);
```

---

## Getting Help

### Before Reporting Issues

1. **Check Logs**
   - Application logs
   - Database logs
   - Bot logs

2. **Verify Configuration**
   - All required settings present
   - Values are correct
   - Secrets not exposed

3. **Test with Postman**
   - Isolate if issue is with client or server
   - Use Postman collection for testing

4. **Check Documentation**
   - API Reference
   - Authentication Flow
   - This troubleshooting guide

### Reporting Bugs

Include:
- Error message (full text)
- Steps to reproduce
- Expected vs actual behavior
- Environment (OS, .NET version)
- Relevant logs
- Configuration (without secrets)

### Emergency Recovery

```bash
# Complete reset (CAUTION: Deletes all data)

# 1. Stop all services
# Ctrl+C in all terminals

# 2. Delete database
rm surveybot.db

# 3. Clean build
dotnet clean
dotnet build

# 4. Recreate database
dotnet ef database update

# 5. Restart services
dotnet run
```

---

## Quick Diagnostic Commands

```bash
# Check API health
curl http://localhost:5000/health

# Verify database exists
ls surveybot.db

# Check .NET version
dotnet --version

# List running dotnet processes
# Windows
tasklist | findstr dotnet

# Linux/Mac
ps aux | grep dotnet

# Check port usage
# Windows
netstat -ano | findstr :5000

# Linux/Mac
lsof -i:5000

# View recent logs
tail -n 50 logs/surveybot-*.log

# Test bot token
curl https://api.telegram.org/bot<TOKEN>/getMe
```

---

## Prevention Tips

1. **Use Version Control** - Commit working configurations
2. **Backup Database** - Regular backups of surveybot.db
3. **Monitor Logs** - Check logs regularly
4. **Test Changes** - Test in dev before production
5. **Document** - Note any configuration changes
6. **Update Dependencies** - Keep packages up to date
7. **Code Reviews** - Have changes reviewed
8. **Automated Tests** - Run tests before deploying
