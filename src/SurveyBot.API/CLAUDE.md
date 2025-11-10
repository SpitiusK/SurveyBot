# SurveyBot.API - REST API Presentation Layer Documentation

## Project Overview

**SurveyBot.API** is the presentation layer of the SurveyBot application, serving as the primary entry point for HTTP requests. It exposes a comprehensive REST API for managing surveys, questions, responses, and user authentication. This layer orchestrates the entire application stack, integrating the domain logic (Core), data access (Infrastructure), and Telegram bot functionality (Bot).

### Purpose and Responsibilities

- **Expose REST API Endpoints**: Provide HTTP endpoints for all survey management operations
- **Handle HTTP Request/Response**: Process incoming requests and format responses
- **Implement Authentication**: JWT-based authentication and authorization
- **Configure Application**: Set up dependency injection, middleware pipeline, and services
- **Host Telegram Bot**: Provide webhook endpoint for Telegram bot updates
- **Centralized Logging**: Serilog integration for structured logging
- **API Documentation**: Swagger/OpenAPI documentation for developers
- **Error Handling**: Global exception handling with standardized error responses
- **Background Processing**: Queue-based background task processing for webhook updates

### Key Technologies and Versions

- **ASP.NET Core 8.0** - Web framework
- **Entity Framework Core 9.0** - ORM (configured here)
- **AutoMapper 12.0** - Object-to-object mapping
- **Serilog 9.0** - Structured logging
- **Swashbuckle 6.6.2** - Swagger/OpenAPI documentation
- **JWT Bearer Authentication 8.0** - Token-based authentication
- **Health Checks** - Application health monitoring
- **System.Threading.Channels** - High-performance background task queue

### Architecture Principles

1. **Clean Architecture**: API layer depends on Core, Infrastructure, and Bot layers
2. **Separation of Concerns**: Controllers handle HTTP, services handle business logic
3. **Dependency Injection**: Constructor injection throughout, configured in Program.cs
4. **Thin Controllers**: Controllers delegate to services, minimal logic
5. **Standardized Responses**: All endpoints return `ApiResponse<T>` wrapper
6. **Centralized Error Handling**: Global exception middleware catches all errors
7. **Logging Best Practices**: Structured logging with Serilog, log context enrichment

---

## Project Structure

```
SurveyBot.API/
├── Controllers/                          # REST API Controllers
│   ├── AuthController.cs                 # Authentication & JWT token management
│   ├── SurveysController.cs              # Survey CRUD operations
│   ├── QuestionsController.cs            # Question management
│   ├── ResponsesController.cs            # Survey response submission
│   ├── UsersController.cs                # User management (TODO)
│   ├── BotController.cs                  # Telegram webhook endpoint
│   ├── HealthController.cs               # Health check endpoints
│   └── TestErrorsController.cs           # Error testing endpoints (dev)
│
├── Middleware/                           # Custom Middleware
│   ├── GlobalExceptionMiddleware.cs      # Centralized exception handling
│   └── RequestLoggingMiddleware.cs       # HTTP request/response logging
│
├── Models/                               # API Response Models
│   ├── ApiResponse.cs                    # Standard success response wrapper
│   └── ErrorResponse.cs                  # Standard error response
│
├── Mapping/                              # AutoMapper Configuration
│   ├── SurveyMappingProfile.cs           # Survey entity mappings
│   ├── QuestionMappingProfile.cs         # Question entity mappings
│   ├── ResponseMappingProfile.cs         # Response entity mappings
│   ├── AnswerMappingProfile.cs           # Answer entity mappings
│   ├── UserMappingProfile.cs             # User entity mappings
│   ├── StatisticsMappingProfile.cs       # Statistics DTO mappings
│   ├── AutoMapperConfigurationTest.cs    # Mapping validation test
│   ├── Resolvers/                        # Custom mapping resolvers (legacy)
│   │   ├── JsonToOptionsResolver.cs
│   │   ├── OptionsToJsonResolver.cs
│   │   ├── AnswersCountResolver.cs
│   │   └── ResponsePercentageResolver.cs
│   └── ValueResolvers/                   # Value resolvers
│       ├── QuestionOptionsResolver.cs    # Deserialize question options JSON
│       ├── QuestionOptionsJsonResolver.cs # Serialize question options
│       ├── AnswerJsonResolver.cs         # Parse answer JSON by type
│       ├── AnswerSelectedOptionsResolver.cs
│       ├── AnswerRatingValueResolver.cs
│       ├── SurveyTotalResponsesResolver.cs
│       ├── SurveyCompletedResponsesResolver.cs
│       ├── ResponseAnsweredCountResolver.cs
│       └── ResponseTotalQuestionsResolver.cs
│
├── Extensions/                           # Service Registration Extensions
│   ├── DatabaseExtensions.cs             # EF Core & database configuration
│   ├── RepositoryExtensions.cs           # Repository DI registration
│   ├── SwaggerExtensions.cs              # Swagger/OpenAPI configuration
│   ├── MiddlewareExtensions.cs           # Middleware registration helpers
│   ├── HealthCheckExtensions.cs          # Health check configuration
│   ├── AutoMapperExtensions.cs           # AutoMapper configuration
│   └── BackgroundServiceExtensions.cs    # Background task queue setup
│
├── Services/                             # Background Services
│   ├── IBackgroundTaskQueue.cs           # Task queue interface
│   ├── BackgroundTaskQueue.cs            # Thread-safe task queue implementation
│   └── QueuedHostedService.cs            # Background worker service
│
├── Exceptions/                           # API-Specific Exceptions
│   ├── ApiException.cs                   # Base API exception
│   ├── NotFoundException.cs              # 404 exception
│   ├── ValidationException.cs            # 400 validation exception
│   ├── BadRequestException.cs            # 400 bad request exception
│   ├── UnauthorizedException.cs          # 401 unauthorized exception
│   ├── ForbiddenException.cs             # 403 forbidden exception
│   └── ConflictException.cs              # 409 conflict exception
│
├── Properties/
│   └── launchSettings.json               # Development launch profiles
│
├── Program.cs                            # Application entry point & configuration
├── VerifyDI.cs                           # DI verification utility (dev)
├── appsettings.json                      # Production configuration
├── appsettings.Development.json          # Development configuration
└── SurveyBot.API.csproj                  # Project file
```

---

## Program.cs - Application Entry Point

The `Program.cs` file is the heart of the application. It configures all services, middleware, and starts the application.

### Complete Program.cs Structure

```csharp
// Lines 1-13: Using statements
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SurveyBot.API.Extensions;
using SurveyBot.Bot.Extensions;
using SurveyBot.Core.Configuration;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Infrastructure.Repositories;
using SurveyBot.Infrastructure.Services;

// Lines 14-23: Early Serilog configuration (BEFORE WebApplicationBuilder)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "SurveyBot.API")
    .CreateLogger();

// Lines 25-304: Main try-catch-finally block
try
{
    Log.Information("Starting SurveyBot API application");

    // Line 29: Create WebApplicationBuilder
    var builder = WebApplication.CreateBuilder(args);

    // Line 32: Replace default logging with Serilog
    builder.Host.UseSerilog();

    // Lines 36-190: SERVICE CONFIGURATION (Dependency Injection)
    // ... (detailed below)

    // Line 192: Build the application
    var app = builder.Build();

    // Lines 194-204: Initialize Telegram Bot
    // ... (detailed below)

    // Lines 206-211: Verify DI Configuration (Development only)
    // ... (detailed below)

    // Lines 213-293: MIDDLEWARE PIPELINE CONFIGURATION
    // ... (detailed below)

    // Lines 294-295: Run the application
    Log.Information("SurveyBot API started successfully");
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

// Lines 306-307: Make Program public for integration tests
public partial class Program { }
```

