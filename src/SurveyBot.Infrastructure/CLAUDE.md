# SurveyBot.Infrastructure - Data Access Layer

**Version**: 1.6.2 | **Target Framework**: .NET 8.0 | **EF Core**: 9.0.10 | **Database**: PostgreSQL 15+

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

**DbSets** (6 entities):
```csharp
public DbSet<User> Users { get; set; }
public DbSet<Survey> Surveys { get; set; }
public DbSet<Question> Questions { get; set; }
public DbSet<QuestionOption> QuestionOptions { get; set; }  // NEW in v1.4.0
public DbSet<Response> Responses { get; set; }
public DbSet<Answer> Answers { get; set; }
```

**Configuration Pattern**: `IEntityTypeConfiguration<T>` (Fluent API)
- 6 separate configuration classes in `Data/Configurations/`
- Applied via `modelBuilder.ApplyConfigurationsFromAssembly()` in `OnModelCreating`
- Keeps DbContext clean, promotes separation of concerns

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

**Benefits**:
- Developers never manually set timestamps - context handles automatically
- Consistent timezone handling (UTC everywhere)
- Prevents timestamp drift or forgotten updates
- **Performance**: Only queries ChangeTracker for Added/Modified entities (no overhead for reads)

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
- Check constraint: `question_type IN ('text', 'multiple_choice', 'single_choice', 'rating', 'yes_no', 'location')`
- **NEW in v1.4.0 - Conditional Flow**:
  - DefaultNextQuestionId: Optional FK to next question (for non-branching)
  - Relationship to Options collection (one-to-many)
  - SupportsBranching computed property (not mapped)
- **REFACTORED in v1.4.1 - Owned Type Mapping for Value Objects**:
  - DefaultNext: NextQuestionDeterminant (owned type, not separate entity)
  - Maps to columns: `default_next_step_type` (enum as string), `default_next_question_id` (int?)
  - Uses `HasConversion<string>()` to persist enum as text
  - CHECK constraints enforce invariants (see "Value Object Persistence" section)

**QuestionOptionConfiguration** - `Data/Configurations/QuestionOptionConfiguration.cs` (NEW in v1.4.0, REFACTORED in v1.4.1):
- Table: `question_options`
- PK: Id (auto-increment)
- FK to Question: `question_id` (cascade delete)
- **REFACTORED in v1.4.1**: Next: NextQuestionDeterminant (owned type)
  - Maps to columns: `next_step_type` (enum as string), `next_question_id` (int?)
  - Uses `HasConversion<string>()` to persist enum as text
  - CHECK constraint enforces invariants (GoToQuestion requires ID > 0, EndSurvey requires null)
- Composite unique index: `(QuestionId, OrderIndex)`
- Composite index: `(QuestionId, OrderIndex)` for ordered retrieval
- Check constraint: `order_index >= 0`

**Owned Type Configuration**:
```csharp
builder.OwnsOne(qo => qo.Next, nb =>
{
    nb.Property(n => n.Type)
        .HasColumnName("next_step_type")
        .HasConversion<string>()
        .IsRequired();
    nb.Property(n => n.NextQuestionId)
        .HasColumnName("next_question_id")
        .IsRequired(false);
});
```

**ResponseConfiguration** - `Data/Configurations/ResponseConfiguration.cs`:
- RespondentTelegramId: **NOT a foreign key** (allows anonymous responses)
- Composite index: `(SurveyId, RespondentTelegramId)` for duplicate checking
- IsComplete: Filtered index (only completed)
- **NEW in v1.4.0 - Conditional Flow**:
  - VisitedQuestionIds: JSONB array for cycle prevention
  - GIN index for efficient JSON queries
  - Default value: `'[]'::jsonb` (empty array)
  - Conversion: List<int> ↔ JSON serialization

**AnswerConfiguration** - `Data/Configurations/AnswerConfiguration.cs`:
- AnswerText: TEXT column (legacy, for Text question answers)
- AnswerJson: JSONB with GIN index (legacy, for structured answers)
- **NEW v1.5.0 - AnswerValue Polymorphic Value Object**:
  - Value: AnswerValue? stored as JSONB in `answer_value_json` column
  - Uses System.Text.Json polymorphic serialization with type discriminators
  - HasConversion: Serialize/deserialize with automatic type detection
  - GIN index on `answer_value_json` for efficient JSON queries
  - **Polymorphic Mapping**: Automatic routing to TextAnswerValue, SingleChoiceAnswerValue, MultipleChoiceAnswerValue, RatingAnswerValue, LocationAnswerValue
  - **Type Safety**: JSON discriminator ($type) ensures correct deserialization
- Composite unique index: `(ResponseId, QuestionId)` (one answer per question)
- **Check constraint** (updated v1.5.0): `answer_text IS NOT NULL OR answer_json IS NOT NULL OR answer_value_json IS NOT NULL`
- **COMPLETED v1.4.2 - Answer.Next Value Object**:
  - Next: NextQuestionDeterminant (owned type) - FULLY MIGRATED
  - Maps to columns: `next_step_type` (TEXT), `next_step_question_id` (INT)
  - CHECK constraint enforces invariants (GoToQuestion requires ID > 0, EndSurvey requires null)
  - Migration 20251126180649_AnswerNextStepValueObject with data transformation
  - **No more magic values**: All conditional flow entities now use value object pattern consistently

---

## Value Object Persistence (DDD - Domain-Driven Design)

### Owned Type Mapping Pattern

In v1.4.1, we refactored conditional flow from primitive `int?` columns to a **value object** (`NextQuestionDeterminant`) following DDD principles. This provides type-safety and enforces invariants at the database level.

**Problem with Primitive Approach**:
```csharp
// OLD (v1.4.0) - Magic number 0 for "end survey"
public int NextQuestionId { get; set; }  // 0 = end, >0 = question ID
// Problems:
// - Semantically unclear (what does 0 mean?)
// - No type safety (any int can be assigned)
// - Invariant: 0 is reserved, but compiler doesn't enforce
```

