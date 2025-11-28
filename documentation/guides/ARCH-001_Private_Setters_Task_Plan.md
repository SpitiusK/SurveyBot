# ARCH-001: Add Private Setters to Entities - Implementation Task Plan

**Task ID**: ARCH-001
**Title**: Add Private Setters to Entities
**Priority**: 🟠 HIGH
**Estimated Effort**: 4-6 hours
**Status**: 📋 Ready
**Dependencies**: None
**Phase**: Phase 1 - Value Objects & Encapsulation

---

## Task Overview

### Description
Refactor all domain entities to use private setters and expose collections as `IReadOnlyCollection<T>`, enforcing proper encapsulation and preventing direct property modification outside the entity itself. This is a foundational DDD improvement that enables controlled state changes through entity methods.

### Business Value
- **Encapsulation**: Prevents external code from bypassing entity validation logic
- **Maintainability**: Changes to entity state centralized in entity methods
- **Consistency**: All property modifications go through validated pathways
- **Foundation**: Enables rich domain model (ARCH-006) and factory methods (ARCH-002)

### Dependencies
- **Prerequisites**: None (this is the starting point for Phase 1)
- **Blocks**: ARCH-002 (Factory Methods) depends on private setters being in place
- **Parallel Work**: Can work on different entities simultaneously if multiple developers

---

## Current State Analysis

### Files Requiring Modification

#### Core Layer - Entity Files
| File Path | Properties to Change | Collection Properties | Estimated Impact |
|-----------|---------------------|----------------------|------------------|
| `src/SurveyBot.Core/Entities/BaseEntity.cs` | Id, CreatedAt, UpdatedAt | None | LOW - Only timestamps |
| `src/SurveyBot.Core/Entities/User.cs` | TelegramId, Username, FirstName, LastName, LastLoginAt | Surveys → IReadOnlyCollection | MEDIUM |
| `src/SurveyBot.Core/Entities/Survey.cs` | Title, Description, Code, CreatorId, IsActive, AllowMultipleResponses, ShowResults | Questions, Responses → IReadOnlyCollection | HIGH |
| `src/SurveyBot.Core/Entities/Question.cs` | SurveyId, QuestionText, QuestionType, OrderIndex, IsRequired, OptionsJson, MediaContent, DefaultNext | Options, Answers → IReadOnlyCollection | HIGH |
| `src/SurveyBot.Core/Entities/QuestionOption.cs` | QuestionId, Text, OrderIndex, Next | None | LOW |
| `src/SurveyBot.Core/Entities/Response.cs` | Id, SurveyId, RespondentTelegramId, IsComplete, StartedAt, SubmittedAt, VisitedQuestionIds | Answers → IReadOnlyCollection | MEDIUM |
| `src/SurveyBot.Core/Entities/Answer.cs` | Id, ResponseId, QuestionId, AnswerText, AnswerJson, CreatedAt, Next | None | LOW |

#### Infrastructure Layer - Service Files (Need Updates)
| File Path | Usages to Fix | Difficulty |
|-----------|---------------|------------|
| `src/SurveyBot.Infrastructure/Services/SurveyService.cs` | ~15 property assignments | MEDIUM |
| `src/SurveyBot.Infrastructure/Services/QuestionService.cs` | ~8 property assignments | LOW |
| `src/SurveyBot.Infrastructure/Services/ResponseService.cs` | ~5 property assignments | LOW |
| `src/SurveyBot.Infrastructure/Services/UserService.cs` | ~3 property assignments | LOW |
| `src/SurveyBot.Infrastructure/Data/Configurations/*.cs` | EF Core configuration updates | LOW (EF Core uses reflection) |

### Current Pattern (Before)

