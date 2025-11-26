# SurveyBot.Bot - Telegram Bot Layer

**Version**: 1.4.0 | **Framework**: .NET 8.0 | **Telegram.Bot**: 22.7.4

> **Main Documentation**: [Project Root CLAUDE.md](../../CLAUDE.md)
> **Related**: [Core Layer](../SurveyBot.Core/CLAUDE.md) | [API Layer](../SurveyBot.API/CLAUDE.md)

---

## Overview

Bot layer provides Telegram interface for survey management. **Depends on Core layer** for domain entities and interfaces.

**Responsibilities**: Update routing, command handling, question handlers with multimedia support, conversation state, navigation, performance monitoring, conditional flow navigation.

### Architecture Highlights

**13 Command Handlers**: Start, Help, Surveys, MySurveys, Survey, Cancel, CreateSurvey, ListSurveys, Activate, Deactivate, Stats, AdminHelp, AllResponses

**4 Question Handlers**: Text, SingleChoice, MultipleChoice, Rating (all with multimedia support)

**3 Specialized Handlers**: NavigationHandler (back/skip), SurveyResponseHandler (answer processing), CompletionHandler

**Core Services**:
- **BotService**: Lifecycle management, webhook/polling modes
- **UpdateHandler**: Central routing hub with performance monitoring
- **CommandRouter**: Dictionary-based O(1) command lookup
- **ConversationStateManager**: Thread-safe in-memory state with auto-expiration
- **SurveyNavigationHelper**: API-driven conditional flow navigation (v1.4.0)
- **TelegramMediaService**: Media file delivery with retry logic (v1.3.0)
- **QuestionMediaHelper**: Multimedia display in questions (v1.3.0)

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

### BotService

**Location**: `Services/BotService.cs`

**Purpose**: Bot lifecycle management and mode configuration (webhook vs polling)

**Features**:
- Telegram Bot API client initialization
- Mode switching: Webhook (production) vs Polling (development)
- Graceful startup and shutdown
- Health monitoring integration

**Webhook Mode** (Production):
```csharp
// Configured via appsettings.json
{
  "BotConfiguration": {
    "UseWebhook": true,
    "WebhookUrl": "https://yourdomain.com",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "your-secret-key"
  }
}
```

**Polling Mode** (Development):
```csharp
// Local development - no HTTPS required
{
  "BotConfiguration": {
    "UseWebhook": false
  }
}
```

**Startup Process**:
1. Initialize TelegramBotClient with token
2. If webhook: Register webhook URL with Telegram API
3. If polling: Start background polling service
4. Verify bot info (GetMeAsync)
5. Log successful initialization

### UpdateHandler

**Location**: `Services/UpdateHandler.cs`

**Purpose**: Central routing hub for all Telegram updates with performance monitoring

**Routes updates by type**:

```
Telegram Update
    ↓
UpdateHandler.HandleUpdateAsync (Performance Monitoring Start)
    ↓
    ├── UpdateType.Message → HandleMessageAsync
    │   ├── Starts with '/' → CommandRouter
    │   │   └── Dictionary<string, ICommandHandler> O(1) lookup
    │   └── Regular text → HandleTextMessageAsync
    │       └── Check ConversationState → Route to SurveyResponseHandler
    │
    ├── UpdateType.CallbackQuery → HandleCallbackQueryAsync
    │   ├── Parse callback data format
    │   ├── "nav_back_q*" → NavigationHandler.HandleBackAsync
    │   ├── "nav_skip_q*" → NavigationHandler.HandleSkipAsync
    │   ├── "cancel_*" → CancelCallbackHandler
    │   ├── "cmd:*" → Execute command via CommandRouter
    │   ├── "listsurveys:page:*" → Pagination handler
    │   ├── "choice_{questionId}_{optionText}" → SingleChoice answer
    │   ├── "toggle_{questionId}_{optionText}" → MultipleChoice toggle
    │   ├── "submit_{questionId}" → MultipleChoice submit
    │   └── "rating_{questionId}_{value}" → Rating answer
    │
    └── UpdateType.EditedMessage → Ignore (logged for diagnostics)
    ↓
Performance Monitoring End (Log if > 800ms warning threshold)
```

**Callback Data Parsing Logic**:

```csharp
// Pattern matching in HandleCallbackQueryAsync
if (callbackData.StartsWith("nav_back_q"))
{
    var questionId = int.Parse(callbackData.Replace("nav_back_q", ""));
    await _navigationHandler.HandleBackAsync(query, questionId);
}
else if (callbackData.StartsWith("choice_"))
{
    // Format: "choice_{questionId}_{optionText}"
    var parts = callbackData.Split('_', 3);
    var questionId = int.Parse(parts[1]);
    var selectedOption = parts[2];
    await _surveyResponseHandler.HandleCallbackResponseAsync(query);
}
// ... other patterns
```

**Callback Data Formats**:
- `cmd:commandName` - Execute command from button
- `nav_back_q{questionId}` - Go back to previous question
- `nav_skip_q{questionId}` - Skip optional question
- `cancel_confirm` / `cancel_dismiss` - Cancel confirmation
- `listsurveys:page:{pageNumber}` - Pagination
- `choice_{questionId}_{optionText}` - SingleChoice selection
- `toggle_{questionId}_{optionText}` - MultipleChoice toggle
- `submit_{questionId}` - MultipleChoice submit
- `rating_{questionId}_{value}` - Rating selection (1-5)

**Performance Targets**:
- **Update handling**: < 2000ms (target), 800ms (warning), 1000ms (slow log)
- **Callback response**: < 100ms (AnswerCallbackQuery to remove loading state)
- **Command execution**: < 1000ms for simple commands, < 2000ms for data fetching

**Error Handling**:
- Never crashes - all exceptions caught and logged
- User-friendly error messages
- Continue processing other updates after error
- Webhook errors logged but don't interrupt bot

### CommandRouter

**Location**: `Services/CommandRouter.cs`

**Purpose**: Parse and route commands to appropriate handlers

**Parses and routes commands**:
```
Input: "/start@botname arg1 arg2"
→ Strip '/' → Remove bot mention → Extract "start" → Route to StartCommandHandler
```

**O(1) lookup** via `Dictionary<string, ICommandHandler>`