### Service Configuration (Lines 36-190)

Services are registered in a specific order. Here's the complete breakdown:

```csharp
// 1. Controllers (Line 37)
builder.Services.AddControllers();

// 2. AutoMapper (Line 40)
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// 3. Entity Framework Core with PostgreSQL (Lines 43-54)
builder.Services.AddDbContext<SurveyBotDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);

    // Enable detailed logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// 4. JWT Settings Configuration (Lines 56-63)
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
    throw new InvalidOperationException("JWT settings are not properly configured in appsettings.json");
}

// 5. JWT Authentication (Lines 66-108)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
    };

    // Add logging for JWT authentication events
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Log.Debug("JWT token validated for user ID: {UserId}", userId);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Log.Warning("JWT authentication challenge: {Error}", context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

// 6. Authorization (Line 111)
builder.Services.AddAuthorization();

// 7. Repository Implementations - Scoped (Lines 113-118)
builder.Services.AddScoped<ISurveyRepository, SurveyRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IResponseRepository, ResponseRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();

// 8. Service Implementations - Scoped (Lines 120-125)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISurveyService, SurveyService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IResponseService, ResponseService>();
builder.Services.AddScoped<IUserService, UserService>();

// 9. Telegram Bot Services (Lines 127-129)
builder.Services.AddTelegramBot(builder.Configuration);
builder.Services.AddBotHandlers();

// 10. Background Task Queue (Line 132)
builder.Services.AddBackgroundTaskQueue(queueCapacity: 100);

// 11. Health Checks (Lines 134-139)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SurveyBotDbContext>(
        name: "database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "postgresql" });

// 12. Swagger/OpenAPI (Lines 141-190)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SurveyBot API",
        Version = "v1",
        Description = "REST API for Telegram Survey Bot - Managing surveys, questions, and responses",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "SurveyBot API Team",
            Email = "support@surveybot.com"
        }
    });

    // Enable XML documentation comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Configure JWT Bearer Authentication for Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

### Telegram Bot Initialization (Lines 194-204)

```csharp
try
{
    await app.Services.InitializeTelegramBotAsync();
    Log.Information("Telegram Bot initialized successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to initialize Telegram Bot. The application will continue without bot functionality.");
    // Don't throw - allow API to start even if bot initialization fails
}
```

**Key Points**:
- Bot initialization happens AFTER `app.Build()` but BEFORE middleware configuration
- Failures are logged but don't crash the application
- API can run without bot functionality (useful for testing)

### DI Verification (Lines 206-211)

```csharp
if (app.Environment.IsDevelopment())
{
    var (success, errors) = SurveyBot.API.DIVerifier.VerifyServiceResolution(app.Services);
    SurveyBot.API.DIVerifier.PrintVerificationResults(success, errors);
}
```

**Purpose**: Verifies all services can be resolved from DI container during development

### Middleware Pipeline (Lines 213-292)

**CRITICAL**: Middleware order matters! Here's the exact order:

```csharp
// 1. Serilog Request Logging (Lines 216-224)
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
    };
});

// 2. Custom Request Logging (Line 227)
app.UseRequestLogging();

// 3. Global Exception Handler (Line 230) - MUST BE EARLY
app.UseGlobalExceptionHandler();

// 4. Swagger (Lines 232-245) - Development Only
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SurveyBot API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "SurveyBot API Documentation";
        options.DisplayRequestDuration();
        options.EnableDeepLinking();
        options.EnableFilter();
        options.ShowExtensions();
    });
}

// 5. HTTPS Redirection (Line 247)
app.UseHttpsRedirection();

// 6. Authentication (Line 250) - MUST come before Authorization
app.UseAuthentication();

// 7. Authorization (Line 251)
app.UseAuthorization();

// 8. Map Controllers (Line 254)
app.MapControllers();

// 9. Health Check Endpoint (Line 257)
app.MapHealthChecks("/health/db");

// 10. Detailed DB Health Check (Lines 260-292)
app.MapGet("/health/db/details", async (SurveyBotDbContext dbContext) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();

        if (canConnect)
        {
            return Results.Ok(new
            {
                status = "healthy",
                database = "connected",
                timestamp = DateTime.UtcNow
            });
        }

        return Results.Problem(
            detail: "Cannot connect to database",
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: $"Database connection error: {ex.Message}",
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }
})
.WithName("DatabaseHealthCheckDetails")
.WithOpenApi()
.WithTags("Health");
```

**Middleware Order Rules**:
1. Exception handling EARLY (catches errors from other middleware)
2. Authentication BEFORE Authorization
3. HTTPS redirection BEFORE authentication
4. Routing (MapControllers) AFTER authentication/authorization

---

## Controllers

All controllers follow consistent patterns and conventions.

### Base Controller Pattern

```csharp
[ApiController]
[Route("api/[controller]")]  // or custom route
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

    // Endpoints with consistent patterns
}
```

### AuthController (Lines 1-282)

**Route**: `/api/auth`
**Purpose**: Authentication, JWT token management, user registration

**Endpoints**:

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/login` | No | Login with Telegram credentials, get JWT |
| POST | `/refresh` | No | Refresh JWT token (MVP placeholder) |
| GET | `/validate` | Yes | Validate current JWT token |
| POST | `/register` | No | Register/update user (upsert pattern) |
| GET | `/me` | Yes | Get current user from token claims |

**Key Features**:
- Upsert pattern for registration (creates or updates)
- Returns `LoginResponseDto` with JWT token and user info
- Token validation endpoint for clients
- Detailed logging for auth events

**Example - Login Endpoint (Lines 41-84)**:

```csharp
[HttpPost("login")]
[AllowAnonymous]
[SwaggerOperation(
    Summary = "Login and get JWT token",
    Description = "Authenticates a user by Telegram ID and returns a JWT access token. Creates a new user if one doesn't exist.",
    Tags = new[] { "Authentication" }
)]
[ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
{
    try
    {
        _logger.LogInformation("Login request received for Telegram ID: {TelegramId}", request.TelegramId);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid login request model state");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid request data"
            });
        }

        var result = await _authService.LoginAsync(request);

        _logger.LogInformation("Login successful for user ID: {UserId}", result.UserId);

        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Login successful"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during login for Telegram ID: {TelegramId}", request.TelegramId);
        return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
        {
            Success = false,
            Message = "An error occurred during login"
        });
    }
}
```

### SurveysController (Lines 1-623)

**Route**: `/api/surveys`
**Purpose**: Complete survey lifecycle management

