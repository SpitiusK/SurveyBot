# SurveyBot.Infrastructure - Data Access Layer Documentation

## Overview

**SurveyBot.Infrastructure** implements data access, database operations, and business logic services. This layer depends only on **SurveyBot.Core** and provides concrete implementations of interfaces defined in Core.

## Purpose

- Implement Entity Framework Core DbContext
- Implement repository interfaces from Core
- Implement service interfaces from Core
- Handle database migrations
- Manage data persistence
- Provide business logic services

## Dependencies

```
SurveyBot.Infrastructure
    └── SurveyBot.Core (domain layer)
    └── EntityFrameworkCore (NuGet)
    └── Npgsql.EntityFrameworkCore.PostgreSQL (NuGet)
    └── AutoMapper (NuGet)
    └── System.IdentityModel.Tokens.Jwt (NuGet)
```

---

## Project Structure

```
SurveyBot.Infrastructure/
├── Data/
│   ├── SurveyBotDbContext.cs           # Main DbContext
│   ├── DataSeeder.cs                   # Test data seeding
│   ├── Configurations/                 # Entity configurations
│   │   ├── UserConfiguration.cs
│   │   ├── SurveyConfiguration.cs
│   │   ├── QuestionConfiguration.cs
│   │   ├── ResponseConfiguration.cs
│   │   └── AnswerConfiguration.cs
│   └── Extensions/
│       └── DatabaseExtensions.cs       # Seeding extensions
├── Repositories/                       # Repository implementations
│   ├── GenericRepository.cs            # Base repository
│   ├── SurveyRepository.cs
│   ├── QuestionRepository.cs
│   ├── ResponseRepository.cs
│   ├── UserRepository.cs
│   └── AnswerRepository.cs
├── Services/                           # Business logic services
│   ├── AuthService.cs
│   ├── SurveyService.cs
│   ├── QuestionService.cs
│   ├── ResponseService.cs
│   └── UserService.cs
├── Migrations/                         # EF Core migrations
│   ├── 20251105190107_InitialCreate.cs
│   ├── 20251106000001_AddLastLoginAtToUser.cs
│   └── SurveyBotDbContextModelSnapshot.cs
└── DependencyInjection.cs              # DI registration (if used)
```

---

## Database Context

### SurveyBotDbContext

**Location**: `Data/SurveyBotDbContext.cs`

**Key Features**:
1. Automatic timestamp management (`CreatedAt`, `UpdatedAt`)
2. Entity configurations via Fluent API
3. PostgreSQL-specific features (JSONB)
4. Development-mode logging

**DbSets**:
```csharp
public DbSet<User> Users { get; set; }
public DbSet<Survey> Surveys { get; set; }
public DbSet<Question> Questions { get; set; }
public DbSet<Response> Responses { get; set; }
public DbSet<Answer> Answers { get; set; }
```

**OnModelCreating**:
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

**SaveChangesAsync Override**:
Automatically updates `CreatedAt` and `UpdatedAt` timestamps for entities inheriting from `BaseEntity`:

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

---

## Entity Configurations

All entity configurations use Fluent API and implement `IEntityTypeConfiguration<T>`.

### UserConfiguration

**Location**: `Data/Configurations/UserConfiguration.cs`

**Key Configurations**:
- Primary key: `Id`
- Unique index on `TelegramId`
- Index on `Username` for faster lookups
- Table name: `users`
- One-to-many relationship with Surveys

```csharp
builder.ToTable("users");
builder.HasKey(u => u.Id);

builder.Property(u => u.TelegramId).IsRequired();
builder.HasIndex(u => u.TelegramId).IsUnique();

builder.HasMany(u => u.Surveys)
    .WithOne(s => s.Creator)
    .HasForeignKey(s => s.CreatorId)
    .OnDelete(DeleteBehavior.Restrict);
```

### SurveyConfiguration

**Key Configurations**:
- Composite index on `CreatorId` and `IsActive`
- Cascade delete for Questions
- Cascade delete for Responses
- Default value for `IsActive`, `AllowMultipleResponses`, `ShowResults`