**Solution with Value Object**:
```csharp
// NEW (v1.4.1) - Type-safe with explicit semantics
public NextQuestionDeterminant DefaultNext { get; set; }
// Immutable value object with factory methods:
// - NextQuestionDeterminant.ToQuestion(5) → Navigate to question 5
// - NextQuestionDeterminant.End() → End the survey
```

### EF Core Owned Type Configuration

**Configuration in QuestionConfiguration.cs**:
```csharp
// Configure DefaultNext as owned type (embedded value object)
builder.OwnsOne(q => q.DefaultNext, nb =>
{
    // Map enum to string column for explicit semantics
    nb.Property(n => n.Type)
        .HasColumnName("default_next_step_type")
        .HasConversion<string>()  // Store "GoToQuestion" or "EndSurvey"
        .IsRequired();

    // Map nullable ID column
    nb.Property(n => n.NextQuestionId)
        .HasColumnName("default_next_question_id")
        .IsRequired(false);  // Nullable (null when EndSurvey)
});
```

**Database Schema** (PostgreSQL):
```sql
-- Questions table with value object columns
CREATE TABLE questions (
    id INT PRIMARY KEY,
    survey_id INT NOT NULL,
    question_text TEXT NOT NULL,
    -- ... other columns ...

    -- Value object columns (owned type)
    default_next_step_type TEXT,           -- 'GoToQuestion' or 'EndSurvey'
    default_next_question_id INT,          -- Target question (null if EndSurvey)

    -- CHECK constraints enforce invariants
    CONSTRAINT chk_question_default_next_invariant CHECK (
        (default_next_step_type IS NULL AND default_next_question_id IS NULL) OR
        (default_next_step_type = 'GoToQuestion' AND default_next_question_id IS NOT NULL AND default_next_question_id > 0) OR
        (default_next_step_type = 'EndSurvey' AND default_next_question_id IS NULL)
    )
);
```

**Key Benefits**:
1. **Type Safety**: Value object enforces invariants at compile-time
2. **Database Constraints**: CHECK constraint prevents invalid states at database level
3. **Semantic Clarity**: "GoToQuestion(5)" vs magic 0/5 values
4. **Immutability**: Value objects are immutable, preventing accidental changes
5. **No Separate Table**: Owned types are embedded, not separate entities

### Query Loading with Owned Types

**Loading Questions with Owned Navigation**:
```csharp
var question = await _context.Questions
    .Include(q => q.DefaultNext)  // Automatically included (owned type)
    .FirstOrDefaultAsync(q => q.Id == questionId);

// Access the value object
if (question?.DefaultNext?.Type == NextStepType.GoToQuestion)
{
    var nextId = question.DefaultNext.NextQuestionId.Value;
}
```

**Important Notes**:
- Owned types are **automatically loaded** (no explicit Include needed for simple queries)
- Owned types are **stored in the same table** (no JOIN overhead)
- Owned types **cannot be null** in C# (use `DefaultNext == null` for "no next question defined")
- Database columns CAN be null (when no next question is configured)

**Serialization to JSON** (automatic via System.Text.Json):
```json
{
  "id": 1,
  "questionText": "Do you like surveys?",
  "defaultNext": {
    "type": "GoToQuestion",
    "nextQuestionId": 2
  }
}
```

### Architecture Decision Records (ADRs)

**ADR-001: Why Owned Types for Value Objects?**

**Decision**: Use EF Core owned types (`OwnsOne`) for NextQuestionDeterminant instead of separate entity table.

**Rationale**:
1. **No JOIN Overhead**: Owned types are embedded in the same table, avoiding JOIN queries
2. **Atomic Updates**: Update question and flow configuration in single database operation
3. **Referential Locality**: Flow data is always loaded with question (no lazy loading issues)
4. **Type Safety**: Value object invariants enforced at both C# and database levels
5. **Simpler Schema**: 2 extra columns vs. separate table with FK management

**Trade-offs**:
- **Pro**: Faster queries (no JOINs), simpler schema, atomic updates
- **Con**: Cannot query owned type independently (acceptable - always used with parent)

**ADR-002: Why SET NULL on FK Delete?**

**Decision**: Use `ON DELETE SET NULL` for NextQuestionId foreign keys instead of CASCADE or RESTRICT.

**Rationale**:
1. **Graceful Degradation**: If target question deleted, survey doesn't break - just ends flow
2. **User Safety**: Prevents accidental survey corruption from deleting intermediate questions
3. **Data Integrity**: NULL is a valid state (means "no next question defined" or "end survey")
4. **Flexibility**: Allows administrators to delete problematic questions without blocking

**Trade-offs**:
- **Pro**: Survey remains functional after question deletion
- **Con**: Application must handle NULL (interpret as "end survey")

**Example**: Survey Q1 → Q2 → Q3. If Q2 is deleted, Q1.DefaultNext becomes NULL (survey ends after Q1).

**ADR-003: Why JSONB for VisitedQuestionIds?**

**Decision**: Use PostgreSQL JSONB column for Response.VisitedQuestionIds instead of junction table.

**Rationale**:
1. **Dynamic Size**: List grows as user answers questions (no fixed schema)
2. **Efficient Indexing**: GIN index supports containment queries (`WHERE visited_ids @> '[5]'`)
3. **Write Efficiency**: Single UPDATE vs. multiple INSERT/DELETE operations
4. **Read Efficiency**: Single column vs. JOIN with junction table
5. **PostgreSQL Native**: JSONB is first-class data type with excellent performance

**Trade-offs**:
- **Pro**: Simpler schema, faster writes, efficient queries with GIN index
- **Con**: Requires PostgreSQL-specific features (no MySQL/SQLite portability)

