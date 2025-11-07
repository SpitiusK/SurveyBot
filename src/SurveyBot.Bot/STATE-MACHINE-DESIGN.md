# Bot Conversation State Machine Design
**Task**: TASK-032
**Status**: In Progress
**Date**: 2025-11-07

---

## Overview

The conversation state machine manages the user's interaction flow with the Telegram bot. It tracks:
- Current survey being taken
- Current question being answered
- User's progress through the survey
- Session timeout

---

## State Definition

### Core States

```
┌─────────────────────────────────────────────────────────────┐
│                   CONVERSATION STATES                       │
└─────────────────────────────────────────────────────────────┘

1. IDLE
   - User registered but not taking survey
   - Available commands: /surveys, /mysurveys, /help, /start
   - No active survey session

2. WAITING_SURVEY_SELECTION
   - User requested survey list with /surveys
   - Awaiting user to select a survey
   - Can /cancel to go back to IDLE

3. IN_SURVEY
   - User selected survey and started it
   - Sub-states track question progress
   - Can navigate with Back/Skip/Next
   - Can /cancel to exit

4. ANSWERING_QUESTION
   - Bot displayed a question
   - Awaiting user's answer
   - Input validation in progress
   - Can go Back or Skip (if optional)

5. RESPONSE_COMPLETE
   - User answered final question
   - Response marked complete
   - Returning to IDLE

6. SESSION_EXPIRED
   - State timeout (30 minutes)
   - User must restart survey
   - Any command returns to IDLE
```

### State Diagram

```
                    ┌─────────────────────┐
                    │      START BOT       │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │      IDLE            │
    ┌──────────────►│ (Registered User)    │◄──────────────┐
    │               └──────────┬──────────┘               │
    │                          │                          │
    │                  /surveys │ /mysurveys              │
    │                          ▼                          │
    │               ┌─────────────────────┐               │
    │               │ WAITING_SURVEY_     │               │
    │               │ SELECTION           │               │
    │               │ (Survey list shown) │               │
    │               └──────────┬──────────┘               │
    │                          │                          │
    │              Survey selected │ /cancel              │
    │                          ▼                          │
    │               ┌─────────────────────┐               │
    │               │  IN_SURVEY          │               │
    │               │ (Response created)  │               │
    │               └──────────┬──────────┘               │
    │                          │                          │
    │          ┌───────────────┼───────────────┐         │
    │          │               │               │         │
    │       Back (if          Next or           Skip     │
    │       not first)         Answer           (if opt.)│
    │          │               │               │         │
    │          ▼               ▼               ▼         │
    │    PREVIOUS_Q   ANSWERING_QUESTION   SKIP_Q       │
    │          │               │               │         │
    │          │  (Validated)  │               │         │
    │          └───────────┬───┴───────────────┘         │
    │                      │                             │
    │         ┌────────────┴─────────────┐               │
    │         │                          │               │
    │     All Answered?           More Questions?        │
    │         │ YES                      │ YES            │
    │         ▼                          ▼               │
    │    RESPONSE_          ANSWERING_QUESTION           │
    │    COMPLETE                │                       │
    │         │                  │                       │
    │         └──────────────┬───┘                       │
    │                        │                           │
    │                   /cancel (anytime)               │
    │                        │                           │
    │                        ▼                           │
    │                ┌──────────────────┐               │
    │                │ SESSION_EXPIRED  │               │
    │                │ or CANCELLED     │               │
    │                └────────┬─────────┘               │
    │                         │                         │
    │                         └─────────────────────────┘
    │
    └─────────────────────────────────────────────────────
```

---

## State Properties

### State Class Definition

