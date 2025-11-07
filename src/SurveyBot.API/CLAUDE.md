# SurveyBot.API - Presentation Layer Documentation

## Overview

**SurveyBot.API** is the presentation layer that exposes REST API endpoints for the SurveyBot application. It handles HTTP requests, authentication, routing, and serves as the entry point for both external clients and the Telegram bot.

## Purpose

- Expose REST API endpoints
- Handle HTTP request/response
- Implement JWT authentication
- Provide Swagger/OpenAPI documentation
- Configure middleware pipeline
- Register all dependencies (DI)
- Host Telegram bot webhook endpoint

## Dependencies

```
SurveyBot.API
    ├── SurveyBot.Core (domain layer)
    ├── SurveyBot.Infrastructure (data access)
    ├── SurveyBot.Bot (Telegram bot logic)
    ├── AutoMapper (NuGet)
    ├── Serilog (NuGet)
    ├── Swashbuckle (Swagger)
    ├── JWT Bearer Authentication
    └── Health Checks
```

---

## Project Structure

```
SurveyBot.API/
├── Controllers/              # API controllers (REST endpoints)
│   ├── AuthController.cs
│   ├── SurveysController.cs
│   ├── QuestionsController.cs
│   ├── ResponsesController.cs
│   ├── UsersController.cs
│   ├── BotController.cs
│   ├── HealthController.cs
│   └── TestErrorsController.cs
├── Middleware/              # Custom middleware
│   ├── GlobalExceptionMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── Models/                  # API-specific models
│   ├── ApiResponse.cs       # Standard response wrapper
│   └── ErrorResponse.cs     # Error details
├── Mapping/                 # AutoMapper profiles
│   ├── SurveyMappingProfile.cs
│   ├── QuestionMappingProfile.cs
│   ├── ResponseMappingProfile.cs
│   ├── AnswerMappingProfile.cs
│   ├── UserMappingProfile.cs
│   ├── StatisticsMappingProfile.cs
│   └── ValueResolvers/      # Custom value resolvers
│       ├── QuestionOptionsResolver.cs
│       ├── AnswerJsonResolver.cs
│       ├── SurveyTotalResponsesResolver.cs
│       └── ...
├── Extensions/              # Service registration extensions
│   ├── DatabaseExtensions.cs
│   ├── RepositoryExtensions.cs
│   ├── SwaggerExtensions.cs
│   ├── MiddlewareExtensions.cs
│   ├── HealthCheckExtensions.cs
│   ├── AutoMapperExtensions.cs
│   └── BackgroundServiceExtensions.cs
├── Services/                # Background services
│   ├── IBackgroundTaskQueue.cs
│   ├── BackgroundTaskQueue.cs
│   └── QueuedHostedService.cs
├── Exceptions/              # API-specific exceptions
│   ├── ApiException.cs
│   ├── NotFoundException.cs
│   ├── ValidationException.cs
│   ├── BadRequestException.cs
│   ├── UnauthorizedException.cs
│   ├── ForbiddenException.cs
│   └── ConflictException.cs
├── Properties/
│   └── launchSettings.json  # Development settings
├── Program.cs               # Application entry point
├── appsettings.json         # Configuration
├── appsettings.Development.json
└── SurveyBot.API.csproj
```

---

## Program.cs - Application Startup

The `Program.cs` file is the entry point and configures the entire application.

### Structure

