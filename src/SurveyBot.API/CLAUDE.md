# SurveyBot.API - REST API Presentation Layer

**Version**: 1.5.0 | **Framework**: .NET 8.0 | **ASP.NET Core** 8.0

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

### Controller Inventory

**Total Controllers**: 10

| Controller | Route | Purpose | Auth | Status |
|------------|-------|---------|------|--------|
| **AuthController** | `/api/auth` | JWT authentication, user registration | Mixed | Stable |
| **SurveysController** | `/api/surveys` | Survey CRUD, activation, statistics | Yes | Enhanced v1.4.0 |
| **QuestionsController** | `/api/surveys/{surveyId}/questions` | Question CRUD, reordering | Mixed | Stable |
| **QuestionFlowController** | `/api/surveys/{surveyId}/questions` | Flow configuration, validation | Yes | NEW v1.4.0 |
| **ResponsesController** | `/api/responses` | Response submission, navigation | Mixed | Enhanced v1.4.0 |
| **MediaController** | `/api/media` | Media upload, deletion | Yes | NEW v1.3.0 |
| **BotController** | `/api/bot` | Webhook, status | Secret | Stable |
| **UsersController** | `/api/users` | User management | Yes | Stable |
| **HealthController** | `/health` | Health checks | No | Stable |
| **TestErrorsController** | `/api/test/errors` | Error testing (dev only) | No | Development |

**Recent Changes (v1.4.0 - Conditional Question Flow)**:
- **QuestionFlowController**: NEW - Complete flow CRUD with cycle detection (685 lines)
- **SurveysController.ActivateSurvey**: Enhanced with flow validation
- **ResponsesController.GetNextQuestion**: NEW - Next question navigation endpoint
- **AutoMapper**: Enhanced for ConditionalFlowDto, NextQuestionDeterminant mappings

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
| `/{id}` | PUT | Yes | Update survey metadata |
| `/{id}/complete` | PUT | Yes | **Complete update (replace all questions)** - NEW |
| `/{id}` | DELETE | Yes | Delete survey |
| `/{id}/activate` | POST | Yes | Activate survey |
| `/{id}/deactivate` | POST | Yes | Deactivate survey |
| `/code/{code}` | GET | **No** | Get by code (public) |
| `/{id}/statistics` | GET | Yes | Get statistics |

**Authorization**: User must own survey (except `/code/{code}`)

**NEW Endpoint - Complete Survey Update**:
- **PUT `/{id}/complete`**: Completely replaces survey and all questions in atomic transaction
- **DESTRUCTIVE**: Deletes all existing questions, responses, and answers
- **Validation**: Performs cycle detection on question flow before saving
- **Request**: `UpdateSurveyWithQuestionsDto` with survey metadata and complete question list
- **Flow Reference**: Uses array index-based references (`null` = sequential, `-1` = end, `0+` = jump to index)
- **Error Responses**: 400 (validation), 401 (unauthorized), 403 (forbidden), 404 (not found), 409 (cycle detected)
- **Use Case**: Complete survey redesign, frontend admin panel bulk updates

**NEW in v1.4.0 - Conditional Flow**:
- **ActivateSurvey** now validates survey flow before activation
- Returns 400 with cycle details if survey has cycle in question flow
- Error response includes `cyclePath` array showing the cycle
- Example: `{ error: "Invalid survey flow", cyclePath: [1, 2, 3, 1] }`

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

### QuestionFlowController (NEW in v1.4.0)

**File**: `Controllers/QuestionFlowController.cs` (685 lines)
**Route**: `/api/surveys/{surveyId}/questions`

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/{questionId}/flow` | GET | Yes | Get question flow configuration |
| `/{questionId}/flow` | PUT | Yes | Update question flow configuration |
| `/validate` | POST | Yes | Validate survey flow (cycle detection) |

**Features** (NEW in v1.4.0 - Conditional Question Flow):
- Get/update question flow for branching and non-branching questions
- Cycle detection prevents invalid survey structures
- Returns ConditionalFlowDto with flow configuration details
- Supports both SingleChoice/Rating (branching) and Text/MultipleChoice (non-branching)
- **Comprehensive diagnostic logging** with emoji markers (Development only)

**Key Endpoints**:

**GET /{surveyId}/questions/{questionId}/flow**
```
Returns ConditionalFlowDto with:
- SupportsBranching: true/false based on question type
- DefaultNextQuestionId: for non-branching questions
- OptionFlows: array of options with their next questions
```

**PUT /{surveyId}/questions/{questionId}/flow**
```
Request: UpdateQuestionFlowDto
- For branching: OptionNextQuestions (dict of option_id → next_question_id)
- For non-branching: DefaultNextQuestionId

Returns: Updated ConditionalFlowDto or 400 if cycle detected
```

**POST /{surveyId}/questions/validate**
```
Validates entire survey for cycles
Returns: { valid: true/false, cyclePath: [1,2,3,1], errors: [...] }
```

**Diagnostic Logging Pattern** (Development Mode Only):

The QuestionFlowController implements comprehensive emoji-based logging for easy log scanning:

```csharp
// Entry point logging
_logger.LogInformation("🔧 [QuestionFlow] UpdateQuestionFlow called: SurveyId={SurveyId}, QuestionId={QuestionId}", surveyId, questionId);

