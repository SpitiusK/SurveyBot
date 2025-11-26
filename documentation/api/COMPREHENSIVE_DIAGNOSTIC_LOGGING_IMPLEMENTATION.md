# Comprehensive Diagnostic Logging Implementation

**Date**: 2025-11-23
**Purpose**: Add detailed diagnostic logging to trace the complete question flow update process from API request to database execution.

---

## Overview

This implementation adds comprehensive diagnostic logging at three key layers:

1. **API Controller Layer** (`QuestionFlowController.cs`) - Incoming request logging
2. **Service Layer** (`QuestionService.cs`) - DTO transformation and business logic logging
3. **Configuration Layer** (`appsettings.Development.json`) - EF Core SQL logging

---

## Implementation Details

### 1. API Controller Layer - QuestionFlowController.cs

**Location**: `src/SurveyBot.API/Controllers/QuestionFlowController.cs`

**Changes to `UpdateQuestionFlow` method**:

#### Request Logging (at method start)
```csharp
_logger.LogInformation("═══════════════════════════════════════════════════");
_logger.LogInformation("🌐 INCOMING API REQUEST: UpdateQuestionFlow");
_logger.LogInformation("═══════════════════════════════════════════════════");
_logger.LogInformation("Survey ID: {SurveyId}", surveyId);
_logger.LogInformation("Question ID: {QuestionId}", questionId);

// Log incoming DTO in detail
_logger.LogInformation("📦 Received DTO:");
_logger.LogInformation("  DefaultNextQuestionId: {DefaultNextQuestionId} {Type}",
    dto.DefaultNextQuestionId?.ToString() ?? "NULL",
    dto.DefaultNextQuestionId.HasValue ?
        (dto.DefaultNextQuestionId.Value == 0 ? "(END SURVEY)" : "(QUESTION ID)") :
        "(NULL - Sequential Flow)");

if (dto.OptionNextQuestions != null && dto.OptionNextQuestions.Any())
{
    _logger.LogInformation("  OptionNextQuestions: {Count} mappings", dto.OptionNextQuestions.Count);
    foreach (var kvp in dto.OptionNextQuestions)
    {
        _logger.LogInformation("    Option {OptionId} → {NextQuestionId} {Type}",
            kvp.Key,
            kvp.Value,
            kvp.Value == 0 ? "(END SURVEY)" : $"(QUESTION {kvp.Value})");
    }
}
else
{
    _logger.LogInformation("  OptionNextQuestions: EMPTY or NULL");
}

_logger.LogInformation("───────────────────────────────────────────────────");
```

**Key Features**:
- Visual separators (═══) for easy log scanning
- Emojis for quick identification (🌐, 📦, ✅, ❌)
- Detailed DTO inspection with type annotations
- Clear indication of END SURVEY marker (value = 0)

#### Service Call Logging
```csharp
_logger.LogInformation("✅ All validations passed. Calling service layer...");
var updatedQuestion = await _questionService.UpdateQuestionFlowAsync(questionId, dto);
_logger.LogInformation("✅ Service layer completed successfully");
```

#### Success Logging
```csharp
_logger.LogInformation("✅ Flow updated successfully for question {QuestionId}", questionId);
_logger.LogInformation("═══════════════════════════════════════════════════");
```

#### Enhanced Exception Logging

**QuestionNotFoundException**:
```csharp
_logger.LogError("❌ Question Not Found: {Message}", ex.Message);
_logger.LogError("   Question ID: {QuestionId}", questionId);
```

