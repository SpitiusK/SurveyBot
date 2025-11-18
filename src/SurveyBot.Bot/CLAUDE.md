# SurveyBot.Bot - Telegram Bot Layer

**Version**: 1.0.0 | **Framework**: .NET 8.0 | **Telegram.Bot**: 22.7.4

> **Main Documentation**: [Project Root CLAUDE.md](../../CLAUDE.md)
> **Related**: [Core Layer](../SurveyBot.Core/CLAUDE.md) | [API Layer](../SurveyBot.API/CLAUDE.md)

---

## Overview

Bot layer provides Telegram interface for survey management. **Depends ONLY on Core layer**.

**Responsibilities**: Update routing, command handling, question handlers, conversation state, navigation, performance monitoring.

---

## Configuration

**BotConfiguration** (`Configuration/BotConfiguration.cs`):

```json
{
  "BotConfiguration": {
    "BotToken": "your-bot-token",
    "BotUsername": "@YourBot",
    "UseWebhook": false,              // false=polling, true=webhook
    "WebhookUrl": "https://yourdomain.com",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "your-secret",
    "ApiBaseUrl": "http://localhost:5000",
    "RequestTimeout": 30,
    "AdminUserIds": [123456789]       // Admin Telegram IDs
  }
}
```

**Modes**: See [Main Documentation](../../CLAUDE.md#telegram-bot-modes) for webhook vs polling details.

---

## Core Services

### UpdateHandler

**Location**: `Services/UpdateHandler.cs`

**Routes updates by type**:

```
Telegram Update
    ↓
UpdateHandler.HandleUpdateAsync
    ↓
    ├── UpdateType.Message → HandleMessageAsync
    │   ├── Starts with '/' → CommandRouter
    │   └── Regular text → HandleTextMessageAsync
    │
    ├── UpdateType.CallbackQuery → HandleCallbackQueryAsync
    │   ├── "nav_back_q*" → NavigationHandler.HandleBackAsync
    │   ├── "nav_skip_q*" → NavigationHandler.HandleSkipAsync
    │   ├── "cancel_*" → CancelCallbackHandler
    │   ├── "cmd:*" → Execute command
    │   └── "listsurveys:page:*" → Pagination
    │
    └── UpdateType.EditedMessage → Ignore (logged)
```

**Callback Data Formats**:
- `cmd:commandName` - Execute command from button
- `nav_back_q{questionId}` - Go back to previous question
- `nav_skip_q{questionId}` - Skip optional question
- `cancel_confirm` / `cancel_dismiss` - Cancel confirmation
- `listsurveys:page:{pageNumber}` - Pagination

**Performance**: < 2 second target for update handling, < 100ms for callback response

### CommandRouter

**Location**: `Services/CommandRouter.cs`

**Parses and routes commands**:
```
Input: "/start@botname arg1 arg2"
→ Strip '/' → Remove bot mention → Extract "start" → Route to StartCommandHandler
```

**O(1) lookup** via Dictionary<string, ICommandHandler>

---

## Command Handlers

**Pattern**: Implement `ICommandHandler` interface

```csharp
public interface ICommandHandler
{
    string Command { get; }
    Task HandleAsync(Message message, CancellationToken cancellationToken);
    string GetDescription();
}
```

### Key Commands

**StartCommandHandler** (`/start`):
- Register/update user via `IUserRepository.CreateOrUpdateAsync()`
- Welcome message (different for new vs returning)
- Main menu inline keyboard

**HelpCommandHandler** (`/help`):
- Display all available commands

**SurveysCommandHandler** (`/surveys`):
- Browse active surveys
- Inline keyboard with "Take Survey" buttons

**MySurveysCommandHandler** (`/mysurveys`):
- Show user's created surveys
- Response counts, status (Active/Inactive)

**StatsCommandHandler** (`/stats <survey_id>`) - Admin only:
- Comprehensive statistics
- Total/completed/incomplete responses
- Completion rate, average time
- Top 3 questions by response count

**Admin Commands** (require AdminUserIds):
- `/createsurvey` - Create new survey
- `/listsurveys [page]` - List all surveys (paginated)
- `/activate <id>` - Activate survey
- `/deactivate <id>` - Deactivate survey
- `/adminhelp` - Show admin commands

---

## Question Handlers

**Pattern**: Implement `IQuestionHandler` interface

```csharp
public interface IQuestionHandler
{
    QuestionType QuestionType { get; }
    Task<int> DisplayQuestionAsync(long chatId, QuestionDto question, ...);
    Task<string?> ProcessAnswerAsync(Message? message, CallbackQuery? callbackQuery, ...);
    bool ValidateAnswer(string? answerJson, QuestionDto question);
}
```

### Handler Implementations

**TextQuestionHandler** (`QuestionType.Text`):
- Input: Free-form text message
- Validation: Required check, 4000 char max
- Skip: `/skip` for optional questions only
- Answer format: `{"text": "User's answer"}`

**SingleChoiceQuestionHandler** (`QuestionType.SingleChoice`):
- Input: Inline keyboard (radio buttons)
- One button per option, stacked vertically
- Answer format: `{"selectedOption": "Option 2"}`

**MultipleChoiceQuestionHandler** (`QuestionType.MultipleChoice`):
- Input: Inline keyboard (toggleable checkboxes)
- Click to toggle: ☐ ↔ ☑️
- "Submit Selected" to finalize
- Answer format: `{"selectedOptions": ["Option A", "Option C"]}`

**RatingQuestionHandler** (`QuestionType.Rating`):
- Input: Inline keyboard (1-5 stars)
- Answer format: `{"rating": 4}`
- Validation: Rating 1-5

---

## Conversation State Management

### ConversationStateManager

**Location**: `Services/ConversationStateManager.cs`

**Features**:
- Thread-safe (`SemaphoreSlim`)
- In-memory storage (`ConcurrentDictionary<long, ConversationState>`)
- Automatic session expiration (30 min inactivity)
- Background cleanup (runs every 5 min)

**Key Methods**:

```csharp
// Start survey
await _stateManager.StartSurveyAsync(userId, surveyId, responseId, totalQuestions);

// Get current state
var state = await _stateManager.GetStateAsync(userId);

// Record answer
await _stateManager.AnswerQuestionAsync(userId, questionIndex, answerJson);

// Navigate
await _stateManager.NextQuestionAsync(userId);
await _stateManager.PreviousQuestionAsync(userId);

// Complete/Cancel
await _stateManager.CompleteSurveyAsync(userId);
await _stateManager.CancelSurveyAsync(userId);
```

### ConversationState Model

**Location**: `Models/ConversationState.cs`

```csharp
public class ConversationState
{
    public string SessionId { get; set; }
    public long UserId { get; set; }
    public ConversationStateType CurrentState { get; set; }

    // Survey progress
    public int? CurrentSurveyId { get; set; }
    public int? CurrentResponseId { get; set; }
    public int? CurrentQuestionIndex { get; set; }
    public int? TotalQuestions { get; set; }

    // Answer tracking
    public List<int> AnsweredQuestionIndices { get; set; }
    public Dictionary<int, string> CachedAnswers { get; set; }

    // Computed properties
    public bool IsExpired { get; }          // > 30 min inactive
    public int ProgressPercent { get; }      // Answered / Total * 100
    public bool IsAllAnswered { get; }
}
```

**State Types**:
- `Idle` - No active survey
- `InSurvey` - Currently taking survey
- `AnsweringQuestion` - Awaiting answer
- `ResponseComplete` - Survey finished
- `SessionExpired` - Timed out
- `Cancelled` - User cancelled

**State Transitions**:
```
Idle → InSurvey → AnsweringQuestion → InSurvey (next) → ResponseComplete → Idle
       ↓ /cancel
       Cancelled → Idle
```

---

## Navigation & Flow Control

### NavigationHandler

**Location**: `Handlers/NavigationHandler.cs`

**HandleBackAsync**:
1. Validate not on first question
2. Call `stateManager.PreviousQuestionAsync()`
3. Display previous question
4. Show cached answer if exists

**HandleSkipAsync**:
1. Validate question is optional
2. Create empty answer by question type
3. Submit to API
4. Move to next or complete

**Empty Answer Formats**:
```csharp
QuestionType.Text => {"text": ""}
QuestionType.SingleChoice => {"selectedOption": ""}
QuestionType.MultipleChoice => {"selectedOptions": []}
QuestionType.Rating => {"rating": null}
```

### Survey Taking Flow

```
1. /survey CODE → Start survey
2. Create ConversationState, set index=0
3. Display first question
4. User answers
5. Validate & submit to API
6. Record in state, move to next
7. Repeat steps 3-6
8. Last question answered → Complete
9. POST /api/responses/{id}/complete
10. Show completion message, clear state
```

**Navigation Options** (per question):
- **Answer** → Next question
- **Back** (if not first) → Previous question
- **Skip** (if optional) → Next question
- **Cancel** → Confirmation dialog

---

## Performance & Caching

### BotPerformanceMonitor

**Location**: `Services/BotPerformanceMonitor.cs`

**Thresholds**:
- Target: 2000ms
- Warning: 800ms
- Slow: 1000ms

**Usage**:
```csharp
var result = await _performanceMonitor.TrackOperationAsync(
    "FetchSurveyData",
    async () => await _httpClient.GetAsync("/api/surveys/123"),
    context: "SurveyId=123"
);
```

### SurveyCache

**Location**: `Services/SurveyCache.cs`

**Features**:
- In-memory cache (thread-safe)
- TTL: 5 minutes (configurable)
- Auto-cleanup every 2 minutes
- Hit rate tracking

**Methods**:
```csharp
// Get or fetch
var survey = await _surveyCache.GetOrAddSurveyAsync(
    surveyId: 123,
    factory: async () => await FetchSurveyFromApi(123),
    ttl: TimeSpan.FromMinutes(5)
);

// Invalidate
_surveyCache.InvalidateSurvey(surveyId);
_surveyCache.InvalidateUserSurveys(userId);
```

---

## Admin Authorization

### AdminAuthService

**Location**: `Services/AdminAuthService.cs`

**Configuration-based whitelist**:
```json
{
  "BotConfiguration": {
    "AdminUserIds": [123456789, 987654321]
  }
}
```

**Usage**:
```csharp
if (!_adminAuthService.IsAdmin(userId))
{
    await SendUnauthorizedMessage(chatId);
    return;
}
// Execute admin command
```

---

## Integration with API

**HttpClient Configuration**:
```csharp
services.AddHttpClient<NavigationHandler>()
    .ConfigureHttpClient((sp, client) =>
    {
        var config = sp.GetRequiredService<IOptions<BotConfiguration>>().Value;
        client.BaseAddress = new Uri(config.ApiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(config.RequestTimeout);
    });
```

**API Call Pattern**:
```csharp
var response = await _httpClient.GetAsync($"/api/surveys/{surveyId}", cancellationToken);
if (!response.IsSuccessStatusCode) { /* handle error */ }

var apiResponse = await response.Content
    .ReadFromJsonAsync<ApiResponse<SurveyDto>>(cancellationToken);
return apiResponse?.Data;
```

**Key Endpoints Used**:
- GET `/api/surveys/{id}` - Fetch survey
- GET `/api/surveys/code/{code}` - Find by code
- POST `/api/surveys/{id}/responses` - Start response
- POST `/api/responses/{id}/answers` - Submit answer
- POST `/api/responses/{id}/complete` - Complete survey

---

## Message Templates

**Inline Keyboard Patterns**:

**Command Buttons**:
```csharp
var keyboard = new InlineKeyboardMarkup(new[]
{
    new[] { InlineKeyboardButton.WithCallbackData("Help", "cmd:help") }
});
```

**Navigation Buttons**:
```csharp
// Back button (if not first)
if (!isFirstQuestion)
{
    buttons.Add(new[]
    {
        InlineKeyboardButton.WithCallbackData("⬅️ Back", $"nav_back_q{questionId}")
    });
}

// Skip button (if optional)
if (!question.IsRequired)
{
    buttons.Add(new[]
    {
        InlineKeyboardButton.WithCallbackData("Skip ⏭️", $"nav_skip_q{questionId}")
    });
}
```

**Pagination**:
```csharp
buttons.Add(new[]
{
    InlineKeyboardButton.WithCallbackData("⬅️ Prev", $"listsurveys:page:{page-1}"),
    InlineKeyboardButton.WithCallbackData($"Page {page}/{totalPages}", "listsurveys:noop"),
    InlineKeyboardButton.WithCallbackData("Next ➡️", $"listsurveys:page:{page+1}")
});
```

---

## Error Handling

**Graceful Degradation**:
- Never crash from exceptions
- Always log with context
- Send user-friendly messages
- Continue processing other updates

**Pattern**:
```csharp
try
{
    // Command logic
}
catch (SurveyNotFoundException ex)
{
    _logger.LogWarning(ex, "Survey not found");
    await SendMessage(chatId, "Survey not found. Please check the ID.");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing command");
    await SendMessage(chatId, "An error occurred. Please try again later.");
}
```

---

## Best Practices

1. **Command Design**: Single responsibility, clear names, help text, confirmation for destructive actions
2. **State Management**: Always validate state, update activity on interaction, clean up after completion
3. **Performance**: Cache aggressively, monitor operations, target < 2s response time
4. **UX**: Show progress ("Question 3 of 10"), always provide Back button (except first), clear error messages
5. **Security**: Validate webhook secret, check AdminUserIds, sanitize output
6. **Testing**: Mock Telegram API, test complete flows, test error paths

---

## Quick Reference

**Sending Messages**:
```csharp
await _botClient.SendMessage(chatId, "Hello!");
await _botClient.SendMessage(chatId, "*Bold* _italic_", parseMode: ParseMode.Markdown);
await _botClient.EditMessageText(chatId, messageId, "Updated");
```

**State Management**:
```csharp
await _stateManager.StartSurveyAsync(userId, surveyId, responseId, total);
var state = await _stateManager.GetStateAsync(userId);
await _stateManager.NextQuestionAsync(userId);
await _stateManager.CompleteSurveyAsync(userId);
```

---

## Summary

**SurveyBot.Bot** provides conversational Telegram interface with:

- **Command routing**: Dictionary-based O(1) lookup
- **Question handlers**: Type-specific display and validation
- **State management**: Thread-safe, auto-expiring sessions
- **Navigation**: Back, skip, cancel with validation
- **Performance**: < 2s response time, caching, monitoring
- **Admin system**: Configuration-based authorization
- **Error handling**: Graceful degradation, never crashes

**Key Features**: Polling & webhook modes, inline keyboards, progress tracking, answer caching, API integration

---

**Related Documentation**:
- [Core Layer](../SurveyBot.Core/CLAUDE.md) - Entities, DTOs, interfaces
- [API Layer](../SurveyBot.API/CLAUDE.md) - REST endpoints, authentication
- [Main Documentation](../../CLAUDE.md) - Setup, bot configuration, troubleshooting