**Endpoints**:

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/` | Yes | Create new survey |
| GET | `/` | Yes | List user's surveys (paginated) |
| GET | `/{id}` | Yes | Get survey details with questions |
| PUT | `/{id}` | Yes | Update survey |
| DELETE | `/{id}` | Yes | Delete survey |
| POST | `/{id}/activate` | Yes | Activate survey for responses |
| POST | `/{id}/deactivate` | Yes | Deactivate survey |
| GET | `/code/{code}` | **No** | Get survey by code (public) |
| GET | `/{id}/statistics` | Yes | Get survey statistics |

**Key Features**:
- All require authentication except `/code/{code}`
- Ownership validation (user must own survey)
- Pagination with filtering and sorting
- Statistics endpoint with comprehensive analytics
- Soft delete for surveys with responses

**Example - Create Survey (Lines 46-98)**:

```csharp
[HttpPost]
[SwaggerOperation(
    Summary = "Create a new survey",
    Description = "Creates a new survey for the authenticated user. Survey is created as inactive by default.",
    Tags = new[] { "Surveys" }
)]
[ProducesResponseType(typeof(ApiResponse<SurveyDto>), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateSurvey([FromBody] CreateSurveyDto dto)
{
    try
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for survey creation");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid request data",
                Data = ModelState
            });
        }

        var userId = GetUserIdFromClaims();
        _logger.LogInformation("Creating survey for user {UserId}", userId);

        var survey = await _surveyService.CreateSurveyAsync(userId, dto);

        return CreatedAtAction(
            nameof(GetSurveyById),
            new { id = survey.Id },
            ApiResponse<SurveyDto>.Ok(survey, "Survey created successfully"));
    }
    catch (SurveyValidationException ex)
    {
        _logger.LogWarning(ex, "Validation error during survey creation");
        return BadRequest(new ApiResponse<object>
        {
            Success = false,
            Message = ex.Message
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating survey");
        return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
        {
            Success = false,
            Message = "An error occurred while creating the survey"
        });
    }
}
```

**Helper Method - Get User ID from Claims (Lines 608-619)**:

```csharp
private int GetUserIdFromClaims()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
    {
        _logger.LogError("Failed to extract user ID from claims");
        throw new Core.Exceptions.UnauthorizedAccessException("Invalid or missing user authentication");
    }

    return userId;
}
```

### QuestionsController (Lines 1-525)

**Route**: `/api/surveys/{surveyId}/questions` and `/api/questions/{id}`
**Purpose**: Question management within surveys

**Endpoints**:

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/surveys/{surveyId}/questions` | Yes | Add question to survey |
| GET | `/api/surveys/{surveyId}/questions` | Optional | List survey questions |
| GET | `/api/questions/{id}` | N/A | Get question details (not implemented) |
| PUT | `/api/questions/{id}` | Yes | Update question |
| DELETE | `/api/questions/{id}` | Yes | Delete question |
| POST | `/api/surveys/{surveyId}/questions/reorder` | Yes | Reorder questions |

**Key Features**:
- Custom DTO validation (checks options for choice questions)
- Questions list is public for active surveys, restricted for inactive
- Reordering validates all question IDs belong to survey
- Cannot modify/delete questions with responses

**Example - Add Question (Lines 48-146)**:

```csharp
[HttpPost("surveys/{surveyId}/questions")]
[Authorize]
[SwaggerOperation(
    Summary = "Add question to survey",
    Description = "Creates a new question for the specified survey. User must own the survey. Options are required for choice-based questions.",
    Tags = new[] { "Questions" }
)]
[ProducesResponseType(typeof(ApiResponse<QuestionDto>), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<ApiResponse<QuestionDto>>> CreateQuestion(
    int surveyId,
    [FromBody] CreateQuestionDto dto)
{
    try
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for question creation in survey {SurveyId}", surveyId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid request data",
                Data = ModelState
            });
        }

        // Validate custom validation logic in DTO
        var validationResults = dto.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(dto)).ToList();
        if (validationResults.Any())
        {
            _logger.LogWarning("Validation failed for question creation: {Errors}",
                string.Join(", ", validationResults.Select(v => v.ErrorMessage)));

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = validationResults.First().ErrorMessage ?? "Validation failed"
            });
        }

        var userId = GetUserIdFromClaims();
        _logger.LogInformation("Creating question for survey {SurveyId} by user {UserId}", surveyId, userId);

        var question = await _questionService.AddQuestionAsync(surveyId, userId, dto);

        return CreatedAtAction(
            nameof(GetQuestionsBySurvey),
            new { surveyId = question.SurveyId },
            ApiResponse<QuestionDto>.Ok(question, "Question created successfully"));
    }
    catch (SurveyNotFoundException ex)
    {
        _logger.LogWarning(ex, "Survey {SurveyId} not found", surveyId);
        return NotFound(new ApiResponse<object>
        {
            Success = false,
            Message = ex.Message
        });
    }
    // ... more exception handling
}
```

### ResponsesController (Lines 1-498)

**Route**: `/api/surveys/{surveyId}/responses` and `/api/responses/{id}`
**Purpose**: Survey response submission and retrieval

**Endpoints**:

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/surveys/{surveyId}/responses` | Yes | List survey responses (paginated) |
| GET | `/api/responses/{id}` | Yes | Get response details with answers |
| POST | `/api/surveys/{surveyId}/responses` | **No** | Create new response (start survey) |
| POST | `/api/responses/{id}/answers` | **No** | Save answer to question |
| POST | `/api/responses/{id}/complete` | **No** | Complete response |

**Key Features**:
- Response lifecycle: Start → Answer → Complete
- Public endpoints for bot integration (no auth)
- Answer validation by question type
- Duplicate response prevention
- Only survey creator can view responses

**Example - Save Answer (Lines 320-417)**:

```csharp
[HttpPost("responses/{id}/answers")]
[AllowAnonymous]
[SwaggerOperation(
    Summary = "Save answer to question",
    Description = "Saves an individual answer to a question within an ongoing response. Public endpoint used by the Telegram bot.",
    Tags = new[] { "Responses" }
)]
[ProducesResponseType(typeof(ApiResponse<ResponseDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<ApiResponse<ResponseDto>>> SaveAnswer(
    int id,
    [FromBody] SubmitAnswerDto dto)
{
    try
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for answer submission");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid request data",
                Data = ModelState
            });
        }

        _logger.LogInformation(
            "Saving answer for response {ResponseId}, question {QuestionId}",
            id,
            dto.Answer.QuestionId);

        // Validate answer format before saving
        var validationResult = await _responseService.ValidateAnswerFormatAsync(
            dto.Answer.QuestionId,
            dto.Answer.AnswerText,
            dto.Answer.SelectedOptions,
            dto.Answer.RatingValue);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Invalid answer format for question {QuestionId}: {Error}",
                dto.Answer.QuestionId,
                validationResult.ErrorMessage);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = validationResult.ErrorMessage
            });
        }

        var response = await _responseService.SaveAnswerAsync(
            id,
            dto.Answer.QuestionId,
            dto.Answer.AnswerText,
            dto.Answer.SelectedOptions,
            dto.Answer.RatingValue);

        return Ok(ApiResponse<ResponseDto>.Ok(response, "Answer saved successfully"));
    }
    catch (ResponseNotFoundException ex)
    {
        _logger.LogWarning(ex, "Response {ResponseId} not found", id);
        return NotFound(new ApiResponse<object>
        {
            Success = false,
            Message = ex.Message
        });
    }
    // ... more exception handling
}
```

### BotController (Lines 1-245)

**Route**: `/api/bot`
**Purpose**: Telegram webhook endpoint

**Endpoints**:

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/webhook` | Secret | Receive Telegram updates |
| GET | `/status` | No | Get bot status and configuration |
| GET | `/health` | No | Bot webhook health check |