```csharp
// 1. Configure Serilog (early initialization)
Log.Logger = new LoggerConfiguration()...

try
{
    // 2. Create WebApplicationBuilder
    var builder = WebApplication.CreateBuilder(args);

    // 3. Replace default logging with Serilog
    builder.Host.UseSerilog();

    // 4. Configure services (DI container)
    // ... service registrations ...

    // 5. Build application
    var app = builder.Build();

    // 6. Initialize Telegram Bot
    await app.Services.InitializeTelegramBotAsync();

    // 7. Configure middleware pipeline
    // ... middleware configuration ...

    // 8. Run application
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

### Service Registration Order

1. **Controllers**: `builder.Services.AddControllers()`
2. **AutoMapper**: `builder.Services.AddAutoMapper(typeof(Program).Assembly)`
3. **DbContext**: `builder.Services.AddDbContext<SurveyBotDbContext>()`
4. **JWT Settings**: `builder.Services.Configure<JwtSettings>()`
5. **Authentication**: `builder.Services.AddAuthentication().AddJwtBearer()`
6. **Authorization**: `builder.Services.AddAuthorization()`
7. **Repositories**: Scoped registrations
8. **Services**: Scoped registrations
9. **Telegram Bot**: `builder.Services.AddTelegramBot()` and `AddBotHandlers()`
10. **Background Services**: `builder.Services.AddBackgroundTaskQueue()`
11. **Health Checks**: `builder.Services.AddHealthChecks()`
12. **Swagger**: `builder.Services.AddSwaggerGen()`

### Middleware Pipeline Order

```csharp
app.UseSerilogRequestLogging();      // 1. Serilog HTTP logging
app.UseRequestLogging();             // 2. Custom request logging
app.UseGlobalExceptionHandler();     // 3. Exception handling (early!)
app.UseSwagger();                    // 4. Swagger (Development only)
app.UseSwaggerUI();                  // 5. Swagger UI
app.UseHttpsRedirection();           // 6. HTTPS redirect
app.UseAuthentication();             // 7. Authentication (before Authorization!)
app.UseAuthorization();              // 8. Authorization
app.MapControllers();                // 9. Route to controllers
app.MapHealthChecks("/health/db");   // 10. Health check endpoint
```

**Important**: Order matters! Authentication must come before Authorization.

---

## Controllers

### Base Controller Pattern

All controllers follow these conventions:

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]  // If authentication required
public class ExampleController : ControllerBase
{
    private readonly IService _service;
    private readonly ILogger<ExampleController> _logger;

    public ExampleController(IService service, ILogger<ExampleController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // Endpoints...
}
```

### Response Pattern

All endpoints return `ApiResponse<T>`:

```csharp
[HttpGet("{id}")]
[ProducesResponseType(typeof(ApiResponse<SurveyDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
public async Task<ActionResult<ApiResponse<SurveyDto>>> GetSurvey(int id)
{
    try
    {
        var survey = await _service.GetSurveyAsync(id);
        return Ok(ApiResponse<SurveyDto>.Ok(survey));
    }
    catch (SurveyNotFoundException ex)
    {
        return NotFound(new ApiResponse<object>
        {
            Success = false,
            Message = ex.Message
        });
    }
}
```

### SurveysController

**Route**: `/api/surveys`

**Endpoints**:

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| POST | `/` | Create survey | Yes |
| GET | `/` | List surveys (paginated) | Yes |
| GET | `/{id}` | Get survey details | Yes |
| PUT | `/{id}` | Update survey | Yes |
| DELETE | `/{id}` | Delete survey | Yes |
| POST | `/{id}/activate` | Activate survey | Yes |
| POST | `/{id}/deactivate` | Deactivate survey | Yes |
| GET | `/{id}/statistics` | Get survey statistics | Yes |

**Key Features**:
- All endpoints require authentication
- User ID extracted from JWT claims
- Authorization checks (user owns survey)
- Pagination support with `PaginationQueryDto`
- Filtering and sorting support

**Example - Create Survey**:
```csharp
[HttpPost]
[SwaggerOperation(
    Summary = "Create a new survey",
    Description = "Creates a new survey for the authenticated user."
)]
[ProducesResponseType(typeof(ApiResponse<SurveyDto>), StatusCodes.Status201Created)]
public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateSurvey(
    [FromBody] CreateSurveyDto dto)
{
    var userId = GetUserIdFromClaims();
    var survey = await _surveyService.CreateSurveyAsync(userId, dto);

    return CreatedAtAction(
        nameof(GetSurveyById),
        new { id = survey.Id },
        ApiResponse<SurveyDto>.Ok(survey, "Survey created successfully"));
}
```

