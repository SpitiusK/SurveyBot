# Bug Report: "Unable to Determine Next Question" Navigation Failure

**Date**: 2025-11-28
**Reporter**: Task Execution Agent
**Severity**: 🔴 **CRITICAL** - Blocks survey completion for all text questions
**Status**: Root cause identified, fix ready for implementation

---

## Executive Summary

Users cannot progress past text questions when taking surveys via Telegram bot. The bot displays "❌ Unable to determine next question" after submitting text answers, preventing survey completion.

**Root Cause**: EF Core cannot deserialize polymorphic `AnswerValue` objects from the database due to missing `TypeInfoResolver` configuration in custom `JsonSerializerOptions`.

**Impact**:
- All surveys with text questions are broken
- Users cannot complete surveys
- Survey response data is saved but navigation fails
- Affects 100% of users taking surveys with text questions

**Fix Complexity**: ⭐ Low - Simple configuration change, no data migration needed

---

## Symptom Description

### User Experience

**Telegram Bot Flow**:
1. User starts survey with `/survey` command ✅ Works
2. User answers Question 1 (single choice) ✅ Works
3. Bot displays Question 2 (text question) ✅ Works
4. User types text answer "1" ✅ Answer saves to database
5. Bot displays: "❌ Unable to determine next question" ❌ **FAILS**
6. Survey flow terminates, user cannot continue

### Example Conversation

```
SecondTestSurveyBot, [28.11.2025 5:02]
Question 1 of 3
<p>11111</p>
(Required)
Select one option:
✓ Your answer: 2

SecondTestSurveyBot, [28.11.2025 5:02]
Question 2 of 3
<p>222222</p>
(Required)
Please type your answer below:
Type /back to go to previous question

Alexandr, [28.11.2025 5:02]
1

SecondTestSurveyBot, [28.11.2025 5:02]
❌ Unable to determine next question
```

---

## Technical Analysis

### Timeline of Failure

| Timestamp | Event | Status |
|-----------|-------|--------|
| 02:02:11 | Survey started (Response ID 17) | ✅ Success |
| 02:02:14 | Question 1 answered (single choice, option "2") | ✅ Success |
| 02:02:16 | Question 2 displayed (text question) | ✅ Success |
| 02:02:21 | Question 2 answered (text "1") saved to DB | ✅ Success |
| 02:02:21 | Bot calls `GET /api/responses/17/next-question` | ❌ **500 Error** |
| 02:02:21 | Bot displays error message | ❌ Failure |

### Error Chain

```
1. User submits text answer "1"
   ↓
2. Bot calls: POST /api/responses/17/answers
   ↓
3. API saves Answer with AnswerValue: {"$type":"Text","text":"1"}
   ✅ Database insert succeeds
   ↓
4. API determines next question (ID 100) using conditional flow logic
   ✅ Next question determination succeeds
   ↓
5. Bot calls: GET /api/responses/17/next-question
   ↓
6. API calls: ResponseRepository.GetByIdWithAnswersAsync(17)
   ↓
7. EF Core executes: SELECT ... FROM responses ... LEFT JOIN answers ...
   ↓
8. EF Core attempts to materialize Answer.Value from answer_value_json column
   ↓
9. EF Core calls: JsonSerializer.Deserialize<AnswerValue>(json, AnswerValueJsonOptions)
   ↓
10. ❌ EXCEPTION: System.NotSupportedException
    "The JSON payload for polymorphic interface or abstract type
    'SurveyBot.Core.ValueObjects.Answers.AnswerValue' must specify a type discriminator."
    ↓
11. Exception propagates to API controller
    ↓
12. API returns: 500 Internal Server Error
    ↓
13. Bot displays: "❌ Unable to determine next question"
```

### Actual Error Message

```
System.NotSupportedException: The JSON payload for polymorphic interface or abstract type
'SurveyBot.Core.ValueObjects.Answers.AnswerValue' must specify a type discriminator.

Stack Trace:
  at System.Text.Json.ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(Type type)
  at System.Text.Json.Serialization.JsonConverter`1.ReadCore(Utf8JsonReader& reader, JsonSerializerOptions options, ReadStack& state)
  at Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter`2.ConvertFromProvider(Object value)
  at Microsoft.EntityFrameworkCore.ChangeTracking.Internal.InternalEntityEntry.ReadPropertyValue(IProperty property, Object dbValue)
  at Microsoft.EntityFrameworkCore.Query.Internal.MaterializationContext.MaterializeEntity(ValueBuffer valueBuffer, InternalEntityEntry entry)

Location:
  File: src/SurveyBot.Infrastructure/Data/Configurations/AnswerConfiguration.cs
  Line: 28 (during deserialization)
  Method: ResponseRepository.GetByIdWithAnswersAsync()
```

