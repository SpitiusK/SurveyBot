# QuestionFlow API Debug Logging Guide

**Version**: 1.0
**Date**: 2025-11-23
**Applies to**: SurveyBot API v1.4.0+

---

## Overview

The QuestionFlowController now includes comprehensive debug logging at every stage of request processing. This guide explains how to use these logs for debugging and monitoring.

---

## Quick Start

### Enable Logging (Already Configured)

The following are pre-configured in `appsettings.Development.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "Microsoft.AspNetCore.Mvc.ModelBinding": "Trace",  // ← Model binding details
        "SurveyBot": "Information"  // ← QuestionFlowController logs
      }
    }
  }
}
```

### View Logs in Real-Time

```bash
# All API logs
docker-compose logs -f surveybot-api

# Filter for question flow operations
docker-compose logs -f surveybot-api | grep "UPDATE QUESTION FLOW"

# Filter for errors/warnings only
docker-compose logs -f surveybot-api | grep -E "(❌|⚠️)"

# Specific survey/question
docker-compose logs surveybot-api | grep "Survey ID: 3" | grep "Question ID: 9"
```

---

## Log Stages

### Stage 1: 📨 Request Received

**What it shows**: Incoming request details before processing

```
[18:30:45 INF] 📨 UPDATE QUESTION FLOW REQUEST
[18:30:45 INF]   Survey ID: 3
[18:30:45 INF]   Question ID: 9
[18:30:45 INF]   User ID: 1
[18:30:45 INF]   DefaultNext:
[18:30:45 INF]     - Type: GoToQuestion (0)
[18:30:45 INF]     - NextQuestionId: 10
[18:30:45 INF]   OptionNextDeterminants: NULL
```

**Use case**: Verify frontend is sending correct data structure

**What to check**:
- ✅ Type is integer (0 or 1), not string ("GoToQuestion")
- ✅ NextQuestionId is valid integer or NULL (not 0)
- ✅ OptionNextDeterminants matches question type (empty for non-branching)

---

### Stage 2: Model State Validation

#### Success Path: ✅

```
[18:30:45 INF] ✅ MODEL STATE VALIDATION PASSED
```

**Meaning**: DTO is valid according to data annotations and IValidatableObject rules

#### Failure Path: ❌

```
[18:30:45 WRN] ❌ MODEL STATE VALIDATION FAILED
[18:30:45 WRN]   Total Errors: 1
[18:30:45 WRN]   Field 'DefaultNext' has 1 error(s):
[18:30:45 WRN]     - Invalid navigation for default navigation: GoToQuestion type requires a valid NextQuestionId greater than 0.
```

**Use case**: Debug validation failures

**Common errors**:
1. **Type mismatch**: Frontend sends string instead of int
   - Error: "Cannot convert 'GoToQuestion' to NextStepType"
   - Fix: Send `type: 0` not `type: "GoToQuestion"`

2. **Missing NextQuestionId**: Type is GoToQuestion but NextQuestionId is null/0
   - Error: "GoToQuestion type requires a valid NextQuestionId greater than 0"
   - Fix: Ensure NextQuestionId is set when Type = GoToQuestion

3. **Invalid option**: OptionNextDeterminants references non-existent option
   - Error: "Option {id} does not belong to question {id}"
   - Fix: Verify option IDs from GET /questions/{id}

---

### Stage 3: 🔄 Service Layer Call

```
[18:30:45 INF] 🔄 Calling QuestionService.UpdateQuestionFlowAsync
[18:30:45 INF]   Survey ID: 3, Question ID: 9
[18:30:45 INF] ✅ Service layer completed successfully
```

**Use case**: Confirm controller successfully delegates to service

**If missing**: Controller failed before service call (check earlier stages)

---

### Stage 4: ✅ Success Response

```
[18:30:45 INF] ✅ UPDATE QUESTION FLOW COMPLETED SUCCESSFULLY
[18:30:45 INF]   Survey ID: 3, Question ID: 9
[18:30:45 INF]   Returned ConditionalFlowDto:
[18:30:45 INF]     - SupportsBranching: false
[18:30:45 INF]     - DefaultNext Type: GoToQuestion
[18:30:45 INF]     - OptionFlows Count: 0
```

**Use case**: Verify response structure