```csharp
// BaseEntity.cs - CURRENT (Anemic)
public abstract class BaseEntity
{
    public int Id { get; set; }                    // ❌ Public setter
    public DateTime CreatedAt { get; set; }        // ❌ Direct modification
    public DateTime UpdatedAt { get; set; }        // ❌ No validation
}

// Survey.cs - CURRENT (Anemic)
public class Survey : BaseEntity
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;  // ❌ Anyone can modify

    public bool IsActive { get; set; }                  // ❌ Can bypass business rules

    public ICollection<Question> Questions { get; set; } = new List<Question>();  // ❌ Direct collection access
}

// Service layer - CURRENT (Directly modifies properties)
public async Task<SurveyDto> UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto)
{
    var survey = await _surveyRepository.GetByIdAsync(surveyId);

    survey.Title = dto.Title;                // ❌ Direct modification
    survey.Description = dto.Description;    // ❌ No validation here
    survey.UpdatedAt = DateTime.UtcNow;      // ❌ Manual timestamp update

    await _surveyRepository.UpdateAsync(survey);
}
```

### Target Pattern (After)

```csharp
// BaseEntity.cs - IMPROVED (Encapsulated)
public abstract class BaseEntity
{
    public int Id { get; private set; }                    // ✅ EF Core can still set via reflection
    public DateTime CreatedAt { get; private set; }        // ✅ Protected from external changes
    public DateTime UpdatedAt { get; private set; }        // ✅ Controlled modification

    // Helper method for derived classes
    protected void SetTimestamps(DateTime? createdAt = null)
    {
        if (createdAt.HasValue)
            CreatedAt = createdAt.Value;
        UpdatedAt = DateTime.UtcNow;
    }
}

// Survey.cs - IMPROVED (Encapsulated)
public class Survey : BaseEntity
{
    [Required]
    [MaxLength(500)]
    public string Title { get; private set; } = string.Empty;  // ✅ Private setter

    public bool IsActive { get; private set; }                  // ✅ Must use Activate() method

    // Backing field for collection
    private readonly List<Question> _questions = new();

    // Read-only exposure
    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();  // ✅ Cannot modify directly

    // Controlled modification methods (to be added in ARCH-002/ARCH-006)
    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new SurveyValidationException("Title cannot be empty");

        Title = title;
        SetTimestamps();
    }
}

// Service layer - IMPROVED (Uses entity methods)
public async Task<SurveyDto> UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto)
{
    var survey = await _surveyRepository.GetByIdAsync(surveyId);

    survey.UpdateTitle(dto.Title);           // ✅ Validation inside entity
    survey.UpdateDescription(dto.Description);  // ✅ Encapsulated behavior

    await _surveyRepository.UpdateAsync(survey);  // ✅ Timestamps handled by entity
}
```

---

## Implementation Steps

### Step 1: Update BaseEntity (0.5 hours)

**File**: `src/SurveyBot.Core/Entities/BaseEntity.cs`

**Changes**:
```csharp
public abstract class BaseEntity
{
    /// <summary>
    /// Gets the unique identifier for the entity.
    /// Set by EF Core via reflection during persistence.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Gets the date and time when the entity was created.
    /// Set automatically by DbContext.SaveChangesAsync.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the date and time when the entity was last updated.
    /// Updated automatically by DbContext.SaveChangesAsync.
    /// </summary>
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Helper method for derived entities to set timestamps.
    /// Use this in entity methods that modify state.
    /// </summary>
    protected void MarkAsModified()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Helper method for entity initialization (used by factory methods in ARCH-002).
    /// </summary>
    protected void InitializeTimestamps()
    {
        var now = DateTime.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }
}
```

**Testing**:
- Verify EF Core can still load entities
- Verify SaveChangesAsync timestamp override still works
- Run: `dotnet test --filter "FullyQualifiedName~BaseEntity"`

---

### Step 2: Update User Entity (0.5 hours)

**File**: `src/SurveyBot.Core/Entities/User.cs`