**Registered Commands** (13 total):
```csharp
private readonly Dictionary<string, ICommandHandler> _commandHandlers = new()
{
    // User commands (all users)
    { "start", _startCommandHandler },
    { "help", _helpCommandHandler },
    { "surveys", _surveysCommandHandler },
    { "mysurveys", _mySurveysCommandHandler },
    { "survey", _surveyCommandHandler },
    { "cancel", _cancelCommandHandler },

    // Admin commands (AdminUserIds only)
    { "createsurvey", _createSurveyCommandHandler },
    { "listsurveys", _listSurveysCommandHandler },
    { "activate", _activateCommandHandler },
    { "deactivate", _deactivateCommandHandler },
    { "stats", _statsCommandHandler },
    { "adminhelp", _adminHelpCommandHandler },
    { "allresponses", _allResponsesCommandHandler }
};
```

**Argument Parsing**:
```csharp
// Extracts arguments from command
// Example: "/stats 123" → command="stats", args=["123"]
var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
var command = parts[0].TrimStart('/').Split('@')[0].ToLower();
var args = parts.Skip(1).ToArray();
```

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
- **Multimedia display**: Shows attached images, videos, audio, documents (NEW in v1.3.0)
- Answer format: `{"text": "User's answer"}`

**SingleChoiceQuestionHandler** (`QuestionType.SingleChoice`):
- Input: Inline keyboard (radio buttons)
- One button per option, stacked vertically
- **Multimedia display**: Shows attached media before options (NEW in v1.3.0)
- Answer format: `{"selectedOption": "Option 2"}`

**MultipleChoiceQuestionHandler** (`QuestionType.MultipleChoice`):
- Input: Inline keyboard (toggleable checkboxes)
- Click to toggle: ☐ ↔ ☑️
- "Submit Selected" to finalize
- **Multimedia display**: Shows attached media before options (NEW in v1.3.0)
- Answer format: `{"selectedOptions": ["Option A", "Option C"]}`

**RatingQuestionHandler** (`QuestionType.Rating`):
- Input: Inline keyboard (1-5 stars)
- **Multimedia display**: Shows attached media before rating scale (NEW in v1.3.0)
- Answer format: `{"rating": 4}`
- Validation: Rating 1-5

### Multimedia Support (NEW in v1.3.0)

**Question Media Display**:
All question handlers automatically display attached multimedia content before the question text using Telegram's native media sending methods:

- **Images**: Sent via `SendPhotoAsync` with caption
- **Videos**: Sent via `SendVideoAsync` with caption
- **Audio**: Sent via `SendAudioAsync` with caption
- **Documents**: Sent via `SendDocumentAsync` with caption

**Implementation Pattern**:
```csharp
// In each question handler's DisplayQuestionAsync method
if (question.MediaContent != null)
{
    var mediaContent = JsonSerializer.Deserialize<MediaContentDto>(question.MediaContent);
    await SendMediaAsync(chatId, mediaContent, question.QuestionText);
}
else
{
    await SendMessage(chatId, question.QuestionText);
}
```

**Media File Handling**:
- Files retrieved from media storage service
- Automatic type detection and appropriate Telegram method selection
- Graceful fallback if media unavailable
- Thumbnail support for images

---

## Media Handling Architecture (v1.3.0)

### TelegramMediaService

**Location**: `Services/TelegramMediaService.cs`

**Purpose**: Robust media file delivery to Telegram with retry logic and rate limiting

**Features**:
- **Retry Logic**: 3 attempts with exponential backoff (1s, 2s, 4s)
- **Rate Limiting**: Sequential sending with 200ms delay between media items
- **Type Detection**: Automatic selection of Telegram API method based on media type
- **Error Recovery**: Graceful fallback if media unavailable or send fails

**Retry Pattern**:
```csharp
public async Task<Message?> SendMediaAsync(
    long chatId,
    MediaContentDto mediaContent,
    string caption,
    CancellationToken cancellationToken = default)
{
    const int maxRetries = 3;
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            // Fetch media file from storage
            var mediaFile = await _mediaStorageService.GetMediaFileAsync(mediaContent.MediaId);

            // Send via appropriate Telegram method
            return mediaContent.MediaType switch
            {
                MediaType.Image => await _botClient.SendPhotoAsync(chatId, mediaFile, caption),
                MediaType.Video => await _botClient.SendVideoAsync(chatId, mediaFile, caption),
                MediaType.Audio => await _botClient.SendAudioAsync(chatId, mediaFile, caption),
                MediaType.Document => await _botClient.SendDocumentAsync(chatId, mediaFile, caption),
                _ => null
            };
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
            _logger.LogWarning(ex, "Media send attempt {Attempt} failed, retrying in {Delay}s", attempt, delay.TotalSeconds);
            await Task.Delay(delay, cancellationToken);
        }
    }
    return null; // All retries failed
}
```

**Multiple Media Handling**:
```csharp
// Sequential sending with rate limiting
foreach (var media in mediaItems)
{
    await SendMediaAsync(chatId, media, caption);
    await Task.Delay(200); // 200ms between items to avoid rate limiting
}
```

### QuestionMediaHelper

**Location**: `Utilities/QuestionMediaHelper.cs`

**Purpose**: Display multimedia content in all question handlers

**Integration Pattern** (used in all 4 question handlers):
```csharp
public async Task<int> DisplayQuestionAsync(long chatId, QuestionDto question, ...)
{
    // 1. Display media if present
    if (question.MediaContent != null)
    {
        var mediaContent = JsonSerializer.Deserialize<MediaContentDto>(question.MediaContent);
        await _questionMediaHelper.DisplayMediaAsync(chatId, mediaContent, question.QuestionText);
    }

    // 2. Display question text
    var messageText = question.MediaContent != null
        ? $"{question.QuestionText}\n\n" // Media already showed caption
        : question.QuestionText;

    // 3. Display options/input method
    var sentMessage = await _botClient.SendMessage(chatId, messageText, replyMarkup: keyboard);
    return sentMessage.MessageId;
}
```

**Media Display Flow**:
```
QuestionHandler.DisplayQuestionAsync()
    ↓
Check if question.MediaContent != null
    ↓
QuestionMediaHelper.DisplayMediaAsync()
    ↓
TelegramMediaService.SendMediaAsync()
    ↓
    ├─ Attempt 1 → Success → Return
    ├─ Attempt 1 → Fail → Wait 1s → Attempt 2
    ├─ Attempt 2 → Fail → Wait 2s → Attempt 3
    └─ Attempt 3 → Fail → Log error, continue without media
    ↓
Display question text and options
```

**Error Handling Strategy**:
- **Media send fails**: Continue with question display (graceful degradation)
- **Log all failures**: Track media delivery issues for troubleshooting
- **User experience**: Never block question display due to media failure
- **Retry exhausted**: Log error, proceed without media, user sees text-only question