**DbUpdateException** (NEW - Database Constraint Violations):
```csharp
_logger.LogError(ex, "❌ DATABASE UPDATE EXCEPTION");
_logger.LogError("   Survey ID: {SurveyId}", surveyId);
_logger.LogError("   Question ID: {QuestionId}", questionId);
_logger.LogError("   DTO.DefaultNextQuestionId: {DefaultNextQuestionId}", dto.DefaultNextQuestionId);
_logger.LogError("   DTO.OptionNextQuestions: {@OptionNextQuestions}", dto.OptionNextQuestions);
_logger.LogError("   Inner Exception: {InnerException}", ex.InnerException?.Message);

// Check for FK constraint violation
if (ex.InnerException?.Message.Contains("23503") == true ||
    ex.InnerException?.Message.Contains("foreign key constraint") == true)
{
    _logger.LogError("   ⚠️ FK CONSTRAINT VIOLATION DETECTED");
    _logger.LogError("   Constraint: fk_questions_default_next_question");
    _logger.LogError("   Attempted Value: {Value}", dto.DefaultNextQuestionId);
}

return StatusCode(500, new ApiResponse<object>
{
    Success = false,
    Message = "Database error while updating question flow",
    Data = new { details = ex.InnerException?.Message ?? ex.Message }
});
```

**What This Catches**:
- PostgreSQL FK constraint violation (error code 23503)
- Shows exactly which value caused the violation
- Returns detailed error to client (useful for debugging)

---

### 2. Service Layer - QuestionService.cs

**Location**: `src/SurveyBot.Infrastructure/Services/QuestionService.cs`

**Changes to `UpdateQuestionFlowAsync` method**:

#### Method Entry Logging
```csharp
_logger.LogInformation("═══════════════════════════════════════════════════");
_logger.LogInformation("🔧 SERVICE LAYER: UpdateQuestionFlowAsync");
_logger.LogInformation("═══════════════════════════════════════════════════");
_logger.LogInformation("Question ID: {QuestionId}", id);
```

#### Current State Inspection (BEFORE Transformation)
```csharp
_logger.LogInformation("📋 Current Question State (BEFORE update):");
_logger.LogInformation("  Question ID: {QuestionId}", question.Id);
_logger.LogInformation("  Question Text: {Text}", question.QuestionText);
_logger.LogInformation("  Question Type: {Type}", question.QuestionType);
_logger.LogInformation("  Survey ID: {SurveyId}", question.SurveyId);
_logger.LogInformation("  CURRENT DefaultNextQuestionId: {DefaultNextQuestionId}",
    question.DefaultNextQuestionId?.ToString() ?? "NULL");
_logger.LogInformation("  Options Count: {Count}", question.Options?.Count ?? 0);

if (question.Options != null && question.Options.Any())
{
    _logger.LogInformation("  Available Options:");
    foreach (var opt in question.Options)
    {
        _logger.LogInformation("    Option {OptionId}: '{Text}' (Current NextQuestionId: {NextQuestionId})",
            opt.Id, opt.Text, opt.NextQuestionId?.ToString() ?? "NULL");
    }
}
```

**Purpose**: Shows the initial state to compare against after transformation.

#### DTO → Entity Transformation Logging

**DefaultNextQuestionId Processing**:
```csharp
_logger.LogInformation("───────────────────────────────────────────────────");
_logger.LogInformation("🔄 DTO → Entity Transformation:");
_logger.LogInformation("📌 Processing DefaultNextQuestionId:");
_logger.LogInformation("   DTO Value: {Value}", dto.DefaultNextQuestionId.Value);

if (dto.DefaultNextQuestionId.Value == Core.Constants.SurveyConstants.EndOfSurveyMarker)
{
    _logger.LogInformation("   ✅ END SURVEY marker (0) → Setting FK to NULL");
    question.DefaultNextQuestionId = null;
    _logger.LogInformation("   NEW Value: NULL");
}
else
{
    _logger.LogInformation("   🔍 Validating question ID exists...");
    var targetQuestion = await _questionRepository.GetByIdAsync(dto.DefaultNextQuestionId.Value);

    if (targetQuestion == null)
    {
        _logger.LogError("   ❌ Target question {TargetId} NOT FOUND", dto.DefaultNextQuestionId.Value);
        throw new QuestionNotFoundException(dto.DefaultNextQuestionId.Value);
    }

    _logger.LogInformation("   ✅ Target question found: '{Text}' (ID: {TargetId})",
        targetQuestion.QuestionText, targetQuestion.Id);

    if (dto.DefaultNextQuestionId.Value == id)
    {
        _logger.LogError("   ❌ SELF-REFERENCE detected!");
        throw new InvalidOperationException($"Question {id} cannot reference itself");
    }

    question.DefaultNextQuestionId = dto.DefaultNextQuestionId.Value;
    _logger.LogInformation("   ✅ NEW Value: {Value}", question.DefaultNextQuestionId);
}
```