**Changes**:
```csharp
public class User : BaseEntity
{
    [Required]
    public long TelegramId { get; private set; }

    [MaxLength(255)]
    public string? Username { get; private set; }

    [MaxLength(255)]
    public string? FirstName { get; private set; }

    [MaxLength(255)]
    public string? LastName { get; private set; }

    public DateTime? LastLoginAt { get; private set; }

    // Backing field
    private readonly List<Survey> _surveys = new();

    // Read-only collection
    public IReadOnlyCollection<Survey> Surveys => _surveys.AsReadOnly();

    // Temporary setter methods (will be replaced by factory in ARCH-002)
    public void SetTelegramId(long telegramId) => TelegramId = telegramId;
    public void SetUsername(string? username) => Username = username;
    public void SetFirstName(string? firstName) => FirstName = firstName;
    public void SetLastName(string? lastName) => LastName = lastName;
    public void SetLastLoginAt(DateTime? lastLoginAt) => LastLoginAt = lastLoginAt;
}
```

**Service Updates**:
- File: `src/SurveyBot.Infrastructure/Services/UserService.cs`
- Change: `user.LastLoginAt = DateTime.UtcNow` → `user.SetLastLoginAt(DateTime.UtcNow)`

**Testing**:
- Verify user creation/update still works
- Verify EF Core navigation properties work
- Run: `dotnet test --filter "FullyQualifiedName~UserService"`

---

### Step 3: Update Survey Entity (1.5 hours)

**File**: `src/SurveyBot.Core/Entities/Survey.cs`

**Changes**:
```csharp
public class Survey : BaseEntity
{
    [Required]
    [MaxLength(500)]
    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    [MaxLength(10)]
    public string? Code { get; private set; }

    [Required]
    public int CreatorId { get; private set; }

    [Required]
    public bool IsActive { get; private set; } = true;

    [Required]
    public bool AllowMultipleResponses { get; private set; } = false;

    [Required]
    public bool ShowResults { get; private set; } = true;

    // Backing fields
    private readonly List<Question> _questions = new();
    private readonly List<Response> _responses = new();

    // Read-only collections
    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();
    public IReadOnlyCollection<Response> Responses => _responses.AsReadOnly();

    // Navigation property
    public User Creator { get; private set; } = null!;

    // Temporary setter methods (will be improved in ARCH-006)
    public void SetTitle(string title)
    {
        Title = title;
        MarkAsModified();
    }

    public void SetDescription(string? description)
    {
        Description = description;
        MarkAsModified();
    }

    public void SetCode(string? code) => Code = code;
    public void SetCreatorId(int creatorId) => CreatorId = creatorId;

    public void SetIsActive(bool isActive)
    {
        IsActive = isActive;
        MarkAsModified();
    }

    public void SetAllowMultipleResponses(bool allow)
    {
        AllowMultipleResponses = allow;
        MarkAsModified();
    }

    public void SetShowResults(bool show)
    {
        ShowResults = show;
        MarkAsModified();
    }

    // Internal methods for EF Core to populate collections
    internal void AddQuestionInternal(Question question) => _questions.Add(question);
    internal void AddResponseInternal(Response response) => _responses.Add(response);
}
```

**Service Updates**:
- File: `src/SurveyBot.Infrastructure/Services/SurveyService.cs`
- Lines to change:
  - `survey.Title = dto.Title` → `survey.SetTitle(dto.Title)`
  - `survey.Description = dto.Description` → `survey.SetDescription(dto.Description)`
  - `survey.IsActive = true` → `survey.SetIsActive(true)`
  - `survey.AllowMultipleResponses = dto.AllowMultipleResponses` → `survey.SetAllowMultipleResponses(dto.AllowMultipleResponses)`
  - `survey.ShowResults = dto.ShowResults` → `survey.SetShowResults(dto.ShowResults)`

**Testing**:
- Verify survey CRUD operations work
- Verify `GetByIdWithQuestionsAsync` still loads questions
- Run: `dotnet test --filter "FullyQualifiedName~SurveyService"`

---

### Step 4: Update Question Entity (1 hour)

**File**: `src/SurveyBot.Core/Entities/Question.cs`