**Usage**:
```sql
-- Check if question already visited
SELECT * FROM responses WHERE visited_question_ids @> '[3]'::jsonb;

-- Add question to visited list (application level)
UPDATE responses SET visited_question_ids = visited_question_ids || '[5]'::jsonb;
```

**ADR-004: Why HasConversion for AnswerValue instead of Owned Type?** (NEW v1.5.0)

**Decision**: Use EF Core `HasConversion` with polymorphic JSON serialization for AnswerValue instead of owned type pattern.

**Rationale**:
1. **Polymorphism**: AnswerValue is an abstract base class with 5 concrete implementations - owned types don't support polymorphism
2. **Type Discriminator**: System.Text.Json [JsonDerivedType] provides automatic type resolution during deserialization
3. **Flexibility**: Adding new answer types (e.g., DateAnswerValue) requires no database migration, just new value object class
4. **Single Column**: All answer types stored in one `answer_value_json` JSONB column with type information embedded
5. **Backward Compatibility**: Legacy `answer_text` and `answer_json` columns remain for migration period

**Implementation**:
```csharp
builder.Property(a => a.Value)
    .HasColumnName("answer_value_json")
    .HasColumnType("jsonb")
    .HasConversion(
        v => v != null ? JsonSerializer.Serialize(v, options) : null,
        json => !string.IsNullOrWhiteSpace(json)
            ? JsonSerializer.Deserialize<AnswerValue>(json, options)
            : null);
```

**Database Storage** (JSONB with type discriminator):
```json
{
  "$type": "SingleChoice",
  "selectedOption": "Option A",
  "selectedOptionIndex": 0
}
```

**Trade-offs**:
- **Pro**: Polymorphic support, easy extensibility, single column, automatic type resolution
- **Con**: Cannot query individual value object properties directly (need JSONB operators like `->`, `->>`)

**Migration Strategy**:
- `answer_value_json` column added alongside legacy columns
- Migration 20251127104737_AddAnswerValueJsonColumn preserves existing data
- Services gradually migrate to using Value property instead of AnswerText/AnswerJson
- Legacy columns can be dropped in v2.0.0 after full migration

---

## Conditional Flow Architecture (v1.4.1)

### Design Overview

**Purpose**: Enable creation of branching surveys where different questions appear based on previous answers, without cycles.

**Key Components**:
1. **NextQuestionDeterminant** (Core/ValueObjects) - Type-safe navigation marker
2. **Question.DefaultNext** (Core/Entities) - For non-branching questions
3. **QuestionOption.Next** (Core/Entities) - For branching (SingleChoice, Rating)
4. **SurveyValidationService** (Infrastructure/Services) - Cycle prevention
5. **ResponseService** (Infrastructure/Services) - Flow execution at runtime

### NextQuestionDeterminant Value Object

**Location**: `Core/ValueObjects/NextQuestionDeterminant.cs`

**Purpose**: Immutable value object representing "where to go next" after answering a question.

**Factory Methods** (enforce invariants):
```csharp
// Navigate to specific question
NextQuestionDeterminant.ToQuestion(5)
// → Type: GoToQuestion, NextQuestionId: 5

// End the survey
NextQuestionDeterminant.End()
// → Type: EndSurvey, NextQuestionId: null
```

**Invariants** (enforced by constructor):
```csharp
// GoToQuestion REQUIRES NextQuestionId > 0
NextQuestionDeterminant.ToQuestion(0)  // Throws ArgumentException

// EndSurvey FORBIDS NextQuestionId
var d = NextQuestionDeterminant.End();
d.NextQuestionId = 5;  // Read-only property, cannot set

// These are enforced at compile-time (sealed class, immutable)
```

**Equality** (value semantics):
```csharp
var det1 = NextQuestionDeterminant.ToQuestion(5);
var det2 = NextQuestionDeterminant.ToQuestion(5);

det1 == det2  // true (value semantics, not reference)
det1.Equals(det2)  // true
```

### Question Flow Types

**Type 1: Non-Branching Questions** (Text, MultipleChoice)
- All answers lead to same next question
- Configured via `Question.DefaultNext`
- Example: "Text to Question 3" (all text answers go to Q3)

**Type 2: Branching Questions** (SingleChoice, Rating)
- Different options lead to different next questions
- Configured via individual `QuestionOption.Next`
- Example: Q1 (SingleChoice) "Favorite color?"
  - Option "Red" → Q2
  - Option "Blue" → Q3
  - Option "Green" → End Survey

### Validation: Cycle Detection

**Purpose**: Ensure survey has no infinite loops before activation

**Location**: `SurveyValidationService.DetectCycleAsync(surveyId)`

**Algorithm**: Depth-First Search (DFS) with:
- **visited set**: Track explored questions
- **recursion stack**: Track current path (detect back-edges)
- **Time complexity**: O(V + E) where V=questions, E=flow edges
- **Space complexity**: O(V) for recursion depth

**Cycle Detection Logic**:
```csharp
public async Task<CycleDetectionResult> DetectCycleAsync(int surveyId)
{
    var questions = await _context.Questions
        .Where(q => q.SurveyId == surveyId)
        .Include(q => q.Options)
        .ToListAsync();

    var visited = new HashSet<int>();
    var recursionStack = new Stack<int>();

    foreach (var question in questions)
    {
        if (!visited.Contains(question.Id))
        {
            // DFS from each unvisited question
            var path = await DfsAsync(question.Id, visited, recursionStack, questions);
            if (path != null)
            {
                return new CycleDetectionResult
                {
                    HasCycle = true,
                    CyclePath = path  // E.g., [1, 2, 3, 1]
                };
            }
        }
    }

    return new CycleDetectionResult { HasCycle = false };
}
```

**Integration**:
- Called by `SurveyService.ActivateSurveyAsync()` before activation
- Throws `SurveyCycleException` with cycle path if found
- Prevents users from publishing broken surveys

### Clean Slate Migration Strategy