**Performance Considerations**:
- **Sequential sending**: Prevents Telegram rate limiting (429 errors)
- **200ms delay**: Balance between speed and rate limits
- **Async/await**: Non-blocking during delays
- **Cancellation support**: Respects operation cancellation

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
    public int? CurrentQuestionIndex { get; set; }   // Index for progress display
    public int? TotalQuestions { get; set; }

    // Answer tracking
    public List<int> AnsweredQuestionIndices { get; set; }      // For backwards compat
    public Dictionary<int, string> CachedAnswers { get; set; }   // Answers by question index

    // NEW in v1.4.0: Conditional flow - runtime cycle prevention
    public List<int> VisitedQuestionIds { get; set; } = new();  // Actual question IDs visited

    // Computed properties
    public bool IsExpired { get; }          // > 30 min inactive
    public int ProgressPercent { get; }     // Answered / Total * 100
    public bool IsAllAnswered { get; }
}

// NEW in v1.4.0: Helper methods for visited question tracking
// Prevents users from re-answering questions in conditional flows (cycle prevention)
public partial class ConversationState
{
    /// <summary>
    /// Check if user has already visited/answered a question.
    /// Used to prevent cycles in conditional question flows.
    /// </summary>
    public bool HasVisitedQuestion(int questionId) =>
        VisitedQuestionIds.Contains(questionId);

    /// <summary>
    /// Record that user visited a question (answered it).
    /// Prevents re-answering same question if flow loops back.
    /// </summary>
    public void RecordVisitedQuestion(int questionId)
    {
        if (!VisitedQuestionIds.Contains(questionId))
            VisitedQuestionIds.Add(questionId);
    }

    /// <summary>
    /// Clear visited questions (internal use, called on survey completion).
    /// </summary>
    public void ClearVisitedQuestions()
    {
        VisitedQuestionIds.Clear();
    }
}
```

**Updated in v1.4.0**:
- `CurrentQuestionIndex` - Used for progress display only ("Question 3 of 10")
- `VisitedQuestionIds` - Actual question IDs user has answered (for cycle detection)
- Helper methods prevent re-answering same question when conditional flow creates cycles
- Separation: Index tracks progress, QuestionId tracks actual questions visited

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

### SurveyNavigationHelper (NEW in v1.4.0)

**Location**: `Utilities/SurveyNavigationHelper.cs`

**Purpose**: Centralize logic for determining next question in conditional survey flows. Queries API for navigation decisions to implement server-side branching and cycle prevention.

**Key Methods**:

```csharp
// Get next question based on current question and answer
// API endpoint: GET /api/responses/{responseId}/next-question?currentQuestionId={currentQuestionId}
public async Task<QuestionNavigationResult> GetNextQuestionAsync(
    int responseId,
    int currentQuestionId,
    string answerText,
    CancellationToken cancellationToken = default)

// Get first question of survey (for starting response)
// API endpoint: GET /api/surveys/{surveyId}/questions
public async Task<QuestionDto?> GetFirstQuestionAsync(
    int surveyId,
    CancellationToken cancellationToken = default)
```

**Features**:
- **Server-side branching logic**: Queries API endpoint that evaluates conditional flow rules
- **No magic values**: Uses HTTP status codes for navigation decisions:
  - **204 No Content** → Survey complete (no more questions)
  - **200 OK** with QuestionDto → Next question determined by API
  - **4xx/5xx** → Error handling
- **Handles both branching and non-branching**:
  - **Branching questions** (SingleChoice, Rating): User's answer determines next question via conditional rules
  - **Non-branching questions** (Text, MultipleChoice): Standard sequential flow
- **API handles complex logic**: Cycle detection, conditional evaluation, endpoint validation
- **Result types**:
  - `SurveyComplete()` - No more questions (HTTP 204 No Content)
  - `WithNextQuestion(question)` - Next question to display (HTTP 200 OK with data)
  - `Error(message)` - Network or API error (4xx/5xx)
  - `NotFound(questionId)` - Question not found

**Integration with SurveyResponseHandler**:
- Called in `HandleMessageResponseAsync()` after text answer submission
- Called in `HandleCallbackResponseAsync()` after choice/rating answer submission
- Passes:
  - ResponseId - Identifies the user's response session
  - CurrentQuestionId - Question just answered (for API's flow evaluation)
  - AnswerText - Answer JSON (for conditional evaluation if needed)
- Returns navigation result with next question or completion status
- Bot role: Display UI and handle user interaction
- API role: Determine flow based on conditional rules, prevent cycles, validate endpoints

**Survey Completion Detection**:
- HTTP 204 No Content response means no more questions
- Clean separation: No hardcoded "magic value 0" for question IDs
- API controls when survey ends

### Survey Taking Flow (UPDATED in v1.4.0)

```
1. User initiates survey (/survey CODE or button)
2. SurveyNavigationHelper.GetFirstQuestionAsync() gets first question
3. Create ConversationState with ResponseId, SurveyId, TotalQuestions
4. Display current question (text, choice, rating, multiple choice)
5. User answers question
6. Validate answer with question handler
7. POST /api/responses/{responseId}/answers → Submit answer to API
8. SurveyResponseHandler checks if visited (cycle prevention)
9. Record visited question ID in state
10. SurveyNavigationHelper.GetNextQuestionAsync() queries API:
    ├─ API evaluates conditional flow rules
    ├─ For branching (SingleChoice, Rating): User's answer determines next
    ├─ For non-branching (Text, MultipleChoice): Standard sequential next
    └─ Cycle detection: Prevents re-visiting same question
11. API response handling:
    ├─ HTTP 204 No Content → Survey complete
    ├─ HTTP 200 OK with QuestionDto → Next question
    └─ HTTP 4xx/5xx → Error handling