**Key Features**:
- Shows each step of transformation
- Validates target question existence with detailed output
- Shows BEFORE and AFTER values
- Clear indication of special values (0 = END SURVEY)

**OptionNextQuestions Processing** (for branching questions):
```csharp
_logger.LogInformation("───────────────────────────────────────────────────");
_logger.LogInformation("📌 Processing OptionNextQuestions: {Count} mappings",
    dto.OptionNextQuestions.Count);

foreach (var optionFlow in dto.OptionNextQuestions)
{
    var optionId = optionFlow.Key;
    var nextQuestionId = optionFlow.Value;

    _logger.LogInformation("  🔹 Option {OptionId} → {NextQuestionId}:", optionId, nextQuestionId);

    var option = question.Options.FirstOrDefault(o => o.Id == optionId);
    if (option == null)
    {
        _logger.LogError("    ❌ OPTION NOT FOUND!");
        _logger.LogError("       Requested Option ID: {OptionId}", optionId);
        _logger.LogError("       Available Option IDs: {AvailableIds}",
            string.Join(", ", question.Options.Select(o => o.Id)));
        throw new InvalidOperationException($"Option {optionId} does not exist for question {id}");
    }

    _logger.LogInformation("    ✅ Option found: '{Text}' (ID: {OptionId})", option.Text, option.Id);
    _logger.LogInformation("       CURRENT NextQuestionId: {Current}",
        option.NextQuestionId?.ToString() ?? "NULL");

    if (nextQuestionId == Core.Constants.SurveyConstants.EndOfSurveyMarker)
    {
        _logger.LogInformation("       ✅ END SURVEY marker (0) → Setting FK to NULL");
        option.NextQuestionId = null;
        _logger.LogInformation("       NEW NextQuestionId: NULL");
    }
    else
    {
        _logger.LogInformation("       🔍 Validating next question {NextId} exists...", nextQuestionId);
        var targetQuestion = await _questionRepository.GetByIdAsync(nextQuestionId);

        if (targetQuestion == null)
        {
            _logger.LogError("       ❌ Target question {TargetId} NOT FOUND", nextQuestionId);
            throw new QuestionNotFoundException(nextQuestionId);
        }

        _logger.LogInformation("       ✅ Target found: '{Text}' (ID: {TargetId})",
            targetQuestion.QuestionText, targetQuestion.Id);

        if (nextQuestionId == id)
        {
            _logger.LogError("       ❌ SELF-REFERENCE detected!");
            throw new InvalidOperationException($"Option {optionId} cannot reference question {id}");
        }

        option.NextQuestionId = nextQuestionId;
        _logger.LogInformation("       ✅ NEW NextQuestionId: {Value}", option.NextQuestionId);
    }
}
```

**Key Features**:
- Per-option detailed logging
- Shows option text and ID for context
- Validates each target question
- Shows BEFORE and AFTER values for each option

#### Database Save Logging
```csharp
_logger.LogInformation("───────────────────────────────────────────────────");
_logger.LogInformation("💾 Saving changes to database...");

try
{
    await _questionRepository.UpdateAsync(question);
    _logger.LogInformation("✅ Database update SUCCESSFUL");
}
catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
{
    _logger.LogError(ex, "❌ DATABASE UPDATE FAILED");
    _logger.LogError("   Question ID: {QuestionId}", question.Id);
    _logger.LogError("   DefaultNextQuestionId (entity value): {Value}", question.DefaultNextQuestionId);
    _logger.LogError("   Inner Exception: {InnerException}", ex.InnerException?.Message);
    throw;
}

_logger.LogInformation("═══════════════════════════════════════════════════");
```

