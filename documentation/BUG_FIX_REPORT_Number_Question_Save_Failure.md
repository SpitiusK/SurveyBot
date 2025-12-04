# Bug Fix Report: Number Question Answer Save Failure

**Date**: 2025-11-29
**Severity**: HIGH - Blocks all Number question functionality
**Status**: IDENTIFIED AND FIX READY

---

## Executive Summary

**Problem**: Users receive "❌ Failed to save your answer. Please try again." when answering Number questions in the Telegram bot.

**Root Cause**: `SurveyResponseHandler.cs` does not extract the number value from answer JSON and populate the `answerText` field in the DTO sent to the API. The API's `AnswerValueFactory.CreateNumberAnswer()` method expects the number value in the `textAnswer` parameter, but receives `null`.

**Impact**: Number question type (v1.5.1 feature) is completely non-functional in Telegram bot.

**Fix**: Update `SurveyResponseHandler.SubmitAnswerAsync()` to extract number value from `{"number": 1.1}` JSON and populate `answerText` field.

---

## Investigation Timeline

### 1. Initial Investigation (Previous Session)

**Symptom**: User enters "1" for Number question, receives error "❌ Failed to save your answer."

**First Hypothesis**: Bot not sending `answerJson` for Number questions.

**Action Taken**:
- Fixed `SurveyResponseHandler.cs` to detect Number/Date answer types
- Added logic to include `answerJson` in DTO for Number/Date questions
- Restarted API container

**Result**: Fix did not resolve the issue. Error persisted.

### 2. Docker Log Analysis (Current Session)

**Tool Used**: @docker-log-analyzer

**Findings**:
- API returning HTTP 400 Bad Request
- Error: `Invalid answer format for question 0 of type Number: Number value is required`
- Exception thrown at `AnswerValueFactory.cs:262`
- Code line: `if (string.IsNullOrWhiteSpace(textAnswer))` → **FAILS**
- Conclusion: `textAnswer` parameter arriving as `null` or empty

**Key Insight**: Previous fix addressed `answerJson` field, but API actually needs `answerText` field populated with the number value.

### 3. Code Analysis

**File Examined**: `src/SurveyBot.Bot/Handlers/SurveyResponseHandler.cs`

**Problematic Code** (lines 464-521, specifically line 486):

```csharp
private async Task<bool> SubmitAnswerAsync(
    int responseId,
    int questionId,
    string answerJson,
    CancellationToken cancellationToken)
{
    var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

    // Determine answer type
    bool isLocationAnswer = answer.TryGetProperty("latitude", out _);
    bool isNumberAnswer = answer.TryGetProperty("number", out _);
    bool isDateAnswer = answer.TryGetProperty("date", out _);

    var submitDto = new
    {
        answer = new
        {
            questionId = questionId,
            // ❌ PROBLEM: Only extracts from "text" property!
            answerText = answer.TryGetProperty("text", out var text) ? text.GetString() : null,
            selectedOptions = /* ... */,
            ratingValue = /* ... */,
            // ✅ This field IS populated correctly for Number questions
            answerJson = (isLocationAnswer || isNumberAnswer || isDateAnswer) ? answerJson : null
        }
    };
    // ...
}
```

**Issue**: Line 486 only extracts `answerText` if the JSON has a "text" property:
```csharp
answerText = answer.TryGetProperty("text", out var text) ? text.GetString() : null
```

For Number questions:
- Input JSON: `{"number": 1.1}`
- Has "text" property? **NO**
- Result: `answerText = null` ❌

**API Expectation**: `AnswerValueFactory.CreateNumberAnswer(string? textAnswer, Question? question)`

The API method expects the number value as a **string** in the `textAnswer` parameter:
```csharp
// File: src/SurveyBot.Core/ValueObjects/Answers/AnswerValueFactory.cs:262
private static NumberAnswerValue CreateNumberAnswer(string? textAnswer, Question? question)
{
    if (string.IsNullOrWhiteSpace(textAnswer))  // ← This check fails!
        throw new InvalidAnswerFormatException(0, QuestionType.Number, "Number value is required");

    // Parse textAnswer as number
    var numberValue = NumberAnswerValue.Parse(textAnswer);
    return NumberAnswerValue.CreateForQuestion(numberValue.Value, question);
}
```

---

## Root Cause Analysis

### Data Flow

**Correct Flow for Number Questions**:

```
1. User types "1.1" in Telegram bot
   ↓
2. NumberQuestionHandler.ProcessAnswerAsync() validates and creates JSON
   → Returns: {"number": 1.1}
   ↓
3. SurveyResponseHandler.SubmitAnswerAsync() receives answerJson = {"number": 1.1}
   ↓
4. ❌ CURRENT: answerText = null (no "text" property in JSON)
   ✅ EXPECTED: answerText = "1.1" (extracted from "number" property)
   ↓
5. HTTP POST /api/responses/{responseId}/answers with DTO:
   {
     "answer": {
       "questionId": 127,
       "answerText": "1.1",  ← Should be here!
       "answerJson": "{\"number\": 1.1}"
     }
   }
   ↓
6. API calls AnswerValueFactory.CreateNumberAnswer(answerText, question)
   ↓
7. ✅ SUCCESS: Parses "1.1" and creates NumberAnswerValue
```

**Actual Flow (Current Bug)**:

```
Steps 1-3: Same as above
   ↓
4. ❌ answerText = null (because no "text" property)
   ↓
5. HTTP POST with DTO:
   {
     "answer": {
       "questionId": 127,
       "answerText": null,  ← PROBLEM!
       "answerJson": "{\"number\": 1.1}"
     }
   }
   ↓
6. API calls AnswerValueFactory.CreateNumberAnswer(null, question)
   ↓
7. ❌ FAIL: Line 262 throws InvalidAnswerFormatException("Number value is required")
   ↓
8. HTTP 400 Bad Request → Bot shows "Failed to save your answer"
```

### Why Previous Fix Didn't Work

**Previous fix** (already applied):
```csharp
// Detect Number/Date answers
bool isNumberAnswer = answer.TryGetProperty("number", out _);
bool isDateAnswer = answer.TryGetProperty("date", out _);

// Include answerJson for Number/Date
answerJson = (isLocationAnswer || isNumberAnswer || isDateAnswer) ? answerJson : null
```

**Why it didn't help**:
- The fix correctly populates `answerJson` field in the DTO
- However, the API doesn't use `answerJson` for Number questions!
- The API's `CreateNumberAnswer()` method only reads from `textAnswer` parameter
- The bot never populated `answerText`, so it remained `null`

### Same Issue for Date Questions

Date questions will have the **exact same problem**:

**Date Question Flow**:
- DateQuestionHandler returns: `{"date": "29.11.2025"}`
- DTO sent to API: `answerText = null` (no "text" property)
- API calls: `AnswerValueFactory.CreateDateAnswer(null, question)`
- Result: HTTP 400 "Date value is required"

---

## The Fix

### Code Change Required

**File**: `src/SurveyBot.Bot/Handlers/SurveyResponseHandler.cs`
**Method**: `SubmitAnswerAsync` (lines 464-521)
**Specific Line**: 486

**Current Code**:
```csharp
answerText = answer.TryGetProperty("text", out var text) ? text.GetString() : null,
```

**Fixed Code**:
```csharp
// Extract answerText based on answer type
answerText = answer.TryGetProperty("text", out var text)
    ? text.GetString()  // Text questions
    : answer.TryGetProperty("number", out var number)
        ? number.GetDecimal().ToString(CultureInfo.InvariantCulture)  // Number questions → "1.1"
        : answer.TryGetProperty("date", out var date)
            ? date.GetString()  // Date questions → "29.11.2025"
            : null,  // Other types (choice, rating, location) don't need answerText
```

### Explanation

**For Text questions**:
- JSON: `{"text": "User's answer"}`
- Extract: `text.GetString()` → `"User's answer"`
- DTO: `answerText = "User's answer"`

**For Number questions**:
- JSON: `{"number": 1.1}`
- Extract: `number.GetDecimal().ToString(CultureInfo.InvariantCulture)` → `"1.1"`
- DTO: `answerText = "1.1"`
- API receives: `textAnswer = "1.1"` → Parses successfully

**For Date questions**:
- JSON: `{"date": "29.11.2025"}`
- Extract: `date.GetString()` → `"29.11.2025"`
- DTO: `answerText = "29.11.2025"`
- API receives: `textAnswer = "29.11.2025"` → Parses successfully

**For other types** (SingleChoice, MultipleChoice, Rating, Location):
- Don't need `answerText` field
- Use `selectedOptions`, `ratingValue`, or `answerJson` instead
- `answerText = null` is correct for these types

### Complete Fixed Method