```csharp
builder.HasOne(s => s.Creator)
    .WithMany(u => u.Surveys)
    .HasForeignKey(s => s.CreatorId)
    .OnDelete(DeleteBehavior.Restrict);

builder.HasMany(s => s.Questions)
    .WithOne(q => q.Survey)
    .HasForeignKey(q => q.SurveyId)
    .OnDelete(DeleteBehavior.Cascade);

builder.HasMany(s => s.Responses)
    .WithOne(r => r.Survey)
    .HasForeignKey(r => r.SurveyId)
    .OnDelete(DeleteBehavior.Cascade);
```

### QuestionConfiguration

**Key Configurations**:
- JSONB column type for `OptionsJson` (PostgreSQL-specific)
- Index on `SurveyId` and `OrderIndex`
- Cascade delete for Answers

```csharp
builder.Property(q => q.OptionsJson)
    .HasColumnType("jsonb");

builder.HasIndex(q => new { q.SurveyId, q.OrderIndex });

builder.HasMany(q => q.Answers)
    .WithOne(a => a.Question)
    .HasForeignKey(a => a.QuestionId)
    .OnDelete(DeleteBehavior.Cascade);
```

### ResponseConfiguration

**Key Configurations**:
- Index on `SurveyId` and `RespondentTelegramId`
- Index on `IsComplete` for filtering
- `RespondentTelegramId` is NOT a foreign key (allows anonymous responses)

```csharp
builder.HasIndex(r => new { r.SurveyId, r.RespondentTelegramId });
builder.HasIndex(r => r.IsComplete);

builder.HasMany(r => r.Answers)
    .WithOne(a => a.Response)
    .HasForeignKey(a => a.ResponseId)
    .OnDelete(DeleteBehavior.Cascade);
```

### AnswerConfiguration

**Key Configurations**:
- JSONB column type for `AnswerJson`
- Unique constraint on `ResponseId` and `QuestionId` (one answer per question per response)

```csharp
builder.Property(a => a.AnswerJson)
    .HasColumnType("jsonb")
    .IsRequired();

builder.HasIndex(a => new { a.ResponseId, a.QuestionId })
    .IsUnique();
```

---

## Repository Implementations

### GenericRepository<T>

**Location**: `Repositories/GenericRepository.cs`

**Purpose**: Base repository implementation providing common CRUD operations.

**Key Members**:
```csharp
protected readonly SurveyBotDbContext _context;
protected readonly DbSet<T> _dbSet;

public virtual async Task<T?> GetByIdAsync(int id)
public virtual async Task<IEnumerable<T>> GetAllAsync()
public virtual async Task<T> AddAsync(T entity)
public virtual async Task<T> UpdateAsync(T entity)
public virtual async Task DeleteAsync(int id)
public virtual async Task<bool> ExistsAsync(int id)
```

**Pattern**:
- Uses `virtual` methods to allow overriding
- All methods are async
- Returns nullable types for Get operations
- Throws exceptions on not found for Update/Delete

### SurveyRepository

**Location**: `Repositories/SurveyRepository.cs`

**Specific Methods**:

1. **GetByIdWithQuestionsAsync**:
   ```csharp
   return await _dbSet
       .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
       .Include(s => s.Creator)
       .FirstOrDefaultAsync(s => s.Id == id);
   ```

2. **GetByIdWithDetailsAsync**:
   ```csharp
   return await _dbSet
       .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
       .Include(s => s.Creator)
       .Include(s => s.Responses)
           .ThenInclude(r => r.Answers)
       .FirstOrDefaultAsync(s => s.Id == id);
   ```

3. **GetActiveSurveysAsync**:
   ```csharp
   return await _dbSet
       .Include(s => s.Questions)
       .Include(s => s.Creator)
       .Where(s => s.IsActive)
       .OrderByDescending(s => s.CreatedAt)
       .ToListAsync();
   ```