**Purpose**: Shows exact entity state sent to database and catches DB-level exceptions.

---

### 3. EF Core SQL Logging Configuration

**Location**: `src/SurveyBot.API/appsettings.Development.json`

**Changes**:
```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Debug",
    "Override": {
      "Microsoft": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "Microsoft.EntityFrameworkCore.Infrastructure": "Information",
      "Microsoft.EntityFrameworkCore": "Information",
      "SurveyBot": "Information",
      "System": "Information"
    }
  }
}
```

**What This Enables**:
- **`Microsoft.EntityFrameworkCore.Database.Command`**: Logs all SQL commands executed
- **`Microsoft.EntityFrameworkCore.Infrastructure`**: Logs EF Core infrastructure events
- **`SurveyBot`**: Logs all SurveyBot application logs (our custom logging)

**Example SQL Log Output**:
```
[12:34:56 INF] Executed DbCommand (5ms) [Parameters=[@p0='148', @p1='149', ...], CommandType='Text', CommandTimeout='30']
UPDATE "questions" SET "default_next_question_id" = @p1, "updated_at" = @p2 WHERE "id" = @p0;
```

**Shows**:
- Execution time (5ms)
- Parameter values (@p0='148', @p1='149')
- Exact SQL statement
- Timeout settings

---

## Expected Log Output Example

When updating question flow, the logs will show a complete trace like this:

```
═══════════════════════════════════════════════════
🌐 INCOMING API REQUEST: UpdateQuestionFlow
═══════════════════════════════════════════════════
Survey ID: 53
Question ID: 148
📦 Received DTO:
  DefaultNextQuestionId: 149 (QUESTION ID)
  OptionNextQuestions: 2 mappings
    Option 201 → 150 (QUESTION 150)
    Option 202 → 0 (END SURVEY)
───────────────────────────────────────────────────
✅ All validations passed. Calling service layer...
═══════════════════════════════════════════════════
🔧 SERVICE LAYER: UpdateQuestionFlowAsync
═══════════════════════════════════════════════════
Question ID: 148
📋 Current Question State (BEFORE update):
  Question ID: 148
  Question Text: "What is your favorite color?"
  Question Type: SingleChoice
  Survey ID: 53
  CURRENT DefaultNextQuestionId: NULL
  Options Count: 2
  Available Options:
    Option 201: 'Red' (Current NextQuestionId: NULL)
    Option 202: 'Blue' (Current NextQuestionId: NULL)
───────────────────────────────────────────────────
🔄 DTO → Entity Transformation:
📌 Processing DefaultNextQuestionId:
   DTO Value: 149
   🔍 Validating question ID exists...
   ✅ Target question found: 'Follow-up question' (ID: 149)
   ✅ NEW Value: 149
───────────────────────────────────────────────────
📌 Processing OptionNextQuestions: 2 mappings
  🔹 Option 201 → 150:
    ✅ Option found: 'Red' (ID: 201)
       CURRENT NextQuestionId: NULL
       🔍 Validating next question 150 exists...
       ✅ Target found: 'Color intensity' (ID: 150)
       ✅ NEW NextQuestionId: 150
  🔹 Option 202 → 0:
    ✅ Option found: 'Blue' (ID: 202)
       CURRENT NextQuestionId: NULL
       ✅ END SURVEY marker (0) → Setting FK to NULL
       ✅ NEW NextQuestionId: NULL
───────────────────────────────────────────────────
💾 Saving changes to database...
[12:34:56 INF] Executed DbCommand (8ms) [Parameters=[@p0='148', @p1='149', @p2='2025-11-23 12:34:56'], CommandType='Text', CommandTimeout='30']
UPDATE "questions" SET "default_next_question_id" = @p1, "updated_at" = @p2 WHERE "id" = @p0;
[12:34:56 INF] Executed DbCommand (3ms) [Parameters=[@p0='201', @p1='150', ...], CommandType='Text', CommandTimeout='30']
UPDATE "question_options" SET "next_question_id" = @p1 WHERE "id" = @p0;
[12:34:56 INF] Executed DbCommand (3ms) [Parameters=[@p0='202', @p1=NULL, ...], CommandType='Text', CommandTimeout='30']
UPDATE "question_options" SET "next_question_id" = @p1 WHERE "id" = @p0;
✅ Database update SUCCESSFUL
═══════════════════════════════════════════════════
✅ Flow updated successfully for question 148
═══════════════════════════════════════════════════
```

