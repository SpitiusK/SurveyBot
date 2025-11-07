# Phase 2 Testing Guide

Comprehensive guide for testing SurveyBot Phase 2 implementation.

## Overview

This guide covers manual testing, automated testing, and integration testing for Phase 2 features.

## Test Environment Setup

### Prerequisites

```bash
# Required software
- .NET 8 SDK
- SQL Server or SQLite
- Postman (optional)
- Telegram account (for bot testing)
```

### Database Setup

```bash
# Run migrations
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet ef database update
```

### Configuration

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=surveybot_test.db"
  },
  "JwtSettings": {
    "SecretKey": "test-secret-key-minimum-32-characters-long",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Client",
    "ExpiresInHours": 24
  },
  "BotConfiguration": {
    "BotToken": "YOUR_TEST_BOT_TOKEN",
    "BotUsername": "YourTestBot"
  }
}
```

## Running Tests

### Unit Tests

```bash
# Run all tests
cd C:\Users\User\Desktop\SurveyBot\tests\SurveyBot.Tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~SurveyServiceTests"
```

### Integration Tests

```bash
# Run integration tests
dotnet test --filter "Category=Integration"

# Run with logging
dotnet test --logger "console;verbosity=detailed"
```

## Manual Testing Checklist

### Authentication Endpoints

- [ ] POST /api/auth/login - Valid Telegram ID
- [ ] POST /api/auth/login - Invalid Telegram ID
- [ ] POST /api/auth/register - New user
- [ ] POST /api/auth/register - Existing user (update)
- [ ] GET /api/auth/validate - Valid token
- [ ] GET /api/auth/validate - Invalid token
- [ ] GET /api/auth/me - Get current user

### Survey Endpoints

**Create Survey:**
- [ ] Valid survey data
- [ ] Missing title
- [ ] Title too long (>200 chars)
- [ ] Description too long (>1000 chars)
- [ ] Without authentication

**List Surveys:**
- [ ] Default pagination
- [ ] Custom page size
- [ ] Search by title
- [ ] Filter by active status
- [ ] Sort by date
- [ ] Empty result set

**Get Survey:**
- [ ] Existing survey (owner)
- [ ] Existing survey (non-owner, active)
- [ ] Existing survey (non-owner, inactive) - should fail
- [ ] Non-existent survey

**Update Survey:**
- [ ] Valid update
- [ ] Update active survey with responses - should fail
- [ ] Update as non-owner - should fail

**Delete Survey:**
- [ ] Delete survey without responses
- [ ] Delete survey with responses (soft delete)
- [ ] Delete as non-owner - should fail

**Activate/Deactivate:**
- [ ] Activate survey with questions
- [ ] Activate survey without questions - should fail
- [ ] Deactivate active survey
- [ ] Toggle by non-owner - should fail

**Statistics:**
- [ ] Get statistics for survey with responses
- [ ] Get statistics for survey without responses
- [ ] Get statistics as non-owner - should fail

### Question Endpoints

**Create Question:**
- [ ] Text question (no options)
- [ ] SingleChoice question (with options)
- [ ] MultipleChoice question (with options)
- [ ] Rating question (no options)
- [ ] Choice question without options - should fail
- [ ] Too few options (<2) - should fail
- [ ] Too many options (>10) - should fail
- [ ] Create for non-owned survey - should fail

**Update Question:**
- [ ] Valid update
- [ ] Update question with responses - should fail
- [ ] Change question type
- [ ] Update options
- [ ] Update as non-owner - should fail

**Delete Question:**
- [ ] Delete question without responses
- [ ] Delete question with responses - should fail
- [ ] Delete from non-owned survey - should fail

**List Questions:**
- [ ] Get questions for active survey (no auth)
- [ ] Get questions for inactive survey (with auth, owner)
- [ ] Get questions for inactive survey (no auth) - should fail

**Reorder Questions:**
- [ ] Valid reorder
- [ ] Invalid question IDs - should fail
- [ ] Missing question IDs - should fail
- [ ] Reorder non-owned survey - should fail

### Bot Commands

- [ ] /start - New user
- [ ] /start - Existing user
- [ ] /help - Display commands
- [ ] /surveys - Show active surveys
- [ ] /surveys - No active surveys
- [ ] /mysurveys - User with surveys
- [ ] /mysurveys - User without surveys
- [ ] /mysurveys - Unregistered user

## Test Data Setup

### Create Test User

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "telegramId": 123456789,
    "username": "testuser",
    "firstName": "Test",
    "lastName": "User"
  }'
```

### Create Test Survey

```bash
# Get token
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 123456789}' | jq -r '.data.accessToken')

# Create survey
curl -X POST http://localhost:5000/api/surveys \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Survey",
    "description": "Test survey description"
  }'
```

### Reset Test Database

```bash
# Delete database
rm surveybot_test.db

# Recreate
dotnet ef database update
```

## Automated Test Examples

### Unit Test Example

```csharp
[Fact]
public async Task CreateSurvey_ValidData_ReturnsSurvey()
{
    // Arrange
    var dto = new CreateSurveyDto
    {
        Title = "Test Survey",
        Description = "Test Description"
    };
    var userId = 1;

    // Act
    var result = await _surveyService.CreateSurveyAsync(userId, dto);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test Survey", result.Title);
    Assert.Equal(userId, result.CreatedBy);
    Assert.False(result.IsActive);
}
```