// Authorization logging
_logger.LogInformation("🔑 [QuestionFlow] User {UserId} requesting flow update", userId);

// Ownership validation
_logger.LogInformation("✅ [QuestionFlow] Ownership validated: User {UserId} owns Survey {SurveyId}", userId, surveyId);

// Validation errors
_logger.LogWarning("⚠️ [QuestionFlow] Validation failed: {ErrorMessage}", errorMessage);

// Success logging
_logger.LogInformation("✅ [QuestionFlow] Flow updated successfully for Question {QuestionId}", questionId);

// Exception logging
_logger.LogError(ex, "❌ [QuestionFlow] Failed to update flow: {ErrorMessage}", ex.Message);
```

**Emoji Legend**:
- 🔧 = Entry point / operation start
- 🔑 = Authentication / authorization
- ✅ = Success / validation passed
- ⚠️ = Warning / validation failed
- ❌ = Error / exception
- 🔍 = Data lookup / inspection
- 📊 = Validation result

**Log Scanning Examples**:

```bash
# Find all flow update operations
grep "🔧 \[QuestionFlow\]" logs/api.log

# Find validation failures
grep "⚠️ \[QuestionFlow\]" logs/api.log

# Find authorization issues
grep "🔑 \[QuestionFlow\]" logs/api.log

# Find errors
grep "❌ \[QuestionFlow\]" logs/api.log
```

**Integration with SurveyValidationService**:

The controller delegates cycle detection to `ISurveyValidationService`:

```csharp
var cycleDetectionResult = await _validationService.DetectCycleAsync(surveyId);
if (cycleDetectionResult.HasCycle)
{
    return BadRequest(ApiResponse<object>.Error(
        $"Survey contains a cycle: {cycleDetectionResult.CycleDescription}",
        new { cyclePath = cycleDetectionResult.CyclePath }
    ));
}
```

### ResponsesController

**Routes**: `/api/surveys/{surveyId}/responses`, `/api/responses/{id}`

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/surveys/{surveyId}/responses` | POST | **No** | Start response |
| `/responses/{id}/answers` | POST | **No** | Save answer |
| `/responses/{id}/complete` | POST | **No** | Complete response |
| `/responses/{id}/next-question` | GET | **No** | Get next question (NEW v1.4.0) |
| `/surveys/{surveyId}/responses` | GET | Yes | List responses |
| `/responses/{id}` | GET | Yes | Get response details |

**Key**: Response submission is PUBLIC (for bot integration), viewing requires auth

**NEW in v1.4.0 - Conditional Flow**:

**GET /responses/{id}/next-question**
```
Returns next question for respondent based on flow configuration
- 200 OK: QuestionDto with next question
- 204 No Content: Survey complete (empty body)
- 404 Not Found: Response not found
- Public endpoint (no auth required) for bot/frontend integration
```

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

**Location Answer Format** (NEW v1.5.0):
```json
{
  "latitude": 40.7128,
  "longitude": -74.0060
}
```
- Validation: Latitude -90 to 90, Longitude -180 to 180
- Privacy-preserving logging (coordinate ranges, not exact values)

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

## Conditional Flow DTOs (NEW in v1.4.0)

### NextQuestionDeterminantDto

**Location**: `Core/DTOs/NextQuestionDeterminantDto.cs`

**Purpose**: Data transfer object for NextQuestionDeterminant value object. Represents the next step decision after answering a question.

```csharp
public class NextQuestionDeterminantDto
{
    public NextStepType Type { get; set; }           // GoToQuestion or EndSurvey
    public int? NextQuestionId { get; set; }         // Only when Type = GoToQuestion
}
```

**Factory Methods**:

```csharp
// Navigate to question 5
var next = NextQuestionDeterminantDto.ToQuestion(5);

// End the survey
var end = NextQuestionDeterminantDto.End();
```

**Breaking Change from v1.3.0**:
- **Old**: `int? nextQuestionId` where 0 meant "end survey" (magic value)
- **New**: `NextQuestionDeterminantDto` with explicit `Type` enum
- **Reason**: Eliminates magic values, type system prevents invalid states
- **Migration**: Use factory methods or explicit constructor with validation

**JSON Examples**:

GoToQuestion:
```json
{
  "type": "GoToQuestion",
  "nextQuestionId": 5
}
```

EndSurvey:
```json
{
  "type": "EndSurvey",
  "nextQuestionId": null
}
```

### ConditionalFlowDto

**Purpose**: Represents the complete flow configuration for a single question.

```csharp
public class ConditionalFlowDto
{
    public int QuestionId { get; set; }
    public bool SupportsBranching { get; set; }
    public NextQuestionDeterminantDto? DefaultNextDeterminant { get; set; }  // For non-branching
    public List<OptionFlowDto> OptionFlows { get; set; } = new();
}
```

**Usage**:
- GET `/api/surveys/{surveyId}/questions/{questionId}/flow` returns this structure
- Shows current flow configuration for a question
- For branching questions: includes individual flows per option
- For non-branching: only DefaultNextDeterminant applies to all answers

### OptionFlowDto

**Purpose**: Represents flow configuration for a single option in a choice question.

```csharp
public class OptionFlowDto
{
    public int OptionId { get; set; }
    public string OptionText { get; set; }
    public NextQuestionDeterminantDto NextDeterminant { get; set; }  // Use value object, not magic int
}
```

