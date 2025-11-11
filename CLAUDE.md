# SurveyBot - Complete AI Assistant Documentation

**Last Updated**: 2025-11-12
**Version**: 1.2.0
**Target Framework**: .NET 8.0
**Status**: Active Development

---

## Table of Contents
- [Project Overview](#project-overview)
- [Critical Setup Information](#critical-setup-information)
- [Solution Architecture](#solution-architecture)
- [SurveyBot.Core - Domain Layer](#surveybotcore---domain-layer)
- [SurveyBot.Infrastructure - Data Access Layer](#surveybotinfrastructure---data-access-layer)
- [SurveyBot.Bot - Telegram Bot Layer](#surveybotbot---telegram-bot-layer)
- [SurveyBot.API - Presentation Layer](#surveybotapi---presentation-layer)
- [Database Schema](#database-schema)
- [Development Workflow](#development-workflow)
- [Testing](#testing)
- [Configuration](#configuration)
- [API Reference](#api-reference)
- [Common Patterns](#common-patterns)
- [Troubleshooting](#troubleshooting)

---

## Project Overview

**SurveyBot** is a comprehensive Telegram-based survey management system built with .NET 8.0 following Clean Architecture principles. The system enables users to create surveys via Telegram bot interface, distribute them to respondents, collect responses, and analyze results through both Telegram bot commands and REST API endpoints.

### Key Technologies
- **.NET 8.0** - Core framework
- **ASP.NET Core Web API** - REST API layer
- **Entity Framework Core 9.0** - ORM with PostgreSQL
- **PostgreSQL 15** - Primary database
- **Telegram.Bot 22.7.4** - Telegram Bot API integration
- **Serilog** - Structured logging with enrichment
- **AutoMapper 12.0** - Object-to-object mapping
- **xUnit** - Testing framework
- **JWT Bearer Authentication** - API security
- **Swashbuckle** - Swagger/OpenAPI documentation
- **Docker & Docker Compose** - PostgreSQL containerization

### Project Purpose
Enable survey creation, distribution, and response collection through Telegram bot interface with comprehensive analytics and management capabilities via REST API. The system provides:
- Telegram bot for creating and taking surveys
- REST API for programmatic access and admin panel integration
- Real-time response collection
- Comprehensive statistics and analytics
- Secure authentication and authorization
- Survey code sharing system (6-character alphanumeric codes)

---

## Critical Setup Information

### Prerequisites

**REQUIRED**:
1. **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download)
2. **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop) (for PostgreSQL)
3. **Telegram Bot Token** - Obtain from [@BotFather](https://t.me/botfather)

**OPTIONAL**:
- Visual Studio 2022 / VS Code / JetBrains Rider
- Git for version control
- pgAdmin (included in docker-compose.yml)

### Initial Setup Steps

#### 1. Clone Repository
```bash
git clone <repository-url>
cd SurveyBot
```

#### 2. Start PostgreSQL Database
```bash
docker-compose up -d
```

This starts:
- **PostgreSQL** on port 5432
  - Database: `surveybot_db`
  - User: `surveybot_user`
  - Password: `surveybot_dev_password`
- **pgAdmin** on port 5050 (optional)
  - Email: `admin@surveybot.local`
  - Password: `admin123`

**Verify Docker containers are running**:
```bash
docker ps
```

#### 3. Configure Telegram Bot Token

**CRITICAL**: There are TWO separate configuration files:

**File 1: `src/SurveyBot.API/appsettings.json`** (Base configuration)
- Contains default/production settings
- Committed to source control
- Should NOT contain real tokens in production

**File 2: `src/SurveyBot.API/appsettings.Development.json`** (Development overrides)
- Overrides settings from appsettings.json during development
- Can be gitignored or contain development-specific tokens
- Takes precedence when `ASPNETCORE_ENVIRONMENT=Development`

**Edit `src/SurveyBot.API/appsettings.Development.json`**:
```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "BotUsername": "@YourBotUsername",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000"
  }
}
```

**How to get a Telegram Bot Token**:
1. Open Telegram and search for [@BotFather](https://t.me/botfather)
2. Send `/newbot` command
3. Choose a name for your bot (e.g., "My Survey Bot")
4. Choose a username (must end in 'bot', e.g., "my_survey_bot")
5. Copy the API token (format: `1234567890:ABCdefGHIjklMNOpqrsTUVwxyz`)
6. Paste into `appsettings.Development.json`

#### 4. Apply Database Migrations
```bash
cd src/SurveyBot.API
dotnet ef database update
```

**If dotnet ef is not installed**:
```bash
dotnet tool install --global dotnet-ef
```

#### 5. Run the Application
```bash
dotnet run
```

**Expected output**:
```
[10:30:00 INF] Starting SurveyBot API application
[10:30:02 INF] Telegram Bot initialized successfully
[10:30:02 INF] SurveyBot API started successfully
Now listening on: http://localhost:5000
```

#### 6. Access the Application
- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **pgAdmin**: http://localhost:5050
- **Health Check**: http://localhost:5000/health/db

---

### Telegram Bot Configuration Modes

**CRITICAL DECISION**: The bot can operate in two modes:

#### Mode 1: Polling (Recommended for Local Development)

**Configuration**:
```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000"
  }
}
```

**How it works**:
- Bot actively polls Telegram servers for updates
- No public URL required
- Works with localhost
- Ideal for local development

**Pros**:
- Simple setup
- No HTTPS/SSL certificate required
- No port forwarding or tunneling
- Works on any machine

**Cons**:
- Slightly higher latency
- Not recommended for production

#### Mode 2: Webhook (Required for Production)

**Configuration**:
```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN",
    "UseWebhook": true,
    "WebhookUrl": "https://yourdomain.com",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "your_secret_key_here"
  }
}
```

**How it works**:
- Telegram sends updates directly to your server
- Requires publicly accessible HTTPS URL
- Real-time push notifications
- Production-ready

**Requirements**:
1. **Public HTTPS URL** - Telegram requires SSL/TLS
2. **Valid SSL certificate** - Self-signed certificates NOT accepted
3. **Webhook endpoint** - `https://yourdomain.com/api/bot/webhook`

**Local Development Workarounds**:

**Option A: ngrok (Recommended)**
```bash
# Install ngrok: https://ngrok.com/download
ngrok http 5000

# Copy the HTTPS URL (e.g., https://abc123.ngrok-free.app)
# Update appsettings.Development.json:
{
  "BotConfiguration": {
    "UseWebhook": true,
    "WebhookUrl": "https://abc123.ngrok-free.app"
  }
}
```

**Option B: localtunnel**
```bash
npx localtunnel --port 5000
```

**Option C: Visual Studio Dev Tunnels**
```bash
devtunnel create
devtunnel port create -p 5000
devtunnel host
```

**IMPORTANT**:
- Localhost URLs (http://localhost:5000) will NOT work with webhooks
- Telegram webhook endpoint: `POST /api/bot/webhook`
- Webhook requires secret token validation
- For local development WITHOUT public URL, use polling mode (`UseWebhook: false`)

---

## Solution Architecture

The solution follows Clean Architecture with clear separation of concerns and dependency inversion:

```
SurveyBot/
├── src/
│   ├── SurveyBot.Core/              # Domain Layer (No dependencies)
│   ├── SurveyBot.Infrastructure/    # Data Access Layer (depends on Core)
│   ├── SurveyBot.Bot/               # Telegram Bot Logic (depends on Core)
│   └── SurveyBot.API/               # Presentation Layer (depends on all)
├── tests/
│   └── SurveyBot.Tests/             # Unit and Integration Tests
├── frontend/                         # React Admin Panel
├── documentation/                    # Project documentation
├── .claude/                          # Claude commands and tasks
├── docker-compose.yml               # PostgreSQL and pgAdmin containers
└── SurveyBot.sln                    # Visual Studio Solution
```

### Dependency Flow
```
API → Infrastructure → Core
API → Bot → Core
Infrastructure → Core
Bot → Core
```

**Core Principle**: Core has zero dependencies. All other projects depend on Core.

### Project Descriptions

**SurveyBot.Core** (`src/SurveyBot.Core/`):
- Domain entities (User, Survey, Question, Response, Answer)
- Repository and service interfaces
- DTOs (Data Transfer Objects)
- Domain exceptions
- Business rules and validation
- Utilities (SurveyCodeGenerator)
- NO dependencies on other projects

**SurveyBot.Infrastructure** (`src/SurveyBot.Infrastructure/`):
- Entity Framework Core DbContext
- Repository implementations
- Service implementations (AuthService, SurveyService, etc.)
- Database migrations
- Data seeding
- Depends ONLY on Core

**SurveyBot.Bot** (`src/SurveyBot.Bot/`):
- Telegram bot client
- Message and callback handlers
- Conversation state management
- Bot commands (start, help, mysurveys, stats, etc.)
- Question handlers (text, single choice, multiple choice, rating)
- Depends ONLY on Core

**SurveyBot.API** (`src/SurveyBot.API/`):
- REST API controllers
- Authentication (JWT)
- Middleware (exception handling, logging)
- AutoMapper configuration
- Swagger/OpenAPI documentation
- Background task queue for webhooks
- Depends on Core, Infrastructure, and Bot

**SurveyBot.Tests** (`tests/SurveyBot.Tests/`):
- Unit tests
- Integration tests
- Repository tests
- Service tests
- API tests

---

## SurveyBot.Core - Domain Layer

### Overview
**Location**: `src/SurveyBot.Core/`

The Core project is the heart of the application containing all business entities, domain logic, interfaces (contracts), and DTOs. It has **zero dependencies** on external projects.

**Architecture Principle**: The Core project should never reference Infrastructure, Bot, or API projects. All dependencies point inward toward Core.

### Project Structure

```
SurveyBot.Core/
├── Entities/                    # Domain entities (business objects)
│   ├── BaseEntity.cs           # Base class with Id, CreatedAt, UpdatedAt
│   ├── User.cs                 # Telegram user
│   ├── Survey.cs               # Survey entity
│   ├── Question.cs             # Survey question
│   ├── Response.cs             # User's survey response
│   ├── Answer.cs               # Individual question answer
│   └── QuestionType.cs         # Question type enumeration
├── Interfaces/                  # Repository and service contracts
│   ├── IRepository.cs          # Generic repository base
│   ├── ISurveyRepository.cs    # Survey-specific repository
│   ├── IQuestionRepository.cs  # Question repository
│   ├── IResponseRepository.cs  # Response repository
│   ├── IUserRepository.cs      # User repository
│   ├── IAnswerRepository.cs    # Answer repository
│   ├── ISurveyService.cs       # Survey business logic
│   ├── IQuestionService.cs     # Question business logic
│   ├── IResponseService.cs     # Response business logic
│   ├── IUserService.cs         # User business logic
│   └── IAuthService.cs         # Authentication service
├── DTOs/                        # Data Transfer Objects
│   ├── Survey/                 # Survey DTOs
│   ├── Question/               # Question DTOs
│   ├── Response/               # Response DTOs
│   ├── Answer/                 # Answer DTOs
│   ├── User/                   # User DTOs
│   ├── Auth/                   # Authentication DTOs
│   ├── Statistics/             # Statistics DTOs
│   └── Common/                 # Common DTOs (PagedResultDto, etc.)
├── Exceptions/                  # Domain-specific exceptions
├── Configuration/               # Configuration models (JwtSettings)
├── Models/                      # Domain models (ValidationResult)
└── Utilities/                   # Domain utilities (SurveyCodeGenerator)
```

### Domain Entities

#### BaseEntity
**Location**: `Entities/BaseEntity.cs`

Abstract base class providing common properties for all entities.

**Properties**:
- `Id` (int, PK) - Unique identifier
- `CreatedAt` (DateTime, UTC) - Creation timestamp
- `UpdatedAt` (DateTime, UTC) - Last update timestamp

**Key Points**:
- All entities except Response and Answer inherit from BaseEntity
- Timestamps are automatically managed by DbContext.SaveChangesAsync override
- Default values set to DateTime.UtcNow

#### User
**Location**: `Entities/User.cs`

Represents a Telegram user in the system.

**Properties**:
- `TelegramId` (long, unique, required) - Telegram's unique user ID
- `Username` (string?, max 255) - Telegram username without @
- `FirstName` (string?, max 255) - User's first name
- `LastName` (string?, max 255) - User's last name
- `LastLoginAt` (DateTime?) - Last login timestamp

**Navigation Properties**:
- `Surveys` (ICollection<Survey>) - Surveys created by this user

**Key Points**:
- TelegramId is the external identifier from Telegram API
- Id is internal database primary key
- Username can be null (not all Telegram users have one)
- LastLoginAt is updated during authentication

#### Survey
**Location**: `Entities/Survey.cs`

Represents a survey with metadata and configuration.

**Properties**:
- `Title` (string, required, max 500) - Survey title
- `Description` (string?) - Survey description
- `Code` (string?, max 10, unique) - Unique survey code for sharing
- `CreatorId` (int, FK, required) - User who created the survey
- `IsActive` (bool, default: false) - Whether survey accepts responses
- `AllowMultipleResponses` (bool, default: false) - Allow duplicate responses from same user
- `ShowResults` (bool, default: true) - Show results to respondents

**Navigation Properties**:
- `Creator` (User) - User who created the survey
- `Questions` (ICollection<Question>) - Survey questions
- `Responses` (ICollection<Response>) - Survey responses

**Business Rules**:
- Must have at least one question to be activated
- Cannot modify if active with responses (requires deactivation first)
- Soft delete if has responses, hard delete otherwise
- Code is auto-generated and unique (6-char alphanumeric, uppercase)

#### Question
**Location**: `Entities/Question.cs`

Represents a question within a survey.

**Properties**:
- `SurveyId` (int, FK, required) - Parent survey ID
- `QuestionText` (string, required) - The question text
- `QuestionType` (QuestionType enum, required) - Type of question
- `OrderIndex` (int, 0-based, required) - Display order within survey
- `IsRequired` (bool, default: true) - Whether answer is required
- `OptionsJson` (string?, JSONB) - JSON array for choice-based questions

**Navigation Properties**:
- `Survey` (Survey) - Parent survey
- `Answers` (ICollection<Answer>) - All answers to this question

**QuestionType Enum** (`Entities/QuestionType.cs`):
- `Text` (0) - Free-form text answer
- `SingleChoice` (1) - Single selection from options (radio button)
- `MultipleChoice` (2) - Multiple selections from options (checkboxes)
- `Rating` (3) - Numeric rating (1-5 scale)

**OptionsJson Format**:

For choice-based questions (MultipleChoice, SingleChoice):
```json
["Option 1", "Option 2", "Option 3"]
```

#### Response
**Location**: `Entities/Response.cs`

Represents a user's response to a survey. Does NOT inherit from BaseEntity.

**Properties**:
- `Id` (int, PK) - Response ID
- `SurveyId` (int, FK, required) - Survey being responded to
- `RespondentTelegramId` (long, required) - Telegram ID of respondent (NOT a foreign key)
- `IsComplete` (bool, default: false) - Whether response is submitted
- `StartedAt` (DateTime?) - When user started the response
- `SubmittedAt` (DateTime?) - When response was submitted

**Navigation Properties**:
- `Survey` (Survey) - Survey this response belongs to
- `Answers` (ICollection<Answer>) - Answers in this response

**Important**: RespondentTelegramId is NOT a foreign key to allow anonymous responses and avoid dependency on User table.

**States**:
- **Started**: `StartedAt` set, `IsComplete = false`
- **Completed**: `SubmittedAt` set, `IsComplete = true`

#### Answer
**Location**: `Entities/Answer.cs`

Represents an answer to a specific question within a response. Does NOT inherit from BaseEntity.

**Properties**:
- `Id` (int, PK) - Answer ID
- `ResponseId` (int, FK, required) - Response this answer belongs to
- `QuestionId` (int, FK, required) - Question being answered
- `AnswerText` (string?) - Text answer for Text questions
- `AnswerJson` (string, JSONB, required) - JSON answer for complex question types
- `CreatedAt` (DateTime, required) - When answer was submitted

**Navigation Properties**:
- `Response` (Response) - Parent response
- `Question` (Question) - Question being answered

**AnswerJson Format by Question Type**:

**Text**:
```json
{"text": "User's answer"}
```

**SingleChoice**:
```json
{"selectedOption": "Option 2"}
```

**MultipleChoice**:
```json
{"selectedOptions": ["Option 1", "Option 3"]}
```

**Rating**:
```json
{"rating": 4}
```

### Repository Interfaces

**IRepository<T>** - Generic repository base:
```csharp
Task<T?> GetByIdAsync(int id);
Task<IEnumerable<T>> GetAllAsync();
Task<T> AddAsync(T entity);
Task<T> UpdateAsync(T entity);
Task DeleteAsync(int id);
Task<bool> ExistsAsync(int id);
```

**ISurveyRepository** - Survey-specific repository:
- `GetByIdWithQuestionsAsync(int id)` - Include questions
- `GetByIdWithDetailsAsync(int id)` - Include questions and responses
- `GetByCreatorIdAsync(int creatorId)` - User's surveys
- `GetActiveSurveysAsync()` - Only active surveys
- `ToggleActiveStatusAsync(int id)` - Toggle IsActive
- `SearchByTitleAsync(string searchTerm)` - Search by title (case-insensitive)
- `GetResponseCountAsync(int surveyId)` - Count responses
- `HasResponsesAsync(int surveyId)` - Check if has responses
- `GetByCodeAsync(string code)` - Find by unique code
- `GetByCodeWithQuestionsAsync(string code)` - Find by code with questions
- `CodeExistsAsync(string code)` - Check if code exists

### Service Interfaces

**ISurveyService** - Survey business logic:
- `CreateSurveyAsync(int userId, CreateSurveyDto dto)` - Create new survey
- `UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto)` - Update survey
- `DeleteSurveyAsync(int surveyId, int userId)` - Delete survey (soft/hard)
- `GetSurveyByIdAsync(int surveyId, int userId)` - Get survey details
- `GetAllSurveysAsync(int userId, PaginationQueryDto query)` - List with pagination
- `ActivateSurveyAsync(int surveyId, int userId)` - Activate survey
- `DeactivateSurveyAsync(int surveyId, int userId)` - Deactivate survey
- `GetSurveyStatisticsAsync(int surveyId, int userId)` - Get comprehensive statistics
- `UserOwnsSurveyAsync(int surveyId, int userId)` - Check ownership
- `GetSurveyByCodeAsync(string code)` - Get by unique code (public)

### Data Transfer Objects

**DTO Naming Conventions**:
- `CreateXxxDto` - For POST requests (creation)
- `UpdateXxxDto` - For PUT requests (updates)
- `XxxDto` - For responses (full details)
- `XxxListDto` - For list responses (summary data)

**SurveyDto** - Full details response:
```csharp
public class SurveyDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string? Code { get; set; }
    public int CreatorId { get; set; }
    public UserDto? Creator { get; set; }
    public bool IsActive { get; set; }
    public bool AllowMultipleResponses { get; set; }
    public bool ShowResults { get; set; }
    public List<QuestionDto> Questions { get; set; }
    public int TotalResponses { get; set; }
    public int CompletedResponses { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**PagedResultDto<T>** - Pagination wrapper:
```csharp
public class PagedResultDto<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
```

### Domain Exceptions

All custom exceptions inherit from `Exception` and are in `SurveyBot.Core.Exceptions`:
- `SurveyNotFoundException` - Survey with ID doesn't exist
- `QuestionNotFoundException` - Question with ID doesn't exist
- `ResponseNotFoundException` - Response with ID doesn't exist
- `SurveyValidationException` - Survey validation fails
- `QuestionValidationException` - Question validation fails
- `SurveyOperationException` - Operation cannot be performed
- `InvalidAnswerFormatException` - Answer JSON doesn't match question type
- `InvalidQuestionTypeException` - Invalid question type provided
- `DuplicateResponseException` - User tries to submit multiple responses when not allowed
- `UnauthorizedAccessException` - User tries to access/modify resource they don't own

### Utilities

**SurveyCodeGenerator** (`Utilities/SurveyCodeGenerator.cs`):

Generates unique, URL-safe, 6-character alphanumeric codes for surveys.

**Key Methods**:
- `GenerateCode()` - Generate random 6-character code (A-Z, 0-9)
- `GenerateUniqueCodeAsync(Func<string, Task<bool>> codeExistsAsync, int maxAttempts = 10)` - Generate unique code with collision checking
- `IsValidCode(string? code)` - Validate code format

**Features**:
- Uses cryptographically secure random number generator
- Base36 character set (A-Z, 0-9) - 2.17 billion combinations
- Collision detection with configurable max attempts
- Case-insensitive (stored as uppercase)

**Example**:
```csharp
var code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
    async (c) => await _surveyRepository.CodeExistsAsync(c)
);
// Returns: "A3B5K9" (example)
```

---

## SurveyBot.Infrastructure - Data Access Layer

### Overview
**Location**: `src/SurveyBot.Infrastructure/`

The Infrastructure project implements data access, database operations, and business logic services. This layer depends only on **SurveyBot.Core**.

### Project Structure

```
SurveyBot.Infrastructure/
├── Data/
│   ├── SurveyBotDbContext.cs           # Main DbContext
│   ├── DataSeeder.cs                   # Test data seeding
│   ├── Configurations/                 # Entity configurations (Fluent API)
│   │   ├── UserConfiguration.cs
│   │   ├── SurveyConfiguration.cs
│   │   ├── QuestionConfiguration.cs
│   │   ├── ResponseConfiguration.cs
│   │   └── AnswerConfiguration.cs
│   └── Extensions/
│       └── DatabaseExtensions.cs       # Seeding extensions
├── Repositories/                       # Repository implementations
│   ├── GenericRepository.cs            # Base repository
│   ├── SurveyRepository.cs
│   ├── QuestionRepository.cs
│   ├── ResponseRepository.cs
│   ├── UserRepository.cs
│   └── AnswerRepository.cs
├── Services/                           # Business logic services
│   ├── AuthService.cs                  # JWT authentication
│   ├── SurveyService.cs
│   ├── QuestionService.cs
│   ├── ResponseService.cs
│   └── UserService.cs
├── Migrations/                         # EF Core migrations
│   ├── 20251105190107_InitialCreate.cs
│   ├── 20251106000001_AddLastLoginAtToUser.cs
│   ├── 20251109000001_AddSurveyCodeColumn.cs
│   └── SurveyBotDbContextModelSnapshot.cs
└── DependencyInjection.cs
```

### Database Context

**SurveyBotDbContext** (`Data/SurveyBotDbContext.cs`):

**Key Features**:
1. Automatic timestamp management (`CreatedAt`, `UpdatedAt`)
2. Entity configurations via Fluent API
3. PostgreSQL-specific features (JSONB)
4. Development-mode detailed logging

**DbSets**:
```csharp
public DbSet<User> Users { get; set; }
public DbSet<Survey> Surveys { get; set; }
public DbSet<Question> Questions { get; set; }
public DbSet<Response> Responses { get; set; }
public DbSet<Answer> Answers { get; set; }
```

**SaveChangesAsync Override**:
```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var entries = ChangeTracker.Entries()
        .Where(e => e.Entity is BaseEntity &&
                   (e.State == EntityState.Added || e.State == EntityState.Modified));

    foreach (var entry in entries)
    {
        var entity = (BaseEntity)entry.Entity;

        if (entry.State == EntityState.Added)
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
        }
        else if (entry.State == EntityState.Modified)
        {
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }

    return base.SaveChangesAsync(cancellationToken);
}
```

### Repository Implementations

**SurveyRepository** (`Repositories/SurveyRepository.cs`):

Key Methods:
- `GetByIdWithQuestionsAsync` - Include questions ordered by OrderIndex
- `GetByIdWithDetailsAsync` - Include questions, responses, and answers
- `GetActiveSurveysAsync` - Only active surveys
- `SearchByTitleAsync` - PostgreSQL case-insensitive LIKE search
- `GetByCodeWithQuestionsAsync` - Find by code with questions
- `CodeExistsAsync` - Check code existence

**Example**:
```csharp
public async Task<Survey?> GetByIdWithQuestionsAsync(int id)
{
    return await _dbSet
        .AsNoTracking()
        .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
        .Include(s => s.Creator)
        .FirstOrDefaultAsync(s => s.Id == id);
}
```

### Service Implementations

**SurveyService** (`Services/SurveyService.cs`):

**Key Methods**:

1. **CreateSurveyAsync**:
   - Validates DTO
   - Maps to Survey entity
   - Sets CreatorId and IsActive=false
   - Generates unique survey code using `SurveyCodeGenerator`
   - Saves to database
   - Returns mapped SurveyDto

2. **UpdateSurveyAsync**:
   - Gets survey with questions
   - Checks authorization (user owns survey)
   - Prevents modification if active with responses
   - Updates properties
   - Returns updated SurveyDto

3. **ActivateSurveyAsync**:
   - Checks authorization
   - Validates survey has at least one question
   - Sets IsActive=true
   - Returns updated SurveyDto

4. **GetSurveyStatisticsAsync**:
   - Gets survey with questions and responses
   - Calculates basic statistics (total, completed, completion rate)
   - Calculates average completion time
   - Calculates question-level statistics
   - Returns comprehensive SurveyStatisticsDto

5. **GetSurveyByCodeAsync**:
   - Validates code format
   - Gets survey by code with questions
   - Only returns if active (for public access)
   - Returns SurveyDto

### Database Migrations

**Commands** (from `SurveyBot.API` directory):
```bash
# Add migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script

# Rollback to specific migration
dotnet ef database update PreviousMigrationName
```

**Existing Migrations**:
- **InitialCreate** (20251105190107) - Creates all tables
- **AddLastLoginAtToUser** (20251106000001) - Adds LastLoginAt column
- **AddSurveyCodeColumn** (20251109000001) - Adds Code column with unique index

---

## SurveyBot.Bot - Telegram Bot Layer

### Overview
**Location**: `src/SurveyBot.Bot/`

The Bot project implements Telegram bot logic and handles all interactions with Telegram's Bot API.

**Dependencies**: SurveyBot.Core, Telegram.Bot 22.7.4

### Project Structure

```
SurveyBot.Bot/
├── Configuration/
│   └── BotConfiguration.cs             # Bot settings model
├── Services/
│   ├── BotService.cs                   # Bot client lifecycle
│   ├── UpdateHandler.cs                # Process incoming updates
│   ├── CommandRouter.cs                # Route commands to handlers
│   ├── ConversationStateManager.cs     # Manage conversation state
│   ├── AdminAuthService.cs
│   ├── ApiErrorHandler.cs
│   ├── BotPerformanceMonitor.cs
│   ├── QuestionErrorHandler.cs
│   └── SurveyCache.cs
├── Handlers/
│   ├── Commands/                       # Command handler implementations
│   │   ├── StartCommandHandler.cs
│   │   ├── HelpCommandHandler.cs
│   │   ├── MySurveysCommandHandler.cs
│   │   ├── SurveysCommandHandler.cs
│   │   ├── StatsCommandHandler.cs
│   │   └── ...
│   ├── Questions/                      # Question type handlers
│   │   ├── TextQuestionHandler.cs
│   │   ├── SingleChoiceQuestionHandler.cs
│   │   ├── MultipleChoiceQuestionHandler.cs
│   │   └── RatingQuestionHandler.cs
│   ├── NavigationHandler.cs
│   └── CancelCallbackHandler.cs
├── Interfaces/
│   ├── IBotService.cs
│   ├── IUpdateHandler.cs
│   ├── ICommandHandler.cs
│   ├── IQuestionHandler.cs
│   ├── IConversationStateManager.cs
│   ├── IAdminAuthService.cs
│   └── IAnswerValidator.cs
├── Models/
│   ├── ConversationState.cs
│   └── ApiResponse.cs
├── Validators/
│   └── AnswerValidator.cs
└── Extensions/
    ├── ServiceCollectionExtensions.cs
    └── BotServiceExtensions.cs
```

### Configuration

**BotConfiguration** (`Configuration/BotConfiguration.cs`):
```json
{
  "BotConfiguration": {
    "BotToken": "your-bot-token",
    "BotUsername": "@YourBot",
    "UseWebhook": false,
    "WebhookUrl": "https://yourdomain.com",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "your-secret",
    "MaxConnections": 40,
    "ApiBaseUrl": "http://localhost:5000",
    "RequestTimeout": 30,
    "AdminUserIds": [123456789]
  }
}
```

### Core Services

**UpdateHandler** (`Services/UpdateHandler.cs`):

Routes updates to appropriate handlers:
- `UpdateType.Message` → HandleMessageAsync
- `UpdateType.CallbackQuery` → HandleCallbackQueryAsync
- `UpdateType.EditedMessage` → HandleEditedMessageAsync

**Callback Data Formats**:
- `cmd:commandName` - Execute command from inline button
- `nav_back_q{questionId}` - Go back to previous question
- `nav_skip_q{questionId}` - Skip optional question
- `cancel_confirm` / `cancel_dismiss` - Cancel confirmation
- `listsurveys:page:{pageNumber}` - Pagination

**ConversationStateManager** (`Services/ConversationStateManager.cs`):

Manages conversation state for Telegram bot users:
- In-memory storage with `ConcurrentDictionary`
- Automatic cleanup of expired states (30 min inactivity)
- Thread-safe state transitions with `SemaphoreSlim`
- Survey progress tracking

**Key Methods**:
- `StartSurveyAsync(userId, surveyId, responseId, totalQuestions)` - Initialize survey session
- `NextQuestionAsync(userId)` - Move to next question
- `PreviousQuestionAsync(userId)` - Move to previous question
- `CompleteSurveyAsync(userId)` - Mark survey as complete
- `CancelSurveyAsync(userId)` - Cancel and clear survey data

**ConversationState Model** (`Models/ConversationState.cs`):

Properties:
- `CurrentSurveyId`, `CurrentResponseId`, `CurrentQuestionIndex`
- `TotalQuestions`, `AnsweredQuestionIndices`, `CachedAnswers`
- `IsExpired`, `ProgressPercent`, `IsAllAnswered`

**ConversationStateType Enum**:
- `Idle`, `WaitingSurveySelection`, `InSurvey`, `AnsweringQuestion`
- `ResponseComplete`, `SessionExpired`, `Cancelled`

### Command Handlers

**StatsCommandHandler** (`Handlers/Commands/StatsCommandHandler.cs`):

**Command**: `/stats <survey_id>`
**Purpose**: Display comprehensive statistics (Admin only)

Output includes:
- Survey title, ID, status
- Total/completed/incomplete responses
- Completion rate, unique respondents
- Average completion time
- Top 3 questions by response count
- Creation date, first/last response timestamps

### Webhook vs Polling

**Webhook Mode (Production)**:
- Real-time updates from Telegram
- HTTPS required with valid SSL certificate
- Set `UseWebhook: true`
- Requires public URL

**Polling Mode (Development)**:
- Bot actively polls Telegram API
- No HTTPS or public URL required
- Set `UseWebhook: false`
- Recommended for local development

---

## SurveyBot.API - Presentation Layer

### Overview
**Location**: `src/SurveyBot.API/`

The API project exposes REST API endpoints and serves as the entry point.

### Project Structure

```
SurveyBot.API/
├── Controllers/                # API controllers
│   ├── AuthController.cs
│   ├── SurveysController.cs
│   ├── QuestionsController.cs
│   ├── ResponsesController.cs
│   ├── UsersController.cs
│   ├── BotController.cs
│   └── HealthController.cs
├── Middleware/
│   ├── GlobalExceptionMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── Models/
│   ├── ApiResponse.cs          # Standard response wrapper
│   └── ErrorResponse.cs
├── Mapping/                    # AutoMapper profiles
│   ├── SurveyMappingProfile.cs
│   ├── QuestionMappingProfile.cs
│   └── ValueResolvers/
├── Extensions/                 # Service registration
├── Services/                   # Background services
│   ├── BackgroundTaskQueue.cs
│   └── QueuedHostedService.cs
├── Exceptions/                 # API-specific exceptions
├── Program.cs                  # Application entry point
├── appsettings.json            # Base configuration
└── appsettings.Development.json # Development overrides
```

### Program.cs - Application Entry Point

**Location**: `Program.cs`

The `Program.cs` file is the heart of the application. It configures all services, middleware, and starts the application.

**Service Registration Order**:
1. Controllers
2. AutoMapper
3. DbContext with PostgreSQL
4. JWT Settings
5. Authentication & Authorization
6. Repositories (Scoped)
7. Services (Scoped)
8. Telegram Bot Services
9. Background Task Queue
10. Health Checks
11. Swagger

**Middleware Pipeline Order** (CRITICAL):
1. Serilog request logging
2. Custom request logging
3. Global exception handler (MUST BE EARLY)
4. Swagger (Development only)
5. HTTPS redirection
6. Authentication (MUST come before Authorization)
7. Authorization
8. Map Controllers
9. Health Check Endpoint

### API Response Pattern

**ApiResponse<T>** (`Models/ApiResponse.cs`):
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; }
    public T? Data { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
```

**Usage**:
```csharp
return Ok(ApiResponse<SurveyDto>.Ok(survey, "Survey created successfully"));
```

**JSON Output**:
```json
{
  "success": true,
  "data": {
    "id": 1,
    "title": "Customer Satisfaction Survey"
  },
  "message": "Survey created successfully"
}
```

### Authentication

**JWT Configuration**:
```json
{
  "JwtSettings": {
    "SecretKey": "min-32-characters",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Clients",
    "TokenLifetimeHours": 24
  }
}
```

**IMPORTANT**: Secret key must be at least 32 characters for HS256 algorithm

### Middleware

**GlobalExceptionMiddleware**:
Maps domain exceptions to HTTP status codes:
- `SurveyNotFoundException` → 404 Not Found
- `SurveyValidationException` → 400 Bad Request
- `DuplicateResponseException` → 409 Conflict
- `UnauthorizedAccessException` → 403 Forbidden

---

## Database Schema

### Entity Relationships

```
User (1) ──creates──> Surveys (*)
Survey (1) ──contains──> Questions (*)
Survey (1) ──receives──> Responses (*)
Question (1) ──answered by──> Answers (*)
Response (1) ──contains──> Answers (*)
```

### Tables

**users**: id, telegram_id (unique), username, first_name, last_name, last_login_at, created_at, updated_at

**surveys**: id, title, description, code (unique), creator_id (FK), is_active, allow_multiple_responses, show_results, created_at, updated_at

**questions**: id, survey_id (FK), question_text, question_type, order_index, is_required, options_json (JSONB), created_at, updated_at

**responses**: id, survey_id (FK), respondent_telegram_id (NOT FK), is_complete, started_at, submitted_at

**answers**: id, response_id (FK), question_id (FK), answer_text, answer_json (JSONB), created_at

### Cascade Delete

- Delete Survey → Questions cascade delete
- Delete Survey → Responses cascade delete
- Delete Question → Answers cascade delete
- Delete Response → Answers cascade delete

---

## Development Workflow

### Initial Setup

1. Install .NET 8.0 SDK, Docker Desktop
2. Clone repository
3. Start Docker: `docker-compose up -d`
4. Configure bot token in `appsettings.Development.json`
5. Apply migrations: `cd src/SurveyBot.API && dotnet ef database update`
6. Run: `dotnet run`
7. Access Swagger: http://localhost:5000/swagger

### Database Migrations

```bash
cd src/SurveyBot.API
dotnet ef migrations add MigrationName
dotnet ef database update
dotnet ef migrations remove
dotnet ef migrations script
```

### Running the Application

```bash
# Development mode
cd src/SurveyBot.API
dotnet run

# Watch mode (auto-restart)
dotnet watch run

# Specific environment
dotnet run --environment Production
```

---

## Testing

### Running Tests

```bash
dotnet test
dotnet test /p:CollectCoverage=true
dotnet test --filter "FullyQualifiedName~SurveyServiceTests"
dotnet test --logger "console;verbosity=detailed"
```

### Test Organization

**Location**: `tests/SurveyBot.Tests/`

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions and database operations
- **API Tests**: Test HTTP endpoints and responses

---

## Configuration

### appsettings.json (Base Configuration)

**Location**: `src/SurveyBot.API/appsettings.json`

This file contains the base configuration for all environments. Settings here are used unless overridden by environment-specific files.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=surveybot_db;User Id=surveybot_user;Password=surveybot_dev_password;"
  },
  "JwtSettings": {
    "SecretKey": "SurveyBot-Super-Secret-Key-For-JWT-Token-Generation-2025-Change-In-Production",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Clients",
    "TokenLifetimeHours": 24,
    "RefreshTokenLifetimeDays": 7
  },
  "BotConfiguration": {
    "BotToken": "",
    "BotUsername": "",
    "UseWebhook": false,
    "WebhookUrl": "",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "",
    "MaxConnections": 40,
    "ApiBaseUrl": "http://localhost:5000",
    "RequestTimeout": 30,
    "AdminUserIds": []
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/surveybot-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

### appsettings.Development.json (Development Overrides)

**Location**: `src/SurveyBot.API/appsettings.Development.json`

**CRITICAL**: This file OVERRIDES settings from appsettings.json when `ASPNETCORE_ENVIRONMENT=Development`

```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "BotUsername": "@YourBotUsername",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information"
      }
    }
  }
}
```

**How Configuration Merging Works**:
1. ASP.NET Core loads `appsettings.json` first (base configuration)
2. Then loads `appsettings.{Environment}.json` (e.g., `appsettings.Development.json`)
3. Settings in environment-specific file OVERRIDE base settings
4. Non-specified settings in environment file use base configuration values

**Example**:
- `BotToken` from appsettings.Development.json overrides empty value in appsettings.json
- `ConnectionStrings` NOT specified in appsettings.Development.json, so uses appsettings.json value
- `Serilog.MinimumLevel` from appsettings.Development.json overrides appsettings.json

**Environment Variable**:
```bash
# Windows (PowerShell)
$env:ASPNETCORE_ENVIRONMENT="Development"

# Windows (CMD)
set ASPNETCORE_ENVIRONMENT=Development

# Linux/Mac
export ASPNETCORE_ENVIRONMENT=Development
```

---

## API Reference

### Authentication

**POST /api/auth/login**: Login with Telegram credentials, get JWT token
**POST /api/auth/refresh**: Refresh JWT token
**GET /api/auth/validate**: Validate current token
**POST /api/auth/register**: Register/update user
**GET /api/auth/me**: Get current user from token

### Surveys

**POST /api/surveys**: Create survey (auth required)
**GET /api/surveys**: List surveys (paginated, auth required)
**GET /api/surveys/{id}**: Get survey details (auth required)
**PUT /api/surveys/{id}**: Update survey (auth required)
**DELETE /api/surveys/{id}**: Delete survey (auth required)
**POST /api/surveys/{id}/activate**: Activate survey (auth required)
**POST /api/surveys/{id}/deactivate**: Deactivate survey (auth required)
**GET /api/surveys/{id}/statistics**: Get statistics (auth required)
**GET /api/surveys/code/{code}**: Get by code (PUBLIC, no auth)

### Questions

**POST /api/surveys/{surveyId}/questions**: Add question (auth required)
**GET /api/surveys/{surveyId}/questions**: List questions (public for active surveys)
**PUT /api/questions/{id}**: Update question (auth required)
**DELETE /api/questions/{id}**: Delete question (auth required)
**POST /api/surveys/{surveyId}/questions/reorder**: Reorder questions (auth required)

### Responses

**POST /api/surveys/{surveyId}/responses**: Start response (PUBLIC, no auth)
**POST /api/responses/{responseId}/answers**: Submit answer (PUBLIC, no auth)
**POST /api/responses/{responseId}/complete**: Complete response (PUBLIC, no auth)
**GET /api/surveys/{surveyId}/responses**: List responses (auth required)
**GET /api/responses/{id}**: Get response details (auth required)

### Bot

**POST /api/bot/webhook**: Telegram webhook (secret validation)
**GET /api/bot/status**: Bot status
**GET /api/bot/health**: Bot health check

### Health

**GET /health/db**: Database health check
**GET /health/db/details**: Detailed database health
**GET /health**: Basic health
**GET /health/ready**: Readiness check
**GET /health/live**: Liveness check

---

## Common Patterns

### Repository Pattern

```csharp
public async Task<Survey?> GetByIdWithQuestionsAsync(int id)
{
    return await _dbSet
        .AsNoTracking()
        .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
        .Include(s => s.Creator)
        .FirstOrDefaultAsync(s => s.Id == id);
}
```

### Service Pattern

```csharp
public async Task<SurveyDto> GetSurveyAsync(int surveyId, int userId)
{
    var survey = await _repository.GetByIdAsync(surveyId);
    if (survey == null)
        throw new SurveyNotFoundException(surveyId);
    if (survey.CreatorId != userId)
        throw new UnauthorizedAccessException();
    return _mapper.Map<SurveyDto>(survey);
}
```

### Controller Pattern

```csharp
[HttpPost]
[Authorize]
public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateSurvey([FromBody] CreateSurveyDto dto)
{
    try
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid data" });

        var userId = GetUserIdFromClaims();
        var survey = await _surveyService.CreateSurveyAsync(userId, dto);

        return CreatedAtAction(
            nameof(GetSurveyById),
            new { id = survey.Id },
            ApiResponse<SurveyDto>.Ok(survey, "Survey created successfully"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating survey");
        return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
    }
}
```

---

## Troubleshooting

### Database Connection Issues

**Problem**: Cannot connect to PostgreSQL

**Solutions**:
1. Check Docker is running: `docker ps`
2. Verify connection string in appsettings.json
3. Ensure PostgreSQL container is healthy: `docker-compose logs postgres`
4. Test connection: `docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db`
5. Restart containers: `docker-compose restart`

### Migration Issues

**Problem**: Migrations fail to apply

**Solutions**:
1. Ensure you're in the correct directory: `cd src/SurveyBot.API`
2. Check EF Core tools are installed: `dotnet tool install --global dotnet-ef`
3. Verify DbContext can be found: `dotnet ef dbcontext info`
4. Check for compilation errors: `dotnet build`
5. Try removing and re-adding migration
6. Ensure database is accessible

### Bot Not Responding

**Problem**: Bot doesn't respond to messages

**Solutions**:
1. **Verify bot token is correct** in `appsettings.Development.json`
2. Check bot initialization logs: Look for "Telegram Bot initialized successfully"
3. **Ensure `UseWebhook: false`** for local development
4. Test bot token with Telegram API: `https://api.telegram.org/bot<TOKEN>/getMe`
5. Check Serilog console output for errors
6. Verify bot username is correct
7. Try sending `/start` command to bot

### Webhook Issues

**Problem**: Webhook not receiving updates

**Solutions**:
1. **Verify you have a PUBLIC HTTPS URL** (not localhost)
2. Check webhook is set correctly: `https://api.telegram.org/bot<TOKEN>/getWebhookInfo`
3. **Ensure SSL certificate is valid** (not self-signed)
4. Verify webhook secret matches configuration
5. Check webhook endpoint is accessible: `POST https://yourdomain.com/api/bot/webhook`
6. **For local development, use polling mode** (`UseWebhook: false`) or ngrok
7. Clear webhook if switching to polling: `https://api.telegram.org/bot<TOKEN>/deleteWebhook`

### JWT Authentication Issues

**Problem**: 401 Unauthorized errors

**Solutions**:
1. Verify SecretKey is at least 32 characters
2. Check token expiration (default 24 hours)
3. Ensure Issuer and Audience match in token and configuration
4. Verify Authorization header format: `Bearer <token>`
5. Check ClockSkew is set (default is 5 minutes)
6. Test with Swagger UI authentication

### Configuration Issues

**Problem**: Settings not applying correctly

**Solutions**:
1. **Check which appsettings file is being used**:
   - Development environment uses `appsettings.Development.json` which OVERRIDES `appsettings.json`
   - Production uses `appsettings.json` only
2. Verify `ASPNETCORE_ENVIRONMENT` variable is set correctly
3. Check JSON syntax is valid (no trailing commas, proper quotes)
4. Ensure property names match exactly (case-sensitive)
5. Restart application after config changes
6. Check logs for configuration loading errors

### Swagger Not Accessible

**Problem**: Swagger UI not loading

**Solutions**:
1. Ensure app is running in Development mode
2. Navigate to: http://localhost:5000/swagger (not /swagger/index.html)
3. Check Swagger is configured in Program.cs
4. Verify port number (default 5000, check launchSettings.json)
5. Check for HTTPS redirection issues

### Common Error Messages

**"The ConnectionString property has not been initialized"**
- Connection string not configured in appsettings.json
- Check `ConnectionStrings.DefaultConnection`
- Verify JSON syntax is correct

**"A network-related or instance-specific error occurred"**
- PostgreSQL not running
- Run `docker-compose up -d`
- Check firewall settings

**"No service for type 'SurveyBot.Infrastructure.Data.SurveyBotDbContext'"**
- DbContext not registered in DI
- Check Program.cs service registration
- Ensure Infrastructure project is referenced

**"The secret key must be at least 32 characters"**
- JWT SecretKey too short
- Update JwtSettings.SecretKey in appsettings.json to at least 32 characters

**"Webhook certificate verification failed"**
- Invalid SSL certificate for webhook
- Use polling mode for development (`UseWebhook: false`)
- Or use ngrok to get valid HTTPS URL

**"Cannot find migration '...'"**
- Migration files corrupted or missing
- Check Migrations folder in Infrastructure project
- Try removing and re-creating migrations

---

## Additional Resources

### Documentation Files

- **README.md** - Project overview and quick start
- **DOCKER-STARTUP-GUIDE.md** - Docker setup guide
- **DI-STRUCTURE.md** - Dependency injection structure
- **documentation/** - Comprehensive documentation folder

### Access Points

- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **pgAdmin**: http://localhost:5050
  - Email: admin@surveybot.local
  - Password: admin123
- **PostgreSQL**: localhost:5432
  - Database: surveybot_db
  - User: surveybot_user
  - Password: surveybot_dev_password

### Useful Commands

```bash
# Build solution
dotnet build

# Restore packages
dotnet restore

# Run tests
dotnet test

# Run application
cd src/SurveyBot.API
dotnet run

# Watch mode (auto-restart on file changes)
dotnet watch run

# Database migrations
dotnet ef migrations add MigrationName
dotnet ef database update
dotnet ef database drop --force
dotnet ef migrations remove

# Docker
docker-compose up -d
docker-compose down
docker-compose logs -f postgres
docker ps
docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db

# Check bot token (replace <TOKEN>)
curl https://api.telegram.org/bot<TOKEN>/getMe

# Check webhook status
curl https://api.telegram.org/bot<TOKEN>/getWebhookInfo

# Delete webhook (switch to polling)
curl https://api.telegram.org/bot<TOKEN>/deleteWebhook
```

---

**End of Documentation**

**Last Updated**: 2025-11-12
**Version**: 1.2.0
**Target Framework**: .NET 8.0
**Status**: Active Development

---

## Summary for AI Assistants

This is a .NET 8.0 Telegram survey bot application following Clean Architecture principles. Key points:

1. **Configuration Files**: There are TWO separate appsettings files - appsettings.json (base) and appsettings.Development.json (overrides for development)

2. **Telegram Bot Modes**:
   - **Polling** (recommended for local dev): No public URL needed, set `UseWebhook: false`
   - **Webhook** (production): Requires PUBLIC HTTPS URL, cannot use localhost

3. **Architecture**: Clean Architecture with four layers (Core, Infrastructure, Bot, API), Core has zero dependencies

4. **Database**: PostgreSQL via Docker, migrations managed with EF Core

5. **Key Technologies**: .NET 8.0, EF Core 9.0, Telegram.Bot 22.7.4, JWT Auth, Serilog, AutoMapper, Swagger

6. **File Paths**: All absolute paths, e.g., `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\appsettings.json`
