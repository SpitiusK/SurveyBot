# SurveyBot Documentation Index

**Version**: 1.5.0 | **Last Updated**: 2025-11-27

This is a comprehensive index of all documentation available in the SurveyBot project. All documentation has been organized into logical categories for easy access.

---

## Project Overview

**Main Documentation**:
- [Project Root CLAUDE.md](../CLAUDE.md) - Main project documentation, quick start, architecture overview
- [README.md](../README.md) - Project introduction and quick setup guide
- [PRD - Product Requirements Document](./PRD_SurveyBot_MVP.md) - Original product requirements and MVP scope

---

## Layer-Specific Documentation

### Core Layer
**Location**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\CLAUDE.md`

**Topics Covered**:
- Domain entities (User, Survey, Question, Response, Answer)
- Repository and service interfaces
- DTOs and exceptions
- Survey code generation utilities

### Infrastructure Layer
**Location**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Infrastructure\CLAUDE.md`

**Topics Covered**:
- Database context and migrations
- Repository implementations
- Service implementations
- PostgreSQL configuration

### Bot Layer
**Location**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\CLAUDE.md`

**Documentation Files**:
- [Bot Layer CLAUDE.md](../src/SurveyBot.Bot/CLAUDE.md) - Main bot documentation
- [Command Handlers Guide](./bot/COMMAND_HANDLERS_GUIDE.md) - Detailed command handler implementation
- [Help Messages](./bot/HELP_MESSAGES.md) - Bot help text and messages
- [Integration Guide](./bot/INTEGRATION_GUIDE.md) - How to integrate with the bot
- [Quick Start](./bot/QUICK_START.md) - Getting started with bot development
- [State Machine Design](./bot/STATE-MACHINE-DESIGN.md) - Conversation state management
- [Bot README](./bot/README.md) - Bot layer overview
- [Command Reference](./bot/BOT_COMMAND_REFERENCE.md) - All available bot commands
- [Bot FAQ](./bot/BOT_FAQ.md) - Frequently asked questions
- [Bot Quick Start](./bot/BOT_QUICK_START.md) - User quick start guide
- [Bot Troubleshooting](./bot/BOT_TROUBLESHOOTING.md) - Common issues and solutions
- [Bot User Guide](./bot/BOT_USER_GUIDE.md) - Complete user guide

**Topics Covered**:
- Telegram bot setup and configuration
- Command handlers (/start, /help, /surveys, /stats)
- Question handlers (text, choice, rating)
- Conversation state management
- Webhook vs polling modes

### API Layer
**Location**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\CLAUDE.md`

**Documentation Files**:
- [API Layer CLAUDE.md](../src/SurveyBot.API/CLAUDE.md) - Main API documentation
- [Logging & Error Handling](./api/LOGGING-ERROR-HANDLING.md) - Error handling patterns
- [Quick Reference](./api/QUICK-REFERENCE.md) - API quick reference guide
- [QuestionFlow Debug Logging Guide](./api/QUESTIONFLOW_DEBUG_LOGGING_GUIDE.md) - Debug logging for question flow API

**Topics Covered**:
- REST API endpoints
- JWT authentication
- Controllers and middleware
- Swagger documentation
- Health checks
- Debug logging and troubleshooting

### Frontend
**Location**: `C:\Users\User\Desktop\SurveyBot\frontend\CLAUDE.md`

**Topics Covered**:
- React 19.2 + TypeScript setup
- Admin panel features
- API integration
- Component architecture

---

## Development Documentation

### Development Guides
- [DI Structure](./development/DI-STRUCTURE.md) - Dependency injection architecture

### Database
- [Quick Start Database Guide](./database/QUICK-START-DATABASE.md) - Database setup and migrations

### Deployment
- [Docker README](./deployment/DOCKER-README.md) - Docker deployment guide
- [Docker Startup Guide](./deployment/DOCKER-STARTUP-GUIDE.md) - Docker quick start
- [README Docker](./deployment/README-DOCKER.md) - Additional Docker information

### Testing
- [Test Summary](./testing/TEST_SUMMARY.md) - Overall test coverage and results
- [Manual Testing Media Checklist](./testing/MANUAL_TESTING_MEDIA_CHECKLIST.md) - Manual testing procedures for multimedia features

---

## Documentation by Topic

### Getting Started
1. [Main README](../README.md) - Project overview
2. [Main CLAUDE.md](../CLAUDE.md) - Quick start guide
3. [Docker Startup Guide](./deployment/DOCKER-STARTUP-GUIDE.md) - Docker setup
4. [Quick Start Database](./database/QUICK-START-DATABASE.md) - Database setup
5. [Bot Quick Start](./bot/BOT_QUICK_START.md) - User quick start

### Architecture & Design
1. [Main CLAUDE.md](../CLAUDE.md) - Clean Architecture overview
2. [Core Layer CLAUDE.md](../src/SurveyBot.Core/CLAUDE.md) - Domain layer with DDD patterns
3. [Infrastructure CLAUDE.md](../src/SurveyBot.Infrastructure/CLAUDE.md) - Data access layer
4. [Architecture Improvements Plan](./features/!PRIORITY_ARCHITECTURE_IMPROVEMENTS.md) - **NEW v1.5.0**: DDD enhancements (private setters, factory methods, value objects)
5. [DI Structure](./development/DI-STRUCTURE.md) - Dependency injection
6. [State Machine Design](./bot/STATE-MACHINE-DESIGN.md) - Bot state management

