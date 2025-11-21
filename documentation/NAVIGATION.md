# SurveyBot Documentation Navigation Guide

**Quick navigation based on your role and what you want to accomplish.**

---

## I'm New Here

**Start Here**:
1. Read [Main README](../README.md) for project overview
2. Review [Main CLAUDE.md](../CLAUDE.md) for architecture and quick start
3. Set up environment following [Quick Start Guide](../CLAUDE.md#quick-start-guide)
4. Explore [layer-specific documentation](../CLAUDE.md#quick-navigation)

**Next Steps**:
- Want to develop? See [Development Workflow](#im-a-developer)
- Want to deploy? See [Deployment](#im-deploying-to-production)
- Want to use the bot? See [Bot Usage](#im-a-bot-user)

---

## I'm a Developer

### Setting Up Development Environment
1. [Quick Start Guide](../CLAUDE.md#quick-start-guide) - 5-minute setup
2. [Docker Startup Guide](./deployment/DOCKER-STARTUP-GUIDE.md) - Start PostgreSQL
3. [Database Quick Start](./database/QUICK-START-DATABASE.md) - Run migrations

### Understanding the Architecture
1. [Architecture Overview](../CLAUDE.md#architecture-overview) - Clean Architecture
2. [Core Layer](../src/SurveyBot.Core/CLAUDE.md) - Domain entities and interfaces
3. [Infrastructure Layer](../src/SurveyBot.Infrastructure/CLAUDE.md) - Database and repositories
4. [DI Structure](./development/DI-STRUCTURE.md) - Dependency injection patterns

### Working on Specific Layers

**Core Layer (Domain)**:
- [Core CLAUDE.md](../src/SurveyBot.Core/CLAUDE.md)
- Entities: User, Survey, Question, Response, Answer
- No external dependencies

**Infrastructure Layer (Data Access)**:
- [Infrastructure CLAUDE.md](../src/SurveyBot.Infrastructure/CLAUDE.md)
- Database context, repositories, services
- Migrations and PostgreSQL

**Bot Layer (Telegram)**:
- [Bot CLAUDE.md](../src/SurveyBot.Bot/CLAUDE.md)
- [Command Handlers Guide](./bot/COMMAND_HANDLERS_GUIDE.md)
- [State Machine Design](./bot/STATE-MACHINE-DESIGN.md)
- [Bot Integration Guide](./bot/INTEGRATION_GUIDE.md)

**API Layer (REST)**:
- [API CLAUDE.md](../src/SurveyBot.API/CLAUDE.md)
- [API Quick Reference](./api/QUICK-REFERENCE.md)
- [Logging & Error Handling](./api/LOGGING-ERROR-HANDLING.md)

**Frontend (React)**:
- [Frontend CLAUDE.md](../frontend/CLAUDE.md)
- Admin panel with React 19.2 + TypeScript

### Common Development Tasks

**Adding a New Feature**:
1. Start in [Core Layer](../src/SurveyBot.Core/CLAUDE.md) - Define entities/interfaces
2. Implement in [Infrastructure Layer](../src/SurveyBot.Infrastructure/CLAUDE.md) - Repositories/services
3. Expose via [API Layer](../src/SurveyBot.API/CLAUDE.md) or [Bot Layer](../src/SurveyBot.Bot/CLAUDE.md)
4. Write tests following [Test Summary](./testing/TEST_SUMMARY.md)

**Database Changes**:
1. Review [Infrastructure CLAUDE.md](../src/SurveyBot.Infrastructure/CLAUDE.md)
2. Modify entities in [Core Layer](../src/SurveyBot.Core/CLAUDE.md)
3. Create migration: `dotnet ef migrations add MigrationName`
4. Apply: `dotnet ef database update`

**Bot Development**:
1. [Bot Quick Start](./bot/QUICK_START.md) - Getting started
2. [Command Handlers Guide](./bot/COMMAND_HANDLERS_GUIDE.md) - Add commands
3. [State Machine Design](./bot/STATE-MACHINE-DESIGN.md) - Manage conversation state
4. [Help Messages](./bot/HELP_MESSAGES.md) - Update user-facing text

**Troubleshooting**:
1. [Main Troubleshooting](../CLAUDE.md#troubleshooting) - Common issues
2. [Bot Troubleshooting](./bot/BOT_TROUBLESHOOTING.md) - Bot-specific
3. [Bot FAQ](./bot/BOT_FAQ.md) - Frequently asked questions

---

## I'm Deploying to Production

### Initial Deployment
1. [Docker README](./deployment/DOCKER-README.md) - Production deployment guide
2. [Main CLAUDE.md - Configuration](../CLAUDE.md#critical-configuration) - Required settings
3. [API CLAUDE.md - Configuration](../src/SurveyBot.API/CLAUDE.md) - API-specific config

### Configuration Checklist
- [ ] PostgreSQL connection string
- [ ] Telegram bot token
- [ ] JWT secret key (≥ 32 characters)
- [ ] Webhook URL (production) or polling (development)
- [ ] Environment variables
- [ ] Docker compose configuration

### Docker Deployment
1. Review [Docker Startup Guide](./deployment/DOCKER-STARTUP-GUIDE.md)
2. Follow [Docker README](./deployment/DOCKER-README.md)
3. See [README Docker](./deployment/README-DOCKER.md)

### Database Setup
1. [Database Quick Start](./database/QUICK-START-DATABASE.md)
2. Run migrations: `dotnet ef database update`
3. Verify: `docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db`

### Monitoring & Troubleshooting
1. Health checks: `http://yourdomain/health/db`
2. Logs: Check Serilog output
3. [Logging & Error Handling](./api/LOGGING-ERROR-HANDLING.md)

---

## I'm Working with the API

### Getting Started with API
1. [API Layer CLAUDE.md](../src/SurveyBot.API/CLAUDE.md) - Complete API documentation
2. [API Quick Reference](./api/QUICK-REFERENCE.md) - Endpoint summary
3. Access Swagger UI: `http://localhost:5000/swagger`

### Authentication
1. [API CLAUDE.md - Authentication](../src/SurveyBot.API/CLAUDE.md#authentication)
2. POST `/api/auth/login` - Get JWT token
3. Use `Bearer <token>` in Authorization header

### API Endpoints Overview
- **/api/auth** - Authentication (login, register, me)
- **/api/surveys** - Survey CRUD, activation, statistics
- **/api/surveys/{surveyId}/questions** - Questions management
- **/api/responses** - Response submission and retrieval
- **/api/media** - Media upload/download
- **/health** - Health checks

### Common API Tasks
- **Create Survey**: POST `/api/surveys`
- **Add Question**: POST `/api/surveys/{surveyId}/questions`
- **Activate Survey**: POST `/api/surveys/{id}/activate`
- **Submit Response**: POST `/api/surveys/{id}/responses`
- **Get Statistics**: GET `/api/surveys/{id}/statistics`

### Error Handling
- [Logging & Error Handling Guide](./api/LOGGING-ERROR-HANDLING.md)
- Standard HTTP status codes
- Structured error responses

---

## I'm a Bot User

### Getting Started
1. [Bot User Guide](./bot/BOT_USER_GUIDE.md) - Complete user guide
2. [Bot Quick Start](./bot/BOT_QUICK_START.md) - Quick start for users
3. [Bot Command Reference](./bot/BOT_COMMAND_REFERENCE.md) - All commands

### Common Tasks

**Creating a Survey**:
1. Send `/start` to bot
2. Follow prompts to create survey
3. Add questions with media support
4. Get shareable survey code

**Taking a Survey**:
1. Send survey code to bot (e.g., `ABC123`)
2. Answer questions in sequence
3. Submit your response

**Managing Surveys**:
- `/surveys` - View your surveys
- `/stats <code>` - View survey statistics
- `/help` - Get help

### Bot Commands
- `/start` - Start bot, create survey
- `/help` - Show help message
- `/surveys` - List your surveys
- `/stats <code>` - View survey statistics
- `/cancel` - Cancel current operation

### Troubleshooting
- [Bot FAQ](./bot/BOT_FAQ.md) - Common questions
- [Bot Troubleshooting](./bot/BOT_TROUBLESHOOTING.md) - Issues and solutions

---

## I'm Testing

### Test Documentation
1. [Test Summary](./testing/TEST_SUMMARY.md) - Overall test coverage
2. [Manual Testing Checklist](./testing/MANUAL_TESTING_MEDIA_CHECKLIST.md) - Manual tests for media

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Specific tests
dotnet test --filter "FullyQualifiedName~SurveyServiceTests"
```

### Test Organization
- Unit tests - Test individual components
- Integration tests - Test layer interactions
- API tests - Test REST endpoints
- Manual tests - Follow checklist for multimedia features

---

## I'm an AI Assistant (Claude)

### Primary Documentation
1. **Start with**: [Main CLAUDE.md](../CLAUDE.md) - Project overview, architecture, quick start
2. **Layer-specific**: Check respective CLAUDE.md in each layer folder
3. **Complete index**: [Documentation INDEX.md](./INDEX.md)

### Quick Context Gathering
```
Project: SurveyBot - Telegram survey management system
Framework: .NET 8.0, Clean Architecture
Database: PostgreSQL 15
Bot: Telegram.Bot 22.7.4
Frontend: React 19.2 + TypeScript
```

### Layer Boundaries (Critical)
- **Core**: NO dependencies, contains entities, interfaces, DTOs
- **Infrastructure**: Depends ONLY on Core, contains database and repositories
- **Bot**: Depends on Core and Infrastructure, Telegram integration
- **API**: Depends on Core and Infrastructure, REST API
- **Frontend**: Separate React application

### Common Questions
1. **Configuration**: [Main CLAUDE.md - Configuration](../CLAUDE.md#critical-configuration)
2. **Architecture**: [Main CLAUDE.md - Architecture](../CLAUDE.md#architecture-overview)
3. **Troubleshooting**: [Main CLAUDE.md - Troubleshooting](../CLAUDE.md#troubleshooting)
4. **API Endpoints**: [API CLAUDE.md](../src/SurveyBot.API/CLAUDE.md)
5. **Database**: [Infrastructure CLAUDE.md](../src/SurveyBot.Infrastructure/CLAUDE.md)

### File Locations (Absolute Paths)
```
Root: C:\Users\User\Desktop\SurveyBot
Core: C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core
Infrastructure: C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Infrastructure
Bot: C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot
API: C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
Frontend: C:\Users\User\Desktop\SurveyBot\frontend
Tests: C:\Users\User\Desktop\SurveyBot\tests\SurveyBot.Tests
Documentation: C:\Users\User\Desktop\SurveyBot\documentation
```

---

## Quick Command Reference

### Development
```bash
# Start database
docker-compose up -d

# Run migrations
cd src/SurveyBot.API && dotnet ef database update

# Run API
cd src/SurveyBot.API && dotnet run

# Run tests
dotnet test

# Build solution
dotnet build
```

### Docker
```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# View logs
docker-compose logs -f postgres

# Access PostgreSQL
docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db
```

### Database Migrations
```bash
# Add migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Remove last migration
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script
```

---

## Documentation Organization

All documentation follows this structure:

```
documentation/
├── INDEX.md           # Complete documentation index with descriptions
├── NAVIGATION.md      # This file - role-based navigation
├── api/              # API layer documentation
├── bot/              # Bot layer documentation
├── core/             # Core layer documentation
├── database/         # Database documentation
├── deployment/       # Deployment guides
├── development/      # Development documentation
├── infrastructure/   # Infrastructure layer documentation
└── testing/          # Testing documentation
```

**Layer CLAUDE.md files** remain in their respective directories:
- `src/SurveyBot.Core/CLAUDE.md`
- `src/SurveyBot.Infrastructure/CLAUDE.md`
- `src/SurveyBot.Bot/CLAUDE.md`
- `src/SurveyBot.API/CLAUDE.md`
- `frontend/CLAUDE.md`

---

## Getting Help

1. **For Development Questions**: Check layer-specific CLAUDE.md files
2. **For API Questions**: [API Quick Reference](./api/QUICK-REFERENCE.md)
3. **For Bot Questions**: [Bot FAQ](./bot/BOT_FAQ.md)
4. **For Configuration Issues**: [Main CLAUDE.md - Configuration](../CLAUDE.md#critical-configuration)
5. **For Troubleshooting**: [Main CLAUDE.md - Troubleshooting](../CLAUDE.md#troubleshooting)
6. **For Everything Else**: [Documentation INDEX.md](./INDEX.md)

---

**Last Updated**: 2025-11-21
**Version**: 1.3.0