```csharp
public enum ConversationStateType
{
    Idle,
    WaitingSurveySelection,
    InSurvey,
    AnsweringQuestion,
    ResponseComplete,
    SessionExpired,
    Cancelled
}

public class ConversationState
{
    // Identity
    public long UserId { get; set; }
    public string SessionId { get; set; } // Unique session identifier

    // State tracking
    public ConversationStateType CurrentState { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }

    // Survey context
    public int? CurrentSurveyId { get; set; }
    public int? CurrentResponseId { get; set; }

    // Question context
    public int? CurrentQuestionIndex { get; set; }
    public List<int> AnsweredQuestionIndices { get; set; } = new();
    public Dictionary<int, string> AnswerData { get; set; } = new();

    // Navigation
    public Stack<ConversationStateType> StateHistory { get; set; } = new();

    // Timeout
    public bool IsExpired => DateTime.UtcNow - CreatedAt > TimeSpan.FromMinutes(30);

    // Metadata
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

---

## State Transitions

### Transition Rules

| Current State | Trigger | Next State | Condition |
|---------------|---------|-----------|-----------|
| IDLE | /surveys | WAITING_SURVEY_SELECTION | Always |
| IDLE | /mysurveys | Show user's surveys | Always |
| IDLE | /help | Show help | Always |
| WAITING_SURVEY_SELECTION | Survey selected | IN_SURVEY | Survey exists, is active |
| WAITING_SURVEY_SELECTION | /cancel | IDLE | Always |
| IN_SURVEY | User message | ANSWERING_QUESTION | Expecting answer |
| IN_SURVEY | /cancel | CANCELLED | Always |
| ANSWERING_QUESTION | Valid answer | IN_SURVEY (next Q) | Answer validates |
| ANSWERING_QUESTION | Invalid answer | ANSWERING_QUESTION | Show error, retry |
| ANSWERING_QUESTION | Back | IN_SURVEY (prev Q) | Not on first question |
| ANSWERING_QUESTION | Skip | IN_SURVEY (next Q) | Question is optional |
| IN_SURVEY | Last Q answered | RESPONSE_COMPLETE | All questions done |
| RESPONSE_COMPLETE | /survey | WAITING_SURVEY_SELECTION | Always |
| ANY | 30 min timeout | SESSION_EXPIRED | No activity |
| ANY | /cancel | CANCELLED | Always |

### Invalid Transitions
- Cannot go back from first question
- Cannot skip required questions
- Cannot answer from IDLE state
- Cannot skip if at last question

---

## Data Storage Strategy

### In-Memory Storage

```csharp
// Thread-safe dictionary with automatic expiration
private readonly ConcurrentDictionary<long, ConversationState> _states = new();

// Background task to cleanup expired states
private Timer _cleanupTimer;

public async Task SetStateAsync(long userId, ConversationState state)
{
    state.LastActivityAt = DateTime.UtcNow;
    _states.AddOrUpdate(userId, state, (_, _) => state);
}

public async Task<ConversationState> GetStateAsync(long userId)
{
    if (_states.TryGetValue(userId, out var state))
    {
        if (state.IsExpired)
        {
            _states.TryRemove(userId, out _);
            return null;
        }
        state.LastActivityAt = DateTime.UtcNow;
        return state;
    }
    return null;
}

public async Task ClearStateAsync(long userId)
{
    _states.TryRemove(userId, out _);
}
```

### Why In-Memory?
1. **Performance**: < 1ms access time
2. **Simplicity**: No database overhead
3. **Scope**: Temporary session data
4. **Scalability**: Can add Redis later if needed
5. **Cleanup**: Automatic 30-minute expiration

### Persistence Strategy

**What gets persisted to database**:
- ✅ Response records (permanently)
- ✅ Answer records (permanently)
- ✅ User's answered questions (in Response)

**What stays in memory**:
- ✅ Current state (temporary, expires in 30 min)
- ✅ Navigation history (temporary)
- ✅ Partial answers before save (temporary)

---

## Timeout Implementation

### Expiration Logic

```csharp
public class ConversationStateManager
{
    private const int EXPIRATION_MINUTES = 30;
    private const int CLEANUP_INTERVAL_MINUTES = 5;

    public void StartCleanupTimer()
    {
        _cleanupTimer = new Timer(
            async _ => await CleanupExpiredStatesAsync(),
            null,
            TimeSpan.FromMinutes(CLEANUP_INTERVAL_MINUTES),
            TimeSpan.FromMinutes(CLEANUP_INTERVAL_MINUTES)
        );
    }

