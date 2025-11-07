# Data Seeding

## Overview

The `DataSeeder` class provides a way to populate the database with realistic development and testing data. It creates users, surveys with all question types, and sample responses.

## Quick Start

### Using Extension Methods

The easiest way to seed the database is using the extension methods:

```csharp
using SurveyBot.Infrastructure.Data.Extensions;

// In your Program.cs or Startup
var app = builder.Build();

// Option 1: Seed only (skips if data exists)
await app.Services.SeedDatabaseAsync();

// Option 2: Apply migrations then seed
await app.Services.MigrateDatabaseAsync();
await app.Services.SeedDatabaseAsync();

// Option 3: Reset and seed (WARNING: Deletes all data!)
await app.Services.ResetAndSeedDatabaseAsync();
```

### Using PowerShell Script

A convenient PowerShell script is provided:

```powershell
# Seed with default connection string
.\scripts\seed-database.ps1

# Seed with custom connection string
.\scripts\seed-database.ps1 -ConnectionString "Host=localhost;Database=mydb;Username=user;Password=pass"

# Reset and seed (WARNING: Drops database!)
.\scripts\seed-database.ps1 -Reset
```

### Direct Usage

For more control, use the `DataSeeder` class directly:

```csharp
var context = serviceProvider.GetRequiredService<SurveyBotDbContext>();
var logger = serviceProvider.GetRequiredService<ILogger<DataSeeder>>();

var seeder = new DataSeeder(context, logger);
await seeder.SeedAsync();
```

## Sample Data

### Users (3)
- John Doe (@john_doe) - TelegramId: 123456789
- Jane Smith (@jane_smith) - TelegramId: 987654321
- Test User (@test_user) - TelegramId: 555555555

### Surveys (3)

#### 1. Customer Satisfaction Survey
- Status: Active
- Creator: John Doe
- Questions: 4 (Rating, Multiple Choice, Single Choice, Text)
- Responses: 2 complete

#### 2. New Product Feature Feedback
- Status: Active
- Creator: John Doe
- Questions: 4 (Single Choice, Rating, Multiple Choice, Text)
- Responses: 2 complete

#### 3. Tech Conference 2025 - Registration
- Status: Inactive
- Creator: John Doe
- Questions: 4 (Text, Single Choice, Multiple Choice, Rating)
- Responses: 1 incomplete

### Question Types Coverage

All 4 question types are represented:
- **Text**: Free-form text answers
- **Single Choice**: Radio button selection
- **Multiple Choice**: Checkbox selections
- **Rating**: 1-5 scale rating

### Responses (5)
- 4 complete responses with full answers
- 1 incomplete response (for testing in-progress surveys)

## Data Characteristics

### Realistic Test Data
- Varied response timestamps (from 3 days ago to 12 hours ago)
- Mixed sentiment (positive, neutral, critical feedback)
- Different user personas (enterprise, small business, individual)
- Realistic text answers with context

### Question Format Examples

#### Text Question
```json
AnswerText: "Great service! Very satisfied..."
AnswerJson: null
```

#### Single Choice Question
```json
AnswerText: null
AnswerJson: {"SelectedOption": "Friend Referral"}
```

#### Multiple Choice Question
```json
AnswerText: null
AnswerJson: ["Online Support", "Mobile App", "Website"]
```

#### Rating Question
```json
AnswerText: null
AnswerJson: {"Value": 5}
```

## Features

### Idempotent
The seeder checks if data exists before running. Safe to call multiple times.

```csharp
// Will skip if users already exist
await seeder.SeedAsync();
```

### Comprehensive Coverage
- All entity types (User, Survey, Question, Response, Answer)
- All question types (Text, SingleChoice, MultipleChoice, Rating)
- All relationship types (one-to-many, many-to-one)
- Active and inactive surveys
- Complete and incomplete responses

### Testing Support
Works seamlessly with in-memory databases for testing:

```csharp
var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
    .UseInMemoryDatabase("TestDb")
    .Options;

var context = new SurveyBotDbContext(options);
var seeder = new DataSeeder(context, logger);
await seeder.SeedAsync();
```