**Why Clean Slate?**

The v1.4.1 migration transitions from:
- **Before**: Primitive `int?` columns with magic 0 value
- **After**: Owned type (NextQuestionDeterminant) with explicit enum and CHECK constraints

This required **complete data truncation** because:
1. **Data format incompatibility**: Old `int?` → New `(enum, int?)` tuple
2. **No meaningful data conversion**: Mock data had incomplete flow definitions
3. **Development stage**: Feature still in active development, no production data
4. **Clean state preferable**: Ensures all flows use new pattern from ground up

**What Gets Deleted**:
```sql
TRUNCATE TABLE answers RESTART IDENTITY CASCADE;
TRUNCATE TABLE responses RESTART IDENTITY CASCADE;
TRUNCATE TABLE question_options RESTART IDENTITY CASCADE;
TRUNCATE TABLE questions RESTART IDENTITY CASCADE;
TRUNCATE TABLE surveys RESTART IDENTITY CASCADE;
TRUNCATE TABLE users RESTART IDENTITY CASCADE;
```

**Why These Tables**:
- **answers, responses, question_options**: Depend on questions
- **questions, surveys**: Directly affected by schema change
- **users**: Cascade delete via surveys

**Constraints Maintained**:
- Cascade deletes remain (orphan cleanup)
- Foreign key relationships rebuilt
- All indexes recreated

### CHECK Constraints for Invariant Enforcement

**Purpose**: Database-level validation of value object invariants

**Question.DefaultNext Invariant**:
```sql
ALTER TABLE questions ADD CONSTRAINT chk_question_default_next_invariant
CHECK (
    -- Case 1: Null (no default next defined)
    (default_next_step_type IS NULL AND default_next_question_id IS NULL) OR
    -- Case 2: GoToQuestion with valid ID
    (default_next_step_type = 'GoToQuestion' AND default_next_question_id IS NOT NULL AND default_next_question_id > 0) OR
    -- Case 3: EndSurvey with null ID
    (default_next_step_type = 'EndSurvey' AND default_next_question_id IS NULL)
);
```

**QuestionOption.Next Invariant**:
```sql
ALTER TABLE question_options ADD CONSTRAINT chk_question_option_next_invariant
CHECK (
    (next_step_type IS NULL AND next_question_id IS NULL) OR
    (next_step_type = 'GoToQuestion' AND next_question_id IS NOT NULL AND next_question_id > 0) OR
    (next_step_type = 'EndSurvey' AND next_question_id IS NULL)
);
```

**Benefits**:
1. **Database prevents corruption**: Invalid states rejected at database layer
2. **Defensive coding**: Even buggy code cannot create inconsistent state
3. **Documentation**: CHECK constraint documents expected states
4. **Performance**: Validation happens before insert/update

### Foreign Key Constraints for Referential Integrity

**Question.DefaultNextQuestionId → questions.id**:
```sql
ALTER TABLE questions
ADD CONSTRAINT fk_questions_default_next_question
FOREIGN KEY (default_next_question_id)
REFERENCES questions(id)
ON DELETE SET NULL;  -- If target question deleted, set to null
```

**QuestionOption.NextQuestionId → questions.id**:
```sql
ALTER TABLE question_options
ADD CONSTRAINT fk_question_options_next_question
FOREIGN KEY (next_question_id)
REFERENCES questions(id)
ON DELETE SET NULL;  -- If target question deleted, set to null
```

**Why SET NULL?**:
- If a question is deleted, referencing flows become null (valid state)
- Application must handle null → allows survey to end gracefully
- Better than CASCADE DELETE (would delete option) or RESTRICT (would block deletion)

**Indexes for Performance**:
```sql
CREATE INDEX idx_questions_default_next_question_id
    ON questions(default_next_question_id);

CREATE INDEX idx_question_options_next_question_id
    ON question_options(next_question_id);
```

These indexes accelerate:
- Finding questions that reference another (cycle detection)
- Flow navigation during survey execution
- Analytics on question relationships

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
- `GetBySurveyIdAsync`: Ordered questions with eager loading
- `GetNextOrderIndexAsync`: Auto-increment order
- `ReorderQuestionsAsync`: Bulk reordering with transaction
- **NEW in v1.4.0 - Conditional Flow Support**:
  - `GetWithFlowConfigurationAsync(surveyId)`: Load questions with Options and DefaultNext
  - Uses **layered eager loading**: `Include(q => q.Options).ThenInclude(o => o.Next)`
  - AsNoTracking for read-only flow navigation queries

**Eager Loading Pattern Example**:
```csharp
public async Task<List<Question>> GetWithFlowConfigurationAsync(int surveyId)
{
    return await _dbSet
        .AsNoTracking()  // Read-only query
        .Where(q => q.SurveyId == surveyId)
        .Include(q => q.Options.OrderBy(o => o.OrderIndex))  // Load options
            .ThenInclude(o => o.Next)  // Load owned type (redundant but explicit)
        .Include(q => q.DefaultNext)  // Load default flow
        .OrderBy(q => q.OrderIndex)
        .ToListAsync();
}
```

**Performance Notes**:
- Single database query (no N+1 problem)
- AsNoTracking reduces memory overhead for read-only scenarios
- Ordered collections prevent client-side sorting

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

### SurveyValidationService (NEW in v1.4.0)

**Location**: `Services/SurveyValidationService.cs`

**Purpose**: Implement cycle detection and survey structure validation for conditional question flow

**Key Methods**:

```csharp
// DFS-based cycle detection, O(V+E) complexity
public async Task<CycleDetectionResult> DetectCycleAsync(int surveyId)

// Check no cycles + has endpoints
public async Task<bool> ValidateSurveyStructureAsync(int surveyId)

// Find questions that lead to survey completion
public async Task<List<int>> FindSurveyEndpointsAsync(int surveyId)
```