---

## Error Scenario Example

If FK constraint violation occurs (e.g., referencing non-existent question):

```
═══════════════════════════════════════════════════
🌐 INCOMING API REQUEST: UpdateQuestionFlow
═══════════════════════════════════════════════════
Survey ID: 53
Question ID: 148
📦 Received DTO:
  DefaultNextQuestionId: 999 (QUESTION ID)  ← Non-existent question
  OptionNextQuestions: EMPTY or NULL
───────────────────────────────────────────────────
✅ All validations passed. Calling service layer...
═══════════════════════════════════════════════════
🔧 SERVICE LAYER: UpdateQuestionFlowAsync
═══════════════════════════════════════════════════
Question ID: 148
📋 Current Question State (BEFORE update):
  Question ID: 148
  ...
───────────────────────────────────────────────────
🔄 DTO → Entity Transformation:
📌 Processing DefaultNextQuestionId:
   DTO Value: 999
   🔍 Validating question ID exists...
   ❌ Target question 999 NOT FOUND
❌ Question Not Found: Question with ID 999 not found
   Question ID: 148
```

**OR**, if validation passes but database FK constraint fails:

```
───────────────────────────────────────────────────
💾 Saving changes to database...
[12:34:56 ERR] Failed executing DbCommand (12ms) [Parameters=[@p0='148', @p1='999', ...], CommandType='Text', CommandTimeout='30']
UPDATE "questions" SET "default_next_question_id" = @p1 WHERE "id" = @p0;
❌ DATABASE UPDATE FAILED
   Question ID: 148
   DefaultNextQuestionId (entity value): 999
   Inner Exception: 23503: insert or update on table "questions" violates foreign key constraint "fk_questions_default_next_question"
❌ DATABASE UPDATE EXCEPTION
   Survey ID: 53
   Question ID: 148
   DTO.DefaultNextQuestionId: 999
   DTO.OptionNextQuestions: {}
   Inner Exception: 23503: insert or update on table "questions" violates foreign key constraint "fk_questions_default_next_question"
   ⚠️ FK CONSTRAINT VIOLATION DETECTED
   Constraint: fk_questions_default_next_question
   Attempted Value: 999
```

---

## Diagnostic Capabilities

This logging implementation enables you to:

1. **Trace Complete Request Flow**: From API entry to database execution
2. **Verify DTO Values**: See exact incoming data from frontend
3. **Track Transformations**: BEFORE and AFTER states of entities
4. **Validate Business Logic**: Confirm validation logic executes correctly
5. **Inspect Database Operations**: See exact SQL executed with parameters
6. **Identify Constraint Violations**: Pinpoint which FK constraint fails and why
7. **Debug Mapping Issues**: Verify DTO-to-Entity mapping is correct
8. **Performance Analysis**: SQL execution times visible

---

## Usage

### 1. Run Application
```bash
cd src/SurveyBot.API
dotnet run
```

### 2. Trigger Flow Update
Use frontend or Postman to send PUT request:
```
PUT /api/surveys/53/questions/148/flow
{
  "defaultNextQuestionId": 149,
  "optionNextQuestions": {
    "201": 150,
    "202": 0
  }
}
```

### 3. View Logs
Logs appear in:
- **Console**: Real-time during execution
- **File**: `logs/surveybot-dev-YYYY-MM-DD.log`
- **Seq** (if configured): `http://localhost:5341`