**Changes**:
```csharp
public class Question : BaseEntity
{
    [Required]
    public int SurveyId { get; private set; }

    [Required]
    public string QuestionText { get; private set; } = string.Empty;

    [Required]
    public QuestionType QuestionType { get; private set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int OrderIndex { get; private set; }

    [Required]
    public bool IsRequired { get; private set; } = true;

    public string? OptionsJson { get; private set; }

    public string? MediaContent { get; private set; }

    public NextQuestionDeterminant? DefaultNext { get; private set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool SupportsBranching =>
        QuestionType == QuestionType.SingleChoice || QuestionType == QuestionType.Rating;

    // Backing fields
    private readonly List<Answer> _answers = new();
    private readonly List<QuestionOption> _options = new();

    // Read-only collections
    public IReadOnlyCollection<Answer> Answers => _answers.AsReadOnly();
    public IReadOnlyCollection<QuestionOption> Options => _options.AsReadOnly();

    // Navigation properties
    public Survey Survey { get; private set; } = null!;

    // Temporary setter methods
    public void SetSurveyId(int surveyId) => SurveyId = surveyId;

    public void SetQuestionText(string text)
    {
        QuestionText = text;
        MarkAsModified();
    }

    public void SetQuestionType(QuestionType type) => QuestionType = type;
    public void SetOrderIndex(int index) => OrderIndex = index;
    public void SetIsRequired(bool required) => IsRequired = required;
    public void SetOptionsJson(string? json) => OptionsJson = json;
    public void SetMediaContent(string? content) => MediaContent = content;
    public void SetDefaultNext(NextQuestionDeterminant? next) => DefaultNext = next;

    // Internal methods for EF Core
    internal void AddAnswerInternal(Answer answer) => _answers.Add(answer);
    internal void AddOptionInternal(QuestionOption option) => _options.Add(option);
}
```

**Service Updates**:
- File: `src/SurveyBot.Infrastructure/Services/QuestionService.cs`
- Change all property assignments to use setter methods

**Testing**:
- Verify question creation/update works
- Verify conditional flow configuration works
- Run: `dotnet test --filter "FullyQualifiedName~QuestionService"`

---

### Step 5: Update QuestionOption Entity (0.5 hours)

**File**: `src/SurveyBot.Core/Entities/QuestionOption.cs`

**Changes**:
```csharp
public class QuestionOption : BaseEntity
{
    [Required]
    public int QuestionId { get; private set; }

    [Required]
    public string Text { get; private set; } = string.Empty;

    [Required]
    [Range(0, int.MaxValue)]
    public int OrderIndex { get; private set; }

    public NextQuestionDeterminant? Next { get; private set; }

    // Navigation properties
    public Question Question { get; private set; } = null!;

    // Temporary setter methods
    public void SetQuestionId(int questionId) => QuestionId = questionId;
    public void SetText(string text) => Text = text;
    public void SetOrderIndex(int index) => OrderIndex = index;
    public void SetNext(NextQuestionDeterminant? next) => Next = next;
}
```

**Service Updates**:
- File: `src/SurveyBot.Infrastructure/Services/QuestionService.cs`
- Update option creation/modification code

**Testing**:
- Verify option creation works with conditional flow
- Run: `dotnet test --filter "FullyQualifiedName~QuestionFlow"`

---

### Step 6: Update Response Entity (0.5 hours)

**File**: `src/SurveyBot.Core/Entities/Response.cs`