**Algorithm**:
- **DFS with visited set + recursion stack** for cycle detection
- Handles both branching and non-branching questions
- Treats 0 (EndOfSurvey) as terminal node

**Performance**:
- Time: O(V + E) where V = questions, E = flow edges
- Space: O(V) for visited tracking
- Typical survey (5-10 questions): <1ms

**Integration**:
- Called by `SurveyService.ActivateAsync()` before activation
- Prevents users from activating surveys with cycles
- Returns detailed cycle path for debugging

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

**ActivateSurveyAsync** (UPDATED in v1.4.0):
- **NEW**: Validates survey has no cycles using ISurveyValidationService
- **NEW**: Validates survey has at least one endpoint (question leading to end)
- **NEW**: Throws SurveyCycleException with cycle path if cycle detected
- Sets IsActive=true only if validation passes
- Only survey creator can activate
- Logs cycle detection attempts and validation success

**UpdateSurveyWithQuestionsAsync** (NEW in v1.5.2):

**Signature**:
```csharp
Task<SurveyDto> UpdateSurveyWithQuestionsAsync(
    int surveyId,
    int userId,
    UpdateSurveyWithQuestionsDto dto,
    CancellationToken cancellationToken = default)
```

**Purpose**: Completely replaces survey metadata and all questions in a single atomic transaction using a three-pass algorithm.

**Three-Pass Algorithm**:

1. **PASS 1 - Delete & Create**:
   - Delete all existing questions (cascades to options, answers, responses)
   - Create new questions from DTO
   - Store mapping of OrderIndex → Database ID

2. **PASS 2 - Transform Flow**:
   - Convert index-based references to database ID references
   - For each question's DefaultNextQuestionIndex → DefaultNextQuestionId
   - For each SingleChoice option's NextQuestionIndex → NextQuestionId
   - Handle special values: null (sequential), -1 (end survey)

3. **PASS 3 - Validate & Commit**:
   - Run cycle detection via ISurveyValidationService
   - Activate survey if isActive=true and no cycles
   - Commit transaction

**Index Reference Convention**:
| Index Value | Meaning | Transforms To |
|-------------|---------|---------------|
| null | Sequential flow | DefaultNextQuestionId = null |
| -1 | End survey | NextQuestionDeterminant.End() |
| 0+ | Goto question | NextQuestionDeterminant.ToQuestion(dbId) |

**Transaction Behavior**:
- Entire operation runs in single database transaction
- If any step fails, all changes are rolled back
- No partial updates possible

**Dependencies Injected**:
- ISurveyRepository
- IQuestionRepository
- ISurveyValidationService
- SurveyBotDbContext (for transactions)
- IMapper

**Exceptions**:
- SurveyNotFoundException - Survey doesn't exist
- UnauthorizedAccessException - User doesn't own survey
- SurveyValidationException - Empty questions, invalid indexes
- SurveyCycleException - Flow creates infinite loop

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

### ResponseService (UPDATED in v1.6.2)

**Location**: `Services/ResponseService.cs`

**Existing Methods**:
- `StartResponseAsync`: Begin survey
- `SaveAnswerAsync`: Save/update answer with validation
- `CompleteResponseAsync`: Finalize response
- Answer validation methods for each question type

**UPDATED Methods in v1.4.2** (Renamed for Value Object Consistency):

```csharp
// NEW NAMES (v1.4.2) - Return value objects instead of primitive int
public async Task<NextQuestionDeterminant> DetermineNextStepAsync(int responseId, int questionId, string answerValue)
public async Task<NextQuestionDeterminant> DetermineBranchingNextStepAsync(Question question, string selectedOption)
public async Task<NextQuestionDeterminant> DetermineNonBranchingNextStepAsync(Question question)

// Record visited question to prevent re-answering
public async Task RecordVisitedQuestionAsync(int responseId, int questionId)

// Get next question based on flow configuration
public async Task<int?> GetNextQuestionAsync(int responseId)
```

**Key Features**:
- `DetermineNextStepAsync`: Returns NextQuestionDeterminant (not int) for type safety
- `RecordVisitedQuestionAsync`: Adds question to response.VisitedQuestionIds (idempotent)
- `GetNextQuestionAsync`:
  - Returns null if survey complete (is_completed = true)
  - Checks last answer's Next value object (not magic 0)
  - Checks `answer.Next.Type == NextStepType.EndSurvey` to mark complete
  - Used by API/Bot to get next question after answer

**Flow Integration**:
- Called after each answer to determine next question
- Marks survey complete when `answer.Next.Type == NextStepType.EndSurvey`
- Thread-safe with database transaction
- **No more magic values**: Uses type-safe value object checks throughout

**FIXED in v1.6.2 (INFRA-FIX-001): DetermineNonBranchingNextStepAsync EndSurvey Bug**:

**Problem**: Non-branching questions (Rating, Text, Number, Date, Location) ignored `DefaultNext = EndSurvey` configuration
- Method only checked for `NextStepType.GoToQuestion` type
- Fell through to sequential fallback even when EndSurvey was configured
- Caused surveys to continue instead of ending when configured to end

**Root Cause Analysis** (lines 1116-1136):
```csharp
// OLD CODE (v1.6.1 and earlier) - BUGGY
private async Task<NextQuestionDeterminant> DetermineNonBranchingNextStepAsync(Question question)
{
    // Check if question has explicit next configured
    if (question.DefaultNext?.Type == NextStepType.GoToQuestion)
    {
        return question.DefaultNext;
    }
    // BUG: No check for EndSurvey - falls through to sequential logic!

    // Sequential fallback (get next by OrderIndex)
    var nextQuestion = await GetNextQuestionByOrderAsync(question.SurveyId, question.OrderIndex);
    return nextQuestion != null
        ? NextQuestionDeterminant.ToQuestion(nextQuestion.Id)
        : NextQuestionDeterminant.End();
}
```

