# SurveyBot.Infrastructure - Data Access Layer

**Version**: 1.3.0 | **Target Framework**: .NET 8.0 | **EF Core**: 9.0.10 | **Database**: PostgreSQL 15+

> **Main Documentation**: [Project Root CLAUDE.md](../../CLAUDE.md)
> **Related**: [Core Layer](../SurveyBot.Core/CLAUDE.md) | [API Layer](../SurveyBot.API/CLAUDE.md)

---

## Overview

Infrastructure layer implements data access and business logic services. **Depends ONLY on Core layer**.

**Responsibilities**: Database access, repository pattern, service implementations, EF Core migrations, data seeding.

---

## Database Context

### SurveyBotDbContext

**Location**: `Data/SurveyBotDbContext.cs`

**DbSets**:
```csharp
public DbSet<User> Users { get; set; }
public DbSet<Survey> Surveys { get; set; }
public DbSet<Question> Questions { get; set; }
public DbSet<Response> Responses { get; set; }
public DbSet<Answer> Answers { get; set; }
```

**Key Feature - Automatic Timestamp Management**:
```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var entries = ChangeTracker.Entries()
        .Where(e => e.Entity is BaseEntity &&
                   (e.State == EntityState.Added || e.State == EntityState.Modified));

    foreach (var entry in entries)
    {
        var entity = (BaseEntity)entry.Entity;
        if (entry.State == EntityState.Added)
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
        }
        else if (entry.State == EntityState.Modified)
        {
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
    return base.SaveChangesAsync(cancellationToken);
}
```

**Benefit**: Developers never manually set timestamps - context handles automatically.

---

## Entity Configurations (Fluent API)

**Pattern**: Separate configuration classes implementing `IEntityTypeConfiguration<T>`

### Key Configurations

**UserConfiguration** - `Data/Configurations/UserConfiguration.cs`:
- TelegramId: Unique index (external identifier)
- Username: Partial index (only non-null values)
- Cascade delete to Surveys

**SurveyConfiguration** - `Data/Configurations/SurveyConfiguration.cs`:
- Code: Unique partial index (6-char sharing codes)
- IsActive: Filtered index (only active surveys)
- Composite index: `(CreatorId, IsActive)` for common queries
- Descending index: `CreatedAt DESC` for newest-first sorting
- Cascade delete to Questions and Responses

**QuestionConfiguration** - `Data/Configurations/QuestionConfiguration.cs`:
- OptionsJson: JSONB with GIN index (PostgreSQL-specific)
- MediaContent: JSONB with GIN index for multimedia metadata (NEW in v1.3.0)
- Composite unique index: `(SurveyId, OrderIndex)` prevents duplicates
- Check constraint: `order_index >= 0`
- Check constraint: `question_type IN ('text', 'multiple_choice', 'single_choice', 'rating', 'yes_no')`

**ResponseConfiguration** - `Data/Configurations/ResponseConfiguration.cs`:
- RespondentTelegramId: **NOT a foreign key** (allows anonymous responses)
- Composite index: `(SurveyId, RespondentTelegramId)` for duplicate checking
- IsComplete: Filtered index (only completed)

**AnswerConfiguration** - `Data/Configurations/AnswerConfiguration.cs`:
- AnswerJson: JSONB with GIN index
- Composite unique index: `(ResponseId, QuestionId)` (one answer per question)
- Check constraint: `answer_text IS NOT NULL OR answer_json IS NOT NULL`

---

## Repository Implementations

### SurveyRepository

**Location**: `Repositories/SurveyRepository.cs`

**Key Methods**:

```csharp
// Eager load questions ordered by index
public async Task<Survey?> GetByIdWithQuestionsAsync(int id)
{
    return await _dbSet
        .AsNoTracking()
        .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
        .Include(s => s.Creator)
        .FirstOrDefaultAsync(s => s.Id == id);
}

// Full data load (questions, responses, answers)
public async Task<Survey?> GetByIdWithDetailsAsync(int id)
{
    return await _dbSet
        .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
        .Include(s => s.Creator)
        .Include(s => s.Responses)
            .ThenInclude(r => r.Answers)
        .FirstOrDefaultAsync(s => s.Id == id);
}

// Find by unique code (case-insensitive)
public async Task<Survey?> GetByCodeAsync(string code)
{
    return await _dbSet
        .Include(s => s.Creator)
        .FirstOrDefaultAsync(s => s.Code == code.ToUpper());
}

// PostgreSQL case-insensitive search
public async Task<IEnumerable<Survey>> SearchByTitleAsync(string searchTerm)
{
    return await _dbSet
        .Include(s => s.Creator)
        .Include(s => s.Questions)
        .Where(s => EF.Functions.ILike(s.Title, $"%{searchTerm}%"))
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();
}
```

**Pattern**: Override base methods to add necessary eager loading.

### Other Repositories

**QuestionRepository** (`Repositories/QuestionRepository.cs`):
- `GetBySurveyIdAsync`: Ordered questions
- `GetNextOrderIndexAsync`: Auto-increment order
- `ReorderQuestionsAsync`: Bulk reordering with transaction

**ResponseRepository** (`Repositories/ResponseRepository.cs`):
- `GetIncompleteResponseAsync`: Resume incomplete survey
- `HasUserCompletedSurveyAsync`: Check completion
- `MarkAsCompleteAsync`: Update completion status

**UserRepository** (`Repositories/UserRepository.cs`):
- `GetByTelegramIdAsync`: Primary user lookup
- `CreateOrUpdateAsync`: **Upsert pattern** (critical for Telegram integration)
- `UpdateLastLoginAsync`: Track login activity

**AnswerRepository** (`Repositories/AnswerRepository.cs`):
- `GetByResponseIdAsync`: All answers for response
- `CreateBatchAsync`: Bulk answer creation
- `DeleteByResponseIdAsync`: Batch delete

---

## Service Layer

### SurveyService

**Location**: `Services/SurveyService.cs`

**Key Business Logic**:

**CreateSurveyAsync**:
1. Validate DTO
2. Map to Survey entity
3. Set CreatorId, IsActive=false
4. **Generate unique survey code** using `SurveyCodeGenerator`
5. Save to database

**UpdateSurveyAsync**:
- Authorization check (user owns survey)
- **Cannot modify active survey with responses** (data integrity)
- Update allowed properties only

**DeleteSurveyAsync - Smart Delete**:
```csharp
var hasResponses = await _surveyRepository.HasResponsesAsync(surveyId);

if (hasResponses)
{
    // Soft delete - preserve response data
    survey.IsActive = false;
    await _surveyRepository.UpdateAsync(survey);
}
else
{
    // Hard delete - clean up unused surveys
    await _surveyRepository.DeleteAsync(surveyId);
}
```

**ActivateSurveyAsync**:
- Validates survey has at least one question
- Sets IsActive=true
- Only survey creator can activate

**GetSurveyStatisticsAsync**:
- Total/completed/incomplete responses
- Completion rate percentage
- Average completion time (seconds)
- Unique respondents count
- Per-question statistics

**GetSurveyByCodeAsync**:
- Public endpoint (no auth required)
- **Only returns active surveys** (security)
- Used by Telegram bot: `/survey ABC123`

### AuthService

**Location**: `Services/AuthService.cs`

