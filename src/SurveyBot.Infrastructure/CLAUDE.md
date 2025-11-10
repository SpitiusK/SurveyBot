# SurveyBot.Infrastructure - Data Access Layer Documentation

## Table of Contents

1. [Project Overview](#project-overview)
2. [Project Structure](#project-structure)
3. [Dependencies and Packages](#dependencies-and-packages)
4. [Database Context](#database-context)
5. [Entity Configurations](#entity-configurations)
6. [Repository Layer](#repository-layer)
7. [Service Layer](#service-layer)
8. [Data Seeding](#data-seeding)
9. [Migrations](#migrations)
10. [Dependency Injection](#dependency-injection)
11. [Key Features](#key-features)
12. [Best Practices](#best-practices)
13. [Common Patterns](#common-patterns)
14. [Performance Optimization](#performance-optimization)
15. [Error Handling](#error-handling)
16. [Testing Approaches](#testing-approaches)
17. [Common Issues and Solutions](#common-issues-and-solutions)
18. [Code Examples](#code-examples)

---

## Project Overview

### Purpose

**SurveyBot.Infrastructure** is the data access layer of the SurveyBot application. It implements:

- Entity Framework Core DbContext for database operations
- Repository pattern for data access abstraction
- Business logic services implementing Core interfaces
- Database migrations and schema management
- Data seeding for development and testing
- PostgreSQL-specific optimizations (JSONB, GIN indexes)

### Responsibilities

1. **Database Access**: All database operations via Entity Framework Core
2. **Repository Implementation**: Concrete implementations of `IRepository<T>` interfaces from Core
3. **Service Implementation**: Business logic services implementing Core service interfaces
4. **Data Persistence**: Transaction management and change tracking
5. **Database Schema**: Entity configurations using Fluent API
6. **Migrations**: Database schema versioning and evolution
7. **Data Seeding**: Test data generation for development

### Architecture Principles

- **Depends only on Core**: No dependencies on API or Bot layers
- **Repository Pattern**: Abstracts data access from business logic
- **Service Layer**: Business logic separate from data access
- **Single Responsibility**: Each repository/service has one clear purpose
- **Dependency Injection**: All dependencies injected via constructor

---

## Project Structure

### Complete File Tree

```
SurveyBot.Infrastructure/
├── Data/
│   ├── SurveyBotDbContext.cs                 # Main DbContext (108 lines)
│   ├── DataSeeder.cs                         # Test data seeding (587 lines)
│   ├── Configurations/                       # Fluent API configurations
│   │   ├── UserConfiguration.cs              # User entity config (73 lines)
│   │   ├── SurveyConfiguration.cs            # Survey entity config (117 lines)
│   │   ├── QuestionConfiguration.cs          # Question entity config (107 lines)
│   │   ├── ResponseConfiguration.cs          # Response entity config (78 lines)
│   │   └── AnswerConfiguration.cs            # Answer entity config (89 lines)
│   └── Extensions/
│       └── DatabaseExtensions.cs             # Seeding extension methods (61 lines)
├── Repositories/                             # Repository implementations
│   ├── GenericRepository.cs                  # Base repository (95 lines)
│   ├── SurveyRepository.cs                   # Survey-specific queries (169 lines)
│   ├── QuestionRepository.cs                 # Question-specific queries (148 lines)
│   ├── ResponseRepository.cs                 # Response-specific queries (160 lines)
│   ├── UserRepository.cs                     # User-specific queries (146 lines)
│   └── AnswerRepository.cs                   # Answer-specific queries (134 lines)
├── Services/                                 # Business logic services
│   ├── AuthService.cs                        # JWT authentication (174 lines)
│   ├── SurveyService.cs                      # Survey business logic (724 lines)
│   ├── QuestionService.cs                    # Question business logic (462 lines)
│   ├── ResponseService.cs                    # Response business logic (623 lines)
│   └── UserService.cs                        # User management (289 lines)
├── Migrations/                               # EF Core migrations
│   ├── 20251105190107_InitialCreate.cs
│   ├── 20251106000001_AddLastLoginAtToUser.cs
│   ├── 20251109000001_AddSurveyCodeColumn.cs
│   └── SurveyBotDbContextModelSnapshot.cs
├── DependencyInjection.cs                    # DI registration (46 lines)
└── SurveyBot.Infrastructure.csproj           # Project file
```

### Organization by Purpose

**Data Layer** (`Data/`):
- DbContext and database configuration
- Entity type configurations
- Data seeding utilities

**Repository Layer** (`Repositories/`):
- Generic base repository
- Entity-specific repositories with custom queries
- Eager loading and query optimization

**Service Layer** (`Services/`):
- Business logic implementation
- Validation and authorization
- DTO mapping and transformation

**Migrations** (`Migrations/`):
- Database schema versions
- Migration scripts
- Model snapshots

---

## Dependencies and Packages

### Project File: `SurveyBot.Infrastructure.csproj`

```xml
<!-- Lines 1-26 -->
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\SurveyBot.Core\SurveyBot.Core.csproj" />
  </ItemGroup>

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

### Key Dependencies

1. **SurveyBot.Core** - Domain entities and interfaces
2. **Entity Framework Core 9.0.10** - ORM framework
3. **Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4** - PostgreSQL provider
4. **AutoMapper 12.0.1** - Object-to-object mapping
5. **System.IdentityModel.Tokens.Jwt 7.1.2** - JWT token generation
6. **Microsoft.Extensions.Options 9.0.10** - Configuration options

---

## Database Context

### SurveyBotDbContext

**Location**: `Data/SurveyBotDbContext.cs` (Lines 1-109)

The central database context managing all entity sets and database operations.

#### Class Declaration and Constructor

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

All entity DbSets exposed for querying:

```csharp
// Lines 18-41
/// <summary>
/// Gets or sets the Users DbSet.
/// </summary>
public DbSet<User> Users { get; set; } = null!;

/// <summary>
/// Gets or sets the Surveys DbSet.
/// </summary>
public DbSet<Survey> Surveys { get; set; } = null!;

/// <summary>
/// Gets or sets the Questions DbSet.
/// </summary>
public DbSet<Question> Questions { get; set; } = null!;

/// <summary>
/// Gets or sets the Responses DbSet.
/// </summary>
public DbSet<Response> Responses { get; set; } = null!;

/// <summary>
/// Gets or sets the Answers DbSet.
/// </summary>
public DbSet<Answer> Answers { get; set; } = null!;
```

#### OnModelCreating - Entity Configuration

Applies all Fluent API configurations:

```csharp
// Lines 43-56
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Apply all entity configurations
    modelBuilder.ApplyConfiguration(new UserConfiguration());
    modelBuilder.ApplyConfiguration(new SurveyConfiguration());
    modelBuilder.ApplyConfiguration(new QuestionConfiguration());
    modelBuilder.ApplyConfiguration(new ResponseConfiguration());
    modelBuilder.ApplyConfiguration(new AnswerConfiguration());

    // Configure automatic timestamp updates for entities with UpdatedAt
    // This will be handled by SaveChangesAsync override
}
```

#### OnConfiguring - Development Settings

Enables detailed logging for development:

```csharp
// Lines 58-68
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

#### SaveChangesAsync Override - Automatic Timestamps

**Critical Feature**: Automatically manages `CreatedAt` and `UpdatedAt` timestamps:

```csharp
// Lines 70-107
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // Update timestamps for entities that have UpdatedAt property
    var entries = ChangeTracker.Entries()
        .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

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

**Key Points**:
- Automatically sets `CreatedAt` on entity creation (EntityState.Added)
- Automatically updates `UpdatedAt` on entity modification (EntityState.Modified)
- Works for entities inheriting from `BaseEntity`
- Uses reflection for non-BaseEntity classes with CreatedAt property
- All timestamps are in UTC

---

## Entity Configurations

All entity configurations use Fluent API pattern implementing `IEntityTypeConfiguration<T>`.

### UserConfiguration

**Location**: `Data/Configurations/UserConfiguration.cs` (Lines 1-73)

Configures the User entity mapping to the `users` table.

#### Complete Configuration

```csharp
// Lines 12-71
public void Configure(EntityTypeBuilder<User> builder)
{
    // Table name
    builder.ToTable("users");

    // Primary key
    builder.HasKey(u => u.Id);
    builder.Property(u => u.Id)
        .HasColumnName("id")
        .ValueGeneratedOnAdd();

    // TelegramId - unique identifier from Telegram
    builder.Property(u => u.TelegramId)
        .HasColumnName("telegram_id")
        .IsRequired();

    builder.HasIndex(u => u.TelegramId)
        .IsUnique()
        .HasDatabaseName("idx_users_telegram_id");

    // Username
    builder.Property(u => u.Username)
        .HasColumnName("username")
        .HasMaxLength(255);

    builder.HasIndex(u => u.Username)
        .HasDatabaseName("idx_users_username")
        .HasFilter("username IS NOT NULL");

    // FirstName
    builder.Property(u => u.FirstName)
        .HasColumnName("first_name")
        .HasMaxLength(255);

    // LastName
    builder.Property(u => u.LastName)
        .HasColumnName("last_name")
        .HasMaxLength(255);

    // CreatedAt
    builder.Property(u => u.CreatedAt)
        .HasColumnName("created_at")
        .HasColumnType("timestamp with time zone")
        .IsRequired()
        .HasDefaultValueSql("CURRENT_TIMESTAMP");

    // UpdatedAt
    builder.Property(u => u.UpdatedAt)
        .HasColumnName("updated_at")
        .HasColumnType("timestamp with time zone")
        .IsRequired()
        .HasDefaultValueSql("CURRENT_TIMESTAMP");

    // Relationships
    builder.HasMany(u => u.Surveys)
        .WithOne(s => s.Creator)
        .HasForeignKey(s => s.CreatorId)
        .OnDelete(DeleteBehavior.Cascade)
        .HasConstraintName("fk_surveys_creator");
}
```

**Key Features**:
- Unique index on `TelegramId` (prevents duplicate users)
- Partial index on `Username` (only when not null)
- Cascade delete: deleting user deletes all their surveys
- PostgreSQL timestamp with time zone
- Snake_case column naming

### SurveyConfiguration

**Location**: `Data/Configurations/SurveyConfiguration.cs` (Lines 1-117)

Configures the Survey entity with advanced indexing and relationships.

#### Key Sections

**Primary Key and Basic Properties**:

```csharp
// Lines 14-32
builder.ToTable("surveys");

// Primary key
builder.HasKey(s => s.Id);
builder.Property(s => s.Id)
    .HasColumnName("id")
    .ValueGeneratedOnAdd();

// Title
builder.Property(s => s.Title)
    .HasColumnName("title")
    .HasMaxLength(500)
    .IsRequired();

// Description
builder.Property(s => s.Description)
    .HasColumnName("description")
    .HasColumnType("text");
```

**Survey Code (for sharing)**:

```csharp
// Lines 34-42
// Code - unique survey code for sharing
builder.Property(s => s.Code)
    .HasColumnName("code")
    .HasMaxLength(10);

builder.HasIndex(s => s.Code)
    .IsUnique()
    .HasDatabaseName("idx_surveys_code")
    .HasFilter("code IS NOT NULL");
```

**Boolean Flags with Defaults**:

```csharp
// Lines 52-72
// IsActive
builder.Property(s => s.IsActive)
    .HasColumnName("is_active")
    .IsRequired()
    .HasDefaultValue(true);

builder.HasIndex(s => s.IsActive)
    .HasDatabaseName("idx_surveys_is_active")
    .HasFilter("is_active = true");

// AllowMultipleResponses
builder.Property(s => s.AllowMultipleResponses)
    .HasColumnName("allow_multiple_responses")
    .IsRequired()
    .HasDefaultValue(false);

// ShowResults
builder.Property(s => s.ShowResults)
    .HasColumnName("show_results")
    .IsRequired()
    .HasDefaultValue(true);
```

**Performance Indexes**:

```csharp
// Lines 88-95
// Composite index for common query pattern (creator + active status)
builder.HasIndex(s => new { s.CreatorId, s.IsActive })
    .HasDatabaseName("idx_surveys_creator_active");

// Index for sorting by creation date
builder.HasIndex(s => s.CreatedAt)
    .HasDatabaseName("idx_surveys_created_at")
    .IsDescending();
```

**Relationships**:

```csharp
// Lines 97-114
// Relationships
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

**Key Features**:
- Unique partial index on survey code (only when not null)
- Composite index for filtering by creator and active status
- Descending index on created_at for newest-first queries
- Cascade delete for questions and responses
- Filtered index on active surveys only

### QuestionConfiguration

**Location**: `Data/Configurations/QuestionConfiguration.cs` (Lines 1-107)

Configures Question entity with JSONB storage and check constraints.

#### JSONB Column and GIN Index

**Critical Feature**: PostgreSQL JSONB for flexible option storage:

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

**Why JSONB?**
- Flexible storage for different option types (text arrays, rating configs)
- Fast searching with GIN indexes
- Binary format for performance
- PostgreSQL-specific optimization

#### Check Constraints

**Data Integrity**: Database-level constraints:

```csharp
// Lines 46-49
// Check constraint for question type
builder.ToTable(t => t.HasCheckConstraint(
    "chk_question_type",
    "question_type IN ('text', 'multiple_choice', 'single_choice', 'rating', 'yes_no')"));

// Lines 65-68
// Check constraint for order index
builder.ToTable(t => t.HasCheckConstraint(
    "chk_order_index",
    "order_index >= 0"));
```

#### Unique Composite Index

**Prevents Duplicate Order**:

```csharp
// Lines 56-63
// Composite index for ordered question retrieval
builder.HasIndex(q => new { q.SurveyId, q.OrderIndex })
    .HasDatabaseName("idx_questions_survey_order");

// Unique constraint for survey_id + order_index
builder.HasIndex(q => new { q.SurveyId, q.OrderIndex })
    .IsUnique()
    .HasDatabaseName("idx_questions_survey_order_unique");
```

**Key Features**:
- JSONB storage with GIN index for fast querying
- Unique constraint on (SurveyId, OrderIndex) prevents duplicate ordering
- Check constraints enforce valid question types and order values
- Cascade delete for answers

### ResponseConfiguration

**Location**: `Data/Configurations/ResponseConfiguration.cs` (Lines 1-78)

Configures Response entity with completion tracking.

#### Key Features

**Respondent Telegram ID** (NOT a foreign key):

```csharp
// Lines 31-38
// RespondentTelegramId - NOT a foreign key to allow anonymous responses
builder.Property(r => r.RespondentTelegramId)
    .HasColumnName("respondent_telegram_id")
    .IsRequired();

// Composite index for survey + respondent (for duplicate checking)
builder.HasIndex(r => new { r.SurveyId, r.RespondentTelegramId })
    .HasDatabaseName("idx_responses_survey_respondent");
```

**Why not a foreign key?**
- Allows responses from users not in the system (anonymous)
- Telegram ID sufficient for tracking without user table entry
- More flexible for public surveys

**Completion Tracking**:

```csharp
// Lines 40-48
// IsComplete
builder.Property(r => r.IsComplete)
    .HasColumnName("is_complete")
    .IsRequired()
    .HasDefaultValue(false);

builder.HasIndex(r => r.IsComplete)
    .HasDatabaseName("idx_responses_complete")
    .HasFilter("is_complete = true");
```

**Filtered indexes** optimize queries for completed responses only.

**Timestamp Indexes**:

```csharp
// Lines 55-62
// SubmittedAt
builder.Property(r => r.SubmittedAt)
    .HasColumnName("submitted_at")
    .HasColumnType("timestamp with time zone");

builder.HasIndex(r => r.SubmittedAt)
    .HasDatabaseName("idx_responses_submitted_at")
    .HasFilter("submitted_at IS NOT NULL");
```

### AnswerConfiguration

**Location**: `Data/Configurations/AnswerConfiguration.cs` (Lines 1-89)

Configures Answer entity with JSONB storage and check constraints.

#### JSONB Answer Storage

```csharp
// Lines 53-61
// AnswerJson - stored as JSONB in PostgreSQL
builder.Property(a => a.AnswerJson)
    .HasColumnName("answer_json")
    .HasColumnType("jsonb");

// GIN index for JSONB answer searching
builder.HasIndex(a => a.AnswerJson)
    .HasDatabaseName("idx_answers_answer_json")
    .HasMethod("gin");
```

#### Data Validation Constraint

**Ensures answer data exists**:

```csharp
// Lines 63-66
// Check constraint: at least one of answer_text or answer_json must be present
builder.ToTable(t => t.HasCheckConstraint(
    "chk_answer_not_null",
    "answer_text IS NOT NULL OR answer_json IS NOT NULL"));
```

#### Unique Answer Constraint

**Prevents duplicate answers**:

```csharp
// Lines 43-46
// Unique constraint: one answer per question per response
builder.HasIndex(a => new { a.ResponseId, a.QuestionId })
    .IsUnique()
    .HasDatabaseName("idx_answers_response_question_unique");
```

**Key Features**:
- JSONB for structured answer data (choices, ratings)
- Check constraint ensures at least one answer field populated
- Unique constraint prevents duplicate answers
- GIN index for fast JSONB querying

---

## Repository Layer

### GenericRepository<T>

**Location**: `Repositories/GenericRepository.cs` (Lines 1-95)

Base repository providing common CRUD operations for all entities.

#### Class Declaration

```csharp
// Lines 11-24
public class GenericRepository<T> : IRepository<T> where T : class
{
    protected readonly SurveyBotDbContext _context;
    protected readonly DbSet<T> _dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericRepository{T}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public GenericRepository(SurveyBotDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<T>();
    }
```

**Pattern**: Protected fields allow derived repositories to access context and DbSet.

#### GetByIdAsync

```csharp
// Lines 26-30
/// <inheritdoc />
public virtual async Task<T?> GetByIdAsync(int id)
{
    return await _dbSet.FindAsync(id);
}
```

**Note**: Uses `FindAsync` which checks local cache first, then queries database.

#### GetAllAsync

```csharp
// Lines 32-36
/// <inheritdoc />
public virtual async Task<IEnumerable<T>> GetAllAsync()
{
    return await _dbSet.ToListAsync();
}
```

**Warning**: Returns all records. Override in derived classes for filtering/ordering.

#### CreateAsync

```csharp
// Lines 38-50
/// <inheritdoc />
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

**Note**: Calls `SaveChangesAsync` which triggers timestamp updates.

#### UpdateAsync

```csharp
// Lines 52-64
/// <inheritdoc />
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

#### DeleteAsync

```csharp
// Lines 66-80
/// <inheritdoc />
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

**Pattern**: Returns `false` if entity not found, `true` on successful delete.

#### ExistsAsync and CountAsync

```csharp
// Lines 82-93
/// <inheritdoc />
public virtual async Task<bool> ExistsAsync(int id)
{
    var entity = await _dbSet.FindAsync(id);
    return entity != null;
}

/// <inheritdoc />
public virtual async Task<int> CountAsync()
{
    return await _dbSet.CountAsync();
}
```

**Key Points**:
- All methods are `virtual` for overriding in derived classes
- All operations are async
- SaveChangesAsync called after modifications
- Null checks on input parameters
- Returns nullable types for queries

### SurveyRepository

**Location**: `Repositories/SurveyRepository.cs` (Lines 1-169)

Survey-specific repository with complex queries and eager loading.

#### Constructor

```csharp
// Lines 11-19
public class SurveyRepository : GenericRepository<Survey>, ISurveyRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SurveyRepository(SurveyBotDbContext context) : base(context)
    {
    }
```

#### GetByIdWithQuestionsAsync

**Eager loads questions ordered by index**:

```csharp
// Lines 21-29
/// <inheritdoc />
public async Task<Survey?> GetByIdWithQuestionsAsync(int id)
{
    return await _dbSet
        .AsNoTracking()
        .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
        .Include(s => s.Creator)
        .FirstOrDefaultAsync(s => s.Id == id);
}
```

**Key Features**:
- `AsNoTracking()` for read-only queries (better performance)
- `Include()` for eager loading (prevents N+1 queries)
- `OrderBy` in Include for sorted questions
- Multiple includes for complete data

#### GetByIdWithDetailsAsync

**Loads everything including responses and answers**:

```csharp
// Lines 31-40
/// <inheritdoc />
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

**Note**: No `AsNoTracking()` here because we might modify loaded entities.

**ThenInclude**: Loads nested relationships (Answers within Responses).

#### GetByCreatorIdAsync

**Gets all surveys for a user**:

```csharp
// Lines 42-51
/// <inheritdoc />
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

**Pattern**: Newest surveys first with full data.

#### GetActiveSurveysAsync

**Public surveys available for responses**:

```csharp
// Lines 53-63
/// <inheritdoc />
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

**Use Case**: Display available surveys to users.

#### SearchByTitleAsync

**PostgreSQL case-insensitive search**:

```csharp
// Lines 82-95
/// <inheritdoc />
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

**EF.Functions.ILike**: PostgreSQL-specific case-insensitive LIKE.

#### Survey Code Methods

**New Feature**: Survey code generation and retrieval:

```csharp
// Lines 131-141
/// <inheritdoc />
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

// Lines 143-156
/// <inheritdoc />
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

// Lines 158-167
/// <inheritdoc />
public async Task<bool> CodeExistsAsync(string code)
{
    if (string.IsNullOrWhiteSpace(code))
    {
        return false;
    }

    return await _dbSet.AnyAsync(s => s.Code == code.ToUpper());
}
```

**Pattern**: Codes stored and compared in uppercase for consistency.

#### Override Base Methods

**Always include Creator**:

```csharp
// Lines 113-118
/// <inheritdoc />
public override async Task<Survey?> GetByIdAsync(int id)
{
    return await _dbSet
        .Include(s => s.Creator)
        .FirstOrDefaultAsync(s => s.Id == id);
}

// Lines 120-128
/// <inheritdoc />
public override async Task<IEnumerable<Survey>> GetAllAsync()
{
    return await _dbSet
        .Include(s => s.Creator)
        .Include(s => s.Questions)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();
}
```

**Best Practice**: Override base methods to add necessary includes.

### QuestionRepository

**Location**: `Repositories/QuestionRepository.cs` (Lines 1-148)

Question-specific repository with ordering and bulk operations.

#### GetBySurveyIdAsync

**Ordered question list**:

```csharp
// Lines 21-28
/// <inheritdoc />
public async Task<IEnumerable<Question>> GetBySurveyIdAsync(int surveyId)
{
    return await _dbSet
        .Where(q => q.SurveyId == surveyId)
        .OrderBy(q => q.OrderIndex)
        .ToListAsync();
}
```

**Critical**: Always ordered by OrderIndex for correct display.

#### GetNextOrderIndexAsync

**Auto-increment order index**:

```csharp
// Lines 77-85
/// <inheritdoc />
public async Task<int> GetNextOrderIndexAsync(int surveyId)
{
    var maxOrderIndex = await _dbSet
        .Where(q => q.SurveyId == surveyId)
        .MaxAsync(q => (int?)q.OrderIndex);

    return (maxOrderIndex ?? -1) + 1;
}
```

**Pattern**:
- Cast to `int?` to handle empty collections
- Returns 0 for first question (max = null → -1 + 1 = 0)

#### ReorderQuestionsAsync

**Bulk reordering with transaction**:

```csharp
// Lines 40-75
/// <inheritdoc />
public async Task<bool> ReorderQuestionsAsync(Dictionary<int, int> questionOrders)
{
    if (questionOrders == null || questionOrders.Count == 0)
    {
        return false;
    }

    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        foreach (var (questionId, newOrderIndex) in questionOrders)
        {
            var question = await GetByIdAsync(questionId);

            if (question == null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            question.OrderIndex = newOrderIndex;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**Key Features**:
- Explicit transaction for atomicity
- Rollback if any question not found
- All-or-nothing update
- Exception propagation after rollback

#### Filtering Methods

```csharp
// Lines 87-94
/// <inheritdoc />
public async Task<IEnumerable<Question>> GetRequiredQuestionsBySurveyIdAsync(int surveyId)
{
    return await _dbSet
        .Where(q => q.SurveyId == surveyId && q.IsRequired)
        .OrderBy(q => q.OrderIndex)
        .ToListAsync();
}

// Lines 96-103
/// <inheritdoc />
public async Task<IEnumerable<Question>> GetByTypeAsync(int surveyId, QuestionType questionType)
{
    return await _dbSet
        .Where(q => q.SurveyId == surveyId && q.QuestionType == questionType)
        .OrderBy(q => q.OrderIndex)
        .ToListAsync();
}
```

### ResponseRepository

**Location**: `Repositories/ResponseRepository.cs` (Lines 1-160)

Response-specific repository with completion and filtering.

#### GetIncompleteResponseAsync

**Resume incomplete survey**:

```csharp
// Lines 62-74
/// <inheritdoc />
public async Task<Response?> GetIncompleteResponseAsync(int surveyId, long telegramId)
{
    return await _dbSet
        .AsNoTracking()
        .Include(r => r.Answers)
            .ThenInclude(a => a.Question)
        .Include(r => r.Survey)
            .ThenInclude(s => s.Questions.OrderBy(q => q.OrderIndex))
        .FirstOrDefaultAsync(r => r.SurveyId == surveyId
            && r.RespondentTelegramId == telegramId
            && !r.IsComplete);
}
```

**Use Case**: User starts survey, closes bot, returns later to continue.

**Complex Includes**: Loads all necessary data for resuming.

#### Completion Tracking

```csharp
// Lines 76-83
/// <inheritdoc />
public async Task<bool> HasUserCompletedSurveyAsync(int surveyId, long telegramId)
{
    return await _dbSet
        .AnyAsync(r => r.SurveyId == surveyId
            && r.RespondentTelegramId == telegramId
            && r.IsComplete);
}

// Lines 85-91
/// <inheritdoc />
public async Task<int> GetCompletedCountAsync(int surveyId)
{
    return await _dbSet
        .Where(r => r.SurveyId == surveyId && r.IsComplete)
        .CountAsync();
}
```

#### Date Range Filtering

```csharp
// Lines 93-104
/// <inheritdoc />
public async Task<IEnumerable<Response>> GetByDateRangeAsync(int surveyId, DateTime startDate, DateTime endDate)
{
    return await _dbSet
        .Include(r => r.Answers)
        .Where(r => r.SurveyId == surveyId
            && r.SubmittedAt.HasValue
            && r.SubmittedAt >= startDate
            && r.SubmittedAt <= endDate)
        .OrderBy(r => r.SubmittedAt)
        .ToListAsync();
}
```

**Use Case**: Analytics and reporting for specific time periods.

#### MarkAsCompleteAsync

**Update response status**:

```csharp
// Lines 106-122
/// <inheritdoc />
public async Task<bool> MarkAsCompleteAsync(int responseId)
{
    var response = await GetByIdAsync(responseId);

    if (response == null)
    {
        return false;
    }

    response.IsComplete = true;
    response.SubmittedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return true;
}
```

**Atomic Operation**: Both fields updated together.

### UserRepository

**Location**: `Repositories/UserRepository.cs` (Lines 1-146)

User-specific repository with Telegram integration.

#### GetByTelegramIdAsync

**Primary user lookup**:

```csharp
// Lines 21-26
/// <inheritdoc />
public async Task<User?> GetByTelegramIdAsync(long telegramId)
{
    return await _dbSet
        .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
}
```

**Use Case**: Every bot interaction starts with Telegram ID lookup.

#### CreateOrUpdateAsync (Upsert Pattern)

**Critical Method**: Handles user registration and updates:

```csharp
// Lines 70-99
/// <inheritdoc />
public async Task<User> CreateOrUpdateAsync(long telegramId, string? username, string? firstName, string? lastName)
{
    var existingUser = await GetByTelegramIdAsync(telegramId);

    if (existingUser != null)
    {
        // Update existing user information
        existingUser.Username = username;
        existingUser.FirstName = firstName;
        existingUser.LastName = lastName;

        await _context.SaveChangesAsync();
        return existingUser;
    }

    // Create new user
    var newUser = new User
    {
        TelegramId = telegramId,
        Username = username,
        FirstName = firstName,
        LastName = lastName
    };

    await _dbSet.AddAsync(newUser);
    await _context.SaveChangesAsync();

    return newUser;
}
```

**Why Upsert?**
- Telegram users can change username/name
- Ensures user info stays current
- Prevents duplicate user records
- Simplifies bot interaction logic

#### Search and Filter Methods

```csharp
// Lines 120-136
/// <inheritdoc />
public async Task<IEnumerable<User>> SearchByNameAsync(string searchTerm)
{
    if (string.IsNullOrWhiteSpace(searchTerm))
    {
        return await GetAllAsync();
    }

    var lowerSearchTerm = searchTerm.ToLower();

    return await _dbSet
        .Where(u =>
            (u.FirstName != null && u.FirstName.ToLower().Contains(lowerSearchTerm)) ||
            (u.LastName != null && u.LastName.ToLower().Contains(lowerSearchTerm)) ||
            (u.Username != null && u.Username.ToLower().Contains(lowerSearchTerm)))
        .OrderBy(u => u.Username ?? u.FirstName ?? u.LastName)
        .ToListAsync();
}
```

**Pattern**: Searches across multiple fields with null handling.

### AnswerRepository

**Location**: `Repositories/AnswerRepository.cs` (Lines 1-134)

Answer-specific repository with batch operations.

#### GetByResponseIdAsync

**Load all answers for a response**:

```csharp
// Lines 21-29
/// <inheritdoc />
public async Task<IEnumerable<Answer>> GetByResponseIdAsync(int responseId)
{
    return await _dbSet
        .Include(a => a.Question)
        .Where(a => a.ResponseId == responseId)
        .OrderBy(a => a.Question.OrderIndex)
        .ToListAsync();
}
```

**Important**: Orders by question order, not answer creation time.

#### CreateBatchAsync

**Bulk answer creation**:

```csharp
// Lines 50-62
/// <inheritdoc />
public async Task<IEnumerable<Answer>> CreateBatchAsync(IEnumerable<Answer> answers)
{
    if (answers == null || !answers.Any())
    {
        return Enumerable.Empty<Answer>();
    }

    await _dbSet.AddRangeAsync(answers);
    await _context.SaveChangesAsync();

    return answers;
}
```

**Performance**: Single database roundtrip for multiple answers.

---

## Service Layer

Business logic layer implementing domain operations with validation and authorization.

### AuthService

**Location**: `Services/AuthService.cs` (Lines 1-174)

JWT token generation and authentication.

#### Dependencies

```csharp
// Lines 18-32
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }
```

#### LoginAsync

**User authentication with token generation**:

```csharp
// Lines 34-67
/// <summary>
/// Authenticates a user by Telegram ID and generates JWT tokens.
/// </summary>
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
- Upsert user (create or update)
- Generate JWT access token
- Generate refresh token (cryptographically secure)
- Set expiration time
- Return complete authentication DTO

#### GenerateAccessToken

**JWT token creation with claims**:

```csharp
// Lines 69-100
/// <summary>
/// Generates a JWT access token with user claims.
/// </summary>
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
- `NameIdentifier`: Internal user ID (for authorization)
- `TelegramId`: Telegram user ID
- `Jti`: Token unique ID
- `Iat`: Issued at timestamp
- `Name` / `Username`: User's username (if available)

#### GenerateRefreshToken

**Cryptographically secure random token**:

```csharp
// Lines 102-111
/// <summary>
/// Generates a cryptographically secure refresh token.
/// </summary>
public string GenerateRefreshToken()
{
    var randomNumber = new byte[32];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(randomNumber);
    return Convert.ToBase64String(randomNumber);
}
```

**Note**: MVP implementation. Production should store refresh tokens in database.

#### ValidateToken

**Token validation**:

```csharp
// Lines 113-147
/// <summary>
/// Validates a JWT token.
/// </summary>
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
- Issuer matches
- Audience matches
- Token not expired
- Signature valid
- Zero clock skew (strict expiration)

### SurveyService

**Location**: `Services/SurveyService.cs` (Lines 1-724)

Complete survey lifecycle management with business logic.

#### Dependencies

```csharp
// Lines 18-41
public class SurveyService : ISurveyService
{
    private readonly ISurveyRepository _surveyRepository;
    private readonly IResponseRepository _responseRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SurveyService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyService"/> class.
    /// </summary>
    public SurveyService(
        ISurveyRepository surveyRepository,
        IResponseRepository responseRepository,
        IAnswerRepository answerRepository,
        IMapper mapper,
        ILogger<SurveyService> logger)
    {
        _surveyRepository = surveyRepository;
        _responseRepository = responseRepository;
        _answerRepository = answerRepository;
        _mapper = mapper;
        _logger = logger;
    }
```

#### CreateSurveyAsync

**Survey creation with code generation**:

```csharp
// Lines 43-74
/// <inheritdoc/>
public async Task<SurveyDto> CreateSurveyAsync(int userId, CreateSurveyDto dto)
{
    _logger.LogInformation("Creating new survey for user {UserId}: {Title}", userId, dto.Title);

    // Validate the DTO
    ValidateCreateSurveyDto(dto);

    // Create survey entity
    var survey = _mapper.Map<Survey>(dto);
    survey.CreatorId = userId;
    survey.IsActive = false; // Always create as inactive, user must explicitly activate

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

**Key Features**:
- Validates input DTO
- Creates survey inactive by default (safety)
- Generates unique shareable code
- Maps entity to DTO
- Initializes response counts

#### Survey Code Generation

**Utility method for unique codes**:

Uses `SurveyCodeGenerator.GenerateUniqueCodeAsync` from Core layer:
- Generates random alphanumeric codes
- Checks for uniqueness via repository
- Retries if collision detected
- Uppercase for consistency

#### UpdateSurveyAsync

**Survey modification with protection**:

```csharp
// Lines 76-127
/// <inheritdoc/>
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

    _logger.LogInformation("Survey {SurveyId} updated successfully", surveyId);

    // Get response counts
    var responseCount = await _surveyRepository.GetResponseCountAsync(surveyId);
    var completedCount = await _responseRepository.GetCompletedCountAsync(surveyId);

    // Map to DTO
    var result = _mapper.Map<SurveyDto>(survey);
    result.TotalResponses = responseCount;
    result.CompletedResponses = completedCount;

    return result;
}
```

**Business Rules**:
1. Survey must exist
2. User must own survey (authorization)
3. Cannot modify active survey with responses (data integrity)
4. Updates timestamp automatically
5. Returns updated counts

#### DeleteSurveyAsync

**Soft delete vs hard delete**:

```csharp
// Lines 129-171
/// <inheritdoc/>
public async Task<bool> DeleteSurveyAsync(int surveyId, int userId)
{
    _logger.LogInformation("Deleting survey {SurveyId} by user {UserId}", surveyId, userId);

    // Get survey
    var survey = await _surveyRepository.GetByIdAsync(surveyId);
    if (survey == null)
    {
        _logger.LogWarning("Survey {SurveyId} not found", surveyId);
        throw new SurveyNotFoundException(surveyId);
    }

    // Check authorization
    if (survey.CreatorId != userId)
    {
        _logger.LogWarning("User {UserId} attempted to delete survey {SurveyId} owned by {OwnerId}",
            userId, surveyId, survey.CreatorId);
        throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
    }

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

**Smart Delete Logic**:
- **Has responses**: Soft delete (deactivate) to preserve data
- **No responses**: Hard delete (remove from database)
- Protects valuable response data from accidental deletion

#### ActivateSurveyAsync

**Validation before activation**:

```csharp
// Lines 260-306
/// <inheritdoc/>
public async Task<SurveyDto> ActivateSurveyAsync(int surveyId, int userId)
{
    _logger.LogInformation("Activating survey {SurveyId} by user {UserId}", surveyId, userId);

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
        _logger.LogWarning("User {UserId} attempted to activate survey {SurveyId} owned by {OwnerId}",
            userId, surveyId, survey.CreatorId);
        throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
    }

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

    _logger.LogInformation("Survey {SurveyId} activated successfully", surveyId);

    // Get response counts
    var responseCount = await _surveyRepository.GetResponseCountAsync(surveyId);
    var completedCount = await _responseRepository.GetCompletedCountAsync(surveyId);

    // Map to DTO
    var result = _mapper.Map<SurveyDto>(survey);
    result.TotalResponses = responseCount;
    result.CompletedResponses = completedCount;

    return result;
}
```

**Validation**: Cannot activate empty survey (must have at least one question).

#### GetSurveyStatisticsAsync

**Comprehensive analytics**:

```csharp
// Lines 350-410
/// <inheritdoc/>
public async Task<SurveyStatisticsDto> GetSurveyStatisticsAsync(int surveyId, int userId)
{
    _logger.LogInformation("Getting statistics for survey {SurveyId} requested by user {UserId}", surveyId, userId);

    // Get survey with questions and responses
    var survey = await _surveyRepository.GetByIdWithDetailsAsync(surveyId);
    if (survey == null)
    {
        _logger.LogWarning("Survey {SurveyId} not found", surveyId);
        throw new SurveyNotFoundException(surveyId);
    }

    // Check authorization
    if (survey.CreatorId != userId)
    {
        _logger.LogWarning("User {UserId} attempted to access statistics for survey {SurveyId} owned by {OwnerId}",
            userId, surveyId, survey.CreatorId);
        throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
    }

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

    // Calculate average completion time for completed responses
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
    statistics.QuestionStatistics = await CalculateQuestionStatisticsAsync(survey.Questions.ToList(), completedResponses);

    _logger.LogInformation("Statistics calculated for survey {SurveyId}", surveyId);

    return statistics;
}
```

**Metrics Calculated**:
- Total responses (all states)
- Completed responses
- Incomplete responses
- Completion rate percentage
- Unique respondents
- Average completion time (in seconds)
- First and last response timestamps
- Per-question statistics

#### Question Statistics

**Per-question analysis**:

```csharp
// Lines 512-560
/// <summary>
/// Calculates statistics for each question in the survey.
/// </summary>
private async Task<List<QuestionStatisticsDto>> CalculateQuestionStatisticsAsync(
    List<Question> questions,
    List<Response> completedResponses)
{
    var statistics = new List<QuestionStatisticsDto>();

    foreach (var question in questions.OrderBy(q => q.OrderIndex))
    {
        var questionStat = new QuestionStatisticsDto
        {
            QuestionId = question.Id,
            QuestionText = question.QuestionText,
            QuestionType = question.QuestionType
        };

        // Get all answers for this question from completed responses
        var responseIds = completedResponses.Select(r => r.Id).ToList();
        var answers = question.Answers
            .Where(a => responseIds.Contains(a.ResponseId))
            .ToList();

        questionStat.TotalAnswers = answers.Count;
        questionStat.SkippedCount = completedResponses.Count - answers.Count;
        questionStat.ResponseRate = completedResponses.Count > 0
            ? Math.Round((double)answers.Count / completedResponses.Count * 100, 2)
            : 0;

        // Calculate type-specific statistics
        switch (question.QuestionType)
        {
            case QuestionType.MultipleChoice:
            case QuestionType.SingleChoice:
                questionStat.ChoiceDistribution = CalculateChoiceDistribution(answers, question.OptionsJson);
                break;

            case QuestionType.Rating:
                questionStat.RatingStatistics = CalculateRatingStatistics(answers);
                break;

            case QuestionType.Text:
                questionStat.TextStatistics = CalculateTextStatistics(answers);
                break;
        }

        statistics.Add(questionStat);
    }

    return statistics;
}
```

**Type-Specific Stats**:
- **Choice questions**: Distribution of selections with percentages
- **Rating questions**: Average, min, max, distribution
- **Text questions**: Average length, min/max length

#### GetSurveyByCodeAsync

**Public survey access**:

```csharp
// Lines 420-458
/// <inheritdoc/>
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

    // Get response counts
    var responseCount = await _surveyRepository.GetResponseCountAsync(survey.Id);
    var completedCount = await _responseRepository.GetCompletedCountAsync(survey.Id);

    // Map to DTO
    var result = _mapper.Map<SurveyDto>(survey);
    result.TotalResponses = responseCount;
    result.CompletedResponses = completedCount;

    _logger.LogInformation("Survey {SurveyId} retrieved by code {Code}", survey.Id, code);

    return result;
}
```

**Security**: Only returns active surveys (inactive surveys not publicly accessible).

### QuestionService

**Location**: `Services/QuestionService.cs` (Lines 1-462)

Question management with type-specific validation.

#### Validation Constants

```csharp
// Lines 22-27
// Constants for validation
private const int MinOptionsCount = 2;
private const int MaxOptionsCount = 10;
private const int MaxOptionLength = 200;
private const int MinRating = 1;
private const int MaxRating = 5;
```

#### AddQuestionAsync

**Question creation with validation**:

```csharp
// Lines 44-108
/// <inheritdoc/>
public async Task<QuestionDto> AddQuestionAsync(int surveyId, int userId, CreateQuestionDto dto)
{
    _logger.LogInformation("Adding question to survey {SurveyId} by user {UserId}", surveyId, userId);

    // Get survey with questions
    var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);
    if (survey == null)
    {
        _logger.LogWarning("Survey {SurveyId} not found", surveyId);
        throw new SurveyNotFoundException(surveyId);
    }

    // Check authorization - user must own the survey
    if (survey.CreatorId != userId)
    {
        _logger.LogWarning("User {UserId} attempted to add question to survey {SurveyId} owned by {OwnerId}",
            userId, surveyId, survey.CreatorId);
        throw new Core.Exceptions.UnauthorizedAccessException(userId, "Survey", surveyId);
    }

    // Check if survey has responses - cannot add questions if survey has responses
    var hasResponses = await _surveyRepository.HasResponsesAsync(surveyId);
    if (hasResponses)
    {
        _logger.LogWarning("Cannot add question to survey {SurveyId} that has responses", surveyId);
        throw new SurveyOperationException(
            "Cannot add questions to a survey that has responses. Create a new survey or deactivate this one first.");
    }

    // Validate question options based on type
    var validationResult = ValidateQuestionOptionsAsync(dto.QuestionType, dto.Options);
    if (!validationResult.IsValid)
    {
        _logger.LogWarning("Question validation failed for survey {SurveyId}: {Errors}",
            surveyId, string.Join(", ", validationResult.Errors));
        throw new QuestionValidationException(
            "Question validation failed: " + string.Join(", ", validationResult.Errors));
    }

    // Create question entity
    var question = new Question
    {
        SurveyId = surveyId,
        QuestionText = dto.QuestionText,
        QuestionType = dto.QuestionType,
        IsRequired = dto.IsRequired,
        OrderIndex = await _questionRepository.GetNextOrderIndexAsync(surveyId)
    };

    // Serialize options for choice-based questions
    if (dto.QuestionType == QuestionType.SingleChoice || dto.QuestionType == QuestionType.MultipleChoice)
    {
        question.OptionsJson = JsonSerializer.Serialize(dto.Options);
    }

    // Save to database
    var createdQuestion = await _questionRepository.CreateAsync(question);

    _logger.LogInformation("Question {QuestionId} added to survey {SurveyId} successfully",
        createdQuestion.Id, surveyId);

    // Map to DTO
    return MapToDto(createdQuestion);
}
```

**Business Rules**:
1. Survey must exist
2. User must own survey
3. Cannot add questions to survey with responses (data integrity)
4. Options must be valid for question type
5. OrderIndex auto-assigned

#### ValidateQuestionOptionsAsync

**Type-specific option validation**:

```csharp
// Lines 355-422
/// <inheritdoc/>
public QuestionValidationResult ValidateQuestionOptionsAsync(QuestionType type, List<string>? options)
{
    switch (type)
    {
        case QuestionType.Text:
            // Text questions should not have options
            if (options != null && options.Any())
            {
                return QuestionValidationResult.Failure("Text questions should not have options.");
            }
            return QuestionValidationResult.Success();

        case QuestionType.SingleChoice:
        case QuestionType.MultipleChoice:
            // Choice-based questions must have options
            if (options == null || !options.Any())
            {
                return QuestionValidationResult.Failure("Choice-based questions must have at least 2 options.");
            }

            if (options.Count < MinOptionsCount)
            {
                return QuestionValidationResult.Failure($"Choice-based questions must have at least {MinOptionsCount} options.");
            }

            if (options.Count > MaxOptionsCount)
            {
                return QuestionValidationResult.Failure($"Questions cannot have more than {MaxOptionsCount} options.");
            }

            // Check for empty options
            if (options.Any(string.IsNullOrWhiteSpace))
            {
                return QuestionValidationResult.Failure("All options must have text.");
            }

            // Check option length
            var longOptions = options.Where(o => o.Length > MaxOptionLength).ToList();
            if (longOptions.Any())
            {
                return QuestionValidationResult.Failure($"Option text cannot exceed {MaxOptionLength} characters.");
            }

            // Check for duplicate options
            var duplicates = options.GroupBy(o => o.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
            {
                return QuestionValidationResult.Failure($"Duplicate options are not allowed: {string.Join(", ", duplicates)}");
            }

            return QuestionValidationResult.Success();

        case QuestionType.Rating:
            // Rating questions should not have options
            if (options != null && options.Any())
            {
                return QuestionValidationResult.Failure("Rating questions should not have options.");
            }
            return QuestionValidationResult.Success();

        default:
            throw new InvalidQuestionTypeException(type);
    }
}
```

**Validation Rules**:
- **Text**: No options allowed
- **Single/Multiple Choice**: 2-10 options required, no duplicates, max 200 chars each
- **Rating**: No options allowed

### ResponseService

**Location**: `Services/ResponseService.cs` (Lines 1-623)

Response and answer management with validation.

#### StartResponseAsync

**Begin survey response**:

```csharp
// Lines 50-91
/// <inheritdoc/>
public async Task<ResponseDto> StartResponseAsync(int surveyId, long telegramUserId, string? username = null, string? firstName = null)
{
    _logger.LogInformation("Starting response for survey {SurveyId} by Telegram user {TelegramUserId}", surveyId, telegramUserId);

    // Validate survey exists and is active
    var survey = await _surveyRepository.GetByIdAsync(surveyId);
    if (survey == null)
    {
        _logger.LogWarning("Survey {SurveyId} not found", surveyId);
        throw new SurveyNotFoundException(surveyId);
    }

    if (!survey.IsActive)
    {
        _logger.LogWarning("Survey {SurveyId} is not active", surveyId);
        throw new SurveyOperationException("This survey is not currently active.");
    }

    // Check for duplicate completed responses
    var hasCompleted = await _responseRepository.HasUserCompletedSurveyAsync(surveyId, telegramUserId);
    if (hasCompleted && !survey.AllowMultipleResponses)
    {
        _logger.LogWarning("User {TelegramUserId} has already completed survey {SurveyId}", telegramUserId, surveyId);
        throw new DuplicateResponseException(surveyId, telegramUserId);
    }

    // Create new response
    var response = new Response
    {
        SurveyId = surveyId,
        RespondentTelegramId = telegramUserId,
        IsComplete = false,
        StartedAt = DateTime.UtcNow
    };

    var createdResponse = await _responseRepository.CreateAsync(response);

    _logger.LogInformation("Response {ResponseId} started for survey {SurveyId} by user {TelegramUserId}",
        createdResponse.Id, surveyId, telegramUserId);

    return await MapToResponseDtoAsync(createdResponse, username, firstName);
}
```

**Checks**:
1. Survey exists and is active
2. User hasn't already completed (unless multiple responses allowed)
3. Creates incomplete response

#### SaveAnswerAsync

**Save answer with validation**:

```csharp
// Lines 93-181
/// <inheritdoc/>
public async Task<ResponseDto> SaveAnswerAsync(
    int responseId,
    int questionId,
    string? answerText = null,
    List<string>? selectedOptions = null,
    int? ratingValue = null,
    int? userId = null)
{
    _logger.LogInformation("Saving answer for response {ResponseId}, question {QuestionId}", responseId, questionId);

    // Get response with survey
    var response = await _responseRepository.GetByIdWithAnswersAsync(responseId);
    if (response == null)
    {
        _logger.LogWarning("Response {ResponseId} not found", responseId);
        throw new ResponseNotFoundException(responseId);
    }

    // Check if response is already completed
    if (response.IsComplete)
    {
        _logger.LogWarning("Cannot save answer to completed response {ResponseId}", responseId);
        throw new SurveyOperationException("Cannot modify a completed response.");
    }

    // Authorize if userId is provided - user must own the survey
    if (userId.HasValue)
    {
        await AuthorizeUserForResponseAsync(response, userId.Value);
    }

    // Validate question exists and belongs to survey
    var question = await _questionRepository.GetByIdAsync(questionId);
    if (question == null)
    {
        _logger.LogWarning("Question {QuestionId} not found", questionId);
        throw new QuestionNotFoundException(questionId);
    }

    if (question.SurveyId != response.SurveyId)
    {
        _logger.LogWarning("Question {QuestionId} does not belong to survey {SurveyId}",
            questionId, response.SurveyId);
        throw new QuestionValidationException("Question does not belong to this survey.");
    }

    // Validate answer format
    var validationResult = await ValidateAnswerFormatAsync(questionId, answerText, selectedOptions, ratingValue);
    if (!validationResult.IsValid)
    {
        _logger.LogWarning("Invalid answer format for question {QuestionId}: {Error}",
            questionId, validationResult.ErrorMessage);
        throw new InvalidAnswerFormatException(questionId, question.QuestionType, validationResult.ErrorMessage!);
    }

    // Check if answer already exists for this question
    var existingAnswer = await _answerRepository.GetByResponseAndQuestionAsync(responseId, questionId);

    if (existingAnswer != null)
    {
        // Update existing answer
        existingAnswer.AnswerText = answerText;
        existingAnswer.AnswerJson = CreateAnswerJson(question.QuestionType, selectedOptions, ratingValue);
        existingAnswer.CreatedAt = DateTime.UtcNow;

        await _answerRepository.UpdateAsync(existingAnswer);
        _logger.LogInformation("Updated existing answer {AnswerId} for response {ResponseId}", existingAnswer.Id, responseId);
    }
    else
    {
        // Create new answer
        var answer = new Answer
        {
            ResponseId = responseId,
            QuestionId = questionId,
            AnswerText = answerText,
            AnswerJson = CreateAnswerJson(question.QuestionType, selectedOptions, ratingValue),
            CreatedAt = DateTime.UtcNow
        };

        await _answerRepository.CreateAsync(answer);
        _logger.LogInformation("Created new answer for response {ResponseId}, question {QuestionId}", responseId, questionId);
    }

    // Return updated response
    var updatedResponse = await _responseRepository.GetByIdWithAnswersAsync(responseId);
    return await MapToResponseDtoAsync(updatedResponse!);
}
```

**Features**:
- Validates response not completed
- Validates question belongs to survey
- Validates answer format for question type
- Updates existing answer or creates new
- Returns updated response with all answers

#### Answer Validation Methods

**Text Answer**:

```csharp
// Lines 515-528
private ValidationResult ValidateTextAnswer(string? answerText, bool isRequired)
{
    if (isRequired && string.IsNullOrWhiteSpace(answerText))
    {
        return ValidationResult.Failure("Text answer is required");
    }

    if (!string.IsNullOrEmpty(answerText) && answerText.Length > MaxTextAnswerLength)
    {
        return ValidationResult.Failure($"Text answer cannot exceed {MaxTextAnswerLength} characters");
    }

    return ValidationResult.Success();
}
```

**Single Choice**:

```csharp
// Lines 530-565
private ValidationResult ValidateSingleChoiceAnswer(List<string>? selectedOptions, string? optionsJson, bool isRequired)
{
    if (isRequired && (selectedOptions == null || !selectedOptions.Any()))
    {
        return ValidationResult.Failure("An option must be selected");
    }

    if (selectedOptions == null || !selectedOptions.Any())
    {
        return ValidationResult.Success(); // Optional question with no answer
    }

    if (selectedOptions.Count > 1)
    {
        return ValidationResult.Failure("Only one option can be selected for single choice questions");
    }

    // Validate option exists in question options
    if (!string.IsNullOrEmpty(optionsJson))
    {
        try
        {
            var validOptions = JsonSerializer.Deserialize<List<string>>(optionsJson);
            if (validOptions != null && !validOptions.Contains(selectedOptions[0]))
            {
                return ValidationResult.Failure("Selected option is not valid for this question");
            }
        }
        catch (JsonException)
        {
            _logger.LogWarning("Failed to parse options JSON");
        }
    }

    return ValidationResult.Success();
}
```

**Rating**:

```csharp
// Lines 603-621
private ValidationResult ValidateRatingAnswer(int? ratingValue, bool isRequired)
{
    if (isRequired && !ratingValue.HasValue)
    {
        return ValidationResult.Failure("Rating is required");
    }

    if (!ratingValue.HasValue)
    {
        return ValidationResult.Success(); // Optional question with no answer
    }

    if (ratingValue < MinRatingValue || ratingValue > MaxRatingValue)
    {
        return ValidationResult.Failure($"Rating must be between {MinRatingValue} and {MaxRatingValue}");
    }

    return ValidationResult.Success();
}
```

### UserService

**Location**: `Services/UserService.cs` (Lines 1-289)

User management with Telegram integration.

#### RegisterAsync (Upsert)

**User registration/update**:

```csharp
// Lines 42-88
/// <summary>
/// Registers a new user or updates an existing user (upsert pattern).
/// This is designed for Telegram bot integration where users are automatically
/// registered on first interaction via /start command.
/// </summary>
public async Task<UserWithTokenDto> RegisterAsync(RegisterDto registerDto)
{
    _logger.LogInformation(
        "User registration/login attempt for Telegram ID: {TelegramId}, Username: {Username}",
        registerDto.TelegramId,
        registerDto.Username);

    // Use the repository's CreateOrUpdateAsync method for upsert pattern
    // This ensures no duplicate users are created
    var user = await _userRepository.CreateOrUpdateAsync(
        registerDto.TelegramId,
        registerDto.Username,
        registerDto.FirstName,
        registerDto.LastName);

    // Update last login timestamp
    user.LastLoginAt = DateTime.UtcNow;
    await _userRepository.UpdateAsync(user);

    _logger.LogInformation(
        "User {Action} successfully: UserId={UserId}, TelegramId={TelegramId}",
        user.CreatedAt == user.UpdatedAt ? "registered" : "updated",
        user.Id,
        user.TelegramId);

    // Generate JWT token
    var accessToken = _authService.GenerateAccessToken(
        user.Id,
        user.TelegramId,
        user.Username);

    var refreshToken = _authService.GenerateRefreshToken();
    var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.TokenLifetimeHours);

    _logger.LogInformation("Generated JWT token for user ID: {UserId}", user.Id);

    // Map to DTOs
    var userDto = MapToUserDto(user);

    return new UserWithTokenDto
    {
        User = userDto,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresAt = expiresAt
    };
}
```

**Features**:
- Upserts user (create or update)
- Updates last login timestamp
- Generates JWT token
- Returns user with token

---

## Data Seeding

### DataSeeder

**Location**: `Data/DataSeeder.cs` (Lines 1-587)

Comprehensive test data generation for development and testing.

#### Main Seeding Method

```csharp
// Lines 22-53
/// <summary>
/// Seeds all development data. Safe to call multiple times - will skip if data already exists.
/// </summary>
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

#### Sample Surveys Created

1. **Customer Satisfaction Survey** (Active)
   - Rating question (1-5 scale)
   - Multiple choice (services used)
   - Single choice (how they heard about us)
   - Text question (feedback)

2. **Product Feedback Survey** (Active, allows multiple responses)
   - Single choice (use case)
   - Rating question (ease of use)
   - Multiple choice (valuable features)
   - Text question (feature requests)

3. **Event Registration Survey** (Inactive, for testing)
   - Text question (full name)
   - Single choice (track interest)
   - Multiple choice (workshop selection)
   - Rating question (expertise level)

#### Sample Response Data

**Complete responses with varied answers**:
- Positive feedback response (5-star rating)
- Mixed feedback response (3-star rating)
- Power user response (multiple features selected)
- Small business response (mobile-focused)
- Incomplete response (for testing resume)

**Use Cases**:
- Development testing
- Demo data
- Integration testing
- UI development

### DatabaseExtensions

**Location**: `Data/Extensions/DatabaseExtensions.cs` (Lines 1-61)

Helper methods for database operations.

#### Seeding Extension

```csharp
// Lines 17-25
/// <summary>
/// Seeds the database with development data.
/// </summary>
/// <param name="serviceProvider">The service provider.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

    var seeder = new DataSeeder(context, logger);
    await seeder.SeedAsync();
}
```

#### Migration Extension

```csharp
// Lines 27-38
/// <summary>
/// Applies any pending migrations to the database.
/// </summary>
/// <param name="serviceProvider">The service provider.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();

    await context.Database.MigrateAsync();
}
```

#### Reset Extension

```csharp
// Lines 40-60
/// <summary>
/// Drops and recreates the database, then seeds with development data.
/// WARNING: This will delete all existing data!
/// </summary>
/// <param name="serviceProvider">The service provider.</param>
/// <returns>A task representing the asynchronous operation.</returns>
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

**DANGER**: `ResetAndSeedDatabaseAsync` deletes all data. Only use in development!

---

## Migrations

### Migration Files

Located in `Migrations/` directory:

1. **20251105190107_InitialCreate.cs**
   - Creates all tables
   - Sets up foreign keys
   - Adds indexes
   - Configures JSONB columns

2. **20251106000001_AddLastLoginAtToUser.cs**
   - Adds `last_login_at` column to users table
   - Nullable datetime for tracking login activity

3. **20251109000001_AddSurveyCodeColumn.cs**
   - Adds `code` column to surveys table
   - Unique partial index on code
   - Max length 10 characters

4. **SurveyBotDbContextModelSnapshot.cs**
   - Current model state
   - Used by EF Core for migration generation

### Running Migrations

**From API project directory** (`src/SurveyBot.API`):

```bash
# Add new migration
dotnet ef migrations add MigrationName --project ../SurveyBot.Infrastructure

# Apply migrations
dotnet ef database update --project ../SurveyBot.Infrastructure

# Rollback to specific migration
dotnet ef database update PreviousMigrationName --project ../SurveyBot.Infrastructure

# Remove last migration (if not applied)
dotnet ef migrations remove --project ../SurveyBot.Infrastructure

# Generate SQL script
dotnet ef migrations script --project ../SurveyBot.Infrastructure

# Generate script for specific range
dotnet ef migrations script FromMigration ToMigration --project ../SurveyBot.Infrastructure
```

### Migration Best Practices

1. **Always backup** production database before migrations
2. **Test migrations** on development/staging first
3. **Review generated SQL** before applying to production
4. **Never edit** already applied migrations
5. **Use descriptive names** (e.g., AddSurveyCodeColumn not Update1)
6. **Keep migrations small** - one logical change per migration
7. **Handle data migration** in Up() method if needed
8. **Provide Down() method** for rollback capability

---

## Dependency Injection

### DependencyInjection.cs

**Location**: `DependencyInjection.cs` (Lines 1-46)

Extension method for registering all Infrastructure services.

```csharp
// Lines 14-45
/// <summary>
/// Extension methods for configuring Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<SurveyBotDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(SurveyBotDbContext).Assembly.FullName)));

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
}
```

**Usage in API**:

```csharp
// In Program.cs
builder.Services.AddInfrastructure(builder.Configuration);
```

**Lifetime**: All services registered as `Scoped` (one instance per HTTP request).

---

## Key Features

### 1. Automatic Timestamp Management

**SaveChangesAsync override** automatically sets:
- `CreatedAt` on entity creation
- `UpdatedAt` on entity modification
- All timestamps in UTC

**No manual timestamp management needed!**

### 2. Survey Code Generation

**Unique shareable codes** for surveys:
- 6-8 character alphanumeric codes
- Uppercase for consistency
- Collision detection and retry
- Used for public survey access

### 3. JSONB Storage

**PostgreSQL JSONB columns** for:
- Question options (flexible structure)
- Answer data (choices, ratings)
- Fast querying with GIN indexes
- Schema flexibility

### 4. Smart Delete Logic

**Survey deletion**:
- Has responses → Soft delete (deactivate)
- No responses → Hard delete (remove)
- Preserves valuable data

### 5. Authorization Checks

**Every operation checks**:
- User owns the resource
- Resource exists
- Operation is permitted

### 6. Comprehensive Statistics

**Survey analytics**:
- Response rates
- Completion rates
- Average completion time
- Per-question distributions
- Respondent counts

### 7. Transaction Support

**Atomic operations** where needed:
- Question reordering
- Batch operations
- Critical updates

### 8. Eager Loading

**Prevent N+1 queries**:
- Include navigation properties
- ThenInclude for nested data
- AsNoTracking for read-only

### 9. Upsert Pattern

**CreateOrUpdateAsync**:
- Telegram user management
- Prevents duplicates
- Keeps data current

### 10. Validation at Every Level

**Multi-layer validation**:
- Database constraints (check, unique)
- Service-level validation
- Type-specific validation
- Business rule enforcement

---

## Best Practices

### Repository Best Practices

1. **Use Virtual Methods**
   ```csharp
   public virtual async Task<T?> GetByIdAsync(int id)
   ```
   Allows derived classes to override with custom logic.

2. **Always Use Async**
   All database operations must be async for scalability.

3. **Include Related Data**
   ```csharp
   .Include(s => s.Creator)
   .Include(s => s.Questions)
   ```
   Prevents N+1 query problems.

4. **Use AsNoTracking for Reads**
   ```csharp
   .AsNoTracking()
   ```
   Better performance for read-only queries.

5. **Order Collections**
   ```csharp
   .OrderBy(q => q.OrderIndex)
   ```
   Consistent ordering for display.

### Service Best Practices

1. **Validate Before Operations**
   Always validate input before database operations.

2. **Check Authorization**
   ```csharp
   if (survey.CreatorId != userId)
       throw new UnauthorizedAccessException();
   ```

3. **Use Logging**
   Log important operations and errors.

4. **Throw Domain Exceptions**
   ```csharp
   throw new SurveyNotFoundException(surveyId);
   ```

5. **Return DTOs, Not Entities**
   Never expose domain entities to upper layers.

### Entity Configuration Best Practices

1. **Use Fluent API**
   Prefer Fluent API over data annotations.

2. **Name Indexes**
   ```csharp
   .HasDatabaseName("idx_surveys_code")
   ```

3. **Use Check Constraints**
   Enforce rules at database level.

4. **Configure Cascade Delete Carefully**
   Think about data preservation.

5. **Use Partial Indexes**
   ```csharp
   .HasFilter("code IS NOT NULL")
   ```

### Performance Best Practices

1. **Use Indexes**
   - Foreign keys
   - Frequently queried columns
   - Composite indexes for common queries

2. **Avoid N+1 Queries**
   Use Include() for related data.

3. **Use Pagination**
   Never load all records.

4. **Use AsNoTracking**
   For read-only queries.

5. **Use Batch Operations**
   AddRangeAsync instead of multiple AddAsync calls.

---

## Common Patterns

### Upsert Pattern

**CreateOrUpdateAsync** pattern:

```csharp
public async Task<User> CreateOrUpdateAsync(long telegramId, ...)
{
    var existingUser = await GetByTelegramIdAsync(telegramId);

    if (existingUser != null)
    {
        // Update existing
        existingUser.Username = username;
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

### Authorization Pattern

**Check ownership before operations**:

```csharp
private async Task AuthorizeUserForResponseAsync(Response response, int userId)
{
    var survey = await _surveyRepository.GetByIdAsync(response.SurveyId);
    if (survey == null)
        throw new SurveyNotFoundException(response.SurveyId);

    if (survey.CreatorId != userId)
        throw new UnauthorizedAccessException(userId, "Response", response.Id);
}
```

### Validation Pattern

**Validation Result object**:

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(string error) => new() { IsValid = false, ErrorMessage = error };
}
```

### Transaction Pattern

**Explicit transactions for atomicity**:

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();

try
{
    // Multiple operations
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

## Performance Optimization

### Index Strategy

1. **Foreign Keys**: Always indexed
2. **Filter Columns**: Active status, completion status
3. **Composite Indexes**: Common query patterns (CreatorId + IsActive)
4. **Partial Indexes**: Filter non-null values
5. **GIN Indexes**: JSONB column searching

### Query Optimization

1. **Eager Loading**
   ```csharp
   .Include(s => s.Questions)
   .ThenInclude(q => q.Answers)
   ```

2. **Select Specific Columns**
   ```csharp
   .Select(s => new { s.Id, s.Title })
   ```

3. **Pagination**
   ```csharp
   .Skip((page - 1) * pageSize)
   .Take(pageSize)
   ```

4. **AsNoTracking**
   ```csharp
   .AsNoTracking() // Read-only, no change tracking
   ```

### Batch Operations

**Bulk insert**:
```csharp
await _dbSet.AddRangeAsync(answers); // Single DB round trip
```

**Bulk delete**:
```csharp
var responses = await _dbSet.Where(r => r.SurveyId == surveyId).ToListAsync();
_dbSet.RemoveRange(responses);
```

---

## Error Handling

### Exception Types

Infrastructure throws domain exceptions from Core:

1. **SurveyNotFoundException**
2. **QuestionNotFoundException**
3. **ResponseNotFoundException**
4. **SurveyValidationException**
5. **QuestionValidationException**
6. **InvalidAnswerFormatException**
7. **UnauthorizedAccessException**
8. **DuplicateResponseException**
9. **SurveyOperationException**

### Exception Handling Pattern

**Catch specific, then general**:

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

### Logging Strategy

**Structured logging with context**:

```csharp
_logger.LogInformation(
    "Survey {SurveyId} created by user {UserId} with code {Code}",
    survey.Id, userId, survey.Code);

_logger.LogWarning(
    "User {UserId} attempted to access survey {SurveyId} owned by {OwnerId}",
    userId, surveyId, survey.CreatorId);

_logger.LogError(ex,
    "Failed to delete survey {SurveyId}",
    surveyId);
```

---

## Testing Approaches

### Unit Testing Repositories

**Mock DbContext**:

```csharp
var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
    .UseInMemoryDatabase(databaseName: "TestDb")
    .Options;

using var context = new SurveyBotDbContext(options);
var repository = new SurveyRepository(context);

// Test repository methods
var survey = await repository.GetByIdAsync(1);
Assert.NotNull(survey);
```

### Unit Testing Services

**Mock repositories**:

```csharp
var mockRepo = new Mock<ISurveyRepository>();
mockRepo.Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(new Survey { Id = 1, Title = "Test" });

var service = new SurveyService(mockRepo.Object, ...);
var result = await service.GetSurveyByIdAsync(1, userId);

Assert.Equal("Test", result.Title);
```

### Integration Testing

**Test with real database**:

```csharp
var factory = new WebApplicationFactory<Program>();
var client = factory.CreateClient();

var response = await client.PostAsync("/api/surveys",
    new StringContent(json, Encoding.UTF8, "application/json"));

Assert.Equal(HttpStatusCode.Created, response.StatusCode);
```

---

## Common Issues and Solutions

### Issue 1: DbContext Lifetime

**Problem**: DbContext disposed before lazy-loaded navigation properties accessed.

**Solution**: Use eager loading with Include():
```csharp
.Include(s => s.Questions)
.Include(s => s.Creator)
```

### Issue 2: Circular References in JSON

**Problem**: Entity navigation properties cause infinite loops in serialization.

**Solution**: Use DTOs, not entities, in API responses:
```csharp
var dto = _mapper.Map<SurveyDto>(survey);
return dto; // Not survey entity directly
```

### Issue 3: N+1 Queries

**Problem**: Loading collection causes query for each item.

**Solution**: Use Include() for related data:
```csharp
.Include(s => s.Questions)
  .ThenInclude(q => q.Answers)
```

### Issue 4: Concurrent Updates

**Problem**: Two users update same entity simultaneously.

**Solution**: Add timestamp/rowversion column and enable optimistic concurrency:
```csharp
builder.Property(e => e.RowVersion)
    .IsRowVersion();
```

### Issue 5: JSONB Querying

**Problem**: Need to query inside JSONB columns.

**Solution**: Use PostgreSQL JSON operators:
```csharp
.Where(q => EF.Functions.JsonContains(q.OptionsJson, "\"Option1\""))
```

Or create GIN index for better performance.

### Issue 6: Migration Conflicts

**Problem**: Multiple developers create migrations simultaneously.

**Solution**:
1. Pull latest code
2. Remove your migration
3. Update database
4. Re-create migration
5. Check for conflicts

### Issue 7: Connection Pooling

**Problem**: Too many database connections.

**Solution**: Configure connection pooling in connection string:
```
"DefaultConnection": "Host=localhost;Port=5432;Database=surveybot_db;Username=user;Password=pass;Pooling=true;MinPoolSize=0;MaxPoolSize=100"
```

### Issue 8: Slow Queries

**Problem**: Queries taking too long.

**Solution**:
1. Add indexes on frequently queried columns
2. Use AsNoTracking() for read-only queries
3. Implement pagination
4. Use compiled queries for repeated queries
5. Enable query logging to identify slow queries

---

## Code Examples

### Example 1: Creating a Survey with Questions

```csharp
// Service layer
var surveyDto = new CreateSurveyDto
{
    Title = "Customer Feedback",
    Description = "Help us improve",
    AllowMultipleResponses = false,
    ShowResults = true
};

var survey = await _surveyService.CreateSurveyAsync(userId, surveyDto);

// Add questions
var question1 = new CreateQuestionDto
{
    QuestionText = "How satisfied are you?",
    QuestionType = QuestionType.Rating,
    IsRequired = true
};

await _questionService.AddQuestionAsync(survey.Id, userId, question1);

// Activate survey
await _surveyService.ActivateSurveyAsync(survey.Id, userId);
```

### Example 2: Responding to a Survey

```csharp
// Start response
var response = await _responseService.StartResponseAsync(surveyId, telegramUserId);

// Save answers
await _responseService.SaveAnswerAsync(
    response.Id,
    questionId,
    ratingValue: 5);

// Complete response
await _responseService.CompleteResponseAsync(response.Id);
```

### Example 3: Getting Survey Statistics

```csharp
var stats = await _surveyService.GetSurveyStatisticsAsync(surveyId, userId);

Console.WriteLine($"Total Responses: {stats.TotalResponses}");
Console.WriteLine($"Completion Rate: {stats.CompletionRate}%");
Console.WriteLine($"Average Time: {stats.AverageCompletionTime} seconds");

foreach (var questionStat in stats.QuestionStatistics)
{
    Console.WriteLine($"Question: {questionStat.QuestionText}");
    Console.WriteLine($"Response Rate: {questionStat.ResponseRate}%");
}
```

### Example 4: Custom Repository Query

```csharp
// In SurveyRepository
public async Task<IEnumerable<Survey>> GetPopularSurveysAsync(int limit)
{
    return await _dbSet
        .AsNoTracking()
        .Include(s => s.Creator)
        .Include(s => s.Responses)
        .Where(s => s.IsActive)
        .OrderByDescending(s => s.Responses.Count)
        .Take(limit)
        .ToListAsync();
}
```

### Example 5: Transaction with Rollback

```csharp
// In QuestionService
public async Task<bool> ReorderQuestionsAsync(int surveyId, int[] questionIds)
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        for (int i = 0; i < questionIds.Length; i++)
        {
            var question = await _questionRepository.GetByIdAsync(questionIds[i]);
            if (question == null)
            {
                await transaction.RollbackAsync();
                return false;
            }
            question.OrderIndex = i;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## Summary

**SurveyBot.Infrastructure** is a well-structured data access layer implementing:

- Clean separation of concerns (Repositories, Services)
- PostgreSQL-specific optimizations (JSONB, GIN indexes)
- Comprehensive business logic with validation
- Automatic timestamp management
- Survey code generation
- Smart delete logic (soft vs hard)
- Authorization at every level
- Comprehensive statistics
- Transaction support
- Eager loading to prevent N+1 queries
- Upsert pattern for Telegram users
- Type-specific validation

**Key Files** (with line counts):
- `SurveyBotDbContext.cs` - 108 lines
- `SurveyService.cs` - 724 lines
- `ResponseService.cs` - 623 lines
- `QuestionService.cs` - 462 lines
- `DataSeeder.cs` - 587 lines
- `SurveyRepository.cs` - 169 lines

**Total Project**: ~5,000+ lines of production code implementing robust data access and business logic.

---

**Last Updated**: 2025-11-10
**Version**: 1.0.0-MVP
**Target Framework**: .NET 8.0
**Database**: PostgreSQL 15+
**EF Core Version**: 9.0.10
