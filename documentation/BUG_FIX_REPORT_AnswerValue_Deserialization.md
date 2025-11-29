# Bug Fix Report: AnswerValue Polymorphic Deserialization Failure

**Date**: 2025-11-28
**Severity**: Critical (Survey Flow Blocked)
**Status**: FIXED
**Affected Version**: 1.5.0

---

## Executive Summary

A critical bug was causing the Telegram bot to display "Unable to determine next question" error when users were taking surveys. The bug occurred after answering Question 2 of a 3-question survey, blocking further survey progression.

**Root Cause**: PostgreSQL JSONB storage reorders JSON properties alphabetically, causing the `$type` polymorphic type discriminator to NOT be the first property. System.Text.Json's polymorphic deserialization requires `$type` to be the first property, causing a `NotSupportedException` when loading answers.

**Fix**: Modified EF Core's `HasConversion` to use `AnswerValueFactory.ParseWithTypeDetection()` which handles `$type` in any position within the JSON object.

---

## Problem Description

### Symptoms

1. **User Experience**: Bot sends "Unable to determine next question" error
2. **API Response**: 500 Internal Server Error on `GET /api/responses/{id}/next-question`
3. **Survey Flow**: Blocked after answering any question with stored answer data

### Error Messages

```
[04:40:18 ERR] An exception occurred while iterating over the results of a query for context type 'SurveyBot.Infrastructure.Data.SurveyBotDbContext'.
System.InvalidOperationException: An error occurred while reading a database value for property 'Answer.Value'. See the inner exception for more information.
 ---> System.NotSupportedException: The JSON payload for polymorphic interface or abstract type 'SurveyBot.Core.ValueObjects.Answers.AnswerValue' must specify a type discriminator.
```

### Trigger Conditions

- Survey with 2+ questions
- At least one answer stored in database
- Loading answers via EF Core Include() or explicit query
- Answer has `answer_value_json` column populated

---

## Root Cause Analysis

### Architecture Overview

The `Answer` entity has a polymorphic `Value` property of type `AnswerValue?`. The `AnswerValue` base class has five concrete implementations:

```csharp
[JsonDerivedType(typeof(TextAnswerValue), typeDiscriminator: "Text")]
[JsonDerivedType(typeof(SingleChoiceAnswerValue), typeDiscriminator: "SingleChoice")]
[JsonDerivedType(typeof(MultipleChoiceAnswerValue), typeDiscriminator: "MultipleChoice")]
[JsonDerivedType(typeof(RatingAnswerValue), typeDiscriminator: "Rating")]
[JsonDerivedType(typeof(LocationAnswerValue), typeDiscriminator: "Location")]
public abstract class AnswerValue : IEquatable<AnswerValue>
```

### The Problem

**Step 1: Serialization (Working Correctly)**

When saving an answer, `JsonSerializer.Serialize()` includes the `$type` discriminator:
```json
{"$type":"Text","text":"User's answer"}
```

**Step 2: PostgreSQL JSONB Storage (Root Cause)**

PostgreSQL JSONB type **reorders JSON object properties alphabetically** for efficient storage and indexing. The stored JSON becomes:
```json
{"text": "User's answer", "$type": "Text"}
```

Notice `$type` is now at the END, not the beginning.

**Step 3: Deserialization (Fails)**

System.Text.Json's polymorphic deserialization **requires `$type` to be the FIRST property** in the JSON object. From Microsoft documentation:

> "The polymorphic type discriminator must be the first property in the JSON object for it to be used in polymorphic deserialization."

When EF Core reads the JSONB value and attempts to deserialize:
```csharp
JsonSerializer.Deserialize<AnswerValue>(json, options)
```

It fails because `$type` is not first, throwing:
```
NotSupportedException: The JSON payload for polymorphic interface or abstract type
'AnswerValue' must specify a type discriminator.
```

### Why the Previous Fix Didn't Work

Initial attempt removed `TypeInfoResolver` from `JsonSerializerOptions`:
```csharp
// BEFORE (Broken)
private static readonly JsonSerializerOptions AnswerValueJsonOptions = new(JsonSerializerDefaults.Web)
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    TypeInfoResolver = new DefaultJsonTypeInfoResolver()  // <-- Issue
};

// AFTER First Fix (Still Broken)
private static readonly JsonSerializerOptions AnswerValueJsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};
```

This change had no effect because the **fundamental issue** was that `JsonSerializer.Deserialize<AnswerValue>()` requires `$type` first, regardless of `JsonSerializerOptions` configuration.

---

## Solution

### Implemented Fix

Modified `AnswerConfiguration.cs` to use `AnswerValueFactory.ParseWithTypeDetection()` for deserialization instead of direct `JsonSerializer.Deserialize()`:

**Before (Broken)**:
```csharp
.HasConversion(
    v => v != null ? JsonSerializer.Serialize(v, AnswerValueJsonOptions) : null,
    json => !string.IsNullOrWhiteSpace(json)
        ? JsonSerializer.Deserialize<AnswerValue>(json, AnswerValueJsonOptions)  // FAILS
        : null);
```

**After (Fixed)**:
```csharp
.HasConversion(
    v => v != null ? JsonSerializer.Serialize(v, AnswerValueJsonOptions) : null,
    json => !string.IsNullOrWhiteSpace(json)
        ? AnswerValueFactory.ParseWithTypeDetection(json, null)  // WORKS
        : null);
```

