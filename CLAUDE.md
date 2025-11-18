# SurveyBot - Project Documentation

**Version**: 1.2.0 | **Framework**: .NET 8.0 | **Status**: Active Development

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
- Telegram Bot Interface - Create and take surveys
- REST API - Full-featured programmatic access
- Survey Code Sharing - 6-character alphanumeric codes
- Real-time Analytics - Statistics, charts, CSV export
- JWT Authentication - Secure token-based auth
- Clean Architecture - Maintainable, testable, scalable

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
│           SurveyBot.API                     │
│   (REST API, Controllers, Middleware)       │
└───────────────┬─────────────────────────────┘
                │ depends on
                ▼
┌─────────────────────────────────────────────┐
│       SurveyBot.Infrastructure              │
│  (Database, Repositories, Services)         │
└───────────────┬─────────────────────────────┘
                │ depends on
                ▼
┌─────────────────────────────────────────────┐
│           SurveyBot.Core                    │
│  (Entities, Interfaces, DTOs, Exceptions)   │
└───────────────▲─────────────────────────────┘
                │ depends on
┌───────────────┴─────────────────────────────┐
│           SurveyBot.Bot                     │
│  (Telegram Bot, Handlers, State Mgmt)      │
└─────────────────────────────────────────────┘
```

**Core Principle**: Core has ZERO dependencies. All layers depend on Core.

### Layer Descriptions

**[SurveyBot.Core](src/SurveyBot.Core/CLAUDE.md)** - Domain Layer (NO dependencies)
- Entities: User, Survey, Question, Response, Answer
- Interfaces: Repository and service contracts
- DTOs: Data transfer objects
- Exceptions: Domain-specific exceptions
- Utilities: SurveyCodeGenerator

**[SurveyBot.Infrastructure](src/SurveyBot.Infrastructure/CLAUDE.md)** - Data Access
- DbContext with PostgreSQL
- Repository implementations
- Business logic services
- Database migrations

**[SurveyBot.Bot](src/SurveyBot.Bot/CLAUDE.md)** - Telegram Integration
- Bot service and update handler
- Command handlers (start, help, surveys, stats)
- Question handlers (text, choice, rating)
- Conversation state management

**[SurveyBot.API](src/SurveyBot.API/CLAUDE.md)** - REST API
- Controllers (Auth, Surveys, Questions, Responses)
- JWT authentication
- Global exception handling
- Swagger documentation

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
User (1) ──creates──> Surveys (*)
  │
  └─> Survey ──contains──> Questions (*)
        │
        └──receives──> Responses (*)
              │
              └──contains──> Answers (*)
```

**See** [Core Layer Documentation](src/SurveyBot.Core/CLAUDE.md) for detailed entity descriptions.

**Question Types**: Text, SingleChoice, MultipleChoice, Rating

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

**Questions** (`/api/surveys/{surveyId}/questions`)
- POST - Add question (auth)
- GET - List questions (public for active)
- PUT `/{id}` - Update (auth)
- DELETE `/{id}` - Delete (auth)
- POST `/reorder` - Reorder (auth)

**Responses** (`/api/responses`)
- POST `/surveys/{id}/responses` - Start (PUBLIC)
- POST `/{id}/answers` - Save answer (PUBLIC)
- POST `/{id}/complete` - Complete (PUBLIC)
- GET `/surveys/{id}/responses` - List (auth)

**Health** (`/health`)
- GET `/health/db` - Database health
- GET `/health` - Basic health

**See** [API Layer Documentation](src/SurveyBot.API/CLAUDE.md) for detailed endpoint info.

---

## Summary for AI Assistants

**SurveyBot** is a .NET 8.0 Telegram bot with React admin panel following Clean Architecture.

**Key Points**:
1. **Two config files**: Base + Development (overrides)
2. **Bot modes**: Polling (local) vs Webhook (prod, needs HTTPS)
3. **Architecture**: Clean Architecture - Core has zero dependencies
4. **Database**: PostgreSQL via Docker, EF Core migrations
5. **Auth**: JWT Bearer with Telegram-based login
6. **Survey codes**: 6-char alphanumeric (Base36)
7. **File paths**: Always use absolute paths

**Quick Setup**: Docker PostgreSQL → Configure bot token → Apply migrations → Run API → Access Swagger

**Layer Documentation**:
- [Core](src/SurveyBot.Core/CLAUDE.md) - Entities, interfaces, DTOs, exceptions
- [Infrastructure](src/SurveyBot.Infrastructure/CLAUDE.md) - Database, repositories, services, migrations
- [Bot](src/SurveyBot.Bot/CLAUDE.md) - Telegram handlers, commands, state management
- [API](src/SurveyBot.API/CLAUDE.md) - Controllers, middleware, authentication, Swagger

**Common Tasks**:
- Add migration: `dotnet ef migrations add Name`
- Apply migrations: `dotnet ef database update`
- Run API: `cd src/SurveyBot.API && dotnet run`
- Access Swagger: http://localhost:5000/swagger
- Check bot: `curl https://api.telegram.org/bot<TOKEN>/getMe`

---

**Last Updated**: 2025-11-12 | **Version**: 1.2.0 | **Target Framework**: .NET 8.0