**Key Features**:
- Webhook secret validation via header
- Background task queuing for updates
- Returns 200 OK immediately (Telegram requires < 60s response)
- Error handling for update processing

**Webhook Endpoint (Lines 50-137)**:

```csharp
[HttpPost("webhook")]
[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> Webhook([FromBody] Update update, CancellationToken cancellationToken = default)
{
    try
    {
        // Validate webhook secret from header
        if (!ValidateWebhookSecret())
        {
            _logger.LogWarning("Webhook request rejected: Invalid or missing secret token");
            return Unauthorized(new ErrorResponse
            {
                Success = false,
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "Invalid webhook secret"
            });
        }

        // Validate update
        if (update == null)
        {
            _logger.LogWarning("Webhook request rejected: Update is null");
            return BadRequest(new ErrorResponse
            {
                Success = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Update cannot be null"
            });
        }

        _logger.LogInformation(
            "Webhook received update {UpdateId} of type {UpdateType}",
            update.Id,
            update.Type);

        // Queue update for background processing (fire-and-forget)
        _backgroundTaskQueue.QueueBackgroundWorkItem(async ct =>
        {
            try
            {
                _logger.LogDebug("Processing update {UpdateId} in background", update.Id);
                await _updateHandler.HandleUpdateAsync(update, ct);
                _logger.LogDebug("Update {UpdateId} processed successfully", update.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing update {UpdateId} in background",
                    update.Id);

                // Handle error through the update handler
                try
                {
                    await _updateHandler.HandleErrorAsync(ex, ct);
                }
                catch (Exception errorHandlerEx)
                {
                    _logger.LogError(
                        errorHandlerEx,
                        "Error handler failed for update {UpdateId}",
                        update.Id);
                }
            }
        });

        // Return 200 OK immediately (Telegram requires response within 60 seconds)
        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Update received and queued for processing"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error handling webhook request");

        // Still return 200 to Telegram to avoid retries
        // Log the error for investigation
        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Update received"
        });
    }
}
```

**Webhook Secret Validation (Lines 188-212)**:

```csharp
private bool ValidateWebhookSecret()
{
    // In development or if webhook is not configured, skip validation
    if (!_botConfiguration.UseWebhook || string.IsNullOrWhiteSpace(_botConfiguration.WebhookSecret))
    {
        _logger.LogWarning("Webhook secret validation skipped (webhook not configured or no secret)");
        return true; // Allow for development/testing
    }

    // Check for Telegram's secret token header
    if (!Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var secretToken))
    {
        _logger.LogWarning("X-Telegram-Bot-Api-Secret-Token header not found");
        return false;
    }

    var isValid = secretToken.ToString() == _botConfiguration.WebhookSecret;

    if (!isValid)
    {
        _logger.LogWarning("Invalid webhook secret token received");
    }

    return isValid;
}
```

### HealthController (Lines 1-90)

**Route**: `/health`
**Purpose**: Health, readiness, and liveness checks

**Endpoints**:

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/health` | Basic health status |
| GET | `/health/ready` | Readiness check |
| GET | `/health/live` | Liveness check |

**Example - Basic Health (Lines 32-48)**:

```csharp
[HttpGet]
[ProducesResponseType(StatusCodes.Status200OK)]
public IActionResult GetHealth()
{
    var response = new
    {
        success = true,
        status = "healthy",
        service = "SurveyBot API",
        version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
        timestamp = DateTime.UtcNow
    };

    _logger.LogInformation("Health check requested - Status: Healthy");

    return Ok(response);
}
```

### TestErrorsController (Lines 1-123)

**Route**: `/api/TestErrors`
**Purpose**: Test exception handling and logging (Development only)

**Endpoints**:

| Method | Route | Throws |
|--------|-------|--------|
| GET | `/logging` | N/A (logs all levels) |
| GET | `/error` | `InvalidOperationException` |
| GET | `/not-found` | `NotFoundException` |
| GET | `/validation` | `ValidationException` |
| GET | `/bad-request` | `BadRequestException` |
| GET | `/unauthorized` | `UnauthorizedException` |
| GET | `/forbidden` | `ForbiddenException` |
| GET | `/conflict` | `ConflictException` |

---

## Middleware

### GlobalExceptionMiddleware (Lines 1-163)

**Purpose**: Centralized exception handling - catches all unhandled exceptions and returns standardized error responses

**Key Features**:
- Catches specific exception types and maps to HTTP status codes
- Logs errors with context (TraceId, path, method)
- Returns `ErrorResponse` model
- Shows stack trace only in Development
- Differentiates between validation errors (400), not found (404), unauthorized (401), forbidden (403), conflict (409), and server errors (500)

**Exception Mapping (Lines 51-151)**:

```csharp
switch (exception)
{
    case ValidationException validationEx:
        response.StatusCode = (int)HttpStatusCode.BadRequest;
        errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
        errorResponse.Message = validationEx.Message;
        errorResponse.Errors = validationEx.ValidationErrors;

        _logger.LogWarning(validationEx,
            "Validation error occurred. TraceId: {TraceId}. Errors: {@Errors}",
            context.TraceIdentifier,
            validationEx.ValidationErrors);
        break;

    case NotFoundException notFoundEx:
        response.StatusCode = (int)HttpStatusCode.NotFound;
        errorResponse.StatusCode = (int)HttpStatusCode.NotFound;
        errorResponse.Message = notFoundEx.Message;

        _logger.LogWarning(notFoundEx,
            "Resource not found. TraceId: {TraceId}. Message: {Message}",
            context.TraceIdentifier,
            notFoundEx.Message);
        break;

    case BadRequestException badRequestEx:
        response.StatusCode = (int)HttpStatusCode.BadRequest;
        errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
        errorResponse.Message = badRequestEx.Message;
        errorResponse.Details = _environment.IsDevelopment() ? badRequestEx.Details : null;

        _logger.LogWarning(badRequestEx,
            "Bad request. TraceId: {TraceId}. Message: {Message}",
            context.TraceIdentifier,
            badRequestEx.Message);
        break;

    case UnauthorizedException unauthorizedEx:
        response.StatusCode = (int)HttpStatusCode.Unauthorized;
        errorResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
        errorResponse.Message = unauthorizedEx.Message;

        _logger.LogWarning(unauthorizedEx,
            "Unauthorized access attempt. TraceId: {TraceId}. Path: {Path}",
            context.TraceIdentifier,
            context.Request.Path);
        break;

    case ForbiddenException forbiddenEx:
        response.StatusCode = (int)HttpStatusCode.Forbidden;
        errorResponse.StatusCode = (int)HttpStatusCode.Forbidden;
        errorResponse.Message = forbiddenEx.Message;

        _logger.LogWarning(forbiddenEx,
            "Forbidden access attempt. TraceId: {TraceId}. Path: {Path}",
            context.TraceIdentifier,
            context.Request.Path);
        break;

    case ConflictException conflictEx:
        response.StatusCode = (int)HttpStatusCode.Conflict;
        errorResponse.StatusCode = (int)HttpStatusCode.Conflict;
        errorResponse.Message = conflictEx.Message;
        errorResponse.Details = _environment.IsDevelopment() ? conflictEx.Details : null;

        _logger.LogWarning(conflictEx,
            "Conflict occurred. TraceId: {TraceId}. Message: {Message}",
            context.TraceIdentifier,
            conflictEx.Message);
        break;

    case ApiException apiEx:
        response.StatusCode = (int)apiEx.StatusCode;
        errorResponse.StatusCode = (int)apiEx.StatusCode;
        errorResponse.Message = apiEx.Message;
        errorResponse.Details = _environment.IsDevelopment() ? apiEx.Details : null;

        _logger.LogError(apiEx,
            "API exception occurred. TraceId: {TraceId}. StatusCode: {StatusCode}. Message: {Message}",
            context.TraceIdentifier,
            apiEx.StatusCode,
            apiEx.Message);
        break;

    default:
        response.StatusCode = (int)HttpStatusCode.InternalServerError;
        errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
        errorResponse.Message = _environment.IsDevelopment()
            ? exception.Message
            : "An internal server error occurred.";
        errorResponse.Details = _environment.IsDevelopment()
            ? exception.StackTrace
            : null;

        _logger.LogError(exception,
            "Unhandled exception occurred. TraceId: {TraceId}. Path: {Path}. Method: {Method}",
            context.TraceIdentifier,
            context.Request.Path,
            context.Request.Method);
        break;
}
```

**Registration**:

```csharp
// In MiddlewareExtensions.cs
public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
{
    return app.UseMiddleware<GlobalExceptionMiddleware>();
}