**Changes**:
```csharp
public class Response
{
    public int Id { get; private set; }

    [Required]
    public int SurveyId { get; private set; }

    [Required]
    public long RespondentTelegramId { get; private set; }

    [Required]
    public bool IsComplete { get; private set; } = false;

    public DateTime? StartedAt { get; private set; }

    public DateTime? SubmittedAt { get; private set; }

    public List<int> VisitedQuestionIds { get; private set; } = new();

    // Backing field
    private readonly List<Answer> _answers = new();

    // Read-only collection
    public IReadOnlyCollection<Answer> Answers => _answers.AsReadOnly();

    // Navigation properties
    public Survey Survey { get; private set; } = null!;

    // Temporary setter methods
    public void SetSurveyId(int surveyId) => SurveyId = surveyId;
    public void SetRespondentTelegramId(long telegramId) => RespondentTelegramId = telegramId;

    public void SetIsComplete(bool complete)
    {
        IsComplete = complete;
        if (complete && !SubmittedAt.HasValue)
            SubmittedAt = DateTime.UtcNow;
    }

    public void SetStartedAt(DateTime? startedAt) => StartedAt = startedAt;
    public void SetSubmittedAt(DateTime? submittedAt) => SubmittedAt = submittedAt;

    // Keep existing helper methods
    public bool HasVisitedQuestion(int questionId) =>
        VisitedQuestionIds.Contains(questionId);

    public void RecordVisitedQuestion(int questionId)
    {
        if (!VisitedQuestionIds.Contains(questionId))
            VisitedQuestionIds.Add(questionId);
    }

    // Internal method for EF Core
    internal void AddAnswerInternal(Answer answer) => _answers.Add(answer);
}
```

**Service Updates**:
- File: `src/SurveyBot.Infrastructure/Services/ResponseService.cs`
- Update property assignments to use setter methods

**Testing**:
- Verify response creation/completion works
- Verify visited question tracking works
- Run: `dotnet test --filter "FullyQualifiedName~ResponseService"`

---

### Step 7: Update Answer Entity (0.5 hours)

**File**: `src/SurveyBot.Core/Entities/Answer.cs`

**Changes**:
```csharp
public class Answer
{
    public int Id { get; private set; }

    [Required]
    public int ResponseId { get; private set; }

    [Required]
    public int QuestionId { get; private set; }

    public string? AnswerText { get; private set; }

    public string? AnswerJson { get; private set; }

    [Required]
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public NextQuestionDeterminant Next { get; private set; } = NextQuestionDeterminant.End();

    // Navigation properties
    public Response Response { get; private set; } = null!;
    public Question Question { get; private set; } = null!;

    // Temporary setter methods
    public void SetResponseId(int responseId) => ResponseId = responseId;
    public void SetQuestionId(int questionId) => QuestionId = questionId;
    public void SetAnswerText(string? text) => AnswerText = text;
    public void SetAnswerJson(string? json) => AnswerJson = json;
    public void SetNext(NextQuestionDeterminant next) => Next = next;
    public void SetCreatedAt(DateTime createdAt) => CreatedAt = createdAt;
}
```

**Service Updates**:
- File: `src/SurveyBot.Infrastructure/Services/ResponseService.cs`
- Update answer creation/modification code

**Testing**:
- Verify answer saving works
- Verify next step determination works
- Run: `dotnet test --filter "FullyQualifiedName~Answer"`

---

### Step 8: Update EF Core Configurations (0.5 hours)

**Files**:
- `src/SurveyBot.Infrastructure/Data/Configurations/UserConfiguration.cs`
- `src/SurveyBot.Infrastructure/Data/Configurations/SurveyConfiguration.cs`
- `src/SurveyBot.Infrastructure/Data/Configurations/QuestionConfiguration.cs`
- `src/SurveyBot.Infrastructure/Data/Configurations/QuestionOptionConfiguration.cs`
- `src/SurveyBot.Infrastructure/Data/Configurations/ResponseConfiguration.cs`
- `src/SurveyBot.Infrastructure/Data/Configurations/AnswerConfiguration.cs`

**Changes Needed**:

EF Core can set private properties via reflection, but collection initialization needs attention:

```csharp
// SurveyConfiguration.cs - Example
public class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        // ... existing configuration ...

        // Collections - use backing field
        builder.HasMany(s => s.Questions)
            .WithOne(q => q.Survey)
            .HasForeignKey(q => q.SurveyId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent!
            .SetPropertyAccessMode(PropertyAccessMode.Field);  // Access _questions field

        builder.HasMany(s => s.Responses)
            .WithOne(r => r.Survey)
            .HasForeignKey(r => r.SurveyId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent!
            .SetPropertyAccessMode(PropertyAccessMode.Field);  // Access _responses field
    }
}
```

