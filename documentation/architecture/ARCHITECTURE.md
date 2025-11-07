# Architecture Documentation
## Telegram Survey Bot MVP

### Version: 1.0.0
### Last Updated: 2025-11-06

---

## Table of Contents

1. [Overview](#overview)
2. [Clean Architecture Principles](#clean-architecture-principles)
3. [Project Layer Breakdown](#project-layer-breakdown)
4. [Design Patterns](#design-patterns)
5. [Dependency Injection](#dependency-injection)
6. [Data Flow](#data-flow)
7. [Technology Decisions](#technology-decisions)
8. [Security Architecture](#security-architecture)
9. [Scalability Considerations](#scalability-considerations)

---

## Overview

The Telegram Survey Bot follows **Clean Architecture** (also known as Onion Architecture or Hexagonal Architecture), a software design philosophy that emphasizes separation of concerns and dependency inversion.

### Core Principles

1. **Independence of frameworks**: Business logic doesn't depend on external libraries
2. **Testability**: Business rules can be tested without UI, database, or external services
3. **Independence of UI**: The UI can change without changing the rest of the system
4. **Independence of database**: Business rules are not bound to the database
5. **Independence of external services**: Business rules don't know about the outside world

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Presentation Layer                     │
│                                                           │
│  ┌─────────────────────────────────────────────────┐   │
│  │           SurveyBot.API                         │   │
│  │  - REST API Controllers                          │   │
│  │  - Middleware (Exceptions, Logging)              │   │
│  │  - API Models (DTOs)                             │   │
│  │  - Swagger/OpenAPI Configuration                 │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                   Application Layer                      │
│                                                           │
│  ┌─────────────────────────────────────────────────┐   │
│  │           SurveyBot.Bot                         │   │
│  │  - Telegram Message Handlers                     │   │
│  │  - Conversation State Management                 │   │
│  │  - Bot Command Processing                        │   │
│  │  - Bot Services                                  │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                Infrastructure Layer                      │
│                                                           │
│  ┌─────────────────────────────────────────────────┐   │
│  │      SurveyBot.Infrastructure                   │   │
│  │  - Entity Framework Core DbContext               │   │
│  │  - Repository Implementations                    │   │
│  │  - Database Migrations                           │   │
│  │  - External Service Integrations                 │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                      Domain Layer                        │
│                                                           │
│  ┌─────────────────────────────────────────────────┐   │
│  │           SurveyBot.Core                        │   │
│  │  - Domain Entities (User, Survey, etc.)          │   │
│  │  - Repository Interfaces                         │   │
│  │  - Business Rules & Validation                   │   │
│  │  - Domain Enums                                  │   │
│  └─────────────────────────────────────────────────┘   │
│                                                           │
│              NO DEPENDENCIES ON OTHER LAYERS             │
└─────────────────────────────────────────────────────────┘
```

---

## Clean Architecture Principles

### Dependency Rule

**Dependencies always point inward**. Outer layers can depend on inner layers, but inner layers never depend on outer layers.

```
API/Bot → Infrastructure → Core
   ↓           ↓            ↑
   └───────────┴────────────┘
   (All depend on Core, Core depends on nothing)
```

### Layer Responsibilities

| Layer | Responsibility | Can Depend On |
|-------|---------------|---------------|
| **Core** | Domain entities, business rules, interfaces | Nothing (pure domain) |
| **Infrastructure** | Data access, external services, implementations | Core only |
| **Bot** | Telegram bot logic, message handling | Core only |
| **API** | REST endpoints, HTTP concerns, presentation | Core, Infrastructure, Bot |

---

## Project Layer Breakdown

### 1. SurveyBot.Core (Domain Layer)

**Purpose**: Contains the business logic and domain model of the application.

**Directory Structure**:
```
SurveyBot.Core/
├── Entities/
│   ├── User.cs                    # User entity
│   ├── Survey.cs                  # Survey entity
│   ├── Question.cs                # Question entity
│   ├── Response.cs                # Response entity
│   └── Answer.cs                  # Answer entity
├── Interfaces/
│   ├── IUserRepository.cs         # User data access interface
│   ├── ISurveyRepository.cs       # Survey data access interface
│   ├── IQuestionRepository.cs     # Question data access interface
│   ├── IResponseRepository.cs     # Response data access interface
│   └── IAnswerRepository.cs       # Answer data access interface
└── Enums/
    └── QuestionType.cs            # Question type enumeration
```

**Key Characteristics**:
- No dependencies on any other project
- Pure C# code with no framework references
- Contains domain entities with business rules
- Defines interfaces (contracts) for repositories
- Implements domain validation logic

**Example Entity**:
```csharp
public class Survey
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int CreatorId { get; set; }
    public bool IsActive { get; set; }
    public bool AllowMultipleResponses { get; set; }
    public bool ShowResults { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User Creator { get; set; }
    public ICollection<Question> Questions { get; set; }
    public ICollection<Response> Responses { get; set; }

    // Business logic methods can go here
    public bool CanUserRespond(long telegramId)
    {
        if (!IsActive) return false;
        if (AllowMultipleResponses) return true;

        return !Responses.Any(r =>
            r.RespondentTelegramId == telegramId &&
            r.IsComplete);
    }
}
```

---

### 2. SurveyBot.Infrastructure (Infrastructure Layer)

**Purpose**: Implements data access and external service integrations.

**Directory Structure**:
```
SurveyBot.Infrastructure/
├── Data/
│   ├── ApplicationDbContext.cs           # EF Core DbContext
│   ├── EntityConfigurations/
│   │   ├── UserConfiguration.cs          # User entity configuration
│   │   ├── SurveyConfiguration.cs        # Survey entity configuration
│   │   ├── QuestionConfiguration.cs      # Question entity configuration
│   │   ├── ResponseConfiguration.cs      # Response entity configuration
│   │   └── AnswerConfiguration.cs        # Answer entity configuration
│   └── DatabaseInitializer.cs            # Database seeding
├── Repositories/
│   ├── UserRepository.cs                 # User repository implementation
│   ├── SurveyRepository.cs               # Survey repository implementation
│   ├── QuestionRepository.cs             # Question repository implementation
│   ├── ResponseRepository.cs             # Response repository implementation
│   └── AnswerRepository.cs               # Answer repository implementation
└── Migrations/
    └── [EF Core generated migrations]
```

**Key Characteristics**:
- Depends only on SurveyBot.Core
- Implements repository interfaces defined in Core
- Contains EF Core DbContext and entity configurations
- Handles database migrations
- No business logic (only data access logic)

**Example Repository**:
```csharp
public class SurveyRepository : ISurveyRepository
{
    private readonly ApplicationDbContext _context;

    public SurveyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Survey> GetByIdAsync(int id)
    {
        return await _context.Surveys
            .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
            .Include(s => s.Responses)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Survey>> GetActiveByUserIdAsync(int userId)
    {
        return await _context.Surveys
            .Where(s => s.CreatorId == userId && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Survey> CreateAsync(Survey survey)
    {
        _context.Surveys.Add(survey);
        await _context.SaveChangesAsync();
        return survey;
    }

    // ... more methods
}
```

**ApplicationDbContext**:
```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Survey> Surveys { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Response> Responses { get; set; }
    public DbSet<Answer> Answers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

---

### 3. SurveyBot.Bot (Application Layer - Bot Logic)

**Purpose**: Handles Telegram bot interactions and conversation flow.

**Directory Structure**:
```
SurveyBot.Bot/
├── Handlers/
│   ├── StartCommandHandler.cs            # /start command handler
│   ├── CreateSurveyHandler.cs            # Survey creation flow
│   ├── ListSurveysHandler.cs             # List user surveys
│   ├── TakeSurveyHandler.cs              # Survey response flow
│   └── CallbackQueryHandler.cs           # Inline button callbacks
├── Services/
│   ├── BotService.cs                     # Main bot service
│   ├── SurveyCreationService.cs          # Survey creation logic
│   └── SurveyResponseService.cs          # Survey response logic
├── States/
│   ├── ConversationState.cs              # User conversation state
│   └── StateManager.cs                   # State management service
└── Models/
    ├── BotCommand.cs                     # Bot command definitions
    └── CallbackData.cs                   # Callback data structures
```

**Key Characteristics**:
- Depends only on SurveyBot.Core
- Handles Telegram-specific logic
- Manages conversation state
- Orchestrates user interactions
- No direct database access (uses repositories)

**Example Handler**:
```csharp
public class CreateSurveyHandler
{
    private readonly ISurveyRepository _surveyRepository;
    private readonly IStateManager _stateManager;
    private readonly ILogger<CreateSurveyHandler> _logger;

    public CreateSurveyHandler(
        ISurveyRepository surveyRepository,
        IStateManager stateManager,
        ILogger<CreateSurveyHandler> logger)
    {
        _surveyRepository = surveyRepository;
        _stateManager = stateManager;
        _logger = logger;
    }

    public async Task HandleCreateSurveyCommand(Message message)
    {
        // Initialize state
        await _stateManager.SetStateAsync(
            message.From.Id,
            ConversationState.CreatingSurvey_EnteringTitle);

        // Send prompt
        await botClient.SendTextMessageAsync(
            message.Chat.Id,
            "Great! Let's create a new survey. Please enter the survey title:");
    }

    public async Task HandleTitleInput(Message message)
    {
        var state = await _stateManager.GetStateAsync(message.From.Id);

        if (state == ConversationState.CreatingSurvey_EnteringTitle)
        {
            // Store title in temporary state
            await _stateManager.SetDataAsync(message.From.Id, "title", message.Text);

            // Move to next state
            await _stateManager.SetStateAsync(
                message.From.Id,
                ConversationState.CreatingSurvey_EnteringDescription);

            await botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Perfect! Now enter a description for your survey:");
        }
    }
}
```

---

### 4. SurveyBot.API (Presentation Layer)

**Purpose**: Exposes REST API endpoints for the admin panel and webhook handling.

**Directory Structure**:
```
SurveyBot.API/
├── Controllers/
│   ├── UsersController.cs                # User management endpoints
│   ├── SurveysController.cs              # Survey CRUD endpoints
│   ├── QuestionsController.cs            # Question management endpoints
│   ├── ResponsesController.cs            # Response viewing endpoints
│   └── HealthController.cs               # Health check endpoint
├── Middleware/
│   ├── GlobalExceptionMiddleware.cs      # Global exception handling
│   └── RequestLoggingMiddleware.cs       # Request/response logging
├── Models/
│   ├── ApiResponse.cs                    # Standard API response wrapper
│   ├── ErrorResponse.cs                  # Error response model
│   └── DTOs/
│       ├── CreateSurveyRequest.cs        # Request DTOs
│       ├── SurveyResponse.cs             # Response DTOs
│       └── ...
├── Exceptions/
│   ├── ApiException.cs                   # Base API exception
│   ├── NotFoundException.cs              # 404 exception
│   ├── BadRequestException.cs            # 400 exception
│   └── ...
├── Extensions/
│   ├── DatabaseExtensions.cs             # Database DI registration
│   ├── RepositoryExtensions.cs           # Repository DI registration
│   ├── SwaggerExtensions.cs              # Swagger configuration
│   └── HealthCheckExtensions.cs          # Health check configuration
└── Program.cs                            # Application entry point
```

**Key Characteristics**:
- Depends on Core, Infrastructure, and Bot
- Contains HTTP-specific concerns
- Handles request/response transformation
- Implements middleware pipeline
- Configures dependency injection

**Example Controller**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class SurveysController : ControllerBase
{
    private readonly ISurveyRepository _surveyRepository;
    private readonly ILogger<SurveysController> _logger;

    public SurveysController(
        ISurveyRepository surveyRepository,
        ILogger<SurveysController> logger)
    {
        _surveyRepository = surveyRepository;
        _logger = logger;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SurveyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SurveyResponse>>> GetSurvey(int id)
    {
        var survey = await _surveyRepository.GetByIdAsync(id);

        if (survey == null)
        {
            throw new NotFoundException($"Survey with ID {id} not found");
        }

        var response = MapToResponse(survey);
        return Ok(ApiResponse<SurveyResponse>.Success(response));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SurveyResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SurveyResponse>>> CreateSurvey(
        [FromBody] CreateSurveyRequest request)
    {
        var survey = MapToEntity(request);
        var created = await _surveyRepository.CreateAsync(survey);

        var response = MapToResponse(created);
        return CreatedAtAction(
            nameof(GetSurvey),
            new { id = created.Id },
            ApiResponse<SurveyResponse>.Success(response));
    }
}
```

---

### 5. SurveyBot.Tests (Test Layer)

**Purpose**: Contains unit and integration tests for all layers.

**Directory Structure**:
```
SurveyBot.Tests/
├── Unit/
│   ├── Core/
│   │   └── SurveyTests.cs                # Domain entity tests
│   ├── Bot/
│   │   └── CreateSurveyHandlerTests.cs   # Bot handler tests
│   └── API/
│       └── SurveysControllerTests.cs     # Controller tests
├── Integration/
│   ├── Repositories/
│   │   └── SurveyRepositoryTests.cs      # Repository integration tests
│   └── API/
│       └── SurveysEndpointTests.cs       # API endpoint tests
└── Fixtures/
    ├── DatabaseFixture.cs                # In-memory database for tests
    └── TestData.cs                       # Test data builders
```

---

## Design Patterns

### Repository Pattern

**Purpose**: Abstracts data access logic and provides a collection-like interface.

**Benefits**:
- Decouples business logic from data access
- Makes testing easier (can mock repositories)
- Centralizes data access logic
- Provides flexibility to change data sources

**Implementation**:
```csharp
// Core layer - Interface definition
public interface ISurveyRepository
{
    Task<Survey> GetByIdAsync(int id);
    Task<IEnumerable<Survey>> GetAllAsync();
    Task<Survey> CreateAsync(Survey survey);
    Task UpdateAsync(Survey survey);
    Task DeleteAsync(int id);
}

// Infrastructure layer - Implementation
public class SurveyRepository : ISurveyRepository
{
    private readonly ApplicationDbContext _context;

    public SurveyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Implementation details...
}
```

### Dependency Injection Pattern

**Purpose**: Inverts control by injecting dependencies rather than creating them.

**Benefits**:
- Loose coupling between components
- Easier testing (can inject mocks)
- Better code maintainability
- Follows SOLID principles

**Configuration** (in Program.cs):
```csharp
// Register database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISurveyRepository, SurveyRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IResponseRepository, ResponseRepository>();
builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();
```

### Unit of Work Pattern (Future Enhancement)

Currently using individual repositories with DbContext SaveChanges. For complex transactions, consider implementing Unit of Work pattern.

---

## Dependency Injection

### Service Registration

All dependencies are registered in `Program.cs` using extension methods for organization:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register database
builder.Services.AddDatabase(builder.Configuration);

// Register repositories
builder.Services.AddRepositories();

// Register Swagger
builder.Services.AddSwaggerDocumentation();

// Register health checks
builder.Services.AddHealthChecks(builder.Configuration);

// Build the app
var app = builder.Build();

// Configure middleware pipeline
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks();

app.Run();
```

### Extension Methods for Organization

**DatabaseExtensions.cs**:
```csharp
public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("SurveyBot.Infrastructure")));

        return services;
    }
}
```

**RepositoryExtensions.cs**:
```csharp
public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(
        this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISurveyRepository, SurveyRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IResponseRepository, ResponseRepository>();
        services.AddScoped<IAnswerRepository, AnswerRepository>();

        return services;
    }
}
```

### Service Lifetimes

| Service Type | Lifetime | Reason |
|-------------|----------|--------|
| DbContext | Scoped | One instance per HTTP request |
| Repositories | Scoped | Tied to DbContext lifetime |
| Bot Handlers | Scoped | Per-request processing |
| Logging | Singleton | Shared across application |
| Configuration | Singleton | Read-only, immutable |

---

## Data Flow

### API Request Flow

```
1. HTTP Request
   ↓
2. Middleware Pipeline (Logging, Exception Handling)
   ↓
3. Controller Action
   ↓
4. Repository Interface Call
   ↓
5. Repository Implementation (EF Core)
   ↓
6. Database Query
   ↓
7. Entity Returned
   ↓
8. Map to DTO
   ↓
9. Wrap in ApiResponse
   ↓
10. HTTP Response
```

### Telegram Bot Message Flow

```
1. Telegram Webhook (POST to API)
   ↓
2. Webhook Controller
   ↓
3. Bot Service
   ↓
4. Message Handler
   ↓
5. State Manager (check conversation state)
   ↓
6. Repository Call (if needed)
   ↓
7. Business Logic Processing
   ↓
8. State Update
   ↓
9. Telegram API Call (send response)
   ↓
10. User receives message
```

---

## Technology Decisions

### Why .NET 8?

- Long-term support (LTS) release
- Excellent performance
- Cross-platform support
- Rich ecosystem and tooling
- Built-in dependency injection
- Modern C# language features

### Why Entity Framework Core?

- Official Microsoft ORM
- Code-first approach with migrations
- LINQ query support
- Change tracking
- Good PostgreSQL support via Npgsql
- Simplifies database operations

### Why PostgreSQL?

- Open-source and free
- Excellent performance for relational data
- JSONB support for flexible data
- Strong ACID compliance
- Great indexing capabilities
- Well-supported by EF Core

### Why Repository Pattern?

- Abstracts data access
- Enables testing with mocks
- Centralizes data access logic
- Provides flexibility to change implementations
- Follows Clean Architecture principles

---

## Security Architecture

### Current Implementation

1. **Input Validation**: All API inputs validated with data annotations
2. **Error Handling**: Global exception handler prevents information leakage
3. **SQL Injection Prevention**: EF Core parameterizes all queries
4. **Logging**: Structured logging with Serilog (no sensitive data logged)

### Future Enhancements

1. **JWT Authentication**: Secure admin panel access
2. **Rate Limiting**: Prevent abuse of API endpoints
3. **CORS Configuration**: Restrict cross-origin requests
4. **API Keys**: Secure Telegram webhook endpoint
5. **Data Encryption**: Encrypt sensitive data at rest

---

## Scalability Considerations

### Current MVP Limitations

- Single database instance
- No caching layer
- Synchronous processing
- Single server deployment

### Future Scalability Enhancements

1. **Caching**: Add Redis for frequently accessed data
2. **Message Queue**: Use RabbitMQ for async processing
3. **Database Read Replicas**: Separate read and write operations
4. **CDN**: Serve static admin panel files
5. **Horizontal Scaling**: Deploy multiple API instances behind load balancer
6. **Database Partitioning**: Partition large tables by date or user

### Performance Targets

| Metric | Current Target | Future Target |
|--------|---------------|---------------|
| API Response Time | < 200ms | < 100ms |
| Database Queries | < 50ms | < 20ms |
| Bot Response Time | < 1s | < 500ms |
| Concurrent Users | 100 | 10,000 |
| Surveys per User | 100 | 1,000 |

---

## Best Practices

### Code Organization

1. Keep Core layer pure (no external dependencies)
2. Use extension methods for service registration
3. Implement interfaces in Core, implement in Infrastructure
4. One entity per file
5. Group related functionality in folders

### Error Handling

1. Use custom exception types
2. Handle exceptions in global middleware
3. Return consistent error responses
4. Log all exceptions with context
5. Never expose internal errors to clients

### Testing

1. Write unit tests for business logic
2. Write integration tests for repositories
3. Mock external dependencies
4. Use in-memory database for testing
5. Aim for > 80% code coverage

### Database

1. Use migrations for all schema changes
2. Index foreign keys and frequently queried columns
3. Use appropriate data types
4. Implement soft deletes for important data
5. Regular backups and monitoring

---

## Diagrams

### Component Diagram

```
┌─────────────────────────────────────────────────┐
│                  Admin Panel                     │
│                   (React)                        │
└─────────────────────────────────────────────────┘
                     │ HTTP REST
                     ↓
┌─────────────────────────────────────────────────┐
│              SurveyBot.API                       │
│  ┌─────────────────────────────────────────┐   │
│  │    Controllers & Middleware              │   │
│  └─────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
         │                          ↑
         │ DI                       │ Webhook
         ↓                          │
┌─────────────────────────┐         │
│   SurveyBot.Bot         │         │
│   (Handlers & Services) │         │
└─────────────────────────┘         │
         │                          │
         │ DI                       │
         ↓                          │
┌─────────────────────────┐    ┌───────────┐
│ SurveyBot.Infrastructure│    │ Telegram  │
│ (Repositories)          │    │    API    │
└─────────────────────────┘    └───────────┘
         │
         │ EF Core
         ↓
┌─────────────────────────┐
│      PostgreSQL         │
│      Database           │
└─────────────────────────┘
```

---

## References

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Microsoft .NET Architecture Guides](https://dotnet.microsoft.com/learn/dotnet/architecture-guides)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)

---

**Document Status**: Complete
**Next Steps**: Implement bot handlers, add authentication, build admin panel