**Solution**: Added explicit EndSurvey check before sequential fallback
```csharp
// NEW CODE (v1.6.2) - FIXED
private async Task<NextQuestionDeterminant> DetermineNonBranchingNextStepAsync(Question question)
{
    // Priority 1: Check for explicit EndSurvey configuration
    if (question.DefaultNext?.Type == NextStepType.EndSurvey)
    {
        return NextQuestionDeterminant.End();
    }

    // Priority 2: Check for explicit GoToQuestion configuration
    if (question.DefaultNext?.Type == NextStepType.GoToQuestion)
    {
        return question.DefaultNext;
    }

    // Priority 3: Sequential fallback (only if DefaultNext is null)
    var nextQuestion = await GetNextQuestionByOrderAsync(question.SurveyId, question.OrderIndex);
    return nextQuestion != null
        ? NextQuestionDeterminant.ToQuestion(nextQuestion.Id)
        : NextQuestionDeterminant.End();
}
```

**Impact**:
- **Affected Question Types**: Rating, Text, Number, Date, Location
- **Before**: These questions always used sequential flow (ignored EndSurvey)
- **After**: These questions correctly end survey when configured with EndSurvey
- **Priority Order**: EndSurvey → GoToQuestion → Sequential fallback
- **Backward Compatibility**: Existing surveys with null DefaultNext still work (sequential)

**Example Scenario**:
```csharp
// Survey with 3 questions: Q1 (Text), Q2 (Rating), Q3 (Text)
// Configuration: Q2.DefaultNext = EndSurvey (rating determines satisfaction, end if low)

// OLD BEHAVIOR (BUGGY):
// User answers Q1 → Q2 (Rating: 2/5) → Q3 (BUG: continues to Q3 instead of ending)

// NEW BEHAVIOR (FIXED):
// User answers Q1 → Q2 (Rating: 2/5) → Survey Complete ✓
```

### Other Services

**QuestionService** (`Services/QuestionService.cs`):
- `AddQuestionAsync`: Type-specific validation
- `UpdateQuestionAsync`: Protection rules
- `ReorderQuestionsAsync`: Change question order

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
5. **AddConditionalQuestionFlow** - Adds QuestionOption entity and flow columns (v1.4.0)
6. **RemoveNextQuestionFKConstraints** - Removes problematic FK constraints (v1.4.0)
7. **CleanSlateNextQuestionDeterminant** - **DESTRUCTIVE** clean slate migration to value objects (v1.4.1)
8. **AnswerNextStepValueObject** - **DATA PRESERVING** Answer.Next value object migration (v1.4.2)

**Migration v1.4.2 Details** (AnswerNextStepValueObject):
- **Type**: DATA PRESERVING (transforms existing data)
- **Purpose**: Complete Answer entity migration to value object pattern
- **Transforms**: `NextQuestionId` (int with magic 0) → `Next` (NextQuestionDeterminant owned type)
- **Data Migration**:
  - `NextQuestionId = 0` → `Next { Type = EndSurvey, NextQuestionId = null }`
  - `NextQuestionId > 0` → `Next { Type = GoToQuestion, NextQuestionId = value }`
- **Schema Changes**:
  - Drops column: `next_question_id`
  - Adds columns: `next_step_type` (TEXT NOT NULL), `next_step_question_id` (INT NULLABLE)
  - Adds CHECK constraint: Enforces value object invariants
- **Impact**: Completes value object consistency across all conditional flow entities

**Migration v1.4.1 Details** (CleanSlateNextQuestionDeterminant):
- **Type**: DESTRUCTIVE (truncates all data)
- **Reason**: Incompatible schema change from `int?` to owned type `NextQuestionDeterminant`
- **Impact**: All users, surveys, questions, responses, answers deleted
- **Data Loss**: Acceptable in development, mock data only
- **Mitigation**: Production would require data migration script

**Best Practices**:
- Always backup before production migrations
- Test in dev/staging first
- Review generated SQL script (`dotnet ef migrations script`)
- Never edit applied migrations
- Use descriptive migration names
- Document destructive migrations clearly

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

## Database Schema Details

### Table Structures (PostgreSQL 15)

**users** (6 columns):
```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    telegram_id BIGINT NOT NULL,
    username TEXT,
    first_name TEXT,
    last_name TEXT,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    last_login_at TIMESTAMP,

    CONSTRAINT uq_users_telegram_id UNIQUE (telegram_id)
);

CREATE INDEX idx_users_telegram_id ON users(telegram_id);
CREATE INDEX idx_users_username ON users(username) WHERE username IS NOT NULL;  -- Partial index
```

**surveys** (9 columns):
```sql
CREATE TABLE surveys (
    id SERIAL PRIMARY KEY,
    title TEXT NOT NULL,
    description TEXT,
    creator_id INT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT FALSE,
    code VARCHAR(6) NOT NULL,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,

    CONSTRAINT fk_surveys_creator FOREIGN KEY (creator_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX idx_surveys_code ON surveys(code) WHERE code IS NOT NULL;  -- Partial unique
CREATE INDEX idx_surveys_is_active ON surveys(is_active) WHERE is_active = TRUE;  -- Partial index
CREATE INDEX idx_surveys_creator_is_active ON surveys(creator_id, is_active);  -- Composite
CREATE INDEX idx_surveys_created_at_desc ON surveys(created_at DESC);  -- Descending
```

