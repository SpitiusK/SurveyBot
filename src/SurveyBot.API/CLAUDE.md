# SurveyBot.API - REST API Presentation Layer

**Version**: 1.3.0 | **Framework**: .NET 8.0 | **ASP.NET Core** 8.0

> **Main Documentation**: [Project Root CLAUDE.md](../../CLAUDE.md)
> **Related**: [Core Layer](../SurveyBot.Core/CLAUDE.md) | [Infrastructure Layer](../SurveyBot.Infrastructure/CLAUDE.md) | [Bot Layer](../SurveyBot.Bot/CLAUDE.md)

---

## Overview

API layer exposes REST endpoints and hosts the application. **Depends on Core, Infrastructure, and Bot layers**.

**Responsibilities**: HTTP controllers, JWT authentication, middleware, Swagger docs, background processing, application entry point.

---

## Program.cs - Application Entry Point

**Location**: `Program.cs` (308 lines)

### Service Configuration Order (Lines 36-190)

```csharp
// 1. Controllers
builder.Services.AddControllers();

// 2. AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// 3. DbContext + PostgreSQL
builder.Services.AddDbContext<SurveyBotDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 4. JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

// 5. Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* token validation params */);
builder.Services.AddAuthorization();

// 6. Repositories (Scoped)
builder.Services.AddScoped<ISurveyRepository, SurveyRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IResponseRepository, ResponseRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();

// 7. Services (Scoped)
builder.Services.AddScoped<ISurveyService, SurveyService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IResponseService, ResponseService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// 8. Telegram Bot
builder.Services.AddTelegramBot(builder.Configuration);
builder.Services.AddBotHandlers();

// 9. Background Task Queue
builder.Services.AddBackgroundTaskQueue(queueCapacity: 100);

// 10. Media Services (NEW in v1.3.0)
builder.Services.AddScoped<IMediaStorageService, MediaStorageService>();
builder.Services.AddScoped<IMediaValidationService, MediaValidationService>();

// 11. Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SurveyBotDbContext>(name: "database");

// 12. Swagger/OpenAPI
builder.Services.AddSwaggerGen(/* JWT bearer config */);
```

### Middleware Pipeline Order (Lines 213-292)

**CRITICAL**: Order matters!

```csharp
// 1. Serilog request logging
app.UseSerilogRequestLogging();

// 2. Custom request logging
app.UseRequestLogging();

// 3. Global exception handler (MUST BE EARLY)
app.UseGlobalExceptionHandler();

// 4. Swagger (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 5. HTTPS redirection
app.UseHttpsRedirection();

// 6. Authentication (BEFORE Authorization)
app.UseAuthentication();

// 7. Authorization
app.UseAuthorization();

// 8. Map controllers
app.MapControllers();

// 9. Health check endpoints
app.MapHealthChecks("/health/db");
app.MapGet("/health/db/details", /* detailed check */);
```

**Rules**:
- Exception handling EARLY (catches all errors)
- Authentication BEFORE Authorization
- HTTPS redirection BEFORE authentication
- Routing AFTER authentication/authorization

---

## Controllers

### Base Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]  // If auth required
public class ExampleController : ControllerBase
{
    private readonly IService _service;
    private readonly ILogger<ExampleController> _logger;

    public ExampleController(IService service, ILogger<ExampleController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // Thin controllers - delegate to services
}
```

### AuthController

**Route**: `/api/auth`

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/login` | POST | No | Login with Telegram, get JWT |
| `/register` | POST | No | Register/update user (upsert) |
| `/refresh` | POST | No | Refresh JWT (placeholder) |
| `/validate` | GET | Yes | Validate current token |
| `/me` | GET | Yes | Get current user from token |

**Key Features**:
- Upsert pattern (creates OR updates)
- Returns `LoginResponseDto` with JWT + user info
- Token validation endpoint

### SurveysController

**Route**: `/api/surveys`

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/` | POST | Yes | Create survey |
| `/` | GET | Yes | List surveys (paginated) |
| `/{id}` | GET | Yes | Get survey details |
| `/{id}` | PUT | Yes | Update survey |
| `/{id}` | DELETE | Yes | Delete survey |
| `/{id}/activate` | POST | Yes | Activate survey |
| `/{id}/deactivate` | POST | Yes | Deactivate survey |
| `/code/{code}` | GET | **No** | Get by code (public) |
| `/{id}/statistics` | GET | Yes | Get statistics |

**Authorization**: User must own survey (except `/code/{code}`)

**Helper Method**:
```csharp
private int GetUserIdFromClaims()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
    {
        throw new UnauthorizedAccessException("Invalid authentication");
    }
    return userId;
}
```

### QuestionsController

**Routes**: `/api/surveys/{surveyId}/questions`, `/api/questions/{id}`

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/surveys/{surveyId}/questions` | POST | Yes | Add question |
| `/surveys/{surveyId}/questions` | GET | Optional | List questions |
| `/questions/{id}` | PUT | Yes | Update question |
| `/questions/{id}` | DELETE | Yes | Delete question |
| `/surveys/{surveyId}/questions/reorder` | POST | Yes | Reorder questions |