---

## Root Cause Analysis

### Problem: Missing Polymorphic Type Support in JsonSerializerOptions

**File**: `src/SurveyBot.Infrastructure/Data/Configurations/AnswerConfiguration.cs`
**Lines**: 18-22

**Current Configuration** ❌:
```csharp
private static readonly JsonSerializerOptions AnswerValueJsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
    // PROBLEM: No TypeInfoResolver or polymorphic support
};
```

**Why This Fails**:

1. **Serialization works** (Answer → Database):
   - Runtime type is known (e.g., `TextAnswerValue`)
   - `[JsonDerivedType]` attributes add `$type` discriminator automatically
   - JSON is correctly saved: `{"$type":"Text","text":"1"}`

2. **Deserialization fails** (Database → Answer):
   - Only abstract type `AnswerValue` is known at compile time
   - Custom `JsonSerializerOptions` don't automatically process `[JsonDerivedType]` metadata
   - Without `TypeInfoResolver`, the `$type` discriminator is ignored
   - Deserializer doesn't know which concrete type to instantiate

### Database Evidence

**Query**:
```sql
SELECT id, question_id, answer_value_json::text
FROM answers
WHERE response_id = 17;
```

**Result**:
```json
{
  "id": 50,
  "question_id": 99,
  "answer_value_json": "{\"$type\":\"Text\",\"text\":\"1\"}"
}
```

✅ Database JSON is **correct** - contains type discriminator
❌ EF Core configuration **cannot deserialize** it

### AnswerValue Class Configuration

**File**: `src/SurveyBot.Core/ValueObjects/Answers/AnswerValue.cs`
**Lines**: 8-13

**Current Attributes** ✅:
```csharp
[JsonDerivedType(typeof(TextAnswerValue), typeDiscriminator: "Text")]
[JsonDerivedType(typeof(SingleChoiceAnswerValue), typeDiscriminator: "SingleChoice")]
[JsonDerivedType(typeof(MultipleChoiceAnswerValue), typeDiscriminator: "MultipleChoice")]
[JsonDerivedType(typeof(RatingAnswerValue), typeDiscriminator: "Rating")]
[JsonDerivedType(typeof(LocationAnswerValue), typeDiscriminator: "Location")]
public abstract class AnswerValue
```

✅ Attributes are **correctly configured**
❌ Custom `JsonSerializerOptions` **don't honor them** without `TypeInfoResolver`

---

## Why Question 1 (Single Choice) Worked

**Question**: If the configuration is broken, why did Question 1 succeed?

**Answer**: Question 1 was answered using a **single choice** question. The navigation logic worked because:

1. Answer was saved successfully (serialization always works)
2. Next question determination happened **before** retrieving the full response with answers
3. Bot called `GET /api/responses/17/next-question` which internally:
   - Determines next question based on **newly saved answer** (still in memory)
   - Returns next question **without** deserializing previous answers from database
4. No deserialization error occurred yet

**Why Question 2 (Text) Failed**:

When moving from Question 2 to Question 3:
1. Answer was saved successfully
2. Next question determination requires retrieving **all previous answers** from database
3. Bot calls `GET /api/responses/17/next-question`
4. API calls `ResponseRepository.GetByIdWithAnswersAsync(17)`
5. EF Core tries to deserialize **previous answers** including Question 1 and Question 2
6. ❌ Deserialization fails on Question 2's `TextAnswerValue`

**Key Insight**: The bug manifests **after** the second question because that's when EF Core first needs to deserialize existing answers from the database.

---

## The Fix

### Recommended Solution (PRIORITY 1) ⭐

**File**: `src/SurveyBot.Infrastructure/Data/Configurations/AnswerConfiguration.cs`
**Lines**: 18-23

**Change**:
```csharp
// BEFORE ❌
private static readonly JsonSerializerOptions AnswerValueJsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};

// AFTER ✅
private static readonly JsonSerializerOptions AnswerValueJsonOptions = new(JsonSerializerDefaults.Web)
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
};
```

**Key Changes**:
1. ✅ Add `JsonSerializerDefaults.Web` to constructor
2. ✅ Add explicit `TypeInfoResolver` property