**Example**:
```json
{
  "optionId": 1,
  "optionText": "Option A",
  "nextDeterminant": {
    "type": "GoToQuestion",
    "nextQuestionId": 5
  }
}
```

### UpdateQuestionFlowDto

**Purpose**: Request body for updating question flow configuration.

```csharp
public class UpdateQuestionFlowDto
{
    public NextQuestionDeterminantDto? DefaultNextDeterminant { get; set; }  // For non-branching
    public Dictionary<int, NextQuestionDeterminantDto> OptionNextDeterminants { get; set; } = new();  // For branching
}
```

**Example Request - Non-branching**:
```json
{
  "defaultNextDeterminant": {
    "type": "GoToQuestion",
    "nextQuestionId": 3
  },
  "optionNextDeterminants": {}
}
```

**Example Request - Branching**:
```json
{
  "defaultNextDeterminant": null,
  "optionNextDeterminants": {
    "1": { "type": "GoToQuestion", "nextQuestionId": 5 },
    "2": { "type": "EndSurvey", "nextQuestionId": null },
    "3": { "type": "GoToQuestion", "nextQuestionId": 7 }
  }
}
```

---

## AutoMapper Configuration

**Profiles Location**: `Mapping/` directory

**Key Profiles**:
- `SurveyMappingProfile` - Survey mappings
- `QuestionMappingProfile` - Question mappings (enhanced v1.4.0)
- `ResponseMappingProfile` - Response mappings
- `AnswerMappingProfile` - Answer mappings (simplified v1.5.0 with AnswerValue pattern matching)
- `UserMappingProfile` - User mappings

**Value Resolvers** (legacy, mostly deprecated in v1.5.0):
- `QuestionOptionsResolver` - Deserialize JSON to List<string>
- `AnswerJsonResolver` - DEPRECATED: Parse answer JSON by type (replaced by AnswerValue pattern matching)
- `SurveyTotalResponsesResolver` - Count total responses
- `SurveyCompletedResponsesResolver` - Count completed
- **NEW v1.5.0 Location Resolvers**:
  - `AnswerLocationLatitudeResolver` - Extract latitude from LocationAnswerValue
  - `AnswerLocationLongitudeResolver` - Extract longitude from LocationAnswerValue
  - `AnswerLocationAccuracyResolver` - Extract accuracy from LocationAnswerValue
  - `AnswerLocationTimestampResolver` - Extract timestamp from LocationAnswerValue

### Enhanced Value Object Mappings (v1.4.0)

**NextQuestionDeterminant Value Object Mapping**:

AutoMapper now handles the complex mapping between `int?` fields in entities and `NextQuestionDeterminantDto` value objects:

```csharp
// QuestionMappingProfile.cs
CreateMap<Question, ConditionalFlowDto>()
    .ForMember(dest => dest.QuestionId, opt => opt.MapFrom(src => src.Id))
    .ForMember(dest => dest.SupportsBranching, opt => opt.MapFrom(src => src.SupportsBranching))
    .ForMember(dest => dest.DefaultNextDeterminant, opt => opt.MapFrom(src =>
        src.DefaultNextQuestionId.HasValue
            ? (src.DefaultNextQuestionId.Value == 0
                ? NextQuestionDeterminantDto.End()
                : NextQuestionDeterminantDto.ToQuestion(src.DefaultNextQuestionId.Value))
            : null))
    .ForMember(dest => dest.OptionFlows, opt => opt.MapFrom(src =>
        src.Options.Select(o => new OptionFlowDto
        {
            OptionId = o.Id,
            OptionText = o.Text,
            NextDeterminant = o.NextQuestionId.HasValue
                ? (o.NextQuestionId.Value == 0
                    ? NextQuestionDeterminantDto.End()
                    : NextQuestionDeterminantDto.ToQuestion(o.NextQuestionId.Value))
                : NextQuestionDeterminantDto.End()
        })));
```

**Key Mapping Patterns**:

1. **Factory Method Pattern**: Uses `NextQuestionDeterminantDto.ToQuestion(id)` and `NextQuestionDeterminantDto.End()` instead of direct construction
2. **Magic Value Conversion**: Converts `0` → `EndSurvey`, `1..N` → `GoToQuestion`
3. **Null Handling**: `null` in entity → `null` in DTO (sequential flow)

**ConditionalFlowDto Complex Nested Mapping**:

```csharp
CreateMap<Question, ConditionalFlowDto>()
    .ForMember(dest => dest.OptionFlows, opt => opt.MapFrom(src =>
        src.Options
            .OrderBy(o => o.OrderIndex)
            .Select(o => new OptionFlowDto
            {
                OptionId = o.Id,
                OptionText = o.Text,
                NextDeterminant = MapToNextDeterminant(o.NextQuestionId)
            })
            .ToList()));

// Helper method for consistent mapping
private static NextQuestionDeterminantDto? MapToNextDeterminant(int? nextQuestionId)
{
    if (!nextQuestionId.HasValue) return null;
    return nextQuestionId.Value == SurveyConstants.EndOfSurveyMarker
        ? NextQuestionDeterminantDto.End()
        : NextQuestionDeterminantDto.ToQuestion(nextQuestionId.Value);
}
```

