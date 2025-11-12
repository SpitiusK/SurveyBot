# SurveyBot.Infrastructure - Data Access Layer

**Last Updated**: 2025-11-12
**Version**: 1.0.0
**Target Framework**: .NET 8.0
**EF Core Version**: 9.0.10
**Database**: PostgreSQL 15+

---

## Table of Contents

1. [Layer Overview](#layer-overview)
2. [Architecture and Dependencies](#architecture-and-dependencies)
3. [Project Structure](#project-structure)
4. [Database Context](#database-context)
5. [Entity Configurations](#entity-configurations)
6. [Repository Layer](#repository-layer)
7. [Service Layer](#service-layer)
8. [Database Migrations](#database-migrations)
9. [Data Seeding](#data-seeding)
10. [Dependency Injection](#dependency-injection)
11. [Performance Optimization](#performance-optimization)
12. [Best Practices and Patterns](#best-practices-and-patterns)
13. [Error Handling](#error-handling)
14. [Testing Strategies](#testing-strategies)
15. [Common Issues and Solutions](#common-issues-and-solutions)

---

## Layer Overview

### Purpose

**SurveyBot.Infrastructure** is the data access layer that implements all database operations and business logic services for the SurveyBot application. This layer is responsible for:

- **Database Access**: All interactions with PostgreSQL via Entity Framework Core
- **Repository Pattern**: Concrete implementations of repository interfaces defined in Core
- **Service Layer**: Business logic services implementing domain operations
- **Data Persistence**: Transaction management, change tracking, and state management
- **Schema Management**: Database migrations and version control
- **Data Initialization**: Seeding development and test data

### Key Responsibilities

1. **Data Access Abstraction**: Isolates database specifics from business logic
2. **Entity Framework Configuration**: Fluent API configurations for all entities
3. **Query Optimization**: Eager loading, indexes, and performance tuning
4. **Business Logic Implementation**: Domain operations with validation and authorization
5. **Database Evolution**: Migration generation and application
6. **Connection Management**: Connection pooling and lifetime management

### Architecture Principles

**Clean Architecture Compliance**:
- Depends **only** on `SurveyBot.Core` (Domain layer)
- **No dependencies** on API or Bot layers
- Implements interfaces defined in Core
- All external concerns abstracted behind interfaces

**Separation of Concerns**:
- **Repositories**: Data access only (queries, commands)
- **Services**: Business logic only (validation, authorization, orchestration)
- **Configurations**: Entity mapping only (Fluent API)
- **Migrations**: Schema evolution only

---

## Architecture and Dependencies

### Dependency Graph

```
SurveyBot.Core (Domain)
        ↑
        |
SurveyBot.Infrastructure (Data Access)
        ↑
        |
SurveyBot.API (Presentation)
```

### Package Dependencies

**Project File**: `SurveyBot.Infrastructure.csproj` (Lines 1-26)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- Project Reference -->
  <ItemGroup>
    <ProjectReference Include="..\SurveyBot.Core\SurveyBot.Core.csproj" />
  </ItemGroup>

  <!-- NuGet Packages -->
  <ItemGroup>
    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.10">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Extensions.Options" Version="9.0.10" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.1.2" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

**Key Dependencies**:

1. **Entity Framework Core 9.0.10**: ORM framework for database operations
2. **Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4**: PostgreSQL database provider
3. **AutoMapper 12.0.1**: Object-to-object mapping (Entity ↔ DTO)
4. **System.IdentityModel.Tokens.Jwt 7.1.2**: JWT token generation/validation
5. **Microsoft.Extensions.Options 9.0.10**: Strongly-typed configuration
6. **EF Core Design**: Migration generation tooling

---

## Project Structure

### Complete Directory Tree

```
SurveyBot.Infrastructure/
├── Data/                                      # Database context and configuration
│   ├── SurveyBotDbContext.cs                 # Main DbContext (109 lines)
│   ├── DataSeeder.cs                         # Test data generation (587 lines)
│   ├── Configurations/                       # Fluent API entity configurations
│   │   ├── UserConfiguration.cs              # User entity config (73 lines)
│   │   ├── SurveyConfiguration.cs            # Survey entity config (117 lines)
│   │   ├── QuestionConfiguration.cs          # Question entity config (107 lines)
│   │   ├── ResponseConfiguration.cs          # Response entity config (78 lines)
│   │   └── AnswerConfiguration.cs            # Answer entity config (89 lines)
│   └── Extensions/
│       └── DatabaseExtensions.cs             # Seeding extension methods (61 lines)
├── Repositories/                             # Repository implementations
│   ├── GenericRepository.cs                  # Base repository (95 lines)
│   ├── SurveyRepository.cs                   # Survey operations (169 lines)
│   ├── QuestionRepository.cs                 # Question operations (148 lines)
│   ├── ResponseRepository.cs                 # Response operations (160 lines)
│   ├── UserRepository.cs                     # User operations (146 lines)
│   └── AnswerRepository.cs                   # Answer operations (134 lines)
├── Services/                                 # Business logic services
│   ├── AuthService.cs                        # JWT authentication (174 lines)
│   ├── SurveyService.cs                      # Survey business logic (724 lines)
│   ├── QuestionService.cs                    # Question business logic (462 lines)
│   ├── ResponseService.cs                    # Response business logic (623 lines)
│   └── UserService.cs                        # User management (289 lines)
├── Migrations/                               # EF Core database migrations
│   ├── 20251105190107_InitialCreate.cs
│   ├── 20251106000001_AddLastLoginAtToUser.cs
│   ├── 20251109000001_AddSurveyCodeColumn.cs
│   └── SurveyBotDbContextModelSnapshot.cs
├── DependencyInjection.cs                    # Service registration (46 lines)
└── SurveyBot.Infrastructure.csproj           # Project file
```

### File Count Summary

- **Total C# Files**: ~25 production files
- **Total Lines of Code**: ~3,500+ lines
- **Entity Configurations**: 5 files (464 lines)
- **Repositories**: 6 files (852 lines)
- **Services**: 5 files (2,272 lines)
- **Migrations**: 3 applied migrations + snapshot

---

## Database Context

### SurveyBotDbContext

**Location**: `Data/SurveyBotDbContext.cs` (Lines 1-109)

The main database context managing all entity sets and database operations.

#### Class Structure

```csharp
// Lines 11-16
public class SurveyBotDbContext : DbContext
{
    public SurveyBotDbContext(DbContextOptions<SurveyBotDbContext> options)
        : base(options)
    {
    }
```

#### DbSet Properties

All domain entities exposed as DbSets:

```csharp
// Lines 21, 26, 31, 36, 41
public DbSet<User> Users { get; set; } = null!;
public DbSet<Survey> Surveys { get; set; } = null!;
public DbSet<Question> Questions { get; set; } = null!;
public DbSet<Response> Responses { get; set; } = null!;
public DbSet<Answer> Answers { get; set; } = null!;
```

**Naming Convention**: Plural table names (users, surveys, questions, responses, answers)

#### OnModelCreating - Entity Configuration

**Lines 43-56**: Applies all Fluent API configurations

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Apply all entity configurations
    modelBuilder.ApplyConfiguration(new UserConfiguration());
    modelBuilder.ApplyConfiguration(new SurveyConfiguration());
    modelBuilder.ApplyConfiguration(new QuestionConfiguration());
    modelBuilder.ApplyConfiguration(new ResponseConfiguration());
    modelBuilder.ApplyConfiguration(new AnswerConfiguration());
}
```

**Pattern**: Separate configuration classes implementing `IEntityTypeConfiguration<T>` for better organization.

#### OnConfiguring - Development Enhancements

**Lines 58-68**: Enables detailed logging in development

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    base.OnConfiguring(optionsBuilder);

    // Enable detailed logging in development
    if (!optionsBuilder.IsConfigured)
    {
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
    }
}
```

**Development Features**:
- `EnableSensitiveDataLogging()`: Shows parameter values in logs
- `EnableDetailedErrors()`: Provides detailed error messages

**Warning**: Only enabled when options not configured (development scenario). Production configuration disables these.

#### SaveChangesAsync Override - Automatic Timestamp Management

**Lines 70-107**: Critical feature for automatic timestamp handling

```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // Update timestamps for entities that have UpdatedAt property
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

    // Update CreatedAt for entities that have it but don't inherit from BaseEntity
    var nonBaseEntities = ChangeTracker.Entries()
        .Where(e => !(e.Entity is BaseEntity) && e.State == EntityState.Added);

    foreach (var entry in nonBaseEntities)
    {
        var entity = entry.Entity;
        var createdAtProperty = entity.GetType().GetProperty("CreatedAt");

        if (createdAtProperty != null && createdAtProperty.PropertyType == typeof(DateTime))
        {
            createdAtProperty.SetValue(entity, DateTime.UtcNow);
        }
    }

    return base.SaveChangesAsync(cancellationToken);
}
```

**Key Features**:
1. **Automatic CreatedAt**: Set when entity first added (EntityState.Added)
2. **Automatic UpdatedAt**: Updated on every modification (EntityState.Modified)
3. **UTC Timestamps**: All timestamps stored in UTC for consistency
4. **BaseEntity Support**: Handles entities inheriting from BaseEntity (User, Survey, Question)
5. **Non-BaseEntity Support**: Uses reflection for entities with CreatedAt but not inheriting BaseEntity (Response, Answer)

**Benefit**: Developers never need to manually set timestamps. The context handles it automatically.

---

## Entity Configurations

All entity configurations use the Fluent API pattern by implementing `IEntityTypeConfiguration<T>`. This approach separates configuration from entity classes and keeps configurations organized.

### UserConfiguration

**Location**: `Data/Configurations/UserConfiguration.cs` (Lines 1-73)

Configures the User entity mapping to the `users` table.

#### Table and Primary Key

```csharp
// Lines 15, 18-21
builder.ToTable("users");

builder.HasKey(u => u.Id);
builder.Property(u => u.Id)
    .HasColumnName("id")
    .ValueGeneratedOnAdd();
```

#### TelegramId - Unique External Identifier

```csharp
// Lines 24-30
builder.Property(u => u.TelegramId)
    .HasColumnName("telegram_id")
    .IsRequired();

builder.HasIndex(u => u.TelegramId)
    .IsUnique()
    .HasDatabaseName("idx_users_telegram_id");
```

**Important**: `TelegramId` is the user's unique identifier from Telegram API. The unique index prevents duplicate user registrations.

#### Username with Partial Index

```csharp
// Lines 33-39
builder.Property(u => u.Username)
    .HasColumnName("username")
    .HasMaxLength(255);

builder.HasIndex(u => u.Username)
    .HasDatabaseName("idx_users_username")
    .HasFilter("username IS NOT NULL");
```

**Partial Index**: Only indexes non-null usernames (optimization). Not all Telegram users have usernames.

#### Name Fields

```csharp
// Lines 42-49
builder.Property(u => u.FirstName)
    .HasColumnName("first_name")
    .HasMaxLength(255);

builder.Property(u => u.LastName)
    .HasColumnName("last_name")
    .HasMaxLength(255);
```

**Note**: All name fields nullable (Telegram doesn't require them).

#### Timestamp Columns

```csharp
// Lines 52-63
builder.Property(u => u.CreatedAt)
    .HasColumnName("created_at")
    .HasColumnType("timestamp with time zone")
    .IsRequired()
    .HasDefaultValueSql("CURRENT_TIMESTAMP");

builder.Property(u => u.UpdatedAt)
    .HasColumnName("updated_at")
    .HasColumnType("timestamp with time zone")
    .IsRequired()
    .HasDefaultValueSql("CURRENT_TIMESTAMP");
```

**PostgreSQL**: Uses `timestamp with time zone` for proper timezone handling.

#### Relationships - Cascade Delete

```csharp
// Lines 66-70
builder.HasMany(u => u.Surveys)
    .WithOne(s => s.Creator)
    .HasForeignKey(s => s.CreatorId)
    .OnDelete(DeleteBehavior.Cascade)
    .HasConstraintName("fk_surveys_creator");
```

**Cascade Delete**: Deleting a user deletes all their surveys (and transitively, questions, responses, and answers).

### SurveyConfiguration

**Location**: `Data/Configurations/SurveyConfiguration.cs` (Lines 1-117)

Configures the Survey entity with advanced indexing strategies.

#### Basic Properties

```csharp
// Lines 15-32
builder.ToTable("surveys");

builder.HasKey(s => s.Id);
builder.Property(s => s.Id)
    .HasColumnName("id")
    .ValueGeneratedOnAdd();

builder.Property(s => s.Title)
    .HasColumnName("title")
    .HasMaxLength(500)
    .IsRequired();

builder.Property(s => s.Description)
    .HasColumnName("description")
    .HasColumnType("text");
```

#### Survey Code - Unique Sharing Code

```csharp
// Lines 35-42
builder.Property(s => s.Code)
    .HasColumnName("code")
    .HasMaxLength(10);

builder.HasIndex(s => s.Code)
    .IsUnique()
    .HasDatabaseName("idx_surveys_code")
    .HasFilter("code IS NOT NULL");
```

**Feature**: Unique alphanumeric code for sharing surveys (e.g., "ABC123"). Partial unique index allows null codes.

**Use Case**: Users can share surveys via code instead of ID: `/survey ABC123`

#### Boolean Flags with Defaults

```csharp
// Lines 53-72
builder.Property(s => s.IsActive)
    .HasColumnName("is_active")
    .IsRequired()
    .HasDefaultValue(true);

builder.HasIndex(s => s.IsActive)
    .HasDatabaseName("idx_surveys_is_active")
    .HasFilter("is_active = true");

builder.Property(s => s.AllowMultipleResponses)
    .HasColumnName("allow_multiple_responses")
    .IsRequired()
    .HasDefaultValue(false);

builder.Property(s => s.ShowResults)
    .HasColumnName("show_results")
    .IsRequired()
    .HasDefaultValue(true);
```

**Filtered Index on IsActive**: Only indexes active surveys for performance (most queries filter by active).

#### Performance Indexes

**Composite Index** for common query pattern:

```csharp
// Lines 89-90
builder.HasIndex(s => new { s.CreatorId, s.IsActive })
    .HasDatabaseName("idx_surveys_creator_active");
```

**Use Case**: Query pattern: "Get all active surveys for user X" (very common).

**Descending Index** for sorting:

```csharp
// Lines 93-95
builder.HasIndex(s => s.CreatedAt)
    .HasDatabaseName("idx_surveys_created_at")
    .IsDescending();
```

**Use Case**: Newest-first sorting (default display order).

#### Relationships - Multiple Cascade Deletes

```csharp
// Lines 98-114
builder.HasOne(s => s.Creator)
    .WithMany(u => u.Surveys)
    .HasForeignKey(s => s.CreatorId)
    .OnDelete(DeleteBehavior.Cascade)
    .HasConstraintName("fk_surveys_creator");

builder.HasMany(s => s.Questions)
    .WithOne(q => q.Survey)
    .HasForeignKey(q => q.SurveyId)
    .OnDelete(DeleteBehavior.Cascade)
    .HasConstraintName("fk_questions_survey");

builder.HasMany(s => s.Responses)
    .WithOne(r => r.Survey)
    .HasForeignKey(r => r.SurveyId)
    .OnDelete(DeleteBehavior.Cascade)
    .HasConstraintName("fk_responses_survey");
```

**Cascade Chain**: User → Survey → Questions/Responses → Answers

### QuestionConfiguration

**Location**: `Data/Configurations/QuestionConfiguration.cs` (Lines 1-107)

Configures Question entity with JSONB storage and check constraints.

#### Question Type with Check Constraint

```csharp
// Lines 38-49
builder.Property(q => q.QuestionType)
    .HasColumnName("question_type")
    .HasMaxLength(50)
    .IsRequired();

builder.HasIndex(q => q.QuestionType)
    .HasDatabaseName("idx_questions_type");

// Check constraint for question type
builder.ToTable(t => t.HasCheckConstraint(
    "chk_question_type",
    "question_type IN ('text', 'multiple_choice', 'single_choice', 'rating', 'yes_no')"));
```

**Database-Level Validation**: Check constraint ensures only valid question types stored.

#### Order Index with Constraints

```csharp
// Lines 52-68
builder.Property(q => q.OrderIndex)
    .HasColumnName("order_index")
    .IsRequired();

// Composite index for ordered question retrieval
builder.HasIndex(q => new { q.SurveyId, q.OrderIndex })
    .HasDatabaseName("idx_questions_survey_order");

// Unique constraint for survey_id + order_index
builder.HasIndex(q => new { q.SurveyId, q.OrderIndex })
    .IsUnique()
    .HasDatabaseName("idx_questions_survey_order_unique");

// Check constraint for order index
builder.ToTable(t => t.HasCheckConstraint(
    "chk_order_index",
    "order_index >= 0"));
```

**Data Integrity**:
- Unique constraint prevents duplicate order indexes within a survey
- Check constraint ensures non-negative ordering
- Index optimizes ordered retrieval

#### JSONB Options Storage

```csharp
// Lines 76-84
// OptionsJson - stored as JSONB in PostgreSQL
builder.Property(q => q.OptionsJson)
    .HasColumnName("options_json")
    .HasColumnType("jsonb");

// GIN index for JSONB options searching
builder.HasIndex(q => q.OptionsJson)
    .HasDatabaseName("idx_questions_options_json")
    .HasMethod("gin");
```

**PostgreSQL JSONB**:
- Binary JSON format (faster than text JSON)
- Supports indexing and querying
- GIN (Generalized Inverted Index) for fast searches
- Flexible schema for different question types

**Options Format**:
```json
["Option 1", "Option 2", "Option 3"]
```

### ResponseConfiguration

**Location**: `Data/Configurations/ResponseConfiguration.cs` (Lines 1-78)

Configures Response entity with completion tracking.

#### RespondentTelegramId - NOT a Foreign Key

```csharp
// Lines 32-38
// RespondentTelegramId - NOT a foreign key to allow anonymous responses
builder.Property(r => r.RespondentTelegramId)
    .HasColumnName("respondent_telegram_id")
    .IsRequired();

// Composite index for survey + respondent (for duplicate checking)
builder.HasIndex(r => new { r.SurveyId, r.RespondentTelegramId })
    .HasDatabaseName("idx_responses_survey_respondent");
```

**Design Decision**: `RespondentTelegramId` is NOT a foreign key to the User table.

**Reasons**:
1. Allows responses from users not in our system (anonymous/public surveys)
2. Telegram ID sufficient for tracking without requiring user registration
3. More flexible for public survey scenarios
4. Still indexed for performance

#### Completion Status with Filtered Index

```csharp
// Lines 41-48
builder.Property(r => r.IsComplete)
    .HasColumnName("is_complete")
    .IsRequired()
    .HasDefaultValue(false);

builder.HasIndex(r => r.IsComplete)
    .HasDatabaseName("idx_responses_complete")
    .HasFilter("is_complete = true");
```

**Filtered Index**: Only indexes completed responses (common query pattern for statistics).

#### Timestamp Tracking

```csharp
// Lines 51-62
builder.Property(r => r.StartedAt)
    .HasColumnName("started_at")
    .HasColumnType("timestamp with time zone");

builder.Property(r => r.SubmittedAt)
    .HasColumnName("submitted_at")
    .HasColumnType("timestamp with time zone");

builder.HasIndex(r => r.SubmittedAt)
    .HasDatabaseName("idx_responses_submitted_at")
    .HasFilter("submitted_at IS NOT NULL");
```

**Response Lifecycle**:
1. **Started**: `StartedAt` set, `IsComplete = false`
2. **In Progress**: User answering questions
3. **Completed**: `SubmittedAt` set, `IsComplete = true`

### AnswerConfiguration

**Location**: `Data/Configurations/AnswerConfiguration.cs` (Lines 1-89)

Configures Answer entity with JSONB storage and data validation.

#### Composite Unique Constraint

```csharp
// Lines 40-46
// Composite index for response + question
builder.HasIndex(a => new { a.ResponseId, a.QuestionId })
    .HasDatabaseName("idx_answers_response_question");

// Unique constraint: one answer per question per response
builder.HasIndex(a => new { a.ResponseId, a.QuestionId })
    .IsUnique()
    .HasDatabaseName("idx_answers_response_question_unique");
```

**Data Integrity**: Prevents duplicate answers to the same question in one response.

#### Dual Storage - Text and JSON

```csharp
// Lines 49-56
builder.Property(a => a.AnswerText)
    .HasColumnName("answer_text")
    .HasColumnType("text");

builder.Property(a => a.AnswerJson)
    .HasColumnName("answer_json")
    .HasColumnType("jsonb");
```

**Why Both?**:
- `AnswerText`: Simple text answers (text questions)
- `AnswerJson`: Structured data (choices, ratings, complex answers)

#### JSONB with GIN Index

```csharp
// Lines 58-61
// GIN index for JSONB answer searching
builder.HasIndex(a => a.AnswerJson)
    .HasDatabaseName("idx_answers_answer_json")
    .HasMethod("gin");
```

**Answer JSON Formats**:

**Text Answer**:
```json
{"text": "User's free-form response"}
```

**Single Choice**:
```json
{"selectedOption": "Option 2"}
```

**Multiple Choice**:
```json
{"selectedOptions": ["Option 1", "Option 3", "Option 5"]}
```

**Rating**:
```json
{"rating": 4}
```

#### Check Constraint - Data Validation

```csharp
// Lines 64-66
builder.ToTable(t => t.HasCheckConstraint(
    "chk_answer_not_null",
    "answer_text IS NOT NULL OR answer_json IS NOT NULL"));
```

**Database-Level Validation**: At least one of `answer_text` or `answer_json` must be populated.

---

## Repository Layer

The repository layer abstracts data access logic and provides a clean API for querying and manipulating entities.

### GenericRepository<T>

**Location**: `Repositories/GenericRepository.cs` (Lines 1-95)

Base repository providing common CRUD operations for all entities.

#### Class Structure

```csharp
// Lines 11-24
public class GenericRepository<T> : IRepository<T> where T : class
{
    protected readonly SurveyBotDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(SurveyBotDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<T>();
    }
```

**Protected Fields**: Allow derived repositories to access context and DbSet directly.

#### Core Operations

**GetByIdAsync** - Find by primary key:

```csharp
// Lines 27-30
public virtual async Task<T?> GetByIdAsync(int id)
{
    return await _dbSet.FindAsync(id);
}
```

**Note**: Uses `FindAsync()` which checks local cache first, then queries database.

**GetAllAsync** - Retrieve all entities:

```csharp
// Lines 33-36
public virtual async Task<IEnumerable<T>> GetAllAsync()
{
    return await _dbSet.ToListAsync();
}
```

**Warning**: Returns ALL records. Override in derived classes for filtering/pagination.

**CreateAsync** - Add new entity:

```csharp
// Lines 39-50
public virtual async Task<T> CreateAsync(T entity)
{
    if (entity == null)
    {
        throw new ArgumentNullException(nameof(entity));
    }

    await _dbSet.AddAsync(entity);
    await _context.SaveChangesAsync();

    return entity;
}
```

**Side Effect**: Calls `SaveChangesAsync()` which triggers automatic timestamp updates.

**UpdateAsync** - Modify existing entity:

```csharp
// Lines 53-64
public virtual async Task<T> UpdateAsync(T entity)
{
    if (entity == null)
    {
        throw new ArgumentNullException(nameof(entity));
    }

    _dbSet.Update(entity);
    await _context.SaveChangesAsync();

    return entity;
}
```

**DeleteAsync** - Remove entity:

```csharp
// Lines 67-80
public virtual async Task<bool> DeleteAsync(int id)
{
    var entity = await GetByIdAsync(id);

    if (entity == null)
    {
        return false;
    }

    _dbSet.Remove(entity);
    await _context.SaveChangesAsync();

    return true;
}
```

**Return Value**: `false` if entity not found, `true` on successful delete.

**ExistsAsync** and **CountAsync**:

```csharp
// Lines 83-93
public virtual async Task<bool> ExistsAsync(int id)
{
    var entity = await _dbSet.FindAsync(id);
    return entity != null;
}

public virtual async Task<int> CountAsync()
{
    return await _dbSet.CountAsync();
}
```

**Key Points**:
- All methods are `virtual` for overriding in derived classes
- All operations are async for scalability
- SaveChangesAsync called after modifications (ensures consistency)
- Null checks on input parameters

### SurveyRepository

**Location**: `Repositories/SurveyRepository.cs` (Lines 1-169)

Survey-specific repository with complex queries and eager loading.

#### GetByIdWithQuestionsAsync - Eager Load Questions

```csharp
// Lines 22-29
public async Task<Survey?> GetByIdWithQuestionsAsync(int id)
{
    return await _dbSet
        .AsNoTracking()
        .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
        .Include(s => s.Creator)
        .FirstOrDefaultAsync(s => s.Id == id);
}
```

**Optimizations**:
- `AsNoTracking()`: Read-only query (no change tracking overhead)
- `Include()`: Eager loading prevents N+1 queries
- `OrderBy` in Include: Questions sorted by display order
- Multiple includes: Loads all necessary related data

#### GetByIdWithDetailsAsync - Full Data Load

```csharp
// Lines 32-40
public async Task<Survey?> GetByIdWithDetailsAsync(int id)
{
    return await _dbSet
        .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
        .Include(s => s.Creator)
        .Include(s => s.Responses)
            .ThenInclude(r => r.Answers)
        .FirstOrDefaultAsync(s => s.Id == id);
}
```

**No AsNoTracking**: Allows modifications to loaded entities.

**ThenInclude**: Loads nested relationships (Answers within Responses).

**Use Case**: Statistics calculation, detailed analysis.

#### GetByCreatorIdAsync - User's Surveys

```csharp
// Lines 43-51
public async Task<IEnumerable<Survey>> GetByCreatorIdAsync(int creatorId)
{
    return await _dbSet
        .Include(s => s.Questions)
        .Include(s => s.Responses)
        .Where(s => s.CreatorId == creatorId)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();
}
```

**Pattern**: Newest surveys first (common UI requirement).

#### GetActiveSurveysAsync - Public Surveys

```csharp
// Lines 54-63
public async Task<IEnumerable<Survey>> GetActiveSurveysAsync()
{
    return await _dbSet
        .AsNoTracking()
        .Include(s => s.Questions)
        .Include(s => s.Creator)
        .Where(s => s.IsActive)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();
}
```

**Use Case**: Display available surveys to users (Telegram bot listing).

#### SearchByTitleAsync - PostgreSQL Full-Text Search

```csharp
// Lines 82-95
public async Task<IEnumerable<Survey>> SearchByTitleAsync(string searchTerm)
{
    if (string.IsNullOrWhiteSpace(searchTerm))
    {
        return await GetAllAsync();
    }

    return await _dbSet
        .Include(s => s.Creator)
        .Include(s => s.Questions)
        .Where(s => EF.Functions.ILike(s.Title, $"%{searchTerm}%"))
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();
}
```

**EF.Functions.ILike**: PostgreSQL-specific case-insensitive LIKE operator.

**Pattern**: `%searchTerm%` matches anywhere in title.

#### Survey Code Operations

**GetByCodeAsync** - Find by sharing code:

```csharp
// Lines 131-141
public async Task<Survey?> GetByCodeAsync(string code)
{
    if (string.IsNullOrWhiteSpace(code))
    {
        return null;
    }

    return await _dbSet
        .Include(s => s.Creator)
        .FirstOrDefaultAsync(s => s.Code == code.ToUpper());
}
```

**ToUpper()**: Codes stored and compared in uppercase for consistency.

**GetByCodeWithQuestionsAsync** - Code lookup with questions:

```csharp
// Lines 144-156
public async Task<Survey?> GetByCodeWithQuestionsAsync(string code)
{
    if (string.IsNullOrWhiteSpace(code))
    {
        return null;
    }

    return await _dbSet
        .AsNoTracking()
        .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
        .Include(s => s.Creator)
        .FirstOrDefaultAsync(s => s.Code == code.ToUpper());
}
```

**CodeExistsAsync** - Check code availability:

```csharp
// Lines 159-167
public async Task<bool> CodeExistsAsync(string code)
{
    if (string.IsNullOrWhiteSpace(code))
    {
        return false;
    }

    return await _dbSet.AnyAsync(s => s.Code == code.ToUpper());
}
```

**Use Case**: Code generation collision detection.

#### Overriding Base Methods

Always include Creator for surveys:

```csharp
// Lines 113-128
public override async Task<Survey?> GetByIdAsync(int id)
{
    return await _dbSet
        .Include(s => s.Creator)
        .FirstOrDefaultAsync(s => s.Id == id);
}

public override async Task<IEnumerable<Survey>> GetAllAsync()
{
    return await _dbSet
        .Include(s => s.Creator)
        .Include(s => s.Questions)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();
}
```

**Best Practice**: Override base methods to add necessary eager loading.

### Other Repositories

**QuestionRepository** (`Repositories/QuestionRepository.cs`, 148 lines):
- `GetBySurveyIdAsync`: Ordered questions for a survey
- `GetNextOrderIndexAsync`: Auto-increment order index
- `ReorderQuestionsAsync`: Bulk reordering with transaction
- `GetRequiredQuestionsBySurveyIdAsync`: Filter by required flag
- `GetByTypeAsync`: Filter by question type

**ResponseRepository** (`Repositories/ResponseRepository.cs`, 160 lines):
- `GetIncompleteResponseAsync`: Resume incomplete survey
- `HasUserCompletedSurveyAsync`: Check completion status
- `GetCompletedCountAsync`: Count completed responses
- `GetByDateRangeAsync`: Analytics date filtering
- `MarkAsCompleteAsync`: Update completion status

**UserRepository** (`Repositories/UserRepository.cs`, 146 lines):
- `GetByTelegramIdAsync`: Primary user lookup
- `CreateOrUpdateAsync`: Upsert pattern (critical for Telegram integration)
- `GetByUsernameAsync`: Username lookup
- `SearchByNameAsync`: Multi-field search
- `UpdateLastLoginAsync`: Track last login time

**AnswerRepository** (`Repositories/AnswerRepository.cs`, 134 lines):
- `GetByResponseIdAsync`: All answers for a response
- `GetByQuestionIdAsync`: All answers for a question
- `CreateBatchAsync`: Bulk answer creation
- `GetByResponseAndQuestionAsync`: Specific answer lookup
- `DeleteByResponseIdAsync`: Batch delete

---

## Service Layer

The service layer implements business logic, validation, authorization, and orchestrates repository operations.

### AuthService

**Location**: `Services/AuthService.cs` (Lines 1-174)

JWT token generation and authentication service.

#### LoginAsync - User Authentication

```csharp
// Lines 37-67
public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
{
    _logger.LogInformation("Login attempt for Telegram ID: {TelegramId}", request.TelegramId);

    // Find or create user using the CreateOrUpdateAsync method
    var user = await _userRepository.CreateOrUpdateAsync(
        request.TelegramId,
        request.Username,
        null, // firstName - not provided in login request
        null  // lastName - not provided in login request
    );

    _logger.LogInformation("User authenticated with ID: {UserId}", user.Id);

    // Generate tokens
    var accessToken = GenerateAccessToken(user.Id, user.TelegramId, user.Username);
    var refreshToken = GenerateRefreshToken();
    var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.TokenLifetimeHours);

    _logger.LogInformation("Successfully generated tokens for user ID: {UserId}", user.Id);

    return new LoginResponseDto
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresAt = expiresAt,
        UserId = user.Id,
        TelegramId = user.TelegramId,
        Username = user.Username
    };
}
```

**Key Features**:
- Upsert user (create if new, update if exists)
- No password validation (Telegram authentication handled externally)
- Generates JWT access token
- Generates cryptographically secure refresh token
- Returns complete authentication response

#### GenerateAccessToken - JWT Creation

```csharp
// Lines 72-100
public string GenerateAccessToken(int userId, long telegramId, string? username)
{
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim("TelegramId", telegramId.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
    };

    if (!string.IsNullOrWhiteSpace(username))
    {
        claims.Add(new Claim(ClaimTypes.Name, username));
        claims.Add(new Claim("Username", username));
    }

    var token = new JwtSecurityToken(
        issuer: _jwtSettings.Issuer,
        audience: _jwtSettings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(_jwtSettings.TokenLifetimeHours),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Claims Included**:
- `NameIdentifier`: Internal database user ID (for authorization)
- `TelegramId`: Telegram user ID (for Telegram operations)
- `Jti`: Token unique identifier
- `Iat`: Issued at timestamp
- `Name` / `Username`: User's Telegram username (if available)

**Signature**: HMAC-SHA256 with secret key from configuration.

#### GenerateRefreshToken - Secure Random

```csharp
// Lines 105-111
public string GenerateRefreshToken()
{
    var randomNumber = new byte[32];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(randomNumber);
    return Convert.ToBase64String(randomNumber);
}
```

**Security**: Uses `RandomNumberGenerator` (cryptographically secure).

**Note**: MVP implementation. Production should store refresh tokens in database with expiration.

#### ValidateToken - Token Verification

```csharp
// Lines 116-147
public bool ValidateToken(string token)
{
    if (string.IsNullOrWhiteSpace(token))
    {
        return false;
    }

    var tokenHandler = new JwtSecurityTokenHandler();
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

    try
    {
        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = securityKey,
            ClockSkew = TimeSpan.Zero
        }, out _);

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Token validation failed");
        return false;
    }
}
```

**Validation Checks**:
1. Issuer matches configuration
2. Audience matches configuration
3. Token not expired
4. Signature valid
5. Zero clock skew (strict expiration enforcement)

### SurveyService

**Location**: `Services/SurveyService.cs` (Lines 1-724)

Complete survey lifecycle management with business logic.

#### CreateSurveyAsync - Survey Creation

```csharp
// Lines 44-74
public async Task<SurveyDto> CreateSurveyAsync(int userId, CreateSurveyDto dto)
{
    _logger.LogInformation("Creating new survey for user {UserId}: {Title}", userId, dto.Title);

    // Validate the DTO
    ValidateCreateSurveyDto(dto);

    // Create survey entity
    var survey = _mapper.Map<Survey>(dto);
    survey.CreatorId = userId;
    survey.IsActive = false; // Always create as inactive

    // Generate unique survey code
    survey.Code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
        _surveyRepository.CodeExistsAsync);

    _logger.LogInformation("Generated unique code {Code} for survey", survey.Code);

    // Save to database
    var createdSurvey = await _surveyRepository.CreateAsync(survey);

    _logger.LogInformation("Survey {SurveyId} created successfully by user {UserId} with code {Code}",
        createdSurvey.Id, userId, createdSurvey.Code);

    // Map to DTO
    var result = _mapper.Map<SurveyDto>(createdSurvey);
    result.TotalResponses = 0;
    result.CompletedResponses = 0;

    return result;
}
```

**Business Rules**:
1. Validate input DTO
2. Map to domain entity
3. Set creator
4. **Create inactive by default** (safety - must explicitly activate)
5. Generate unique shareable code
6. Initialize response counts

#### UpdateSurveyAsync - Survey Modification

```csharp
// Lines 77-127
public async Task<SurveyDto> UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto)
{
    _logger.LogInformation("Updating survey {SurveyId} by user {UserId}", surveyId, userId);

    // Get survey with questions
    var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);
    if (survey == null)
    {
        _logger.LogWarning("Survey {SurveyId} not found", surveyId);
        throw new SurveyNotFoundException(surveyId);
    }

    // Check authorization
    if (survey.CreatorId != userId)
    {
        _logger.LogWarning("User {UserId} attempted to update survey {SurveyId} owned by {OwnerId}",
            userId, surveyId, survey.CreatorId);
        throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
    }

    // Check if survey can be modified
    if (survey.IsActive && await _surveyRepository.HasResponsesAsync(surveyId))
    {
        _logger.LogWarning("Cannot modify active survey {SurveyId} that has responses", surveyId);
        throw new SurveyOperationException(
            "Cannot modify an active survey that has responses. Deactivate the survey first or create a new version.");
    }

    // Update survey properties
    survey.Title = dto.Title;
    survey.Description = dto.Description;
    survey.AllowMultipleResponses = dto.AllowMultipleResponses;
    survey.ShowResults = dto.ShowResults;
    survey.UpdatedAt = DateTime.UtcNow;

    // Save changes
    await _surveyRepository.UpdateAsync(survey);

    // ... [rest of method]
}
```

**Business Rules**:
1. Survey must exist
2. User must own survey (authorization)
3. **Cannot modify active survey with responses** (data integrity protection)
4. Update allowed properties only
5. Timestamp automatically updated

#### DeleteSurveyAsync - Smart Delete Logic

```csharp
// Lines 130-171
public async Task<bool> DeleteSurveyAsync(int surveyId, int userId)
{
    // ... [authorization checks]

    // Check if survey has responses
    var hasResponses = await _surveyRepository.HasResponsesAsync(surveyId);

    if (hasResponses)
    {
        // Soft delete - just deactivate
        survey.IsActive = false;
        survey.UpdatedAt = DateTime.UtcNow;
        await _surveyRepository.UpdateAsync(survey);

        _logger.LogInformation("Survey {SurveyId} soft deleted (deactivated)", surveyId);
    }
    else
    {
        // Hard delete - survey has no responses
        await _surveyRepository.DeleteAsync(surveyId);

        _logger.LogInformation("Survey {SurveyId} hard deleted", surveyId);
    }

    return true;
}
```

**Smart Delete**:
- **Has responses**: Soft delete (deactivate only) - preserves valuable response data
- **No responses**: Hard delete (remove from database) - cleans up unused surveys

**Data Protection**: Prevents accidental deletion of surveys with collected responses.

#### ActivateSurveyAsync - Validation Before Activation

```csharp
// Lines 260-306
public async Task<SurveyDto> ActivateSurveyAsync(int surveyId, int userId)
{
    // ... [get survey and authorization]

    // Validate survey has at least one question
    if (survey.Questions == null || survey.Questions.Count == 0)
    {
        _logger.LogWarning("Cannot activate survey {SurveyId} with no questions", surveyId);
        throw new SurveyValidationException(
            "Cannot activate a survey with no questions. Please add at least one question before activating.");
    }

    // Activate survey
    survey.IsActive = true;
    survey.UpdatedAt = DateTime.UtcNow;

    await _surveyRepository.UpdateAsync(survey);

    // ... [return DTO]
}
```

**Validation**: Cannot activate empty survey. Must have at least one question.

#### GetSurveyStatisticsAsync - Comprehensive Analytics

```csharp
// Lines 350-410 (partial)
public async Task<SurveyStatisticsDto> GetSurveyStatisticsAsync(int surveyId, int userId)
{
    // ... [authorization]

    // Get all responses
    var allResponses = survey.Responses.ToList();
    var completedResponses = allResponses.Where(r => r.IsComplete).ToList();

    // Calculate basic statistics
    var statistics = new SurveyStatisticsDto
    {
        SurveyId = surveyId,
        Title = survey.Title,
        TotalResponses = allResponses.Count,
        CompletedResponses = completedResponses.Count,
        IncompleteResponses = allResponses.Count - completedResponses.Count,
        CompletionRate = allResponses.Count > 0
            ? Math.Round((double)completedResponses.Count / allResponses.Count * 100, 2)
            : 0,
        UniqueRespondents = allResponses.Select(r => r.RespondentTelegramId).Distinct().Count(),
        FirstResponseAt = allResponses.OrderBy(r => r.StartedAt).FirstOrDefault()?.StartedAt,
        LastResponseAt = allResponses.OrderByDescending(r => r.StartedAt).FirstOrDefault()?.StartedAt
    };

    // Calculate average completion time
    if (completedResponses.Any())
    {
        var completionTimes = completedResponses
            .Where(r => r.StartedAt.HasValue && r.SubmittedAt.HasValue)
            .Select(r => (r.SubmittedAt!.Value - r.StartedAt!.Value).TotalSeconds)
            .ToList();

        if (completionTimes.Any())
        {
            statistics.AverageCompletionTime = Math.Round(completionTimes.Average(), 2);
        }
    }

    // Calculate question-level statistics
    statistics.QuestionStatistics = await CalculateQuestionStatisticsAsync(
        survey.Questions.ToList(), completedResponses);

    return statistics;
}
```

**Metrics Calculated**:
- Total responses (all states)
- Completed vs incomplete responses
- Completion rate percentage
- Unique respondents count
- Average completion time (seconds)
- First and last response timestamps
- Per-question statistics (choice distribution, rating averages, etc.)

#### GetSurveyByCodeAsync - Public Survey Access

```csharp
// Lines 420-458
public async Task<SurveyDto> GetSurveyByCodeAsync(string code)
{
    _logger.LogInformation("Getting survey by code: {Code}", code);

    // Validate code format
    if (!SurveyCodeGenerator.IsValidCode(code))
    {
        _logger.LogWarning("Invalid survey code format: {Code}", code);
        throw new SurveyNotFoundException($"Survey with code '{code}' not found");
    }

    // Get survey with questions
    var survey = await _surveyRepository.GetByCodeWithQuestionsAsync(code);
    if (survey == null)
    {
        _logger.LogWarning("Survey with code {Code} not found", code);
        throw new SurveyNotFoundException($"Survey with code '{code}' not found");
    }

    // Only return active surveys for public access
    if (!survey.IsActive)
    {
        _logger.LogWarning("Survey {SurveyId} with code {Code} is not active", survey.Id, code);
        throw new SurveyNotFoundException($"Survey with code '{code}' is not available");
    }

    // ... [return DTO with counts]
}
```

**Security**: Only returns active surveys via code. Inactive surveys not publicly accessible.

**Use Case**: Telegram bot command `/survey ABC123`

### Other Services

**QuestionService** (`Services/QuestionService.cs`, 462 lines):
- `AddQuestionAsync`: Create question with type-specific validation
- `UpdateQuestionAsync`: Modify question (with protection rules)
- `DeleteQuestionAsync`: Remove question
- `ReorderQuestionsAsync`: Change question order
- `ValidateQuestionOptionsAsync`: Type-specific option validation

**ResponseService** (`Services/ResponseService.cs`, 623 lines):
- `StartResponseAsync`: Begin survey response
- `SaveAnswerAsync`: Save/update answer with validation
- `CompleteResponseAsync`: Finalize response
- `ValidateAnswerFormatAsync`: Answer format validation
- Answer validation methods for each question type

**UserService** (`Services/UserService.cs`, 289 lines):
- `RegisterAsync`: User registration with JWT token (upsert pattern)
- `GetUserByIdAsync`: Retrieve user details
- `GetUserByTelegramIdAsync`: Telegram ID lookup
- `UpdateUserAsync`: Update user information

---

## Database Migrations

Entity Framework Core migrations provide database schema version control.

### Existing Migrations

**Location**: `Migrations/` directory

1. **20251105190107_InitialCreate.cs**
   - Creates all tables (users, surveys, questions, responses, answers)
   - Sets up foreign key relationships
   - Adds indexes (single, composite, unique, partial, GIN)
   - Configures JSONB columns for PostgreSQL
   - Sets up cascade delete behaviors

2. **20251106000001_AddLastLoginAtToUser.cs**
   - Adds `last_login_at` column to `users` table
   - Nullable timestamp for tracking login activity
   - No default value (null until first login tracked)

3. **20251109000001_AddSurveyCodeColumn.cs**
   - Adds `code` column to `surveys` table
   - Max length 10 characters, nullable
   - Creates unique partial index: `idx_surveys_code` with filter `code IS NOT NULL`
   - Supports survey sharing feature

4. **SurveyBotDbContextModelSnapshot.cs**
   - Current complete model state
   - Used by EF Core for generating new migrations
   - Regenerated with each migration

### Migration Commands

**All commands run from API project directory**: `src/SurveyBot.API`

#### Create New Migration

```bash
dotnet ef migrations add MigrationName --project ../SurveyBot.Infrastructure
```

**Naming Convention**: Descriptive name (e.g., `AddSurveyCodeColumn`, not `Update1`)

#### Apply Migrations to Database

```bash
# Apply all pending migrations
dotnet ef database update --project ../SurveyBot.Infrastructure

# Apply to specific migration
dotnet ef database update MigrationName --project ../SurveyBot.Infrastructure

# Rollback to previous migration
dotnet ef database update PreviousMigrationName --project ../SurveyBot.Infrastructure
```

#### Remove Last Migration

```bash
# Only if NOT applied to database yet
dotnet ef migrations remove --project ../SurveyBot.Infrastructure
```

**Warning**: Cannot remove applied migrations. Must rollback first.

#### Generate SQL Script

```bash
# Generate script for all migrations
dotnet ef migrations script --project ../SurveyBot.Infrastructure --output migration.sql

# Generate script for specific range
dotnet ef migrations script FromMigration ToMigration --project ../SurveyBot.Infrastructure

# Generate idempotent script (safe to run multiple times)
dotnet ef migrations script --idempotent --project ../SurveyBot.Infrastructure
```

**Use Case**: Review SQL before applying to production.

### Migration Best Practices

1. **Always Backup**: Backup production database before migrations
2. **Test First**: Apply to development/staging environments first
3. **Review SQL**: Generate and review SQL script for production
4. **Never Edit Applied Migrations**: Create new migration instead
5. **Descriptive Names**: Use clear, specific migration names
6. **Small Migrations**: One logical change per migration
7. **Handle Data Migration**: Include data transformation in `Up()` method if needed
8. **Provide Rollback**: Implement `Down()` method for reverting changes
9. **Check Constraints**: Verify check constraints, indexes, and foreign keys
10. **Test Rollback**: Ensure `Down()` method works correctly

### Migration Workflow

**Development Workflow**:

1. Modify entity or configuration
2. Create migration: `dotnet ef migrations add DescriptiveName`
3. Review generated migration code
4. Apply to local database: `dotnet ef database update`
5. Test application with new schema
6. Commit migration files to source control

**Production Deployment**:

1. Generate SQL script: `dotnet ef migrations script --idempotent`
2. Review script for performance impact
3. Schedule maintenance window if needed
4. Backup production database
5. Apply script to production database
6. Verify application functionality
7. Monitor for issues

---

## Data Seeding

### DataSeeder

**Location**: `Data/DataSeeder.cs` (Lines 1-587)

Generates comprehensive test data for development and testing.

#### Main Seeding Method

```csharp
// Lines 22-53
public async Task SeedAsync()
{
    try
    {
        // Ensure database is created
        await _context.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await _context.Users.AnyAsync())
        {
            _logger.LogInformation("Database already contains data. Skipping seed.");
            return;
        }

        _logger.LogInformation("Starting database seeding...");

        // Seed in order due to foreign key relationships
        await SeedUsersAsync();
        await SeedSurveysAsync();
        await SeedResponsesAsync();

        _logger.LogInformation("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "An error occurred while seeding the database.");
        throw;
    }
}
```

**Safe**: Checks for existing data, doesn't re-seed if data present.

**Order**: Respects foreign key dependencies (Users → Surveys → Questions → Responses → Answers).

#### Sample Data Created

**Users**:
- John Doe (Telegram ID: 123456789)
- Jane Smith (Telegram ID: 987654321)
- Bob Wilson (Telegram ID: 555555555)

**Surveys**:

1. **Customer Satisfaction Survey** (Active)
   - Rating question: "How satisfied are you?" (1-5 scale)
   - Multiple choice: "Which services have you used?"
   - Single choice: "How did you hear about us?"
   - Text question: "Additional feedback"

2. **Product Feedback Survey** (Active, allows multiple responses)
   - Single choice: "Primary use case"
   - Rating question: "Ease of use rating"
   - Multiple choice: "Most valuable features"
   - Text question: "Feature requests"

3. **Event Registration Survey** (Inactive, for testing)
   - Text question: "Full name"
   - Single choice: "Track interest"
   - Multiple choice: "Workshop selection"
   - Rating question: "Expertise level"

**Responses**:
- Complete positive response (5-star rating)
- Complete mixed response (3-star rating)
- Complete power user response (multiple features selected)
- Complete business user response
- Incomplete response (for testing resume functionality)

#### Use Cases

1. **Development**: Local database with realistic data
2. **Testing**: Integration tests with known data
3. **Demos**: Showcase functionality with sample surveys
4. **UI Development**: Frontend development without API dependency

### DatabaseExtensions

**Location**: `Data/Extensions/DatabaseExtensions.cs` (Lines 1-61)

Extension methods for database operations.

#### SeedDatabaseAsync

```csharp
// Lines 17-25
public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

    var seeder = new DataSeeder(context, logger);
    await seeder.SeedAsync();
}
```

**Usage in Program.cs**:
```csharp
// Development environment only
if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDatabaseAsync();
}
```

#### MigrateDatabaseAsync

```csharp
// Lines 27-38
public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();

    await context.Database.MigrateAsync();
}
```

**Applies pending migrations**: Automatically brings database up to latest version.

#### ResetAndSeedDatabaseAsync

```csharp
// Lines 40-60
public static async Task ResetAndSeedDatabaseAsync(this IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

    // Drop and recreate
    await context.Database.EnsureDeletedAsync();
    await context.Database.MigrateAsync();

    // Seed
    var seeder = new DataSeeder(context, logger);
    await seeder.SeedAsync();
}
```

**DANGER**: Deletes all data. **Development only!**

**Use Case**: Reset local database to clean state with fresh test data.

---

## Dependency Injection

### DependencyInjection.cs

**Location**: `DependencyInjection.cs` (Lines 1-46)

Extension method registering all Infrastructure services.

```csharp
// Lines 22-44
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Add DbContext
    services.AddDbContext<SurveyBotDbContext>(options =>
        options.UseNpgsql(
            configuration.GetConnectionString("DefaultConnection"),
            npgsqlOptions => npgsqlOptions.MigrationsAssembly(
                typeof(SurveyBotDbContext).Assembly.FullName)));

    // Register Repositories
    services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
    services.AddScoped<ISurveyRepository, SurveyRepository>();
    services.AddScoped<IQuestionRepository, QuestionRepository>();
    services.AddScoped<IResponseRepository, ResponseRepository>();
    services.AddScoped<IAnswerRepository, AnswerRepository>();
    services.AddScoped<IUserRepository, UserRepository>();

    // Register Services
    services.AddScoped<ISurveyService, SurveyService>();
    services.AddScoped<IQuestionService, QuestionService>();
    services.AddScoped<IAuthService, AuthService>();

    return services;
}
```

**Usage in API Project** (`Program.cs`):

```csharp
builder.Services.AddInfrastructure(builder.Configuration);
```

**Service Lifetimes**:
- **DbContext**: Scoped (one instance per HTTP request)
- **Repositories**: Scoped (one instance per HTTP request)
- **Services**: Scoped (one instance per HTTP request)

**Why Scoped?**:
- DbContext is scoped by design
- Repositories depend on DbContext (must match lifetime)
- Services depend on repositories (must match lifetime)
- One instance per request ensures consistency within request
- Disposed at end of request (releases database connections)

### Connection String Configuration

**appsettings.json**:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=surveybot_db;Username=postgres;Password=your_password;Pooling=true;MinPoolSize=0;MaxPoolSize=100"
  }
}
```

**Connection String Parameters**:
- `Host`: PostgreSQL server hostname
- `Port`: PostgreSQL port (default: 5432)
- `Database`: Database name
- `Username`: PostgreSQL user
- `Password`: User password
- `Pooling=true`: Enable connection pooling
- `MinPoolSize=0`: Minimum connections in pool
- `MaxPoolSize=100`: Maximum connections in pool

**Environment-Specific Overrides**:

**appsettings.Development.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=surveybot_dev;..."
  }
}
```

**appsettings.Production.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=production-server;Port=5432;Database=surveybot_prod;..."
  }
}
```

**User Secrets** (Development):
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;..."
```

---

## Performance Optimization

### Index Strategy

**Implemented Indexes**:

1. **Primary Key Indexes** (automatic): All `Id` columns
2. **Foreign Key Indexes**: All foreign key columns (`creator_id`, `survey_id`, etc.)
3. **Unique Indexes**: `telegram_id`, `code` (with partial filter)
4. **Partial Indexes**: Filtered indexes on commonly queried subsets
   - `is_active = true` (active surveys)
   - `is_complete = true` (completed responses)
   - `username IS NOT NULL` (users with usernames)
5. **Composite Indexes**: Multi-column indexes for common query patterns
   - `(creator_id, is_active)` - User's active surveys
   - `(survey_id, order_index)` - Ordered questions
   - `(survey_id, respondent_telegram_id)` - Response lookup
   - `(response_id, question_id)` - Answer lookup (unique)
6. **GIN Indexes**: PostgreSQL JSONB columns
   - `options_json` on questions
   - `answer_json` on answers
7. **Descending Indexes**: For reverse ordering
   - `created_at DESC` - Newest-first sorting

**Index Guidelines**:
- Index foreign keys (used in joins)
- Index frequently filtered columns
- Use partial indexes for common WHERE clauses
- Composite indexes for multi-column queries
- Don't over-index (write performance impact)

### Query Optimization Techniques

#### 1. Eager Loading (Include)

**Prevent N+1 Queries**:

```csharp
// BAD: N+1 queries
var surveys = await _context.Surveys.ToListAsync();
foreach (var survey in surveys)
{
    // Lazy loading triggers query for each survey
    Console.WriteLine(survey.Creator.Username);
}

// GOOD: Single query with eager loading
var surveys = await _context.Surveys
    .Include(s => s.Creator)
    .ToListAsync();
```

#### 2. AsNoTracking for Read-Only Queries

**Performance Improvement**:

```csharp
// Tracked query (default)
var survey = await _context.Surveys
    .Include(s => s.Questions)
    .FirstOrDefaultAsync(s => s.Id == id);
// EF Core tracks all entities for change detection

// Read-only query (better performance)
var survey = await _context.Surveys
    .AsNoTracking()
    .Include(s => s.Questions)
    .FirstOrDefaultAsync(s => s.Id == id);
// No change tracking overhead
```

**Use When**: Displaying data, no modifications planned.

#### 3. Select Specific Columns

**Only retrieve needed data**:

```csharp
// BAD: Loads all columns
var surveys = await _context.Surveys.ToListAsync();

// GOOD: Only needed columns
var surveyTitles = await _context.Surveys
    .Select(s => new { s.Id, s.Title, s.CreatedAt })
    .ToListAsync();
```

#### 4. Pagination

**Avoid loading all records**:

```csharp
// BAD: Loads everything
var surveys = await _context.Surveys.ToListAsync();

// GOOD: Pagination
var page = 1;
var pageSize = 20;
var surveys = await _context.Surveys
    .OrderByDescending(s => s.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

#### 5. Filtered Queries

**Database-side filtering**:

```csharp
// BAD: Filter in memory
var allSurveys = await _context.Surveys.ToListAsync();
var activeSurveys = allSurveys.Where(s => s.IsActive).ToList();

// GOOD: Filter in database
var activeSurveys = await _context.Surveys
    .Where(s => s.IsActive)
    .ToListAsync();
```

#### 6. Compiled Queries

**For repeated queries**:

```csharp
private static readonly Func<SurveyBotDbContext, int, Task<Survey?>>
    GetSurveyByIdCompiled =
        EF.CompileAsyncQuery((SurveyBotDbContext context, int id) =>
            context.Surveys
                .Include(s => s.Questions)
                .FirstOrDefault(s => s.Id == id));

// Usage
var survey = await GetSurveyByIdCompiled(_context, surveyId);
```

**Benefit**: Query compilation cached, faster subsequent executions.

### Batch Operations

**Bulk Insert**:

```csharp
// BAD: Individual inserts
foreach (var answer in answers)
{
    await _context.Answers.AddAsync(answer);
    await _context.SaveChangesAsync(); // Multiple DB round trips
}

// GOOD: Batch insert
await _context.Answers.AddRangeAsync(answers);
await _context.SaveChangesAsync(); // Single DB round trip
```

**Bulk Delete**:

```csharp
var answersToDelete = await _context.Answers
    .Where(a => a.ResponseId == responseId)
    .ToListAsync();
_context.Answers.RemoveRange(answersToDelete);
await _context.SaveChangesAsync();
```

### Connection Pooling

**Configuration** (connection string):

```
Pooling=true;MinPoolSize=0;MaxPoolSize=100;Connection Lifetime=0
```

**Benefits**:
- Reuses connections (faster than creating new)
- Limits concurrent connections
- Automatic connection management

**Best Practices**:
- Enable pooling (default in Npgsql)
- Set reasonable max pool size
- Don't hold connections longer than needed
- Dispose DbContext promptly (automatic with DI)

---

## Best Practices and Patterns

### Repository Pattern Best Practices

1. **Use Virtual Methods**: Allow derived classes to override

```csharp
public virtual async Task<T?> GetByIdAsync(int id)
{
    return await _dbSet.FindAsync(id);
}
```

2. **All Operations Async**: Scalability and responsiveness

```csharp
// GOOD
public async Task<Survey> CreateAsync(Survey survey)

// BAD
public Survey Create(Survey survey)
```

3. **Include Related Data**: Prevent N+1 queries

```csharp
return await _dbSet
    .Include(s => s.Creator)
    .Include(s => s.Questions)
    .ToListAsync();
```

4. **Use AsNoTracking for Reads**: Performance optimization

```csharp
return await _dbSet
    .AsNoTracking()
    .Include(s => s.Questions)
    .FirstOrDefaultAsync(s => s.Id == id);
```

5. **Order Collections**: Consistent display

```csharp
.Include(s => s.Questions.OrderBy(q => q.OrderIndex))
```

### Service Pattern Best Practices

1. **Validate Before Operations**: Early failure

```csharp
if (string.IsNullOrWhiteSpace(dto.Title))
{
    throw new SurveyValidationException("Title is required");
}
```

2. **Check Authorization**: Every operation

```csharp
if (survey.CreatorId != userId)
{
    throw new UnauthorizedAccessException(userId, "Survey", surveyId);
}
```

3. **Use Logging**: Track operations and errors

```csharp
_logger.LogInformation("Survey {SurveyId} created by user {UserId}", surveyId, userId);
_logger.LogWarning("User {UserId} attempted unauthorized access", userId);
_logger.LogError(ex, "Failed to create survey");
```

4. **Throw Domain Exceptions**: Not generic exceptions

```csharp
// GOOD
throw new SurveyNotFoundException(surveyId);

// BAD
throw new Exception("Survey not found");
```

5. **Return DTOs, Not Entities**: Don't expose domain model

```csharp
// GOOD
return _mapper.Map<SurveyDto>(survey);

// BAD
return survey; // Exposes entity directly
```

### Entity Configuration Best Practices

1. **Prefer Fluent API**: Over data annotations

```csharp
// GOOD: Fluent API
builder.Property(s => s.Title)
    .HasColumnName("title")
    .HasMaxLength(500)
    .IsRequired();

// AVOID: Data annotations
[Column("title")]
[MaxLength(500)]
[Required]
public string Title { get; set; }
```

2. **Name Indexes**: Explicit naming

```csharp
builder.HasIndex(s => s.Code)
    .IsUnique()
    .HasDatabaseName("idx_surveys_code");
```

3. **Use Check Constraints**: Database-level validation

```csharp
builder.ToTable(t => t.HasCheckConstraint(
    "chk_order_index",
    "order_index >= 0"));
```

4. **Configure Cascade Delete**: Data integrity

```csharp
builder.HasMany(s => s.Questions)
    .WithOne(q => q.Survey)
    .HasForeignKey(q => q.SurveyId)
    .OnDelete(DeleteBehavior.Cascade);
```

5. **Use Partial Indexes**: Optimization

```csharp
builder.HasIndex(s => s.Code)
    .IsUnique()
    .HasFilter("code IS NOT NULL");
```

### Common Patterns

#### 1. Upsert Pattern

**CreateOrUpdateAsync** for user management:

```csharp
public async Task<User> CreateOrUpdateAsync(long telegramId, ...)
{
    var existingUser = await GetByTelegramIdAsync(telegramId);

    if (existingUser != null)
    {
        // Update existing
        existingUser.Username = username;
        existingUser.FirstName = firstName;
        existingUser.LastName = lastName;
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

**Use Case**: Telegram users can change username/names. Always current.

#### 2. Transaction Pattern

**Atomic operations**:

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();

try
{
    // Multiple operations
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

**Use Cases**: Question reordering, batch updates, complex operations.

#### 3. Validation Result Pattern

**Structured validation**:

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static ValidationResult Success() =>
        new() { IsValid = true };

    public static ValidationResult Failure(string error) =>
        new() { IsValid = false, ErrorMessage = error };
}

// Usage
var result = ValidateAnswer(answer);
if (!result.IsValid)
{
    throw new ValidationException(result.ErrorMessage);
}
```

#### 4. Smart Delete Pattern

**Soft vs hard delete**:

```csharp
var hasResponses = await HasResponsesAsync(surveyId);

if (hasResponses)
{
    // Soft delete - preserve data
    survey.IsActive = false;
    await UpdateAsync(survey);
}
else
{
    // Hard delete - clean up
    await DeleteAsync(surveyId);
}
```

**Use Cases**: Surveys, important data preservation.

---

## Error Handling

### Exception Types

Infrastructure throws domain exceptions from Core:

1. **SurveyNotFoundException**: Survey ID doesn't exist
2. **QuestionNotFoundException**: Question ID doesn't exist
3. **ResponseNotFoundException**: Response ID doesn't exist
4. **SurveyValidationException**: Survey validation fails
5. **QuestionValidationException**: Question validation fails
6. **InvalidAnswerFormatException**: Answer doesn't match question type
7. **UnauthorizedAccessException**: User doesn't own resource
8. **DuplicateResponseException**: Multiple responses not allowed
9. **SurveyOperationException**: Operation not permitted

### Exception Handling Pattern

**Service Layer**:

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

**Pattern**:
1. Catch specific EF Core exceptions
2. Log with context
3. Throw domain exception with user-friendly message
4. Include inner exception for debugging

### Logging Strategy

**Structured Logging with Serilog**:

```csharp
// Information - normal operation
_logger.LogInformation(
    "Survey {SurveyId} created by user {UserId} with code {Code}",
    survey.Id, userId, survey.Code);

// Warning - suspicious activity
_logger.LogWarning(
    "User {UserId} attempted to access survey {SurveyId} owned by {OwnerId}",
    userId, surveyId, survey.CreatorId);

// Error - operation failed
_logger.LogError(ex,
    "Failed to delete survey {SurveyId}",
    surveyId);
```

**Best Practices**:
- Use structured logging (named parameters)
- Log at appropriate levels
- Include context (IDs, usernames)
- Don't log sensitive data (passwords, tokens)
- Log exceptions with stack traces

---

## Testing Strategies

### Unit Testing Repositories

**In-Memory Database**:

```csharp
[Fact]
public async Task GetByIdAsync_ReturnsCorrectSurvey()
{
    // Arrange
    var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
        .UseInMemoryDatabase(databaseName: "TestDb")
        .Options;

    using var context = new SurveyBotDbContext(options);
    var repository = new SurveyRepository(context);

    var survey = new Survey { Id = 1, Title = "Test Survey", CreatorId = 1 };
    context.Surveys.Add(survey);
    await context.SaveChangesAsync();

    // Act
    var result = await repository.GetByIdAsync(1);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test Survey", result.Title);
}
```

**Note**: In-memory database doesn't support all PostgreSQL features (JSONB, GIN indexes).

### Unit Testing Services

**Mock Repositories**:

```csharp
[Fact]
public async Task CreateSurveyAsync_GeneratesUniqueCode()
{
    // Arrange
    var mockRepo = new Mock<ISurveyRepository>();
    mockRepo.Setup(r => r.CodeExistsAsync(It.IsAny<string>()))
        .ReturnsAsync(false); // Code is unique
    mockRepo.Setup(r => r.CreateAsync(It.IsAny<Survey>()))
        .ReturnsAsync((Survey s) => s);

    var mockMapper = new Mock<IMapper>();
    mockMapper.Setup(m => m.Map<Survey>(It.IsAny<CreateSurveyDto>()))
        .Returns(new Survey());
    mockMapper.Setup(m => m.Map<SurveyDto>(It.IsAny<Survey>()))
        .Returns(new SurveyDto());

    var service = new SurveyService(
        mockRepo.Object,
        Mock.Of<IResponseRepository>(),
        Mock.Of<IAnswerRepository>(),
        mockMapper.Object,
        Mock.Of<ILogger<SurveyService>>());

    // Act
    var result = await service.CreateSurveyAsync(1, new CreateSurveyDto
    {
        Title = "Test Survey",
        Description = "Test"
    });

    // Assert
    Assert.NotNull(result);
    mockRepo.Verify(r => r.CodeExistsAsync(It.IsAny<string>()), Times.Once);
    mockRepo.Verify(r => r.CreateAsync(It.IsAny<Survey>()), Times.Once);
}
```

### Integration Testing

**Test with Real Database**:

```csharp
public class SurveyIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SurveyIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateSurvey_ReturnsCreatedSurvey()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateSurveyDto
        {
            Title = "Integration Test Survey",
            Description = "Testing"
        };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/surveys", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var survey = JsonSerializer.Deserialize<SurveyDto>(responseBody);
        Assert.NotNull(survey);
        Assert.Equal("Integration Test Survey", survey.Title);
    }
}
```

### Test Database Setup

**Use separate test database**:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=surveybot_test;..."
  }
}
```

**Reset between tests**:

```csharp
public async Task ResetDatabaseAsync()
{
    await _context.Database.EnsureDeletedAsync();
    await _context.Database.MigrateAsync();
}
```

---

## Common Issues and Solutions

### Issue 1: DbContext Disposed Early

**Problem**: DbContext disposed before lazy-loaded navigation properties accessed.

**Solution**: Use eager loading with `Include()`:

```csharp
// BAD: Lazy loading after DbContext disposed
var survey = await _repository.GetByIdAsync(id);
// DbContext disposed here (end of scope)
foreach (var question in survey.Questions) // Exception!
{
    // ...
}

// GOOD: Eager loading
var survey = await _repository.GetByIdWithQuestionsAsync(id);
// All data loaded, DbContext can be disposed
foreach (var question in survey.Questions) // Works!
{
    // ...
}
```

### Issue 2: Circular Reference in JSON Serialization

**Problem**: Entity navigation properties cause infinite loops.

**Solution**: Return DTOs, not entities:

```csharp
// BAD: Return entity
return survey; // Circular: Survey → Questions → Survey → ...

// GOOD: Return DTO
return _mapper.Map<SurveyDto>(survey); // No circular references
```

**Alternative**: Configure JSON serializer to ignore cycles:

```csharp
services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
```

### Issue 3: N+1 Query Problem

**Problem**: Loading collection causes query for each item.

**Symptoms**: Slow performance, many database queries.

**Solution**: Use `Include()` for eager loading:

```csharp
// BAD: N+1 queries
var surveys = await _context.Surveys.ToListAsync(); // 1 query
foreach (var survey in surveys)
{
    Console.WriteLine(survey.Creator.Username); // N queries (one per survey)
}

// GOOD: Single query with eager loading
var surveys = await _context.Surveys
    .Include(s => s.Creator)
    .ToListAsync(); // Single query
```

### Issue 4: Concurrent Update Conflicts

**Problem**: Two users update same entity simultaneously, one overwrites other's changes.

**Solution**: Add row version for optimistic concurrency:

```csharp
// In entity
[Timestamp]
public byte[] RowVersion { get; set; }

// Or in configuration
builder.Property(e => e.RowVersion)
    .IsRowVersion();
```

**Handle DbUpdateConcurrencyException**:

```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    throw new SurveyOperationException("Survey was modified by another user", ex);
}
```

### Issue 5: JSONB Query Performance

**Problem**: Slow queries filtering on JSONB columns.

**Solution**: Create GIN index:

```csharp
builder.HasIndex(q => q.OptionsJson)
    .HasDatabaseName("idx_questions_options_json")
    .HasMethod("gin");
```

**Query JSONB**:

```csharp
// PostgreSQL JSON operators
.Where(q => EF.Functions.JsonContains(q.OptionsJson, "\"Option1\""))
```

### Issue 6: Migration Conflicts

**Problem**: Multiple developers create migrations simultaneously, conflicts.

**Solution**:

1. Pull latest code: `git pull`
2. Remove your migration: `dotnet ef migrations remove`
3. Update database: `dotnet ef database update`
4. Re-create migration: `dotnet ef migrations add YourMigrationName`
5. Verify no conflicts
6. Commit and push

### Issue 7: Connection Pool Exhaustion

**Problem**: "Connection pool exhausted" error.

**Causes**:
- DbContext not disposed
- Long-running operations
- Connection leaks

**Solutions**:

1. **Ensure DbContext disposed** (automatic with DI):

```csharp
// GOOD: DI handles disposal
public class SurveyService
{
    private readonly SurveyBotDbContext _context;
    // Context automatically disposed at end of request
}
```

2. **Increase pool size** (connection string):

```
MaxPoolSize=200
```

3. **Use async operations** (don't block threads):

```csharp
// GOOD
await _context.SaveChangesAsync();

// BAD
_context.SaveChanges(); // Blocks thread
```

### Issue 8: Slow Database Queries

**Problem**: Queries taking too long.

**Diagnosis**:

1. Enable query logging:

```csharp
optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
```

2. Use query execution plans (PostgreSQL):

```sql
EXPLAIN ANALYZE SELECT * FROM surveys WHERE is_active = true;
```

**Solutions**:

1. **Add missing indexes** (check WHERE clauses)
2. **Use AsNoTracking()** for read-only queries
3. **Implement pagination** (limit result sets)
4. **Use compiled queries** for repeated queries
5. **Optimize includes** (only load needed data)

### Issue 9: DateTime Timezone Issues

**Problem**: DateTime values lose timezone information.

**Solution**: Always use UTC:

```csharp
// GOOD
entity.CreatedAt = DateTime.UtcNow;

// BAD
entity.CreatedAt = DateTime.Now; // Local time, loses timezone
```

**Database Configuration**:

```csharp
builder.Property(u => u.CreatedAt)
    .HasColumnType("timestamp with time zone"); // PostgreSQL
```

### Issue 10: EF Core Tracking Issues

**Problem**: Changes not saved or unexpected behavior.

**Diagnosis**:

```csharp
var entries = _context.ChangeTracker.Entries();
foreach (var entry in entries)
{
    Console.WriteLine($"{entry.Entity.GetType().Name}: {entry.State}");
}
```

**Solution**: Understand entity states:

- **Added**: New entity, will be inserted
- **Modified**: Existing entity changed, will be updated
- **Deleted**: Entity removed, will be deleted
- **Unchanged**: No changes, no operation
- **Detached**: Not tracked by context

**Fix tracking issues**:

```csharp
// Attach detached entity
_context.Attach(entity);

// Mark as modified
_context.Entry(entity).State = EntityState.Modified;

// Stop tracking
_context.Entry(entity).State = EntityState.Detached;
```

---

## Summary

**SurveyBot.Infrastructure** is a well-architected data access layer implementing:

**Core Features**:
- Clean Architecture compliance (depends only on Core)
- Repository pattern for data access abstraction
- Service layer with comprehensive business logic
- Fluent API entity configurations
- PostgreSQL-specific optimizations (JSONB, GIN indexes, partial indexes)
- Automatic timestamp management
- Smart delete logic (soft vs hard)
- Survey code generation and validation
- Comprehensive authorization checks
- EF Core migrations for schema version control

**Performance**:
- Eager loading prevents N+1 queries
- AsNoTracking for read-only queries
- Composite indexes for common query patterns
- Partial indexes for filtered queries
- Connection pooling
- Batch operations

**Data Integrity**:
- Foreign key relationships with cascade delete
- Unique constraints
- Check constraints (database-level validation)
- Transaction support for atomic operations
- Optimistic concurrency (via row version if implemented)

**Key Files**:
- `SurveyBotDbContext.cs` - 109 lines
- `SurveyService.cs` - 724 lines
- `ResponseService.cs` - 623 lines
- `QuestionService.cs` - 462 lines
- `AuthService.cs` - 174 lines
- `SurveyRepository.cs` - 169 lines
- `DataSeeder.cs` - 587 lines

**Total**: ~3,500+ lines of production code implementing robust data access layer with comprehensive business logic.

---

**Last Updated**: 2025-11-12
**Maintained By**: SurveyBot Development Team
**For Questions**: Refer to main CLAUDE.md or project documentation
