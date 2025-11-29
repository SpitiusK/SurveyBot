# Number and Date Question Type Publication Failure - Diagnostic Report

**Report Date**: 2025-11-28
**Issue ID**: NUMBER-DATE-PUBLISH-FAILURE
**Severity**: Critical
**Status**: Partially Fixed (Migration Required)

---

## Executive Summary

Publishing surveys with `QuestionType.Number` (value 5) or `QuestionType.Date` (value 6) fails with HTTP 500 error due to a **two-layer problem**:

1. **Application Layer**: Missing validation cases in `QuestionService.cs` ✅ **FIXED**
2. **Database Layer**: PostgreSQL CHECK constraint `chk_question_type` rejects values 5 and 6 ❌ **REQUIRES MIGRATION**

The v1.5.1 feature implementation for Number and Date question types was **incomplete** - the QuestionType enum was updated, but the database schema and service validation logic were not synchronized.

---

## Problem Timeline

### Initial Symptom (2025-11-28 21:16:09)
- **User Action**: Attempted to publish survey with "Number" question type via frontend
- **Result**: HTTP 500 Internal Server Error
- **Frontend Log**: `AxiosError: Request failed with status code 500`
- **Error Message**: "An error occurred while creating the question"

### First Analysis
- **Tool Used**: docker-log-analyzer agent on surveybot-api container
- **Finding**: `InvalidQuestionTypeException` thrown at `QuestionService.ValidateQuestionOptionsAsync():480`
- **Root Cause Identified**: Missing switch cases for `QuestionType.Number` (5) and `QuestionType.Date` (6)

### First Fix Applied (2025-11-28 21:18)
- **File Modified**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Infrastructure\Services\QuestionService.cs`
- **Changes**:
  1. Added validation case for `QuestionType.Number` (lines 479-485)
  2. Added validation case for `QuestionType.Date` (lines 487-493)
  3. Updated `GetQuestionTypeValidationAsync` with Number and Date rules (lines 399-411)
- **API Container**: Restarted via `docker restart surveybot-api`

### Continued Error (2025-11-28 21:27:10)
- **User Report**: Error still occurring after fix
- **Frontend Log**: Same HTTP 500 error with identical symptoms
- **Request Payload**: `questionType: 5` (Number)

### Second Analysis - True Root Cause Discovered
- **Tool Used**: docker-log-analyzer agent (second analysis)
- **Critical Finding**: PostgreSQL CHECK constraint violation
- **Database Error**:
  ```
  23514: new row for relation "questions" violates check constraint "chk_question_type"
  ```
- **Constraint Definition**: `CHECK (question_type = ANY (ARRAY[0, 1, 2, 3, 4]))`
- **Impact**: Database-level rejection of QuestionType values 5 (Number) and 6 (Date)

---

## Technical Analysis

### Problem Layer 1: Application Code ✅ FIXED

**Location**: `src/SurveyBot.Infrastructure/Services/QuestionService.cs`

**Issue**: The `ValidateQuestionOptionsAsync` method had a switch statement that only handled QuestionType values 0-4:

```csharp
// BEFORE (Missing Number and Date cases)
switch (questionType)
{
    case QuestionType.Text:        // 0
    case QuestionType.SingleChoice: // 1
    case QuestionType.MultipleChoice: // 2
    case QuestionType.Rating:      // 3
    case QuestionType.Location:    // 4
        // ... validation logic ...

    default:
        throw new InvalidQuestionTypeException($"Unsupported question type: {questionType}");
}
```

**Fix Applied**:
```csharp
// AFTER (Added Number and Date cases)
case QuestionType.Number:
    // Number questions should not have options
    if (options != null && options.Any())
    {
        return QuestionValidationResult.Failure("Number questions should not have options.");
    }
    return QuestionValidationResult.Success();

case QuestionType.Date:
    // Date questions should not have options
    if (options != null && options.Any())
    {
        return QuestionValidationResult.Failure("Date questions should not have options.");
    }
    return QuestionValidationResult.Success();