**ConstructUsing Pattern for Private Constructors**:

If value objects have private constructors (recommended for DDD), use `ConstructUsing`:

```csharp
CreateMap<UpdateQuestionFlowDto, Question>()
    .ForMember(dest => dest.DefaultNextQuestionId, opt => opt.MapFrom(src =>
        src.DefaultNextDeterminant == null
            ? (int?)null
            : (src.DefaultNextDeterminant.Type == NextStepType.EndSurvey
                ? 0
                : src.DefaultNextDeterminant.NextQuestionId)));
```

**Reverse Mapping (DTO → Entity)**:

```csharp
// When updating from API request
CreateMap<UpdateQuestionFlowDto, Question>()
    .ForMember(dest => dest.DefaultNextQuestionId, opt => opt.MapFrom(src =>
        ConvertDeterminantToInt(src.DefaultNextDeterminant)));

private static int? ConvertDeterminantToInt(NextQuestionDeterminantDto? determinant)
{
    if (determinant == null) return null;
    return determinant.Type switch
    {
        NextStepType.EndSurvey => 0,
        NextStepType.GoToQuestion => determinant.NextQuestionId,
        _ => throw new InvalidOperationException($"Unknown NextStepType: {determinant.Type}")
    };
}
```

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

**Testing Value Object Mappings**:

```csharp
[Fact]
public void Should_Map_EndOfSurvey_To_NextDeterminant()
{
    // Arrange
    var question = new Question { DefaultNextQuestionId = 0 };

    // Act
    var dto = _mapper.Map<ConditionalFlowDto>(question);

    // Assert
    Assert.NotNull(dto.DefaultNextDeterminant);
    Assert.Equal(NextStepType.EndSurvey, dto.DefaultNextDeterminant.Type);
    Assert.Null(dto.DefaultNextDeterminant.NextQuestionId);
}

[Fact]
public void Should_Map_QuestionId_To_NextDeterminant()
{
    // Arrange
    var question = new Question { DefaultNextQuestionId = 5 };

    // Act
    var dto = _mapper.Map<ConditionalFlowDto>(question);

    // Assert
    Assert.NotNull(dto.DefaultNextDeterminant);
    Assert.Equal(NextStepType.GoToQuestion, dto.DefaultNextDeterminant.Type);
    Assert.Equal(5, dto.DefaultNextDeterminant.NextQuestionId);
}
```

### Answer Value Object Mappings (v1.5.0)

**Simplified AnswerMappingProfile with Pattern Matching**:

The ARCH-003 migration eliminates complex JSON parsing logic in AutoMapper by using pattern matching on the `answer.Value` property. This provides type safety and clarity.

**Before (v1.4.x)** - Complex JSON parsing with separate resolvers:
```csharp
// OLD: Required custom value resolvers for each answer type
CreateMap<Answer, AnswerDto>()
    .ForMember(dest => dest.AnswerText, opt => opt.MapFrom<AnswerTextResolver>())
    .ForMember(dest => dest.SelectedOptions, opt => opt.MapFrom<AnswerSelectedOptionsResolver>())
    .ForMember(dest => dest.RatingValue, opt => opt.MapFrom<AnswerRatingResolver>());

// Each resolver had to parse JSON manually
public class AnswerTextResolver : IValueResolver<Answer, AnswerDto, string?>
{
    public string? Resolve(Answer source, AnswerDto destination, string? destMember, ResolutionContext context)
    {
        if (source.QuestionType == QuestionType.Text)
            return source.AnswerText;
        // Complex JSON parsing logic...
    }
}
```

**After (v1.5.0)** - Simple pattern matching:
```csharp
// NEW: Clean AfterMap with pattern matching on Value property
CreateMap<Answer, AnswerDto>()
    .ForMember(dest => dest.AnswerText, opt => opt.Ignore())
    .ForMember(dest => dest.SelectedOptions, opt => opt.Ignore())
    .ForMember(dest => dest.RatingValue, opt => opt.Ignore())
    .ForMember(dest => dest.Latitude, opt => opt.Ignore())
    .ForMember(dest => dest.Longitude, opt => opt.Ignore())
    .AfterMap((src, dest) =>
    {
        // Type-safe pattern matching - no JSON parsing!
        switch (src.Value)
        {
            case TextAnswerValue textValue:
                dest.AnswerText = textValue.Text;
                break;

            case SingleChoiceAnswerValue singleChoice:
                dest.SelectedOptions = new List<string> { singleChoice.SelectedOption };
                break;

            case MultipleChoiceAnswerValue multipleChoice:
                dest.SelectedOptions = multipleChoice.SelectedOptions.ToList();
                break;

            case RatingAnswerValue ratingValue:
                dest.RatingValue = ratingValue.Rating;
                break;

            case LocationAnswerValue locationValue:
                dest.Latitude = locationValue.Latitude;
                dest.Longitude = locationValue.Longitude;
                dest.LocationAccuracy = locationValue.Accuracy;
                dest.LocationTimestamp = locationValue.Timestamp;
                break;

            case null:
                // Legacy fallback for backward compatibility
                var legacyValue = AnswerValueFactory.ConvertFromLegacy(
                    src.AnswerText, src.AnswerJson, src.Question.QuestionType);
                // ... handle legacy data
                break;
        }
    });
```