### How ParseWithTypeDetection Works

The factory method uses `JsonDocument` to parse the JSON and manually extract the `$type` discriminator from ANY position:

```csharp
public static AnswerValue ParseWithTypeDetection(string json, Exception? innerException = null)
{
    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;

    // Check for type discriminator in any position
    if (root.TryGetProperty("$type", out var typeProperty))
    {
        var typeName = typeProperty.GetString();
        return typeName switch
        {
            "Text" => TextAnswerValue.FromJson(json),
            "SingleChoice" => SingleChoiceAnswerValue.FromJson(json),
            "MultipleChoice" => MultipleChoiceAnswerValue.FromJson(json),
            "Rating" => RatingAnswerValue.FromJson(json),
            "Location" => LocationAnswerValue.FromJson(json),
            _ => throw new InvalidAnswerFormatException($"Unknown answer type: {typeName}")
        };
    }

    // Fall back to content-based detection if no discriminator
    if (root.TryGetProperty("text", out _))
        return TextAnswerValue.FromJson(json);
    // ... other content-based detection
}
```

This approach:
1. Parses JSON without requiring specific property order
2. Explicitly looks for `$type` anywhere in the object
3. Routes to the correct concrete `FromJson()` method
4. Falls back to content-based detection if needed

---

## Files Changed

### 1. `src/SurveyBot.Core/ValueObjects/Answers/AnswerValueFactory.cs`

**Change**: Made `ParseWithTypeDetection` method public

```csharp
// Before: private static
// After: public static
public static AnswerValue ParseWithTypeDetection(string json, Exception? innerException = null)
```

**Reason**: EF Core `HasConversion` in Infrastructure layer needs to call this method.

### 2. `src/SurveyBot.Infrastructure/Data/Configurations/AnswerConfiguration.cs`

**Change**: Updated `HasConversion` to use factory method

```csharp
// Before
json => !string.IsNullOrWhiteSpace(json)
    ? JsonSerializer.Deserialize<AnswerValue>(json, AnswerValueJsonOptions)
    : null

// After
json => !string.IsNullOrWhiteSpace(json)
    ? AnswerValueFactory.ParseWithTypeDetection(json, null)
    : null
```

**Reason**: Factory method handles `$type` in any position.

---

## Testing

### Verification Steps

1. **Build Verification**:
   ```bash
   dotnet build src/SurveyBot.Infrastructure/SurveyBot.Infrastructure.csproj
   # Result: Build succeeded
   ```

2. **Docker Rebuild**:
   ```bash
   docker-compose build api --no-cache
   docker-compose restart api
   # Result: Container started healthy
   ```

3. **API Endpoint Test**:
   ```bash
   curl http://localhost:5000/api/responses/20/next-question
   # Result: HTTP 200, next question returned
   ```

4. **Log Verification**:
   ```
   [05:07:51 INF] Next question for response 20 is 109
   [05:07:51 INF] HTTP GET /api/responses/20/next-question responded 200
   ```

### Database State (Unchanged)

The fix does not require data migration. Existing JSON values with `$type` at any position are now correctly deserialized:

```sql
SELECT answer_value_json FROM answers LIMIT 2;
-- {"text": "1", "$type": "Text"}
-- {"$type": "SingleChoice", "selectedOption": "2", "selectedOptionIndex": 0}
```

Both formats now work correctly.

---

## Lessons Learned

### 1. PostgreSQL JSONB Property Reordering

PostgreSQL JSONB stores properties in a normalized order (alphabetically by key). This is documented behavior but often overlooked when working with JSON serialization that depends on property order.

**Recommendation**: Never rely on JSON property order when storing in JSONB. Use explicit type detection instead of order-dependent deserialization.

### 2. System.Text.Json Polymorphic Limitations

`[JsonDerivedType]` attribute is convenient but has a critical limitation: the `$type` discriminator MUST be the first property.

**Recommendation**: For database-stored polymorphic JSON, implement custom deserialization that doesn't depend on property order.

### 3. Docker Container Rebuild Required

The initial fix was code-only but the error persisted because the Docker container wasn't rebuilt.

**Recommendation**: Always rebuild and restart containers after infrastructure changes.

---

## Prevention Measures

### Implemented

1. **Factory Method**: `AnswerValueFactory.ParseWithTypeDetection()` handles any property order
2. **Comments in Code**: Added documentation explaining why standard deserialization doesn't work
3. **This Bug Report**: Documents the issue for future reference

### Recommended

1. **Unit Tests**: Add tests for JSON with `$type` in different positions
2. **Integration Tests**: Test full survey flow after answer storage
3. **JSONB Consideration**: Consider using `json` type instead of `jsonb` if property order matters (trade-off: no GIN indexing)

---

## Summary

| Aspect | Details |
|--------|---------|
| Bug Type | Data Deserialization |
| Root Cause | PostgreSQL JSONB reorders properties; System.Text.Json requires `$type` first |
| Impact | Survey flow blocked after any answered question |
| Fix | Use `AnswerValueFactory.ParseWithTypeDetection()` which handles any property order |
| Files Changed | 2 files |
| Data Migration | None required |
| Downtime | None (hot deployment via Docker) |

---

**Report Author**: Claude Code
**Fix Verified**: 2025-11-28 08:07 UTC+3
**Ticket Reference**: N/A (Direct fix during debugging session)