### QuestionsController

**Route**: `/api/questions` and `/api/surveys/{surveyId}/questions`

**Endpoints**:

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/surveys/{surveyId}/questions` | Add question to survey |
| GET | `/api/surveys/{surveyId}/questions` | List survey questions |
| GET | `/api/questions/{id}` | Get question details |
| PUT | `/api/questions/{id}` | Update question |
| DELETE | `/api/questions/{id}` | Delete question |
| POST | `/api/surveys/{surveyId}/questions/reorder` | Reorder questions |

**Authorization**:
- User must own the survey to manage questions
- Validated in service layer

### ResponsesController

**Route**: `/api/responses` and `/api/surveys/{surveyId}/responses`

**Endpoints**:

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/surveys/{surveyId}/responses/start` | Start new response |
| POST | `/api/responses/{responseId}/answers` | Submit answer |
| POST | `/api/responses/{responseId}/complete` | Complete response |
| GET | `/api/surveys/{surveyId}/responses` | List survey responses |
| GET | `/api/responses/{id}` | Get response details |

**Key Features**:
- Start-Answer-Complete workflow
- Duplicate response prevention
- Answer validation by question type

### AuthController

**Route**: `/api/auth`

**Endpoints**:

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| POST | `/login` | Login with Telegram credentials | No |
| POST | `/refresh` | Refresh JWT token | No |

**Login Request**:
```json
{
  "telegramId": 123456789,
  "username": "john_doe",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Login Response**:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "...",
    "expiresIn": 3600,
    "user": {
      "id": 1,
      "telegramId": 123456789,
      "username": "john_doe"
    }
  }
}
```

### BotController

**Route**: `/api/bot`

**Endpoints**:

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| POST | `/webhook` | Telegram webhook endpoint | Secret token |

**Webhook Validation**:
- Validates `X-Telegram-Bot-Api-Secret-Token` header
- Queues update for background processing
- Returns 200 OK immediately

### UsersController

**Route**: `/api/users`

**Endpoints**:

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| GET | `/me` | Get current user profile | Yes |
| PUT | `/me` | Update user profile | Yes |

---

## API Models

### ApiResponse<T>

**Location**: `Models/ApiResponse.cs`

**Purpose**: Standard response wrapper for all API endpoints

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message
        };
    }
}
```

**Usage**:
```csharp
// Success
return Ok(ApiResponse<SurveyDto>.Ok(survey));

// With custom message
return Ok(ApiResponse<SurveyDto>.Ok(survey, "Survey created successfully"));

