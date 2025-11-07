# Database Seeding Guide

## Overview

The DataSeeder class provides a simple way to populate your development database with realistic test data. It creates users, surveys with all question types, and sample responses.

## What Gets Seeded

### Users (3 total)
1. **John Doe** (@john_doe, TelegramId: 123456789)
2. **Jane Smith** (@jane_smith, TelegramId: 987654321)
3. **Test User** (@test_user, TelegramId: 555555555)

### Surveys (3 total)

#### 1. Customer Satisfaction Survey (Active)
- **Creator**: John Doe
- **Status**: Active
- **Multiple Responses**: No
- **Show Results**: Yes
- **Questions**:
  1. Rating: "How would you rate our service overall?" (1-5 scale)
  2. Multiple Choice: "Which of our services have you used?" (5 options)
  3. Single Choice: "How did you hear about us?" (5 options)
  4. Text: "Please share any additional feedback or suggestions:" (optional)

#### 2. New Product Feature Feedback (Active)
- **Creator**: John Doe
- **Status**: Active
- **Multiple Responses**: Yes
- **Show Results**: No
- **Questions**:
  1. Single Choice: "What is your primary use case?" (5 options)
  2. Rating: "How easy is the product to use?" (1-5 scale)
  3. Multiple Choice: "Which features do you find most valuable?" (5 options)
  4. Text: "Describe a feature you wish we had:" (optional)

#### 3. Tech Conference 2025 - Registration (Inactive)
- **Creator**: John Doe
- **Status**: Inactive (for testing)
- **Multiple Responses**: No
- **Show Results**: Yes
- **Questions**:
  1. Text: "What is your full name?" (required)
  2. Single Choice: "Which track are you most interested in?" (5 options)
  3. Multiple Choice: "Which workshops would you like to attend?" (5 options, optional)
  4. Rating: "How would you rate your technical expertise?" (1-5 scale)

### Responses (5 total)

#### Customer Satisfaction Survey - 2 Responses
1. **Jane Smith** (Completed)
   - Rating: 5/5
   - Services used: Online Support, Mobile App, Website
   - Heard via: Friend Referral
   - Feedback: Positive review

2. **Test User** (Completed)
   - Rating: 3/5
   - Services used: Phone Support, Website
   - Heard via: Search Engine
   - Feedback: Mixed review with suggestions

#### Product Feedback Survey - 2 Responses
1. **John Doe** (Completed)
   - Use case: Enterprise
   - Ease of use: 4/5
   - Valuable features: 4 selected
   - Feature request: API improvements

2. **Jane Smith** (Completed)
   - Use case: Small Business
   - Ease of use: 5/5
   - Valuable features: Mobile Access, Custom Reports
   - Feature request: Offline mode

#### Tech Conference Registration - 1 Response
1. **Test User** (Incomplete)
   - Name: Alice Johnson
   - Track: Web Development
   - Incomplete - stopped after 2 questions

## Usage

### Option 1: Using Extension Method

```csharp
// In your application startup (Program.cs or Startup.cs)
using SurveyBot.Infrastructure.Data.Extensions;

// After building your service provider
var app = builder.Build();

// Seed the database
await app.Services.SeedDatabaseAsync();
```

### Option 2: Direct Usage

```csharp
using SurveyBot.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

// Get services
var context = serviceProvider.GetRequiredService<SurveyBotDbContext>();
var logger = serviceProvider.GetRequiredService<ILogger<DataSeeder>>();

// Create and run seeder
var seeder = new DataSeeder(context, logger);
await seeder.SeedAsync();
```

### Option 3: Reset and Seed (WARNING: Deletes All Data)

```csharp
// This drops the database, recreates it, and seeds data
await app.Services.ResetAndSeedDatabaseAsync();
```

## Example Application Integration

### ASP.NET Core Application

```csharp
// Program.cs
using SurveyBot.Infrastructure.Data.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure services...
builder.Services.AddDbContext<SurveyBotDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Seed database in development
if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDatabaseAsync();
}

app.Run();
```

### Console Application

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SurveyBot.Infrastructure.Data;

// Setup DI
var services = new ServiceCollection();

services.AddDbContext<SurveyBotDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=surveybot;Username=postgres;Password=postgres"));

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var serviceProvider = services.BuildServiceProvider();

// Seed database
using var scope = serviceProvider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

var seeder = new DataSeeder(context, logger);
await seeder.SeedAsync();

Console.WriteLine("Database seeded successfully!");
```

## Important Notes

1. **Idempotent**: The seeder is safe to run multiple times. It checks if data exists and skips seeding if the database already has users.

2. **Development Only**: The seeder is designed for development and testing. Do NOT use in production environments.

3. **Clean Database**: For best results, run against a clean database. Use `ResetAndSeedDatabaseAsync()` to drop and recreate.

4. **Migrations Required**: Ensure all migrations are applied before seeding:
   ```csharp
   await context.Database.MigrateAsync();
   ```

5. **Testing**: The seeder works with in-memory databases for testing purposes.

## Verification

After seeding, you can verify the data:

```csharp
// Check users
var userCount = await context.Users.CountAsync();
Console.WriteLine($"Users: {userCount}"); // Should be 3

// Check surveys
var surveyCount = await context.Surveys.CountAsync();
Console.WriteLine($"Surveys: {surveyCount}"); // Should be 3

// Check questions
var questionCount = await context.Questions.CountAsync();
Console.WriteLine($"Questions: {questionCount}"); // Should be 12

// Check responses
var responseCount = await context.Responses.CountAsync();
Console.WriteLine($"Responses: {responseCount}"); // Should be 5

// Check answers
var answerCount = await context.Answers.CountAsync();
Console.WriteLine($"Answers: {answerCount}"); // Should be 18

// Verify all question types
var questionTypes = await context.Questions
    .GroupBy(q => q.QuestionType)
    .Select(g => new { Type = g.Key, Count = g.Count() })
    .ToListAsync();

foreach (var qt in questionTypes)
{
    Console.WriteLine($"{qt.Type}: {qt.Count}");
}
```

## Sample Data Statistics

- **Total Users**: 3
- **Total Surveys**: 3 (2 active, 1 inactive)
- **Total Questions**: 12 (3 Text, 3 Single Choice, 3 Multiple Choice, 3 Rating)
- **Total Responses**: 5 (4 complete, 1 incomplete)
- **Total Answers**: 18 (covering all question types)

## Answer Format Examples

### Text Question
```json
AnswerText: "Great service! Very satisfied with the quick response times..."
AnswerJson: null
```

### Single Choice Question
```json
AnswerText: null
AnswerJson: {"SelectedOption": "Friend Referral"}
```

### Multiple Choice Question
```json
AnswerText: null
AnswerJson: ["Online Support", "Mobile App", "Website"]
```

### Rating Question
```json
AnswerText: null
AnswerJson: {"Value": 5}
```

## Troubleshooting

### "Table already exists" Error
The seeder checks if data exists before seeding. If you see this error, migrations might not have been applied correctly. Run migrations first.

### Connection String Issues
Ensure your connection string is correct and the PostgreSQL server is running.

### Permission Errors
Make sure the database user has permission to create tables and insert data.

### Seeding Takes Too Long
The seeder should complete in under 1 second for in-memory databases and a few seconds for PostgreSQL. If it's slower, check your database connection and performance.