**Why This Works**:
- `JsonSerializerDefaults.Web` initializes default web-friendly settings
- `DefaultJsonTypeInfoResolver` enables recognition of `[JsonDerivedType]` attributes
- Polymorphic type information is now available during deserialization
- `$type` discriminator in JSON is properly interpreted
- Correct concrete type is instantiated based on discriminator value

---

## Alternative Solutions (For Reference)

### Alternative 1: Use Default JsonSerializerOptions

**File**: `src/SurveyBot.Infrastructure/Data/Configurations/AnswerConfiguration.cs`

```csharp
builder.Property(a => a.Value)
    .HasColumnName("answer_value_json")
    .HasColumnType("jsonb")
    .HasConversion(
        v => JsonSerializer.Serialize(v),  // Use defaults
        v => JsonSerializer.Deserialize<AnswerValue>(v)!  // Use defaults
    );
```

**Pros**: Simplest change, automatic polymorphic support
**Cons**: Loses control over JSON formatting (camelCase, etc.)

### Alternative 2: Configure at DbContext Level

**File**: `src/SurveyBot.Infrastructure/Data/SurveyBotDbContext.cs`

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseNpgsql(...)
        .UseJsonOptions(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
        });
}
```

**Pros**: Global configuration for all JSON columns
**Cons**: Affects other entities, requires EF Core 8+ specific API

---

## Impact Assessment

### Systems Affected

| Component | Impact | Details |
|-----------|--------|---------|
| **Telegram Bot** | 🔴 Critical | Users cannot complete surveys with text questions |
| **API** | 🔴 Critical | 500 errors on `GET /api/responses/{id}/next-question` |
| **Database** | ✅ No Impact | Data is correctly saved, no corruption |
| **Frontend** | 🟡 Indirect | Admin panel may fail to display response details |

### Question Types Affected

| Question Type | Affected | Reason |
|---------------|----------|--------|
| Text | ❌ Yes | Uses `TextAnswerValue` - triggers deserialization error |
| Single Choice | ❌ Yes* | Uses `SingleChoiceAnswerValue` - fails when retrieved |
| Multiple Choice | ❌ Yes* | Uses `MultipleChoiceAnswerValue` - fails when retrieved |
| Rating | ❌ Yes* | Uses `RatingAnswerValue` - fails when retrieved |
| Location | ❌ Yes* | Uses `LocationAnswerValue` - fails when retrieved |

*All types fail during deserialization, but error may not appear until **second question** when previous answers must be loaded.

### Data Integrity

✅ **No data corruption** - All answers are correctly saved to database
✅ **JSON format correct** - Type discriminators present in database
✅ **No migration needed** - Fix is configuration-only
⚠️ **Incomplete responses** - Users abandoned surveys due to navigation failure

---

## Testing Plan

### Unit Tests (REQUIRED)

**File**: `tests/SurveyBot.Tests/Unit/ValueObjects/AnswerValueSerializationTests.cs` (NEW)

```csharp
[Fact]
public void AnswerValue_SerializeAndDeserialize_WithCustomOptions_ShouldSucceed()
{
    // Arrange
    var options = AnswerConfiguration.AnswerValueJsonOptions;
    var originalValue = new TextAnswerValue("test answer");

    // Act
    var json = JsonSerializer.Serialize<AnswerValue>(originalValue, options);
    var deserializedValue = JsonSerializer.Deserialize<AnswerValue>(json, options);

    // Assert
    deserializedValue.Should().BeOfType<TextAnswerValue>();
    ((TextAnswerValue)deserializedValue).Text.Should().Be("test answer");
}