**questions** (11 columns + 2 value object columns):
```sql
CREATE TABLE questions (
    id SERIAL PRIMARY KEY,
    survey_id INT NOT NULL,
    question_text TEXT NOT NULL,
    question_type TEXT NOT NULL,  -- 'text', 'single_choice', 'multiple_choice', 'rating', 'yes_no'
    order_index INT NOT NULL,
    options_json JSONB,
    is_required BOOLEAN NOT NULL DEFAULT TRUE,
    media_content JSONB,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,

    -- Value object columns (owned type NextQuestionDeterminant)
    default_next_step_type TEXT,  -- 'GoToQuestion' or 'EndSurvey'
    default_next_question_id INT,

    CONSTRAINT fk_questions_survey FOREIGN KEY (survey_id) REFERENCES surveys(id) ON DELETE CASCADE,
    CONSTRAINT fk_questions_default_next_question FOREIGN KEY (default_next_question_id)
        REFERENCES questions(id) ON DELETE SET NULL,

    -- CHECK constraints
    CONSTRAINT chk_question_order_index CHECK (order_index >= 0),
    CONSTRAINT chk_question_type CHECK (question_type IN ('text', 'single_choice', 'multiple_choice', 'rating', 'yes_no', 'location')),
    CONSTRAINT chk_question_default_next_invariant CHECK (
        (default_next_step_type IS NULL AND default_next_question_id IS NULL) OR
        (default_next_step_type = 'GoToQuestion' AND default_next_question_id IS NOT NULL AND default_next_question_id > 0) OR
        (default_next_step_type = 'EndSurvey' AND default_next_question_id IS NULL)
    ),

    CONSTRAINT uq_questions_survey_order UNIQUE (survey_id, order_index)
);

CREATE INDEX idx_questions_options_json ON questions USING GIN (options_json);  -- JSONB index
CREATE INDEX idx_questions_media_content ON questions USING GIN (media_content);  -- JSONB index
CREATE INDEX idx_questions_default_next_question_id ON questions(default_next_question_id);
```

**question_options** (7 columns + 2 value object columns):
```sql
CREATE TABLE question_options (
    id SERIAL PRIMARY KEY,
    question_id INT NOT NULL,
    option_text TEXT NOT NULL,
    order_index INT NOT NULL,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,

    -- Value object columns (owned type NextQuestionDeterminant)
    next_step_type TEXT,  -- 'GoToQuestion' or 'EndSurvey'
    next_question_id INT,

    CONSTRAINT fk_question_options_question FOREIGN KEY (question_id) REFERENCES questions(id) ON DELETE CASCADE,
    CONSTRAINT fk_question_options_next_question FOREIGN KEY (next_question_id)
        REFERENCES questions(id) ON DELETE SET NULL,

    -- CHECK constraints
    CONSTRAINT chk_question_option_order_index CHECK (order_index >= 0),
    CONSTRAINT chk_question_option_next_invariant CHECK (
        (next_step_type IS NULL AND next_question_id IS NULL) OR
        (next_step_type = 'GoToQuestion' AND next_question_id IS NOT NULL AND next_question_id > 0) OR
        (next_step_type = 'EndSurvey' AND next_question_id IS NULL)
    ),

    CONSTRAINT uq_question_options_question_order UNIQUE (question_id, order_index)
);

CREATE INDEX idx_question_options_question_id ON question_options(question_id, order_index);  -- Composite
CREATE INDEX idx_question_options_next_question_id ON question_options(next_question_id);
```

**responses** (8 columns + 1 JSONB column):
```sql
CREATE TABLE responses (
    id SERIAL PRIMARY KEY,
    survey_id INT NOT NULL,
    respondent_telegram_id BIGINT,  -- NOT a foreign key (anonymous allowed)
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    submitted_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    visited_question_ids JSONB NOT NULL DEFAULT '[]'::jsonb,  -- Array of question IDs

    CONSTRAINT fk_responses_survey FOREIGN KEY (survey_id) REFERENCES surveys(id) ON DELETE CASCADE
);

CREATE INDEX idx_responses_survey_respondent ON responses(survey_id, respondent_telegram_id);  -- Composite
CREATE INDEX idx_responses_is_completed ON responses(is_completed) WHERE is_completed = TRUE;  -- Partial
CREATE INDEX idx_responses_visited_question_ids ON responses USING GIN (visited_question_ids);  -- JSONB
```

**answers** (8 columns + 1 deprecated column):
```sql
CREATE TABLE answers (
    id SERIAL PRIMARY KEY,
    response_id INT NOT NULL,
    question_id INT NOT NULL,
    answer_text TEXT,
    answer_json JSONB,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,

    CONSTRAINT fk_answers_response FOREIGN KEY (response_id) REFERENCES responses(id) ON DELETE CASCADE,
    CONSTRAINT fk_answers_question FOREIGN KEY (question_id) REFERENCES questions(id) ON DELETE CASCADE,

    -- CHECK constraints
    CONSTRAINT chk_answer_has_content CHECK (answer_text IS NOT NULL OR answer_json IS NOT NULL),

    CONSTRAINT uq_answers_response_question UNIQUE (response_id, question_id)
);

CREATE INDEX idx_answers_answer_json ON answers USING GIN (answer_json);  -- JSONB index
```

### Index Strategy Summary

**Index Types Used**:
1. **B-tree (default)**: Primary keys, foreign keys, simple columns
2. **GIN (Generalized Inverted Index)**: JSONB columns, full-text search
3. **Partial indexes**: Filtered indexes for common subsets (is_active=true, is_completed=true)
4. **Composite indexes**: Multi-column for common query patterns
5. **Descending indexes**: For reverse ordering (created_at DESC)

**Total Indexes**: 25+ across 6 tables

**Performance Impact**:
- **Read queries**: 10-100x faster with indexes
- **Write operations**: ~5-10% slower (index maintenance)
- **Disk space**: ~20-30% additional storage
- **Trade-off**: Acceptable for read-heavy workloads (surveys are read more than written)

### Foreign Key Cascade Behavior

**CASCADE DELETE** (owned data):
- surveys → questions (delete survey deletes all questions)
- surveys → responses (delete survey deletes all responses)
- questions → question_options (delete question deletes all options)
- responses → answers (delete response deletes all answers)

**SET NULL** (graceful degradation):
- questions.default_next_question_id → questions.id
- question_options.next_question_id → questions.id