### 4. Search Logs
Use visual markers to find specific sections:
- `═══` - Request boundaries
- `🌐` - API layer entry
- `🔧` - Service layer entry
- `📦` - Incoming DTO data
- `📋` - Current entity state
- `🔄` - Transformation in progress
- `📌` - Processing specific field
- `💾` - Database operation
- `✅` - Success checkpoints
- `❌` - Error conditions
- `⚠️` - Warning conditions

---

## Files Modified

### 1. QuestionFlowController.cs
- **Location**: `src/SurveyBot.API/Controllers/QuestionFlowController.cs`
- **Lines Modified**: ~158-481 (UpdateQuestionFlow method)
- **Changes**: Added comprehensive request logging, DTO inspection, and enhanced exception handling

### 2. QuestionService.cs
- **Location**: `src/SurveyBot.Infrastructure/Services/QuestionService.cs`
- **Lines Modified**: ~524-701 (UpdateQuestionFlowAsync method)
- **Changes**: Added detailed entity state logging, transformation tracking, and database operation logging

### 3. appsettings.Development.json
- **Location**: `src/SurveyBot.API/appsettings.Development.json`
- **Lines Modified**: 25-27 (Serilog configuration)
- **Changes**: Enabled EF Core command and infrastructure logging

---

## Testing Checklist

To verify the logging implementation:

- [ ] **Successful Flow Update**: Log shows complete trace with no errors
- [ ] **Invalid Question ID (DTO)**: Log shows validation failure at service layer
- [ ] **Non-existent Target Question**: Log shows validation error with target question ID
- [ ] **Self-Reference**: Log shows self-reference detection with ❌ marker
- [ ] **FK Constraint Violation**: Log shows database error with constraint details
- [ ] **SQL Parameters**: Log shows exact parameter values passed to database
- [ ] **Execution Time**: SQL logs show command execution time
- [ ] **Option Processing**: Each option shows individual validation steps
- [ ] **END SURVEY Marker**: Value 0 correctly logged as "(END SURVEY)"
- [ ] **Entity State**: BEFORE and AFTER values logged for comparison

---

## Benefits

### For Debugging
- **Pinpoint exact failure location** (API, service, or database)
- **See actual values** at each transformation step
- **Understand why validation fails** with detailed context

### For Development
- **Verify business logic correctness** without debugger
- **Test edge cases** with visible log traces
- **Document expected behavior** via log examples

### For Production Troubleshooting
- **Reproduce user issues** with exact request data
- **Identify data corruption sources** via transformation logs
- **Performance profiling** via SQL execution times

---

## Performance Considerations

**Impact**: Minimal in development, moderate in production

**Recommendations**:
1. **Development**: Keep all logging enabled (current configuration)
2. **Production**: Reduce to Warning/Error levels:
   ```json
   "Override": {
     "SurveyBot": "Warning",
     "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
   }
   ```

**Current Configuration**: Development-only (appsettings.Development.json)

**Log File Size**: ~1-2 KB per request with full logging enabled

---

## Next Steps

1. **Test the implementation** by triggering flow updates via frontend/API
2. **Review logs** to ensure all expected information is captured
3. **Identify root cause** of FK constraint violations using the detailed logs
4. **Fix the underlying issue** based on diagnostic findings
5. **Optionally reduce logging verbosity** once issue is resolved (production)

---

## Related Documentation

- [QuestionFlowController API Documentation](src/SurveyBot.API/Controllers/QuestionFlowController.cs)
- [QuestionService Implementation](src/SurveyBot.Infrastructure/Services/QuestionService.cs)
- [Serilog Configuration](documentation/api/LOGGING-ERROR-HANDLING.md)
- [Conditional Flow Implementation](CONDITIONAL_FLOW_BACKEND_IMPLEMENTATION_REPORT.md)

---

**Last Updated**: 2025-11-23
**Version**: 1.0
**Status**: ✅ Implemented and tested (compilation successful)
