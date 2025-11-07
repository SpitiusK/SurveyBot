# SurveyBot - AI Assistant Documentation

## Project Overview

**SurveyBot** is a Telegram-based survey management system built with .NET 8.0, implementing Clean Architecture principles. The system enables users to create surveys via Telegram, distribute them to respondents, collect responses, and analyze results through a REST API.

### Key Technologies
- **.NET 8.0** - Core framework
- **ASP.NET Core Web API** - REST API layer
- **Entity Framework Core 9.0** - ORM with PostgreSQL
- **PostgreSQL 15** - Primary database
- **Telegram.Bot 22.7.4** - Telegram Bot API integration
- **Serilog** - Structured logging
- **AutoMapper 12.0** - Object mapping
- **xUnit** - Testing framework
- **JWT Bearer Authentication** - API security

### Project Purpose
Enable survey creation and response collection through Telegram bot interface with comprehensive analytics and management capabilities via REST API.

---

## Solution Structure

The solution follows Clean Architecture with clear separation of concerns:

```
SurveyBot/
├── src/
│   ├── SurveyBot.Core/              # Domain Layer (No dependencies)
│   ├── SurveyBot.Infrastructure/    # Data Access Layer (depends on Core)
│   ├── SurveyBot.Bot/               # Telegram Bot Logic (depends on Core)
│   └── SurveyBot.API/               # Presentation Layer (depends on all)
├── tests/
│   └── SurveyBot.Tests/             # Unit and Integration Tests
├── documentation/                    # Comprehensive project documentation
├── docker-compose.yml               # PostgreSQL and pgAdmin containers
└── SurveyBot.sln                    # Visual Studio Solution

Dependency Flow: API → Infrastructure/Bot → Core
```

---

## Architecture Patterns

### Clean Architecture Layers

1. **SurveyBot.Core** (Domain Layer)
   - Contains: Entities, Interfaces, DTOs, Enums, Exceptions
   - No external dependencies
   - Pure business logic and domain models
   - All other projects depend on this

2. **SurveyBot.Infrastructure** (Data Access Layer)
   - Contains: DbContext, Repositories, EF Configurations, Migrations, Services
   - Depends on: Core only
   - Implements repository interfaces from Core
   - Handles all database operations

3. **SurveyBot.Bot** (Application Layer - Bot Logic)
   - Contains: Bot handlers, Command handlers, Update processing
   - Depends on: Core only
   - Implements Telegram bot interaction logic
   - Webhook and polling support

4. **SurveyBot.API** (Presentation Layer)
   - Contains: Controllers, Middleware, AutoMapper profiles, API models
   - Depends on: Core, Infrastructure, Bot
   - Exposes REST API endpoints
   - Handles HTTP requests/responses

### Key Design Patterns

1. **Repository Pattern**
   - Generic base repository: `IRepository<T>` / `GenericRepository<T>`
   - Specific repositories: `ISurveyRepository` / `SurveyRepository`
   - All repositories are scoped per HTTP request

2. **Service Pattern**
   - Business logic in services: `ISurveyService`, `IAuthService`, etc.
   - Services use repositories for data access
   - Services contain validation and business rules

3. **Dependency Injection**
   - Constructor injection throughout
   - Scoped lifetime for DbContext, Repositories, Services
   - Configured in `Program.cs` and extension methods

4. **AutoMapper for DTO Mapping**
   - Profiles: `SurveyMappingProfile`, `QuestionMappingProfile`, etc.
   - Value resolvers for complex mappings
   - Located in `SurveyBot.API/Mapping/`

---

## Database Schema

### Core Entities

1. **User** (Telegram users)
   - `Id` (PK, auto-increment)
   - `TelegramId` (unique, long) - Telegram's user ID
   - `Username`, `FirstName`, `LastName`
   - `LastLoginAt` (DateTime?)
   - `CreatedAt`, `UpdatedAt`
   - Navigation: `Surveys` (created by user)