### API & Integration
1. [API Layer CLAUDE.md](../src/SurveyBot.API/CLAUDE.md) - API documentation
2. [API Quick Reference](./api/QUICK-REFERENCE.md) - Endpoint reference
3. [Bot Integration Guide](./bot/INTEGRATION_GUIDE.md) - Bot integration

### Bot Development
1. [Bot Layer CLAUDE.md](../src/SurveyBot.Bot/CLAUDE.md) - Bot architecture
2. [Command Handlers Guide](./bot/COMMAND_HANDLERS_GUIDE.md) - Handler implementation
3. [Bot Command Reference](./bot/BOT_COMMAND_REFERENCE.md) - All commands
4. [Help Messages](./bot/HELP_MESSAGES.md) - Bot messages

### Troubleshooting & Support
1. [Main CLAUDE.md - Troubleshooting Section](../CLAUDE.md#troubleshooting) - Common issues
2. [Bot Troubleshooting](./bot/BOT_TROUBLESHOOTING.md) - Bot-specific issues
3. [Bot FAQ](./bot/BOT_FAQ.md) - Frequently asked questions

### Deployment & Operations
1. [Docker README](./deployment/DOCKER-README.md) - Production deployment
2. [Docker Startup Guide](./deployment/DOCKER-STARTUP-GUIDE.md) - Quick deployment
3. [API CLAUDE.md - Configuration](../src/SurveyBot.API/CLAUDE.md) - API configuration

### Testing & Quality
1. [Test Summary](./testing/TEST_SUMMARY.md) - Test results
2. [Manual Testing Checklist](./testing/MANUAL_TESTING_MEDIA_CHECKLIST.md) - Manual tests

---

## Feature Implementation Plans

### Completed in v1.5.0
- [Architecture Improvements Plan](./features/!PRIORITY_ARCHITECTURE_IMPROVEMENTS.md) - ✅ **COMPLETED**: ARCH-001, ARCH-002, ARCH-003 (Private setters, factory methods, AnswerValue value objects)

### Ready for Implementation
- [Location Question Implementation Plan](./features/LOCATION_QUESTION_IMPLEMENTATION_PLAN.md) - Geographic coordinate collection feature
- [Architecture Improvements - Next Phase](./features/!PRIORITY_ARCHITECTURE_IMPROVEMENTS.md#arch-004) - ARCH-004 to ARCH-007 (SurveyCode, MediaContent value objects, rich domain models)

---

## Documentation Structure

```
documentation/
├── INDEX.md                        # This file - complete documentation index
├── NAVIGATION.md                   # Role-based navigation guide
├── PRD_SurveyBot_MVP.md           # Product requirements document
├── api/                           # API layer documentation
│   ├── LOGGING-ERROR-HANDLING.md
│   └── QUICK-REFERENCE.md
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
├── core/                          # Core layer documentation
│   └── (Reserved for future core-specific docs)
├── database/                      # Database documentation
│   └── QUICK-START-DATABASE.md
├── deployment/                    # Deployment documentation
│   ├── DOCKER-README.md
│   ├── DOCKER-STARTUP-GUIDE.md
│   └── README-DOCKER.md
├── development/                   # Development documentation
│   └── DI-STRUCTURE.md
├── features/                      # Feature implementation plans
│   └── LOCATION_QUESTION_IMPLEMENTATION_PLAN.md
├── infrastructure/                # Infrastructure layer documentation
│   └── (Reserved for future infrastructure-specific docs)
└── testing/                       # Testing documentation
    ├── MANUAL_TESTING_MEDIA_CHECKLIST.md
    └── TEST_SUMMARY.md
```

---

## Quick Links by Role

### For New Developers
1. Start with [Main CLAUDE.md](../CLAUDE.md)
2. Review [Architecture Overview](../CLAUDE.md#architecture-overview)
3. Follow [Quick Start Guide](../CLAUDE.md#quick-start-guide)
4. Explore layer-specific CLAUDE.md files

### For DevOps Engineers
1. [Docker README](./deployment/DOCKER-README.md)
2. [Docker Startup Guide](./deployment/DOCKER-STARTUP-GUIDE.md)
3. [Database Quick Start](./database/QUICK-START-DATABASE.md)
4. [API Configuration](../src/SurveyBot.API/CLAUDE.md#configuration)

### For API Consumers
1. [API Layer CLAUDE.md](../src/SurveyBot.API/CLAUDE.md)
2. [API Quick Reference](./api/QUICK-REFERENCE.md)
3. [Swagger Documentation](http://localhost:5000/swagger) (when running)

### For Bot Users
1. [Bot User Guide](./bot/BOT_USER_GUIDE.md)
2. [Bot Quick Start](./bot/BOT_QUICK_START.md)
3. [Bot Command Reference](./bot/BOT_COMMAND_REFERENCE.md)
4. [Bot FAQ](./bot/BOT_FAQ.md)

### For AI Assistants (Claude)
1. [Main CLAUDE.md](../CLAUDE.md) - Start here for project context
2. Layer-specific CLAUDE.md files for detailed information
3. [DI Structure](./development/DI-STRUCTURE.md) - Dependency patterns
4. This INDEX.md for finding specific documentation

---

## Maintenance Notes

**Documentation Standards**:
- All CLAUDE.md files remain in their respective layer directories
- Additional documentation is organized in `documentation/` subfolders
- Keep this index updated when adding new documentation
- Update version and date when making significant changes

**Last Organized**: 2025-11-21
**By**: Project Cleanup Agent
**Reason**: Centralized all documentation for better organization and discoverability

---

For any questions about documentation, refer to the main [CLAUDE.md](../CLAUDE.md) file or check the appropriate layer-specific documentation.