**Features**:
- Custom DTO validation (options for choice questions)
- Public access for active survey questions
- Reordering with transaction

### ResponsesController

**Routes**: `/api/surveys/{surveyId}/responses`, `/api/responses/{id}`

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/surveys/{surveyId}/responses` | POST | **No** | Start response |
| `/responses/{id}/answers` | POST | **No** | Save answer |
| `/responses/{id}/complete` | POST | **No** | Complete response |
| `/surveys/{surveyId}/responses` | GET | Yes | List responses |
| `/responses/{id}` | GET | Yes | Get response details |

**Key**: Response submission is PUBLIC (for bot integration), viewing requires auth

### BotController

**Route**: `/api/bot`

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/webhook` | POST | Secret | Telegram webhook |
| `/status` | GET | No | Bot status |
| `/health` | GET | No | Bot health |

**Webhook Endpoint**:
```csharp
[HttpPost("webhook")]
public async Task<IActionResult> Webhook([FromBody] Update update, ...)
{
    // Validate webhook secret
    if (!ValidateWebhookSecret()) return Unauthorized();

    // Queue for background processing (fire-and-forget)
    _backgroundTaskQueue.QueueBackgroundWorkItem(async ct =>
    {
        await _updateHandler.HandleUpdateAsync(update, ct);
    });

    // Return 200 OK immediately (Telegram requires < 60s)
    return Ok(new ApiResponse { Success = true, Message = "Update queued" });
}
```

**Secret Validation**:
```csharp
private bool ValidateWebhookSecret()
{
    if (!Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var token))
        return false;
    return token.ToString() == _botConfiguration.WebhookSecret;
}
```

### HealthController

**Route**: `/health`

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Basic health status |
| `/health/ready` | GET | Readiness check |
| `/health/live` | GET | Liveness check |

### MediaController (NEW in v1.3.0)

**Route**: `/api/media`

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/upload` | POST | Yes | Upload media file with auto-detection |
| `/{mediaId}` | DELETE | Yes | Delete media file |

**Upload Endpoint Features**:
- **Auto-detection**: Analyzes file content to determine type (magic bytes, MIME, extension)
- **Multi-format support**: Images, videos, audio, documents, archives
- **Size limits**: Type-specific (10-100 MB)
- **Thumbnail generation**: Automatic for images (200x200px)
- **Validation**: Comprehensive file validation with detailed error messages

**Request Example**:
```bash
POST /api/media/upload
Content-Type: multipart/form-data
Authorization: Bearer <token>

form-data:
- file: [binary file data]
- mediaType: "image" (optional, auto-detects if omitted)
```

**Response Example** (201 Created):
```json
{
  "success": true,
  "data": {
    "id": "abc123-def456-ghi789",
    "type": "image",
    "filePath": "/media/images/photo_20251121_123456.jpg",
    "thumbnailPath": "/media/thumbnails/photo_20251121_123456_thumb.jpg",
    "fileSize": 1048576,
    "mimeType": "image/jpeg",
    "uploadedAt": "2025-11-21T10:30:00Z"
  },
  "message": "Media uploaded successfully. Type: image"
}
```

**Supported File Types**:
- Images: jpg, png, gif, webp, bmp, tiff, svg (max 10 MB)
- Videos: mp4, webm, mov, avi, mkv, flv, wmv (max 50 MB)
- Audio: mp3, wav, ogg, m4a, flac, aac (max 20 MB)
- Documents: pdf, doc, docx, xls, xlsx, ppt, pptx, txt, csv (max 25 MB)
- Archives: zip, rar, 7z, tar, gz, bz2 (max 100 MB)

---

## Middleware

### GlobalExceptionMiddleware

**Location**: `Middleware/GlobalExceptionMiddleware.cs`

**Maps exceptions to HTTP status codes**:

```csharp
switch (exception)
{
    case ValidationException:        return 400 BadRequest;
    case BadRequestException:        return 400 BadRequest;
    case UnauthorizedException:      return 401 Unauthorized;
    case ForbiddenException:         return 403 Forbidden;
    case NotFoundException:          return 404 NotFound;
    case ConflictException:          return 409 Conflict;
    case SurveyNotFoundException:    return 404 NotFound;
    case SurveyValidationException:  return 400 BadRequest;
    default:                         return 500 InternalServerError;
}
```

**Features**:
- Standardized error responses (`ErrorResponse`)
- Stack trace only in Development
- Structured logging with TraceId

**Registration**:
```csharp
// MiddlewareExtensions.cs
public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
{
    return app.UseMiddleware<GlobalExceptionMiddleware>();
}