4. **SearchByTitleAsync**:
   Uses PostgreSQL's case-insensitive LIKE:
   ```csharp
   .Where(s => EF.Functions.ILike(s.Title, $"%{searchTerm}%"))
   ```

**Override Pattern**:
Overrides base `GetByIdAsync` to always include Creator:
```csharp
public override async Task<Survey?> GetByIdAsync(int id)
{
    return await _dbSet
        .Include(s => s.Creator)
        .FirstOrDefaultAsync(s => s.Id == id);
}
```

### QuestionRepository

**Specific Methods**:

1. **GetBySurveyIdAsync**:
   ```csharp
   return await _dbSet
       .Where(q => q.SurveyId == surveyId)
       .OrderBy(q => q.OrderIndex)
       .ToListAsync();
   ```

2. **GetMaxOrderIndexAsync**:
   ```csharp
   var maxOrder = await _dbSet
       .Where(q => q.SurveyId == surveyId)
       .MaxAsync(q => (int?)q.OrderIndex);
   return maxOrder ?? -1;
   ```

3. **ReorderQuestionsAsync**:
   Bulk update question order indices:
   ```csharp
   foreach (var (questionId, newOrder) in newOrders)
   {
       var question = await GetByIdAsync(questionId);
       if (question != null && question.SurveyId == surveyId)
       {
           question.OrderIndex = newOrder;
       }
   }
   await _context.SaveChangesAsync();
   ```

### ResponseRepository

**Specific Methods**:

1. **GetIncompleteResponseAsync**:
   ```csharp
   return await _dbSet
       .Where(r => r.SurveyId == surveyId &&
                   r.RespondentTelegramId == telegramId &&
                   !r.IsComplete)
       .FirstOrDefaultAsync();
   ```

2. **GetResponseWithAnswersAsync**:
   ```csharp
   return await _dbSet
       .Include(r => r.Answers)
           .ThenInclude(a => a.Question)
       .Include(r => r.Survey)
       .FirstOrDefaultAsync(r => r.Id == responseId);
   ```

### UserRepository

**Specific Methods**:

1. **GetByTelegramIdAsync**:
   ```csharp
   return await _dbSet
       .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
   ```

2. **CreateOrUpdateFromTelegramAsync**:
   Upsert pattern for Telegram users:
   ```csharp
   var user = await GetByTelegramIdAsync(telegramUser.Id);

   if (user == null)
   {
       user = new User
       {
           TelegramId = telegramUser.Id,
           Username = telegramUser.Username,
           FirstName = telegramUser.FirstName,
           LastName = telegramUser.LastName
       };
       await AddAsync(user);
   }
   else
   {
       user.Username = telegramUser.Username;
       user.FirstName = telegramUser.FirstName;
       user.LastName = telegramUser.LastName;
       await UpdateAsync(user);
   }

   return user;
   ```

---

## Service Implementations

### AuthService

**Location**: `Services/AuthService.cs`

**Dependencies**:
- `IUserRepository`
- `IOptions<JwtSettings>`
- `ILogger<AuthService>`

**Key Methods**:

1. **AuthenticateAsync**:
   ```csharp
   public async Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto request)
   {
       var user = await _userRepository.GetByTelegramIdAsync(request.TelegramId);

       if (user == null)
       {
           user = await _userRepository.CreateOrUpdateFromTelegramAsync(
               new TelegramUser { /* ... */ });
       }

       await _userRepository.UpdateLastLoginAsync(user.Id);

       var token = GenerateJwtToken(user);
       var refreshToken = GenerateRefreshToken();

       return new LoginResponseDto
       {
           AccessToken = token,
           RefreshToken = refreshToken,
           ExpiresIn = _jwtSettings.ExpirationMinutes * 60,
           User = MapToUserDto(user)
       };
   }
   ```