// In Program.cs
app.UseGlobalExceptionHandler();
```

### RequestLoggingMiddleware (Lines 1-60)

**Purpose**: Log HTTP request and response information

**Key Features**:
- Logs request method and path at start
- Measures response time with Stopwatch
- Log level based on status code:
  - 500+ → Error
  - 400-499 → Warning
  - < 400 → Information
- Includes TraceId for correlation

**Implementation (Lines 21-58)**:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    var stopwatch = Stopwatch.StartNew();
    var request = context.Request;

    // Log request
    _logger.LogInformation(
        "HTTP {Method} {Path} started. TraceId: {TraceId}",
        request.Method,
        request.Path,
        context.TraceIdentifier);

    try
    {
        await _next(context);
    }
    finally
    {
        stopwatch.Stop();
        var response = context.Response;

        // Determine log level based on status code
        var logLevel = response.StatusCode >= 500
            ? LogLevel.Error
            : response.StatusCode >= 400
                ? LogLevel.Warning
                : LogLevel.Information;

        _logger.Log(
            logLevel,
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms. TraceId: {TraceId}",
            request.Method,
            request.Path,
            response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            context.TraceIdentifier);
    }
}
```

---

## API Response Models

### ApiResponse<T> (Lines 1-81)

**Purpose**: Standardized wrapper for successful API responses

**Structure**:

```csharp
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

**Factory Methods**:

```csharp
// With data only
public static ApiResponse<T> Ok(T data) => new(data);

// With data and message
public static ApiResponse<T> Ok(T data, string message) => new(data, message);
```

**Usage Examples**:

```csharp
// Success response
return Ok(ApiResponse<SurveyDto>.Ok(survey));

// Success with message
return Ok(ApiResponse<SurveyDto>.Ok(survey, "Survey created successfully"));

// Created response
return CreatedAtAction(
    nameof(GetSurveyById),
    new { id = survey.Id },
    ApiResponse<SurveyDto>.Ok(survey, "Survey created successfully"));
```

**JSON Output**:

```json
{
  "success": true,
  "data": {
    "id": 1,
    "title": "Customer Satisfaction Survey",
    "description": "Help us improve our service"
  },
  "message": "Survey created successfully",
  "timestamp": "2025-11-10T10:30:00.000Z"
}
```

### ErrorResponse (Lines 1-54)

**Purpose**: Standardized error response model

**Structure**:

```csharp
public class ErrorResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = false;

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Details { get; set; }

    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? Errors { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }
}
```

**JSON Output (Validation Error)**:

```json
{
  "success": false,
  "statusCode": 400,
  "message": "Validation failed",
  "errors": {
    "Title": ["Title is required", "Title must be at least 3 characters"],
    "Description": ["Description cannot be empty"]
  },
  "timestamp": "2025-11-10T10:30:00.000Z",
  "traceId": "0HMVFE3QK1234"
}
```

---

## AutoMapper Configuration

### Mapping Profiles

**Location**: `Mapping/` directory

**Profiles**:
1. `SurveyMappingProfile` - Survey entity mappings
2. `QuestionMappingProfile` - Question entity mappings
3. `ResponseMappingProfile` - Response entity mappings
4. `AnswerMappingProfile` - Answer entity mappings
5. `UserMappingProfile` - User entity mappings
6. `StatisticsMappingProfile` - Statistics DTOs

### SurveyMappingProfile (Lines 1-53)

```csharp
public class SurveyMappingProfile : Profile
{
    public SurveyMappingProfile()
    {
        // Survey -> SurveyDto (Entity to DTO for reading)
        CreateMap<Survey, SurveyDto>()
            .ForMember(dest => dest.TotalResponses,
                opt => opt.MapFrom<SurveyTotalResponsesResolver>())
            .ForMember(dest => dest.CompletedResponses,
                opt => opt.MapFrom<SurveyCompletedResponsesResolver>());

        // Survey -> SurveyListDto (Entity to DTO for list view)
        CreateMap<Survey, SurveyListDto>()
            .ForMember(dest => dest.QuestionCount,
                opt => opt.MapFrom(src => src.Questions.Count))
            .ForMember(dest => dest.TotalResponses,
                opt => opt.MapFrom<SurveyListTotalResponsesResolver>())
            .ForMember(dest => dest.CompletedResponses,
                opt => opt.MapFrom<SurveyListCompletedResponsesResolver>());

        // CreateSurveyDto -> Survey (DTO to Entity for creation)
        CreateMap<CreateSurveyDto, Survey>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatorId, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.Creator, opt => opt.Ignore())
            .ForMember(dest => dest.Questions, opt => opt.Ignore())
            .ForMember(dest => dest.Responses, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // UpdateSurveyDto -> Survey (DTO to Entity for update)
        CreateMap<UpdateSurveyDto, Survey>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatorId, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore()) // Use ToggleSurveyStatusDto instead
            .ForMember(dest => dest.Creator, opt => opt.Ignore())
            .ForMember(dest => dest.Questions, opt => opt.Ignore())
            .ForMember(dest => dest.Responses, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }
}
```

**Key Points**:
- Ignores properties that shouldn't be mapped (IDs, audit fields, navigation properties)
- Uses custom value resolvers for calculated properties
- Separate mappings for detail vs list views

### Value Resolvers

**Purpose**: Handle complex property mappings that can't be done with simple lambda expressions

**Common Resolvers**:

1. **QuestionOptionsResolver**: Deserializes `OptionsJson` (string) to `Options` (List<string>)
2. **AnswerJsonResolver**: Parses answer JSON based on question type
3. **SurveyTotalResponsesResolver**: Counts total responses for survey
4. **SurveyCompletedResponsesResolver**: Counts completed responses

**Example - QuestionOptionsResolver**:

```csharp
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

