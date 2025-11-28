# SurveyBot - Project Documentation

**Version**: 1.5.0 | **Framework**: .NET 8.0 | **Status**: Active Development

---

## Quick Navigation

**Layer-Specific Documentation**:
- [Core Layer](src/SurveyBot.Core/CLAUDE.md) - Domain entities, interfaces, DTOs, exceptions
- [Infrastructure Layer](src/SurveyBot.Infrastructure/CLAUDE.md) - Database, repositories, services
- [Bot Layer](src/SurveyBot.Bot/CLAUDE.md) - Telegram bot handlers, commands, state management
- [API Layer](src/SurveyBot.API/CLAUDE.md) - REST API, controllers, middleware, authentication
- [Frontend](frontend/CLAUDE.md) - React admin panel (if applicable)

---

## Project Overview

**SurveyBot** is a comprehensive Telegram-based survey management system built with .NET 8.0 following Clean Architecture principles. Users create surveys via Telegram bot or web interface, distribute with shareable codes, collect responses, and analyze results.

### Key Features
- **Conditional Question Flow** - Dynamic branching with cycle detection and validation (v1.4.0)
- **Telegram Bot Interface** - Create and take surveys with conditional navigation
- **REST API** - Full-featured programmatic access with flow configuration
- **Survey Code Sharing** - 6-character alphanumeric codes (Base36)
- **Multimedia Support** - Images (JPG, PNG), videos (MP4), audio (MP3, OGG), documents (PDF)
- **Real-time Analytics** - Statistics, charts, CSV export
- **JWT Authentication** - Secure token-based auth
- **Value Object Pattern** - Type-safe domain modeling with DDD principles (v1.5.0 - complete)
- **Factory Methods** - Entity creation with validation and encapsulation (v1.5.0)
- **Domain Encapsulation** - Private setters with controlled modification (v1.5.0)
- **Clean Architecture** - Maintainable, testable, scalable with zero-dependency core

### Technology Stack
- .NET 8.0, ASP.NET Core Web API
- Entity Framework Core 9.0, PostgreSQL 15
- Telegram.Bot 22.7.4
- Serilog, AutoMapper 12.0
- React 19.2 + TypeScript (Frontend)
- Docker

---

## Quick Start Guide

### Prerequisites