**JWT Token Generation**:
```csharp
public string GenerateAccessToken(int userId, long telegramId, string? username)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim("TelegramId", telegramId.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
    };

    if (!string.IsNullOrWhiteSpace(username))
    {
        claims.Add(new Claim(ClaimTypes.Name, username));
    }

    var token = new JwtSecurityToken(
        issuer: _jwtSettings.Issuer,
        audience: _jwtSettings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(_jwtSettings.TokenLifetimeHours),
        signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            SecurityAlgorithms.HmacSha256)
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**LoginAsync**:
- Upsert user (create if new, update if exists)
- No password validation (Telegram auth handled externally)
- Returns JWT + refresh token

### Other Services

**QuestionService** (`Services/QuestionService.cs`):
- `AddQuestionAsync`: Type-specific validation
- `UpdateQuestionAsync`: Protection rules
- `ReorderQuestionsAsync`: Change question order

**ResponseService** (`Services/ResponseService.cs`):
- `StartResponseAsync`: Begin survey
- `SaveAnswerAsync`: Save/update answer with validation
- `CompleteResponseAsync`: Finalize response
- Answer validation methods for each question type

**UserService** (`Services/UserService.cs`):
- `RegisterAsync`: User registration with JWT (upsert)
- `GetUserByTelegramIdAsync`: Telegram ID lookup

**MediaStorageService** (`Services/MediaStorageService.cs`) - NEW in v1.3.0:
- `SaveMediaAsync`: Store uploaded files with type-specific handling
- `DeleteMediaAsync`: Remove media files from storage
- `GetMediaAsync`: Retrieve media file information
- File organization: `/media/{type}/{filename}` structure
- Thumbnail generation for images (200x200px)
- Automatic cleanup on deletion

**MediaValidationService** (`Services/MediaValidationService.cs`) - NEW in v1.3.0:
- `ValidateMediaAsync`: Validate file type, size, format
- `ValidateMediaWithAutoDetectionAsync`: Auto-detect file type from content
- Magic byte analysis for file type detection
- MIME type verification
- File extension validation as fallback
- Comprehensive error reporting with validation details

---

## Database Migrations

**Location**: `Migrations/` directory

**Commands** (run from API project):
```bash
cd src/SurveyBot.API

# Add migration
dotnet ef migrations add MigrationName --project ../SurveyBot.Infrastructure

# Apply migrations
dotnet ef database update --project ../SurveyBot.Infrastructure

# Remove last migration (if not applied)
dotnet ef migrations remove --project ../SurveyBot.Infrastructure

# Generate SQL script
dotnet ef migrations script --project ../SurveyBot.Infrastructure
```

**Existing Migrations**:
1. **InitialCreate** - Creates all tables, indexes, relationships
2. **AddLastLoginAtToUser** - Adds LastLoginAt column
3. **AddSurveyCodeColumn** - Adds Code column with unique index
4. **AddMediaContentToQuestion** - Adds MediaContent JSONB column (v1.3.0)

**Best Practices**:
- Always backup before production migrations
- Test in dev/staging first
- Review generated SQL script
- Never edit applied migrations
- Use descriptive migration names

---

## Performance Optimizations

### Index Strategy

**Implemented Indexes**:
- **Primary keys**: Automatic on all Id columns
- **Foreign keys**: All foreign key columns
- **Unique indexes**: `telegram_id`, `code` (with partial filter)
- **Partial indexes**: Filtered indexes on commonly queried subsets
- **Composite indexes**: Multi-column for common query patterns
- **GIN indexes**: JSONB columns (`options_json`, `answer_json`)
- **Descending indexes**: For reverse ordering (`created_at DESC`)

### Query Optimization

**Eager Loading** (Prevents N+1):
```csharp
// BAD: N+1 queries
var surveys = await _context.Surveys.ToListAsync();
foreach (var survey in surveys)
{
    Console.WriteLine(survey.Creator.Username); // Query per survey!
}

// GOOD: Single query with eager loading
var surveys = await _context.Surveys
    .Include(s => s.Creator)
    .ToListAsync();
```

**AsNoTracking** (Read-only queries):
```csharp
return await _dbSet
    .AsNoTracking() // No change tracking overhead
    .Include(s => s.Questions)
    .FirstOrDefaultAsync(s => s.Id == id);