**Usage**:

```csharp
CreateMap<Question, QuestionDto>()
    .ForMember(dest => dest.Options,
        opt => opt.MapFrom<QuestionOptionsResolver>());
```

### Configuration Validation

**AutoMapperConfigurationTest.cs**:

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

**Purpose**: Ensures all mappings are configured correctly at compile time

---

## Background Services

### IBackgroundTaskQueue (Lines 1-22)

**Purpose**: Interface for thread-safe background task queue

```csharp
public interface IBackgroundTaskQueue
{
    void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);
    Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}
```

### BackgroundTaskQueue (Lines 1-70)

**Purpose**: Thread-safe queue implementation using `System.Threading.Channels`

**Key Features**:
- Bounded channel with configurable capacity
- `BoundedChannelFullMode.Wait` - blocks when full
- High-performance async operations
- Logging for queue operations

**Implementation (Lines 9-69)**:

```csharp
public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, Task>> _queue;
    private readonly ILogger<BackgroundTaskQueue> _logger;

    public BackgroundTaskQueue(int capacity, ILogger<BackgroundTaskQueue> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create bounded channel with specified capacity
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        _queue = Channel.CreateBounded<Func<CancellationToken, Task>>(options);

        _logger.LogInformation("BackgroundTaskQueue initialized with capacity {Capacity}", capacity);
    }

    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        if (!_queue.Writer.TryWrite(workItem))
        {
            _logger.LogWarning("Failed to queue background work item - queue may be full");
        }
        else
        {
            _logger.LogDebug("Background work item queued successfully");
        }
    }

    public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        _logger.LogDebug("Background work item dequeued");

        return workItem;
    }
}
```

### QueuedHostedService (Lines 1-84)

**Purpose**: Background service that processes queued tasks

**Key Features**:
- Inherits from `BackgroundService`
- Continuous processing loop
- Error handling with retry delay
- Graceful shutdown support

**Implementation (Lines 27-72)**:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("QueuedHostedService is starting");

    await BackgroundProcessing(stoppingToken);
}

private async Task BackgroundProcessing(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            var workItem = await _taskQueue.DequeueAsync(stoppingToken);

            try
            {
                _logger.LogDebug("Executing background work item");

                await workItem(stoppingToken);

                _logger.LogDebug("Background work item completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing background work item");
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation - service is stopping
            _logger.LogInformation("QueuedHostedService is stopping");
            break;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in background processing loop");

            // Delay before retrying to avoid tight loop on persistent errors
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

### Registration (BackgroundServiceExtensions.cs)

```csharp
public static IServiceCollection AddBackgroundTaskQueue(
    this IServiceCollection services,
    int queueCapacity = 100)
{
    // Register the background task queue as singleton
    services.AddSingleton<IBackgroundTaskQueue>(serviceProvider =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<BackgroundTaskQueue>>();
        return new BackgroundTaskQueue(queueCapacity, logger);
    });

    // Register the hosted service that processes queued tasks
    services.AddHostedService<QueuedHostedService>();

    return services;
}
```

**Usage in BotController**:

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
    "SecretKey": "SurveyBot-Super-Secret-Key-For-JWT-Token-Generation-2025-Change-In-Production",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Clients",
    "TokenLifetimeHours": 24,
    "RefreshTokenLifetimeDays": 7
  }
}
```

**IMPORTANT**: Secret key must be at least 32 characters for HS256 algorithm

### Program.cs Configuration (Lines 56-108)

```csharp
// 1. Load JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

// 2. Validate settings
if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
    throw new InvalidOperationException("JWT settings are not properly configured in appsettings.json");
}