**What to check**:
- SupportsBranching matches question type (true for SingleChoice/Rating)
- DefaultNext is set for non-branching questions
- OptionFlows count matches number of options (for branching)

---

## Error Scenarios

### Scenario 1: Question Not Found (404)

```
[18:30:45 WRN] ❌ Question not found: Question with ID 999 not found
[18:30:45 WRN]   Survey ID: 3, Question ID: 999
```

**Cause**: Invalid questionId in route
**Fix**: Verify question exists: `GET /api/surveys/3/questions`

---

### Scenario 2: Survey Cycle Detected (400)

```
[18:30:45 INF] 📨 UPDATE QUESTION FLOW REQUEST
[18:30:45 INF]   ... (request details)
[18:30:45 INF] ✅ MODEL STATE VALIDATION PASSED
[18:30:45 INF] 🔄 Calling QuestionService.UpdateQuestionFlowAsync
[18:30:45 INF]   Survey ID: 3, Question ID: 9
[18:30:45 INF] ✅ Service layer completed successfully
[18:30:45 WRN] ❌ Survey cycle detected: Cycle detected in survey flow
[18:30:45 WRN]   Cycle Path: 9 → 10 → 11 → 9
```

**Cause**: Flow configuration creates circular reference
**Fix**: Change one question's next to break the cycle

**Example**:
```
Question 9 → 10
Question 10 → 11
Question 11 → 9  ← CYCLE!
```

**Solution**: Question 11 should point to END or different question

---

### Scenario 3: Database Constraint Violation (500)

```
[18:30:45 ERR] ❌ DATABASE UPDATE EXCEPTION
[18:30:45 ERR]   Survey ID: 3, Question ID: 9
[18:30:45 ERR]   DTO.DefaultNext: Type=GoToQuestion, NextQuestionId=999
[18:30:45 ERR]   Inner Exception: 23503: foreign key constraint "fk_questions_default_next_question"
[18:30:45 ERR]   ⚠️ FK CONSTRAINT VIOLATION DETECTED
[18:30:45 ERR]   Constraint: fk_questions_default_next_question
[18:30:45 ERR]   Attempted Value: Type=GoToQuestion, NextQuestionId=999
```

**Cause**: NextQuestionId references non-existent question
**Fix**: Validation should catch this earlier - investigate why it didn't

---

### Scenario 4: Unexpected Error (500)

```
[18:30:45 ERR] ❌ UNEXPECTED ERROR in UpdateQuestionFlow
[18:30:45 ERR]   Survey ID: 3, Question ID: 9
[18:30:45 ERR]   Exception Type: NullReferenceException
[18:30:45 ERR]   Stack Trace: at SurveyBot.Infrastructure.Services.QuestionService...
```

**Cause**: Unhandled exception in service layer
**Action**: Report bug with full stack trace

---

## Model Binding Trace Logs

**When enabled** (`Microsoft.AspNetCore.Mvc.ModelBinding: Trace`), you'll see:

```
[18:30:45 DBG] Attempting to bind parameter 'dto' of type 'UpdateQuestionFlowDto'
[18:30:45 DBG] Attempting to bind property 'UpdateQuestionFlowDto.DefaultNext' ...
[18:30:45 DBG] Attempting to bind property 'NextQuestionDeterminantDto.Type' ...
[18:30:45 DBG] Attempting to bind property 'NextQuestionDeterminantDto.NextQuestionId' ...
[18:30:45 DBG] Done attempting to bind property 'DefaultNext'
```

**Use case**: Debug why model binding fails (type conversion, missing fields)

**Common issues**:
- JSON keys don't match C# property names (case-sensitive)
- Enum sent as string instead of int
- Nested object structure mismatch

---

## Frontend Integration

### Correct Request Format

```typescript
// ✅ CORRECT
const flowUpdate = {
  defaultNext: {
    type: 0,  // Integer enum value (GoToQuestion)
    nextQuestionId: 10  // Valid question ID or null
  },
  optionNextDeterminants: {}  // Empty for non-branching
};

await api.put(`/api/surveys/${surveyId}/questions/${questionId}/flow`, flowUpdate);
```

### Common Mistakes