12. If complete: POST /api/responses/{responseId}/complete
13. Show completion message, clear state
14. Return to main menu
```

**Navigation per Question**:
- **Answer button** → Submits and gets next question via API flow
- **Back button** (if supported) → Previous question from cache
- **Skip button** (if optional) → Empty answer, get next via API
- **Cancel** → Confirmation dialog, clear state

**Conditional Flow Features (NEW in v1.4.0)**:
- **SurveyNavigationHelper** replaces hardcoded sequential logic
- **Server-side branching**: API determines next question based on answer + rules
- **Cycle prevention**: State tracks visited question IDs
- **HTTP status code semantics**: 204 = complete, 200 = next question, 4xx = error
- **Clean separation**: Bot handles UI/state, API handles flow logic
- **No magic values**: Question ID-based (not index-based with magic 0 value)

---

## Conditional Question Flow Architecture (v1.4.0)

### Overview

**Purpose**: Enable survey creators to define branching logic where user's answer to one question determines which question appears next.

**Use Cases**:
- **Skip irrelevant questions**: "Do you own a car?" → No → Skip car-related questions
- **Personalized surveys**: "Age?" → Under 18 → Different question set
- **Diagnostic flows**: "Experiencing pain?" → Yes → "Where is the pain?"
- **Complex branching**: Rating-based routing, multi-path surveys

### Architecture Separation

**Bot Layer Responsibilities** (UI & State):
- Display questions with appropriate input methods
- Capture and validate user answers
- Track visited questions for cycle prevention
- Manage conversation state (in-memory)
- Handle Back/Skip navigation

**API Layer Responsibilities** (Flow Logic):
- Evaluate conditional flow rules (if answer = X, go to question Y)
- Determine next question based on answer + rules
- Detect cycles (question A → B → A)
- Validate flow endpoints (target questions exist, are in same survey)
- Enforce business rules (can't branch to inactive survey)

**Integration**: HttpClient-based API calls for navigation decisions only

### Cycle Prevention Strategy

**Problem**: Conditional flow can create cycles (Question A → B → C → A)

**Solution**: Two-layer prevention

**Layer 1: Design-Time Validation (API)**:
```csharp
// In SurveyValidationService (Infrastructure layer)
public async Task ValidateSurveyFlowAsync(int surveyId)
{
    // Build graph of all possible paths
    // Depth-first search from each question
    // Detect back-edges (cycles)
    // Throw SurveyCycleException if cycle found
}
```

**Layer 2: Runtime Protection (Bot)**:
```csharp
// In ConversationState model
public List<int> VisitedQuestionIds { get; set; } = new();

public bool HasVisitedQuestion(int questionId) =>
    VisitedQuestionIds.Contains(questionId);

// In SurveyResponseHandler
if (state.HasVisitedQuestion(nextQuestionId))
{
    _logger.LogWarning("Cycle detected: User already answered question {QuestionId}", nextQuestionId);
    await SendMessage(chatId, "This survey has a configuration error. Please contact support.");
    await _stateManager.CancelSurveyAsync(userId);
    return;
}

// Record visit after successful answer
state.RecordVisitedQuestion(currentQuestionId);
```

**Why Two Layers?**
- **Design-time**: Prevents bad survey creation (survey can't be activated with cycles)
- **Runtime**: Safety net if rules change after survey activation, or edge cases

### Index vs Question ID Separation

**Critical Design Decision** (v1.4.0):

**Before v1.4.0**:
- `CurrentQuestionIndex` - Used for both progress display AND identifying questions
- Problem: Sequential-only, couldn't handle branching, confusing semantics

**After v1.4.0**:
- `CurrentQuestionIndex` - **Only for progress display** ("Question 3 of 10")
- `VisitedQuestionIds` - **Actual question IDs answered** (for cycle prevention, navigation)

**Why Separate?**

```csharp
// Sequential survey: Index matches order
Survey: Q1 → Q2 → Q3 → Q4 → Q5
Index:  1     2     3     4     5
QuestionIds: [1, 2, 3, 4, 5] (sequential)

// Branching survey: Index doesn't match actual path
Survey: Q1 → Q2 (if Yes) → Q4 → Q5
             └→ Q3 (if No) → Q5

User answers "No":
Index progression: 1 → 2 → 3 → 4 (progress: 25%, 50%, 75%, 100%)
Actual questions visited: [1, 2, 3, 5] (not sequential!)
```

**Benefits**:
- **Progress display** remains accurate (Question 3 of 10)
- **Flow logic** uses actual question IDs (flexible branching)
- **Cycle detection** works with IDs (same question = same ID, even if index differs)
- **Clear separation** of concerns (UX vs business logic)

### SurveyNavigationHelper HTTP Semantics

**Location**: `Utilities/SurveyNavigationHelper.cs`

**API Endpoint**: `GET /api/responses/{responseId}/next-question?currentQuestionId={questionId}`

**HTTP Status Code Design**:

```csharp
// 204 No Content - Survey complete (no more questions)
if (nextQuestionId == null)
{
    return NoContent(); // HTTP 204
}

// 200 OK with QuestionDto - Next question determined
return Ok(new ApiResponse<QuestionDto>
{
    Success = true,
    Data = questionDto
}); // HTTP 200

// 404 Not Found - Response or question not found
if (response == null)
{
    return NotFound(new ApiResponse { Success = false, Message = "Response not found" });
}

// 400 Bad Request - Cycle detected or validation error
if (cycleDetected)
{
    return BadRequest(new ApiResponse { Success = false, Message = "Cycle detected in survey flow" });
}
```

**Bot Handling**:
```csharp
var result = await _navigationHelper.GetNextQuestionAsync(responseId, currentQuestionId, answerJson);

return result switch
{
    { IsSurveyComplete: true } => await CompleteSurveyAsync(chatId, userId),
    { NextQuestion: not null } => await DisplayNextQuestionAsync(result.NextQuestion),
    { Error: not null } => await HandleNavigationError(result.Error),
    _ => await HandleUnexpectedResult()
};
```

**Why This Design?**

- **No magic values**: HTTP 204 clearly means "no more questions" (not "questionId = 0" or "questionId = -1")
- **Standard semantics**: 200 = success with data, 204 = success without data, 4xx = client error
- **Strongly typed**: QuestionNavigationResult encapsulates all possible outcomes
- **API-driven**: Bot doesn't need to know flow logic, just interpret HTTP responses

### Integration Patterns: Direct DI vs HTTP Client

**Design Principle**: Use direct DI for core operations, HTTP client only for navigation

**Direct DI** (Infrastructure layer injected into Bot):
```csharp
// In BotServiceExtensions.ConfigureServices()
services.AddScoped<ISurveyRepository, SurveyRepository>();
services.AddScoped<IResponseRepository, ResponseRepository>();
services.AddScoped<IUserRepository, UserRepository>();

// Used in command handlers
public class StartCommandHandler
{
    private readonly IUserRepository _userRepository;