2. **GenerateJwtToken**:
   ```csharp
   private string GenerateJwtToken(User user)
   {
       var claims = new[]
       {
           new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
           new Claim(ClaimTypes.Name, user.Username ?? user.TelegramId.ToString()),
           new Claim("telegram_id", user.TelegramId.ToString())
       };

       var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
       var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

       var token = new JwtSecurityToken(
           issuer: _jwtSettings.Issuer,
           audience: _jwtSettings.Audience,
           claims: claims,
           expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
           signingCredentials: credentials
       );

       return new JwtSecurityTokenHandler().WriteToken(token);
   }
   ```

### SurveyService

**Location**: `Services/SurveyService.cs`

**Dependencies**:
- `ISurveyRepository`
- `IQuestionRepository`
- `IResponseRepository`
- `ILogger<SurveyService>`

**Key Methods**:

1. **CreateSurveyAsync**:
   ```csharp
   public async Task<SurveyDto> CreateSurveyAsync(int userId, CreateSurveyDto dto)
   {
       var survey = new Survey
       {
           Title = dto.Title,
           Description = dto.Description,
           CreatorId = userId,
           IsActive = false, // Created as inactive
           AllowMultipleResponses = dto.AllowMultipleResponses,
           ShowResults = dto.ShowResults
       };

       var created = await _surveyRepository.AddAsync(survey);
       return MapToDto(created);
   }
   ```

2. **ActivateSurveyAsync**:
   ```csharp
   public async Task<SurveyDto> ActivateSurveyAsync(int surveyId, int userId)
   {
       var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);

       if (survey == null)
           throw new SurveyNotFoundException(surveyId);

       if (survey.CreatorId != userId)
           throw new UnauthorizedAccessException();

       if (survey.Questions.Count == 0)
           throw new SurveyValidationException("Cannot activate survey without questions");

       survey.IsActive = true;
       await _surveyRepository.UpdateAsync(survey);

       return MapToDto(survey);
   }
   ```

3. **GetSurveyStatisticsAsync**:
   Calculates comprehensive statistics:
   ```csharp
   public async Task<SurveyStatisticsDto> GetSurveyStatisticsAsync(int surveyId, int userId)
   {
       var survey = await _surveyRepository.GetByIdWithDetailsAsync(surveyId);

       // Authorization check
       if (survey.CreatorId != userId)
           throw new UnauthorizedAccessException();

       var totalResponses = survey.Responses.Count;
       var completedResponses = survey.Responses.Count(r => r.IsComplete);
       var completionRate = totalResponses > 0
           ? (double)completedResponses / totalResponses * 100
           : 0;

       // Calculate question-level statistics
       var questionStats = survey.Questions.Select(q =>
           CalculateQuestionStatistics(q, survey.Responses)).ToList();

       return new SurveyStatisticsDto
       {
           SurveyId = surveyId,
           TotalResponses = totalResponses,
           CompletedResponses = completedResponses,
           CompletionRate = completionRate,
           Questions = questionStats
       };
   }
   ```

### QuestionService

**Key Business Logic**:

1. **Automatic OrderIndex Assignment**:
   ```csharp
   private async Task<int> GetNextOrderIndexAsync(int surveyId)
   {
       var maxIndex = await _questionRepository.GetMaxOrderIndexAsync(surveyId);
       return maxIndex + 1;
   }
   ```

2. **Question Type Validation**:
   ```csharp
   private void ValidateQuestionOptions(CreateQuestionDto dto)
   {
       if (dto.QuestionType == QuestionType.MultipleChoice ||
           dto.QuestionType == QuestionType.SingleChoice)
       {
           if (dto.Options == null || dto.Options.Count < 2)
               throw new QuestionValidationException(
                   "Choice questions must have at least 2 options");
       }
   }
   ```

### ResponseService

**Key Features**:

1. **Duplicate Response Prevention**:
   ```csharp
   public async Task<ResponseDto> StartResponseAsync(long telegramId, int surveyId)
   {
       var survey = await _surveyRepository.GetByIdAsync(surveyId);

       if (!survey.AllowMultipleResponses)
       {
           var existing = await _responseRepository
               .GetByRespondentAsync(telegramId, surveyId);

           if (existing != null)
               throw new DuplicateResponseException();
       }

       var response = new Response
       {
           SurveyId = surveyId,
           RespondentTelegramId = telegramId,
           StartedAt = DateTime.UtcNow,
           IsComplete = false
       };

       return await _responseRepository.AddAsync(response);
   }
   ```