**NO CASCADE** (independent data):
- surveys.creator_id → users.id (CASCADE DELETE - deleting user deletes their surveys)
- responses.respondent_telegram_id → NOT a foreign key (anonymous responses allowed)

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

**Use Cases**:
- Bulk operations that must succeed together (reordering questions)
- Complex business logic with multiple entity updates
- Data migration scripts

**Alternative - Implicit Transactions**:
```csharp
// EF Core automatically wraps SaveChangesAsync in a transaction
await _context.SaveChangesAsync();  // Atomic (all or nothing)
```

### Async/Await Pattern (CRITICAL)

**ALWAYS use async operations** to prevent thread pool starvation:

```csharp
// GOOD - Async all the way
public async Task<Survey> GetSurveyAsync(int id)
{
    return await _context.Surveys
        .Include(s => s.Questions)
        .FirstOrDefaultAsync(s => s.Id == id);
}

// BAD - Blocking sync operation
public Survey GetSurvey(int id)
{
    return _context.Surveys
        .Include(s => s.Questions)
        .FirstOrDefault(s => s.Id == id);  // Blocks thread!
}

// VERY BAD - Async-over-sync (deadlock risk)
public Survey GetSurvey(int id)
{
    return GetSurveyAsync(id).Result;  // DEADLOCK RISK!
}
```

**Performance Impact**:
- Sync operations block threads (limited thread pool)
- Async operations release threads while waiting for I/O
- **Rule**: Use async for all database/network/file operations

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

### Issue: Value Object Invariant Violations

**Problem**: Trying to create invalid NextQuestionDeterminant

```csharp
// BAD - Will throw ArgumentException
var det = NextQuestionDeterminant.ToQuestion(0);  // ID must be > 0

// BAD - Will throw ArgumentException
var det = NextQuestionDeterminant.ToQuestion(-5);  // ID must be > 0
```

**Solution**: Always use factory methods correctly:
```csharp
// GOOD - Navigate to question 5
var det = NextQuestionDeterminant.ToQuestion(5);

// GOOD - End survey
var det = NextQuestionDeterminant.End();
```

**Database-Level Protection**: CHECK constraints prevent invalid states even if code bypasses validation.

### Issue: Owned Type Not Loading

**Problem**: Accessing owned type navigation after DbContext disposed

```csharp
// BAD - DbContext disposed before access
var question = await _repository.GetByIdAsync(id);
// ... DbContext disposed here (end of scope)
var nextId = question.DefaultNext.NextQuestionId;  // May be null!
```

**Solution**: Owned types are automatically loaded, but ensure DbContext is alive:
```csharp
// GOOD - Access within repository/service scope
var question = await _repository.GetByIdAsync(id);
var nextId = question.DefaultNext?.NextQuestionId;  // Safe
```

### Issue: JSONB Query Performance

**Problem**: Querying JSONB without GIN index

```sql
-- SLOW - Sequential scan
SELECT * FROM responses WHERE visited_question_ids @> '[5]'::jsonb;
```

**Solution**: Ensure GIN index exists (already configured):
```sql
-- Index automatically created by migration
CREATE INDEX idx_responses_visited_question_ids
ON responses USING GIN (visited_question_ids);

-- Now query is fast (index scan)
SELECT * FROM responses WHERE visited_question_ids @> '[5]'::jsonb;
```

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

    // Validation Services (NEW in v1.4.0)
    services.AddScoped<ISurveyValidationService, SurveyValidationService>();

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

### Architectural Patterns
- **Clean Architecture**: Depends only on Core layer (zero external dependencies in Core)
- **Repository Pattern**: Generic and specialized repositories for data access abstraction
- **Service Layer**: Comprehensive business logic (SurveyService, ResponseService, ValidationService)
- **DDD Value Objects**: NextQuestionDeterminant as owned types for type-safe flow configuration

### Database Design
- **Fluent API**: Entity configurations with PostgreSQL-specific optimizations
- **Owned Types**: EF Core owned types for value objects (no JOIN overhead)
- **JSONB Columns**: PostgreSQL JSONB for dynamic data (OptionsJson, VisitedQuestionIds)
- **GIN Indexes**: Full-text and JSON containment queries
- **Partial Indexes**: Filtered indexes for common query patterns
- **CHECK Constraints**: Database-level invariant enforcement

### Data Integrity
- **Foreign Keys**: CASCADE delete for ownership, SET NULL for graceful degradation
- **Unique Constraints**: Prevent duplicates (TelegramId, Survey Code, composite keys)
- **Transactions**: Implicit (SaveChangesAsync) and explicit (BeginTransactionAsync)
- **Automatic Timestamps**: Context automatically manages CreatedAt/UpdatedAt

### Business Logic
- **Smart Delete**: Soft delete for surveys with responses, hard delete for unused
- **Survey Codes**: 6-character Base36 generation with collision detection
- **Cycle Detection**: DFS algorithm for conditional flow validation (O(V+E))
- **Flow Execution**: Runtime navigation with VisitedQuestionIds tracking

### Performance Optimizations
- **Eager Loading**: Include() and ThenInclude() to prevent N+1 queries
- **AsNoTracking**: Read-only queries without change tracking overhead
- **Async/Await**: Non-blocking I/O operations throughout
- **Connection Pooling**: Scoped DbContext with automatic disposal
- **Batch Operations**: Bulk inserts and updates where applicable

### Migration Strategy
- **EF Core Migrations**: Schema version control with code-first approach
- **Clean Slate Migrations**: v1.4.1 destructive migration for incompatible schema changes
- **Idempotent Scripts**: Safe to re-run migration scripts

**Total**: ~4,500+ lines implementing data access, business logic, and database optimizations

**Key Technologies**: EF Core 9.0.10, PostgreSQL 15, Npgsql, DDD patterns, async/await

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

**Last Updated**: 2025-12-02 | **Version**: 1.6.2 (ResponseService EndSurvey Bug Fix)