[Theory]
[InlineData("Text", typeof(TextAnswerValue))]
[InlineData("SingleChoice", typeof(SingleChoiceAnswerValue))]
[InlineData("MultipleChoice", typeof(MultipleChoiceAnswerValue))]
[InlineData("Rating", typeof(RatingAnswerValue))]
[InlineData("Location", typeof(LocationAnswerValue))]
public void AnswerValue_Deserialize_WithTypeDiscriminator_ShouldCreateCorrectType(
    string typeDiscriminator, Type expectedType)
{
    // Arrange
    var options = AnswerConfiguration.AnswerValueJsonOptions;
    var json = $"{{\"$type\":\"{typeDiscriminator}\",\"text\":\"test\"}}";

    // Act
    var value = JsonSerializer.Deserialize<AnswerValue>(json, options);

    // Assert
    value.Should().BeOfType(expectedType);
}
```

### Integration Tests (REQUIRED)

**File**: `tests/SurveyBot.Tests/Integration/ResponseRepositoryDeserializationTests.cs` (NEW)

```csharp
[Fact]
public async Task GetByIdWithAnswersAsync_WithTextAnswer_ShouldDeserializeSuccessfully()
{
    // Arrange
    var response = EntityBuilder.CreateResponse(userId: 1, surveyId: 1);
    var question = EntityBuilder.CreateQuestion(surveyId: 1, type: QuestionType.Text);
    var answer = Answer.CreateWithValue(
        responseId: response.Id,
        questionId: question.Id,
        value: new TextAnswerValue("test answer")
    );

    await _context.Responses.AddAsync(response);
    await _context.Questions.AddAsync(question);
    await _context.Answers.AddAsync(answer);
    await _context.SaveChangesAsync();

    // Act
    var retrievedResponse = await _repository.GetByIdWithAnswersAsync(response.Id);

    // Assert
    retrievedResponse.Should().NotBeNull();
    retrievedResponse.Answers.Should().HaveCount(1);
    retrievedResponse.Answers[0].Value.Should().BeOfType<TextAnswerValue>();
}
```

### Manual Testing Checklist

- [ ] Start survey via Telegram bot
- [ ] Answer Question 1 (any type)
- [ ] Answer Question 2 (text question) with text input
- [ ] Verify bot displays Question 3 (not error message)
- [ ] Complete entire survey
- [ ] Verify all answers saved correctly
- [ ] Check admin panel displays response details
- [ ] Test all question types: Text, Single Choice, Multiple Choice, Rating, Location
- [ ] Test survey with 5+ questions to ensure multiple deserializations work
- [ ] Verify backward navigation (`/back` command) works

---

## Implementation Checklist

### Step 1: Apply Code Fix ✅

- [ ] Update `src/SurveyBot.Infrastructure/Data/Configurations/AnswerConfiguration.cs`
- [ ] Add `using System.Text.Json.Serialization.Metadata;`
- [ ] Modify `AnswerValueJsonOptions` initialization (lines 18-23)
- [ ] Add `JsonSerializerDefaults.Web` parameter
- [ ] Add `TypeInfoResolver = new DefaultJsonTypeInfoResolver()`
- [ ] Build solution: `dotnet build`
- [ ] Verify no compilation errors

### Step 2: Add Unit Tests ✅

- [ ] Create `tests/SurveyBot.Tests/Unit/ValueObjects/AnswerValueSerializationTests.cs`
- [ ] Add serialization round-trip test
- [ ] Add type discriminator tests for all 5 types
- [ ] Run tests: `dotnet test --filter "AnswerValueSerializationTests"`
- [ ] Verify all tests pass

### Step 3: Add Integration Tests ✅

- [ ] Create `tests/SurveyBot.Tests/Integration/ResponseRepositoryDeserializationTests.cs`
- [ ] Add test for each AnswerValue type deserialization
- [ ] Add test for response with multiple answers
- [ ] Run tests: `dotnet test --filter "ResponseRepositoryDeserializationTests"`
- [ ] Verify all tests pass

### Step 4: Restart API ✅

- [ ] Stop API container: `docker-compose stop api`
- [ ] Rebuild API: `dotnet build src/SurveyBot.API`
- [ ] Start API container: `docker-compose start api`
- [ ] Verify API starts without errors
- [ ] Check health endpoint: `curl http://localhost:5000/health/db`

### Step 5: Manual Testing ✅

- [ ] Start survey via Telegram bot
- [ ] Complete survey with text questions
- [ ] Verify no "Unable to determine next question" error
- [ ] Test backward navigation
- [ ] Test all question types
- [ ] Verify admin panel displays responses correctly

### Step 6: Verify Logs ✅

- [ ] Check API logs: `docker-compose logs api`
- [ ] Verify no deserialization errors
- [ ] Verify successful question navigation
- [ ] Check for any new warnings or errors

### Step 7: Update Documentation ✅

- [ ] Update `src/SurveyBot.Infrastructure/CLAUDE.md`
  - Document `AnswerValueJsonOptions` configuration
  - Explain polymorphic type support requirement
- [ ] Update `documentation/fixes/` with this bug report
- [ ] Add to release notes for next version

---

## Migration Considerations

### Database Migration

✅ **NO DATABASE MIGRATION REQUIRED**

- Database schema is unchanged
- Existing JSON data is already correct (contains `$type` discriminators)
- Only code configuration changes needed
- Backward compatible with existing data

### Deployment

**Zero-downtime deployment possible**:

1. Deploy code fix to staging
2. Run automated tests
3. Manual verification in staging
4. Deploy to production
5. Monitor API error logs
6. Verify survey completion rates increase