// 3. Configure authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
    };

    // Add logging for JWT authentication events
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Log.Debug("JWT token validated for user ID: {UserId}", userId);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Log.Warning("JWT authentication challenge: {Error}", context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

// 4. Add authorization
builder.Services.AddAuthorization();
```

### Using Authentication in Controllers

**Controller-Level**:

```csharp
[Authorize]  // All endpoints require authentication
public class SurveysController : ControllerBase
{
    // ...
}
```

**Endpoint-Level Override**:

```csharp
[HttpGet("code/{code}")]
[AllowAnonymous]  // Override controller-level [Authorize]
public async Task<ActionResult<ApiResponse<SurveyDto>>> GetSurveyByCode(string code)
{
    // Public endpoint
}
```

### Extracting User Information from JWT

```csharp
private int GetUserIdFromClaims()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
    {
        _logger.LogError("Failed to extract user ID from claims");
        throw new UnauthorizedAccessException("Invalid or missing user authentication");
    }

    return userId;
}
```

**Available Claims**:
- `ClaimTypes.NameIdentifier` - User ID (primary key)
- `"TelegramId"` - Telegram user ID
- `"Username"` - Telegram username
- `"FirstName"` - User's first name

### Testing with Swagger

1. Navigate to http://localhost:5000/swagger
2. Click "Authorize" button (top right)
3. Enter: `Bearer <your-jwt-token>`
4. Click "Authorize"
5. Test authenticated endpoints

---

## Swagger/OpenAPI Documentation

### Configuration (Program.cs Lines 143-190)

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SurveyBot API",
        Version = "v1",
        Description = "REST API for Telegram Survey Bot - Managing surveys, questions, and responses",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "SurveyBot API Team",
            Email = "support@surveybot.com"
        }
    });

    // Enable XML documentation comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Configure JWT Bearer Authentication for Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

### SwaggerOperation Attributes

**Usage**:

```csharp
[HttpPost]
[SwaggerOperation(
    Summary = "Create a new survey",
    Description = "Creates a new survey for the authenticated user. Survey is created as inactive by default.",
    Tags = new[] { "Surveys" }
)]
[ProducesResponseType(typeof(ApiResponse<SurveyDto>), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateSurvey([FromBody] CreateSurveyDto dto)
{
    // ...
}
```

**Key Attributes**:
- `[SwaggerOperation]` - Summary, description, tags
- `[ProducesResponseType]` - Document response types and status codes
- XML comments (///) - Additional documentation

### Accessing Swagger UI

**URL**: http://localhost:5000/swagger

**Features**:
- Interactive API documentation
- Try out endpoints
- View request/response schemas
- JWT authentication support
- Request duration display
- Deep linking
- Search/filter

---

## Health Checks

### Database Health Check

**Configuration (Program.cs Lines 134-139)**:

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SurveyBotDbContext>(
        name: "database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "postgresql" });
```

### Endpoints

**Basic Health Check**:
- **URL**: `/health/db`
- **Response**:
  - `Healthy` - Database is accessible
  - `Unhealthy` - Database is not accessible

**Detailed Health Check**:
- **URL**: `/health/db/details`
- **Response**:

```json
{
  "status": "healthy",
  "database": "connected",
  "timestamp": "2025-11-10T10:30:00.000Z"
}
```

**Additional Endpoints (HealthController)**:
- `/health` - Basic service health
- `/health/ready` - Readiness check
- `/health/live` - Liveness check

---

## Configuration Files

### appsettings.json (Lines 1-69)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=surveybot_db;User Id=surveybot_user;Password=surveybot_dev_password;"
  },
  "JwtSettings": {
    "SecretKey": "SurveyBot-Super-Secret-Key-For-JWT-Token-Generation-2025-Change-In-Production",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Clients",
    "TokenLifetimeHours": 24,
    "RefreshTokenLifetimeDays": 7
  },
  "BotConfiguration": {
    "BotToken": "",
    "BotUsername": "",
    "UseWebhook": false,
    "WebhookUrl": "",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "",
    "MaxConnections": 40,
    "ApiBaseUrl": "http://localhost:5000",
    "RequestTimeout": 30,
    "AdminUserIds": []
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information",
        "Microsoft.EntityFrameworkCore": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/surveybot-.log",
          "rollingInterval": "Day",
          "rollOnFileSizeLimit": true,
          "fileSizeLimitBytes": 10485760,
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ],
    "Properties": {
      "Application": "SurveyBot.API"
    }
  },
  "AllowedHosts": "*"
}
```

### appsettings.Development.json (Lines 1-46)

```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information",
        "Microsoft.EntityFrameworkCore": "Information",
        "System": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/surveybot-dev-.log",
          "rollingInterval": "Day",
          "rollOnFileSizeLimit": true,
          "fileSizeLimitBytes": 10485760,
          "retainedFileCountLimit": 7,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ]
  }
}
```

**Key Configuration Sections**:

1. **ConnectionStrings**: PostgreSQL connection string
2. **JwtSettings**: JWT token configuration
3. **BotConfiguration**: Telegram bot settings
4. **Serilog**: Structured logging configuration
   - Console sink with color theme
   - File sink with rolling daily logs
   - Seq sink for centralized logging

---

## DI Verification Utility

### VerifyDI.cs (Lines 1-106)

**Purpose**: Development utility to verify all services can be resolved from DI container

**Usage (Program.cs Lines 206-211)**:

```csharp
if (app.Environment.IsDevelopment())
{
    var (success, errors) = SurveyBot.API.DIVerifier.VerifyServiceResolution(app.Services);
    SurveyBot.API.DIVerifier.PrintVerificationResults(success, errors);
}
```

**Verified Services**:
- `SurveyBotDbContext`
- `ISurveyRepository`
- `IQuestionRepository`
- `IResponseRepository`
- `IUserRepository`
- `IAnswerRepository`

**Console Output**:

```
=== Dependency Injection Verification ===
✓ All services resolved successfully!

Registered Services:
  - SurveyBotDbContext (Scoped)
  - ISurveyRepository -> SurveyRepository (Scoped)
  - IQuestionRepository -> QuestionRepository (Scoped)
  - IResponseRepository -> ResponseRepository (Scoped)
  - IUserRepository -> UserRepository (Scoped)
  - IAnswerRepository -> AnswerRepository (Scoped)
==========================================
```

---

## Best Practices

### Controller Design

1. **Thin Controllers**: Delegate business logic to services
2. **Consistent Patterns**: Use same structure across all controllers
3. **Exception Handling**: Catch specific exceptions before general
4. **Logging**: Log important operations with context
5. **Model Validation**: Always check `ModelState.IsValid`
6. **Authorization**: Extract user ID from claims, verify ownership
7. **Response Wrapping**: Always return `ApiResponse<T>`

### API Design

1. **HTTP Verbs**:
   - GET - Retrieve resources
   - POST - Create resources
   - PUT - Update resources
   - DELETE - Delete resources

2. **Status Codes**:
   - 200 OK - Successful GET, PUT, DELETE
   - 201 Created - Successful POST
   - 204 No Content - Successful DELETE with no response
   - 400 Bad Request - Validation errors
   - 401 Unauthorized - Not authenticated
   - 403 Forbidden - Authenticated but not authorized
   - 404 Not Found - Resource doesn't exist
   - 409 Conflict - Duplicate/constraint violation
   - 500 Internal Server Error - Unhandled exception

3. **Route Naming**:
   - Use lowercase
   - Use hyphens for multi-word (RESTful convention)
   - Nest resources: `/api/surveys/{id}/questions`

### Security

1. **Authentication**: All endpoints require JWT unless explicitly `[AllowAnonymous]`
2. **HTTPS**: Use HTTPS in production (`RequireHttpsMetadata = true`)
3. **Input Validation**: Never trust client input
4. **Authorization**: Check user owns resource before allowing access
5. **Secrets**: Store sensitive config in environment variables or Azure Key Vault
6. **Error Messages**: Don't expose stack traces in production

### Logging

1. **Structured Logging**: Use Serilog with structured properties
2. **Log Levels**:
   - Trace: Very detailed (disabled in production)
   - Debug: Detailed (development only)
   - Information: General flow
   - Warning: Unexpected but not error
   - Error: Errors and exceptions
   - Critical: Critical failures

3. **Include Context**: TraceId, UserId, SurveyId, etc.
4. **Don't Log Sensitive Data**: Passwords, tokens, personal info

---

## Common Tasks

### Adding a New Endpoint

1. **Add method to controller**:

```csharp
[HttpPost("new-endpoint")]
[Authorize]
[SwaggerOperation(
    Summary = "Short description",
    Description = "Detailed description",
    Tags = new[] { "ControllerName" }
)]
[ProducesResponseType(typeof(ApiResponse<ResultDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<ApiResponse<ResultDto>>> NewEndpoint([FromBody] RequestDto dto)
{
    try
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid request data",
                Data = ModelState
            });
        }

        var userId = GetUserIdFromClaims();
        var result = await _service.PerformOperationAsync(userId, dto);

        return Ok(ApiResponse<ResultDto>.Ok(result, "Operation successful"));
    }
    catch (SpecificException ex)
    {
        _logger.LogWarning(ex, "Specific error occurred");
        return BadRequest(new ApiResponse<object>
        {
            Success = false,
            Message = ex.Message
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error performing operation");
        return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
        {
            Success = false,
            Message = "An error occurred"
        });
    }
}
```

2. **Test with Swagger**: http://localhost:5000/swagger

### Adding a New Controller

1. **Create controller class** in `Controllers/`
2. **Add attributes**:

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class NewController : ControllerBase
{
    private readonly IService _service;
    private readonly ILogger<NewController> _logger;

    public NewController(IService service, ILogger<NewController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // Endpoints...
}
```

3. **Add endpoints** following patterns above
4. **No registration needed** - controllers auto-discovered

### Debugging API Issues

1. **Check Swagger**: http://localhost:5000/swagger
2. **Review logs**: Console output or `logs/` directory
3. **Enable EF logging**:
   - Set `"Microsoft.EntityFrameworkCore.Database.Command": "Information"` in appsettings.Development.json
