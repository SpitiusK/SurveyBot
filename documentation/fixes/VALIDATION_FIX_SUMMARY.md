# QuestionService Validation Fix - Quick Summary

**Date**: 2025-11-23
**Status**: ✅ COMPLETED
**Build**: ✅ SUCCESS

---

## What Was Fixed

**File**: `src/SurveyBot.Infrastructure/Services/QuestionService.cs`
**Method**: `UpdateQuestionFlowAsync`
**Lines**: 524-665 (142 lines, expanded from 47)

### Root Problem
Direct assignment of DTO values to entity FK properties without validation, causing PostgreSQL FK constraint violations.

```csharp
// ❌ BEFORE: Direct assignment - causes FK violations
question.DefaultNextQuestionId = dto.DefaultNextQuestionId;
```

### Solution
Multi-stage validation before assignment:

```csharp
// ✅ AFTER: Comprehensive validation
if (dto.DefaultNextQuestionId.HasValue)
{
    if (dto.DefaultNextQuestionId.Value == 0)  // EndOfSurveyMarker
        question.DefaultNextQuestionId = null;  // Map to NULL in FK
    else
    {
        // Validate target question exists
        var targetQuestion = await _questionRepository.GetByIdAsync(dto.DefaultNextQuestionId.Value);
        if (targetQuestion == null)
            throw new QuestionNotFoundException(dto.DefaultNextQuestionId.Value);

        // Prevent self-reference
        if (dto.DefaultNextQuestionId.Value == id)
            throw new InvalidOperationException($"Question {id} cannot reference itself");

        question.DefaultNextQuestionId = dto.DefaultNextQuestionId.Value;
    }
}
else
    question.DefaultNextQuestionId = null;  // Clear flow config
```

---

## Key Improvements

### 1. DefaultNextQuestionId Validation
- ✅ Validates target question exists
- ✅ Prevents self-reference loops
- ✅ Properly handles EndOfSurveyMarker (0 → NULL)
- ✅ Clear error messages

### 2. Option Flow Validation
- ✅ Fail-fast on invalid option IDs (was silent skip)
- ✅ Validates each option's NextQuestionId
- ✅ Prevents self-references
- ✅ Proper NULL handling for end-of-survey

### 3. Exception Handling
- ✅ Throws `QuestionNotFoundException` for invalid IDs
- ✅ Throws `InvalidOperationException` for invalid operations
- ✅ Comprehensive structured logging
- ✅ Catches and logs unexpected errors

---

## Validation Rules

| Input | Database | Action |
|-------|----------|--------|
| `null` | `NULL` | Clear flow config ✅ |
| `0` | `NULL` | End survey ✅ |
| Valid ID | Question ID | Verify exists, assign ✅ |
| Invalid ID | N/A | Throw QuestionNotFoundException ❌ |
| Self-ref | N/A | Throw InvalidOperationException ❌ |

---

## Error Messages

### QuestionNotFoundException (404)
```
"Question with ID {id} was not found."
```

**When**:
- DefaultNextQuestionId references non-existent question
- Option NextQuestionId references non-existent question

### InvalidOperationException (400)
```
"Option {optionId} does not exist for question {questionId}"
"Question {id} cannot reference itself as next question"
```

**When**:
- Invalid option ID in OptionNextQuestions
- Self-reference attempt

---

## Logging Examples

**Successful Flow Update**:
```
[INFO] Updating flow for question 5
[INFO] Set DefaultNextQuestionId for question 5 to 6
[INFO] Updating 2 option-specific flows for question 5
[INFO] Set option 10 (question 5) to next question 7
[INFO] Set option 11 (question 5) to end survey
[INFO] Successfully updated flow for question 5
```

**Invalid Question ID**:
```
[WARN] Invalid DefaultNextQuestionId 999 for question 5 - question does not exist
```

**Invalid Option ID**:
```
[ERROR] Option 99 not found for question 5. Available options: 10, 11
```

---

## Build Status

```
Build succeeded.
    4 Warning(s) [pre-existing, unrelated]
    0 Error(s)

Time Elapsed 00:00:05.45
```

---

## Testing Checklist

### Required Manual Tests

- [ ] Valid flow: Set DefaultNextQuestionId to existing question
- [ ] End survey: Set DefaultNextQuestionId = 0, verify NULL in DB
- [ ] Invalid ID: Set DefaultNextQuestionId = 9999, expect 404
- [ ] Self-reference: Set DefaultNextQuestionId = questionId, expect 400
- [ ] Invalid option: Use non-existent option ID, expect 400
- [ ] Clear flow: Set DefaultNextQuestionId = null, verify NULL in DB

### API Endpoint

**Endpoint**: `PUT /api/questions/{id}/flow`

**Request Body**:
```json
{
  "defaultNextQuestionId": 6,
  "optionNextQuestions": {
    "10": 7,
    "11": 0
  }
}
```

**Test with**: Swagger UI at `http://localhost:5000/swagger`

---

## Performance Impact

**Before**: 1 database query
**After**: 1 + N queries (N = unique target question IDs)

**Example**:
- Question with DefaultNextQuestionId + 3 option flows
- Total queries: 1 + 4 = 5 queries

**Justification**:
- Flow updates are infrequent (admin operations)
- Data integrity > minor performance cost
- Future optimization possible (batch validation)

---

## Related Fixes

**Multi-layer Bug Fix Plan**:

1. ✅ **Layer 1** - QuestionFlowController input validation (already completed)
2. ✅ **Layer 2** - QuestionService validation (this fix)
3. ⏳ **Layer 3** - Database/Option FK handling (if needed)

---

## Impact

### Before
- ❌ FK constraint violations (PostgreSQL error 23503)
- ❌ Silent failures (skipped invalid option IDs)
- ❌ EndOfSurveyMarker stored as FK (invalid)
- ❌ Self-references allowed (infinite loops)

### After
- ✅ No FK violations
- ✅ Fail-fast with clear errors
- ✅ EndOfSurveyMarker → NULL (correct)
- ✅ Self-references prevented
- ✅ Comprehensive logging

---

## Next Steps

1. ⏳ Manual testing with Postman/Swagger
2. ⏳ Database verification (check NULL values)
3. ⏳ Frontend integration testing
4. ⏳ Add unit/integration tests
5. ⏳ Update CLAUDE.md documentation

---

**Full Report**: `QUESTIONSERVICE_VALIDATION_FIX_REPORT.md`
