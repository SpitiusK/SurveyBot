# SurveyBot - Complete AI Assistant Documentation

**Last Updated**: 2025-11-10
**Version**: 1.1.0-Enhanced
**Target Framework**: .NET 8.0
**Total Source Files Documented**: 184

---

## Table of Contents
- [Project Overview](#project-overview)
- [Solution Architecture](#solution-architecture)
- [SurveyBot.Core - Domain Layer](#surveybot-core---domain-layer)
- [SurveyBot.Infrastructure - Data Access Layer](#surveybot-infrastructure---data-access-layer)
- [SurveyBot.Bot - Telegram Bot Layer](#surveybot-bot---telegram-bot-layer)
- [SurveyBot.API - Presentation Layer](#surveybot-api---presentation-layer)
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

### Project Purpose
Enable survey creation, distribution, and response collection through Telegram bot interface with comprehensive analytics and management capabilities via REST API. The system provides:
- Telegram bot for creating and taking surveys
- REST API for programmatic access
- Real-time response collection
- Comprehensive statistics and analytics
- Secure authentication and authorization
- Survey code sharing system

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
├── documentation/                    # Project documentation
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
**Location**: `Entities/BaseEntity.cs` (Lines 1-23)

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
**Location**: `Entities/User.cs` (Lines 1-48)

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
**Location**: `Entities/Survey.cs` (Lines 1-69)

Represents a survey with metadata and configuration.

**Properties**:
- `Title` (string, required, max 500) - Survey title
- `Description` (string?) - Survey description
- `Code` (string?, max 10) - Unique survey code for sharing
- `CreatorId` (int, FK, required) - User who created the survey
- `IsActive` (bool, default: true) - Whether survey accepts responses
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
- Code is auto-generated and unique (6-char alphanumeric)

#### Question
**Location**: `Entities/Question.cs` (Lines 1-59)

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

**QuestionType Enum** (`Entities/QuestionType.cs`, Lines 1-28):
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
**Location**: `Entities/Response.cs` (Lines 1-56)

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
**Location**: `Entities/Answer.cs` (Lines 1-56)

Represents an answer to a specific question within a response. Does NOT inherit from BaseEntity.

**Properties**:
- `Id` (int, PK) - Answer ID
- `ResponseId` (int, FK, required) - Response this answer belongs to
- `QuestionId` (int, FK, required) - Question being answered
- `AnswerText` (string?) - Text answer for Text questions
- `AnswerJson` (string, JSONB) - JSON answer for complex question types
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

**ISurveyRepository** - Survey-specific repository (`Interfaces/ISurveyRepository.cs`, Lines 1-86):
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

**ISurveyService** - Survey business logic (`Interfaces/ISurveyService.cs`, Lines 1-109):
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

**SurveyDto** - Full details response (`DTOs/Survey/SurveyDto.cs`, Lines 1-81):
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

**SurveyCodeGenerator** (`Utilities/SurveyCodeGenerator.cs`, Lines 1-73):

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

**SurveyBotDbContext** (`Data/SurveyBotDbContext.cs`, Lines 1-109):

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

**SaveChangesAsync Override** (Lines 70-107):
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

**SurveyRepository** (`Repositories/SurveyRepository.cs`, Lines 1-169):

Key Methods:
- `GetByIdWithQuestionsAsync` (Lines 22-29) - Include questions ordered by OrderIndex
- `GetByIdWithDetailsAsync` (Lines 32-40) - Include questions, responses, and answers
- `GetActiveSurveysAsync` (Lines 54-63) - Only active surveys
- `SearchByTitleAsync` (Lines 82-95) - PostgreSQL case-insensitive LIKE search
- `GetByCodeWithQuestionsAsync` (Lines 144-156) - Find by code with questions
- `CodeExistsAsync` (Lines 159-167) - Check code existence

### Service Implementations

**SurveyService** (`Services/SurveyService.cs`, Lines 1-724):

**Key Methods**:

1. **CreateSurveyAsync** (Lines 44-74):
   - Validates DTO
   - Maps to Survey entity
   - Sets CreatorId and IsActive=false
   - Generates unique survey code using `SurveyCodeGenerator`
   - Saves to database
   - Returns mapped SurveyDto

2. **UpdateSurveyAsync** (Lines 77-127):
   - Gets survey with questions
   - Checks authorization (user owns survey)
   - Prevents modification if active with responses
   - Updates properties
   - Returns updated SurveyDto

3. **ActivateSurveyAsync** (Lines 260-306):
   - Checks authorization
   - Validates survey has at least one question
   - Sets IsActive=true
   - Returns updated SurveyDto

4. **GetSurveyStatisticsAsync** (Lines 350-410):
   - Gets survey with questions and responses
   - Calculates basic statistics (total, completed, completion rate)
   - Calculates average completion time
   - Calculates question-level statistics
   - Returns comprehensive SurveyStatisticsDto

5. **GetSurveyByCodeAsync** (Lines 420-458):
   - Validates code format
   - Gets survey by code with questions
   - Only returns if active (for public access)
   - Returns SurveyDto

**Private Helper Methods**:
- `CalculateQuestionStatisticsAsync` (Lines 512-560) - Question-level statistics
- `CalculateChoiceDistribution` (Lines 565-644) - Choice question distribution
- `CalculateRatingStatistics` (Lines 649-696) - Rating statistics
- `CalculateTextStatistics` (Lines 701-720) - Text answer statistics

### Database Migrations

**Commands** (from `SurveyBot.API` directory):
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
dotnet ef migrations remove
dotnet ef migrations script
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
│   │   ├── StatsCommandHandler.cs      # (Lines 1-239)
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
│   ├── ConversationState.cs            # (Lines 1-241)
│   └── ApiResponse.cs
├── Validators/
│   └── AnswerValidator.cs
└── Extensions/
    ├── ServiceCollectionExtensions.cs
    └── BotServiceExtensions.cs
```

### Configuration

**BotConfiguration** (`Configuration/BotConfiguration.cs`, Lines 1-122):
```json
{
  "BotConfiguration": {
    "BotToken": "your-bot-token",
    "WebhookUrl": "https://yourdomain.com",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "your-secret",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000",
    "MaxConnections": 40,
    "AdminUserIds": [123456789]
  }
}
```

### Core Services

**UpdateHandler** (`Services/UpdateHandler.cs`, Lines 1-505):

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

**ConversationStateManager** (`Services/ConversationStateManager.cs`, Lines 1-544):

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

**ConversationState Model** (`Models/ConversationState.cs`, Lines 1-241):

Properties:
- `CurrentSurveyId`, `CurrentResponseId`, `CurrentQuestionIndex`
- `TotalQuestions`, `AnsweredQuestionIndices`, `CachedAnswers`
- `IsExpired`, `ProgressPercent`, `IsAllAnswered`

**ConversationStateType Enum** (Lines 197-240):
- `Idle`, `WaitingSurveySelection`, `InSurvey`, `AnsweringQuestion`
- `ResponseComplete`, `SessionExpired`, `Cancelled`

### Command Handlers

**StatsCommandHandler** (`Handlers/Commands/StatsCommandHandler.cs`, Lines 1-239):

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

**Polling Mode (Development)**:
- Bot actively polls Telegram API
- No HTTPS or public URL required
- Set `UseWebhook: false`

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
├── Program.cs                  # Application entry point (Lines 1-308)
└── appsettings.json
```

### Program.cs

**Location**: `Program.cs` (Lines 1-308)

**Service Registration Order** (Lines 37-139):
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

**Middleware Pipeline** (Lines 216-257):
1. Serilog request logging
2. Custom request logging
3. Global exception handling
4. Swagger (Development)
5. HTTPS redirection
6. Authentication
7. Authorization
8. Map Controllers
9. Map Health Checks

### API Response Pattern

**ApiResponse<T>** (`Models/ApiResponse.cs`):
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
```

### Authentication

**JWT Configuration** (Lines 56-108):
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});
```

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
3. Configure `.env` file
4. Start Docker: `docker-compose up -d`
5. Apply migrations: `cd src/SurveyBot.API && dotnet ef database update`
6. Configure bot token in `appsettings.json`
7. Run: `dotnet run`
8. Access Swagger: http://localhost:5000/swagger

### Database Migrations

```bash
cd src/SurveyBot.API
dotnet ef migrations add MigrationName
dotnet ef database update
dotnet ef migrations remove
dotnet ef migrations script
```

---

## Testing

### Running Tests

```bash
dotnet test
dotnet test /p:CollectCoverage=true
dotnet test --filter "FullyQualifiedName~SurveyServiceTests"
```

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=surveybot_db;..."
  },
  "JwtSettings": {
    "SecretKey": "min-32-characters",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Client",
    "ExpirationMinutes": 60
  },
  "BotConfiguration": {
    "BotToken": "",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000",
    "AdminUserIds": []
  }
}
```

---

## API Reference

### Authentication

**POST /api/auth/login**: Login with Telegram credentials
**POST /api/auth/refresh**: Refresh JWT token

### Surveys

**POST /api/surveys**: Create survey
**GET /api/surveys**: List surveys (paginated)
**GET /api/surveys/{id}**: Get survey details
**PUT /api/surveys/{id}**: Update survey
**DELETE /api/surveys/{id}**: Delete survey
**POST /api/surveys/{id}/activate**: Activate survey
**POST /api/surveys/{id}/deactivate**: Deactivate survey
**GET /api/surveys/{id}/statistics**: Get statistics
**GET /api/surveys/code/{code}**: Get by code (public)

### Questions

**POST /api/surveys/{surveyId}/questions**: Add question
**GET /api/surveys/{surveyId}/questions**: List questions
**GET /api/questions/{id}**: Get question
**PUT /api/questions/{id}**: Update question
**DELETE /api/questions/{id}**: Delete question
**POST /api/surveys/{surveyId}/questions/reorder**: Reorder

### Responses

**POST /api/surveys/{surveyId}/responses/start**: Start response
**POST /api/responses/{responseId}/answers**: Submit answer
**POST /api/responses/{responseId}/complete**: Complete response
**GET /api/surveys/{surveyId}/responses**: List responses
**GET /api/responses/{id}**: Get response details

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

---

## Troubleshooting

### Database Connection

**Problem**: Cannot connect to PostgreSQL
**Solutions**: Check Docker is running (`docker ps`), verify connection string

### Migrations

**Problem**: Migrations fail
**Solutions**: Ensure in API directory, check EF Core tools installed

### Bot Not Responding

**Problem**: Bot doesn't respond
**Solutions**: Verify bot token, check initialization logs, test with `/start`

### JWT Authentication

**Problem**: 401 Unauthorized
**Solutions**: Verify SecretKey is 32+ chars, check token expiration, verify Issuer/Audience match

---

**End of Documentation**