**Benefits of New Pattern**:
1. **No JSON Parsing**: Value objects are already deserialized by EF Core
2. **Type Safety**: Compiler catches missing cases
3. **Readability**: Clear intent, single AfterMap block
4. **Backward Compatible**: Fallback to legacy AnswerText/AnswerJson when Value is null
5. **Easier Testing**: Mock AnswerValue, not JSON strings

**Location Answer Mapping Example**:
```csharp
// Mapping a location answer with full metadata
var answer = new Answer
{
    Id = 1,
    QuestionId = 5,
    Value = LocationAnswerValue.Create(
        latitude: 40.7128,
        longitude: -74.0060,
        accuracy: 10.5,
        timestamp: DateTime.UtcNow)
};

var dto = _mapper.Map<AnswerDto>(answer);
// dto.Latitude = 40.7128
// dto.Longitude = -74.0060
// dto.LocationAccuracy = 10.5
// dto.LocationTimestamp = [timestamp]
```

**Deprecated Resolvers** (removed in v1.5.0):
- `AnswerJsonResolver` - No longer needed, replaced by pattern matching
- Custom answer type resolvers - All consolidated into single AfterMap

**Remaining Resolvers**:
- Location resolvers still used as value resolvers for extracting specific properties from LocationAnswerValue in scenarios where AfterMap isn't suitable

**Testing Answer Mappings**:
```csharp
[Fact]
public void Should_Map_TextAnswerValue_To_AnswerDto()
{
    // Arrange
    var answer = Answer.CreateWithValue(
        responseId: 1,
        questionId: 1,
        value: TextAnswerValue.Create("Sample answer"));

    // Act
    var dto = _mapper.Map<AnswerDto>(answer);

    // Assert
    Assert.Equal("Sample answer", dto.AnswerText);
    Assert.Null(dto.SelectedOptions);
    Assert.Null(dto.RatingValue);
}

[Fact]
public void Should_Map_LocationAnswerValue_To_AnswerDto()
{
    // Arrange
    var answer = Answer.CreateWithValue(
        responseId: 1,
        questionId: 1,
        value: LocationAnswerValue.Create(40.7128, -74.0060, 10.5));

    // Act
    var dto = _mapper.Map<AnswerDto>(answer);

    // Assert
    Assert.Equal(40.7128, dto.Latitude);
    Assert.Equal(-74.0060, dto.Longitude);
    Assert.Equal(10.5, dto.LocationAccuracy);
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

### JWT Claims Structure

**Claims in Token** (set by AuthService):

```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),           // User ID (database)
    new Claim("TelegramId", user.TelegramId.ToString()),                // Telegram ID
    new Claim(ClaimTypes.Name, user.Username ?? user.TelegramId.ToString()),
    new Claim(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
    new Claim(ClaimTypes.Surname, user.LastName ?? string.Empty),
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())   // Unique token ID
};
```

**Claims Usage**:
- **ClaimTypes.NameIdentifier**: Primary user ID (int) - used for ownership checks
- **"TelegramId"**: Telegram user ID (long) - used for bot integration
- **ClaimTypes.Name**: Display name for UI
- **JwtRegisteredClaimNames.Jti**: Token revocation tracking

### Using in Controllers

**Controller-level Authentication**:
```csharp
[Authorize]  // All endpoints require auth
public class SurveysController : ControllerBase { }
```

**Endpoint-level override**:
```csharp
[HttpGet("code/{code}")]
[AllowAnonymous]  // Public endpoint (survey taking)
public async Task<ActionResult> GetSurveyByCode(string code) { }
```

**Mixed Authorization Pattern** (common for survey responses):
```csharp
// Some endpoints public (survey taking), others protected (viewing results)
public class ResponsesController : ControllerBase
{
    [HttpPost("/surveys/{surveyId}/responses")]
    [AllowAnonymous]  // PUBLIC - anyone can start survey
    public async Task<ActionResult> StartResponse(int surveyId) { }