    private async Task CleanupExpiredStatesAsync()
    {
        var expired = _states
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var userId in expired)
        {
            _states.TryRemove(userId, out _);
            _logger.LogInformation($"Expired state for user {userId}");
        }
    }
}
```

### User Notification

When state expires:
```csharp
var expiredMessage = "⏱️ Your survey session has expired (30 minutes inactive).\n\n" +
    "Type /survey CODE to restart.";

await _botClient.SendTextMessageAsync(userId, expiredMessage);
```

### Prevention

Update `LastActivityAt` on every user interaction:
- ✅ Answering questions
- ✅ Clicking buttons
- ✅ Sending messages
- ✅ Navigation actions

---

## Concurrency & Thread Safety

### Thread-Safe Implementation

```csharp
public class ConversationStateManager
{
    // Use ConcurrentDictionary instead of Dictionary
    private readonly ConcurrentDictionary<long, ConversationState> _states = new();

    // Lock for complex multi-step operations
    private readonly SemaphoreSlim _transitionLock = new(1, 1);

    public async Task<bool> TryTransitionAsync(
        long userId,
        ConversationStateType targetState)
    {
        await _transitionLock.WaitAsync();
        try
        {
            var current = _states[userId];
            if (IsValidTransition(current.CurrentState, targetState))
            {
                current.CurrentState = targetState;
                current.StateHistory.Push(current.CurrentState);
                return true;
            }
            return false;
        }
        finally
        {
            _transitionLock.Release();
        }
    }
}
```

### Race Condition Prevention

1. **State Atomicity**: All state changes atomic via ConcurrentDictionary
2. **Transition Locking**: Lock during multi-step transitions
3. **Validation First**: Validate before updating
4. **Logging**: Log all state changes for debugging

---

## Error Handling & Edge Cases

### Timeout During Survey

```
User takes survey
5 minutes: No activity
State expires
User sends message: "What's the next question?"
Bot responds: "⏱️ Session expired. Type /survey CODE to restart."
```

### Back Button on First Question

```
State: IN_SURVEY, CurrentQuestionIndex = 0
User clicks: [Back]
Validation: FirstQuestion? Yes
Response: No back button shown
```

### Skip on Required Question

```
Question: "Name?", IsRequired = true
User clicks: [Skip]
Validation: Required? Yes
Response: "This question is required."
```

### Network Timeout

```
User sends answer
Network fails
State still transitions (optimistic)
User: "The message didn't send!"
Bot: Shows current question state
User can answer again or navigate
```

---

## State Metadata

### Additional Context

```csharp
public class ConversationState
{
    public Dictionary<string, object> Metadata { get; set; } = new()
    {
        // Survey-specific
        { "SurveyCode", "ABC123" },
        { "SurveyTitle", "Customer Satisfaction" },
        { "TotalQuestions", 10 },

        // Progress
        { "ProgressPercent", 30 },
        { "QuestionsAnswered", 3 },

        // User-specific
        { "UserName", "John Doe" },
        { "StartTime", DateTime.UtcNow },

        // Session-specific
        { "BrowserVersion", "..." },
        { "Timezone", "UTC" }
    };
}
```

---

## State Persistence to Database (Optional)

### When to Save State to DB

For MVP, keep in memory. For future:

```csharp
// Optional: Persist to database every 5 minutes
// Allows recovery if bot service crashes

private Timer _persistenceTimer;

public async Task PersistStateAsync(long userId)
{
    var state = await GetStateAsync(userId);
    if (state != null)
    {
        var sessionRecord = new SessionState
        {
            UserId = userId,
            StateJson = JsonConvert.SerializeObject(state),
            SavedAt = DateTime.UtcNow
        };
        await _sessionRepository.SaveAsync(sessionRecord);
    }
}
```

---

## State Machine Interface

### Public API

```csharp
public interface IConversationStateManager
{
    // State access
    Task<ConversationState> GetStateAsync(long userId);
    Task SetStateAsync(long userId, ConversationState state);
    Task ClearStateAsync(long userId);
    Task<bool> HasActiveStateAsync(long userId);