    public async Task HandleAsync(Message message, CancellationToken ct)
    {
        // Direct repository call
        await _userRepository.CreateOrUpdateAsync(user, ct);
    }
}
```

**HTTP Client** (Only for conditional navigation):
```csharp
// In BotServiceExtensions.ConfigureServices()
services.AddHttpClient<SurveyNavigationHelper>()
    .ConfigureHttpClient((sp, client) =>
    {
        client.BaseAddress = new Uri(botConfig.ApiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(botConfig.RequestTimeout);
    });

// Used in SurveyResponseHandler
public class SurveyResponseHandler
{
    private readonly SurveyNavigationHelper _navigationHelper;

    private async Task<QuestionNavigationResult> GetNextQuestionAsync(...)
    {
        // HTTP call to API endpoint
        return await _navigationHelper.GetNextQuestionAsync(responseId, questionId, answerJson);
    }
}
```

**Rationale**:

| Operation | Approach | Why |
|-----------|----------|-----|
| User registration | Direct DI (IUserRepository) | Simple CRUD, no complex logic |
| Fetch survey list | Direct DI (ISurveyRepository) | Data retrieval, no branching |
| Submit answer | Direct DI (IResponseRepository) | Straightforward save operation |
| **Determine next question** | **HTTP Client (API)** | **Complex flow evaluation, cycle detection, needs centralized logic** |

**Why Not HTTP for Everything?**
- **Performance**: Direct DI is faster (no HTTP overhead)
- **Simplicity**: Most operations don't need API layer
- **Independence**: Bot can function even if API layer has issues (for basic operations)

**Why HTTP for Navigation?**
- **Complexity**: Conditional flow evaluation is complex, belongs in API layer
- **Consistency**: API layer is source of truth for flow rules
- **Separation**: Bot shouldn't duplicate flow logic (DRY principle)
- **Flexibility**: Flow rules can change without bot redeployment

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

## Security Considerations

### Webhook Secret Validation

**Purpose**: Prevent unauthorized webhook calls (production mode only)

**Configuration**:
```json
{
  "BotConfiguration": {
    "WebhookSecret": "your-secure-random-secret-key"
  }
}
```

**Validation** (in BotController.cs in API layer):
```csharp
[HttpPost("webhook")]
public async Task<IActionResult> HandleWebhook(
    [FromHeader(Name = "X-Telegram-Bot-Api-Secret-Token")] string? secretToken,
    [FromBody] Update update)
{
    // Validate secret token
    if (_botConfig.WebhookSecret != secretToken)
    {
        _logger.LogWarning("Invalid webhook secret token received");
        return Unauthorized();
    }

    await _updateHandler.HandleUpdateAsync(update);
    return Ok();
}
```

**Best Practices**:
- Generate strong random secret (32+ characters)
- Don't commit secret to source control (use User Secrets or environment variables)
- Rotate secret periodically (update in config + re-register webhook)
- Monitor for unauthorized attempts (failed secret validation)

### Admin Authorization

**Whitelist-Based**:
```csharp
// Only these Telegram user IDs can execute admin commands
"AdminUserIds": [123456789, 987654321]
```

**Enforcement**:
- Checked before executing admin commands
- Returns "Unauthorized" message to non-admin users
- Logged for audit trail

**How to Get Your Telegram User ID**:
1. Message [@userinfobot](https://t.me/userinfobot) on Telegram
2. It will reply with your user ID
3. Add to `AdminUserIds` array in configuration

### Input Sanitization

**Message Length Limits**:
```csharp
// Text answers limited to 4000 characters (Telegram message max)
if (messageText.Length > 4000)
{
    await SendMessage(chatId, "Your answer is too long. Please limit to 4000 characters.");
    return;
}
```

**Command Parsing**:
```csharp
// Sanitize command input to prevent injection
var command = parts[0].TrimStart('/').Split('@')[0].ToLower();
var args = parts.Skip(1).Select(arg => arg.Trim()).ToArray();
```

**Callback Data Validation**:
```csharp
// Validate callback data format before parsing
if (!callbackData.StartsWith("choice_") || callbackData.Split('_').Length != 3)
{
    _logger.LogWarning("Invalid callback data format: {CallbackData}", callbackData);
    await AnswerCallbackQuery(queryId, "Invalid action");
    return;
}
```

### Rate Limiting

**Telegram Rate Limits** (enforced by Telegram API):
- **Messages**: 30 messages/second per bot
- **Bulk messages**: 20 messages/minute to same user
- **Inline queries**: 50 requests/second

**Bot Layer Mitigation**:
- Sequential media sending with 200ms delay
- Cache survey data (avoid repeated API calls)
- Performance monitoring (detect slow operations)

**Future Enhancement**: Implement per-user rate limiting for command execution

### Data Privacy

**In-Memory State** (trade-offs):
- **Pro**: Fast, no persistent storage of answers during survey
- **Con**: Lost on restart (users must start over)
- **Privacy**: Answers only persisted to database when submitted via API

**Logging**:
- **Log**: User IDs, command names, errors, performance metrics
- **Don't log**: Answer content, personal information, webhook secrets
- **Structured logging**: Serilog with context enrichment

---

## Testing Guidance

### Unit Testing Strategy

**What to Test**:

1. **Command Handlers**:
   - Argument parsing (valid/invalid formats)
   - Authorization (admin-only commands)
   - Error handling (repository exceptions)
   - Response message formatting

2. **Question Handlers**:
   - Answer validation (required/optional, format)
   - Media display logic (with/without media)
   - Keyboard generation (correct buttons, callback data)
   - Empty answer creation (skip logic)

3. **Conversation State**:
   - State transitions (Idle → InSurvey → Complete)
   - Progress tracking (CurrentQuestionIndex increment)
   - Visited questions (cycle prevention)
   - Expiration logic (30-minute timeout)

4. **Navigation Logic**:
   - Back button (not available on first question)
   - Skip button (only for optional questions)
   - Cycle detection (HasVisitedQuestion check)
   - Survey completion (all questions answered)

**Mock Strategy**:
```csharp
// Mock Telegram Bot API (ITelegramBotClient)
var mockBotClient = new Mock<ITelegramBotClient>();
mockBotClient
    .Setup(x => x.SendMessage(It.IsAny<ChatId>(), It.IsAny<string>(), ...))
    .ReturnsAsync(new Message { MessageId = 123 });

// Mock repositories
var mockUserRepo = new Mock<IUserRepository>();
mockUserRepo
    .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
    .ReturnsAsync(new User { TelegramUserId = 12345, FirstName = "Test" });
```

**Example Unit Test**:
```csharp
[Fact]
public async Task StartCommandHandler_NewUser_SendsWelcomeMessage()
{
    // Arrange
    var mockBotClient = new Mock<ITelegramBotClient>();
    var mockUserRepo = new Mock<IUserRepository>();
    mockUserRepo
        .Setup(x => x.GetByTelegramIdAsync(12345, default))
        .ReturnsAsync((User?)null); // New user

    var handler = new StartCommandHandler(mockBotClient.Object, mockUserRepo.Object);
    var message = new Message { Chat = new Chat { Id = 12345 }, From = new User { Id = 12345, FirstName = "Test" } };

    // Act
    await handler.HandleAsync(message, default);

    // Assert
    mockBotClient.Verify(x => x.SendMessage(
        It.Is<ChatId>(c => c.Identifier == 12345),
        It.Is<string>(msg => msg.Contains("Welcome")),
        It.IsAny<ParseMode>(),
        It.IsAny<IReplyMarkup>(),
        It.IsAny<CancellationToken>()
    ), Times.Once);

    mockUserRepo.Verify(x => x.CreateOrUpdateAsync(It.IsAny<User>(), default), Times.Once);
}
```

### Integration Testing Strategy

**What to Test**:

1. **Complete Survey Flow**:
   - Start survey → Display first question
   - Answer questions (all types: Text, SingleChoice, MultipleChoice, Rating)
   - Navigate back/skip
   - Complete survey → Verify answers persisted

2. **Conditional Flow**:
   - Branching based on answer (SingleChoice, Rating)
   - Cycle prevention (runtime check)
   - Survey completion after branching

3. **Media Handling**:
   - Display question with media (image, video, audio, document)
   - Retry logic on failure
   - Graceful fallback if media unavailable

4. **State Management**:
   - Concurrent users (multiple ConversationStates)
   - Session expiration (30-minute timeout)
   - State cleanup (background task)

**Test Environment Setup**:
```csharp
// Use in-memory database for repositories
services.AddDbContext<SurveyBotDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));