```

**Pagination**:
```csharp
var page = 1;
var pageSize = 20;
var surveys = await _context.Surveys
    .OrderByDescending(s => s.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

## Common Patterns

### Upsert Pattern (User Management)

```csharp
public async Task<User> CreateOrUpdateAsync(long telegramId, ...)
{
    var existingUser = await GetByTelegramIdAsync(telegramId);

    if (existingUser != null)
    {
        // Update existing
        existingUser.Username = username;
        existingUser.FirstName = firstName;
        await _context.SaveChangesAsync();
        return existingUser;
    }

    // Create new
    var newUser = new User { TelegramId = telegramId, ... };
    await _dbSet.AddAsync(newUser);
    await _context.SaveChangesAsync();
    return newUser;
}
```

### Transaction Pattern

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    await UpdateOperation1();
    await UpdateOperation2();
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

---

## Error Handling

**Service Layer Exception Pattern**:
```csharp
try
{
    await _surveyRepository.UpdateAsync(survey);
}
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogError(ex, "Concurrency conflict updating survey {SurveyId}", surveyId);
    throw new SurveyOperationException("Survey was modified by another user", ex);
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Database error updating survey {SurveyId}", surveyId);
    throw new SurveyOperationException("Error updating survey", ex);
}
```

**Logging Best Practices**:
```csharp
// Structured logging with context
_logger.LogInformation(
    "Survey {SurveyId} created by user {UserId} with code {Code}",
    survey.Id, userId, survey.Code);

// Warning for suspicious activity
_logger.LogWarning(
    "User {UserId} attempted to access survey {SurveyId} owned by {OwnerId}",
    userId, surveyId, survey.CreatorId);

// Error with exception
_logger.LogError(ex, "Failed to delete survey {SurveyId}", surveyId);
```

---

## Common Issues & Solutions

### Issue: DbContext Disposed Early

**Problem**: Navigation properties accessed after DbContext disposed

**Solution**: Use eager loading with `Include()`:
```csharp
// GOOD: Eager loading
var survey = await _repository.GetByIdWithQuestionsAsync(id);
foreach (var question in survey.Questions) // Works!
```

### Issue: Circular Reference in JSON

**Problem**: Entity navigation properties cause infinite loops

**Solution**: Return DTOs, not entities:
```csharp
// GOOD: Return DTO
return _mapper.Map<SurveyDto>(survey); // No circular references
```

### Issue: N+1 Query Problem

**Solution**: Use `Include()` for eager loading (see Query Optimization above)

### Issue: Connection Pool Exhaustion

**Solutions**:
1. Ensure DbContext disposed (automatic with DI Scoped lifetime)
2. Increase pool size: `MaxPoolSize=200` in connection string
3. Use async operations: `await _context.SaveChangesAsync()`

---

## Dependency Injection

**Registration** (`DependencyInjection.cs`):

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // DbContext (Scoped)
    services.AddDbContext<SurveyBotDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

    // Repositories (Scoped)
    services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
    services.AddScoped<ISurveyRepository, SurveyRepository>();
    services.AddScoped<IQuestionRepository, QuestionRepository>();
    services.AddScoped<IResponseRepository, ResponseRepository>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IAnswerRepository, AnswerRepository>();

    // Services (Scoped)
    services.AddScoped<ISurveyService, SurveyService>();
    services.AddScoped<IQuestionService, QuestionService>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IResponseService, ResponseService>();
    services.AddScoped<IUserService, UserService>();

    // Media Services (NEW in v1.3.0)
    services.AddScoped<IMediaStorageService, MediaStorageService>();
    services.AddScoped<IMediaValidationService, MediaValidationService>();

    return services;
}
```

**Usage in API** (`Program.cs`):
```csharp
builder.Services.AddInfrastructure(builder.Configuration);
```

**Service Lifetimes**: All Scoped (one instance per HTTP request, disposed at end)

---

## Summary

**SurveyBot.Infrastructure** implements robust data access layer with:

- **Clean Architecture**: Depends only on Core
- **Repository Pattern**: Data access abstraction
- **Service Layer**: Comprehensive business logic
- **Fluent API**: Entity configurations with PostgreSQL optimizations
- **Automatic Timestamps**: Context manages CreatedAt/UpdatedAt
- **Smart Delete**: Soft delete for surveys with responses
- **Survey Codes**: Generation and collision detection
- **EF Core Migrations**: Schema version control

**Performance**: Eager loading, AsNoTracking, indexes (composite, partial, GIN), connection pooling, batch operations

**Data Integrity**: Foreign keys with cascade delete, unique constraints, check constraints, transactions

**Total**: ~3,500+ lines implementing data access and business logic

---

## Related Documentation

### Documentation Hub

For comprehensive project documentation, see the **centralized documentation folder**:

**Main Documentation**:
- [Project Root CLAUDE.md](../../CLAUDE.md) - Overall project overview and quick start
- [Documentation Index](../../documentation/INDEX.md) - Complete documentation catalog
- [Navigation Guide](../../documentation/NAVIGATION.md) - Role-based navigation

**Database Documentation**:
- [Quick Start Database](../../documentation/database/QUICK-START-DATABASE.md) - Database setup and migrations
- [ER Diagram](../../documentation/database/ER_DIAGRAM.md) - Entity relationships
- [Entity Relationships](../../documentation/database/RELATIONSHIPS.md) - Detailed relationships
- [Index Optimization](../../documentation/database/INDEX_OPTIMIZATION.md) - Performance indexing
- [Database README](../../documentation/database/README.md) - Database overview

**Related Layer Documentation**:
- [Core Layer](../SurveyBot.Core/CLAUDE.md) - Domain entities and interfaces
- [API Layer](../SurveyBot.API/CLAUDE.md) - Controllers using Infrastructure services
- [Bot Layer](../SurveyBot.Bot/CLAUDE.md) - Bot using Infrastructure services

**Development Resources**:
- [DI Structure](../../documentation/development/DI-STRUCTURE.md) - Dependency injection patterns
- [Developer Onboarding](../../documentation/DEVELOPER_ONBOARDING.md) - Getting started guide
- [Troubleshooting](../../documentation/TROUBLESHOOTING.md) - Common database issues

**Deployment**:
- [Docker Startup Guide](../../documentation/deployment/DOCKER-STARTUP-GUIDE.md) - PostgreSQL setup
- [Docker README](../../documentation/deployment/DOCKER-README.md) - Production deployment

### Documentation Maintenance

**When updating Infrastructure layer**:
1. Update this CLAUDE.md file with repository/service changes
2. Update [Database README](../../documentation/database/README.md) if schema changes
3. Update [ER Diagram](../../documentation/database/ER_DIAGRAM.md) if relationships change
4. Update [Quick Start Database](../../documentation/database/QUICK-START-DATABASE.md) if migration process changes
5. Update [Core CLAUDE.md](../SurveyBot.Core/CLAUDE.md) if interface contracts change
6. Update [Documentation Index](../../documentation/INDEX.md) if adding significant documentation

**Where to save Infrastructure-related documentation**:
- Repository implementations → This file
- Service layer logic → This file
- Entity configurations → This file
- Migration procedures → `documentation/database/`
- Database optimization → `documentation/database/INDEX_OPTIMIZATION.md`
- Database fixes → `documentation/fixes/`
- Performance tuning → `documentation/database/`

**Migration Documentation**:
- After creating migrations, document schema changes in [Database README](../../documentation/database/README.md)
- Update task completion reports in `documentation/database/TASK_*.md` if applicable
- Document breaking changes in main [CLAUDE.md](../../CLAUDE.md)

---

**Last Updated**: 2025-11-21 | **Version**: 1.3.0