2. **Survey**
   - `Id` (PK)
   - `Title`, `Description`
   - `CreatorId` (FK to User)
   - `IsActive` (bool) - Accept responses or not
   - `AllowMultipleResponses` (bool)
   - `ShowResults` (bool)
   - `CreatedAt`, `UpdatedAt`
   - Navigation: `Creator`, `Questions`, `Responses`

3. **Question**
   - `Id` (PK)
   - `SurveyId` (FK)
   - `QuestionText`
   - `QuestionType` (enum: Text, MultipleChoice, SingleChoice, YesNo, Rating)
   - `OrderIndex` (int) - Display order
   - `IsRequired` (bool)
   - `OptionsJson` (string, JSONB) - For choice-based questions
   - `CreatedAt`, `UpdatedAt`
   - Navigation: `Survey`, `Answers`

4. **Response** (User's survey submission)
   - `Id` (PK)
   - `SurveyId` (FK)
   - `RespondentTelegramId` (long) - Not FK, allows anonymous
   - `IsComplete` (bool)
   - `StartedAt`, `SubmittedAt`
   - Navigation: `Survey`, `Answers`

5. **Answer** (Individual question answer)
   - `Id` (PK)
   - `ResponseId` (FK)
   - `QuestionId` (FK)
   - `AnswerJson` (string, JSONB) - Stores answer data
   - `CreatedAt`
   - Navigation: `Response`, `Question`

### Entity Relationships

```
User (1) ──< Surveys (*)
Survey (1) ──< Questions (*)
Survey (1) ──< Responses (*)
Question (1) ──< Answers (*)
Response (1) ──< Answers (*)
```

### Database Configuration
- **Connection String**: `appsettings.json` → `ConnectionStrings:DefaultConnection`
- **Provider**: PostgreSQL via Npgsql
- **Migrations**: Located in `SurveyBot.Infrastructure/Migrations/`
- **Context**: `SurveyBotDbContext` with automatic timestamp updates

---

## Project Details by Layer

### SurveyBot.Core (Domain)

**Purpose**: Define domain models, business rules, and contracts (interfaces)

**Key Directories**:
- `Entities/` - Domain entities (User, Survey, Question, Response, Answer)
- `Interfaces/` - Repository and service contracts
- `DTOs/` - Data transfer objects organized by feature
  - `Survey/`, `Question/`, `Response/`, `Answer/`, `User/`, `Statistics/`, `Common/`, `Auth/`
- `Exceptions/` - Custom domain exceptions
- `Configuration/` - Settings models (JwtSettings)

**Important Interfaces**:
- `IRepository<T>` - Generic repository base
- `ISurveyRepository`, `IQuestionRepository`, `IResponseRepository`, `IUserRepository`, `IAnswerRepository`
- `ISurveyService`, `IQuestionService`, `IResponseService`, `IUserService`, `IAuthService`

**DTOs Pattern**:
- `CreateXxxDto` - For creation requests
- `UpdateXxxDto` - For update requests
- `XxxDto` - For responses (full details)
- `XxxListDto` - For list responses (summary)
- `PagedResultDto<T>` - For paginated responses

**Exceptions**:
- `SurveyNotFoundException`, `QuestionNotFoundException`, `ResponseNotFoundException`
- `SurveyValidationException`, `QuestionValidationException`
- `SurveyOperationException`, `InvalidAnswerFormatException`, `DuplicateResponseException`
- `UnauthorizedAccessException`

### SurveyBot.Infrastructure (Data Access)

**Purpose**: Implement data access, database context, and business logic services

**Key Directories**:
- `Data/` - DbContext and configurations
  - `SurveyBotDbContext` - Main database context
  - `Configurations/` - Fluent API entity configurations
  - `DataSeeder.cs` - Test data seeding
- `Repositories/` - Repository implementations
  - `GenericRepository<T>` - Base implementation
  - Specific repositories for each entity
- `Services/` - Business logic services
  - `AuthService` - JWT authentication
  - `SurveyService`, `QuestionService`, `ResponseService`, `UserService`
- `Migrations/` - EF Core migrations

**DbContext Features**:
- Automatic `CreatedAt`/`UpdatedAt` timestamp management
- Entity configurations via `IEntityTypeConfiguration<T>`
- Eager loading of navigation properties in repositories
- Sensitive data logging in Development

**Repository Pattern**:
- All repositories inherit from `GenericRepository<T>`
- Specific methods for common queries (e.g., `GetByIdWithQuestionsAsync`)
- Include navigation properties as needed
- Return `IEnumerable<T>` for collections

**Services**:
- Implement business logic and validation
- Use repositories for data access
- Throw domain exceptions for error conditions
- Return DTOs, not entities

### SurveyBot.Bot (Telegram Bot)

**Purpose**: Handle Telegram bot interactions, commands, and updates

**Key Directories**:
- `Configuration/` - Bot configuration model
- `Services/` - Core bot services
  - `BotService` - Bot client lifecycle management
  - `UpdateHandler` - Process incoming updates
  - `CommandRouter` - Route commands to handlers
- `Handlers/Commands/` - Command handlers
  - `StartCommandHandler`, `HelpCommandHandler`, `MySurveysCommandHandler`, `SurveysCommandHandler`
- `Interfaces/` - Bot-related interfaces
- `Extensions/` - DI registration extensions

**Bot Configuration** (`appsettings.json`):
```json
{
  "BotConfiguration": {
    "BotToken": "your-bot-token",
    "WebhookUrl": "https://yourdomain.com",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "your-secret",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000"
  }
}
```

**Command Handler Pattern**:
- Implement `ICommandHandler` interface
- Register in DI container
- `CommandRouter` dispatches to appropriate handler

**Webhook vs Polling**:
- Webhook: Production mode, set `UseWebhook: true`
- Polling: Development mode, set `UseWebhook: false`

### SurveyBot.API (Presentation)

**Purpose**: Expose REST API endpoints, handle HTTP requests, authentication

**Key Directories**:
- `Controllers/` - API controllers
  - `SurveysController`, `QuestionsController`, `ResponsesController`, `UsersController`
  - `AuthController` - Login/authentication
  - `BotController` - Webhook endpoint
  - `HealthController`, `TestErrorsController`
- `Middleware/` - Custom middleware
  - `GlobalExceptionMiddleware` - Centralized error handling
  - `RequestLoggingMiddleware` - Request/response logging
- `Models/` - API-specific models
  - `ApiResponse<T>` - Standard response wrapper
  - `ErrorResponse` - Error details
- `Mapping/` - AutoMapper profiles
  - `SurveyMappingProfile`, `QuestionMappingProfile`, etc.
  - `ValueResolvers/` - Custom mapping resolvers
- `Extensions/` - Service registration extensions
- `Services/` - Background services
  - `BackgroundTaskQueue` - Queue for webhook processing

**Program.cs Structure**:
1. Configure Serilog
2. Add Controllers
3. Configure AutoMapper
4. Register DbContext with PostgreSQL
5. Configure JWT Authentication
6. Register Repositories (Scoped)
7. Register Services (Scoped)
8. Register Telegram Bot Services
9. Add Health Checks
10. Configure Swagger/OpenAPI
11. Build app and configure middleware pipeline

**Authentication**:
- JWT Bearer tokens
- Configuration in `appsettings.json` → `JwtSettings`
- Required fields: `SecretKey`, `Issuer`, `Audience`, `ExpirationMinutes`
- Most endpoints require `[Authorize]` attribute
- User ID extracted from `ClaimTypes.NameIdentifier`

**API Response Pattern**:
```csharp
ApiResponse<T> {
    bool Success,
    string Message,
    T Data,
    Dictionary<string, object> Metadata
}
```

**Controllers Best Practices**:
- Use DTOs for requests/responses
- Return `ApiResponse<T>` wrapper
- Log operations
- Handle specific exceptions (catch specific before general)
- Extract user ID from JWT claims
- Use `[SwaggerOperation]` attributes for documentation

---

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login with Telegram credentials
- `POST /api/auth/refresh` - Refresh JWT token

### Surveys
- `POST /api/surveys` - Create survey
- `GET /api/surveys` - List user's surveys (paginated, filterable)
- `GET /api/surveys/{id}` - Get survey details with questions
- `PUT /api/surveys/{id}` - Update survey
- `DELETE /api/surveys/{id}` - Delete survey (soft/hard)
- `POST /api/surveys/{id}/activate` - Activate survey
- `POST /api/surveys/{id}/deactivate` - Deactivate survey
- `GET /api/surveys/{id}/statistics` - Get survey statistics

### Questions
- `POST /api/surveys/{surveyId}/questions` - Add question
- `GET /api/surveys/{surveyId}/questions` - List questions
- `GET /api/questions/{id}` - Get question details
- `PUT /api/questions/{id}` - Update question
- `DELETE /api/questions/{id}` - Delete question
- `POST /api/surveys/{surveyId}/questions/reorder` - Reorder questions

### Responses
- `POST /api/surveys/{surveyId}/responses/start` - Start response
- `POST /api/responses/{responseId}/answers` - Submit answer
- `POST /api/responses/{responseId}/complete` - Complete response
- `GET /api/surveys/{surveyId}/responses` - List survey responses
- `GET /api/responses/{id}` - Get response details

### Users
- `GET /api/users/me` - Get current user profile
- `PUT /api/users/me` - Update user profile

### Bot
- `POST /api/bot/webhook` - Telegram webhook endpoint (if enabled)

### Health
- `GET /health/db` - Database health check
- `GET /health/db/details` - Detailed DB health info

---

## Testing

### Test Project Structure
```
SurveyBot.Tests/
├── Unit/                      # Unit tests (isolated components)
│   ├── Repositories/          # Repository tests
│   ├── Services/              # Service tests
│   └── Validators/            # Validation tests
├── Integration/               # Integration tests (with database)
│   ├── Controllers/           # API endpoint tests
│   ├── Database/              # Database operation tests
│   └── Scenarios/             # End-to-end scenarios
├── Fixtures/                  # Test fixtures and helpers
│   ├── DatabaseFixture.cs     # In-memory database setup
│   └── TestDataBuilder.cs     # Test data creation
└── Helpers/                   # Test utilities
```

### Testing Stack
- **xUnit** - Test framework
- **Moq** - Mocking framework
- **FluentAssertions** - Assertion library
- **EF Core InMemory** - In-memory database for tests
- **Microsoft.AspNetCore.Mvc.Testing** - API integration tests

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test project
dotnet test tests/SurveyBot.Tests/SurveyBot.Tests.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~SurveyServiceTests"
```

### Test Patterns
1. **Arrange-Act-Assert** structure
2. Use `DatabaseFixture` for tests requiring database
3. Mock repositories/services for unit tests
4. Use `WebApplicationFactory` for integration tests
5. Test both success and failure scenarios

---

## Development Workflow

### Initial Setup
1. Install .NET 8.0 SDK
2. Install Docker Desktop (for PostgreSQL)
3. Clone repository
4. Copy `.env.example` to `.env` and configure
5. Start Docker: `docker-compose up -d`
6. Navigate to API: `cd src/SurveyBot.API`
7. Apply migrations: `dotnet ef database update`
8. Run API: `dotnet run`

### Database Migrations
```bash
# Add new migration (from SurveyBot.API directory)
cd src/SurveyBot.API
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script
```

### Configuration Files

**appsettings.json** (SurveyBot.API):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=surveybot_db;Username=surveybot_user;Password=surveybot_dev_password"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-chars",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Client",
    "ExpirationMinutes": 60
  },
  "BotConfiguration": {
    "BotToken": "your-telegram-bot-token",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000"
  },
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

**.env** (Docker):
```bash
POSTGRES_DB=surveybot_db
POSTGRES_USER=surveybot_user
POSTGRES_PASSWORD=surveybot_dev_password
PGADMIN_DEFAULT_EMAIL=admin@surveybot.local
PGADMIN_DEFAULT_PASSWORD=admin123
```

### Adding New Features

#### Adding a New Entity
1. Create entity in `SurveyBot.Core/Entities/`
2. Add DbSet to `SurveyBotDbContext`
3. Create entity configuration in `Infrastructure/Data/Configurations/`
4. Apply configuration in `OnModelCreating`
5. Create migration: `dotnet ef migrations add AddNewEntity`
6. Apply migration: `dotnet ef database update`
7. Create repository interface in `Core/Interfaces/`
8. Implement repository in `Infrastructure/Repositories/`
9. Register repository in `Program.cs`

#### Adding a New API Endpoint
1. Create DTOs in `SurveyBot.Core/DTOs/`
2. Create/update service interface in `Core/Interfaces/`
3. Implement service in `Infrastructure/Services/`
4. Create AutoMapper profile in `API/Mapping/`
5. Create/update controller in `API/Controllers/`
6. Add XML documentation comments
7. Test with Swagger UI

#### Adding a New Bot Command
1. Create command handler in `SurveyBot.Bot/Handlers/Commands/`
2. Implement `ICommandHandler` interface
3. Register handler in DI (`BotServiceExtensions`)
4. Add command to `CommandRouter`
5. Test with Telegram bot

---

## Coding Standards

### C# Conventions
- **Naming**: PascalCase for classes/methods, camelCase for parameters/locals
- **Async**: All async methods end with `Async` suffix
- **Nullability**: Nullable reference types enabled
- **Documentation**: XML comments for public APIs
- **Constants**: Use `const` or `static readonly` as appropriate

### File Organization
- One class per file
- File name matches class name
- Organize using statements (System first, then third-party, then project)
- Use `namespace` file-scoped declaration (C# 10+)

### Entity Framework
- Use async methods: `ToListAsync()`, `FirstOrDefaultAsync()`, etc.
- Include navigation properties explicitly: `.Include(x => x.Navigation)`
- Use `AsNoTracking()` for read-only queries
- Always use parameterized queries (EF does this automatically)

### API Controllers
- Return `ActionResult<T>` or `IActionResult`
- Use `[ApiController]` attribute
- Use route attributes: `[Route("api/[controller]")]`
- Use HTTP verb attributes: `[HttpGet]`, `[HttpPost]`, etc.
- Add `[ProducesResponseType]` for Swagger documentation
- Wrap responses in `ApiResponse<T>`

### Error Handling
- Throw domain exceptions in services
- Catch specific exceptions in controllers
- Use `GlobalExceptionMiddleware` for unhandled exceptions
- Log errors with context
- Return appropriate HTTP status codes

### Dependency Injection
- Use constructor injection
- Register services with appropriate lifetime (Scoped for most)
- Avoid service locator pattern
- Register in `Program.cs` or extension methods

---

## Common Tasks Reference

### Debugging
- **API**: Set `SurveyBot.API` as startup project
- **Logs**: Check console or Serilog output
- **Database**: Use pgAdmin at http://localhost:5050
- **Swagger**: http://localhost:5000/swagger
- **EF Queries**: Enable logging in `appsettings.json`

### Database Access
- **pgAdmin**: http://localhost:5050
  - Email: admin@surveybot.local
  - Password: admin123
- **Connection**:
  - Host: localhost
  - Port: 5432
  - Database: surveybot_db
  - Username: surveybot_user
  - Password: surveybot_dev_password

### Telegram Bot Testing
1. Get token from @BotFather
2. Add to `appsettings.json` → `BotConfiguration:BotToken`
3. Set `UseWebhook: false` for development
4. Run API
5. Message your bot on Telegram

### Troubleshooting

**Database connection fails**:
- Ensure Docker is running: `docker ps`
- Check connection string matches `.env`
- Verify PostgreSQL container is healthy

**Migrations fail**:
- Ensure you're in `SurveyBot.API` directory
- Check EF Core tools installed: `dotnet tool list -g`
- Install if needed: `dotnet tool install --global dotnet-ef`

**Bot not responding**:
- Verify bot token is correct
- Check bot initialization logs
- For webhook mode, ensure webhook URL is accessible

**JWT authentication fails**:
- Verify `SecretKey` is at least 32 characters
- Check token hasn't expired
- Ensure `Issuer` and `Audience` match

---

## Project Status

### Completed Features
- Clean Architecture solution structure
- Database schema and EF Core setup
- Repository pattern implementation
- Service layer with business logic
- REST API with full CRUD operations
- JWT authentication
- AutoMapper configuration
- Global exception handling
- Request logging
- Health checks
- Swagger/OpenAPI documentation
- Docker Compose setup
- Basic Telegram bot infrastructure
- Unit and integration testing setup

### In Progress
- Telegram bot conversation flows
- Survey response collection via bot
- Advanced analytics and reporting

### Planned
- Admin panel (React SPA)
- Survey export functionality (CSV, Excel, PDF)
- Response rate tracking
- Survey templates
- Multi-language support
- CI/CD pipeline

---

## Important Notes for AI Assistants

### When Making Changes
1. **Always preserve Clean Architecture**: Don't add business logic to controllers
2. **Use existing patterns**: Follow repository/service patterns
3. **Update migrations**: Create migration after entity changes
4. **Update DTOs**: Keep DTOs in sync with entities
5. **Add tests**: Write tests for new functionality
6. **Update AutoMapper**: Add mapping profiles for new DTOs
7. **Document APIs**: Add XML comments and Swagger attributes

### When Reading Code
1. **Start with Core**: Understand entities and interfaces first
2. **Follow dependencies**: Core → Infrastructure/Bot → API
3. **Check DI registration**: Look in `Program.cs` and extension methods
4. **Review DTOs**: Understand data contracts
5. **Check AutoMapper**: Understand entity-to-DTO mappings

### When Debugging
1. **Check logs**: Serilog provides detailed logging
2. **Use Swagger**: Test API endpoints interactively
3. **Verify DI**: Check `DIVerifier` output in Development
4. **Database state**: Use pgAdmin to inspect data
5. **Migration history**: Check `__EFMigrationsHistory` table

### Key Files to Reference
- `Program.cs` - Application startup and DI configuration
- `SurveyBotDbContext.cs` - Database context and entity configuration
- `DI-STRUCTURE.md` - Detailed DI documentation
- `README.md` - Project overview and quick start
- `documentation/` - Comprehensive documentation by topic

---

## Additional Resources

### Documentation Files
- `README.md` - Project overview and quick start guide
- `DI-STRUCTURE.md` - Dependency injection structure
- `DOCKER-STARTUP-GUIDE.md` - Docker setup instructions
- `QUICK-START-DATABASE.md` - Database setup guide
- `documentation/DEVELOPER_ONBOARDING.md` - Complete developer guide
- `documentation/TROUBLESHOOTING.md` - Common issues and solutions
- `documentation/architecture/` - Architecture diagrams and decisions
- `documentation/database/` - Database schema and design
- `documentation/api/` - API reference documentation

### External Resources
- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Telegram Bot API](https://core.telegram.org/bots/api)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

**Last Updated**: 2025-11-07
**Version**: 1.0.0-MVP
**Target Framework**: .NET 8.0
