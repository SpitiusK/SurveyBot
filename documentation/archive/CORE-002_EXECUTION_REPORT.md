# CORE-002 Execution Report: Update Question and QuestionOption Entities

## Task Status: ✅ COMPLETED

**Task**: CORE-002 - Update Question and QuestionOption entities to use NextQuestionDeterminant value object
**Date**: 2025-11-23
**Dependencies**: CORE-001 (NextQuestionDeterminant Value Object) ✅
**Next Task**: INFRA-001 (Update EF Core configurations)

---

## Changes Summary

### 1. Question.cs Updates ✅

**File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Entities\Question.cs`

**Changes Applied**:
1. ✅ Added `using SurveyBot.Core.ValueObjects;`
2. ✅ Added `using SurveyBot.Core.Enums;`
3. ✅ Replaced `public int? DefaultNextQuestionId` with `public NextQuestionDeterminant? DefaultNext`
4. ✅ Removed `public Question? DefaultNextQuestion` navigation property (no longer needed with value object)
5. ✅ Updated XML documentation to reflect value object usage

**Before**:
```csharp
/// <summary>
/// Gets or sets the fixed next question ID for non-branching questions.
/// For Text and MultipleChoice questions, all answers navigate to this question.
/// Ignored for branching questions (SingleChoice, Rating) which use option-specific navigation.
/// Set to null to end the survey.
/// </summary>
public int? DefaultNextQuestionId { get; set; }

/// <summary>
/// Gets or sets the navigation property to the default next question.
/// </summary>
public Question? DefaultNextQuestion { get; set; }
```

**After**:
```csharp
/// <summary>
/// Gets or sets the default navigation behavior for non-branching questions.
/// For Text and MultipleChoice questions, all answers navigate according to this determinant.
/// Ignored for branching questions (SingleChoice, Rating) which use option-specific navigation.
/// Set to null to maintain backward compatibility (no default flow defined).
/// Use NextQuestionDeterminant.End() to end the survey or NextQuestionDeterminant.ToQuestion(id) to navigate.
/// </summary>
public NextQuestionDeterminant? DefaultNext { get; set; }
```

---

### 2. QuestionOption.cs Updates ✅

**File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Entities\QuestionOption.cs`

**Changes Applied**:
1. ✅ Added `using SurveyBot.Core.ValueObjects;`
2. ✅ Added `using SurveyBot.Core.Enums;`
3. ✅ Replaced `public int? NextQuestionId` with `public NextQuestionDeterminant? Next`
4. ✅ Removed `public Question? NextQuestion` navigation property (no longer needed with value object)
5. ✅ Updated XML documentation to reflect value object usage

**Before**:
```csharp
/// <summary>
/// Gets or sets the ID of the next question for branching questions.
/// For branching questions (SingleChoice, Rating), the next question ID if this option is selected.
/// Set to 0 (special value) to end the survey for this option.
/// Ignored for non-branching questions.
/// </summary>
public int? NextQuestionId { get; set; }

/// <summary>
/// Gets or sets the navigation property to the next question.
/// </summary>
public Question? NextQuestion { get; set; }
```

**After**:
```csharp
/// <summary>
/// Gets or sets the navigation behavior when this option is selected.
/// For branching questions (SingleChoice, Rating), determines where to go if this option is selected.
/// Ignored for non-branching questions.
/// Set to null to maintain backward compatibility (no flow defined for this option).
/// Use NextQuestionDeterminant.End() to end the survey or NextQuestionDeterminant.ToQuestion(id) to navigate.
/// </summary>
public NextQuestionDeterminant? Next { get; set; }
```

---

## Magic Value Elimination ✅

### Removed References:
1. ✅ **Question.cs**: No more `DefaultNextQuestionId` property
2. ✅ **QuestionOption.cs**: No more `NextQuestionId` property
3. ✅ **Documentation**: Removed all mentions of "0 = end survey" magic value
4. ✅ **Navigation Properties**: Removed EF Core navigation properties (will be reconsidered in INFRA-001)

### Verification:
```bash
# Grep search for old property names and magic value 0 assignment
grep -r "DefaultNextQuestionId|NextQuestionId\s*=\s*0" src/SurveyBot.Core/Entities/
# Result: No matches found ✅
```