// Program.cs
app.UseGlobalExceptionHandler(); // MUST BE EARLY
```

### RequestLoggingMiddleware

**Location**: `Middleware/RequestLoggingMiddleware.cs`

**Features**:
- Logs request start/end
- Measures response time (Stopwatch)
- Log level by status code:
  - 500+ → Error
  - 400-499 → Warning
  - < 400 → Information

---

## Response Models

### ApiResponse<T>

**Location**: `Models/ApiResponse.cs`

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

**Usage**:
```csharp
return Ok(ApiResponse<SurveyDto>.Ok(survey, "Survey created successfully"));
```

**JSON**:
```json
{
  "success": true,
  "data": { "id": 1, "title": "Customer Survey" },
  "message": "Survey created successfully",
  "timestamp": "2025-11-10T10:30:00.000Z"
}
```

### ErrorResponse

**Location**: `Models/ErrorResponse.cs`

```csharp
public class ErrorResponse
{
    public bool Success { get; set; } = false;
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public string? Details { get; set; }  // Stack trace (dev only)
    public Dictionary<string, string[]>? Errors { get; set; }  // Validation errors
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }
}
```

---

## AutoMapper Configuration

**Profiles Location**: `Mapping/` directory

**Key Profiles**:
- `SurveyMappingProfile` - Survey mappings
- `QuestionMappingProfile` - Question mappings
- `ResponseMappingProfile` - Response mappings
- `AnswerMappingProfile` - Answer mappings
- `UserMappingProfile` - User mappings

**Value Resolvers**:
- `QuestionOptionsResolver` - Deserialize JSON to List<string>
- `AnswerJsonResolver` - Parse answer JSON by type
- `SurveyTotalResponsesResolver` - Count total responses
- `SurveyCompletedResponsesResolver` - Count completed

**Configuration Validation**:
```csharp
// AutoMapperConfigurationTest.cs
[Fact]
public void AutoMapper_Configuration_IsValid()
{
    var config = new MapperConfiguration(cfg =>
        cfg.AddMaps(typeof(Program).Assembly));
    config.AssertConfigurationIsValid(); // Throws if invalid
}
```

---

## Background Services

### BackgroundTaskQueue

**Location**: `Services/BackgroundTaskQueue.cs`

**Implementation**: `System.Threading.Channels` (high-performance)

```csharp
public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, Task>> _queue;

    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
    {
        _queue.Writer.TryWrite(workItem);
    }

    public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken ct)
    {
        return await _queue.Reader.ReadAsync(ct);
    }
}
```

**Features**:
- Bounded capacity (default 100)
- BoundedChannelFullMode.Wait (blocks when full)
- Thread-safe, async

### QueuedHostedService

**Location**: `Services/QueuedHostedService.cs`

**Processes queued tasks in background**:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        var workItem = await _taskQueue.DequeueAsync(stoppingToken);
        await workItem(stoppingToken);
    }
}
```

**Registration**:
```csharp
builder.Services.AddBackgroundTaskQueue(queueCapacity: 100);
```

**Usage**:
```csharp
_backgroundTaskQueue.QueueBackgroundWorkItem(async ct =>
{
    await _updateHandler.HandleUpdateAsync(update, ct);
});
```

---

## Authentication & Authorization

### JWT Configuration

**appsettings.json**:
```json
{
  "JwtSettings": {
    "SecretKey": "min-32-chars-for-HS256-algorithm",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Clients",
    "TokenLifetimeHours": 24
  }
}
```

**Program.cs Setup**:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            ClockSkew = TimeSpan.Zero  // No grace period
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context => { /* log */ },
            OnTokenValidated = context => { /* log */ },
            OnChallenge = context => { /* log */ }
        };
    });