// Failure
return BadRequest(ApiResponse<object>.Fail("Invalid data"));
```

### ErrorResponse

**Location**: `Models/ErrorResponse.cs`

**Purpose**: Detailed error information (used by exception middleware)

```csharp
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? TraceId { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }  // Validation errors
}
```

---

## Middleware

### GlobalExceptionMiddleware

**Location**: `Middleware/GlobalExceptionMiddleware.cs`

**Purpose**: Centralized exception handling

**Features**:
- Catches all unhandled exceptions
- Maps domain exceptions to HTTP status codes
- Logs errors with context
- Returns standardized error responses

**Exception Mapping**:
```csharp
private int GetStatusCode(Exception exception) => exception switch
{
    SurveyNotFoundException => StatusCodes.Status404NotFound,
    QuestionNotFoundException => StatusCodes.Status404NotFound,
    ResponseNotFoundException => StatusCodes.Status404NotFound,
    SurveyValidationException => StatusCodes.Status400BadRequest,
    QuestionValidationException => StatusCodes.Status400BadRequest,
    InvalidAnswerFormatException => StatusCodes.Status400BadRequest,
    DuplicateResponseException => StatusCodes.Status409Conflict,
    UnauthorizedAccessException => StatusCodes.Status403Forbidden,
    _ => StatusCodes.Status500InternalServerError
};
```

**Registration**:
```csharp
app.UseGlobalExceptionHandler();  // Extension method in MiddlewareExtensions
```

### RequestLoggingMiddleware

**Location**: `Middleware/RequestLoggingMiddleware.cs`

**Purpose**: Log HTTP request/response details

**Logs**:
- Request method, path, query string
- Request headers (excluding sensitive)
- Response status code
- Response time (milliseconds)
- User information (if authenticated)

---

## AutoMapper Configuration

### Mapping Profiles

Each domain area has its own mapping profile:

1. **SurveyMappingProfile**:
   - `Survey` ↔ `SurveyDto`
   - `Survey` → `SurveyListDto`
   - `CreateSurveyDto` → `Survey`
   - Custom resolvers for calculated fields

2. **QuestionMappingProfile**:
   - `Question` ↔ `QuestionDto`
   - Handles OptionsJson serialization/deserialization

3. **ResponseMappingProfile**:
   - `Response` ↔ `ResponseDto`
   - Includes answer count, progress calculation

4. **AnswerMappingProfile**:
   - `Answer` ↔ `AnswerDto`
   - Handles AnswerJson parsing

5. **UserMappingProfile**:
   - `User` ↔ `UserDto`

### Value Resolvers

Custom resolvers for complex mappings:

**QuestionOptionsResolver**:
```csharp
// Deserializes OptionsJson (string) to Options (List<string>)
public class QuestionOptionsResolver : IValueResolver<Question, QuestionDto, List<string>?>
{
    public List<string>? Resolve(Question source, QuestionDto destination,
        List<string>? destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.OptionsJson))
            return null;

        return JsonSerializer.Deserialize<List<string>>(source.OptionsJson);
    }
}
```

**AnswerJsonResolver**:
Parses answer JSON based on question type

**SurveyTotalResponsesResolver**:
Counts total responses for a survey

**Usage in Profile**:
```csharp
CreateMap<Question, QuestionDto>()
    .ForMember(dest => dest.Options,
        opt => opt.MapFrom<QuestionOptionsResolver>());
```

### Testing AutoMapper

**AutoMapperConfigurationTest**:
```csharp
public class AutoMapperConfigurationTest
{
    [Fact]
    public void AutoMapper_Configuration_IsValid()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(Program).Assembly);
        });

        config.AssertConfigurationIsValid();
    }
}
```

---

## Authentication & Authorization

### JWT Configuration

**appsettings.json**:
```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-minimum-32-characters-for-security",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Client",
    "ExpirationMinutes": 60
  }
}
```

**Registration in Program.cs**:
```csharp
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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

### Using Authentication

**Controller Level**:
```csharp
[Authorize]  // All endpoints require authentication
public class SurveysController : ControllerBase
{
    // ...
}
```

**Endpoint Level**:
```csharp
[AllowAnonymous]  // Override controller-level [Authorize]
public async Task<IActionResult> PublicEndpoint()
{
    // ...
}
```

**Extracting User ID from Claims**:
```csharp
private int GetUserIdFromClaims()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!int.TryParse(userIdClaim, out int userId))
        throw new UnauthorizedAccessException("Invalid user authentication");

    return userId;
}
```

---

## Swagger/OpenAPI Documentation

### Configuration

**Program.cs**:
```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SurveyBot API",
        Version = "v1",
        Description = "REST API for Telegram Survey Bot"
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // JWT Bearer Authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement { /* ... */ });
});
```

### Using Swagger

**Access**: http://localhost:5000/swagger

**Annotating Endpoints**:
```csharp
[HttpPost]
[SwaggerOperation(
    Summary = "Create a new survey",
    Description = "Creates a new survey for the authenticated user. Survey is created inactive by default.",
    Tags = new[] { "Surveys" }
)]
[ProducesResponseType(typeof(ApiResponse<SurveyDto>), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult> CreateSurvey([FromBody] CreateSurveyDto dto)
{
    // ...
}
```