// Mock Telegram Bot API (can't actually send messages in tests)
services.AddSingleton<ITelegramBotClient>(mockBotClient.Object);

// Real services (state manager, handlers, etc.)
services.AddScoped<ConversationStateManager>();
services.AddScoped<UpdateHandler>();
// ... register all handlers
```

**Example Integration Test**:
```csharp
[Fact]
public async Task CompleteSurveyFlow_TextAndRatingQuestions_SavesAnswers()
{
    // Arrange: Create survey with 2 questions (Text, Rating)
    var survey = await CreateTestSurvey(userId: 1, questions: new[]
    {
        new { Type = QuestionType.Text, Text = "What is your name?", IsRequired = true },
        new { Type = QuestionType.Rating, Text = "How satisfied are you?", IsRequired = true }
    });

    var botClient = _serviceProvider.GetRequiredService<ITelegramBotClient>();
    var updateHandler = _serviceProvider.GetRequiredService<UpdateHandler>();
    var responseRepo = _serviceProvider.GetRequiredService<IResponseRepository>();

    // Act: Simulate user flow
    // 1. Start survey
    await updateHandler.HandleUpdateAsync(new Update
    {
        Message = new Message { Text = $"/survey {survey.SurveyCode}", Chat = new Chat { Id = 12345 } }
    });

    // 2. Answer first question (text)
    await updateHandler.HandleUpdateAsync(new Update
    {
        Message = new Message { Text = "John Doe", Chat = new Chat { Id = 12345 } }
    });

    // 3. Answer second question (rating)
    await updateHandler.HandleUpdateAsync(new Update
    {
        CallbackQuery = new CallbackQuery { Data = "rating_2_5", Message = new Message { Chat = new Chat { Id = 12345 } } }
    });

    // Assert: Verify response saved with correct answers
    var responses = await responseRepo.GetBySurveyIdAsync(survey.Id, default);
    Assert.Single(responses);
    Assert.Equal(2, responses[0].Answers.Count);
    Assert.Equal("John Doe", responses[0].Answers[0].AnswerJson); // Text answer
    Assert.Equal("{\"rating\": 5}", responses[0].Answers[1].AnswerJson); // Rating answer
}
```

### Manual Testing Checklist

**Basic Commands**:
- [ ] `/start` - User registration, welcome message
- [ ] `/help` - Display all commands
- [ ] `/surveys` - List active surveys
- [ ] `/mysurveys` - Show user's created surveys

**Admin Commands** (AdminUserIds required):
- [ ] `/createsurvey` - Create new survey
- [ ] `/listsurveys` - Paginated list
- [ ] `/activate <id>` - Activate survey
- [ ] `/deactivate <id>` - Deactivate survey
- [ ] `/stats <id>` - Survey statistics

**Survey Flow**:
- [ ] Start survey via `/survey <code>` or button
- [ ] Answer text question (required, optional, skip)
- [ ] Answer single choice question
- [ ] Answer multiple choice question (toggle, submit)
- [ ] Answer rating question (1-5 stars)
- [ ] Back button (not on first question)
- [ ] Skip button (only on optional questions)
- [ ] Cancel survey (confirmation dialog)
- [ ] Complete survey (all required answered)

**Conditional Flow**:
- [ ] Branching question (SingleChoice → different next question based on answer)
- [ ] Rating-based branching (Rating → different next question based on score)
- [ ] Cycle prevention (runtime check prevents re-answering)
- [ ] Survey completion after branching (HTTP 204 detection)

**Media Handling**:
- [ ] Question with image (displays before question text)
- [ ] Question with video
- [ ] Question with audio
- [ ] Question with document
- [ ] Multiple media items (sequential sending)
- [ ] Retry logic (simulate failure, verify retry)

**Error Scenarios**:
- [ ] Invalid survey code
- [ ] Non-existent survey ID (admin commands)
- [ ] Unauthorized admin command (non-admin user)
- [ ] Answer too long (> 4000 chars)
- [ ] Network error (API unreachable)
- [ ] Session timeout (30 minutes idle)

---

## Troubleshooting Common Issues

### Bot Not Responding

**Symptoms**: User sends `/start`, no response

**Diagnostic Steps**:
1. **Check bot is running**:
   ```bash
   # Look for "Telegram Bot initialized successfully" in logs
   docker logs surveybot-api | grep "Telegram Bot"
   ```

2. **Verify bot token**:
   ```bash
   # Test token validity
   curl https://api.telegram.org/bot<TOKEN>/getMe
   # Should return bot info, not "Unauthorized"
   ```

3. **Check mode configuration**:
   ```json
   // For local dev, ensure polling mode
   {
     "BotConfiguration": {
       "UseWebhook": false  // Must be false for local
     }
   }
   ```

4. **Check logs for errors**:
   ```bash
   # Look for exceptions during update handling
   docker logs surveybot-api --tail 100 | grep "ERROR"
   ```

**Common Causes**:
- **Invalid token**: Typo in `appsettings.Development.json`
- **Webhook mode locally**: `UseWebhook: true` requires public HTTPS URL
- **Firewall**: Polling blocked by firewall (rare)
- **Bot service not started**: Check DI registration

### Webhook Not Receiving Updates (Production)

**Symptoms**: Bot works locally (polling), but not in production (webhook)

**Diagnostic Steps**:
1. **Check webhook status**:
   ```bash
   curl https://api.telegram.org/bot<TOKEN>/getWebhookInfo
   # Look for "url", "pending_update_count", "last_error_message"
   ```

2. **Verify HTTPS with valid certificate**:
   ```bash
   # Must be HTTPS, not HTTP
   # Certificate must be valid (not self-signed)
   curl -v https://yourdomain.com/api/bot/webhook
   ```

3. **Check webhook secret**:
   ```csharp
   // In BotController.cs, verify secret validation logic
   if (_botConfig.WebhookSecret != secretToken)
   {
       return Unauthorized(); // This will show in webhook errors
   }
   ```

4. **Test webhook manually**:
   ```bash
   # Send test update to webhook
   curl -X POST https://yourdomain.com/api/bot/webhook \
     -H "Content-Type: application/json" \
     -H "X-Telegram-Bot-Api-Secret-Token: your-secret" \
     -d '{"update_id":1,"message":{"chat":{"id":12345},"text":"/start"}}'
   ```

**Common Causes**:
- **Invalid SSL**: Self-signed certificate (Telegram requires valid CA-signed)
- **Wrong webhook URL**: Typo in `WebhookUrl` configuration
- **Port blocked**: Telegram only supports 443, 80, 88, 8443
- **Secret mismatch**: `WebhookSecret` in config ≠ header value

### Survey Flow Stuck

**Symptoms**: User answers question, bot doesn't proceed to next question

**Diagnostic Steps**:
1. **Check conversation state**:
   ```csharp
   // In ConversationStateManager, add debug logging
   _logger.LogInformation("State: {State}, QuestionIndex: {Index}, TotalQuestions: {Total}",
       state.CurrentState, state.CurrentQuestionIndex, state.TotalQuestions);
   ```

2. **Verify answer submission**:
   ```bash
   # Check API logs for POST /api/responses/{id}/answers
   docker logs surveybot-api | grep "responses" | grep "answers"
   ```

3. **Check navigation API call**:
   ```bash
   # Look for GET /api/responses/{id}/next-question
   docker logs surveybot-api | grep "next-question"
   ```

4. **Inspect HTTP status codes**:
   ```csharp
   // In SurveyNavigationHelper, log API response
   _logger.LogInformation("Navigation API response: {StatusCode}", response.StatusCode);
   // Should be 200 (next question) or 204 (complete)
   ```

**Common Causes**:
- **API endpoint error**: 4xx/5xx response from `/next-question` endpoint
- **Cycle detected**: User revisited same question, flow terminated
- **State mismatch**: ConversationState out of sync (rare, restart bot)
- **Network timeout**: API unreachable or slow response

### Media Not Displaying

**Symptoms**: Question with media shows text but no image/video/audio

**Diagnostic Steps**:
1. **Check media content exists**:
   ```bash
   # Verify question has MediaContent field
   curl http://localhost:5000/api/surveys/{id}/questions
   # Look for "mediaContent" field with mediaId
   ```

2. **Verify media file in storage**:
   ```bash
   # Check media file exists
   curl http://localhost:5000/api/media/{mediaId}
   # Should return file, not 404
   ```

3. **Check retry logs**:
   ```bash
   # Look for retry attempts in TelegramMediaService
   docker logs surveybot-api | grep "Media send attempt"
   ```

4. **Test Telegram API**:
   ```csharp
   // Verify bot can send media at all
   await _botClient.SendPhotoAsync(chatId, "https://picsum.photos/200");
   // If this fails, issue is with Telegram API or bot permissions
   ```

**Common Causes**:
- **Media file deleted**: File exists in DB but not in storage
- **Network error**: Can't fetch file from storage service
- **Telegram rate limit**: Too many media sends (429 error)
- **Invalid media format**: File corrupted or unsupported format

### Performance Issues

**Symptoms**: Bot slow to respond (> 2 seconds), or users report delays

**Diagnostic Steps**:
1. **Check performance logs**:
   ```bash
   # Look for "Slow operation" warnings
   docker logs surveybot-api | grep "Slow operation"
   ```

2. **Monitor cache hit rate**:
   ```csharp
   // In SurveyCache, log cache statistics
   _logger.LogInformation("Cache hit rate: {HitRate}%", _cacheHitRate * 100);
   ```

3. **Profile database queries**:
   ```bash
   # Enable EF Core query logging
   docker logs surveybot-api | grep "Executed DbCommand"
   ```

4. **Check API response times**:
   ```csharp
   // In SurveyNavigationHelper, log API call duration
   var stopwatch = Stopwatch.StartNew();
   var response = await _httpClient.GetAsync(url);
   _logger.LogInformation("API call took {Duration}ms", stopwatch.ElapsedMilliseconds);
   ```

**Common Causes**:
- **Cache disabled**: Survey cache not working (verify TTL, cleanup task)
- **Database slow**: Large survey list, missing indexes
- **API latency**: Network delay between Bot and API layers
- **N+1 queries**: Loading questions separately instead of with survey

**Solutions**:
- Enable SurveyCache (5-minute TTL)
- Use `.Include(s => s.Questions)` to eager-load questions
- Add database indexes on frequently queried columns
- Increase HttpClient timeout if API is slow
- Monitor with BotPerformanceMonitor (< 2s target)

---

## Integration with API

### HttpClient Configuration

```csharp
services.AddHttpClient<NavigationHandler>()
    .ConfigureHttpClient((sp, client) =>
    {
        var config = sp.GetRequiredService<IOptions<BotConfiguration>>().Value;
        client.BaseAddress = new Uri(config.ApiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(config.RequestTimeout);
    });
```

### API Call Pattern

```csharp
var response = await _httpClient.GetAsync($"/api/surveys/{surveyId}", cancellationToken);
if (!response.IsSuccessStatusCode) { /* handle error */ }

var apiResponse = await response.Content
    .ReadFromJsonAsync<ApiResponse<SurveyDto>>(cancellationToken);
return apiResponse?.Data;
```

### SurveyResponseHandler Flow (UPDATED in v1.4.0)

**Location**: `Handlers/SurveyResponseHandler.cs`

**For Text/Callback Answers**:

```csharp
1. HandleMessageResponseAsync / HandleCallbackResponseAsync
2. Check current state (survey, question, response IDs)
3. Fetch survey with questions (cached)
4. Get question handler for question type
5. Call handler.ProcessAnswerAsync() → Validate & format answer JSON
6. SubmitAnswerAsync() → POST /api/responses/{responseId}/answers
7. RecordVisitedQuestion(questionId) → Cycle prevention
8. GetNextQuestionAsync via SurveyNavigationHelper:
   ├─ Calls GET /api/responses/{responseId}/next-question?currentQuestionId={questionId}
   ├─ API evaluates conditional rules, detects cycles, finds next question
   ├─ Returns QuestionNavigationResult
   └─ Handle result: Complete / Next Question / Error
9. If complete: CompleteSurveyAsync() → POST /api/responses/{responseId}/complete
10. If next: DisplayQuestionAsync() using appropriate handler
```

**Key Features**:
- **Cycle Prevention**: HasVisitedQuestion() check before processing answer
- **Visited Tracking**: RecordVisitedQuestion() after successful answer submission
- **Navigation**: SurveyNavigationHelper queries API for next question
- **Error Handling**: User-friendly messages for API errors
- **Performance**: Survey cached 5 minutes, questions fetched from repository

### Key Endpoints Used

**Survey Management**:
- GET `/api/surveys/{id}` - Fetch survey with questions
- GET `/api/surveys/code/{code}` - Find by code
- GET `/api/surveys/{id}/questions` - Get survey questions (for first question)

**Response Handling**:
- POST `/api/surveys/{id}/responses` - Start survey response
- POST `/api/responses/{id}/answers` - Submit answer
- POST `/api/responses/{id}/complete` - Complete survey
- GET `/api/responses/{responseId}/next-question` - **NEW in v1.4.0**: Get next question based on conditional flow

**Media**:
- GET `/api/media/{mediaId}` - Fetch media file for display (NEW in v1.3.0)

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

## Related Documentation

### Documentation Hub

For comprehensive project documentation, see the **centralized documentation folder**:

**Main Documentation**:
- [Project Root CLAUDE.md](../../CLAUDE.md) - Overall project overview and quick start
- [Documentation Index](../../documentation/INDEX.md) - Complete documentation catalog
- [Navigation Guide](../../documentation/NAVIGATION.md) - Role-based navigation

**Bot Documentation**:
- [Bot User Guide](../../documentation/bot/BOT_USER_GUIDE.md) - Complete user guide
- [Bot Command Reference](../../documentation/bot/BOT_COMMAND_REFERENCE.md) - All available commands
- [Bot FAQ](../../documentation/bot/BOT_FAQ.md) - Frequently asked questions
- [Bot Quick Start](../../documentation/bot/BOT_QUICK_START.md) - User quick start guide
- [Bot Troubleshooting](../../documentation/bot/BOT_TROUBLESHOOTING.md) - Common issues
- [Command Handlers Guide](../../documentation/bot/COMMAND_HANDLERS_GUIDE.md) - Handler implementation
- [State Machine Design](../../documentation/bot/STATE-MACHINE-DESIGN.md) - Conversation state management
- [Integration Guide](../../documentation/bot/INTEGRATION_GUIDE.md) - Bot integration patterns
- [Help Messages](../../documentation/bot/HELP_MESSAGES.md) - Bot messages and text
- [Bot README](../../documentation/bot/README.md) - Bot layer overview

**Related Layer Documentation**:
- [Core Layer](../SurveyBot.Core/CLAUDE.md) - Domain entities and interfaces
- [Infrastructure Layer](../SurveyBot.Infrastructure/CLAUDE.md) - Services used by bot
- [API Layer](../SurveyBot.API/CLAUDE.md) - API endpoints and bot registration

**User Flow Documentation**:
- [Survey Creation Flow](../../documentation/flows/SURVEY_CREATION_FLOW.md) - Creating surveys via bot
- [Survey Taking Flow](../../documentation/flows/SURVEY_TAKING_FLOW.md) - Taking surveys via bot

**Development Resources**:
- [DI Structure](../../documentation/development/DI-STRUCTURE.md) - Dependency injection patterns
- [Developer Onboarding](../../documentation/DEVELOPER_ONBOARDING.md) - Getting started guide
- [Troubleshooting](../../documentation/TROUBLESHOOTING.md) - Common bot issues

### Documentation Maintenance

**When updating Bot layer**:
1. Update this CLAUDE.md file with handler/service changes
2. Update [Bot Command Reference](../../documentation/bot/BOT_COMMAND_REFERENCE.md) if adding/changing commands
3. Update [Command Handlers Guide](../../documentation/bot/COMMAND_HANDLERS_GUIDE.md) if handler patterns change
4. Update [State Machine Design](../../documentation/bot/STATE-MACHINE-DESIGN.md) if conversation flow changes
5. Update [Bot User Guide](../../documentation/bot/BOT_USER_GUIDE.md) if user-facing features change
6. Update [Bot FAQ](../../documentation/bot/BOT_FAQ.md) with common questions
7. Update [Help Messages](../../documentation/bot/HELP_MESSAGES.md) if bot messages change
8. Update [Documentation Index](../../documentation/INDEX.md) if adding significant documentation

**Where to save Bot-related documentation**:
- Technical implementation details → This file
- User-facing documentation → `documentation/bot/`
- Command reference → `documentation/bot/BOT_COMMAND_REFERENCE.md`
- Troubleshooting → `documentation/bot/BOT_TROUBLESHOOTING.md`
- User guides → `documentation/bot/BOT_USER_GUIDE.md`
- Handler patterns → `documentation/bot/COMMAND_HANDLERS_GUIDE.md`
- State management → `documentation/bot/STATE-MACHINE-DESIGN.md`

**User Documentation Updates**:
- After adding new commands, update all user-facing documentation
- After changing bot behavior, update [Bot User Guide](../../documentation/bot/BOT_USER_GUIDE.md)
- After fixing common issues, update [Bot FAQ](../../documentation/bot/BOT_FAQ.md)
- After changing help text, update [Help Messages](../../documentation/bot/HELP_MESSAGES.md)

---

**Last Updated**: 2025-11-25 | **Version**: 1.4.0 (Comprehensive Architecture Documentation Update)
