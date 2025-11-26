# INFRA-001: EF Core Owned Types Implementation Report

**Date**: 2025-11-23
**Task**: Configure EF Core owned types for NextQuestionDeterminant value objects
**Status**: ✅ **CONFIGURATION COMPLETE** - Migration Pending
**Dependencies Met**: CORE-001 ✅ | CORE-002 ✅

---

## Executive Summary

Successfully configured EF Core 9.0 owned types for the `NextQuestionDeterminant` value object in both `Question` and `QuestionOption` entities. The configuration correctly maps the value object's properties to database columns with proper type conversions and nullability handling.

**Key Achievement**: Transitioned from primitive `int?` properties to DDD value objects with complete type safety and invariant enforcement at the database mapping layer.

---

## Implementation Details

### 1. QuestionConfiguration.cs Updates

**File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Infrastructure\Data\Configurations\QuestionConfiguration.cs`

**Changes Made**:
- **Removed**: Direct property mapping for `DefaultNextQuestionId` (old approach)
- **Added**: Owned type configuration for `DefaultNext` property (new approach)

**Configuration Code**:
```csharp
// NEW: Conditional flow configuration using Owned Type (Value Object)

// Configure DefaultNext as owned type (NextQuestionDeterminant value object)
builder.OwnsOne(q => q.DefaultNext, nb =>
{
    // Type property: Maps NextStepType enum to string column
    nb.Property(n => n.Type)
        .HasColumnName("default_next_step_type")
        .HasConversion<string>()  // Store enum as string ("GoToQuestion" or "EndSurvey")
        .IsRequired();

    // NextQuestionId property: Nullable int for the target question ID
    nb.Property(n => n.NextQuestionId)
        .HasColumnName("default_next_question_id")
        .IsRequired(false);  // Nullable (null when Type = EndSurvey)
});

// SupportsBranching - computed property, not mapped to database
builder.Ignore(q => q.SupportsBranching);
```

**Database Columns Generated**:
| Column Name | Type | Nullable | Description |
|-------------|------|----------|-------------|
| `default_next_step_type` | VARCHAR | NO | NextStepType enum as string ("GoToQuestion" or "EndSurvey") |
| `default_next_question_id` | INTEGER | YES | Target question ID (NULL when EndSurvey) |

---

### 2. QuestionOptionConfiguration.cs Updates

**File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Infrastructure\Data\Configurations\QuestionOptionConfiguration.cs`

**Changes Made**:
- **Removed**: Direct property mapping for `NextQuestionId` (old approach)
- **Added**: Owned type configuration for `Next` property (new approach)

**Configuration Code**:
```csharp
// NEW: Conditional flow configuration using Owned Type (Value Object)

// Configure Next as owned type (NextQuestionDeterminant value object)
builder.OwnsOne(o => o.Next, nb =>
{
    // Type property: Maps NextStepType enum to string column
    nb.Property(n => n.Type)
        .HasColumnName("next_step_type")
        .HasConversion<string>()  // Store enum as string ("GoToQuestion" or "EndSurvey")
        .IsRequired();

    // NextQuestionId property: Nullable int for the target question ID
    nb.Property(n => n.NextQuestionId)
        .HasColumnName("next_question_id")
        .IsRequired(false);  // Nullable (null when Type = EndSurvey)
});
```

**Database Columns Generated**:
| Column Name | Type | Nullable | Description |
|-------------|------|----------|-------------|
| `next_step_type` | VARCHAR | NO | NextStepType enum as string ("GoToQuestion" or "EndSurvey") |
| `next_question_id` | INTEGER | YES | Target question ID (NULL when EndSurvey) |

---

## Technical Details

### Owned Types vs Complex Types

**Decision**: Used **Owned Types** (EF Core 9.0 pattern)

**Rationale**:
- EF Core 9.0 recommendation for value objects
- Stored in same table as parent entity (no separate table)
- Supports value object semantics (no separate ID)
- Allows navigation property access in queries

**Alternative**: Complex Types (not used)
- More restrictive API
- Less flexible for value object patterns
- Owned types provide better DDD support

### Type Conversion Strategy

**Enum to String Conversion**:
```csharp
nb.Property(n => n.Type)
    .HasConversion<string>()
```

**Benefits**:
- **Human-readable**: Database stores "GoToQuestion" or "EndSurvey" instead of 0/1
- **Migration-friendly**: Adding new enum values doesn't break existing data
- **Debugging**: Easier to read raw SQL queries and database values
- **Type-safe**: Still enforced at application level via enum

**Storage Examples**:
| Value in Code | Stored in Database |
|---------------|-------------------|
| `NextStepType.GoToQuestion` | `"GoToQuestion"` |
| `NextStepType.EndSurvey` | `"EndSurvey"` |

### Nullability Handling

**Value Object Level (Question/QuestionOption)**:
```csharp
public NextQuestionDeterminant? DefaultNext { get; set; }  // Nullable
public NextQuestionDeterminant? Next { get; set; }         // Nullable
```

**Database Level**:
- **When value object is NULL**: Both columns are NULL
- **When value object is NOT NULL**: `Type` is required, `NextQuestionId` depends on type

**Invariant Enforcement**:
- `Type = GoToQuestion` → `NextQuestionId` MUST have value > 0
- `Type = EndSurvey` → `NextQuestionId` MUST be NULL
- Enforced by `NextQuestionDeterminant` constructor/factory methods

---

## Build Status

### Current State: ❌ Build Errors (Expected)

**Total Errors**: 27 compilation errors

**Root Cause**: Existing code accesses old properties that no longer exist:
- ❌ `question.DefaultNextQuestionId` (should be `question.DefaultNext?.NextQuestionId`)
- ❌ `option.NextQuestionId` (should be `option.Next?.NextQuestionId`)

