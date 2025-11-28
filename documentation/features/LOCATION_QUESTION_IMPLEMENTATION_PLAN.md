# Implementation Plan: Geolocation Question Type

**Version**: 1.5.0 | **Feature**: Location Question Type | **Status**: Ready for Implementation
**Created**: 2025-11-26 | **Complexity**: Medium | **Risk**: Low
**Estimated Total Effort**: 28-42 hours | **Calendar Time**: 3-5 days (1 developer)

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Requirement Summary](#requirement-summary)
3. [Architecture Analysis](#architecture-analysis)
4. [Implementation Plan by Phase](#implementation-plan-by-phase)
5. [Dependency Graph](#dependency-graph)
6. [Testing Strategy](#testing-strategy)
7. [Documentation Updates](#documentation-updates)
8. [Risk Assessment](#risk-assessment)
9. [Effort Summary](#effort-summary)
10. [Execution Timeline](#execution-timeline)

---

## Executive Summary

### Feature Overview

Implement a new **Location** question type that enables survey creators to request geographic coordinates from respondents via Telegram's built-in location sharing functionality.

### Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Storage | Existing `Answer.AnswerJson` JSONB | No migration needed, proven pattern |
| Input Method | `ReplyKeyboardMarkup` with `RequestLocation` | Telegram API requirement |
| Frontend Display | Text coordinates only | User requirement (no map) |
| Conditional Flow | NOT supported | Like Text questions, continuous data |
| Validation | Multi-layer (DTO, Service, Domain) | Comprehensive coverage |

### Feasibility Confirmation

- **Telegram.Bot Version**: 22.7.4 (confirmed in `SurveyBot.Bot.csproj`)
- **RequestLocation Support**: Fully supported via `KeyboardButton.RequestLocation`
- **Database Impact**: None (uses existing JSONB column)
- **Breaking Changes**: None (additive enum value)

---

## Requirement Summary

### Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-001 | Add Location to QuestionType enum (value = 4) | Must |
| FR-002 | Display "Share Location" button via ReplyKeyboardMarkup | Must |
| FR-003 | Accept and validate location data from Telegram | Must |
| FR-004 | Store coordinates in Answer.AnswerJson as JSON | Must |
| FR-005 | Support /skip command for optional questions | Must |
| FR-006 | Display coordinates as text in frontend | Must |
| FR-007 | Provide Google Maps link for location answers | Should |
| FR-008 | Show accuracy and timestamp if available | Should |

### Non-Functional Requirements

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-001 | Response time for location save | < 2 seconds |
| NFR-002 | Unit test coverage | >= 80% |
| NFR-003 | Mobile platform support | iOS, Android |
| NFR-004 | Desktop platform support | Manual map selection |

### Out of Scope

- Map visualization in frontend
- Address geocoding/reverse geocoding
- Distance calculations
- Conditional branching based on location
- Location-based survey filtering

---

## Architecture Analysis

### Layer Impact Assessment

```
┌─────────────────────────────────────────┐
│        Frontend (React Admin)           │  Add Location display component
│        API (Controllers)                │  Add LocationAnswerDto mapping
│        Bot (Handlers)                   │  NEW LocationQuestionHandler
└───────────────┬─────────────────────────┘
                │ depends on
                ▼
┌─────────────────────────────────────────┐
│       Infrastructure (Services)         │  Add coordinate validation
└───────────────┬─────────────────────────┘
                │ depends on
                ▼
┌─────────────────────────────────────────┐
│       Core (Enums, DTOs)                │  Add Location = 4, LocationAnswerDto
└─────────────────────────────────────────┘
       ↑ ZERO DEPENDENCIES (Clean Architecture)
```

### Data Storage Format

**Location**: `Answer.AnswerJson` (PostgreSQL JSONB)

```json
{
  "latitude": 40.712776,
  "longitude": -74.005974,
  "accuracy": 65.0,
  "timestamp": "2025-11-26T10:30:00Z"
}
```

### Validation Rules

| Field | Type | Range | Required |
|-------|------|-------|----------|
| latitude | double | -90.0 to +90.0 | Yes |
| longitude | double | -180.0 to +180.0 | Yes |
| accuracy | double? | >= 0 | No |
| timestamp | DateTime? | Valid UTC | No |

---

## Implementation Plan by Phase

### Phase 1: Core Layer (3-5 hours)

#### CORE-001: Add Location to QuestionType Enum

**File**: `src/SurveyBot.Core/Enums/QuestionType.cs`
**Estimated**: 0.5 hours
**Dependencies**: None

```csharp
public enum QuestionType
{
    Text = 0,
    SingleChoice = 1,
    MultipleChoice = 2,
    Rating = 3,
    Location = 4  // NEW
}
```

**Acceptance Criteria**:
- [ ] Location enum value added with value 4
- [ ] XML documentation added
- [ ] Project compiles without errors

---

#### CORE-002: Create LocationAnswerDto

**File**: `src/SurveyBot.Core/DTOs/LocationAnswerDto.cs`
**Estimated**: 1-1.5 hours
**Dependencies**: CORE-001

```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs
{
    /// <summary>
    /// Data transfer object for location-based answers.
    /// </summary>
    public class LocationAnswerDto
    {
        [Required]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
        public double Latitude { get; set; }

        [Required]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
        public double Longitude { get; set; }

        public double? Accuracy { get; set; }

        public DateTime? Timestamp { get; set; }

        public string FormattedCoordinates => $"{Latitude:F6}, {Longitude:F6}";

        public bool IsValid() =>
            Latitude >= -90.0 && Latitude <= 90.0 &&
            Longitude >= -180.0 && Longitude <= 180.0;
    }
}
```

**Acceptance Criteria**:
- [ ] DTO includes Latitude, Longitude, Accuracy, Timestamp
- [ ] Data annotations for validation
- [ ] FormattedCoordinates property for display
- [ ] IsValid() method for validation

---

#### CORE-003: Update AnswerDto for Location Support

**File**: `src/SurveyBot.Core/DTOs/AnswerDto.cs`
**Estimated**: 1-2 hours
**Dependencies**: CORE-002

**Changes**:
- Add `LocationData` property (nullable `LocationAnswerDto`)
- Update `GetDisplayValue()` method for Location type

**Acceptance Criteria**:
- [ ] LocationData property added
- [ ] GetDisplayValue() handles Location type
- [ ] Backward compatible with existing types

---

#### CORE-004: Add InvalidLocationException

**File**: `src/SurveyBot.Core/Exceptions/InvalidLocationException.cs`
**Estimated**: 0.5-1 hour
**Dependencies**: None

```csharp
namespace SurveyBot.Core.Exceptions
{
    public class InvalidLocationException : Exception
    {
        public double? Latitude { get; }
        public double? Longitude { get; }

        public InvalidLocationException(string message) : base(message) { }

        public InvalidLocationException(string message, double latitude, double longitude)
            : base(message)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
```

**Acceptance Criteria**:
- [ ] Custom exception created
- [ ] Includes Latitude/Longitude properties
- [ ] Follows existing exception patterns

---

### Phase 2: Bot Layer (10-15 hours)

#### BOT-001: Create LocationQuestionHandler

**File**: `src/SurveyBot.Bot/Handlers/Questions/LocationQuestionHandler.cs`
**Estimated**: 4-6 hours
**Dependencies**: CORE-001, CORE-002, CORE-004

> **⚠️ CRITICAL (from CRITICAL-006)**: Must use `ReplyKeyboardMarkup`, NOT `InlineKeyboardMarkup`!
> `KeyboardButton.WithRequestLocation()` ONLY works with ReplyKeyboardMarkup.

**Key Implementation Points**:

1. **Display Question with Location Button** (MUST use ReplyKeyboardMarkup):
```csharp
private ReplyKeyboardMarkup CreateLocationKeyboard(bool isRequired)
{
    var buttons = new List<KeyboardButton[]>
    {
        // CRITICAL: Use KeyboardButton.WithRequestLocation - only works with ReplyKeyboardMarkup
        new[] { KeyboardButton.WithRequestLocation("📍 Share Location") }
    };

    if (!isRequired)
    {
        buttons.Add(new[] { new KeyboardButton("/skip") });
    }

    return new ReplyKeyboardMarkup(buttons)
    {
        ResizeKeyboard = true,
        OneTimeKeyboard = true
    };
}
```

2. **Process Location Message and Serialize to JSON** (for CRITICAL-003):
```csharp
if (message.Location != null)
{
    var latitude = message.Location.Latitude;
    var longitude = message.Location.Longitude;
    var accuracy = message.Location.HorizontalAccuracy;

    // CRITICAL: Serialize to JSON for SaveAnswerAsync (uses AnswerJson parameter)
    var locationJson = JsonSerializer.Serialize(new LocationAnswerDto
    {
        Latitude = latitude,
        Longitude = longitude,
        Accuracy = accuracy,
        Timestamp = DateTime.UtcNow
    });

    // Save using answerJson parameter (not separate lat/lon params)
    await _responseService.SaveAnswerAsync(responseId, questionId, answerJson: locationJson);
}
```

3. **Handle /skip for Optional Questions**:
```csharp
if (message.Text?.ToLower() == "/skip" && !question.IsRequired)
{
    // Skip logic
}
```

4. **Remove Keyboard After Submission** (CRITICAL-006):
```csharp
// After saving location, remove the custom keyboard
await botClient.SendMessage(
    chatId: chatId,
    text: "📍 Location received! Thank you.",
    replyMarkup: new ReplyKeyboardRemove()  // CRITICAL: Remove keyboard after use
);
```

**Acceptance Criteria**:
- [ ] Implements IQuestionHandler interface
- [ ] CanHandle returns true for QuestionType.Location
- [ ] **Uses ReplyKeyboardMarkup (NOT InlineKeyboardMarkup)**
- [ ] Displays "Share Location" button via `KeyboardButton.WithRequestLocation()`
- [ ] Validates coordinates (-90 to 90 lat, -180 to 180 lon)
- [ ] **Serializes location to JSON for AnswerJson parameter**
- [ ] Saves location to Answer.AnswerJson
- [ ] Supports /skip for optional questions
- [ ] Handles text input with error message
- [ ] **Removes keyboard with ReplyKeyboardRemove after submission**
- [ ] **Logging: ILogger<LocationQuestionHandler> injected**
- [ ] **Logging: Logs button display, location received, validation, save, skip events**
- [ ] **Logging: Uses GetCoordinateRange() for privacy-preserving coordinate logging**

---

#### BOT-002: Update UpdateHandler for Location Messages

**File**: `src/SurveyBot.Bot/Services/UpdateHandler.cs`
**Estimated**: 2-3 hours
**Dependencies**: BOT-001

> **⚠️ CRITICAL (from CRITICAL-002)**: Current code at line 107-111 returns `false` for ANY message
> without text, which means Location messages are SILENTLY DROPPED! Must fix this routing.

**Current Problematic Code** (line 107-111):
```csharp
// CURRENT - BROKEN for Location messages
private async Task<bool> HandleMessageAsync(Message message, CancellationToken cancellationToken)
{
    if (message.Text == null)
    {
        _logger.LogDebug("Message has no text content, ignoring");
        return false;  // ❌ Location messages get ignored here!
    }
    // ... rest of text handling
}
```

**Required Fix** - Restructure to handle Location BEFORE null text check:
```csharp
private async Task<bool> HandleMessageAsync(Message message, CancellationToken cancellationToken)
{
    // CRITICAL: Check Location FIRST, before text null check
    if (message.Location != null)
    {
        _logger.LogDebug("Processing location message from {UserId}", message.From?.Id);
        return await HandleLocationMessageAsync(message, cancellationToken);
    }

    if (message.Text != null)
    {
        return await HandleTextMessageAsync(message, cancellationToken);
    }

    // Only now ignore truly unhandled message types
    _logger.LogDebug("Message has no text or location content, ignoring");
    return false;
}
```

**New Method to Add**:
```csharp
private async Task<bool> HandleLocationMessageAsync(Message message, CancellationToken cancellationToken)
{
    var userId = message.From?.Id;
    if (userId == null) return false;

    // Get conversation state
    var state = await _stateService.GetStateAsync(userId.Value);
    if (state?.CurrentSurveyId == null || state?.CurrentQuestionId == null)
    {
        await _botClient.SendMessage(
            message.Chat.Id,
            "⚠️ No active survey. Use /surveys to start one.",
            cancellationToken: cancellationToken);
        return false;
    }

    // Delegate to LocationQuestionHandler
    var handler = _questionHandlers.FirstOrDefault(h => h.CanHandle(QuestionType.Location));
    if (handler != null)
    {
        return await handler.HandleAsync(message, state, cancellationToken);
    }

    return false;
}
```

**Acceptance Criteria**:
- [ ] **Location check happens BEFORE text null check** (critical fix)
- [ ] HandleLocationMessageAsync method added
- [ ] Routes to LocationQuestionHandler via IQuestionHandler
- [ ] Handles missing conversation state gracefully
- [ ] Error handling with user-friendly messages
- [ ] **Logging: Logs location message detection and routing**
- [ ] **Logging: Logs warnings for missing conversation state**

---

#### BOT-003: Register LocationQuestionHandler in DI

**File**: `src/SurveyBot.Bot/Extensions/BotServiceExtensions.cs`
**Estimated**: 0.5 hours
**Dependencies**: BOT-001

```csharp
services.AddScoped<IQuestionHandler, LocationQuestionHandler>();
```

**Acceptance Criteria**:
- [ ] Handler registered in DI container
- [ ] Application starts without errors

---

#### BOT-004: Update Help Command

**File**: `src/SurveyBot.Bot/Handlers/HelpCommandHandler.cs`
**Estimated**: 1 hour
**Dependencies**: BOT-001

**Changes**:
- Add Location to question types list
- Add usage instructions for location sharing

**Acceptance Criteria**:
- [ ] Location mentioned in help text
- [ ] Mobile/desktop guidance included

---

#### BOT-005: Update Bot CLAUDE.md

**File**: `src/SurveyBot.Bot/CLAUDE.md`
**Estimated**: 2-3 hours
**Dependencies**: BOT-001 through BOT-004

**Changes**:
- Document LocationQuestionHandler
- Document location message routing
- Add location answer storage format
- Update version to 1.5.0

---

### Phase 3: Infrastructure Layer (4-6 hours)

#### INFRA-001: Add Location Validation to ResponseService

**File**: `src/SurveyBot.Infrastructure/Services/ResponseService.cs`
**Estimated**: 2-3 hours
**Dependencies**: CORE-002, CORE-004

> **⚠️ CRITICAL (from CRITICAL-004)**: `ValidateAnswerFormatAsync` method (lines 357-378) does NOT
> have a case for `QuestionType.Location` - validation will be skipped!

> **⚠️ CRITICAL (from CRITICAL-005)**: `CreateAnswerJson` helper (lines 638-658) returns `null`
> for Location type in the default case - location JSON data will be LOST!

**Fix 1: Add Location case to ValidateAnswerFormatAsync** (around line 357):
```csharp
private async Task ValidateAnswerFormatAsync(Question question, string? answerText,
    List<string>? selectedOptions, int? ratingValue, string? answerJson)
{
    switch (question.QuestionType)
    {
        case QuestionType.Text:
            // existing validation...
            break;
        case QuestionType.SingleChoice:
            // existing validation...
            break;
        case QuestionType.MultipleChoice:
            // existing validation...
            break;
        case QuestionType.Rating:
            // existing validation...
            break;

        // CRITICAL: Add this case!
        case QuestionType.Location:
            if (string.IsNullOrWhiteSpace(answerJson))
                throw new ValidationException("Location answer requires coordinate data.");
            ValidateLocationData(answerJson);
            break;
    }
}
```

**Fix 2: Add ValidateLocationData helper method**:
```csharp
private void ValidateLocationData(string answerJson)
{
    try
    {
        var locationData = JsonSerializer.Deserialize<LocationAnswerDto>(answerJson);

        if (locationData == null)
            throw new InvalidLocationException("Location data is null.");

        if (!locationData.IsValid())
            throw new InvalidLocationException(
                $"Invalid coordinates: lat={locationData.Latitude}, lon={locationData.Longitude}",
                locationData.Latitude, locationData.Longitude);
    }
    catch (JsonException ex)
    {
        throw new ValidationException("Invalid JSON format for location answer.", ex);
    }
}
```

**Fix 3: Update CreateAnswerJson helper** (around line 638):
```csharp
private string? CreateAnswerJson(Question question, string? answerText,
    List<string>? selectedOptions, int? ratingValue, string? answerJson)
{
    switch (question.QuestionType)
    {
        case QuestionType.SingleChoice:
        case QuestionType.MultipleChoice:
            // existing JSON creation...

        case QuestionType.Rating:
            // existing JSON creation...

        // CRITICAL: Add this case - pass through already-formatted JSON
        case QuestionType.Location:
            return answerJson;  // Already serialized by handler

        default:
            return null;
    }
}
```

**Fix 4: Update SaveAnswerAsync method signature** (if needed):
- Ensure `answerJson` parameter exists or add it
- Current signature may need `string? answerJson = null` parameter

**Acceptance Criteria**:
- [ ] **`case QuestionType.Location` added to ValidateAnswerFormatAsync**
- [ ] ValidateLocationData helper method added
- [ ] Validates coordinate bounds (-90 to 90, -180 to 180)
- [ ] Throws InvalidLocationException for invalid coordinates
- [ ] **`case QuestionType.Location` added to CreateAnswerJson** (returns answerJson pass-through)
- [ ] SaveAnswerAsync properly handles answerJson parameter for Location
- [ ] Called in SaveAnswerAsync for Location questions
- [ ] **Logging: ILogger<ResponseService> already injected (verify)**
- [ ] **Logging: Logs validation start, success, failure with coordinate ranges**
- [ ] **Logging: Logs JSON deserialization errors**
- [ ] **Logging: Logs save operations for Location answers**
- [ ] **Logging: Uses GetCoordinateRange() helper method**

---

#### INFRA-002: Add Location Validation to QuestionService

**File**: `src/SurveyBot.Infrastructure/Services/QuestionService.cs`
**Estimated**: 1-2 hours
**Dependencies**: CORE-001

**Validation Rules**:
- Location questions cannot have conditional flow (DefaultNextQuestionId must be null)
- Location questions cannot have options

**Acceptance Criteria**:
- [ ] Validates no options for Location questions
- [ ] Validates no conditional flow
- [ ] Clear error messages

---

#### INFRA-003: Update Infrastructure CLAUDE.md

**File**: `src/SurveyBot.Infrastructure/CLAUDE.md`
**Estimated**: 1 hour
**Dependencies**: INFRA-001, INFRA-002

---

### Phase 4: API Layer (3-5 hours)

#### API-001: Update AutoMapper Profile

**File**: `src/SurveyBot.API/Mapping/AnswerMappingProfile.cs`
**Estimated**: 2-3 hours
**Dependencies**: CORE-002, CORE-003

> **⚠️ CRITICAL (from CRITICAL-007)**: Current AnswerMappingProfile (lines 16-20) does NOT include
> LocationData property mapping. API responses will NOT include location data without this fix!

**Current Code** (missing LocationData):
```csharp
CreateMap<Answer, AnswerDto>()
    .ForMember(dest => dest.SelectedOptions,
        opt => opt.MapFrom<AnswerSelectedOptionsResolver>())
    .ForMember(dest => dest.RatingValue,
        opt => opt.MapFrom<AnswerRatingValueResolver>());
    // ❌ MISSING: .ForMember(dest => dest.LocationData, ...)
```

**Required Fix - Add LocationData mapping with custom resolver**:
```csharp
CreateMap<Answer, AnswerDto>()
    .ForMember(dest => dest.SelectedOptions,
        opt => opt.MapFrom<AnswerSelectedOptionsResolver>())
    .ForMember(dest => dest.RatingValue,
        opt => opt.MapFrom<AnswerRatingValueResolver>())
    .ForMember(dest => dest.LocationData,
        opt => opt.MapFrom<AnswerLocationDataResolver>());  // ✅ ADD THIS
```

**New Resolver Class to Create** (in same file or separate):
```csharp
/// <summary>
/// Resolves LocationData from Answer.AnswerJson for Location question types.
/// </summary>
public class AnswerLocationDataResolver : IValueResolver<Answer, AnswerDto, LocationAnswerDto?>
{
    public LocationAnswerDto? Resolve(Answer source, AnswerDto destination,
        LocationAnswerDto? destMember, ResolutionContext context)
    {
        // Only parse for Location question type
        if (source.Question?.QuestionType != QuestionType.Location)
            return null;

        if (string.IsNullOrWhiteSpace(source.AnswerJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<LocationAnswerDto>(source.AnswerJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            // Log warning here if logger available
            return null;
        }
    }
}
```

**Also Required - Update AnswerDto** (if not done in CORE-003):
```csharp
public class AnswerDto
{
    // ... existing properties ...

    /// <summary>
    /// Location data for Location question types. Null for other types.
    /// </summary>
    public LocationAnswerDto? LocationData { get; set; }
}
```

**Acceptance Criteria**:
- [ ] **AnswerLocationDataResolver class created**
- [ ] **LocationData ForMember mapping added to CreateMap**
- [ ] Resolver only parses for QuestionType.Location
- [ ] Graceful handling of invalid/missing JSON (returns null)
- [ ] AnswerDto has LocationData property
- [ ] API response includes location data for Location answers
- [ ] **Logging: ILogger injected into AnswerLocationDataResolver (optional but recommended)**
- [ ] **Logging: Logs parse attempts, success, and failures**
- [ ] **Logging: Logs warnings for invalid JSON**

---

#### API-002: Add Validation to Controller

**File**: `src/SurveyBot.API/Controllers/ResponsesController.cs`
**Estimated**: 1-2 hours
**Dependencies**: CORE-002, API-001

**Acceptance Criteria**:
- [ ] Validates location answer data
- [ ] Returns 400 on validation failure
- [ ] Clear error messages

---

#### API-003: Update API CLAUDE.md

**File**: `src/SurveyBot.API/CLAUDE.md`
**Estimated**: 1 hour
**Dependencies**: API-001, API-002

---

### Phase 5: Frontend (4-6 hours)

#### FRONTEND-001: Add Location to Question Type Selector

**File**: `frontend/src/components/Questions/QuestionTypeSelector.tsx` (or equivalent)
**Estimated**: 1-2 hours
**Dependencies**: CORE-001

```typescript
export enum QuestionType {
  Text = 0,
  SingleChoice = 1,
  MultipleChoice = 2,
  Rating = 3,
  Location = 4,
}

export const questionTypeOptions = [
  // ... existing types
  { value: QuestionType.Location, label: 'Location', icon: '📍' },
];
```

**Acceptance Criteria**:
- [ ] Location option in dropdown
- [ ] Icon displayed

---

#### FRONTEND-002: Create LocationAnswerDisplay Component

**File**: `frontend/src/components/Answers/LocationAnswerDisplay.tsx`
**Estimated**: 2-3 hours
**Dependencies**: CORE-002

**Features**:
- Display latitude/longitude as text
- Show accuracy if available
- Show timestamp if available
- Google Maps link

```typescript
const formattedCoords = `${latitude.toFixed(6)}, ${longitude.toFixed(6)}`;
const googleMapsUrl = `https://www.google.com/maps?q=${latitude},${longitude}`;
```

**Acceptance Criteria**:
- [ ] Displays coordinates as text
- [ ] Shows accuracy/timestamp if available
- [ ] Provides Google Maps link
- [ ] Material-UI styling

---

#### FRONTEND-003: Integrate in Response Viewer

**File**: `frontend/src/components/Responses/ResponseDetail.tsx`
**Estimated**: 1 hour
**Dependencies**: FRONTEND-002

**Acceptance Criteria**:
- [ ] Location answers use LocationAnswerDisplay
- [ ] Fallback for missing data

---

#### FRONTEND-004: Update Frontend CLAUDE.md

**File**: `frontend/CLAUDE.md`
**Estimated**: 1 hour
**Dependencies**: FRONTEND-001 through FRONTEND-003

---

### Phase 6: Testing (8-12 hours)

#### TEST-001: Unit Tests for LocationQuestionHandler

**File**: `tests/SurveyBot.Tests/Handlers/LocationQuestionHandlerTests.cs`
**Estimated**: 3-4 hours
**Dependencies**: BOT-001

**Test Cases**:
```csharp
[Fact] CanHandle_LocationQuestion_ReturnsTrue()
[Fact] HandleAsync_ValidLocation_SavesAnswer()
[Fact] HandleAsync_InvalidLatitude_ThrowsException()
[Fact] HandleAsync_InvalidLongitude_ThrowsException()
[Fact] HandleAsync_SkipOptionalQuestion_Succeeds()
[Fact] HandleAsync_SkipRequiredQuestion_ShowsError()
[Fact] HandleAsync_TextInsteadOfLocation_ShowsError()
[Fact] HandleAsync_LocationOutOfBounds_ThrowsException()
```

---

#### TEST-002: Unit Tests for Location Validation

**File**: `tests/SurveyBot.Tests/Services/ResponseServiceLocationTests.cs`
**Estimated**: 2-3 hours
**Dependencies**: INFRA-001

**Test Cases**:
```csharp
[Fact] SaveAnswerAsync_ValidLocation_Succeeds()
[Fact] SaveAnswerAsync_InvalidLatitude_ThrowsInvalidLocationException()
[Fact] SaveAnswerAsync_InvalidLongitude_ThrowsInvalidLocationException()
[Fact] SaveAnswerAsync_MissingAnswerJson_ThrowsValidationException()
[Fact] SaveAnswerAsync_InvalidJson_ThrowsValidationException()

[Theory]
[InlineData(-91.0, 0.0)]   // Latitude too low
[InlineData(91.0, 0.0)]    // Latitude too high
[InlineData(0.0, -181.0)]  // Longitude too low
[InlineData(0.0, 181.0)]   // Longitude too high
SaveAnswerAsync_CoordinatesOutOfBounds_ThrowsException(double lat, double lon)
```

---

#### TEST-003: Integration Tests for Location API

**File**: `tests/SurveyBot.Tests/Controllers/ResponsesControllerLocationTests.cs`
**Estimated**: 2-3 hours
**Dependencies**: API-001, API-002

**Test Cases**:
```csharp
[Fact] SaveAnswer_ValidLocationAnswer_Returns201()
[Fact] SaveAnswer_InvalidLocationCoordinates_Returns400()
[Fact] SaveAnswer_MissingLocationData_Returns400()
[Fact] GetResponseWithLocationAnswers_ReturnsLocationData()
```

---

#### TEST-004: Manual Testing Checklist

**Estimated**: 1-2 hours (document creation + execution)

**Survey Creation**:
- [ ] Create survey with Location question via bot
- [ ] Create survey with Location question via API
- [ ] Create optional Location question
- [ ] Create required Location question

**Bot Testing - Mobile**:
- [ ] "Share Location" button displays
- [ ] Tap button opens permission dialog
- [ ] Location sent successfully
- [ ] Confirmation shows coordinates
- [ ] Next question displays

**Bot Testing - Desktop**:
- [ ] "Share Location" button displays
- [ ] Click opens map picker
- [ ] Manual selection works

**Bot Testing - Validation**:
- [ ] Text message shows error
- [ ] /skip optional succeeds
- [ ] /skip required shows error

**API Testing**:
- [ ] POST with location returns 201
- [ ] POST with invalid coords returns 400
- [ ] GET shows location data

**Frontend Testing**:
- [ ] Create Location question
- [ ] View location answer
- [ ] Coordinates display as text
- [ ] Google Maps link works

---

### Phase 7: Documentation (3-5 hours)

#### DOCS-001: Update Core Layer Documentation

**File**: `src/SurveyBot.Core/CLAUDE.md`
**Estimated**: 1 hour

**Changes**:
- Add Location to QuestionType documentation
- Document LocationAnswerDto
- Document InvalidLocationException
- Update version to 1.5.0

---

#### DOCS-002: Create Feature Documentation

**File**: `documentation/features/LOCATION_QUESTIONS.md`
**Estimated**: 2-3 hours

**Content**:
- Feature overview
- User guide
- Technical implementation
- API reference
- Troubleshooting

---

#### DOCS-003: Update Documentation Index

**File**: `documentation/INDEX.md`
**Estimated**: 0.5 hours

---

#### DOCS-004: Update Main CLAUDE.md

**File**: `CLAUDE.md`
**Estimated**: 1 hour

**Changes**:
- Update version to 1.5.0
- Add Location to question types
- Update entity diagram
- Add to "Recent Changes"

---

## Dependency Graph

```
DATABASE-001 (CHECK constraint migration) ──► CORE-001 (QuestionType enum)
                                                  │
    ┌─────────────────────────────────────────────┤
    │                                             │
    ▼                                             ▼
CORE-004 (InvalidLocationException)        CORE-002 (LocationAnswerDto)
    │                                             │
    │                                             ├──► CORE-003 (AnswerDto update)
    │                                             │
    │                                             └──► BOT-001 (LocationQuestionHandler)
    │                                                      │
    │                                                      ├──► BOT-002 (UpdateHandler)
    │                                                      │        │
    │                                                      │        └──► BOT-003 (DI registration)
    │                                                      │
    │                                                      └──► BOT-004 (Help command)
    │
    ├──► BOT-001 (LocationQuestionHandler)
    │
    └──► INFRA-001 (ResponseService validation)
              │
              └──► API-002 (Controller validation)

CORE-002 + CORE-003
    │
    └──► API-001 (AutoMapper profile + AnswerLocationDataResolver)
              │
              └──► FRONTEND-002 (LocationAnswerDisplay)
                        │
                        └──► FRONTEND-003 (Integration)

All Implementation Tasks
    │
    └──► TEST-001 through TEST-004
              │
              └──► DOCS-001 through DOCS-004
```

### Critical Path (Updated v1.1)

1. **DATABASE-001** (CHECK constraint migration) - **BLOCKER** - Must be first!
2. **CORE-001** (QuestionType enum) - Foundation
3. **CORE-002** (LocationAnswerDto) - Data structure
4. **BOT-001** (LocationQuestionHandler) - Core feature with ReplyKeyboardMarkup
5. **BOT-002** (UpdateHandler routing) - Add Location message branch
6. **INFRA-001** (Validation) - Add Location case to ValidateAnswerFormatAsync
7. **API-001** (AutoMapper) - Add AnswerLocationDataResolver
8. **FRONTEND-002** (Display component) - UI
9. **Testing** - Quality assurance
10. **Documentation** - Completion

---

## Testing Strategy

### Coverage Goals

| Test Type | Target Coverage | Focus Areas |
|-----------|-----------------|-------------|
| Unit Tests | >= 80% | Handler, validation, DTOs |
| Integration Tests | All endpoints | API responses, DB operations |
| Manual Tests | All user flows | Mobile, desktop, error cases |

### Test Priority

1. **P0 (Critical)**: Coordinate validation, answer storage
2. **P1 (High)**: Handler flow, /skip functionality
3. **P2 (Medium)**: Error messages, edge cases
4. **P3 (Low)**: Frontend display, documentation

---

## Risk Assessment

### Low Risks (Acceptable)

| Risk | Impact | Mitigation |
|------|--------|------------|
| Telegram API compatibility | Low | Verified Telegram.Bot 22.7.4 support |
| Data storage | Low | Uses proven JSONB pattern |
| Coordinate validation bypass | Low | Multi-layer validation |

### Medium Risks (Monitor)

| Risk | Impact | Mitigation |
|------|--------|------------|
| Desktop UX limitations | Medium | Clear user guidance, manual map selection |
| User permission denial | Medium | Support /skip, clear error messages |
| Integration testing complexity | Medium | Focus on unit tests, manual testing |

### No High Risks Identified

---

## Effort Summary (Updated v1.1)

| Layer | Tasks | Estimated Hours |
|-------|-------|-----------------|
| **Database** | 1 (DATABASE-001) | 0.5 |
| Core | 4 | 3-5 |
| Bot | 5 | 10-15 |
| Infrastructure | 3 | 4-6 |
| API | 3 | 3-5 |
| Frontend | 4 | 4-6 |
| Testing | 4 | 8-12 |
| Documentation | 4 | 3-5 |
| **TOTAL** | **28** | **35.5-54.5** |

**Adjusted Estimate**: 28-42 hours (with parallelization)

---

## Execution Timeline (Updated v1.1)

### Week 1: Implementation

| Day | Tasks | Hours |
|-----|-------|-------|
| Mon | **DATABASE-001 (FIRST!)**, CORE-001 through CORE-004, BOT-001 start | 6-8 |
| Tue | BOT-001 complete, BOT-002 (Location message handling), BOT-003 | 6-8 |
| Wed | INFRA-001 (ValidateAnswerFormatAsync + CreateAnswerJson), INFRA-002, API-001 (+ AnswerLocationDataResolver) | 6-8 |
| Thu | API-002, FRONTEND-001 through FRONTEND-003 | 6-8 |
| Fri | BOT-004, integration, bug fixes | 4-6 |

### Week 2: Testing & Documentation

| Day | Tasks | Hours |
|-----|-------|-------|
| Mon | TEST-001, TEST-002 | 5-7 |
| Tue | TEST-003, TEST-004 (manual testing) | 5-7 |
| Wed | DOCS-001 through DOCS-004 | 4-6 |
| Thu | Final QA, bug fixes | 4-6 |
| Fri | Release preparation | 2-4 |

---

## Success Criteria Checklist

### Functional

- [ ] Location added to QuestionType enum (value = 4)
- [ ] "Share Location" button displays in Telegram
- [ ] Location data saved to Answer.AnswerJson
- [ ] Coordinate validation working (-90/90, -180/180)
- [ ] /skip works for optional questions
- [ ] Frontend displays coordinates as text
- [ ] Google Maps link provided

### Technical

- [ ] Core layer has zero dependencies
- [ ] No database migration required
- [ ] Unit test coverage >= 80%
- [ ] All integration tests passing
- [ ] Manual testing checklist complete

### Documentation

- [ ] All CLAUDE.md files updated
- [ ] Feature documentation created
- [ ] Version updated to 1.5.0

---

## Appendix: Code Templates

### LocationAnswerDto JSON Format

```json
{
  "latitude": 40.712776,
  "longitude": -74.005974,
  "accuracy": 65.0,
  "timestamp": "2025-11-26T10:30:00Z"
}
```

### Telegram ReplyKeyboardMarkup

```csharp
new ReplyKeyboardMarkup(new[]
{
    new[] { KeyboardButton.WithRequestLocation("📍 Share Location") },
    new[] { new KeyboardButton("/skip") }  // Only for optional
})
{
    ResizeKeyboard = true,
    OneTimeKeyboard = true
}
```

### Frontend Display Format

```
Coordinates: 40.712776, -74.005974
Latitude: 40.712776°
Longitude: -74.005974°
Accuracy: ±65 meters
Captured: 11/26/2025, 10:30:00 AM
[View on Google Maps]
```

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-26 | Feature Planning Agent | Initial plan |
| 1.1 | 2025-11-26 | Codebase Analysis | Added 7 critical findings section, DATABASE-001 task |
| 1.2 | 2025-11-26 | Codebase Analysis | Integrated all critical findings into task descriptions (BOT-001, BOT-002, INFRA-001, API-001) with detailed code fixes |
| 1.3 | 2025-11-26 | Task Execution Agent | Added comprehensive logging strategy with structured logging, privacy-preserving coordinate logging, and debugging scenarios |

---

**Status**: Ready for Implementation
**Next Step**: Begin with DATABASE-001 (Update CHECK constraint), then CORE-001 (Add Location to QuestionType enum)

---

## Logging Strategy (Added per User Request)

> **📝 User Requirement**: Add comprehensive logging at each stage of implementation to facilitate debugging.

### Logging Principles

1. **Structured Logging**: Use Serilog's structured logging with context properties
2. **Log Levels**:
   - `Debug`: Detailed flow information (message received, validation started)
   - `Information`: Key milestones (location saved, validation passed)
   - `Warning`: Recoverable issues (invalid coordinates, missing optional data)
   - `Error`: Failures requiring attention (validation failed, save failed)
3. **Context Properties**: Always include UserId, QuestionId, ResponseId, coordinates when relevant
4. **PII Considerations**: Log coordinate ranges, not exact values in production

### Logging Standards by Layer

#### Bot Layer Logging

**LocationQuestionHandler.cs**:
```csharp
// When displaying location button
_logger.LogInformation(
    "Displaying location question {QuestionId} to user {UserId} for survey {SurveyId}",
    question.Id, userId, surveyId);

// When location received
_logger.LogDebug(
    "Location message received from user {UserId}: Lat={LatitudeRange}, Lon={LongitudeRange}, Accuracy={Accuracy}m",
    userId,
    GetCoordinateRange(latitude),
    GetCoordinateRange(longitude),
    accuracy ?? -1);

// Validation success
_logger.LogInformation(
    "Location coordinates validated for user {UserId}, question {QuestionId}",
    userId, questionId);

// Validation failure
_logger.LogWarning(
    "Invalid location coordinates from user {UserId}: Lat={Latitude}, Lon={Longitude}",
    userId, latitude, longitude);

// Save success
_logger.LogInformation(
    "Location answer saved for response {ResponseId}, question {QuestionId}",
    responseId, questionId);

// Save failure
_logger.LogError(ex,
    "Failed to save location answer for response {ResponseId}, question {QuestionId}",
    responseId, questionId);

// Skip command
_logger.LogInformation(
    "User {UserId} skipped optional location question {QuestionId}",
    userId, questionId);
```

**UpdateHandler.cs**:
```csharp
// Location message detected
_logger.LogDebug(
    "Location message detected from user {UserId}, routing to LocationQuestionHandler",
    message.From?.Id);

// No active state
_logger.LogWarning(
    "Location message from user {UserId} with no active survey state",
    userId);
```

#### Infrastructure Layer Logging

**ResponseService.cs**:
```csharp
// ValidateAnswerFormatAsync - Location case
_logger.LogDebug(
    "Validating Location answer format for question {QuestionId}",
    question.Id);

// ValidateLocationData - Start
_logger.LogDebug(
    "Validating location coordinates: Lat range {LatRange}, Lon range {LonRange}",
    GetCoordinateRange(locationData.Latitude),
    GetCoordinateRange(locationData.Longitude));

// ValidateLocationData - Success
_logger.LogInformation(
    "Location coordinates validation passed for question {QuestionId}",
    questionId);

// ValidateLocationData - Failure
_logger.LogWarning(
    "Invalid location coordinates: Lat={Latitude}, Lon={Longitude} (bounds: -90 to 90, -180 to 180)",
    locationData.Latitude, locationData.Longitude);

// JSON deserialization error
_logger.LogError(ex,
    "Failed to deserialize location JSON for question {QuestionId}: {Json}",
    questionId, answerJson);

// SaveAnswerAsync - Location
_logger.LogInformation(
    "Saving location answer for response {ResponseId}, question {QuestionId}",
    responseId, questionId);
```

#### API Layer Logging

**AnswerMappingProfile.cs** (AnswerLocationDataResolver):
```csharp
// Parsing location data
_logger?.LogDebug(
    "Parsing LocationData for answer {AnswerId}, question type {QuestionType}",
    source.Id, source.Question?.QuestionType);

// Parse success
_logger?.LogDebug(
    "Successfully parsed LocationData for answer {AnswerId}",
    source.Id);

// Parse failure (invalid JSON)
_logger?.LogWarning(
    "Failed to parse LocationData JSON for answer {AnswerId}: {Json}",
    source.Id, source.AnswerJson);

// Not a location question
_logger?.LogDebug(
    "Skipping LocationData parsing for non-location question type {QuestionType}",
    source.Question?.QuestionType);
```

### Helper Methods for Privacy-Preserving Logging

Add to each layer that logs coordinates:

```csharp
/// <summary>
/// Returns coordinate range for logging (e.g., "40-50" for latitude 45.123).
/// Prevents logging exact coordinates while maintaining debugging utility.
/// </summary>
private static string GetCoordinateRange(double coordinate)
{
    var rounded = Math.Floor(coordinate / 10) * 10;
    return $"{rounded} to {rounded + 10}";
}
```

### Logging Integration Checklist

Each implementation task must include:

- [ ] Add ILogger<T> dependency injection to class constructor
- [ ] Log at entry points (method start)
- [ ] Log validation steps (start, success, failure)
- [ ] Log key decisions (skip, save, route)
- [ ] Log errors with exception details
- [ ] Use structured logging with context properties
- [ ] Include correlation IDs (UserId, QuestionId, ResponseId)
- [ ] Avoid logging exact coordinates in production (use ranges)

### Log Analysis Scenarios

**Scenario 1: User reports "location not saving"**
- Search logs for `ResponseId` from user's response
- Filter for `LocationQuestionHandler` and `ResponseService`
- Look for validation failures or save errors
- Check coordinate values in warning logs

**Scenario 2: High error rate for location questions**
- Query for `LogLevel=Error` filtered by `QuestionType=Location`
- Group by error message to identify patterns
- Check if specific coordinate ranges causing issues
- Identify if validation or save stage failing

**Scenario 3: Bot not showing location button**
- Search logs for `UserId` and `QuestionId`
- Check if LocationQuestionHandler invoked
- Verify UpdateHandler routing logs
- Look for state management issues

---

## Critical Findings from Codebase Analysis (v1.1)

> **Note**: This section was added after in-depth codebase analysis to prevent bugs and ensure consistency across all layers.

### CRITICAL-001: Database CHECK Constraint Requires Update

**Issue**: The database CHECK constraint in `QuestionConfiguration.cs` (line 47-49) only allows values 0-3:
```csharp
builder.ToTable(t => t.HasCheckConstraint(
    "chk_question_type",
    "question_type IN (0, 1, 2, 3)"));  // Missing Location = 4
```

**Solution**: Must create a new migration to update the CHECK constraint:
```sql
ALTER TABLE questions DROP CONSTRAINT chk_question_type;
ALTER TABLE questions ADD CONSTRAINT chk_question_type CHECK (question_type IN (0, 1, 2, 3, 4));
```

**Priority**: BLOCKER - Must be done before any Location questions can be created.

---

### CRITICAL-002: UpdateHandler Ignores Location Messages

**Issue**: In `UpdateHandler.cs` (line 107-111), the `HandleMessageAsync` method returns `false` for any message without text:
```csharp
private async Task<bool> HandleMessageAsync(Message message, CancellationToken cancellationToken)
{
    if (message.Text == null)
    {
        _logger.LogDebug("Message has no text content, ignoring");
        return false;  // Location messages get ignored here!
    }
```

**Solution**: Add explicit Location message handling:
```csharp
private async Task<bool> HandleMessageAsync(Message message, CancellationToken cancellationToken)
{
    if (message.Text != null)
    {
        return await HandleTextMessageAsync(message, cancellationToken);
    }
    else if (message.Location != null)
    {
        return await HandleLocationMessageAsync(message, cancellationToken);  // NEW
    }
    else
    {
        _logger.LogDebug("Message has no text or location content, ignoring");
        return false;
    }
}
```

**Priority**: BLOCKER - Location messages will be silently dropped without this fix.

---

### CRITICAL-003: ResponseService.SaveAnswerAsync Missing Latitude/Longitude Parameters

**Issue**: In `ResponseService.cs` (lines 100-106), the `SaveAnswerAsync` method signature does not include latitude/longitude parameters:
```csharp
public async Task<ResponseDto> SaveAnswerAsync(
    int responseId,
    int questionId,
    string? answerText = null,
    List<string>? selectedOptions = null,
    int? ratingValue = null,
    int? userId = null)
```

**Solution**: Either extend the method signature or use AnswerJson parameter for Location data:
- **Option A**: Add `double? latitude = null, double? longitude = null` parameters
- **Option B (Recommended)**: Use existing `answerText` parameter to pass JSON-serialized LocationAnswerDto

**Recommended Approach**: Use AnswerJson approach (already exists in Answer entity) to avoid breaking existing callers:
```csharp
// In LocationQuestionHandler - serialize location to JSON
var locationJson = JsonSerializer.Serialize(new LocationAnswerDto
{
    Latitude = message.Location.Latitude,
    Longitude = message.Location.Longitude,
    Accuracy = message.Location.HorizontalAccuracy,
    Timestamp = DateTime.UtcNow
});
await _responseService.SaveAnswerAsync(responseId, questionId, answerJson: locationJson);
```

---

### CRITICAL-004: ValidateAnswerFormatAsync Does Not Handle Location Type

**Issue**: In `ResponseService.cs` (lines 357-378), the `ValidateAnswerFormatAsync` method validates Text, SingleChoice, MultipleChoice, Rating types but NOT Location:
```csharp
private async Task ValidateAnswerFormatAsync(Question question, ...)
{
    switch (question.QuestionType)
    {
        case QuestionType.Text:
            // validation...
        case QuestionType.SingleChoice:
            // validation...
        case QuestionType.MultipleChoice:
            // validation...
        case QuestionType.Rating:
            // validation...
        // MISSING: case QuestionType.Location
    }
}
```

**Solution**: Add Location validation case:
```csharp
case QuestionType.Location:
    if (string.IsNullOrWhiteSpace(answerJson))
        throw new ValidationException("Location answer requires AnswerJson data.");
    ValidateLocationData(answerJson);
    break;
```

---

### CRITICAL-005: CreateAnswerJson Helper Missing Location Support

**Issue**: In `ResponseService.cs` (lines 638-658), the `CreateAnswerJson` helper method doesn't handle Location type:
```csharp
private string? CreateAnswerJson(Question question, string? answerText, List<string>? selectedOptions, int? ratingValue)
{
    switch (question.QuestionType)
    {
        case QuestionType.SingleChoice:
        case QuestionType.MultipleChoice:
            // JSON for options...
        case QuestionType.Rating:
            // JSON for rating...
        default:
            return null;  // Returns null for Location!
    }
}
```

**Solution**: For Location questions, the JSON should be passed directly (already serialized by handler), so add handling:
```csharp
case QuestionType.Location:
    return answerJson;  // Pass through - already formatted by handler
```

---

### CRITICAL-006: ReplyKeyboardMarkup vs InlineKeyboardMarkup Clarification

**Issue**: The implementation plan correctly specifies `ReplyKeyboardMarkup`, but this is a CRITICAL distinction.

**Technical Requirement**: `KeyboardButton.WithRequestLocation()` ONLY works with `ReplyKeyboardMarkup`, NOT `InlineKeyboardMarkup`:
- `InlineKeyboardMarkup` - Inline buttons attached to messages (NO location support)
- `ReplyKeyboardMarkup` - Custom keyboard replacing default keyboard (HAS location support)

**Correct Implementation**:
```csharp
// CORRECT - ReplyKeyboardMarkup
new ReplyKeyboardMarkup(new[]
{
    new[] { KeyboardButton.WithRequestLocation("📍 Share Location") }
})
{
    ResizeKeyboard = true,
    OneTimeKeyboard = true
}

// WRONG - InlineKeyboardMarkup CANNOT request location
// InlineKeyboardButton does NOT have WithRequestLocation method
```

**Additional Note**: After user shares location, remove keyboard with:
```csharp
await botClient.SendMessage(
    chatId: chatId,
    text: "Thank you!",
    replyMarkup: new ReplyKeyboardRemove()
);
```

---

### CRITICAL-007: AutoMapper AnswerMappingProfile Needs Update

**Issue**: In `AnswerMappingProfile.cs` (lines 16-20), the current mapping does not include LocationData property:
```csharp
CreateMap<Answer, AnswerDto>()
    .ForMember(dest => dest.SelectedOptions,
        opt => opt.MapFrom<AnswerSelectedOptionsResolver>())
    .ForMember(dest => dest.RatingValue,
        opt => opt.MapFrom<AnswerRatingValueResolver>());
    // MISSING: .ForMember(dest => dest.LocationData, ...)
```

**Solution**: Add LocationData mapping with a custom resolver:
```csharp
CreateMap<Answer, AnswerDto>()
    .ForMember(dest => dest.SelectedOptions,
        opt => opt.MapFrom<AnswerSelectedOptionsResolver>())
    .ForMember(dest => dest.RatingValue,
        opt => opt.MapFrom<AnswerRatingValueResolver>())
    .ForMember(dest => dest.LocationData,
        opt => opt.MapFrom<AnswerLocationDataResolver>());  // NEW

// New resolver class
public class AnswerLocationDataResolver : IValueResolver<Answer, AnswerDto, LocationAnswerDto?>
{
    public LocationAnswerDto? Resolve(Answer source, AnswerDto dest, LocationAnswerDto? destMember, ResolutionContext context)
    {
        if (source.Question?.QuestionType != QuestionType.Location)
            return null;
        if (string.IsNullOrWhiteSpace(source.AnswerJson))
            return null;
        try
        {
            return JsonSerializer.Deserialize<LocationAnswerDto>(source.AnswerJson);
        }
        catch
        {
            return null;
        }
    }
}
```

---

### Summary: Implementation Task Updates Required

Based on the codebase analysis, the following tasks need modifications:

| Task | Original Description | Required Modification |
|------|---------------------|----------------------|
| **NEW TASK** | - | Add migration to update CHECK constraint (BLOCKER) |
| BOT-001 | Create LocationQuestionHandler | Use `ReplyKeyboardMarkup` (not Inline), add `ReplyKeyboardRemove` after submission |
| BOT-002 | Update UpdateHandler | Add `else if (message.Location != null)` branch BEFORE the null check |
| INFRA-001 | Add Location Validation | Add `case QuestionType.Location` to `ValidateAnswerFormatAsync` and `CreateAnswerJson` |
| API-001 | Update AutoMapper Profile | Create `AnswerLocationDataResolver` class |

### New Task: DATABASE-001 (Insert Before CORE-001)

**DATABASE-001: Update QuestionType CHECK Constraint**

**File**: New migration
**Command**: `dotnet ef migrations add AddLocationQuestionType`
**Estimated**: 0.5 hours
**Priority**: BLOCKER - Must be first task

**Migration Content**:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Drop existing constraint
    migrationBuilder.Sql("ALTER TABLE questions DROP CONSTRAINT IF EXISTS chk_question_type;");

    // Add updated constraint with Location = 4
    migrationBuilder.Sql("ALTER TABLE questions ADD CONSTRAINT chk_question_type CHECK (question_type IN (0, 1, 2, 3, 4));");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // Revert to original constraint
    migrationBuilder.Sql("ALTER TABLE questions DROP CONSTRAINT IF EXISTS chk_question_type;");
    migrationBuilder.Sql("ALTER TABLE questions ADD CONSTRAINT chk_question_type CHECK (question_type IN (0, 1, 2, 3));");
}
```

**Acceptance Criteria**:
- [ ] Migration created and applied
- [ ] CHECK constraint allows value 4
- [ ] Existing data unaffected