**Testing**:
- Verify all entity loading works
- Verify SaveChanges works
- Verify navigation properties populate
- Run: `dotnet ef database update` (verify no errors)
- Run: `dotnet test --filter "FullyQualifiedName~Repository"`

---

### Step 9: Compile and Fix Service Layer (1 hour)

**Process**:
1. Compile the solution: `dotnet build`
2. Fix compilation errors in service layer:
   - Replace direct property assignments with setter methods
   - Update collection access to use read-only collections where needed
3. Update AutoMapper configurations if needed (unlikely for setters)

**Affected Services**:
- `SurveyService` (~15 fixes)
- `QuestionService` (~8 fixes)
- `ResponseService` (~5 fixes)
- `UserService` (~3 fixes)

**Example Fix Pattern**:
```csharp
// BEFORE
survey.Title = dto.Title;
survey.IsActive = true;

// AFTER
survey.SetTitle(dto.Title);
survey.SetIsActive(true);
```

---

### Step 10: Run Full Test Suite (0.5 hours)

**Commands**:
```bash
# Clean rebuild
dotnet clean
dotnet build

# Run all tests
dotnet test --verbosity normal

# Run specific test categories
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# Check test coverage (if configured)
dotnet test /p:CollectCoverage=true
```

**Expected Results**:
- All existing tests pass
- No new test failures
- EF Core queries work correctly
- Navigation properties load correctly
- Collections are read-only where expected

**If Tests Fail**:
1. Check entity property access modes in EF configurations
2. Verify backing fields are named correctly (`_questions` not `_Questions`)
3. Ensure setter methods are public (not internal)
4. Check that EF Core can access private setters via reflection

---

## Testing Strategy

### Unit Tests to Add

**File**: `tests/SurveyBot.Tests/Unit/Entities/EntityEncapsulationTests.cs` (NEW)

```csharp
public class EntityEncapsulationTests
{
    [Fact]
    public void Survey_Title_HasPrivateSetter()
    {
        // Arrange
        var property = typeof(Survey).GetProperty(nameof(Survey.Title));

        // Assert
        Assert.NotNull(property);
        Assert.True(property!.SetMethod!.IsPrivate, "Title setter should be private");
    }

    [Fact]
    public void Survey_Questions_IsReadOnly()
    {
        // Arrange
        var property = typeof(Survey).GetProperty(nameof(Survey.Questions));

        // Assert
        Assert.NotNull(property);
        Assert.True(typeof(IReadOnlyCollection<Question>).IsAssignableFrom(property!.PropertyType),
            "Questions should be IReadOnlyCollection");
    }

    [Fact]
    public void Survey_CannotModifyQuestionsDirectly()
    {
        // Arrange
        var survey = new Survey(); // Will need factory method in ARCH-002

        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
        {
            ((ICollection<Question>)survey.Questions).Add(new Question());
        });
    }

    // Repeat for all entities...
}
```

### Integration Tests to Update

**Files to Update**:
- `tests/SurveyBot.Tests/Integration/Services/SurveyServiceIntegrationTests.cs`
- `tests/SurveyBot.Tests/Integration/Services/QuestionServiceIntegrationTests.cs`
- `tests/SurveyBot.Tests/Integration/Services/ResponseServiceIntegrationTests.cs`

**Pattern**: Replace direct property assignments with setter method calls in test setup.

### Regression Tests

**Ensure No Breakage**:
1. Survey creation/update/delete
2. Question creation with conditional flow
3. Response submission
4. EF Core eager loading (Include statements)
5. AutoMapper mappings
6. API endpoint integration tests

---

## Risk Assessment

### HIGH Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| **EF Core Cannot Access Private Setters** | Test failures, runtime errors | EF Core DOES support private setters via reflection; verify with minimal test case |
| **Collection Property Access Breaks** | Navigation properties fail to load | Use `PropertyAccessMode.Field` in configurations |
| **Service Layer Compilation Errors** | Many files need updates | Systematic approach, update one service at a time |