```csharp
private async Task<bool> SubmitAnswerAsync(
    int responseId,
    int questionId,
    string answerJson,
    CancellationToken cancellationToken)
{
    try
    {
        // Parse answer JSON to extract the appropriate field based on answer structure
        var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

        // Determine answer type based on JSON structure
        bool isLocationAnswer = answer.TryGetProperty("latitude", out _);
        bool isNumberAnswer = answer.TryGetProperty("number", out _);
        bool isDateAnswer = answer.TryGetProperty("date", out _);

        // Create submit DTO with the correct structure (wrapped in "answer" property)
        var submitDto = new
        {
            answer = new
            {
                questionId = questionId,

                // ✅ FIXED: Extract answerText based on question type
                answerText = answer.TryGetProperty("text", out var text)
                    ? text.GetString()  // Text questions
                    : answer.TryGetProperty("number", out var number)
                        ? number.GetDecimal().ToString(CultureInfo.InvariantCulture)  // Number questions
                        : answer.TryGetProperty("date", out var date)
                            ? date.GetString()  // Date questions
                            : null,  // Other types don't need answerText

                selectedOptions = answer.TryGetProperty("selectedOptions", out var options)
                    ? options.EnumerateArray().Select(e => e.GetString()).ToList()
                    : (answer.TryGetProperty("selectedOption", out var option)
                        ? new List<string?> { option.GetString() }
                        : null),

                ratingValue = answer.TryGetProperty("rating", out var rating) && rating.ValueKind != JsonValueKind.Null
                    ? rating.GetInt32()
                    : (int?)null,

                // For location, number, and date answers, include the entire JSON in answerJson field
                answerJson = (isLocationAnswer || isNumberAnswer || isDateAnswer) ? answerJson : null
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/api/responses/{responseId}/answers",
            submitDto,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Failed to submit answer: {StatusCode}",
                response.StatusCode);
            return false;
        }

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error submitting answer for question {QuestionId}", questionId);
        return false;
    }
}
```

---

## Testing Plan

### Unit Test (Recommended)

```csharp
[Fact]
public void SubmitAnswerAsync_NumberQuestion_PopulatesAnswerTextWithNumberValue()
{
    // Arrange
    var answerJson = "{\"number\": 1.1}";
    var responseId = 28;
    var questionId = 127;

    // Act
    var submitDto = CreateSubmitDto(questionId, answerJson);

    // Assert
    Assert.Equal("1.1", submitDto.answer.answerText);
    Assert.Equal("{\"number\": 1.1}", submitDto.answer.answerJson);
}

[Fact]
public void SubmitAnswerAsync_DateQuestion_PopulatesAnswerTextWithDateString()
{
    // Arrange
    var answerJson = "{\"date\": \"29.11.2025\"}";

    // Act
    var submitDto = CreateSubmitDto(123, answerJson);

    // Assert
    Assert.Equal("29.11.2025", submitDto.answer.answerText);
    Assert.Equal("{\"date\": \"29.11.2025\"}", submitDto.answer.answerJson);
}
```

### Manual Test (End-to-End)

1. **Setup**: Ensure bot and API containers are running with the fix deployed
2. **Create Number Question Survey**:
   - Create survey via admin panel or API
   - Add Number question: "Enter a number (1-10)"
   - Set range: min=1, max=10, decimalPlaces=1
   - Activate survey
3. **Take Survey in Bot**:
   - Open Telegram bot
   - Send `/surveys`
   - Select the survey
   - Bot displays: "Question 1 of 1\n\n**Enter a number (1-10)**\n\n(Required)\n(Range: 1 to 10, Up to 1 decimal place(s))\nPlease enter a number:"
4. **Enter Answer**:
   - Type: `5.5`
   - Expected: "✅ Answer saved!" + "Thank you for completing the survey!"
   - NOT: "❌ Failed to save your answer. Please try again."
5. **Verify in Database**:
   - Check `answers` table
   - Should have record with:
     - `question_id = 127`
     - `answer_text = "5.5"`
     - `answer_value_json = {"number": 5.5, ...}`

### Edge Cases to Test

- **Integer input**: `5` → Should work (parsed as 5.0 or 5 depending on decimal places config)
- **Decimal with comma**: `3,14` → NumberQuestionHandler normalizes to `3.14`
- **Out of range**: `-1` or `100` → Validation error (handled by NumberQuestionHandler)
- **Invalid input**: `abc` → Validation error (handled by NumberQuestionHandler)
- **Optional question skip**: `/skip` → Should create `{"number": null}`

---

## Deployment Steps

### 1. Apply Code Changes

**File to edit**: `src/SurveyBot.Bot/Handlers/SurveyResponseHandler.cs`

**Line 486** - Replace:
```csharp
answerText = answer.TryGetProperty("text", out var text) ? text.GetString() : null,
```

With:
```csharp
answerText = answer.TryGetProperty("text", out var text)
    ? text.GetString()
    : answer.TryGetProperty("number", out var number)
        ? number.GetDecimal().ToString(CultureInfo.InvariantCulture)
        : answer.TryGetProperty("date", out var date)
            ? date.GetString()
            : null,
```

**Add using directive** at top of file (if not already present):
```csharp
using System.Globalization;
```

### 2. Rebuild and Restart Bot Container

**Option A: Docker Compose** (rebuilds all containers):
```bash
cd C:\Users\User\Desktop\SurveyBot
docker compose down
docker compose up --build -d
```

