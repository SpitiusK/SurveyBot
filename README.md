# Telegram Survey Bot

A comprehensive Telegram-based survey management system with conditional question flow, multimedia support, and React admin panel for analytics.

## Table of Contents

- [Overview](#overview)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Architecture](#architecture)
- [Development](#development)
- [Testing](#testing)
- [Documentation](#documentation)
- [License](#license)

## Overview

SurveyBot is a full-featured survey platform that enables users to create surveys via Telegram bot or web interface, distribute them with shareable codes, collect responses with conditional branching, and analyze results through an admin panel. Built with Clean Architecture and Domain-Driven Design principles for maintainability and scalability.

### Key Features

- **Conditional Question Flow** - Dynamic branching based on user answers with cycle detection (v1.4.0)
- **Telegram Bot Interface** - Create and take surveys with conditional navigation
- **Multiple Question Types** - Text, single choice, multiple choice, rating scales
- **Multimedia Support** - Images (JPG, PNG), videos (MP4), audio (MP3, OGG), documents (PDF)
- **Survey Code Sharing** - 6-character alphanumeric codes (Base36)
- **Admin Panel** - React-based dashboard for analytics and management
- **Real-time Analytics** - Statistics, charts, CSV export
- **JWT Authentication** - Secure token-based authentication
- **Value Object Pattern** - Type-safe domain modeling with DDD principles

## Technology Stack

### Backend
- **.NET 8.0** - Modern, cross-platform framework
- **ASP.NET Core Web API** - REST API with 10 controllers
- **Entity Framework Core 9.0** - ORM with owned types for value objects
- **PostgreSQL 15** - Relational database with JSONB support
- **Npgsql** - PostgreSQL provider for EF Core

### Telegram Integration
- **Telegram.Bot 22.7.4** - Official Telegram Bot API library
- **Polling/Webhook** - Flexible message processing modes

### Frontend
- **React 19.2** - Modern UI framework
- **TypeScript** - Type-safe JavaScript
- **Material-UI** - Component library

### Development Tools
- **Serilog** - Structured logging
- **AutoMapper 12.0** - Object-to-object mapping with value object support
- **Swashbuckle** - API documentation (Swagger/OpenAPI)
- **xUnit** - Unit and integration testing
- **Docker & Docker Compose** - Containerization

## Project Structure

```
SurveyBot/
├── src/
│   ├── SurveyBot.Core/              # Domain layer (ZERO dependencies)
│   │   ├── Entities/                # 7 entities (User, Survey, Question, QuestionOption, Response, Answer, MediaFile)
│   │   ├── Interfaces/              # 16 repository/service interfaces
│   │   ├── DTOs/                    # 42+ data transfer objects
│   │   ├── ValueObjects/            # NextQuestionDeterminant (v1.4.0)
│   │   ├── Exceptions/              # Domain exceptions (SurveyCycleException)
│   │   └── Enums/                   # QuestionType, MediaType, SurveyStatus
│   │
│   ├── SurveyBot.Infrastructure/    # Data access layer
│   │   ├── Data/                    # DbContext with owned type configurations
│   │   │   └── Configurations/      # Entity configurations with value objects
│   │   ├── Repositories/            # Generic + specialized repositories
│   │   ├── Services/                # QuestionService, ResponseService, SurveyValidationService
│   │   └── Migrations/              # EF Core migrations (clean slate approach)
│   │
│   ├── SurveyBot.Bot/               # Telegram bot logic
│   │   ├── Handlers/                # Message, callback, and survey response handlers
│   │   ├── Services/                # TelegramBotService (polling/webhook)
│   │   ├── Models/                  # ConversationState with VisitedQuestions
│   │   └── Utilities/               # SurveyNavigationHelper for conditional flow
│   │
│   └── SurveyBot.API/               # Web API layer
│       ├── Controllers/             # 10 controllers (incl. QuestionFlowController)
│       ├── Middleware/              # Exception handling, request logging
│       ├── Mapping/                 # AutoMapper profiles with value object support
│       └── Extensions/              # Service registration extensions
│
├── frontend/                        # React admin panel
│   ├── src/
│   │   ├── components/              # Survey builder, charts, forms
│   │   ├── pages/                   # Dashboard, survey management
│   │   └── services/                # API integration
│   └── package.json
│
├── tests/
│   └── SurveyBot.Tests/             # Test project
│       ├── Unit/                    # Unit tests (services, handlers)
│       ├── Integration/             # Integration tests (API, database)
│       └── Fixtures/                # Test data and fixtures
│
├── documentation/                    # Centralized documentation hub
│   ├── INDEX.md                     # Documentation catalog
│   ├── api/                         # API documentation
│   ├── architecture/                # Architecture documentation
│   ├── bot/                         # Bot user guides
│   ├── database/                    # Database schema and design
│   └── testing/                     # Testing guides
│
├── docker-compose.yml               # Docker services configuration
├── .env.example                     # Environment variables template
├── CLAUDE.md                        # AI assistant documentation
└── SurveyBot.sln                    # Visual Studio solution file
```

## Getting Started

### Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 8.0 SDK** or later ([Download](https://dotnet.microsoft.com/download))
- **Docker Desktop** ([Download](https://www.docker.com/products/docker-desktop))
- **Git** for version control
- **Visual Studio 2022** or **VS Code** (recommended)
- **Telegram Bot Token** from [@BotFather](https://t.me/botfather)

### Quick Start with Docker

The fastest way to get started is using Docker Compose:

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd SurveyBot
   ```

2. **Configure environment variables**
   ```bash
   cp .env.example .env
   ```
   Edit `.env` and add your Telegram bot token:
   ```
   TELEGRAM_BOT_TOKEN=your_bot_token_here
   DATABASE_CONNECTION=Host=postgres;Port=5432;Database=surveybot_db;Username=surveybot_user;Password=surveybot_dev_password
   ```

3. **Start services with Docker Compose**
   ```bash
   docker-compose up -d
   ```
   This starts:
   - PostgreSQL database on port 5432
   - pgAdmin on port 5050 (optional, for database management)

4. **Apply database migrations**
   ```bash
   cd src/SurveyBot.API
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the API**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger
   - pgAdmin: http://localhost:5050 (admin@surveybot.local / admin123)

### Manual Setup (Without Docker)

If you prefer to set up PostgreSQL manually:

1. **Install PostgreSQL 15+**
   - Download from [postgresql.org](https://www.postgresql.org/download/)
   - Create a database named `surveybot_db`

2. **Update connection string**
   Edit `src/SurveyBot.API/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=surveybot_db;Username=your_user;Password=your_password"
     }
   }
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Apply migrations**
   ```bash
   cd src/SurveyBot.API
   dotnet ef database update
   ```

5. **Run the API**
   ```bash
   dotnet run
   ```

### Obtaining a Telegram Bot Token

1. Open Telegram and search for [@BotFather](https://t.me/botfather)
2. Send `/newbot` command
3. Follow the instructions to create your bot
4. Copy the API token provided
5. Add the token to your `.env` file or `appsettings.json`

## Architecture

This project follows **Clean Architecture** and **Domain-Driven Design** principles with clear separation of concerns:

### Layer Overview

```
┌─────────────────────────────────────────────────┐
│           SurveyBot.API (v1.4.0)                │
│   REST API, 10 Controllers, Middleware          │
│   NEW: QuestionFlowController                   │
└───────────────┬─────────────────────────────────┘
                │ depends on
                ▼
┌─────────────────────────────────────────────────┐
│   SurveyBot.Infrastructure (v1.4.1)             │
│   Database, Repositories, Services              │
│   NEW: Owned Types, Cycle Detection (DFS)       │
└───────────────┬─────────────────────────────────┘
                │ depends on
                ▼
┌─────────────────────────────────────────────────┐
│       SurveyBot.Core (v1.4.0)                   │
│   7 Entities, 16 Interfaces, 42+ DTOs           │
│   NEW: QuestionOption, Value Objects            │
│   ZERO DEPENDENCIES ✓                           │
└───────────────▲─────────────────────────────────┘
                │ depends on
┌───────────────┴─────────────────────────────────┐
│       SurveyBot.Bot (v1.4.0)                    │
│   Telegram Bot, Handlers, State Mgmt            │
│   NEW: Conditional Flow, Navigation Helper      │
└─────────────────────────────────────────────────┘
```

### Architectural Patterns

SurveyBot implements 8 core design patterns:

1. **Clean Architecture** - Zero-dependency core, onion-style layers
2. **Repository Pattern** - Generic + specialized repositories with Include support
3. **Service Layer Pattern** - Business logic encapsulation
4. **DTO Pattern** - 42+ DTOs for API contracts
5. **Value Object Pattern** - NextQuestionDeterminant (type-safe, immutable)
6. **Owned Entity Types** - EF Core owned types for value objects
7. **Graph Algorithms** - DFS-based cycle detection for survey validation
8. **Strategy Pattern** - Polymorphic question handling

### Core Principles

1. **Dependency Inversion** - High-level modules don't depend on low-level modules
2. **Single Responsibility** - Each layer has a distinct purpose
3. **Interface Segregation** - Repository pattern with clean interfaces
4. **Testability** - Business logic isolated from infrastructure
5. **Domain-Driven Design** - Value objects, aggregates, domain services

### Entity Relationships

```
User (1) ──creates──> Survey (*) ──contains──> Question (*)
                        │                        │
                        │                        ├──> DefaultNext (0..1) ─────┐
                        │                        │                           │
                        │                        └──> QuestionOption (*) ────┤
                        │                             (for choice questions) │
                        │                             └──> NextQuestion (0..1)┘
                        │
                        └──receives──> Response (*)
                                        │
                                        ├──> VisitedQuestionIds (List<int>)
                                        │
                                        └──contains──> Answer (*)
```

For detailed architecture documentation, see [documentation/architecture/ARCHITECTURE.md](documentation/architecture/ARCHITECTURE.md)

## Development

### Building the Solution

```bash
# Build all projects
dotnet build

# Build specific project
dotnet build src/SurveyBot.API/SurveyBot.API.csproj

# Build in Release mode
dotnet build -c Release
```

### Running the Application

```bash
# Run the API
cd src/SurveyBot.API
dotnet run

# Run with specific environment
dotnet run --environment Development

# Watch mode (auto-restart on changes)
dotnet watch run
```

### Database Migrations

```bash
# Add a new migration
cd src/SurveyBot.API
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script
```

### Code Style and Formatting

```bash
# Format code
dotnet format

# Analyze code
dotnet build /p:TreatWarningsAsErrors=true
```

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true

# Run specific test project
dotnet test tests/SurveyBot.Tests/SurveyBot.Tests.csproj

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~SurveyBot.Tests.SurveyServiceTests"
```

### Test Organization

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions and database operations
- **API Tests**: Test HTTP endpoints and responses

## Documentation

Comprehensive documentation is organized in two locations:

### Centralized Documentation Hub (`documentation/`)

- **[Documentation Index](documentation/INDEX.md)** - Complete catalog of all documentation
- **[Navigation Guide](documentation/NAVIGATION.md)** - Role-based navigation (developer, DevOps, user, AI)
- **[Developer Onboarding](documentation/DEVELOPER_ONBOARDING.md)** - New developer quick start
- **[Troubleshooting Guide](documentation/TROUBLESHOOTING.md)** - Common issues and solutions

**By Topic**:
- **Architecture**: [Architecture Overview](documentation/architecture/ARCHITECTURE.md)
- **API**: [API Reference](documentation/api/API_REFERENCE.md) | [Quick Reference](documentation/api/QUICK-REFERENCE.md)
- **Bot**: [User Guide](documentation/bot/BOT_USER_GUIDE.md) | [Commands](documentation/bot/BOT_COMMAND_REFERENCE.md)
- **Database**: [ER Diagram](documentation/database/ER_DIAGRAM.md) | [Quick Start](documentation/database/QUICK-START-DATABASE.md)
- **Deployment**: [Docker Guide](documentation/deployment/DOCKER-STARTUP-GUIDE.md)
- **Testing**: [Test Summary](documentation/testing/TEST_SUMMARY.md)

### Layer-Specific Documentation (CLAUDE.md files)

Technical implementation details for AI assistants and developers:

- **[Root CLAUDE.md](CLAUDE.md)** - Project overview, quick start, configuration
- **[Core Layer](src/SurveyBot.Core/CLAUDE.md)** - Entities, interfaces, DTOs, value objects
- **[Infrastructure Layer](src/SurveyBot.Infrastructure/CLAUDE.md)** - Database, repositories, services
- **[Bot Layer](src/SurveyBot.Bot/CLAUDE.md)** - Telegram handlers, state management
- **[API Layer](src/SurveyBot.API/CLAUDE.md)** - Controllers, middleware, authentication
- **[Frontend](frontend/CLAUDE.md)** - React admin panel

### Quick References

- **API Endpoints**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health/db
- **pgAdmin**: http://localhost:5050

## Project Status

### Completed (v1.4.1)

**Core Features**
- [x] Solution and project structure with Clean Architecture
- [x] Database schema design with PostgreSQL
- [x] Entity Framework Core 9.0 with owned types
- [x] Repository pattern (generic + specialized)
- [x] Dependency injection configuration
- [x] 10 API controllers with full CRUD
- [x] Global exception handling middleware
- [x] Request logging middleware
- [x] Health checks
- [x] Swagger/OpenAPI documentation
- [x] Docker Compose setup
- [x] Comprehensive documentation

**Telegram Bot (v1.4.0)**
- [x] Complete Telegram bot handler implementation
- [x] Bot conversation state management with VisitedQuestions
- [x] Conditional question flow navigation
- [x] SurveyNavigationHelper for branching logic
- [x] Multimedia support (images, videos, audio, documents)
- [x] Polling and webhook modes

**Admin Panel**
- [x] React 19.2 + TypeScript frontend
- [x] Survey builder with drag-and-drop
- [x] Question flow visualization
- [x] Analytics dashboard
- [x] JWT authentication integration

**Advanced Features (v1.4.0+)**
- [x] JWT authentication
- [x] Conditional question flow with cycle detection
- [x] Value objects (NextQuestionDeterminant)
- [x] DFS-based survey validation
- [x] QuestionFlowController for flow configuration
- [x] Survey code sharing (6-char Base36)

### In Progress
- [ ] CSV export functionality
- [ ] Advanced analytics charts
- [ ] CI/CD pipeline

### Planned
- [ ] Production deployment configuration
- [ ] Rate limiting
- [ ] Multi-language support

## Common Development Tasks

### Adding a New Entity

1. Create entity in `SurveyBot.Core/Entities/`
2. Add DbSet to `SurveyBotDbContext`
3. Create entity configuration in `Infrastructure/Data/Configurations/`
4. Create migration: `dotnet ef migrations add AddNewEntity`
5. Apply migration: `dotnet ef database update`
6. Create repository interface in `Core/Interfaces/`
7. Implement repository in `Infrastructure/Repositories/`
8. Add controller if needed

### Adding Conditional Flow Logic

1. Update `QuestionOption` entity with `NextQuestionId` property
2. Ensure `NextQuestionDeterminant` value object is used correctly
3. Update `QuestionFlowController` for flow configuration
4. Use `SurveyValidationService` for cycle detection
5. Update bot handlers via `SurveyNavigationHelper`

### Adding a New API Endpoint

1. Create/update controller in `SurveyBot.API/Controllers/`
2. Add DTOs in `SurveyBot.Core/DTOs/`
3. Add AutoMapper profile in `API/Mapping/`
4. Implement business logic in service
5. Add XML documentation comments
6. Test with Swagger UI

### Debugging

- **API Debugging**: Set `SurveyBot.API` as startup project in Visual Studio
- **Database Queries**: Enable EF Core logging in `appsettings.json`
- **Bot Messages**: Check Serilog console output
- **Database**: Use pgAdmin at http://localhost:5050
- **Conditional Flow**: Check `SurveyValidationService` logs for cycle detection

## Environment Variables

Create a `.env` file based on `.env.example`:

```bash
# Telegram Bot Configuration
TELEGRAM_BOT_TOKEN=your_bot_token_here
TELEGRAM_WEBHOOK_URL=https://your-domain.com/api/telegram/webhook

# Database Configuration
DATABASE_CONNECTION=Host=postgres;Port=5432;Database=surveybot_db;Username=surveybot_user;Password=surveybot_dev_password

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5000

# Logging
SERILOG_MINIMUM_LEVEL=Information
```

## Troubleshooting

### Common Issues

**Database connection fails**
- Ensure PostgreSQL is running: `docker-compose ps`
- Check connection string in `appsettings.json`
- Verify database exists: Connect via pgAdmin

**Migrations not applying**
- Delete migrations folder and start fresh
- Ensure you're in the correct directory (`src/SurveyBot.API`)
- Check EF Core tools are installed: `dotnet tool install --global dotnet-ef`

**Telegram bot not responding**
- Verify bot token is correct
- Check webhook is set up (if using webhook mode)
- Review logs for errors

For more detailed troubleshooting, see [documentation/TROUBLESHOOTING.md](documentation/TROUBLESHOOTING.md)

## Contributing

This is an MVP project. Follow these guidelines:

1. Keep it simple - avoid over-engineering
2. Follow Clean Architecture principles
3. Write tests for business logic
4. Document public APIs with XML comments
5. Use standard .NET conventions

## Support

For questions or issues:
- Review documentation in `documentation/` folder
- Check existing issues in the repository
- Contact the development team

## License

TBD

---

**Last Updated**: 2025-11-25
**Version**: 1.4.1
**Status**: Active Development