### MEDIUM Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| **AutoMapper Cannot Map to Private Setters** | Mapping failures | AutoMapper CAN map to private setters; verify configuration |
| **Existing Tests Break** | Test failures | Update tests to use setter methods instead of direct assignment |
| **Repository Include Statements Break** | Data not loaded | Verify Include() still works with backing fields |

### LOW Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Performance Impact** | Negligible (reflection overhead minimal) | Benchmark if concerned |
| **Documentation Out of Sync** | Minor, update after implementation | Update CLAUDE.md files |

### Breaking Changes

**NONE** - This is an internal refactoring:
- API contracts unchanged (DTOs unaffected)
- Database schema unchanged (no migration needed)
- External behavior identical (only internal implementation changes)

### Rollback Strategy

If critical issues arise:

1. **Immediate Rollback**:
   ```bash
   git revert <commit-hash>
   dotnet build
   dotnet test
   ```

2. **Partial Rollback**:
   - Revert individual entity files
   - Keep successful entities
   - Fix problematic ones separately

3. **Recovery Time**: < 30 minutes (simple git revert)

---

## Acceptance Criteria

### Functionality Criteria

- [ ] **All entity properties have private setters** (verify via reflection test)
- [ ] **All collection properties are IReadOnlyCollection<T>** (verify via type check)
- [ ] **EF Core can still persist entities** (verify with integration test)
- [ ] **EF Core can load navigation properties** (verify Include queries work)
- [ ] **Services can modify entities via setter methods** (verify all CRUD operations)
- [ ] **No direct property assignment in service layer** (verify with code search)
- [ ] **Backing fields correctly named and accessed** (verify EF Core configurations)

### Testing Criteria

- [ ] **All existing unit tests pass** (run `dotnet test --filter Category=Unit`)
- [ ] **All integration tests pass** (run `dotnet test --filter Category=Integration`)
- [ ] **New encapsulation tests added** (EntityEncapsulationTests.cs created)
- [ ] **Test coverage maintained or improved** (check coverage report)
- [ ] **No test skipped or ignored** (all tests run)

### Code Quality Criteria

- [ ] **No compilation errors** (run `dotnet build`)
- [ ] **No compilation warnings** (check build output)
- [ ] **Code follows naming conventions** (backing fields use `_camelCase`)
- [ ] **XML documentation updated** (getter properties documented)
- [ ] **Setter methods have consistent naming** (`SetPropertyName` pattern)

### Documentation Criteria

- [ ] **Core CLAUDE.md updated** (reflect private setter pattern)
- [ ] **Infrastructure CLAUDE.md updated** (EF Core configuration notes)
- [ ] **This task plan marked complete** (update status to ✅ COMPLETED)
- [ ] **ARCH-001_COMPLETION_REPORT.md created** (document findings)

---

## Technical Notes

### EF Core and Private Setters

**Key Facts**:
1. EF Core can set private properties via reflection (confirmed since EF Core 1.0)
2. Collections need `PropertyAccessMode.Field` for backing field access
3. No migration needed (schema unchanged)
4. No performance impact (reflection cached)

**Configuration Pattern**:
```csharp
builder.Property(e => e.Title)
    .HasColumnName("title")
    .IsRequired();
// No special configuration needed - EF Core automatically uses reflection
```

### Backing Field Naming

**Conventions**:
- Use underscore prefix: `_questions` (not `questions` or `_Questions`)
- Camel case: `_questions` (not `_Questions`)
- Matches property name: `_questions` for `Questions` property

**EF Core Auto-Detection**:
EF Core automatically finds backing fields using these patterns:
1. `_<camel-cased property name>` (e.g., `_questions` for `Questions`)
2. `_<property name>` (e.g., `_Questions` for `Questions`)
3. `m_<camel-cased property name>` (less common)

### Temporary Setter Methods

**Why Needed**:
- Services currently use direct property assignment
- Factory methods (ARCH-002) will replace most setters
- Rich domain model (ARCH-006) will add behavior methods
- These are intermediate helpers, not final design

**Naming Convention**:
- `Set{PropertyName}` (e.g., `SetTitle`)
- Public accessibility (services need access)
- Simple delegation to property assignment
- Will be removed/replaced in Phase 2

