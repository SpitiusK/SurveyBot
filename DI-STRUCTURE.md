# Dependency Injection Structure

## DI Container Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    ASP.NET Core DI Container                 │
│                     (Microsoft.Extensions.DI)                │
└─────────────────────────────────────────────────────────────┘
                              │
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
┌──────────────┐    ┌──────────────┐      ┌──────────────┐
│  Framework   │    │  Application │      │   Health     │
│  Services    │    │   Services   │      │   Checks     │
└──────────────┘    └──────────────┘      └──────────────┘
        │                     │                     │
        ├─Controllers         ├─DbContext           ├─DbContextCheck
        ├─Swagger             ├─Repositories        └─/health/db
        ├─Logging             └─Services (Future)
        └─Authentication
```

## Service Registration Details

### 1. DbContext Layer (Scoped)

```
┌─────────────────────────────────────┐
│      SurveyBotDbContext             │
│         (Scoped)                    │
├─────────────────────────────────────┤
│  - ConnectionString                 │
│  - PostgreSQL Provider              │
│  - Change Tracking                  │
│  - Sensitive Data Logging (Dev)     │
│  - Detailed Errors (Dev)            │
└─────────────────────────────────────┘
                  │
                  │ Injected into
                  ▼
        All Repository Classes
```

### 2. Repository Layer (All Scoped)

```
┌──────────────────────────────────────────────────────┐
│                   Repositories                       │
├──────────────────────────────────────────────────────┤
│                                                      │
│  ISurveyRepository    ──► SurveyRepository         │
│  IQuestionRepository  ──► QuestionRepository       │
│  IResponseRepository  ──► ResponseRepository       │
│  IUserRepository      ──► UserRepository           │
│  IAnswerRepository    ──► AnswerRepository         │
│                                                      │
│  Each inherits from GenericRepository<T>            │
│  Each receives SurveyBotDbContext via constructor   │
└──────────────────────────────────────────────────────┘
```

### 3. Logging Configuration

```
┌────────────────────────────┐
│     Logging Providers      │
├────────────────────────────┤
│  Console Provider          │
│  Debug Provider            │
│  EF Core SQL (Dev only)    │
└────────────────────────────┘
```

### 4. Health Check Configuration

```
┌────────────────────────────────────┐
│        Health Check System         │
├────────────────────────────────────┤
│  Endpoint: /health/db              │
│  Check: DbContextCheck             │
│  Status: Healthy/Unhealthy         │
│  Tags: db, sql, postgresql         │
└────────────────────────────────────┘
```

## Service Lifetime Flow

```
HTTP Request ──────┐
                   │
                   ▼
            ┌──────────────┐
            │  DI Scope    │  ◄── Created per request
            │   Created    │
            └──────────────┘
                   │
                   ├──► DbContext Instance (new)
                   │         │
                   │         ├──► Used by all repositories
                   │         │    in this request
                   │         │
                   ├──► SurveyRepository (new)
                   ├──► QuestionRepository (new)
                   ├──► ResponseRepository (new)
                   ├──► UserRepository (new)
                   └──► AnswerRepository (new)
                           │
                           ▼
                    Single Transaction
                   (if needed via UoW)
                           │
                           ▼
            ┌──────────────────────┐
            │  Scope Disposed      │
            │  DbContext.Dispose() │
            └──────────────────────┘
                           │
                           ▼
                  HTTP Response
```

## Dependency Graph

```
Controllers (Future)
    │
    ├──► ISurveyRepository ──────┐
    ├──► IQuestionRepository ────┤
    ├──► IResponseRepository ────┼──► SurveyBotDbContext
    ├──► IUserRepository ─────────┤
    └──► IAnswerRepository ───────┘
```

## Service Resolution Example

```csharp
// Automatic DI Resolution in Controller
public class SurveysController : ControllerBase
{
    private readonly ISurveyRepository _surveyRepo;
    private readonly IQuestionRepository _questionRepo;

    // Constructor Injection - DI Container resolves automatically
    public SurveysController(
        ISurveyRepository surveyRepo,
        IQuestionRepository questionRepo)
    {
        _surveyRepo = surveyRepo;      // ◄── SurveyRepository instance
        _questionRepo = questionRepo;  // ◄── QuestionRepository instance
    }

    // Both repositories share the SAME DbContext instance
    // within this request scope
}
```

## Configuration Files

### Program.cs Registration Order

```
1. Configure Logging
   └─ Console, Debug, EF Core SQL

2. Add Controllers
   └─ MVC Controllers with JSON

3. Add DbContext
   └─ PostgreSQL, Connection String

4. Register Repositories
   └─ 5 repository interfaces → implementations

5. Add Health Checks
   └─ DbContext health check

6. Configure Swagger/OpenAPI
   └─ API documentation

7. Build Application
   └─ Create service provider

8. Verify DI (Development)
   └─ Test service resolution

9. Configure Middleware Pipeline
   └─ Health checks, controllers, swagger
```

## Service Locator Pattern (Anti-Pattern Avoided)

```
❌ BAD - Service Locator:
public void MyMethod()
{
    var repo = ServiceProvider.GetService<ISurveyRepository>();
}

✅ GOOD - Constructor Injection:
public class MyController
{
    private readonly ISurveyRepository _repo;

    public MyController(ISurveyRepository repo)
    {
        _repo = repo;
    }
}
```

## Testing Implications

```
Unit Tests
    │
    ├──► Mock ISurveyRepository
    ├──► Mock IQuestionRepository
    ├──► Mock IResponseRepository
    ├──► Mock IUserRepository
    └──► Mock IAnswerRepository
         │
         └──► No DbContext needed!

Integration Tests
    │
    └──► Use In-Memory DbContext
         or Test Database
```

## Future Extensions

When adding new services, follow this pattern:

```csharp
// 1. Define interface in Core project
public interface ISurveyService
{
    Task<SurveyDto> GetSurveyAsync(int id);
}

// 2. Implement in Application/Services
public class SurveyService : ISurveyService
{
    private readonly ISurveyRepository _repo;

    public SurveyService(ISurveyRepository repo)
    {
        _repo = repo;
    }

    public async Task<SurveyDto> GetSurveyAsync(int id)
    {
        var survey = await _repo.GetByIdAsync(id);
        return MapToDto(survey);
    }
}

// 3. Register in Program.cs
builder.Services.AddScoped<ISurveyService, SurveyService>();

// 4. Inject in controller
public class SurveysController
{
    private readonly ISurveyService _service;

    public SurveysController(ISurveyService service)
    {
        _service = service;
    }
}
```

## Summary

The DI container is configured with:
- Clean architecture separation
- Proper lifetime management (Scoped)
- Constructor injection pattern
- Testability through interfaces
- Health monitoring
- Comprehensive logging

All acceptance criteria met and verified!