```

### Using in Controllers

**Controller-level**:
```csharp
[Authorize]  // All endpoints require auth
public class SurveysController : ControllerBase { }
```

**Endpoint-level override**:
```csharp
[HttpGet("code/{code}")]
[AllowAnonymous]  // Public endpoint
public async Task<ActionResult> GetSurveyByCode(string code) { }
```

**Extract claims**:
```csharp
var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
var telegramId = long.Parse(User.FindFirst("TelegramId")!.Value);
var username = User.FindFirst(ClaimTypes.Name)?.Value;
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

    // JWT Bearer Authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and your token"
    });

    options.AddSecurityRequirement(/* global requirement */);
});
```

### Endpoint Documentation

```csharp
[HttpPost]
[SwaggerOperation(
    Summary = "Create a new survey",
    Description = "Creates survey for authenticated user. Inactive by default.",
    Tags = new[] { "Surveys" }
)]
[ProducesResponseType(typeof(ApiResponse<SurveyDto>), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateSurvey([FromBody] CreateSurveyDto dto)
```

**Access**: http://localhost:5000/swagger

---

## Health Checks

**Configuration**:
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SurveyBotDbContext>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "postgresql" });
```

**Endpoints**:
- `/health/db` - Basic DB health (Healthy/Unhealthy)
- `/health/db/details` - Detailed status + timestamp

---

## Configuration Files

**appsettings.json** (Base/Production):
- ConnectionStrings, JwtSettings, BotConfiguration, Serilog
- Committed to source control
- Default values

**appsettings.Development.json** (Overrides):
- BotToken, Debug logging, Development-specific settings
- OVERRIDES base settings when `ASPNETCORE_ENVIRONMENT=Development`
- Can be gitignored

**How Merging Works**:
1. Load appsettings.json (base)
2. Load appsettings.{Environment}.json (overrides)
3. Settings in environment file win

---

## Common Patterns

### Controller Endpoint Pattern

```csharp
[HttpPost]
[Authorize]
public async Task<ActionResult<ApiResponse<ResultDto>>> Endpoint([FromBody] RequestDto dto)
{
    try
    {
        // 1. Validate ModelState
        if (!ModelState.IsValid)
            return BadRequest(/* error response */);

        // 2. Get user ID from claims
        var userId = GetUserIdFromClaims();

        // 3. Delegate to service
        var result = await _service.OperationAsync(userId, dto);

        // 4. Return standardized response
        return Ok(ApiResponse<ResultDto>.Ok(result, "Success"));
    }
    catch (SpecificException ex)
    {
        _logger.LogWarning(ex, "Specific error");
        return BadRequest(/* error response */);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error");
        return StatusCode(500, /* error response */);
    }
}
```

---

## Best Practices

1. **Thin Controllers**: Business logic in services, not controllers
2. **Consistent Responses**: Always use `ApiResponse<T>` wrapper
3. **Exception Handling**: Let middleware catch unhandled exceptions
4. **Logging**: Structured logging with context (TraceId, UserId)
5. **Authentication**: Extract user ID once, pass to services
6. **Swagger**: Document all endpoints with `[SwaggerOperation]`
7. **Validation**: Check `ModelState.IsValid`, use Data Annotations

---

## Common Issues

### Issue: 401 Unauthorized

**Solutions**:
1. Check SecretKey is ≥ 32 chars
2. Verify token format: `Bearer <token>`
3. Check Issuer/Audience match
4. Ensure token not expired (24h default)

### Issue: Middleware Not Working

**Solution**: Check order in Program.cs (authentication BEFORE authorization)

### Issue: CORS Errors

**Solution**: Add CORS policy:
```csharp
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

app.UseCors("AllowAll"); // Before UseAuthentication
```

---

## Summary

**SurveyBot.API** provides production-ready REST API with:

- **Clean Architecture**: Orchestrates Core, Infrastructure, Bot layers
- **JWT Authentication**: Telegram-based login with token claims
- **Global Exception Handling**: Standardized error responses
- **Swagger Documentation**: Interactive API explorer
- **Background Processing**: Non-blocking webhook handling
- **Health Checks**: Database connectivity monitoring
- **AutoMapper**: Entity-to-DTO mapping
- **Serilog**: Structured logging with enrichment

**Middleware Order**: Logging → Exception → Swagger → HTTPS → Auth → Authorization → Controllers

**Key Controllers**: Auth (JWT), Surveys (CRUD), Questions, Responses (public), Bot (webhook), Media (upload/delete)

---

## Related Documentation

### Documentation Hub

For comprehensive project documentation, see the **centralized documentation folder**:

**Main Documentation**:
- [Project Root CLAUDE.md](../../CLAUDE.md) - Overall project overview and quick start
- [Documentation Index](../../documentation/INDEX.md) - Complete documentation catalog
- [Navigation Guide](../../documentation/NAVIGATION.md) - Role-based navigation

**API Documentation**:
- [API Quick Reference](../../documentation/api/QUICK-REFERENCE.md) - Quick endpoint reference
- [API Reference](../../documentation/api/API_REFERENCE.md) - Complete API documentation
- [Phase 2 API Reference](../../documentation/api/PHASE2_API_REFERENCE.md) - Phase 2 endpoints
- [Logging & Error Handling](../../documentation/api/LOGGING-ERROR-HANDLING.md) - Error handling patterns

**Related Layer Documentation**:
- [Core Layer](../SurveyBot.Core/CLAUDE.md) - Domain entities, DTOs, interfaces
- [Infrastructure Layer](../SurveyBot.Infrastructure/CLAUDE.md) - Services and repositories used by API
- [Bot Layer](../SurveyBot.Bot/CLAUDE.md) - Telegram bot integrated with API
- [Frontend](../../frontend/CLAUDE.md) - React admin panel consuming this API

**Authentication & Authorization**:
- [Authentication Flow](../../documentation/auth/AUTHENTICATION_FLOW.md) - JWT authentication process

**Development Resources**:
- [DI Structure](../../documentation/development/DI-STRUCTURE.md) - Dependency injection patterns
- [Developer Onboarding](../../documentation/DEVELOPER_ONBOARDING.md) - Getting started guide
- [Troubleshooting](../../documentation/TROUBLESHOOTING.md) - Common API issues

**Deployment**:
- [Docker Startup Guide](../../documentation/deployment/DOCKER-STARTUP-GUIDE.md) - Docker setup
- [Docker README](../../documentation/deployment/DOCKER-README.md) - Production deployment
- [README Docker](../../documentation/deployment/README-DOCKER.md) - Additional Docker info

**Testing**:
- [Test Summary](../../documentation/testing/TEST_SUMMARY.md) - Test coverage
- [Phase 2 Testing Guide](../../documentation/testing/PHASE2_TESTING_GUIDE.md) - Testing procedures
- [Manual Testing Checklist](../../documentation/testing/MANUAL_TESTING_MEDIA_CHECKLIST.md) - Media testing

### Documentation Maintenance

**When updating API layer**:
1. Update this CLAUDE.md file with controller/middleware changes
2. Update [API Quick Reference](../../documentation/api/QUICK-REFERENCE.md) if adding/changing endpoints
3. Update [API Reference](../../documentation/api/API_REFERENCE.md) with detailed endpoint documentation
4. Update [Logging & Error Handling](../../documentation/api/LOGGING-ERROR-HANDLING.md) if error handling changes
5. Update [Authentication Flow](../../documentation/auth/AUTHENTICATION_FLOW.md) if auth logic changes
6. Update [Main CLAUDE.md](../../CLAUDE.md) API Endpoints section if major changes
7. Update [Frontend CLAUDE.md](../../frontend/CLAUDE.md) if API changes affect frontend
8. Update [Documentation Index](../../documentation/INDEX.md) if adding significant documentation

**Where to save API-related documentation**:
- Technical implementation details → This file
- API endpoint reference → `documentation/api/API_REFERENCE.md`
- Quick reference → `documentation/api/QUICK-REFERENCE.md`
- Error handling patterns → `documentation/api/LOGGING-ERROR-HANDLING.md`
- Authentication flows → `documentation/auth/`
- Testing procedures → `documentation/testing/`
- Deployment guides → `documentation/deployment/`

**Swagger Documentation**:
- Keep Swagger annotations up to date in controller code
- Access Swagger UI: `http://localhost:5000/swagger`
- Use Swagger as source of truth for API contract

**Breaking Changes**:
- Document breaking changes in [Main CLAUDE.md](../../CLAUDE.md)
- Update API version if necessary
- Notify frontend team via [Frontend CLAUDE.md](../../frontend/CLAUDE.md)
- Update all API documentation files

---

**Last Updated**: 2025-11-21 | **Version**: 1.3.0