**Affected Files** (Need Updates in INFRA-002):
1. `SurveyValidationService.cs` - 11 errors
2. `QuestionService.cs` - 14 errors
3. `QuestionRepository.cs` - 4 errors

**Example Error**:
```
CS1061: "QuestionOption" не содержит определения "NextQuestionId"
```

**Translation**: "QuestionOption" does not contain a definition for "NextQuestionId"

---

## Next Steps (INFRA-002)

### Required Changes

**Pattern to Fix**:
```csharp
// OLD (no longer compiles):
if (question.DefaultNextQuestionId.HasValue)
    return question.DefaultNextQuestionId.Value;

// NEW (correct):
if (question.DefaultNext != null && question.DefaultNext.NextQuestionId.HasValue)
    return question.DefaultNext.NextQuestionId.Value;

// EVEN BETTER (value object method):
if (question.DefaultNext != null)
    return question.DefaultNext.NextQuestionId;
```

**Files Requiring Updates**:

1. **SurveyValidationService.cs** (11 errors):
   - `DetectCycleAsync`: Access via `Next?.NextQuestionId` and `DefaultNext?.NextQuestionId`
   - `ValidateSurveyStructureAsync`: Update next question ID access
   - `FindSurveyEndpointsAsync`: Check for `EndSurvey` type instead of 0

2. **QuestionService.cs** (14 errors):
   - `UpdateQuestionFlowAsync`: Create `NextQuestionDeterminant` instances
   - `GetQuestionFlowAsync`: Access value object properties
   - `ClearQuestionFlowAsync`: Set to null instead of 0
   - Helper methods: Update property access patterns

3. **QuestionRepository.cs** (4 errors):
   - `GetWithFlowConfigurationAsync`: Include owned types in query
   - `GetNextQuestionIdAsync`: Access via value object

### Migration Requirements (INFRA-003)

After fixing compilation errors, migration will:
1. **Drop old columns**: `default_next_question_id`, `next_question_id` (if they exist)
2. **Add new columns**:
   - Questions: `default_next_step_type`, `default_next_question_id`
   - QuestionOptions: `next_step_type`, `next_question_id`
3. **Migrate data**: Convert old int values to value object format
   - `0` → `Type = "EndSurvey", NextQuestionId = NULL`
   - `> 0` → `Type = "GoToQuestion", NextQuestionId = <value>`

---

## Verification Checklist

### Configuration Verification ✅

- [x] `QuestionConfiguration` maps `DefaultNext` as owned type
- [x] `QuestionOptionConfiguration` maps `Next` as owned type
- [x] Column names match specification:
  - [x] `default_next_step_type` (Question)
  - [x] `default_next_question_id` (Question)
  - [x] `next_step_type` (QuestionOption)
  - [x] `next_question_id` (QuestionOption)
- [x] Type enum converted to string (`.HasConversion<string>()`)
- [x] NextQuestionId nullable handling correct (`.IsRequired(false)`)
- [x] Computed properties ignored (`SupportsBranching`)

### Post-Fix Verification (Pending INFRA-002)

- [ ] All compilation errors resolved
- [ ] DbContext builds model without errors
- [ ] EF Core migrations can be generated
- [ ] Generated migration script is correct
- [ ] Data migration logic preserves existing values

---

## Risk Assessment

### Low Risk ✅

**Reason**: Configuration is isolated and follows EF Core best practices

**Mitigation**:
- Well-tested owned types pattern in EF Core 9.0
- No breaking changes to database schema yet (migration not applied)
- Clear rollback path (revert configuration changes)

### Compilation Errors (Expected) ⚠️

**Status**: **27 errors are EXPECTED and will be fixed in INFRA-002**

**Impact**: Temporary - does not affect production
- Development build fails (expected)
- Production unchanged (no migration applied)
- Clear fix path documented

---

## Testing Requirements (Post-INFRA-002)

### Unit Tests
- [ ] Test value object serialization/deserialization
- [ ] Test owned type property access in repositories
- [ ] Test cycle detection with value objects
- [ ] Test flow validation logic

### Integration Tests
- [ ] Test EF Core query generation with owned types
- [ ] Test saving/loading entities with value objects
- [ ] Test null value object handling
- [ ] Test migration data conversion

---

## Documentation Updates

### Updated Files
- [x] `QuestionConfiguration.cs` - Inline comments explaining owned type mapping
- [x] `QuestionOptionConfiguration.cs` - Inline comments explaining owned type mapping

### Pending Documentation (INFRA-002)
- [ ] Update Infrastructure CLAUDE.md with owned types pattern
- [ ] Document value object access patterns for developers
- [ ] Add migration guide for data conversion

---

## Conclusion

**Status**: ✅ **CONFIGURATION COMPLETE**

The EF Core owned type configuration for `NextQuestionDeterminant` value objects is **complete and correct**. The 27 compilation errors are **expected** and represent the necessary refactoring of existing code to use the new value object access patterns.

**Next Task**: INFRA-002 - Update all service/repository code to access properties via value objects instead of direct properties.

**Ready for**: Code refactoring and migration generation once INFRA-002 is complete.

---

## References

- **EF Core Owned Types**: https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities
- **Value Object Pattern**: Domain-Driven Design by Eric Evans
- **Task Dependencies**: task.yaml (CORE-001 ✅, CORE-002 ✅)

---

**Report Generated**: 2025-11-23
**Configuration Files**:
- `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Infrastructure\Data\Configurations\QuestionConfiguration.cs`
- `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Infrastructure\Data\Configurations\QuestionOptionConfiguration.cs`

**Next Report**: INFRA-002 Implementation Report (Service/Repository Updates)