**Example**:
```csharp
// Temporary helper (ARCH-001)
public void SetTitle(string title) => Title = title;

// Will become (ARCH-006):
public void UpdateTitle(string title)
{
    if (IsActive && Responses.Any())
        throw new SurveyOperationException("Cannot modify active survey with responses");

    if (string.IsNullOrWhiteSpace(title))
        throw new SurveyValidationException("Title required");

    Title = title;
    MarkAsModified();
}
```

### AutoMapper Compatibility

AutoMapper can map to private setters:
```csharp
// AutoMapper configuration (no changes needed)
CreateMap<CreateSurveyDto, Survey>();

// AutoMapper uses reflection internally
var survey = _mapper.Map<Survey>(dto);  // Works with private setters
```

### Collection Read-Only Pattern

**Implementation**:
```csharp
private readonly List<Question> _questions = new();
public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();
```

**Benefits**:
- External code cannot modify collection
- Internal methods can still add/remove
- EF Core can populate backing field
- Clear API contract (read-only intent)

**EF Core Configuration**:
```csharp
builder.HasMany(s => s.Questions)
    .WithOne(q => q.Survey)
    .HasForeignKey(q => q.SurveyId)
    .Metadata.PrincipalToDependent!
    .SetPropertyAccessMode(PropertyAccessMode.Field);
```

---

## Completion Checklist

### Pre-Implementation
- [ ] Read and understand this task plan
- [ ] Review current entity implementations
- [ ] Ensure development environment is ready
- [ ] Backup current branch: `git checkout -b backup-before-arch-001`
- [ ] Create feature branch: `git checkout -b feat/arch-001-private-setters`

### Implementation Phase
- [ ] Step 1: Update BaseEntity (0.5h)
- [ ] Step 2: Update User (0.5h)
- [ ] Step 3: Update Survey (1.5h)
- [ ] Step 4: Update Question (1h)
- [ ] Step 5: Update QuestionOption (0.5h)
- [ ] Step 6: Update Response (0.5h)
- [ ] Step 7: Update Answer (0.5h)
- [ ] Step 8: Update EF Core configurations (0.5h)
- [ ] Step 9: Fix service layer compilation (1h)
- [ ] Step 10: Run full test suite (0.5h)

### Testing Phase
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] New encapsulation tests added
- [ ] Manual smoke testing completed
- [ ] No regressions found

### Documentation Phase
- [ ] Update Core CLAUDE.md
- [ ] Update Infrastructure CLAUDE.md
- [ ] Create completion report (ARCH-001_COMPLETION_REPORT.md)
- [ ] Update !PRIORITY_ARCHITECTURE_IMPROVEMENTS.md status

### Finalization
- [ ] Code review completed
- [ ] Merge feature branch to development
- [ ] Tag release: `git tag v1.4.3-arch-001`
- [ ] Mark task as ✅ COMPLETED

---

## Next Steps After Completion

### Immediate Follow-Up
1. Create ARCH-001_COMPLETION_REPORT.md documenting:
   - Issues encountered
   - Solutions applied
   - Test results
   - Lessons learned

2. Update documentation:
   - Mark ARCH-001 as ✅ COMPLETED in !PRIORITY_ARCHITECTURE_IMPROVEMENTS.md
   - Update Core/CLAUDE.md with private setter pattern
   - Update Infrastructure/CLAUDE.md with EF Core notes

### Prepare for ARCH-002
With private setters in place, proceed to:
- **ARCH-002**: Add Factory Methods to Entities
- Dependencies resolved: Private setters enable factory pattern
- Estimated effort: 4-6 hours
- Next task plan: `ARCH-002_Factory_Methods_Task_Plan.md`

---

**Task Plan Created**: 2025-11-27
**Author**: project-manager-agent
**Status**: 📋 Ready for Implementation
**Estimated Total Time**: 4-6 hours
**Parallel Work Possible**: Yes (different entities can be done simultaneously)