---

## Build Verification ✅

**Command**: `dotnet build "C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core"`

**Result**: ✅ SUCCESS
- Warnings: 0
- Errors: 0
- Build Time: 1.93s

**Output**:
```
Определение проектов для восстановления...
Все проекты обновлены для восстановления.
SurveyBot.Core -> C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\bin\Debug\net8.0\SurveyBot.Core.dll

Сборка успешно завершена.
    Предупреждений: 0
    Ошибок: 0
```

---

## Acceptance Criteria Verification

| Criterion | Status | Notes |
|-----------|--------|-------|
| Question.DefaultNextQuestionId replaced with DefaultNext | ✅ | Property replaced, correctly typed as `NextQuestionDeterminant?` |
| QuestionOption.NextQuestionId replaced with Next | ✅ | Property replaced, correctly typed as `NextQuestionDeterminant?` |
| No magic value (0) properties or logic remain | ✅ | All references to magic value 0 removed |
| Navigation properties updated if needed | ✅ | Removed EF Core navigation properties (reconsidered in INFRA-001) |
| Entities compile without errors | ✅ | Build successful, 0 errors, 0 warnings |
| Nullable handling correct | ✅ | Both properties nullable (`NextQuestionDeterminant?`) |
| Using statements added | ✅ | `SurveyBot.Core.ValueObjects` and `SurveyBot.Core.Enums` imported |
| XML documentation updated | ✅ | Clear usage instructions for factory methods |

---

## Key Design Decisions

### 1. Nullable Properties
Both `DefaultNext` and `Next` are nullable (`NextQuestionDeterminant?`) to support:
- **Backward compatibility**: Existing surveys without flow configuration
- **Partial configuration**: Admin may not set flow immediately
- **Validation**: Null indicates "not configured" vs explicit "end survey"

### 2. Removed Navigation Properties
**Removed**:
- `Question.DefaultNextQuestion`
- `QuestionOption.NextQuestion`

**Rationale**:
- Value object encapsulates navigation logic (no need for EF Core navigation)
- Simplifies entity model (less coupling)
- Navigation can be reconsidered in INFRA-001 if EF Core requires it for complex queries

### 3. Factory Method Usage Pattern
Documentation explicitly recommends:
```csharp
// End survey after this question
question.DefaultNext = NextQuestionDeterminant.End();

// Navigate to question with ID 5
question.DefaultNext = NextQuestionDeterminant.ToQuestion(5);

// No flow configured (backward compatibility)
question.DefaultNext = null;
```

### 4. Clean Architecture Compliance
- ✅ Pure domain model change (no framework dependencies)
- ✅ Core layer has zero external dependencies
- ✅ Value object defined in Core.ValueObjects
- ✅ Enum defined in Core.Enums
- ✅ No EF Core concerns in entity definitions

---

## Value Object Benefits

### Before (Magic Value Approach):
```csharp
// Ambiguous: What does 0 mean? What about null vs 0?
public int? NextQuestionId { get; set; }

// Easy to misuse
option.NextQuestionId = 0;  // End survey? Or invalid?
option.NextQuestionId = -1;  // What does this mean?
option.NextQuestionId = null;  // Not configured? Or error?
```

### After (Value Object Approach):
```csharp
// Explicit and type-safe
public NextQuestionDeterminant? Next { get; set; }

// Clear intent
option.Next = NextQuestionDeterminant.End();  // Explicit: survey ends
option.Next = NextQuestionDeterminant.ToQuestion(5);  // Explicit: go to Q5
option.Next = null;  // Explicit: not configured

// Compile-time safety
option.Next = NextQuestionDeterminant.ToQuestion(0);  // ❌ ArgumentException at runtime
option.Next = NextQuestionDeterminant.ToQuestion(-1);  // ❌ ArgumentException at runtime
```

### Enforced Invariants:
1. **GoToQuestion**: `NextQuestionId` must be > 0 (valid question ID)
2. **EndSurvey**: `NextQuestionId` must be null (no next question)
3. **Immutability**: Cannot modify after creation (value semantics)
4. **Equality**: Based on `Type` and `NextQuestionId` values

---

## Next Steps (INFRA-001)

The following tasks remain for Infrastructure layer:

### 1. EF Core Configuration
- [ ] Configure `NextQuestionDeterminant` as owned type or complex type
- [ ] Map to database columns (likely JSON/JSONB)
- [ ] Consider using PostgreSQL JSONB for efficient querying

### 2. Migration Creation
- [ ] Create migration to:
  - Drop old columns: `default_next_question_id`, `next_question_id`
  - Add new columns: `default_next_type`, `default_next_question_id` (or JSONB `default_next`)
  - Add new columns: `next_type`, `next_question_id` (or JSONB `next`)
  - Migrate existing data (0 → EndSurvey, positive IDs → GoToQuestion)

### 3. Repository Updates
- [ ] Update LINQ queries to work with value object
- [ ] Update cycle detection queries
- [ ] Consider navigation/eager loading strategies

### 4. Service Layer Updates
- [ ] Update services to use factory methods
- [ ] Update validation logic
- [ ] Update serialization (DTOs, JSON)

---

## Testing Recommendations

### Unit Tests (Post-INFRA-001)
1. **Value Object Persistence**:
   - Verify `NextQuestionDeterminant.End()` persists correctly
   - Verify `NextQuestionDeterminant.ToQuestion(id)` persists correctly
   - Verify null values persist correctly

2. **Entity Retrieval**:
   - Verify entities load with correct value object state
   - Verify nullable handling (null vs populated)

3. **Data Migration**:
   - Verify existing 0 values convert to `EndSurvey`
   - Verify existing positive IDs convert to `GoToQuestion`
   - Verify existing null values remain null

### Integration Tests (Post-SERVICE-001)
1. **Survey Flow**:
   - Create survey with `DefaultNext = End()`
   - Create survey with `DefaultNext = ToQuestion(id)`
   - Verify flow navigation works correctly

2. **Question Options**:
   - Create option with `Next = End()`
   - Create option with `Next = ToQuestion(id)`
   - Verify branching logic works correctly

---

## Backward Compatibility Strategy

### Nullable Properties:
- `null` = No flow configured (existing surveys without conditional flow)
- Allows gradual migration of existing surveys
- Validation can enforce flow configuration for new surveys if needed

### Database Migration:
- Old data: `default_next_question_id = 0` → `DefaultNext = NextQuestionDeterminant.End()`
- Old data: `default_next_question_id = N` → `DefaultNext = NextQuestionDeterminant.ToQuestion(N)`
- Old data: `default_next_question_id = NULL` → `DefaultNext = null`

---

## Files Modified

1. **C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Entities\Question.cs**
   - Added using statements
   - Replaced `DefaultNextQuestionId` with `DefaultNext`
   - Removed `DefaultNextQuestion` navigation property
   - Updated XML documentation

2. **C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Entities\QuestionOption.cs**
   - Added using statements
   - Replaced `NextQuestionId` with `Next`
   - Removed `NextQuestion` navigation property
   - Updated XML documentation

---

## Dependencies

### Depends On:
- ✅ CORE-001: NextQuestionDeterminant Value Object (COMPLETED)
- ✅ CORE-001: NextStepType Enum (COMPLETED)

### Required By:
- ⏳ INFRA-001: Update EF Core configurations (PENDING)
- ⏳ INFRA-002: Create database migration (PENDING)
- ⏳ SERVICE-001: Update service layer (PENDING)
- ⏳ DTO-001: Update DTOs (PENDING)

---

## Conclusion

CORE-002 has been **successfully completed** with the following achievements:

1. ✅ **Question entity updated**: `DefaultNext` replaces `DefaultNextQuestionId`
2. ✅ **QuestionOption entity updated**: `Next` replaces `NextQuestionId`
3. ✅ **Magic values eliminated**: No more `0 = end survey` logic
4. ✅ **Type safety enforced**: Value object ensures valid states only
5. ✅ **Build verified**: 0 errors, 0 warnings
6. ✅ **Documentation complete**: Clear usage instructions in XML comments
7. ✅ **Clean Architecture maintained**: Pure domain model, no framework dependencies

**Ready for INFRA-001**: EF Core configuration and database migration can now proceed.

---

**Task Completed**: 2025-11-23
**Next Task**: INFRA-001 (Update EF Core configurations for Question and QuestionOption)