**REQUIRED**:
1. .NET 8.0 SDK - [Download](https://dotnet.microsoft.com/download)
2. Docker Desktop - [Download](https://www.docker.com/products/docker-desktop)
3. Telegram Bot Token - Get from [@BotFather](https://t.me/botfather)

### 5-Minute Setup

```bash
# 1. Clone & navigate
git clone <repository-url>
cd SurveyBot

# 2. Start PostgreSQL
docker-compose up -d

# 3. Configure bot token (edit appsettings.Development.json)
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000"
  }
}

# 4. Apply migrations
cd src/SurveyBot.API
dotnet ef database update

# 5. Run
dotnet run
```

**Access Points**:
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Health: http://localhost:5000/health/db

### Getting Telegram Bot Token

1. Open Telegram → [@BotFather](https://t.me/botfather)
2. Send `/newbot`
3. Choose name: "My Survey Bot"
4. Choose username: "my_survey_bot" (must end in 'bot')
5. Copy token: `1234567890:ABCdefGHIjklMNOpqrsTUVwxyz`
6. Paste into `appsettings.Development.json`

---

## Architecture Overview

### Clean Architecture Layers

```
┌─────────────────────────────────────────────┐
│           SurveyBot.API (v1.4.0)            │
│   REST API, 10 Controllers, Middleware      │
│   NEW: QuestionFlowController               │
└───────────────┬─────────────────────────────┘
                │ depends on
                ▼
┌─────────────────────────────────────────────┐
│   SurveyBot.Infrastructure (v1.4.1)         │
│   Database, Repositories, Services          │
│   NEW: Owned Types, Cycle Detection (DFS)   │
└───────────────┬─────────────────────────────┘
                │ depends on
                ▼
┌─────────────────────────────────────────────┐
│       SurveyBot.Core (v1.4.0)               │
│   7 Entities, 16 Interfaces, 42+ DTOs       │
│   NEW: QuestionOption, Value Objects        │
│   ZERO DEPENDENCIES ✓                       │
└───────────────▲─────────────────────────────┘
                │ depends on
┌───────────────┴─────────────────────────────┐
│       SurveyBot.Bot (v1.4.0)                │
│   Telegram Bot, Handlers, State Mgmt        │
│   NEW: Conditional Flow, Navigation Helper  │
└─────────────────────────────────────────────┘
```

**Core Principle**: Core has ZERO dependencies. All layers depend on Core.

**NEW in v1.4.0**: Conditional question flow spans all layers with type-safe value objects, DFS-based cycle detection, and comprehensive validation.

### Layer Descriptions

**[SurveyBot.Core](src/SurveyBot.Core/CLAUDE.md) v1.5.0** - Domain Layer (ZERO dependencies)
- **Entities**: User, Survey, Question (with DefaultNext), QuestionOption (NEW), Response (with VisitedQuestionIds), Answer, MediaFile
- **DDD Patterns**: Private setters, factory methods, value objects (v1.5.0)
- **Value Objects**: NextQuestionDeterminant, AnswerValue hierarchy (polymorphic - NEW v1.5.0)
- **Interfaces**: 16 repository/service contracts including ISurveyValidationService
- **DTOs**: 42+ data transfer objects with nested structures
- **Exceptions**: Domain-specific exceptions + SurveyCycleException
- **Utilities**: SurveyCodeGenerator, AnswerValueFactory (NEW v1.5.0)

**[SurveyBot.Infrastructure](src/SurveyBot.Infrastructure/CLAUDE.md) v1.4.1** - Data Access & Business Logic
- **DbContext**: PostgreSQL with EF Core 9.0, owned type configurations (NextQuestionDeterminant)
- **Repositories**: Generic + specialized (Question, Response, Survey, User)
- **Services**: QuestionService, ResponseService (with conditional flow), SurveyValidationService (DFS cycle detection - NEW)
- **Migrations**: Clean slate approach for complex value objects
- **Validation**: Graph-based cycle detection, FK constraint validation

**[SurveyBot.Bot](src/SurveyBot.Bot/CLAUDE.md) v1.4.0** - Telegram Integration
- **Bot Service**: TelegramBotService with polling/webhook modes
- **Update Handler**: Message routing and error handling
- **Command Handlers**: /start, /help, /surveys, /stats with inline keyboards
- **Question Handlers**: Text, choice, rating with multimedia support
- **Conversation State**: ConversationState with VisitedQuestions tracking (NEW)
- **Navigation**: SurveyNavigationHelper for conditional flow (NEW)

**[SurveyBot.API](src/SurveyBot.API/CLAUDE.md) v1.4.0** - REST API
- **Controllers**: 10 controllers (Auth, Surveys, Questions, Responses, Media, QuestionFlow - NEW)
- **AutoMapper**: Value object mappings (NextQuestionDeterminant ↔ NextQuestionDeterminantDto)
- **Middleware**: Authentication, global exception handling, request logging
- **JWT Authentication**: Token-based security with role support
- **Swagger**: OpenAPI documentation with auth UI

### Architectural Patterns (v1.5.0+)

SurveyBot implements 10 core design patterns for maintainability and scalability:

1. **Clean Architecture** - Zero-dependency core, onion-style layers
2. **Repository Pattern** - Generic + specialized repositories with Include support
3. **Service Layer Pattern** - Business logic encapsulation (QuestionService, ResponseService, SurveyValidationService)
4. **DTO Pattern** - 42+ DTOs for API contracts and data transfer
5. **Value Object Pattern** - NextQuestionDeterminant, AnswerValue hierarchy (type-safe, immutable, equality-based)
6. **Factory Method Pattern** - Entity.Create() methods with validation (v1.5.0)
7. **Owned Entity Types** - EF Core owned types for value objects (prevents magic values)
8. **Encapsulation Pattern** - Private setters with controlled modification methods (v1.5.0)
9. **Graph Algorithms** - DFS-based cycle detection for survey flow validation
10. **Polymorphic Value Objects** - AnswerValue hierarchy with JSON discriminators (v1.5.0)

**Key DDD Principles** (Enhanced in v1.5.0):
- **Encapsulation**: All entities have private setters, modification through methods
- **Factory Methods**: Centralized entity creation with validation (Survey.Create(), Question.Create(), etc.)
- **Value Objects**: NextQuestionDeterminant + AnswerValue hierarchy eliminate primitive obsession
- **Aggregates**: Survey is aggregate root containing Questions/QuestionOptions
- **Domain Services**: SurveyValidationService for complex validation logic
- **Domain Exceptions**: Strongly-typed exceptions for business rule violations
- **Immutability**: Value objects are immutable with value semantics

**See**: [Architecture Documentation](documentation/architecture/ARCHITECTURE.md) for detailed pattern descriptions.

### Recent Changes (v1.5.x)

**v1.5.0 (DDD Architecture Enhancements - Complete)**:
- **Private Setters (ARCH-001)**: All entities now enforce encapsulation with private setters
  - Properties exposed as `{ get; private set; }`
  - Collections use backing fields with IReadOnlyCollection exposure
  - Modification through dedicated setter methods (SetTitle(), SetCode(), etc.)
  - Internal methods for EF Core compatibility (AddQuestionInternal(), etc.)
- **Factory Methods (ARCH-002)**: Centralized entity creation with validation
  - All entities have static Create() factory methods
  - Private constructors prevent direct instantiation
  - Validation at construction time (title length, non-null IDs, etc.)
  - Auto-generation of defaults (timestamps, codes) in factory
- **AnswerValue Value Object (ARCH-003)**: ✅ **COMPLETE** - Polymorphic answer type system
  - Abstract AnswerValue base class with [JsonDerivedType] for polymorphic JSON serialization
  - Five concrete value objects: TextAnswerValue, SingleChoiceAnswerValue, MultipleChoiceAnswerValue, RatingAnswerValue, LocationAnswerValue
  - AnswerValueFactory utility class: Parse(), TryParse(), CreateFromInput(), ParseWithTypeDiscriminator(), ConvertFromLegacy()
  - Answer entity: New Value property (AnswerValue?) alongside legacy AnswerText/AnswerJson for backward compatibility
  - EF Core owned type configuration: Stored as JSONB in answer_value_json column with type discriminator
  - Migration 20251127104737_AddAnswerValueJsonColumn: Preserves existing data, adds new column
  - Eliminates string parsing across all layers (ResponseService, SurveyService, API controllers)
  - Provides compile-time type safety, validation at value object creation, no runtime JSON errors

**v1.4.2 (Complete Value Object Migration)**:
- **Completed Answer entity migration** to NextQuestionDeterminant value object
- Eliminated the last magic value (0) in conditional flow system
- Updated ResponseService with renamed methods for value object consistency
- Added comprehensive owned type configuration for Answer.Next
- Created migration with data preservation (0 → EndSurvey, >0 → GoToQuestion)
- Added CHECK constraints for database-level invariant enforcement
- Complete type safety achieved across all conditional flow entities

**v1.4.1 (Infrastructure Refactoring)**:
- Implemented EF Core owned types for NextQuestionDeterminant
- Added clean slate migration approach for complex value object changes
- Enhanced FK constraint validation and error handling
- Improved DFS cycle detection algorithm

**v1.4.0 (Conditional Question Flow)**:
- Added QuestionOption entity with NextQuestionId for branching
- Implemented NextQuestionDeterminant value object (replaces magic values)
- Added Question.DefaultNextQuestionId for fallback navigation
- Enhanced Response with VisitedQuestionIds tracking
- Created QuestionFlowController for flow configuration
- Added SurveyValidationService with DFS-based cycle prevention
- Updated bot handlers for conditional navigation
- Implemented SurveyNavigationHelper utility

**Breaking Changes v1.5.0**:
- **Entity Creation**: Must use factory methods (Survey.Create(), Answer.CreateWithValue(), etc.) - new keyword no longer recommended
- **Entity Modification**: Direct property setting no longer possible - use setter methods (survey.SetTitle(), answer.UpdateValue())
- **Answer Value** (ARCH-003):
  - New Value property (AnswerValue?) is the preferred way to access answer content
  - Legacy AnswerText/AnswerJson properties maintained for backward compatibility
  - Recommended: Use Answer.CreateWithValue() instead of CreateTextAnswer()/CreateJsonAnswer()
  - Services should use AnswerValue for type-safe answer handling, not string parsing
  - AutoMapper profiles simplified - no more JSON parsing in mapping code

**Breaking Changes v1.4.0**:
- QuestionOption now uses NextQuestionDeterminant value object instead of nullable int
- Response requires VisitedQuestionIds (List<int>) for cycle tracking
- Question.Options collection is now required for choice questions
- AutoMapper profiles updated for value object mapping

---

## Critical Configuration

### Two Configuration Files (IMPORTANT)

**File 1**: `src/SurveyBot.API/appsettings.json` (Base/Production)
- Default settings, committed to source control

**File 2**: `src/SurveyBot.API/appsettings.Development.json` (Development Overrides)
- OVERRIDES base when `ASPNETCORE_ENVIRONMENT=Development`
- Contains your bot token

### Telegram Bot Modes

**Mode 1: Polling (Local Development)** ✅ Recommended

```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000"
  }
}
```

**Pros**: No HTTPS needed, works behind firewall, simple setup

**Mode 2: Webhook (Production)** ✅ Required for production

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

**Requires**: Public HTTPS URL with valid SSL certificate

**Local Webhook (ngrok)**:
```bash
ngrok http 5000
# Use the HTTPS URL in WebhookUrl
```

### Database Configuration

**Docker PostgreSQL** (Included):
```yaml
PostgreSQL: localhost:5432
Database: surveybot_db
User: surveybot_user
Password: surveybot_dev_password

pgAdmin: http://localhost:5050
Email: admin@surveybot.local
Password: admin123
```

**Verify**: `docker ps`

### JWT Configuration

**IMPORTANT**: Secret key must be ≥ 32 characters for HS256

```json
{
  "JwtSettings": {
    "SecretKey": "SurveyBot-Super-Secret-Key-For-JWT-Token-Generation-2025",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Clients",
    "TokenLifetimeHours": 24
  }
}
```

---

## Development Workflow

### Database Migrations

```bash
cd src/SurveyBot.API

# Add migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
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

**Expected Output**:
```
[10:30:00 INF] Starting SurveyBot API application
[10:30:02 INF] Telegram Bot initialized successfully
[10:30:02 INF] SurveyBot API started successfully
Now listening on: http://localhost:5000
```

---

## Testing

```bash
# Run all tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true

# Specific test class
dotnet test --filter "FullyQualifiedName~SurveyServiceTests"

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

**Test Organization**: Unit, Integration, API tests in `tests/SurveyBot.Tests/`

---

## Troubleshooting

### Database Connection Issues

**Problem**: Cannot connect to PostgreSQL

**Solutions**:
1. Check Docker: `docker ps`
2. Verify connection string in appsettings.json
3. Check logs: `docker-compose logs postgres`
4. Test: `docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db`
5. Restart: `docker-compose restart`

### Bot Not Responding

**Problem**: Telegram bot doesn't respond

**Solutions**:
1. Verify bot token in `appsettings.Development.json`
2. Check logs for "Telegram Bot initialized successfully"
3. **Ensure `UseWebhook: false`** for local dev
4. Test token: `https://api.telegram.org/bot<TOKEN>/getMe`
5. Try `/start` in Telegram

### Webhook Issues (Production)

**Problem**: Webhook not receiving updates

**Solutions**:
1. **Verify PUBLIC HTTPS URL** (not localhost)
2. Check status: `https://api.telegram.org/bot<TOKEN>/getWebhookInfo`
3. **Ensure valid SSL certificate** (not self-signed)
4. For local dev: use **polling mode** or **ngrok**
5. Clear webhook: `https://api.telegram.org/bot<TOKEN>/deleteWebhook`

### Migration Issues

**Problem**: Migrations fail to apply

**Solutions**:
1. Ensure correct directory: `cd src/SurveyBot.API`
2. Install tools: `dotnet tool install --global dotnet-ef`
3. Verify DbContext: `dotnet ef dbcontext info`
4. Check build: `dotnet build`
5. Try removing and re-adding migration

### JWT Authentication Issues

**Problem**: 401 Unauthorized

**Solutions**:
1. Verify SecretKey ≥ 32 characters
2. Check token expiration (default 24h)
3. Ensure Issuer/Audience match
4. Verify format: `Bearer <token>`
5. Test with Swagger UI authentication

### Configuration Not Loading

**Problem**: Settings not applying

**Solutions**:
1. Check `ASPNETCORE_ENVIRONMENT` variable
2. Verify JSON syntax (no trailing commas)
3. Development file overrides base
4. Restart after config changes
5. Check logs for config errors

---

## Useful Commands

### Build & Restore
```bash
dotnet build
dotnet restore
dotnet clean
```

### Database
```bash
dotnet ef database update
dotnet ef database drop --force
dotnet ef migrations script
```

### Docker
```bash
docker-compose up -d
docker-compose down
docker-compose logs -f postgres
docker ps
docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db
```

### Telegram Bot Testing
```bash
# Check bot info
curl https://api.telegram.org/bot<TOKEN>/getMe

# Check webhook status
curl https://api.telegram.org/bot<TOKEN>/getWebhookInfo

# Delete webhook (switch to polling)
curl https://api.telegram.org/bot<TOKEN>/deleteWebhook
```

---

## Entity Relationship Overview

```
User (1) ──creates──> Survey (*) ──contains──> Question (*)
                        │                        │
                        │                        ├──> DefaultNext (0..1) ─────┐
                        │                        │                           │
                        │                        └──> QuestionOption (*) ────┤
                        │                             (for choice questions) │
                        │                             ├──> NextQuestion (0..1)┘
                        │                             └──> Value Objects:
                        │                                  NextQuestionDeterminant
                        │
                        └──receives──> Response (*)
                                        │
                                        ├──> VisitedQuestionIds (List<int>) - NEW v1.4.0
                                        │
                                        └──contains──> Answer (*)
                                                        │
                                                        ├──> QuestionId (FK)
                                                        └──> SelectedOptions (for choice)
```

**NEW in v1.4.0**:
- **QuestionOption**: Represents individual options in choice questions with conditional flow logic
- **DefaultNext**: Question.DefaultNextQuestionId for fallback navigation
- **NextQuestion**: QuestionOption.NextQuestionId for conditional branching
- **VisitedQuestionIds**: Response tracking for cycle prevention and navigation history

**See** [Core Layer Documentation](src/SurveyBot.Core/CLAUDE.md) for detailed entity descriptions.

**Question Types**: Text, SingleChoice, MultipleChoice, Rating, Location (NEW v1.5.0)

**Media Types**: Image (JPG, PNG), Video (MP4), Audio (MP3, OGG), Document (PDF)

---

## API Endpoints Quick Reference

**Authentication** (`/api/auth`)
- POST `/login` - Login with Telegram, get JWT
- POST `/register` - Register/update user
- GET `/me` - Get current user

**Surveys** (`/api/surveys`)
- POST `/` - Create survey (auth)
- GET `/` - List surveys (auth, paginated)
- GET `/{id}` - Get survey (auth)
- PUT `/{id}` - Update (auth)
- DELETE `/{id}` - Delete (auth)
- POST `/{id}/activate` - Activate (auth)
- GET `/code/{code}` - Get by code (PUBLIC)
- GET `/{id}/statistics` - Statistics (auth)

**Media** (`/api/media`)
- POST `/upload` - Upload media file (auth, auto-detects type)
- DELETE `/{mediaId}` - Delete media file (auth)

**Questions** (`/api/surveys/{surveyId}/questions`)
- POST - Add question (auth)
- GET - List questions (public for active)
- PUT `/{id}` - Update (auth)
- DELETE `/{id}` - Delete (auth)
- POST `/reorder` - Reorder (auth)

**Question Flow** (`/api/questionflow`) - NEW v1.4.0
- GET `/{surveyId}` - Get conditional flow configuration (auth)
- PUT `/{surveyId}` - Update flow with validation (auth, prevents cycles)

**Responses** (`/api/responses`)
- POST `/surveys/{id}/responses` - Start (PUBLIC)
- POST `/{id}/answers` - Save answer (PUBLIC, respects conditional flow)
- POST `/{id}/complete` - Complete (PUBLIC)
- GET `/surveys/{id}/responses` - List (auth)

**Health** (`/health`)
- GET `/health/db` - Database health
- GET `/health` - Basic health

**See** [API Layer Documentation](src/SurveyBot.API/CLAUDE.md) for detailed endpoint info.

---

## Documentation Structure

### Centralized Documentation Hub

All project documentation is organized in the **`documentation/`** folder for easy access and maintenance. This centralized approach keeps documentation organized by category while maintaining layer-specific CLAUDE.md files in their respective directories.

**Location**: `C:\Users\User\Desktop\SurveyBot\documentation\`

### Documentation Organization

```
documentation/
├── INDEX.md                        # Complete documentation index (start here)
├── NAVIGATION.md                   # Role-based navigation guide
├── AGENT_DOCUMENTATION_UPDATE_REPORT.md  # Documentation maintenance report
├── PRD_SurveyBot_MVP.md           # Product requirements document
├── api/                           # API layer documentation
│   ├── LOGGING-ERROR-HANDLING.md
│   ├── QUICK-REFERENCE.md
│   ├── API_REFERENCE.md
│   └── PHASE2_API_REFERENCE.md
├── architecture/                  # Architecture documentation
│   └── ARCHITECTURE.md
├── auth/                          # Authentication documentation
│   └── AUTHENTICATION_FLOW.md
├── bot/                           # Bot layer documentation
│   ├── BOT_COMMAND_REFERENCE.md
│   ├── BOT_FAQ.md
│   ├── BOT_QUICK_START.md
│   ├── BOT_TROUBLESHOOTING.md
│   ├── BOT_USER_GUIDE.md
│   ├── COMMAND_HANDLERS_GUIDE.md
│   ├── HELP_MESSAGES.md
│   ├── INTEGRATION_GUIDE.md
│   ├── QUICK_START.md
│   ├── README.md
│   └── STATE-MACHINE-DESIGN.md
├── database/                      # Database documentation
│   ├── ER_DIAGRAM.md
│   ├── INDEX_OPTIMIZATION.md
│   ├── QUICK-START-DATABASE.md
│   ├── README.md
│   ├── RELATIONSHIPS.md
│   └── TASK_*.md
├── deployment/                    # Deployment guides
│   ├── DOCKER-README.md
│   ├── DOCKER-STARTUP-GUIDE.md
│   └── README-DOCKER.md
├── development/                   # Development documentation
│   └── DI-STRUCTURE.md
├── fixes/                         # Fix documentation
│   └── MEDIA_STORAGE_FIX.md
├── flows/                         # User flow documentation
│   ├── SURVEY_CREATION_FLOW.md
│   └── SURVEY_TAKING_FLOW.md
├── guides/                        # General guides
│   └── DOCUMENTATION_OVERVIEW.md
└── testing/                       # Testing documentation
    ├── MANUAL_TESTING_MEDIA_CHECKLIST.md
    ├── PHASE2_TESTING_GUIDE.md
    └── TEST_SUMMARY.md
```

### Quick Access Documentation

**Essential Starting Points**:
- **[Documentation Index](documentation/INDEX.md)** - Complete documentation catalog with descriptions
- **[Navigation Guide](documentation/NAVIGATION.md)** - Role-based navigation (developer, DevOps, user, AI)
- **[Developer Onboarding](documentation/DEVELOPER_ONBOARDING.md)** - New developer quick start
- **[Troubleshooting Guide](documentation/TROUBLESHOOTING.md)** - Common issues and solutions

**Layer-Specific CLAUDE.md Files** (remain in layer directories):
- [Core Layer](src/SurveyBot.Core/CLAUDE.md) - Domain entities, interfaces, DTOs
- [Infrastructure Layer](src/SurveyBot.Infrastructure/CLAUDE.md) - Database, repositories, services
- [Bot Layer](src/SurveyBot.Bot/CLAUDE.md) - Telegram bot implementation
- [API Layer](src/SurveyBot.API/CLAUDE.md) - REST API, controllers, middleware
- [Frontend](frontend/CLAUDE.md) - React admin panel

**By Topic**:
- **Architecture**: [Architecture Overview](documentation/architecture/ARCHITECTURE.md)
- **API**: [API Quick Reference](documentation/api/QUICK-REFERENCE.md) | [API Reference](documentation/api/API_REFERENCE.md)
- **Bot**: [Bot User Guide](documentation/bot/BOT_USER_GUIDE.md) | [Bot FAQ](documentation/bot/BOT_FAQ.md)
- **Database**: [Quick Start Database](documentation/database/QUICK-START-DATABASE.md) | [ER Diagram](documentation/database/ER_DIAGRAM.md)
- **Deployment**: [Docker Startup Guide](documentation/deployment/DOCKER-STARTUP-GUIDE.md)
- **Testing**: [Test Summary](documentation/testing/TEST_SUMMARY.md)

### Documentation Maintenance

**When to Update Documentation**:
1. After implementing new features - Update relevant layer CLAUDE.md and add specific docs to `documentation/`
2. After architecture changes - Update architecture and layer documentation
3. After API changes - Update `documentation/api/` files
4. After bot changes - Update `documentation/bot/` files
5. After fixing bugs - Add to `documentation/fixes/` if significant

**Where to Save Documentation**:
- **Layer-specific implementation details** → Layer's CLAUDE.md file
- **User guides and references** → `documentation/` subfolders by category
- **Fix documentation** → `documentation/fixes/`
- **Testing procedures** → `documentation/testing/`
- **Deployment guides** → `documentation/deployment/`

**Documentation Standards**:
- Keep layer CLAUDE.md files focused on technical implementation
- Use `documentation/` for user-facing guides and comprehensive references
- Always update [Documentation Index](documentation/INDEX.md) when adding new docs
- Include version numbers and last-updated dates
- Cross-reference related documentation

---

## Summary for AI Assistants

**SurveyBot v1.5.0** is a .NET 8.0 Telegram bot with React admin panel following Clean Architecture and DDD principles with comprehensive value object implementation.

**Key Points**:
1. **Version**: v1.5.0 (Core DDD enhancements complete) - Enhanced encapsulation, factory methods, polymorphic value objects
2. **Architecture**: Clean Architecture with 10 design patterns, ZERO-dependency core
3. **NEW in v1.5.0**: Private setters, factory methods, AnswerValue polymorphic hierarchy
4. **Features v1.4.x**: Conditional branching, cycle detection (DFS), value objects, owned types
4. **Config files**: Base (appsettings.json) + Development (appsettings.Development.json) overrides
5. **Bot modes**: Polling (local dev) vs Webhook (prod with HTTPS)
6. **Database**: PostgreSQL via Docker, EF Core 9.0 with owned type migrations
7. **Auth**: JWT Bearer with Telegram-based login
8. **Survey codes**: 6-char alphanumeric (Base36 via SurveyCodeGenerator)
9. **File paths**: Always use absolute paths (e.g., C:\Users\User\Desktop\SurveyBot\...)
10. **Documentation**: Centralized in `documentation/` + layer-specific CLAUDE.md files

**Architectural Highlights v1.5.0**:
- **7 Entities**: User, Survey, Question, QuestionOption, Response, Answer, MediaFile (all with private setters + factory methods)
- **Value Objects**:
  - NextQuestionDeterminant (conditional flow navigation - v1.4.0)
  - AnswerValue hierarchy (polymorphic answer types - v1.5.0)
- **Encapsulation**: All entities use private setters with modification methods
- **Factory Methods**: Survey.Create(), Question.Create(), Answer.CreateWithValue(), etc.
- **Graph Validation**: DFS-based cycle detection in SurveyValidationService
- **Conditional Flow**: Type-safe navigation throughout with value object enforcement
- **Response Tracking**: VisitedQuestionIds (List<int>) for cycle prevention
- **10 Controllers**: Including QuestionFlowController for flow configuration
- **42+ DTOs**: Nested structures with AutoMapper value object support

**Breaking Changes v1.5.0** (IMPORTANT):
- **Entity Creation**: Use factory methods (Survey.Create(), not new Survey())
- **Entity Modification**: Use setter methods (survey.SetTitle(), not survey.Title = ...)
- **Answer Value**: New AnswerValue polymorphic hierarchy alongside legacy fields
- **Collections**: Exposed as IReadOnlyCollection, modify through Add/Remove methods

**Breaking Changes v1.4.2**:
- **Answer entity**: `NextQuestionId` removed, replaced with `Next` (NextQuestionDeterminant value object)
- **QuestionOption**: Uses NextQuestionDeterminant value object (not nullable int)
- **Question**: DefaultNext is NextQuestionDeterminant value object
- **Response**: Requires VisitedQuestionIds property
- **AutoMapper**: Profiles updated for value object mappings
- **No more magic values**: 0 no longer means "end survey" - use `NextQuestionDeterminant.End()`

**Quick Setup**: Docker PostgreSQL → Configure bot token → Apply migrations → Run API → Access Swagger

**Documentation Hub**:
- **Start Here**: [Documentation Index](documentation/INDEX.md) | [Navigation Guide](documentation/NAVIGATION.md)
- **Layer Documentation**: [Core v1.5.0](src/SurveyBot.Core/CLAUDE.md) | [Infrastructure v1.4.1](src/SurveyBot.Infrastructure/CLAUDE.md) | [Bot v1.4.0](src/SurveyBot.Bot/CLAUDE.md) | [API v1.4.0](src/SurveyBot.API/CLAUDE.md)
- **Architecture**: [Architecture Overview](documentation/architecture/ARCHITECTURE.md) - Design patterns and principles
- **Architecture Improvements**: [Priority Architecture Tasks](documentation/features/!PRIORITY_ARCHITECTURE_IMPROVEMENTS.md) - DDD enhancements
- **Quick References**: [API](documentation/api/QUICK-REFERENCE.md) | [Bot Commands](documentation/bot/BOT_COMMAND_REFERENCE.md) | [Database](documentation/database/QUICK-START-DATABASE.md)

**Common Tasks**:
- Add migration: `cd src/SurveyBot.API && dotnet ef migrations add MigrationName`
- Apply migrations: `dotnet ef database update`
- Run API: `dotnet run` (from SurveyBot.API directory)
- Access Swagger: http://localhost:5000/swagger
- Check bot: `curl https://api.telegram.org/bot<TOKEN>/getMe`
- Validate survey flow: POST `/api/questionflow/{surveyId}` (auto-detects cycles)
- Find documentation: Check [documentation/INDEX.md](documentation/INDEX.md)

**Design Pattern References**:
1. Clean Architecture - Layers and dependencies
2. Repository Pattern - [Infrastructure CLAUDE.md](src/SurveyBot.Infrastructure/CLAUDE.md#repositories)
3. Value Objects - [Core CLAUDE.md](src/SurveyBot.Core/CLAUDE.md#value-objects)
4. Factory Methods - [Core CLAUDE.md](src/SurveyBot.Core/CLAUDE.md#factory-methods)
5. Encapsulation - [Core CLAUDE.md](src/SurveyBot.Core/CLAUDE.md#encapsulation)
6. Polymorphic Value Objects - [AnswerValue Hierarchy](src/SurveyBot.Core/CLAUDE.md#answervalue-hierarchy)
7. Owned Types - [Infrastructure CLAUDE.md](src/SurveyBot.Infrastructure/CLAUDE.md#owned-types)
8. DFS Cycle Detection - [SurveyValidationService](src/SurveyBot.Infrastructure/CLAUDE.md#survey-validation)
9. Conditional Flow - All layer documentation sections
10. AutoMapper - [API CLAUDE.md](src/SurveyBot.API/CLAUDE.md#automapper)

---

**Last Updated**: 2025-11-27 | **Version**: 1.5.0 | **Target Framework**: .NET 8.0