### Integration Test Example

```csharp
[Fact]
public async Task POST_Surveys_ReturnsCreated()
{
    // Arrange
    var client = _factory.CreateClient();
    var token = await GetTestTokenAsync(client);
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    var dto = new CreateSurveyDto
    {
        Title = "Integration Test Survey",
        Description = "Test"
    };

    // Act
    var response = await client.PostAsJsonAsync("/api/surveys", dto);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
    Assert.True(result.Success);
    Assert.NotNull(result.Data);
}
```

## Coverage Metrics

### Target Coverage

- **Unit Tests:** 80%+ code coverage
- **Integration Tests:** All API endpoints
- **Manual Tests:** Critical user flows

### Generate Coverage Report

```bash
# Install report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html

# Open report
start coveragereport/index.html
```

## Common Test Scenarios

### Scenario 1: Complete Survey Creation Flow

```bash
# 1. Login
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 123456789}' | jq -r '.data.accessToken')

# 2. Create survey
SURVEY_ID=$(curl -s -X POST http://localhost:5000/api/surveys \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Test","description":"Test"}' | jq -r '.data.id')

# 3. Add question
curl -X POST http://localhost:5000/api/surveys/$SURVEY_ID/questions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"text":"Test?","type":"Rating","isRequired":true}'

# 4. Activate
curl -X POST http://localhost:5000/api/surveys/$SURVEY_ID/activate \
  -H "Authorization: Bearer $TOKEN"

# 5. Verify active
curl -X GET http://localhost:5000/api/surveys/$SURVEY_ID \
  -H "Authorization: Bearer $TOKEN"
```

### Scenario 2: Permission Verification

```bash
# Create survey as User A
TOKEN_A=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 111111111}' | jq -r '.data.accessToken')

SURVEY_ID=$(curl -s -X POST http://localhost:5000/api/surveys \
  -H "Authorization: Bearer $TOKEN_A" \
  -H "Content-Type: application/json" \
  -d '{"title":"User A Survey","description":"Test"}' | jq -r '.data.id')

# Try to access as User B (should fail)
TOKEN_B=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 222222222}' | jq -r '.data.accessToken')

curl -X PUT http://localhost:5000/api/surveys/$SURVEY_ID \
  -H "Authorization: Bearer $TOKEN_B" \
  -H "Content-Type: application/json" \
  -d '{"title":"Hacked","description":"Test"}'
# Expected: 403 Forbidden
```

## Debugging Tests

### Enable Detailed Logging

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "SurveyBot": "Debug"
    }
  }
}
```

### View Test Logs

```bash
# Run with verbose logging
dotnet test --logger "console;verbosity=detailed"

# View application logs
tail -f logs/surveybot.log
```

### Debug Test in VS Code

**.vscode/launch.json:**
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Test",
      "type": "coreclr",
      "request": "launch",
      "program": "dotnet",
      "args": ["test", "--no-build"],
      "cwd": "${workspaceFolder}/tests/SurveyBot.Tests",
      "stopAtEntry": false
    }
  ]
}
```

## Performance Testing

### Load Test with Apache Bench

```bash
# Test survey list endpoint
ab -n 1000 -c 10 -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/surveys

# Test survey creation
ab -n 100 -c 5 -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -p survey.json \
  http://localhost:5000/api/surveys
```

### Expected Performance

- **GET endpoints:** <100ms response time
- **POST endpoints:** <200ms response time
- **Concurrent users:** Support 50+ simultaneous users
- **Database queries:** <50ms average

## Continuous Integration

### GitHub Actions Example

**.github/workflows/test.yml:**
```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: 8.0.x
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal
```

## Test Reports

### Generate Test Report

```bash
# Install trx logger
dotnet test --logger "trx;LogFileName=test-results.trx"

# View results
# Open test-results.trx in Visual Studio or compatible tool
```

### Coverage Badge

```markdown
![Coverage](https://img.shields.io/badge/coverage-85%25-green)
```

## Troubleshooting Tests

### Problem: Tests Fail with Database Error

**Solution:**
- Ensure test database exists
- Run migrations: `dotnet ef database update`
- Check connection string in appsettings.Test.json

### Problem: Integration Tests Timeout

**Solution:**
- Increase test timeout
- Check if API is running
- Verify database is accessible
- Review logs for errors

### Problem: Authentication Tests Fail

**Solution:**
- Verify JWT secret key is configured
- Check token expiration time
- Ensure user exists in test database

## Best Practices

1. **Isolate Tests** - Each test should be independent
2. **Clean Up** - Reset database after tests
3. **Use Fixtures** - Share test setup code
4. **Mock External Dependencies** - Don't call real Telegram API
5. **Test Edge Cases** - Invalid data, null values, empty strings
6. **Descriptive Names** - Test names should describe what they test
7. **Assert Clear** - Use specific assertions with messages

## Quick Test Commands

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "ClassName=SurveyServiceTests"

# Run specific test method
dotnet test --filter "MethodName=CreateSurvey_ValidData_ReturnsSurvey"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run integration tests only
dotnet test --filter "Category=Integration"

# Run unit tests only
dotnet test --filter "Category=Unit"
```