2. **Answer Validation**:
   ```csharp
   private void ValidateAnswer(Question question, SubmitAnswerDto dto)
   {
       switch (question.QuestionType)
       {
           case QuestionType.Text:
               if (string.IsNullOrWhiteSpace(dto.TextAnswer))
                   throw new InvalidAnswerFormatException("Text answer required");
               break;

           case QuestionType.SingleChoice:
               if (string.IsNullOrWhiteSpace(dto.SelectedOption))
                   throw new InvalidAnswerFormatException("Option selection required");
               break;

           // ... other types
       }
   }
   ```

---

## Database Migrations

### Migration Commands

From `SurveyBot.API` directory:

```bash
# Add new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script

# Generate script from-to
dotnet ef migrations script FromMigration ToMigration
```

### Migration Structure

**InitialCreate** (20251105190107):
- Creates all tables: users, surveys, questions, responses, answers
- Sets up foreign keys and relationships
- Creates indexes for performance
- Configures JSONB columns for PostgreSQL

**AddLastLoginAtToUser** (20251106000001):
- Adds `last_login_at` column to users table
- Nullable DateTime type

### Best Practices

1. **Always backup database before migration**
2. **Test migrations on development database first**
3. **Review generated SQL script**:
   ```bash
   dotnet ef migrations script > migration.sql
   ```
4. **Never edit applied migrations**
5. **Use descriptive migration names**

---

## Data Seeding

### DataSeeder Class

**Location**: `Data/DataSeeder.cs`

**Purpose**: Seed test data for development

**Usage**:
```csharp
// In Program.cs or startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
    await DataSeeder.SeedAsync(context);
}
```

**Seed Data Includes**:
- Test users with Telegram IDs
- Sample surveys (active and inactive)
- Questions of different types
- Sample responses and answers

**Important**: Only seed in Development environment!

---

## Best Practices

### Repository Layer

1. **Always use async methods**
2. **Include related entities when needed** via `.Include()`
3. **Order collections** (e.g., Questions by OrderIndex)
4. **Return null for not found** (let service decide to throw)
5. **Use specific return types** (avoid object)

### Service Layer

1. **Validate inputs** before database operations
2. **Throw domain exceptions** for business rule violations
3. **Check authorization** (user owns resource)
4. **Use repositories** for data access, never DbContext directly
5. **Log important operations**

### Entity Configuration

1. **Use Fluent API** over data annotations
2. **Configure indexes** for frequently queried columns
3. **Set cascade delete** appropriately
4. **Use JSONB** for flexible JSON storage (PostgreSQL)
5. **Set default values** where appropriate

### Performance

1. **Use AsNoTracking()** for read-only queries
2. **Avoid N+1 queries** with `.Include()`
3. **Use pagination** for large result sets
4. **Create indexes** on foreign keys and filter columns
5. **Use compiled queries** for frequently executed queries

---

## Common Issues

### Issue: DbContext lifetime
**Problem**: DbContext disposed before lazy-loaded navigation properties accessed
**Solution**: Use `.Include()` to eager load, or use scoped DbContext

### Issue: Circular references in JSON
**Problem**: Entity navigation properties cause infinite loops
**Solution**: Use DTOs, not entities, in API responses

### Issue: Concurrent updates
**Problem**: Two users update same entity simultaneously
**Solution**: Use optimistic concurrency with `[Timestamp]` attribute

### Issue: JSONB queries
**Problem**: Can't query inside JSONB columns easily
**Solution**: Use PostgreSQL JSON operators or extract to separate columns

---

**Key Takeaway**: Infrastructure layer implements the "HOW" - it's where domain concepts meet actual database and external services. Keep it focused on data access and defer business logic to services when possible.