**Option B: Specific Container** (faster, only bot):
```bash
# If bot runs in API container (check docker-compose.yml)
docker compose up --build -d api

# OR if bot has separate container
docker compose up --build -d bot
```

### 3. Verify Deployment

**Check logs for successful startup**:
```bash
docker logs surveybot-api --tail 50
```

Expected log lines:
```
[INFO] Telegram Bot initialized successfully
[INFO] SurveyBot API started successfully
```

### 4. Test the Fix

**Quick Smoke Test**:
1. Open Telegram bot
2. Create a test survey with one Number question (or use existing)
3. Take the survey
4. Enter a number (e.g., "5.5")
5. **Expected**: "✅ Answer saved!" + "Thank you for completing the survey!"
6. **NOT Expected**: "❌ Failed to save your answer. Please try again."

**Verification**:
```bash
# Check API logs for successful POST
docker logs surveybot-api | grep "responses/28/answers"
# Should see HTTP 200, not HTTP 400
```

---

## Risk Assessment

**Risk Level**: LOW
- Single file change in bot layer
- No database migrations required
- No API changes required
- Backward compatible (doesn't affect other question types)

**Affected Components**:
- ✅ Bot layer: `SurveyResponseHandler.cs` (fixed)
- ❌ API layer: No changes
- ❌ Database: No changes
- ❌ Frontend: No changes

**Rollback Plan**:
If the fix causes issues, revert the change:
```bash
git checkout HEAD -- src/SurveyBot.Bot/Handlers/SurveyResponseHandler.cs
docker compose up --build -d
```

---

## Lessons Learned

### 1. Always Check Both Sides of Integration

**Issue**: Fixed bot to send `answerJson`, but didn't verify what API actually expects.

**Lesson**: When debugging integration issues, verify the contract on **both sides**:
- What does the sender send? (Bot)
- What does the receiver expect? (API)

### 2. Log Analysis is Critical

**Issue**: Without Docker logs, we wouldn't know the exact error message or where it occurred.

**Lesson**:
- Use `@docker-log-analyzer` for runtime diagnostics
- Log both request and response in integration points
- Include stack traces in error logs

### 3. Question Type Abstraction Has Edge Cases

**Issue**: Generic DTO structure assumes all question types use same fields, but Number/Date need special handling.

**Lesson**:
- Document which DTO fields are used by which question types
- Consider type-specific DTOs for complex answer structures
- Add integration tests for each question type

### 4. Test End-to-End, Not Just Layers

**Issue**: Previous fix appeared correct for bot layer in isolation, but failed in integration.

**Lesson**:
- Always test complete user flow (bot → API → database)
- Don't assume fix is complete until E2E test passes
- Add automated integration tests for new question types

---

## Documentation Updates Required

After deploying this fix, update:

1. **Bot Layer CLAUDE.md** (`src/SurveyBot.Bot/CLAUDE.md`):
   - Update "Survey Taking Flow" to clarify that Number/Date questions populate both `answerText` and `answerJson`
   - Add note about DTO field mapping for each question type

2. **Number Question Implementation Plan** (`documentation/features/NUMBER_DATE_QUESTIONS_IMPLEMENTATION_PLAN.md`):
   - Mark bot integration as COMPLETE
   - Add this bug fix to "Known Issues Resolved" section

3. **Bot Troubleshooting Guide** (`documentation/bot/BOT_TROUBLESHOOTING.md`):
   - Add entry: "Number/Date answers fail to save → Check DTO field mapping in SurveyResponseHandler"

---

## Related Issues

### Date Question Type

**Same Bug**: Date questions will have the identical issue (not extracting date value to `answerText`).

**Fix**: Already included in the code change above:
```csharp
: answer.TryGetProperty("date", out var date)
    ? date.GetString()
    : null,
```

**Test**: After deploying fix, test Date questions as well.

### Location Question Type

**Status**: ✅ Already works correctly

**Why**: Location questions don't use `answerText` field. They use `answerJson` exclusively, which is already populated correctly.

---

## Summary

**Root Cause**: Bot's `SurveyResponseHandler.SubmitAnswerAsync()` method only extracts `answerText` from JSON with "text" property. Number and Date questions use "number" and "date" properties, resulting in `answerText = null`. API's `AnswerValueFactory` expects number/date values in `textAnswer` parameter.

**Fix**: Update line 486 to extract value from "number" or "date" properties and convert to string for `answerText` field.

**Impact**: Fixes both Number and Date question types in bot.

**Deployment**: Single file change, rebuild bot container, no migration required.

**Testing**: Manual test with Number question, verify "✅ Answer saved!" message instead of error.

---

**Report Created**: 2025-11-29
**Created By**: Task Execution Agent (via @docker-log-analyzer root cause analysis)
**Fix Status**: READY TO APPLY
**Priority**: HIGH - Blocks Number/Date question functionality