### Testing with Swagger

1. Navigate to http://localhost:5000/swagger
2. Click "Authorize" button
3. Enter: `Bearer your-jwt-token`
4. Test endpoints interactively

---

## Health Checks

### Configuration

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SurveyBotDbContext>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "postgresql" });
```

### Endpoints

**Basic Health Check**:
- URL: `/health/db`
- Returns: `Healthy`, `Degraded`, or `Unhealthy`

**Detailed Health Check**:
- URL: `/health/db/details`
- Returns JSON with database connection status:
```json
{
  "status": "healthy",
  "database": "connected",
  "timestamp": "2025-11-07T10:30:00Z"
}
```

---

## Background Services

### Background Task Queue

**Purpose**: Queue webhook updates for background processing

**Components**:
1. **IBackgroundTaskQueue** - Interface
2. **BackgroundTaskQueue** - Thread-safe queue implementation
3. **QueuedHostedService** - Background worker that processes queue

**Usage**:
```csharp
// In BotController
await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async token =>
{
    await _updateHandler.HandleUpdateAsync(update, token);
});
```

**Registration**:
```csharp
builder.Services.AddBackgroundTaskQueue(queueCapacity: 100);
```

---

## Configuration Files

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=surveybot_db;Username=surveybot_user;Password=surveybot_dev_password"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-minimum-32-characters",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Client",
    "ExpirationMinutes": 60
  },
  "BotConfiguration": {
    "BotToken": "",
    "WebhookUrl": "",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  }
}
```

### appsettings.Development.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    }
  }
}
```

---

## Best Practices

### Controller Design

1. **Keep controllers thin** - delegate to services
2. **Use action filters** for cross-cutting concerns
3. **Return consistent response types** - ApiResponse<T>
4. **Add XML documentation** for Swagger
5. **Handle exceptions** - specific catches before general
6. **Validate inputs** - use model validation
7. **Log important operations** - with context

### API Design

1. **Use proper HTTP verbs** - GET, POST, PUT, DELETE
2. **Use meaningful routes** - RESTful conventions
3. **Version your API** - prepare for future changes
4. **Use status codes correctly**:
   - 200 OK - Success
   - 201 Created - Resource created
   - 204 No Content - Success with no body
   - 400 Bad Request - Validation error
   - 401 Unauthorized - Not authenticated
   - 403 Forbidden - Not authorized
   - 404 Not Found - Resource doesn't exist
   - 409 Conflict - Duplicate/conflict
   - 500 Internal Server Error - Unhandled error

### Security

1. **Use HTTPS** in production
2. **Validate all inputs** - never trust client
3. **Use authentication** - JWT tokens
4. **Implement authorization** - check ownership
5. **Don't expose stack traces** - only in Development
6. **Rate limit** - prevent abuse
7. **Sanitize error messages** - don't leak sensitive info

---

## Common Tasks

### Adding a New Endpoint

1. **Create/Update Controller**:
   ```csharp
   [HttpPost("new-endpoint")]
   public async Task<ActionResult<ApiResponse<ResultDto>>> NewEndpoint([FromBody] RequestDto dto)
   {
       // Implementation
   }
   ```

2. **Add Swagger annotations**
3. **Test with Swagger UI**
4. **Add integration tests**

### Adding a New Controller

1. Create controller class
2. Inherit from `ControllerBase`
3. Add `[ApiController]` attribute
4. Add `[Route]` attribute
5. Inject dependencies in constructor
6. Implement endpoints
7. Add to Swagger tags

### Debugging API

1. **Use Swagger UI** - http://localhost:5000/swagger
2. **Check logs** - Console output with Serilog
3. **Enable EF logging** - See SQL queries
4. **Use breakpoints** - Visual Studio debugger
5. **Check middleware order** - Common issue

---

**Key Takeaway**: The API layer is the entry point. Keep it focused on HTTP concerns, delegate business logic to services, and maintain consistent patterns across all endpoints.