    [HttpGet("/surveys/{surveyId}/responses")]
    [Authorize]  // PROTECTED - only survey owner can view
    public async Task<ActionResult> GetResponses(int surveyId) { }
}
```

### GetUserIdFromClaims Helper Pattern

**Standard Implementation** (used across all controllers):

```csharp
private int GetUserIdFromClaims()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
    {
        _logger.LogWarning("Invalid or missing user ID claim");
        throw new UnauthorizedAccessException("Invalid authentication token");
    }

    return userId;
}
```

**Usage in Endpoints**:

```csharp
[HttpPost]
[Authorize]
public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateSurvey([FromBody] CreateSurveyDto dto)
{
    var userId = GetUserIdFromClaims();  // Extract once, use throughout
    var survey = await _surveyService.CreateSurveyAsync(userId, dto);
    return CreatedAtAction(nameof(GetSurvey), new { id = survey.Id },
        ApiResponse<SurveyDto>.Ok(survey, "Survey created successfully"));
}
```

**Benefits**:
- **Centralized extraction** - DRY principle
- **Consistent error handling** - Throws if claim invalid/missing
- **Type safety** - Returns int, not string
- **Logging** - Warning logged if claim invalid

### Ownership Verification Pattern

**Service-Level Ownership Check** (recommended approach):

```csharp
[HttpPut("{id}")]
[Authorize]
public async Task<ActionResult<ApiResponse<SurveyDto>>> UpdateSurvey(int id, [FromBody] UpdateSurveyDto dto)
{
    try
    {
        var userId = GetUserIdFromClaims();

        // Service throws ForbiddenException if user doesn't own survey
        var survey = await _surveyService.UpdateSurveyAsync(userId, id, dto);

        return Ok(ApiResponse<SurveyDto>.Ok(survey, "Survey updated successfully"));
    }
    catch (ForbiddenException ex)
    {
        return StatusCode(403, ApiResponse<object>.Error(ex.Message));
    }
}
```

**Service Implementation** (SurveyService.UpdateSurveyAsync):

```csharp
public async Task<SurveyDto> UpdateSurveyAsync(int userId, int surveyId, UpdateSurveyDto dto)
{
    var survey = await _surveyRepository.GetByIdAsync(surveyId);

    if (survey == null)
        throw new SurveyNotFoundException(surveyId);

    // OWNERSHIP VERIFICATION
    if (survey.CreatedByUserId != userId)
    {
        _logger.LogWarning(
            "User {UserId} attempted to update survey {SurveyId} owned by {OwnerId}",
            userId, surveyId, survey.CreatedByUserId);

        throw new ForbiddenException($"User does not have permission to update survey {surveyId}");
    }

    // ... update logic
}
```

**Alternative: Controller-Level Check** (for simple cases):

```csharp
[HttpDelete("{id}")]
[Authorize]
public async Task<ActionResult<ApiResponse<object>>> DeleteSurvey(int id)
{
    var userId = GetUserIdFromClaims();
    var survey = await _surveyService.GetSurveyByIdAsync(id);

    if (survey == null)
        return NotFound(ApiResponse<object>.Error("Survey not found"));

    // Check ownership at controller level
    if (survey.CreatedByUserId != userId)
    {
        _logger.LogWarning("User {UserId} attempted to delete survey {SurveyId} they don't own", userId, id);
        return StatusCode(403, ApiResponse<object>.Error("You do not have permission to delete this survey"));
    }

    await _surveyService.DeleteSurveyAsync(id);
    return Ok(ApiResponse<object>.Ok(null, "Survey deleted successfully"));
}
```

**Recommendation**: Prefer **service-level** ownership checks for:
- Consistency across API and Bot layers
- Centralized business rules
- Easier testing
- DRY principle

### Authorization Flow Example

**Complete Request Flow with Authorization**:

```
1. Client Request
   POST /api/surveys/5/questions
   Authorization: Bearer eyJhbGc...
   Body: { questionText: "What's your age?", ... }

2. ASP.NET Core Middleware
   - UseAuthentication(): Validates JWT signature, expiry
   - Populates User.Claims from token
   - UseAuthorization(): Checks [Authorize] attribute

3. Controller Method Entry
   [HttpPost]
   [Authorize]
   public async Task<ActionResult> CreateQuestion(int surveyId, CreateQuestionDto dto)
   {
       var userId = GetUserIdFromClaims();  // Extract: 123

4. Ownership Verification
       var survey = await _surveyService.GetSurveyByIdAsync(surveyId);
       if (survey.CreatedByUserId != userId)
           return Forbid();  // 403 Forbidden

5. Business Logic Execution
       var question = await _questionService.AddQuestionAsync(surveyId, dto);
       return Ok(question);
   }