```typescript
// ❌ WRONG - String enum
const flowUpdate = {
  defaultNext: {
    type: "GoToQuestion",  // Should be 0
    nextQuestionId: 10
  }
};

// ❌ WRONG - Magic 0 for end survey
const flowUpdate = {
  defaultNext: {
    type: 0,
    nextQuestionId: 0  // Should be null or use type: 1 (EndSurvey)
  }
};

// ✅ CORRECT - End survey
const flowUpdate = {
  defaultNext: {
    type: 1,  // EndSurvey
    nextQuestionId: null
  }
};
```

---

## Enum Values Reference

**NextStepType**:
```
0 = GoToQuestion
1 = EndSurvey
```

**ALWAYS** send integers in API requests, not strings.

---

## Debugging Workflow

### Step 1: Check Request Received Log (📨)

**Look for**:
- Survey ID and Question ID match your request
- DefaultNext structure (Type and NextQuestionId)
- OptionNextDeterminants (if branching question)

**Red flags**:
- Type is logged as string instead of integer
- NextQuestionId is 0 (should be null or valid ID)
- OptionNextDeterminants is NULL for branching question

---

### Step 2: Check Model State Validation

**If ✅ PASSED**: Proceed to Step 3

**If ❌ FAILED**:
- Read field name and error message
- Check DTO validation rules in `UpdateQuestionFlowDto.cs`
- Fix frontend payload and retry

---

### Step 3: Check Service Layer Call (🔄)

**If missing**: Controller failed authorization or validation

**If present but no success log**: Service layer threw exception (check catch blocks)

---

### Step 4: Check Final Outcome

**✅ SUCCESS**: Request completed, check response DTO details

**❌ ERROR**: Check exception type and context in catch block logs

---

## Production Monitoring

### Recommended Log Filters

**Errors only**:
```bash
docker-compose logs surveybot-api | grep "❌"
```

**Validation failures** (identify bad client requests):
```bash
docker-compose logs surveybot-api | grep "MODEL STATE VALIDATION FAILED" -A 10
```

**Cycle detection** (track flow configuration issues):
```bash
docker-compose logs surveybot-api | grep "cycle detected" -i
```

**Successful updates** (audit trail):
```bash
docker-compose logs surveybot-api | grep "UPDATE QUESTION FLOW COMPLETED SUCCESSFULLY"
```

---

## Log Retention

**Development** (`appsettings.Development.json`):
- Console: Real-time output
- File: `logs/surveybot-dev-YYYYMMDD.log` (7 days, 10 MB per file)
- Seq: http://localhost:5341 (optional)

**Production** (`appsettings.json`):
- Should reduce to `Warning` level for model binding
- Keep `Information` for QuestionFlowController
- Archive logs after 30 days

---

## Troubleshooting Tips

### Logs Not Appearing

1. Check log level in `appsettings.Development.json`:
   ```json
   "SurveyBot": "Information"  // Not "Warning" or "Error"
   ```

2. Restart API after config changes:
   ```bash
   docker-compose restart surveybot-api
   ```

3. Check Docker logs:
   ```bash
   docker-compose logs surveybot-api | tail -100
   ```

### Too Much Logging

**Reduce model binding trace** (if overwhelming):
```json
"Microsoft.AspNetCore.Mvc.ModelBinding": "Information"  // Instead of "Trace"
```

**Filter Docker logs**:
```bash
docker-compose logs -f surveybot-api | grep -v "ModelBinding"
```

---

## Related Documentation

- [API Layer CLAUDE.md](../../src/SurveyBot.API/CLAUDE.md) - Complete API documentation
- [API Quick Reference](QUICK-REFERENCE.md) - Endpoint reference
- [Logging & Error Handling](LOGGING-ERROR-HANDLING.md) - Error handling patterns
- [Infrastructure CLAUDE.md](../../src/SurveyBot.Infrastructure/CLAUDE.md) - Service layer details

---

## Summary

**Log Stages**:
1. 📨 Request received → DTO structure
2. ✅/❌ Model state validation
3. 🔄 Service layer call
4. ✅ Success response OR ❌ Exception

**Quick Debugging**:
- Check request log for data structure issues
- Check validation log for DTO errors
- Check exception logs for service failures
- Use grep with emoji indicators for fast filtering

**Key Points**:
- ALL requests log surveyId and questionId
- Enum values logged as both name and integer
- NULL values explicitly logged (not hidden)
- Every code path has logging (no silent failures)

---

**Last Updated**: 2025-11-23
**Version**: 1.0
**Maintainer**: SurveyBot Development Team