## Verification

After seeding, verify the data:

```csharp
var stats = new
{
    Users = await context.Users.CountAsync(),
    Surveys = await context.Surveys.CountAsync(),
    Questions = await context.Questions.CountAsync(),
    Responses = await context.Responses.CountAsync(),
    Answers = await context.Answers.CountAsync()
};

Console.WriteLine($"Users: {stats.Users}");        // 3
Console.WriteLine($"Surveys: {stats.Surveys}");    // 3
Console.WriteLine($"Questions: {stats.Questions}"); // 12
Console.WriteLine($"Responses: {stats.Responses}"); // 5
Console.WriteLine($"Answers: {stats.Answers}");    // 18
```

## Integration Examples

### ASP.NET Core Web Application

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SurveyBotDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDatabaseAsync();
}

app.Run();
```

### Console Application

```csharp
var services = new ServiceCollection();

services.AddDbContext<SurveyBotDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=surveybot;Username=postgres;Password=postgres"));

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var serviceProvider = services.BuildServiceProvider();

using var scope = serviceProvider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

var seeder = new DataSeeder(context, logger);
await seeder.SeedAsync();
```

### Testing

```csharp
[Fact]
public async Task SeedAsync_CreatesAllEntities()
{
    // Arrange
    var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
        .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
        .Options;

    var context = new SurveyBotDbContext(options);
    var logger = LoggerFactory.Create(b => b.AddDebug())
        .CreateLogger<DataSeeder>();

    var seeder = new DataSeeder(context, logger);

    // Act
    await seeder.SeedAsync();

    // Assert
    Assert.Equal(3, await context.Users.CountAsync());
    Assert.Equal(3, await context.Surveys.CountAsync());
    Assert.Equal(12, await context.Questions.CountAsync());
    Assert.Equal(5, await context.Responses.CountAsync());
    Assert.Equal(18, await context.Answers.CountAsync());
}
```

## Important Notes

1. **Development Only**: This seeder is intended for development and testing environments only. Do not use in production.

2. **PostgreSQL**: Designed for PostgreSQL with JSONB support for storing question options and answers.

3. **Migrations**: Ensure migrations are applied before seeding. The seeder calls `EnsureCreatedAsync()` but migrations are preferred.

4. **Idempotency**: The seeder checks for existing users and skips if found. For a complete reset, use `ResetAndSeedDatabaseAsync()`.

5. **Timestamps**: Seed data uses realistic timestamps relative to the current time, not fixed dates.

## Troubleshooting

### "Data already exists" message
This is normal. The seeder is idempotent and skips if data exists. Use `-Reset` flag if you need fresh data.

### Connection errors
Ensure PostgreSQL is running and connection string is correct.

### Missing tables
Run migrations first: `dotnet ef database update`

### Performance
Seeding should take less than 5 seconds. If slower, check database performance.

## Extending the Seeder

To add more sample data, edit the `DataSeeder.cs` file:

```csharp
private async Task SeedSurveysAsync()
{
    var creator = await _context.Users.FirstAsync();

    var surveys = new List<Survey>
    {
        CreateCustomerSatisfactionSurvey(creator.Id),
        CreateProductFeedbackSurvey(creator.Id),
        CreateEventRegistrationSurvey(creator.Id),
        // Add your custom survey here
        CreateMyCustomSurvey(creator.Id)
    };

    _context.Surveys.AddRange(surveys);
    await _context.SaveChangesAsync();
}
```

## API Reference

### DataSeeder Class

#### Constructor
```csharp
public DataSeeder(SurveyBotDbContext context, ILogger<DataSeeder> logger)
```

#### Methods
```csharp
// Main seeding method
public async Task SeedAsync()
```

### Extension Methods

```csharp
// Seed database (idempotent)
public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)

// Apply migrations
public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider)

// Drop, recreate, and seed (WARNING: destructive!)
public static async Task ResetAndSeedDatabaseAsync(this IServiceProvider serviceProvider)
```