```

### Common Authorization Scenarios

**Scenario 1: Survey Owner Only**
```csharp
// All survey management endpoints require ownership
// Pattern: Check CreatedByUserId == User.FindFirst(ClaimTypes.NameIdentifier)
```

**Scenario 2: Public Survey Access**
```csharp
// Survey taking is anonymous (no auth required)
[AllowAnonymous]
[HttpGet("/surveys/code/{code}")]
public async Task<ActionResult> GetSurveyByCode(string code)
{
    var survey = await _surveyService.GetSurveyByCodeAsync(code);
    if (!survey.IsActive)
        return BadRequest("Survey is not active");
    return Ok(survey);
}
```

**Scenario 3: Conditional Authorization**
```csharp
// If authenticated, filter by user; if not, show public surveys
[HttpGet]
public async Task<ActionResult> GetSurveys()
{
    if (User.Identity?.IsAuthenticated == true)
    {
        var userId = GetUserIdFromClaims();
        var surveys = await _surveyService.GetSurveysByUserAsync(userId);
        return Ok(surveys);
    }
    else
    {
        var publicSurveys = await _surveyService.GetActiveSurveysAsync();
        return Ok(publicSurveys);
    }
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

## Data Flow Analysis (v1.4.0 Conditional Flow)

### Survey Creation → Activation Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Frontend: User creates survey with conditional flow         │
│    POST /api/surveys                                            │
│    Body: CreateSurveyDto { title, description }                │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. Add Questions with Flow Configuration                       │
│    POST /api/surveys/{surveyId}/questions                      │
│    Body: CreateQuestionDto {                                    │
│      questionText: "Do you like pizza?",                       │
│      questionType: "SingleChoice",                             │
│      options: ["Yes", "No"],                                   │
│      optionNextQuestions: { 0: 5, 1: 0 }  // Index→NextQID    │
│    }                                                            │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. QuestionService: Creates Question + QuestionOptions         │
│    - Maps option index to QuestionOption entity                │
│    - Option 0 → QuestionOption { Id: 10, NextQuestionId: 5 }   │
│    - Option 1 → QuestionOption { Id: 11, NextQuestionId: 0 }   │
│    - Stores in database (no FK validation on NextQuestionId)   │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. Attempt Survey Activation                                   │
│    POST /api/surveys/{surveyId}/activate                       │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. SurveysController.ActivateSurvey                            │
│    - Calls ISurveyValidationService.ValidateSurveyStructureAsync│
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 6. SurveyValidationService: Cycle Detection                    │
│    - Loads all questions with flow configuration               │
│    - Builds directed graph: Q1 → Q5, Q5 → Q3, Q3 → Q1 (cycle!) │
│    - Runs DFS to detect cycles                                 │
│    - Result: { HasCycle: true, CyclePath: [1, 5, 3, 1] }       │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 7. Response: 400 Bad Request                                   │
│    {                                                            │
│      "success": false,                                          │
│      "error": "Invalid survey flow",                           │
│      "cyclePath": [1, 5, 3, 1],                                 │
│      "message": "Cycle: Q1 → Q5 → Q3 → Q1"                     │
│    }                                                            │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 8. User fixes cycle via flow configuration                     │
│    PUT /api/surveys/{surveyId}/questions/{questionId}/flow     │
│    Body: UpdateQuestionFlowDto {                                │
│      optionNextQuestions: { 10: 7, 11: 0 }  // OptionId→NextQ  │
│    }                                                            │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 9. Retry Activation: POST /api/surveys/{surveyId}/activate    │
│    - Validation passes (no cycles)                             │
│    - Survey.IsActive = true                                    │
│    - Survey.SurveyCode generated (6-char Base36)               │
│    - Response: 200 OK with SurveyDto                           │
└─────────────────────────────────────────────────────────────────┘
```

### Response Submission with Conditional Navigation

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User starts survey via Bot or Frontend                      │
│    POST /api/surveys/{surveyId}/responses                      │
│    Body: { userIdentifier: "telegram_123456" }                 │
│    Response: { responseId: 101, firstQuestionId: 1 }           │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. User answers Question 1 (SingleChoice: "Do you like pizza?")│
│    POST /api/responses/101/answers                             │
│    Body: {                                                      │
│      questionId: 1,                                            │
│      answerJson: "{\"selectedOption\": \"Yes\"}"               │
│    }                                                            │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. ResponseService.SaveAnswerAsync                             │
│    - Loads Question 1 with Options                             │
│    - Question is branching (SingleChoice)                      │
│    - Parses selectedOption: "Yes"                              │
│    - Finds matching QuestionOption: { Id: 10, Text: "Yes",    │
│                                        NextQuestionId: 5 }      │
│    - Creates Answer: { NextQuestionId: 5 }                     │
│    - Updates Response.VisitedQuestionIds: [1]                  │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. Get Next Question                                            │
│    GET /api/responses/101/next-question                        │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. ResponsesController.GetNextQuestion                         │
│    - Loads Response 101                                        │
│    - Finds last Answer: { NextQuestionId: 5 }                  │
│    - NextQuestionId != 0 (not end marker)                      │
│    - Loads Question 5                                          │
│    - Maps to QuestionDto                                       │
│    - Response: 200 OK with QuestionDto                         │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 6. User answers Question 5 (Text: "Why do you like pizza?")   │
│    POST /api/responses/101/answers                             │
│    Body: {                                                      │
│      questionId: 5,                                            │
│      answerJson: "{\"answerText\": \"It's delicious!\"}"       │
│    }                                                            │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 7. ResponseService.SaveAnswerAsync                             │
│    - Question 5 is non-branching (Text)                        │
│    - Uses Question.DefaultNextQuestionId: 0                    │
│    - Creates Answer: { NextQuestionId: 0 }                     │
│    - Updates Response.VisitedQuestionIds: [1, 5]               │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 8. Get Next Question                                            │
│    GET /api/responses/101/next-question                        │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 9. ResponsesController.GetNextQuestion                         │
│    - Loads last Answer: { NextQuestionId: 0 }                  │
│    - Detects SurveyConstants.IsEndOfSurvey(0) → true           │
│    - Marks Response.IsCompleted = true                         │
│    - Response: 204 No Content (survey complete)                │
└─────────────────────────────────────────────────────────────────┘
```

### Question Flow Update with Cycle Detection

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User updates flow configuration                             │
│    PUT /api/surveys/1/questions/5/flow                         │
│    Body: UpdateQuestionFlowDto {                                │
│      defaultNextDeterminant: null,                             │
│      optionNextDeterminants: {                                 │
│        10: { type: "GoToQuestion", nextQuestionId: 1 }  // Cycle!│
│      }                                                          │
│    }                                                            │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. QuestionFlowController.UpdateQuestionFlow                   │
│    - Validates ownership (GetUserIdFromClaims)                 │
│    - Calls QuestionService.UpdateQuestionFlowAsync             │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. QuestionService: Update Flow                                │
│    - Converts NextQuestionDeterminantDto → int?                │
│    - type: "GoToQuestion", nextQuestionId: 1 → int? = 1        │
│    - Validates target question exists (database query)         │
│    - Updates QuestionOption.NextQuestionId = 1                 │
│    - SaveChanges to database                                   │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. Optional: Immediate Cycle Validation                        │
│    POST /api/surveys/1/questions/validate                      │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. SurveyValidationService.DetectCycleAsync                    │
│    - Loads all questions: Q1, Q5, Q7                           │
│    - Builds adjacency list:                                    │
│      Q1 → [Q5] (option "Yes")                                  │
│      Q5 → [Q1] (option "Yes") ← CYCLE!                         │
│      Q7 → [0] (end)                                            │
│    - DFS from Q1: Visit Q1 → Q5 → Q1 (cycle detected)          │
│    - Result: { HasCycle: true, CyclePath: [1, 5, 1] }          │
└───────────────┬─────────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 6. Response: 400 Bad Request                                   │
│    {                                                            │
│      "valid": false,                                           │
│      "cyclePath": [1, 5, 1],                                    │
│      "errors": ["Cycle detected in question flow"]            │
│    }                                                            │
└─────────────────────────────────────────────────────────────────┘
```

**Key Integration Points**:

1. **API ↔ Infrastructure**:
   - Controllers inject `IQuestionService`, `ISurveyValidationService`
   - Services handle all business logic, data access
   - Controllers transform DTOs, handle HTTP concerns

2. **API ↔ Bot**:
   - Bot calls public endpoints: `/api/responses/{id}/next-question`
   - No authentication required for survey taking
   - Bot delegates ALL flow logic to API (clean separation)

3. **API ↔ Frontend**:
   - Frontend calls protected endpoints: `/api/surveys/{id}/questions/{id}/flow`
   - JWT authentication required
   - CORS configured for frontend origin
   - Real-time flow visualization via GET endpoint

---

## Best Practices

1. **Thin Controllers**: Business logic in services, not controllers
2. **Consistent Responses**: Always use `ApiResponse<T>` wrapper
3. **Exception Handling**: Let middleware catch unhandled exceptions
4. **Logging**: Structured logging with context (TraceId, UserId)
5. **Authentication**: Extract user ID once, pass to services
6. **Swagger**: Document all endpoints with `[SwaggerOperation]`
7. **Validation**: Check `ModelState.IsValid`, use Data Annotations
8. **Ownership Verification**: Prefer service-level checks for consistency
9. **Diagnostic Logging**: Use emoji markers for easy log scanning (Development only)
10. **Value Object Mapping**: Use factory methods in AutoMapper for value objects

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

## Architecture Summary (v1.4.0)

**SurveyBot.API** orchestrates the REST API layer with comprehensive conditional question flow support:

### Layer Responsibilities
- **Controllers** (10): Thin HTTP handlers delegating to services
- **Middleware Pipeline**: Exception → Logging → Auth → Authorization → Controllers
- **AutoMapper**: Complex value object mappings with factory methods
- **Background Services**: Non-blocking webhook processing via Channels
- **JWT Authentication**: Claims-based with ownership verification pattern

### Conditional Flow Architecture (v1.4.0)

**New Components**:
1. **QuestionFlowController** (685 lines): Flow CRUD, cycle detection, comprehensive logging
2. **Enhanced SurveysController**: Activation with flow validation (prevents cycles)
3. **Enhanced ResponsesController**: Next-question navigation endpoint (204 No Content when complete)
4. **AutoMapper Enhancements**: NextQuestionDeterminantDto factory method mappings

**Data Flow**:
- Survey creation → Question flow configuration → Cycle validation → Activation
- Response submission → Answer with conditional navigation → Next question determination → Completion
- Flow updates → Validation → Database update → Cycle re-check

**Integration Points**:
- **API ↔ Infrastructure**: Service injection, business logic delegation
- **API ↔ Bot**: Public endpoints for survey taking, no auth required
- **API ↔ Frontend**: Protected endpoints with JWT, CORS, flow visualization

### Key Patterns

1. **GetUserIdFromClaims Helper**: Centralized claim extraction with error handling
2. **Ownership Verification**: Service-level checks for consistency (recommended)
3. **Diagnostic Logging**: Emoji-based markers for easy log scanning (🔧 🔑 ✅ ⚠️ ❌)
4. **Value Object Mapping**: Factory methods in AutoMapper (ToQuestion, End)
5. **Mixed Authorization**: Public survey taking, protected survey management

### Performance Considerations

- **Background Processing**: Webhook handling via bounded Channel (capacity: 100)
- **Cycle Detection**: DFS algorithm, runs only on activation/validation
- **Database**: No FK constraints on NextQuestionId (allows magic value 0)
- **Logging**: Structured logging with Serilog, TraceId correlation

### Breaking Changes from v1.3.0

**NextQuestionDeterminant Value Object**:
- **Old**: `int? nextQuestionId` with magic value `0` = end survey
- **New**: `NextQuestionDeterminantDto { Type, NextQuestionId }` with explicit enum
- **Reason**: Type safety, eliminates magic values, prevents invalid states
- **Migration**: Use factory methods `ToQuestion(id)` or `End()`

**AutoMapper**:
- **Old**: Direct int? mapping
- **New**: Complex mapping with ConstructUsing for value objects
- **Impact**: Compilation errors if custom mappings exist

---

**Last Updated**: 2025-11-27 | **Version**: 1.5.0 (Location Question Type Support Added)