4. **Check middleware order**: Authentication before Authorization
5. **Verify DI registration**: Check DI verification output on startup
6. **Use breakpoints**: Set breakpoints in controller methods
7. **Check TraceId**: Use TraceId from error response to find logs

---

## Common Issues & Solutions

### Authentication Failures

**Issue**: 401 Unauthorized

**Solutions**:
1. Check JWT token is valid (not expired)
2. Verify token format: `Bearer <token>`
3. Check `Issuer` and `Audience` match configuration
4. Ensure `SecretKey` is at least 32 characters
5. Check `ClockSkew` (default is 5 minutes)

### CORS Issues

**Issue**: CORS errors in browser

**Solution**: Add CORS policy in Program.cs:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowAll");  // Before UseAuthentication
```

### Middleware Order Problems

**Issue**: 401/403 errors, authentication not working

**Solution**: Ensure correct order:

```csharp
app.UseGlobalExceptionHandler();  // First
app.UseHttpsRedirection();
app.UseAuthentication();          // Before Authorization
app.UseAuthorization();
app.MapControllers();
```

### Dependency Injection Issues

**Issue**: Service not resolved

**Solutions**:
1. Check service is registered in Program.cs
2. Verify service lifetime (Scoped/Singleton/Transient)
3. Check DI verification output on startup
4. Ensure interface and implementation match
5. Check for circular dependencies

### AutoMapper Configuration Errors

**Issue**: "Unmapped members" exception

**Solutions**:
1. Run `AutoMapperConfigurationTest`
2. Add `.ForMember(dest => dest.Property, opt => opt.Ignore())` for unmapped properties
3. Ensure all profiles are in correct assembly
4. Check value resolver implementations

---

## API Endpoints Reference

### Complete Endpoint List

| Method | Route | Auth | Controller | Description |
|--------|-------|------|------------|-------------|
| **Authentication** |
| POST | `/api/auth/login` | No | AuthController | Login, get JWT token |
| POST | `/api/auth/refresh` | No | AuthController | Refresh token (placeholder) |
| GET | `/api/auth/validate` | Yes | AuthController | Validate current token |
| POST | `/api/auth/register` | No | AuthController | Register/update user |
| GET | `/api/auth/me` | Yes | AuthController | Get current user |
| **Surveys** |
| POST | `/api/surveys` | Yes | SurveysController | Create survey |
| GET | `/api/surveys` | Yes | SurveysController | List user's surveys |
| GET | `/api/surveys/{id}` | Yes | SurveysController | Get survey details |
| PUT | `/api/surveys/{id}` | Yes | SurveysController | Update survey |
| DELETE | `/api/surveys/{id}` | Yes | SurveysController | Delete survey |
| POST | `/api/surveys/{id}/activate` | Yes | SurveysController | Activate survey |
| POST | `/api/surveys/{id}/deactivate` | Yes | SurveysController | Deactivate survey |
| GET | `/api/surveys/code/{code}` | No | SurveysController | Get survey by code |
| GET | `/api/surveys/{id}/statistics` | Yes | SurveysController | Get survey statistics |
| **Questions** |
| POST | `/api/surveys/{surveyId}/questions` | Yes | QuestionsController | Add question |
| GET | `/api/surveys/{surveyId}/questions` | Optional | QuestionsController | List questions |
| PUT | `/api/questions/{id}` | Yes | QuestionsController | Update question |
| DELETE | `/api/questions/{id}` | Yes | QuestionsController | Delete question |
| POST | `/api/surveys/{surveyId}/questions/reorder` | Yes | QuestionsController | Reorder questions |
| **Responses** |
| GET | `/api/surveys/{surveyId}/responses` | Yes | ResponsesController | List responses |
| GET | `/api/responses/{id}` | Yes | ResponsesController | Get response details |
| POST | `/api/surveys/{surveyId}/responses` | No | ResponsesController | Create response |
| POST | `/api/responses/{id}/answers` | No | ResponsesController | Save answer |
| POST | `/api/responses/{id}/complete` | No | ResponsesController | Complete response |
| **Bot** |
| POST | `/api/bot/webhook` | Secret | BotController | Telegram webhook |
| GET | `/api/bot/status` | No | BotController | Bot status |
| GET | `/api/bot/health` | No | BotController | Bot health |
| **Health** |
| GET | `/health` | No | HealthController | Basic health |
| GET | `/health/ready` | No | HealthController | Readiness check |
| GET | `/health/live` | No | HealthController | Liveness check |
| GET | `/health/db` | No | Program.cs | DB health check |
| GET | `/health/db/details` | No | Program.cs | DB health details |

---

## Key Files Reference

### Critical Files

1. **Program.cs** (308 lines)
   - Application entry point
   - Service configuration
   - Middleware pipeline
   - Lines 14-23: Early Serilog config
   - Lines 66-108: JWT authentication
   - Lines 143-190: Swagger config
   - Lines 216-292: Middleware pipeline

2. **SurveysController.cs** (623 lines)
   - Complete survey management
   - Lines 46-98: Create survey
   - Lines 113-165: List surveys
   - Lines 176-224: Get survey by ID
   - Lines 377-435: Activate survey
   - Lines 496-540: Get survey by code
   - Lines 551-599: Get statistics

3. **BotController.cs** (245 lines)
   - Telegram webhook
   - Lines 50-137: Webhook endpoint
   - Lines 88-116: Background task queuing
   - Lines 188-212: Secret validation

4. **GlobalExceptionMiddleware.cs** (163 lines)
   - Centralized error handling
   - Lines 51-151: Exception mapping

5. **appsettings.json** (69 lines)
   - Production configuration
   - Lines 2-4: Connection string
   - Lines 5-11: JWT settings
   - Lines 12-23: Bot configuration
   - Lines 24-66: Serilog configuration

---

## Summary

The **SurveyBot.API** project is a well-architected REST API presentation layer that:

1. **Exposes comprehensive REST endpoints** for survey management
2. **Implements JWT authentication** for secure access
3. **Uses Clean Architecture** with proper separation of concerns
4. **Provides Swagger documentation** for easy API exploration
5. **Handles errors globally** with standardized responses
6. **Logs extensively** using Serilog structured logging
7. **Supports background processing** for webhook updates
8. **Validates health** with built-in health checks

### Architecture Highlights

- **Thin Controllers**: Business logic in services, controllers handle HTTP
- **Dependency Injection**: All dependencies injected via constructor
- **Middleware Pipeline**: Proper order for authentication, authorization, error handling
- **AutoMapper**: Clean separation between entities and DTOs
- **Background Services**: Non-blocking webhook processing

### Development Workflow

1. Start application: `dotnet run`
2. Access Swagger: http://localhost:5000/swagger
3. Login to get JWT token
4. Use token to test authenticated endpoints
5. Check logs in console or `logs/` directory
6. Use health endpoints to verify application state

---

**Last Updated**: 2025-11-10
**Version**: 1.0.0-MVP
**Target Framework**: .NET 8.0
**Project**: SurveyBot.API (Presentation Layer)