```

**Additional Fix**: Updated `GetQuestionTypeValidationAsync` to include validation rules for Number and Date:

```csharp
case QuestionType.Number:
    rules["requiresOptions"] = false;
    rules["supportsRange"] = true;
    rules["supportsDecimalPlaces"] = true;
    rules["description"] = "Numeric input (integer or decimal)";
    break;

case QuestionType.Date:
    rules["requiresOptions"] = false;
    rules["supportsDateRange"] = true;
    rules["dateFormat"] = "DD.MM.YYYY";
    rules["description"] = "Date input in DD.MM.YYYY format";
    break;
```

### Problem Layer 2: Database Schema ❌ REQUIRES MIGRATION

**Location**: PostgreSQL database `surveybot_db`, table `questions`

**Issue**: CHECK constraint `chk_question_type` only allows values `[0, 1, 2, 3, 4]`

**Current Constraint**:
```sql
CONSTRAINT chk_question_type CHECK (question_type = ANY (ARRAY[0, 1, 2, 3, 4]))
```

**Database Rejection**:
```
PostgreSQL Error 23514: new row for relation "questions" violates check constraint "chk_question_type"
Detail: Failing row contains (questionType: 5)
```

**Required Fix**: Update constraint to allow values 5 (Number) and 6 (Date):
```sql
CONSTRAINT chk_question_type CHECK (question_type = ANY (ARRAY[0, 1, 2, 3, 4, 5, 6]))
```

---

## Request/Response Flow Analysis

### Failed Request Path

1. **Frontend** → POST `/api/surveys/{surveyId}/questions`
   ```json
   {
     "questionType": 5,
     "text": "Age Question",
     "orderIndex": 0
   }
   ```

2. **API Controller** → Receives request, calls `QuestionService.CreateQuestionAsync()`

3. **QuestionService** → ✅ **PASS** - Validates question type (after fix)
   - `ValidateQuestionOptionsAsync()` now recognizes QuestionType.Number
   - Returns `QuestionValidationResult.Success()`

4. **QuestionRepository** → Attempts to insert into database
   ```csharp
   await _context.Questions.AddAsync(question);
   await _context.SaveChangesAsync();
   ```

5. **PostgreSQL Database** → ❌ **REJECT** - CHECK constraint violation
   ```
   ERROR: new row violates check constraint "chk_question_type"
   DETAIL: Failing row contains (question_type = 5)
   ```

6. **Exception Propagation**:
   - EF Core → `DbUpdateException`
   - QuestionService → Re-thrown
   - API Controller → Caught by global exception handler
   - Frontend → HTTP 500 with generic error message

---

## QuestionType Enum Definition

**Location**: `src/SurveyBot.Core/Entities/QuestionType.cs`

```csharp
public enum QuestionType
{
    Text = 0,              // Free-form text answer
    SingleChoice = 1,      // Single choice from multiple options (radio button)
    MultipleChoice = 2,    // Multiple choices from multiple options (checkboxes)
    Rating = 3,            // Numeric rating (1-5 scale)
    Location = 4,          // Geographic location (latitude/longitude)
    Number = 5,            // Numeric input (integer or decimal) - v1.5.1 ❌ NOT IN DB
    Date = 6               // Date input (DD.MM.YYYY format) - v1.5.1 ❌ NOT IN DB
}
```

**Discrepancy**: Enum defines values 0-6, but database constraint only allows 0-4.

---

## Root Cause Analysis

### Why Did This Happen?

The v1.5.1 feature implementation for Number and Date question types was **incomplete**:

1. ✅ **Core Layer Updated**: `QuestionType` enum extended with Number (5) and Date (6)
2. ✅ **Value Objects Created**: `NumberAnswerValue.cs` and `DateAnswerValue.cs` implemented
3. ✅ **Bot Handlers Created**: `NumberQuestionHandler.cs` and `DateQuestionHandler.cs` added
4. ✅ **Statistics DTOs Created**: `NumberStatisticsDto.cs` and `DateStatisticsDto.cs` implemented
5. ❌ **Service Validation NOT Updated**: `QuestionService.cs` validation logic missed
6. ❌ **Database Migration NOT Created**: No migration to update `chk_question_type` constraint

### Synchronization Failure

The implementation followed a **partial rollout pattern** where:
- Domain models were extended (QuestionType enum)
- Supporting infrastructure was added (handlers, value objects)
- **Critical validation and database layers were overlooked**

This created a **layer synchronization gap**:
```
Core Layer (QuestionType enum)          → Values 0-6 ✅
Infrastructure Layer (QuestionService)  → Values 0-4 ❌ (NOW FIXED ✅)
Database Layer (CHECK constraint)       → Values 0-4 ❌ (STILL BROKEN)
```

---

## Impact Assessment

### Severity: Critical

- **User Impact**: Cannot create surveys with Number or Date question types
- **Business Impact**: Core v1.5.1 feature completely non-functional
- **Data Integrity**: No risk - database correctly rejects invalid state
- **Scope**: Affects all users attempting to use Number/Date question types

### Affected Components

1. **Frontend**: Survey Builder wizard (Number and Date question type selection)
2. **API**: `POST /api/surveys/{surveyId}/questions` endpoint
3. **Infrastructure**: `QuestionService.CreateQuestionAsync()` method
4. **Database**: `questions` table INSERT operations

### Error Manifestation

**User Experience**:
- Survey creation appears to succeed in UI
- Publishing step fails with generic error message
- No clear indication of what went wrong
- No validation feedback during question creation

**Technical Symptoms**:
- HTTP 500 Internal Server Error
- Generic error message: "An error occurred while creating the question"
- Database constraint violation logged in API container
- Frontend receives no actionable error details

---

## Fix Status

### Completed Fixes ✅

1. **Application-Level Validation** (2025-11-28 21:18)
   - File: `src/SurveyBot.Infrastructure/Services/QuestionService.cs`
   - Added `QuestionType.Number` validation case (lines 479-485)
   - Added `QuestionType.Date` validation case (lines 487-493)
   - Updated `GetQuestionTypeValidationAsync` with Number/Date rules (lines 399-411)
   - Status: **COMPLETE**

### Required Fixes ❌

2. **Database Schema Migration** (PENDING)
   - Action Required: Create EF Core migration to update CHECK constraint
   - Migration Name: `UpdateQuestionTypeConstraintForNumberAndDate`
   - SQL Change:
     ```sql
     ALTER TABLE questions
     DROP CONSTRAINT IF EXISTS chk_question_type;

     ALTER TABLE questions
     ADD CONSTRAINT chk_question_type
     CHECK (question_type = ANY (ARRAY[0, 1, 2, 3, 4, 5, 6]));
     ```
   - Status: **NOT STARTED**

---

## Recommended Fix Implementation

### Step 1: Create Database Migration

```bash
cd src/SurveyBot.API
dotnet ef migrations add UpdateQuestionTypeConstraintForNumberAndDate
```

### Step 2: Verify Migration SQL

Check the generated migration file to ensure it contains:

```csharp
migrationBuilder.Sql(@"
    ALTER TABLE questions
    DROP CONSTRAINT IF EXISTS chk_question_type;

    ALTER TABLE questions
    ADD CONSTRAINT chk_question_type
    CHECK (question_type = ANY (ARRAY[0, 1, 2, 3, 4, 5, 6]));
");
```

### Step 3: Apply Migration

```bash
dotnet ef database update
```

### Step 4: Restart API Container

```bash
docker restart surveybot-api
```

### Step 5: Verify Fix

Test creating a Number question type:

```bash
# Via frontend Survey Builder
# OR via API:
curl -X POST http://localhost:5000/api/surveys/{surveyId}/questions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "questionType": 5,
    "text": "What is your age?",
    "orderIndex": 0
  }'
