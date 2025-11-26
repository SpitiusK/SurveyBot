# ConversationStateQuestionIndexTests - Test Suite Documentation

## Overview

This test suite (`ConversationStateQuestionIndexTests.cs`) provides comprehensive unit tests for conversation state question index tracking during conditional flow transitions in the SurveyBot Telegram bot.

**Purpose**: Verify that the question index logic correctly handles conditional navigation to prevent the "already answered" bug when transitioning between questions.

**Bug Fixed**: When users answered a question and conditional flow navigated to a non-sequential question (e.g., Q77 → Q78 skipping Q79), the `CurrentQuestionIndex` wasn't being updated. This caused the handler to fetch the wrong question from the cached questions list, resulting in "already answered" errors.

## Test File Location

```
C:\Users\User\Desktop\SurveyBot\tests\SurveyBot.Tests\Unit\Bot\ConversationStateQuestionIndexTests.cs
```

## Test Categories

### Category 1: Question Index Mapping Tests (6 tests)

Tests verifying correct question ID to index mapping in survey DTOs:

- ✅ `FindQuestionIndex_SequentialQuestions_ReturnsCorrectIndex`
- ✅ `FindQuestionIndex_NonSequentialIds_ReturnsCorrectIndex`
- ✅ `FindQuestionIndex_QuestionNotFound_ReturnsNegativeOne`
- ✅ `FindQuestionIndex_EmptySurvey_ReturnsNegativeOne`
- ✅ `AccessQuestionByIndex_ValidIndex_ReturnsCorrectQuestion`
- ✅ `AccessQuestionByIndex_InvalidIndex_ThrowsException`

**Key Insight**: Question IDs may not be sequential (e.g., Q77, Q79, Q78), but indexes in the Questions list are always sequential (0, 1, 2).

### Category 2: Conversation State Index Tracking (4 tests)

Tests verifying `ConversationState.CurrentQuestionIndex` behavior:

- ✅ `ConversationState_InitialIndex_IsZero`
- ✅ `ConversationState_UpdateIndex_TracksNewValue`
- ✅ `ConversationState_BackwardNavigation_AllowsDecreasingIndex`
- ✅ `ConversationState_SkipMultipleQuestions_UpdatesIndexCorrectly`

**Key Insight**: Index must be updated whenever conditional flow navigates to a new question, even if going backward or skipping multiple questions.

### Category 3: Visited Question Tracking (5 tests)

Tests verifying cycle prevention using `VisitedQuestionIds`:

- ✅ `HasVisitedQuestion_NewQuestion_ReturnsFalse`
- ✅ `RecordVisitedQuestion_AddsQuestionId`
- ✅ `RecordVisitedQuestion_DuplicateId_DoesNotAddTwice`
- ✅ `HasVisitedQuestion_MultipleQuestions_TracksAll`
- ✅ `ClearVisitedQuestions_RemovesAllRecords`

**Key Insight**: `VisitedQuestionIds` tracks actual question IDs visited (for cycle prevention), independent of `CurrentQuestionIndex` (for progress display).

### Category 4: Index and VisitedQuestions Integration (2 tests)

Tests verifying both tracking mechanisms work together:

- ✅ `IndexUpdate_IndependentOfVisitedTracking`
- ✅ `MultipleTransitions_TracksBothIndexAndVisited`

**Key Insight**: Two separate concerns:
- `CurrentQuestionIndex` (int?) → For progress display ("Question 3 of 10")
- `VisitedQuestionIds` (List<int>) → For cycle prevention (prevent re-answering)

### Category 5: Bug Scenario Verification (3 tests)

Tests documenting the bug and verifying the fix:

- ✅ `BugScenario_NonSequentialIds_IndexMustBeUpdated`
- ✅ `BugScenario_StaleIndex_CausesAlreadyAnsweredError`
- ✅ `FixScenario_UpdatedIndex_PreventsAlreadyAnsweredError`

**Key Insight**: These tests serve as regression tests and documentation of what the bug was and how it was fixed.

## Total Test Count: 20 Tests

## Test Approach

### What We Test

This test suite uses **unit testing** approach focusing on:

1. **Data structure behavior** - How `SurveyDto.Questions` list works with `FindIndex()`
2. **State management** - How `ConversationState` properties behave
3. **Logic validation** - The core logic used by `SurveyResponseHandler.UpdateQuestionIndexAsync`

### What We Don't Test

We do NOT test:

- Full integration with `SurveyResponseHandler` (would require complex mocking)
- Telegram bot API calls
- HTTP client calls to API
- Database operations

**Rationale**: The private `UpdateQuestionIndexAsync` method uses simple logic:

```csharp
var questionIndex = survey.Questions.FindIndex(q => q.Id == questionId);
if (questionIndex >= 0)
{
    state.CurrentQuestionIndex = questionIndex;
    await _stateManager.SetStateAsync(userId, state);
}
```

Testing the data structures and logic used by this method provides sufficient coverage without the complexity of integration testing.

## Bug Context

### The Original Bug

**Scenario**:
- Survey: [Q77 (index 0), Q79 (index 1), Q78 (index 2)]
- User at Q77 (index 0), answers it
- Conditional flow: Q77 → Q78 (skip Q79)
- Bug: `CurrentQuestionIndex` stayed at 0
- Handler fetched: `survey.Questions[0]` = Q77 (wrong!)
- Result: "⚠️ You've already answered this question" error

### The Fix

**SurveyResponseHandler.UpdateQuestionIndexAsync** (added in bug fix):

```csharp
private async Task UpdateQuestionIndexAsync(
    long userId,
    ConversationState state,
    SurveyDto survey,
    int questionId)
{
    var questionIndex = survey.Questions.FindIndex(q => q.Id == questionId);

    if (questionIndex >= 0)
    {
        _logger.LogDebug(
            "Updating CurrentQuestionIndex from {OldIndex} to {NewIndex} for Question {QuestionId}",
            state.CurrentQuestionIndex,
            questionIndex,
            questionId);

        state.CurrentQuestionIndex = questionIndex;
        await _stateManager.SetStateAsync(userId, state);
    }
    else
    {
        _logger.LogWarning(
            "Could not find Question {QuestionId} in cached survey questions list. Index not updated.",
            questionId);
    }
}
```

**Called from**:
- `HandleMessageResponseAsync` (line ~208) - After text answer processing
- `HandleCallbackResponseAsync` (line ~376) - After choice/rating answer processing

## Running the Tests

```bash
# Run all tests in this file
dotnet test --filter "FullyQualifiedName~ConversationStateQuestionIndexTests"

# Run specific test
dotnet test --filter "FullyQualifiedName~ConversationStateQuestionIndexTests.BugScenario_NonSequentialIds_IndexMustBeUpdated"

# Run with detailed output
dotnet test --filter "FullyQualifiedName~ConversationStateQuestionIndexTests" --logger "console;verbosity=detailed"
```

## Test Data Patterns

### Survey Creation Helper

```csharp
var survey = CreateSurveyWithQuestions(
    (77, QuestionType.SingleChoice, 0),  // Q77 at index 0
    (79, QuestionType.SingleChoice, 1),  // Q79 at index 1
    (78, QuestionType.Text, 2)           // Q78 at index 2
);
```

### State Creation Helper

```csharp
var state = new ConversationState
{
    CurrentQuestionIndex = 0,
    VisitedQuestionIds = new List<int>()
};
```

## Key Principles Tested

1. **Question IDs ≠ Question Indexes** - IDs can be non-sequential, indexes are always 0, 1, 2, ...
2. **Index Must Be Updated** - When navigating to new question, find its index and update state
3. **Two Tracking Mechanisms** - Index for progress, IDs for cycle prevention
4. **Graceful Degradation** - Handle edge cases (empty surveys, missing questions) without crashing

## Integration with Actual Code

These tests verify the logic used by:

- **File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Handlers\SurveyResponseHandler.cs`
- **Method**: `UpdateQuestionIndexAsync` (lines 563-589)
- **Callers**:
  - `HandleMessageResponseAsync` (line 208)
  - `HandleCallbackResponseAsync` (line 376)

## Success Criteria

All 20 tests pass, verifying:

✅ Question index mapping works correctly for sequential and non-sequential IDs
✅ Conversation state correctly tracks current question index
✅ Visited questions tracking prevents cycles
✅ Both mechanisms work together
✅ Bug scenario is documented and prevented

## Related Documentation

- **Main**: `C:\Users\User\Desktop\SurveyBot\CLAUDE.md`
- **Bot Layer**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\CLAUDE.md`
- **Testing Guide**: `C:\Users\User\Desktop\SurveyBot\tests\SurveyBot.Tests\CLAUDE.md`

---

**Last Updated**: 2025-11-24
**Version**: 1.0.0
**Status**: ✅ All Tests Passing
