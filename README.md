# Telegram Survey Bot MVP

A Telegram bot application for creating and managing surveys with an admin panel for analytics and management.

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

The Telegram Survey Bot MVP enables users to create surveys directly from Telegram, distribute them to respondents, and analyze results through an admin panel. The system is built with Clean Architecture principles, ensuring maintainability and testability.

### Key Features

- Create surveys via Telegram bot interface
- Multiple question types (text, multiple choice, single choice, yes/no, rating)
- Survey distribution through Telegram
- Response collection and validation
- Admin panel for analytics and management
- Real-time response tracking
- Export capabilities for survey results

## Technology Stack

### Backend
- **.NET 8.0** - Modern, cross-platform framework
- **ASP.NET Core Web API** - REST API endpoints
- **Entity Framework Core 8.0** - Object-relational mapping
- **PostgreSQL 15** - Relational database
- **Npgsql** - PostgreSQL provider for EF Core

### Telegram Integration
- **Telegram.Bot 19.0+** - Official Telegram Bot API library
- **Webhook-based** - Real-time message processing

### Development Tools
- **Serilog** - Structured logging
- **Swashbuckle** - API documentation (Swagger/OpenAPI)
- **xUnit** - Unit and integration testing
- **Docker & Docker Compose** - Containerization

## Project Structure

```
SurveyBot/
├── src/
│   ├── SurveyBot.Core/              # Domain layer (entities, interfaces)
│   │   ├── Entities/                # Domain entities
│   │   ├── Interfaces/              # Repository and service interfaces
│   │   └── Enums/                   # Domain enumerations
│   │
│   ├── SurveyBot.Infrastructure/    # Data access layer
│   │   ├── Data/                    # DbContext and configurations
│   │   ├── Repositories/            # Repository implementations
│   │   └── Migrations/              # EF Core migrations
│   │
│   ├── SurveyBot.Bot/               # Telegram bot logic
│   │   ├── Handlers/                # Message and callback handlers
│   │   ├── Services/                # Bot business logic
│   │   └── States/                  # Conversation state management
│   │
│   └── SurveyBot.API/               # Web API layer
│       ├── Controllers/             # REST API endpoints
│       ├── Middleware/              # Custom middleware
│       ├── Models/                  # DTOs and request/response models
│       └── Extensions/              # Service registration extensions
│
├── tests/
│   └── SurveyBot.Tests/             # Test project
│       ├── Unit/                    # Unit tests
│       ├── Integration/             # Integration tests
│       └── Fixtures/                # Test data and fixtures
│
├── documentation/                    # Project documentation
│   ├── database/                    # Database schema and design
│   ├── architecture/                # Architecture documentation
│   └── api/                         # API documentation
│
├── docker-compose.yml               # Docker services configuration
├── .env.example                     # Environment variables template
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

This project follows **Clean Architecture** principles with clear separation of concerns:

### Layer Overview

```
┌─────────────────────────────────────────────────┐
│         SurveyBot.API (Presentation)            │
│  - Controllers, Middleware, API Models          │
│  - Dependency: Core, Infrastructure, Bot        │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│     SurveyBot.Bot (Application - Bot Logic)     │
│  - Telegram handlers, Bot services              │
│  - Dependency: Core only                        │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│  SurveyBot.Infrastructure (Infrastructure)      │
│  - EF Core, Repositories, Data Access           │
│  - Dependency: Core only                        │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│         SurveyBot.Core (Domain Layer)           │
│  - Entities, Interfaces, Business Rules         │
│  - No dependencies on other projects            │
└─────────────────────────────────────────────────┘
```

### Core Principles

1. **Dependency Inversion**: High-level modules don't depend on low-level modules
2. **Single Responsibility**: Each layer has a distinct purpose
3. **Interface Segregation**: Repository pattern with clean interfaces
4. **Testability**: Business logic isolated from infrastructure

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

Comprehensive documentation is available in the `documentation/` directory:

### Available Documentation

- **[Developer Onboarding Guide](documentation/DEVELOPER_ONBOARDING.md)** - Complete setup guide for new developers
- **[Architecture Documentation](documentation/architecture/ARCHITECTURE.md)** - System design and patterns
- **[Database Documentation](documentation/database/README.md)** - Database schema, ER diagrams, and query patterns
- **[API Documentation](documentation/api/API_REFERENCE.md)** - REST API endpoints and examples
- **[Troubleshooting Guide](documentation/TROUBLESHOOTING.md)** - Common issues and solutions

### Quick References

- **Setup Instructions**: See [DEVELOPER_ONBOARDING.md](documentation/DEVELOPER_ONBOARDING.md)
- **Database Schema**: See [database/ER_DIAGRAM.md](documentation/database/ER_DIAGRAM.md)
- **API Endpoints**: Run the app and visit http://localhost:5000/swagger
- **Docker Setup**: See [DOCKER-STARTUP-GUIDE.md](DOCKER-STARTUP-GUIDE.md)

## Project Status

### Completed
- [x] Solution and project structure
- [x] Database schema design and implementation
- [x] Entity Framework Core setup with migrations
- [x] Repository pattern implementation
- [x] Dependency injection configuration
- [x] API controllers and endpoints
- [x] Global exception handling middleware
- [x] Request logging middleware
- [x] Health checks
- [x] Swagger/OpenAPI documentation
- [x] Docker Compose setup
- [x] Core documentation

### In Progress
- [ ] Telegram bot handler implementation
- [ ] Bot conversation state management
- [ ] Admin panel (React)

### Planned
- [ ] JWT authentication
- [ ] Survey export functionality
- [ ] Advanced analytics
- [ ] Deployment configuration
- [ ] CI/CD pipeline

## Common Development Tasks

### Adding a New Entity

1. Create entity in `SurveyBot.Core/Entities/`
2. Add DbSet to `ApplicationDbContext`
3. Create migration: `dotnet ef migrations add AddNewEntity`
4. Apply migration: `dotnet ef database update`
5. Create repository interface and implementation
6. Add controller if needed

### Adding a New API Endpoint

1. Create/update controller in `SurveyBot.API/Controllers/`
2. Add DTOs in `SurveyBot.API/Models/`
3. Implement business logic in service/repository
4. Add XML documentation comments
5. Test with Swagger UI

### Debugging

- **API Debugging**: Set `SurveyBot.API` as startup project in Visual Studio
- **Database Queries**: Enable EF Core logging in `appsettings.json`
- **Bot Messages**: Check Serilog console output
- **Database**: Use pgAdmin at http://localhost:5050

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

**Last Updated**: 2025-11-06
**Version**: 1.0.0-MVP
**Status**: Active Development