    // Transitions
    Task<bool> TryTransitionAsync(long userId, ConversationStateType newState);

    // Survey operations
    Task<bool> StartSurveyAsync(long userId, int surveyId, int responseId);
    Task<bool> AnswerQuestionAsync(long userId, int questionIndex, string answer);
    Task<bool> GoBackAsync(long userId);
    Task<bool> SkipQuestionAsync(long userId);
    Task<bool> CancelSurveyAsync(long userId);
    Task<bool> CompleteSurveyAsync(long userId);

    // Utilities
    Task<int> GetCurrentQuestionIndexAsync(long userId);
    Task<int> GetCurrentSurveyIdAsync(long userId);
    Task<float> GetProgressPercentAsync(long userId);
}
```

---

## Implementation Checklist

### Phase 3.1: State Machine Design ✅
- [x] State diagram
- [x] State transitions table
- [x] State persistence strategy (In-memory)
- [x] Timeout logic
- [x] Thread safety approach
- [x] Error handling strategy

### Phase 3.2: State Manager Implementation (TASK-033)
- [ ] ConversationState class
- [ ] ConversationStateManager class
- [ ] Expiration/cleanup logic
- [ ] Thread-safe operations
- [ ] DI registration
- [ ] Unit tests

### Phase 3.3+: Use in Commands
- [ ] /survey command uses state
- [ ] Question handlers update state
- [ ] Navigation updates state
- [ ] Completion clears state
- [ ] Timeout handling

---

## Key Decisions

### Decision 1: In-Memory vs Database
**Choice**: In-Memory with 30-minute expiration
**Reason**:
- Performance critical (< 100ms response)
- Temporary session data
- Automatic cleanup
- Simple implementation
- Can upgrade to Redis later

### Decision 2: Expiration Time
**Choice**: 30 minutes
**Reason**:
- Not too short (user might pause)
- Not too long (resource waste)
- Telegram typical session length
- Configurable if needed

### Decision 3: State History Tracking
**Choice**: Stack of state transitions
**Reason**:
- Useful for debugging
- Can implement "Go back to previous state"
- Minimal overhead
- Helpful for logging

### Decision 4: Concurrent Access
**Choice**: ConcurrentDictionary + SemaphoreSlim
**Reason**:
- Thread-safe without complex locking
- Lock only when necessary (transitions)
- High concurrency support
- Proven pattern in .NET

---

## Testing Strategy

### Unit Tests (TASK-046)
```csharp
[Fact]
public async Task StateExpires_After30Minutes()
{
    var state = new ConversationState { CreatedAt = DateTime.UtcNow.AddMinutes(-31) };
    Assert.True(state.IsExpired);
}

[Fact]
public async Task CannotGoBack_OnFirstQuestion()
{
    var state = new ConversationState { CurrentQuestionIndex = 0 };
    var result = await _manager.GoBackAsync(userId);
    Assert.False(result);
}

[Fact]
public async Task CanSkip_OnOptionalQuestion()
{
    // Question with IsRequired = false
    var result = await _manager.SkipQuestionAsync(userId);
    Assert.True(result);
}

[Fact]
public async Task ConcurrentUpdates_AreThreadSafe()
{
    var tasks = Enumerable.Range(0, 100)
        .Select(i => _manager.SetStateAsync(userId, state))
        .ToList();

    await Task.WhenAll(tasks);

    var final = await _manager.GetStateAsync(userId);
    Assert.NotNull(final);
}
```

---

## Summary

### State Machine Complete ✅

**Deliverables**:
- ✅ 6 core states defined
- ✅ State transition rules documented
- ✅ In-memory storage strategy
- ✅ 30-minute timeout with cleanup
- ✅ Thread-safe implementation plan
- ✅ Error handling approach
- ✅ Testing strategy

**Next Step**: TASK-033 (Implement State Manager)

**Estimated Time**: 7 hours

---

**Status**: Design Complete ✅
**Next**: Implementation (TASK-033)