**Rollback plan**: Revert code change if issues arise (no database changes to revert)

---

## Prevention Measures

### Code Review Checklist

- [ ] When using custom `JsonSerializerOptions` with polymorphic types:
  - [ ] Always include `JsonSerializerDefaults.Web` or `JsonSerializerDefaults.General`
  - [ ] Always set `TypeInfoResolver = new DefaultJsonTypeInfoResolver()`
  - [ ] Add unit tests for serialization/deserialization round-trip
- [ ] When using EF Core `.ToJson()` or `.HasConversion()`:
  - [ ] Test with actual database reads, not just writes
  - [ ] Add integration tests that retrieve entities after saving
- [ ] When using `[JsonDerivedType]` attributes:
  - [ ] Verify custom JsonSerializerOptions honor attributes
  - [ ] Test all derived types individually

### Monitoring

**Add Application Insights metrics**:
- Track deserialization errors separately
- Alert on increase in 500 errors for `/next-question` endpoint
- Monitor survey completion rates

**Add specific logging**:
```csharp
catch (NotSupportedException ex) when (ex.Message.Contains("type discriminator"))
{
    _logger.LogError(ex, "JSON deserialization failed for polymorphic type. " +
        "Check JsonSerializerOptions configuration for TypeInfoResolver.");
    throw;
}
```

---

## Related Issues

### Previously Reported

- **v1.5.0 Implementation**: AnswerValue polymorphic hierarchy introduced
- **Migration 20251127104737**: Added `answer_value_json` column with JSONB type
- **No reported issues** with serialization (saving to database)
- **First report** of deserialization issues (reading from database)

### Potentially Related

- Check if `QuestionOption.Next` (NextQuestionDeterminant) has similar configuration
- Verify other owned types using `.ToJson()` don't have same issue
- Review all custom `JsonSerializerOptions` usage in Infrastructure layer

---

## Lessons Learned

### Architectural

1. **Custom JsonSerializerOptions require explicit configuration** for polymorphic types
2. **[JsonDerivedType] attributes are not automatically honored** without TypeInfoResolver
3. **Serialization and deserialization have different requirements** - serialization works with runtime type knowledge, deserialization needs compile-time metadata
4. **EF Core JSON columns require careful configuration** - default options may not match application needs

### Testing

1. **Test database reads, not just writes** - Many tests only verify data saves, not retrieval
2. **Integration tests are critical for EF Core configurations** - Unit tests can't catch DbContext issues
3. **Always test polymorphic types round-trip** - Serialization success doesn't guarantee deserialization works

### Development Process

1. **Docker logs analysis is essential** for diagnosing runtime errors quickly
2. **Codebase analysis tools complement logs** by providing code context
3. **Type discriminator errors are often configuration issues** not data issues

---

## References

### Documentation

- [System.Text.Json Polymorphic Serialization](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism)
- [EF Core JSON Columns](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions?tabs=data-annotations#json-value-objects)
- [JsonSerializerOptions.TypeInfoResolver](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializeroptions.typeinforesolver)

### Related Files

- Core: `src/SurveyBot.Core/ValueObjects/Answers/AnswerValue.cs`
- Infrastructure: `src/SurveyBot.Infrastructure/Data/Configurations/AnswerConfiguration.cs`
- Migration: `src/SurveyBot.Infrastructure/Migrations/20251127104737_AddAnswerValueJsonColumn.cs`

### Analysis Reports

- Docker Log Analysis: `.claude/out/docker-log-analysis-2025-11-28-02-02-json-discriminator-error.md`
- Codebase Analysis: `.claude/out/codebase-analysis-2025-11-28-answervalue-json-deserialization.md`

---

## Conclusion

This bug demonstrates the critical importance of:
1. Proper `JsonSerializerOptions` configuration for polymorphic types
2. Comprehensive integration testing that includes database reads
3. Runtime log analysis for quick diagnosis
4. Clear understanding of EF Core JSON column serialization behavior

The fix is straightforward (2-line code change), low-risk (no database changes), and immediately deployable. After fix deployment, survey completion rates should return to normal and no "Unable to determine next question" errors should occur.

**Estimated time to fix**: 30 minutes (code change + tests)
**Estimated time to deploy**: 10 minutes (restart API container)
**Risk level**: ⭐ Low (configuration-only change)
**Data loss risk**: None (no database changes, data already correct)

---

**Report Generated**: 2025-11-28 by Task Execution Agent
**Report Location**: `C:\Users\User\Desktop\SurveyBot\BUG_REPORT_Next_Question_Navigation_Failure.md`