```

Expected: HTTP 201 Created (not HTTP 500)

---

## Testing Recommendations

### Pre-Migration Tests (Should FAIL)
1. Attempt to create Number question (questionType: 5)
2. Attempt to create Date question (questionType: 6)
3. Expected: HTTP 500 with database constraint error

### Post-Migration Tests (Should PASS)
1. Create Number question with valid payload
2. Create Date question with valid payload
3. Verify questions appear in database with correct question_type values
4. Verify frontend Survey Builder can publish surveys with Number/Date questions
5. Test survey taking flow with Number/Date questions via Telegram bot

### Regression Tests
1. Verify existing question types (0-4) still work correctly
2. Test question validation for all question types
3. Verify database constraint rejects invalid question_type values (e.g., 7, -1)

---

## Prevention Measures

### For Future Feature Implementations

1. **Checklist Approach**:
   - [ ] Core layer updated (entities, enums, value objects)
   - [ ] Infrastructure layer updated (services, repositories)
   - [ ] Database schema updated (migrations applied)
   - [ ] API layer updated (controllers, DTOs)
   - [ ] Bot layer updated (handlers, state management)
   - [ ] Frontend updated (UI components, validation)
   - [ ] Tests added (unit, integration, end-to-end)

2. **Layer Synchronization Verification**:
   - Run integration tests that span all layers
   - Verify database constraints match domain model invariants
   - Check for switch statements that need updating when enums change

3. **Database-First Validation**:
   - Always verify database schema supports new domain model values
   - Test database INSERT operations before application-level testing
   - Use CHECK constraints as additional validation layer

4. **Documentation Updates**:
   - Update CLAUDE.md files when adding new features
   - Document database schema changes in migration comments
   - Maintain feature implementation checklists

---

## Related Files

### Modified Files (Current Fix)
- `src/SurveyBot.Infrastructure/Services/QuestionService.cs` (lines 399-411, 479-493)

### Files Requiring Migration
- `src/SurveyBot.API/Migrations/` (new migration to be created)
- Database: `questions` table constraint `chk_question_type`

### Related v1.5.1 Implementation Files
- `src/SurveyBot.Core/Entities/QuestionType.cs`
- `src/SurveyBot.Core/ValueObjects/Answers/NumberAnswerValue.cs`
- `src/SurveyBot.Core/ValueObjects/Answers/DateAnswerValue.cs`
- `src/SurveyBot.Bot/Handlers/Questions/NumberQuestionHandler.cs`
- `src/SurveyBot.Bot/Handlers/Questions/DateQuestionHandler.cs`
- `src/SurveyBot.Core/DTOs/Statistics/NumberStatisticsDto.cs`
- `src/SurveyBot.Core/DTOs/Statistics/DateStatisticsDto.cs`

---

## Conclusion

The Number and Date question type publication failure was caused by **incomplete feature implementation** across the Clean Architecture layers. While the domain model and supporting infrastructure were updated for v1.5.1, the service validation logic and database schema were not synchronized.

**Current Status**:
- ✅ Application-level validation fixed (QuestionService.cs updated)
- ❌ Database schema still rejects Number (5) and Date (6) values
- ⏳ Migration required to complete the fix

**Next Steps**:
1. Create and apply database migration to update `chk_question_type` constraint
2. Test Number and Date question creation end-to-end
3. Verify v1.5.1 feature is fully functional

**Estimated Time to Resolution**: 10-15 minutes (migration creation + application + testing)

---

**Report Author**: Claude Code (docker-log-analyzer agent)
**Analysis Tools Used**: Docker log analysis, codebase inspection, database schema review
**Documentation**: C:\Users\User\Desktop\SurveyBot\documentation\fixes\NUMBER_DATE_QUESTION_TYPE_DIAGNOSTIC_REPORT.md
