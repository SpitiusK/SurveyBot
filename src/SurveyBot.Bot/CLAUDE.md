# SurveyBot.Bot - Telegram Bot Layer Documentation

**Last Updated**: 2025-11-12
**Version**: 1.0.0
**Framework**: .NET 8.0
**Telegram.Bot Version**: 22.7.4

---

## Table of Contents
- [Layer Overview](#layer-overview)
- [Architecture & Dependencies](#architecture--dependencies)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [Core Services](#core-services)
- [Update Handling](#update-handling)
- [Command System](#command-system)
- [Question Handlers](#question-handlers)
- [Conversation State Management](#conversation-state-management)
- [Navigation & Flow Control](#navigation--flow-control)
- [Performance & Caching](#performance--caching)
- [Admin Authorization](#admin-authorization)
- [Error Handling](#error-handling)
- [Webhook vs Polling](#webhook-vs-polling)
- [Message Templates & Keyboards](#message-templates--keyboards)
- [Integration with API](#integration-with-api)
- [Testing Strategy](#testing-strategy)
- [Monitoring & Debugging](#monitoring--debugging)
- [Best Practices](#best-practices)

---

## Layer Overview

**SurveyBot.Bot** is the Telegram bot interface layer that handles all user interactions through Telegram's Bot API. This layer provides command-based and conversational interfaces for survey management and participation.

### Purpose

- Receive and process Telegram updates (messages, callbacks, edits)
- Route commands to appropriate handlers
- Manage conversational survey-taking flow
- Handle navigation (back, skip, cancel)
- Display questions with appropriate input methods
- Validate and submit user answers
- Provide admin commands for survey management
- Monitor performance and ensure responsive interactions

### Key Features

- Command-based interface (/start, /help, /surveys, /mysurveys, /stats, etc.)
- Conversational survey taking with state management
- Question type handlers (Text, SingleChoice, MultipleChoice, Rating)
- Navigation controls (Back, Skip, Cancel)
- Inline keyboard interactions
- Session management with timeout
- In-memory caching for performance
- Performance monitoring (< 2 second target response time)
- Admin authorization system
- Both webhook and polling modes

---

## Architecture & Dependencies

### Dependency Flow

```
SurveyBot.Bot
    ├── SurveyBot.Core (Domain Layer)
    │   ├── Entities (User, Survey, Question, Response, Answer)
    │   ├── Interfaces (Repositories, Services)
    │   ├── DTOs (Data Transfer Objects)
    │   └── Exceptions
    └── External Dependencies
        ├── Telegram.Bot 22.7.4 (Telegram Bot API)
        ├── Microsoft.Extensions.DependencyInjection
        ├── Microsoft.Extensions.Logging (Serilog)
        ├── System.Net.Http (API calls)
        └── Newtonsoft.Json (JSON serialization)
```

**Important**: Bot layer depends ONLY on Core. It does NOT depend on Infrastructure or API projects.

### Integration Points

1. **Telegram API**: Receives updates, sends messages, manages bot lifecycle
2. **SurveyBot API**: HTTP calls to backend for data operations
3. **Core DTOs**: Uses SurveyDto, QuestionDto, AnswerDto, etc.
4. **Core Interfaces**: IUserRepository for user registration

---

## Project Structure

```
SurveyBot.Bot/
├── Configuration/
│   └── BotConfiguration.cs                # Bot settings (token, webhook, admin IDs)
│
├── Services/
│   ├── BotService.cs                      # Bot client lifecycle management
│   ├── UpdateHandler.cs                   # Routes updates to handlers
│   ├── CommandRouter.cs                   # Routes commands to command handlers
│   ├── ConversationStateManager.cs        # Manages survey-taking state
│   ├── AdminAuthService.cs                # Admin authorization
│   ├── SurveyCache.cs                     # In-memory survey caching
│   ├── BotPerformanceMonitor.cs           # Performance tracking
│   ├── QuestionErrorHandler.cs            # Question validation errors
│   └── ApiErrorHandler.cs                 # API error handling
│
├── Handlers/
│   ├── Commands/                          # Command handler implementations
│   │   ├── StartCommandHandler.cs         # /start - Welcome & registration
│   │   ├── HelpCommandHandler.cs          # /help - Show commands
│   │   ├── SurveysCommandHandler.cs       # /surveys - Browse surveys
│   │   ├── MySurveysCommandHandler.cs     # /mysurveys - User's surveys
│   │   ├── SurveyCommandHandler.cs        # /survey <code> - Take survey
│   │   ├── StatsCommandHandler.cs         # /stats <id> - Survey statistics (Admin)
│   │   ├── CancelCommandHandler.cs        # /cancel - Cancel survey
│   │   ├── ListSurveysCommandHandler.cs   # /listsurveys - Admin survey list
│   │   ├── CreateSurveyCommandHandler.cs  # /createsurvey - Admin create
│   │   ├── ActivateCommandHandler.cs      # /activate <id> - Admin activate
│   │   ├── DeactivateCommandHandler.cs    # /deactivate <id> - Admin deactivate
│   │   ├── AdminHelpCommandHandler.cs     # /adminhelp - Admin commands
│   │   └── CompletionHandler.cs           # Survey completion
│   │
│   ├── Questions/                         # Question type handlers
│   │   ├── TextQuestionHandler.cs         # Free-form text input
│   │   ├── SingleChoiceQuestionHandler.cs # Radio button selection
│   │   ├── MultipleChoiceQuestionHandler.cs # Checkbox selection
│   │   └── RatingQuestionHandler.cs       # 1-5 star rating
│   │
│   ├── NavigationHandler.cs               # Back/Skip/Next navigation
│   └── CancelCallbackHandler.cs           # Cancel confirmation
│
├── Interfaces/
│   ├── IBotService.cs                     # Bot service contract
│   ├── IUpdateHandler.cs                  # Update handler contract
│   ├── ICommandHandler.cs                 # Command handler interface
│   ├── IQuestionHandler.cs                # Question handler interface
│   ├── IConversationStateManager.cs       # State manager interface
│   ├── IAdminAuthService.cs               # Admin auth interface
│   └── IAnswerValidator.cs                # Answer validation interface
│
├── Models/
│   ├── ConversationState.cs               # User conversation state
│   ├── ApiResponse.cs                     # API response wrapper
│   └── (other models)
│
├── Validators/
│   └── AnswerValidator.cs                 # Answer format validation
│
└── Extensions/
    ├── ServiceCollectionExtensions.cs     # DI registration
    └── BotServiceExtensions.cs            # Bot initialization helpers
```

---

## Configuration

### BotConfiguration

**Location**: `Configuration/BotConfiguration.cs` (Lines 1-122)

Manages all bot-related settings from appsettings.json.

**Properties**:

```csharp
public class BotConfiguration
{
    public const string SectionName = "BotConfiguration";

    public string BotToken { get; set; }              // Telegram bot token from BotFather
    public string WebhookUrl { get; set; }            // Public HTTPS URL for webhook
    public string WebhookPath { get; set; }           // Webhook endpoint path (default: /api/bot/webhook)
    public string WebhookSecret { get; set; }         // Secret token for webhook validation
    public int MaxConnections { get; set; }           // Max concurrent webhook connections (default: 40)
    public string ApiBaseUrl { get; set; }            // SurveyBot API base URL
    public bool UseWebhook { get; set; }              // true=webhook, false=polling
    public string BotUsername { get; set; }           // Bot username (auto-populated)
    public int RequestTimeout { get; set; }           // HTTP request timeout (default: 30s)
    public long[] AdminUserIds { get; set; }          // Telegram user IDs with admin access

    public string FullWebhookUrl => $"{WebhookUrl.TrimEnd('/')}{WebhookPath}";

    public bool IsValid(out List<string> errors) { ... }
}
```

### appsettings.json Configuration

**Development (Polling Mode)**:
```json
{
  "BotConfiguration": {
    "BotToken": "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz",
    "UseWebhook": false,
    "ApiBaseUrl": "http://localhost:5000",
    "AdminUserIds": [123456789, 987654321]
  }
}
```

**Production (Webhook Mode)**:
```json
{
  "BotConfiguration": {
    "BotToken": "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz",
    "WebhookUrl": "https://yourdomain.com",
    "WebhookPath": "/api/bot/webhook",
    "WebhookSecret": "your-secure-random-string-32-chars",
    "UseWebhook": true,
    "ApiBaseUrl": "https://api.yourdomain.com",
    "MaxConnections": 40,
    "AdminUserIds": [123456789]
  }
}
```

### Configuration Validation

The `IsValid()` method (Lines 82-120) validates:
- BotToken is not empty
- If UseWebhook=true: WebhookUrl and WebhookSecret are required
- URLs are valid absolute URIs

---

## Core Services

### BotService

**Location**: `Services/BotService.cs` (Lines 1-202)

**Interface**: `IBotService`

Manages the Telegram bot client lifecycle and provides access to bot functionality.

**Key Responsibilities**:
- Initialize and validate bot token
- Set up/remove webhooks
- Provide access to ITelegramBotClient
- Validate webhook secret tokens

**Key Methods**:

1. **InitializeAsync** (Lines 44-71):
   ```csharp
   public async Task<User> InitializeAsync(CancellationToken cancellationToken = default)
   {
       // Validate bot token by getting bot info from Telegram
       var botInfo = await _botClient.GetMe(cancellationToken);

       _logger.LogInformation(
           "Bot initialized successfully. Bot: @{BotUsername} (ID: {BotId})",
           botInfo.Username,
           botInfo.Id);

       // Update configuration with bot username if not set
       if (string.IsNullOrWhiteSpace(_configuration.BotUsername))
           _configuration.BotUsername = botInfo.Username ?? string.Empty;

       return botInfo;
   }
   ```

2. **SetWebhookAsync** (Lines 74-117):
   - Sets webhook URL with secret token
   - Verifies webhook was set correctly
   - Logs webhook info (URL, pending updates, errors)

3. **RemoveWebhookAsync** (Lines 120-139):
   - Deletes webhook from Telegram
   - Used when switching to polling mode

4. **ValidateWebhookSecret** (Lines 172-200):
   - Validates incoming webhook requests
   - Compares secret token with configured value
   - Returns true if valid, false otherwise

### UpdateHandler

**Location**: `Services/UpdateHandler.cs` (Lines 1-505)

**Interface**: `IUpdateHandler`

Routes incoming Telegram updates to appropriate handlers based on update type.

**Update Flow**:

```
Telegram Update
    ↓
UpdateHandler.HandleUpdateAsync
    ↓
    ├── UpdateType.Message → HandleMessageAsync
    │   ├── Starts with '/' → CommandRouter.RouteCommandAsync
    │   └── Regular text → HandleTextMessageAsync (survey answer)
    │
    ├── UpdateType.CallbackQuery → HandleCallbackQueryAsync
    │   ├── "nav_back_*" → NavigationHandler.HandleBackAsync
    │   ├── "nav_skip_*" → NavigationHandler.HandleSkipAsync
    │   ├── "cancel_*" → CancelCallbackHandler
    │   ├── "cmd:*" → Execute command via CommandRouter
    │   ├── "survey:*" → Survey action callback
    │   ├── "action:*" → Generic action callback
    │   └── "listsurveys:page:*" → Pagination
    │
    ├── UpdateType.EditedMessage → HandleEditedMessageAsync (ignore)
    │
    └── Other → HandleUnsupportedUpdateAsync (log and ignore)
```

**Key Methods**:

1. **HandleUpdateAsync** (Lines 41-78):
   - Wraps update processing with performance monitoring
   - Routes by update type using switch expression
   - Catches and logs exceptions
   - Ensures bot never crashes from update processing

2. **HandleMessageAsync** (Lines 101-123):
   - Checks if message has text
   - Commands (start with '/') → CommandRouter
   - Regular text → Currently sends help message (will handle survey answers in future)

3. **HandleCallbackQueryAsync** (Lines 156-233):
   - Parses callback data format
   - Routes to appropriate handler
   - Always answers callback query (removes loading indicator)
   - Performance monitored (< 100ms target for callback response)

**Callback Data Formats**:

```
Format: "action:parameter1:parameter2:..."

Examples:
- "cmd:help" → Execute /help command
- "cmd:surveys" → Execute /surveys command
- "nav_back_q123" → Go back to question 123
- "nav_skip_q123" → Skip question 123
- "cancel_confirm" → Confirm survey cancellation
- "cancel_dismiss" → Dismiss cancel dialog
- "listsurveys:page:2" → Show page 2 of surveys
- "listsurveys:noop" → No-op (page indicator button)
- "survey:toggle:456" → Toggle survey 456 active status
- "action:create_survey" → Create new survey
```

### CommandRouter

**Location**: `Services/CommandRouter.cs` (Lines 1-205)

Routes command messages to registered command handlers.

**Command Parsing**:

```csharp
// Input: "/start@botname arg1 arg2"
// Parsed command: "start"

// Steps:
1. Strip leading '/' → "start@botname arg1 arg2"
2. Remove bot mention → "start arg1 arg2"
3. Extract command name → "start"
4. Convert to lowercase → "start"
```

**Key Methods**:

1. **RouteCommandAsync** (Lines 45-109):
   - Parses command from message text
   - Looks up handler in dictionary (O(1) lookup)
   - Executes handler if found
   - Sends "Unknown command" if not found

2. **IsCommandRegistered** (Lines 125-131):
   - Checks if command has registered handler
   - Used by UpdateHandler for callback commands

3. **GetAllHandlers** (Lines 115-118):
   - Returns all registered command handlers
   - Used for listing available commands

**Handler Registration**: Handlers are registered in DI container and automatically discovered via `IEnumerable<ICommandHandler>`.

---

## Update Handling

### Update Types & Routing

**Supported Update Types**:

1. **Message** (user sends text/command):
   - Commands: Routed to CommandRouter
   - Text: Handled as survey answer (future implementation)
   - Current: Sends help message for non-command text

2. **CallbackQuery** (user clicks inline keyboard button):
   - Navigation: Back, Skip buttons
   - Commands: Buttons that trigger commands
   - Actions: Survey actions, pagination
   - Always answered to remove loading indicator

3. **EditedMessage** (user edits previous message):
   - Currently ignored (logged only)
   - Could be used to allow editing survey answers

4. **Other types**: Logged and ignored

### Performance Targets

- **Update handling**: < 2 seconds total
- **Callback query answer**: < 100ms (fast visual feedback)
- **Question display**: < 1 second
- **API calls**: < 800ms (warning if slower)

All operations are wrapped with `BotPerformanceMonitor.TrackOperationAsync()`.

---

## Command System

### ICommandHandler Interface

**Location**: `Interfaces/ICommandHandler.cs`

```csharp
public interface ICommandHandler
{
    /// <summary>
    /// Command name (without slash). Example: "start", "help"
    /// </summary>
    string Command { get; }

    /// <summary>
    /// Handles the command execution.
    /// </summary>
    Task HandleAsync(Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Command description for help text.
    /// </summary>
    string GetDescription();
}
```

### Command Handlers

#### StartCommandHandler

**Location**: `Handlers/Commands/StartCommandHandler.cs` (Lines 1-143)

**Command**: `/start`

**Purpose**: Welcome users and register them in the system

**Flow**:
1. Extract user info from message.From
2. Call `IUserRepository.CreateOrUpdateAsync()` to register/update user
3. Determine if new user (CreatedAt == UpdatedAt)
4. Build welcome message (different for new vs returning users)
5. Create main menu inline keyboard
6. Send welcome message with keyboard

**Welcome Message** (Lines 105-125):

New users:
```
Hello, {FirstName}!

Welcome to SurveyBot! I can help you create and manage surveys,
or participate in surveys created by others.

*What would you like to do?*
- Create and manage your own surveys
- Take surveys shared by others
- View results and statistics

Use the buttons below to get started, or type /help to see all available commands.
```

Returning users:
```
Hello, {FirstName}!

Welcome back to SurveyBot!

*What would you like to do today?*
Use the buttons below or type /help for assistance.
```

**Inline Keyboard** (Lines 127-141):
```
[  Find Surveys  ]
[ My Surveys ] [ Help ]
```

#### HelpCommandHandler

**Command**: `/help`

**Purpose**: Display available commands and usage information

**Output**: Lists all commands with descriptions

#### SurveysCommandHandler

**Command**: `/surveys`

**Purpose**: Browse available surveys (active surveys only)

**Features**:
- Fetches active surveys from API
- Displays survey list with metadata
- Inline keyboard with "Take Survey" buttons

#### MySurveysCommandHandler

**Command**: `/mysurveys`

**Purpose**: Show surveys created by the user

**Features**:
- Fetches user's surveys from API
- Shows survey status (Active/Inactive)
- Response count
- Inline keyboard with View/Edit/Stats buttons

#### StatsCommandHandler

**Location**: `Handlers/Commands/StatsCommandHandler.cs` (Lines 1-239)

**Command**: `/stats <survey_id>`

**Purpose**: Display comprehensive survey statistics (Admin only)

**Authorization**: Requires admin privileges (checked via AdminAuthService)

**Usage**:
```
/stats 123
```

**Output Example** (Lines 123-188):
```
📊 *Survey Statistics*

*Customer Satisfaction Survey*
ID: 123
Status: ✅ Active

📈 *Response Stats:*
Total Responses: 145
Completed: 132
Incomplete: 13
Completion Rate: 91.0%
Unique Respondents: 140
Avg. Completion Time: 3m 24s

❓ *Questions:*
Total Questions: 8

📋 *Top Questions by Responses:*
1. How satisfied are you with our service?
   Answers: 132 (91.0%)
2. Would you recommend us?
   Answers: 130 (89.7%)
3. Any additional feedback?
   Answers: 98 (67.6%)

📅 Created: 2025-11-01 10:00
🕐 First Response: 2025-11-01 10:15
🕑 Last Response: 2025-11-12 14:30
```

**Error Handling**:
- SurveyNotFoundException → "Survey not found"
- UnauthorizedAccessException → "You don't have permission"
- Generic Exception → "An error occurred"

#### Admin Commands

**Admin-Only Commands** (require AdminUserIds in config):

1. **/createsurvey**: Create new survey (interactive flow)
2. **/listsurveys [page]**: List all surveys with pagination
3. **/activate <survey_id>**: Activate survey
4. **/deactivate <survey_id>**: Deactivate survey
5. **/stats <survey_id>**: Detailed statistics
6. **/adminhelp**: Show admin commands

**Authorization Check**:
```csharp
if (!_adminAuthService.IsAdmin(telegramUserId))
{
    await SendUnauthorizedMessage(chatId);
    return;
}
```

---

## Question Handlers

### IQuestionHandler Interface

**Location**: `Interfaces/IQuestionHandler.cs`

```csharp
public interface IQuestionHandler
{
    /// <summary>
    /// Question type this handler supports
    /// </summary>
    QuestionType QuestionType { get; }

    /// <summary>
    /// Displays the question to user
    /// </summary>
    Task<int> DisplayQuestionAsync(
        long chatId,
        QuestionDto question,
        int currentIndex,
        int totalQuestions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes user's answer
    /// </summary>
    Task<string?> ProcessAnswerAsync(
        Message? message,
        CallbackQuery? callbackQuery,
        QuestionDto question,
        long userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates answer format
    /// </summary>
    bool ValidateAnswer(string? answerJson, QuestionDto question);
}
```

### TextQuestionHandler

**Location**: `Handlers/Questions/TextQuestionHandler.cs` (Lines 1-195)

**Question Type**: `QuestionType.Text`

**Input Method**: Free-form text message

**Display Format** (Lines 43-77):
```
Question 3 of 10

*What did you like most about our service?*

(Required)

Please type your answer below:

Type /back to go to previous question
```

**Answer Format**:
```json
{"text": "User's typed answer here"}
```

**Validation** (Lines 80-163):
- Required questions: Text cannot be empty
- Maximum length: 4000 characters (Telegram limit)
- Minimum content: At least some text for required questions
- Skip handling: `/skip` command for optional questions only

**Skip Behavior**:
```csharp
if (text.Equals("/skip", StringComparison.OrdinalIgnoreCase))
{
    if (question.IsRequired)
    {
        await ShowError("This question is required and cannot be skipped.");
        return null;
    }
    return JsonSerializer.Serialize(new { text = "" }); // Empty answer
}
```

### SingleChoiceQuestionHandler

**Question Type**: `QuestionType.SingleChoice`

**Input Method**: Inline keyboard with radio button-style options

**Display Format**:
```
Question 2 of 10

*How satisfied are you with our service?*

(Required)

Select one option:

[ Very Satisfied ]
[ Satisfied ]
[ Neutral ]
[ Dissatisfied ]
[ Very Dissatisfied ]

[ ⬅️ Back ]
```

**Answer Format**:
```json
{"selectedOption": "Very Satisfied"}
```

**Keyboard Layout**: One button per option, stacked vertically

### MultipleChoiceQuestionHandler

**Question Type**: `QuestionType.MultipleChoice`

**Input Method**: Inline keyboard with checkbox-style buttons (toggle)

**Display Format**:
```
Question 4 of 10

*Which features do you use most?* (Select all that apply)

(Optional - /skip to skip)

Select one or more options:

[ ☑️ Feature A ]
[ ☐ Feature B ]
[ ☑️ Feature C ]
[ ☐ Feature D ]

[ Submit Selected ] [ ⬅️ Back ] [ Skip ⏭️ ]
```

**Answer Format**:
```json
{"selectedOptions": ["Feature A", "Feature C"]}
```

**Interaction**:
- Click option to toggle selection (☐ ↔ ☑️)
- Message is edited to show updated selections
- "Submit Selected" to finalize answer

### RatingQuestionHandler

**Question Type**: `QuestionType.Rating`

**Input Method**: Inline keyboard with star rating (1-5)

**Display Format**:
```
Question 5 of 10

*Rate your overall experience*

(Required)

Select a rating:

[ ⭐ 1 ] [ ⭐⭐ 2 ] [ ⭐⭐⭐ 3 ] [ ⭐⭐⭐⭐ 4 ] [ ⭐⭐⭐⭐⭐ 5 ]

[ ⬅️ Back ]
```

**Answer Format**:
```json
{"rating": 4}
```

**Validation**: Rating must be between 1 and 5

---

## Conversation State Management

### ConversationStateManager

**Location**: `Services/ConversationStateManager.cs` (Lines 1-544)

Manages conversation state for users taking surveys. Uses in-memory storage with automatic cleanup.

**Key Features**:
- Thread-safe state transitions (`SemaphoreSlim`)
- Automatic session expiration (30 minutes inactivity)
- Progress tracking
- Answer caching before API submission
- Background cleanup timer (runs every 5 minutes)

**Storage**: `ConcurrentDictionary<long, ConversationState>` (userId → state)

**Key Methods**:

1. **GetStateAsync** (Lines 45-63):
   - Retrieves state for user
   - Checks expiration
   - Updates last activity timestamp
   - Returns null if expired or not found

2. **SetStateAsync** (Lines 68-76):
   - Creates or updates state
   - Updates activity timestamp
   - Thread-safe upsert

3. **StartSurveyAsync** (Lines 139-180):
   ```csharp
   public async Task<bool> StartSurveyAsync(
       long userId,
       int surveyId,
       int responseId,
       int totalQuestions)
   {
       var state = new ConversationState
       {
           UserId = userId,
           CurrentState = ConversationStateType.InSurvey,
           CurrentSurveyId = surveyId,
           CurrentResponseId = responseId,
           CurrentQuestionIndex = 0,
           TotalQuestions = totalQuestions
       };

       await SetStateAsync(userId, state);
       return true;
   }
   ```

4. **AnswerQuestionAsync** (Lines 185-215):
   - Validates current question index
   - Caches answer in memory
   - Marks question as answered
   - Updates activity timestamp

5. **NextQuestionAsync** (Lines 220-250):
   - Increments question index
   - Validates not past last question
   - Returns false if already at end

6. **PreviousQuestionAsync** (Lines 255-285):
   - Decrements question index
   - Validates not before first question
   - Used by "Back" button

7. **CompleteSurveyAsync** (Lines 310-329):
   - Transitions to ResponseComplete state
   - Logs completion
   - State can be cleared after showing completion message

8. **CancelSurveyAsync** (Lines 334-355):
   - Transitions to Cancelled state
   - Clears survey data
   - Logs cancellation

**Session Management** (Lines 364-400):

- **CheckSessionTimeoutAsync**: Returns true if expired
- **GetSessionTimeRemainingAsync**: Returns TimeSpan remaining
- **Expiration**: 30 minutes of inactivity
- **Cleanup**: Timer runs every 5 minutes to remove expired states

### ConversationState Model

**Location**: `Models/ConversationState.cs` (Lines 1-241)

Represents the state of a user's conversation with the bot.

**Properties**:

```csharp
public class ConversationState
{
    // Session tracking
    public string SessionId { get; set; }              // Unique session ID (GUID)
    public long UserId { get; set; }                   // Telegram user ID
    public ConversationStateType CurrentState { get; set; }
    public DateTime CreatedAt { get; set; }            // State creation time
    public DateTime LastActivityAt { get; set; }       // Last interaction time

    // Survey progress
    public int? CurrentSurveyId { get; set; }
    public int? CurrentResponseId { get; set; }
    public int? CurrentQuestionIndex { get; set; }     // 0-based index
    public int? TotalQuestions { get; set; }

    // Answer tracking
    public List<int> AnsweredQuestionIndices { get; set; }
    public Dictionary<int, string> CachedAnswers { get; set; }

    // Navigation
    public Stack<ConversationStateType> StateHistory { get; set; }

    // Metadata
    public Dictionary<string, object> Metadata { get; set; }
}
```

**Computed Properties**:

- `IsExpired`: True if > 30 minutes of inactivity
- `ProgressPercent`: (AnsweredCount / TotalQuestions) * 100
- `AnsweredCount`: Number of answered questions
- `IsAllAnswered`: All questions have been answered
- `IsFirstQuestion`: CurrentQuestionIndex == 0
- `IsLastQuestion`: CurrentQuestionIndex == (TotalQuestions - 1)

**Helper Methods**:

- `MarkQuestionAnswered(int index)`: Add to answered list
- `CacheAnswer(int index, string json)`: Store answer locally
- `GetCachedAnswer(int index)`: Retrieve cached answer
- `UpdateActivity()`: Set LastActivityAt = UtcNow
- `TransitionTo(ConversationStateType newState)`: Change state with history
- `ClearSurveyData()`: Reset all survey-related fields
- `Reset()`: Full state reset

### ConversationStateType Enum

**Location**: `Models/ConversationState.cs` (Lines 197-240)

```csharp
public enum ConversationStateType
{
    Idle,                    // No active survey
    WaitingSurveySelection,  // Browsing surveys
    InSurvey,               // Currently taking survey
    AnsweringQuestion,      // Awaiting answer input
    ResponseComplete,       // Survey completed
    SessionExpired,         // Timed out (30 min)
    Cancelled              // User cancelled survey
}
```

**State Transitions**:

```
Idle
  ↓ /survey CODE
InSurvey
  ↓ Display question
AnsweringQuestion
  ↓ Answer submitted
InSurvey (next question)
  ↓ Last question answered
ResponseComplete
  ↓ Show completion message
Idle (clear state)

Cancel flow:
Any state → Cancelled → Idle
Timeout: Any state → SessionExpired → Idle
```

---

## Navigation & Flow Control

### NavigationHandler

**Location**: `Handlers/NavigationHandler.cs` (Lines 1-518)

Manages navigation actions during survey taking (Back, Skip, Next).

**Key Responsibilities**:
- Handle "Back" button clicks
- Handle "Skip" button clicks
- Display previous question with cached answer
- Submit empty answers for skipped questions
- Complete survey when last question is answered

**Key Methods**:

1. **HandleBackAsync** (Lines 67-161):
   ```
   Flow:
   1. Get current state from ConversationStateManager
   2. Validate not on first question
   3. Call stateManager.PreviousQuestionAsync()
   4. Fetch survey with questions (cached)
   5. Get previous question DTO
   6. Display question with cached answer highlighted
   ```

2. **HandleSkipAsync** (Lines 167-282):
   ```
   Flow:
   1. Get current state
   2. Fetch question DTO
   3. Validate question is optional (IsRequired = false)
   4. Create empty answer JSON for question type
   5. Submit empty answer to API
   6. Record in state manager
   7. If not last question:
      - Move to next question
      - Display next question
   8. If last question:
      - Complete survey
   ```

**Empty Answer Formats** (Lines 414-424):

```csharp
private string CreateEmptyAnswerForQuestionType(QuestionType questionType)
{
    return questionType switch
    {
        QuestionType.Text => {"text": ""},
        QuestionType.SingleChoice => {"selectedOption": ""},
        QuestionType.MultipleChoice => {"selectedOptions": []},
        QuestionType.Rating => {"rating": null},
        _ => "{}"
    };
}
```

**Previous Answer Display** (Lines 330-364):

When user goes back, the system displays:
1. The question
2. A message showing their previous answer (if exists)

Example:
```
Question 3 of 10

*What did you like most?*

Your previous answer: "Great customer service and fast delivery"
```

### Survey Taking Flow

```
1. User starts survey (/survey CODE)
   ↓
2. ConversationStateManager.StartSurveyAsync()
   - Creates ConversationState
   - Sets CurrentQuestionIndex = 0
   ↓
3. Display first question
   - Get appropriate QuestionHandler
   - Call DisplayQuestionAsync()
   ↓
4. User provides answer
   - Message or CallbackQuery
   - QuestionHandler.ProcessAnswerAsync()
   - Validates answer
   ↓
5. Submit answer to API
   - POST /api/responses/{responseId}/answers
   ↓
6. Record answer in state
   - ConversationStateManager.AnswerQuestionAsync()
   - Cache answer locally
   ↓
7. Move to next question
   - ConversationStateManager.NextQuestionAsync()
   - If not last: Go to step 3
   - If last: Go to step 8
   ↓
8. Complete survey
   - POST /api/responses/{responseId}/complete
   - Show completion message
   - Clear state
```

**Navigation Options**:

At any question, user can:
- **Answer**: Provide response → Next question
- **Back** (if not first): Go to previous question
- **Skip** (if optional): Skip → Next question
- **Cancel**: /cancel → Confirmation dialog → Cancel survey

---

## Performance & Caching

### BotPerformanceMonitor

**Location**: `Services/BotPerformanceMonitor.cs` (Lines 1-244)

Tracks operation durations and alerts on slow operations.

**Thresholds**:
- **Target response time**: 2000ms
- **Warning threshold**: 800ms (log warning)
- **Slow operation**: 1000ms (log as slow)

**Usage**:

```csharp
var result = await _performanceMonitor.TrackOperationAsync(
    "FetchSurveyData",
    async () => await _httpClient.GetAsync("/api/surveys/123"),
    context: "SurveyId=123"
);
```

**Output**:
```
[Debug] Operation FetchSurveyData [SurveyId=123] completed in 450ms
[Warning] Operation FetchSurveyData [SurveyId=123] completed in 850ms (approaching threshold)
[Warning] SLOW OPERATION: FetchSurveyData [SurveyId=123] completed in 1200ms (threshold: 1000ms)
```

**Metrics Tracked** (Lines 230-243):

```csharp
public class PerformanceMetrics
{
    public string OperationName { get; set; }
    public long TotalCalls { get; set; }
    public long SuccessfulCalls { get; set; }
    public long FailedCalls { get; set; }
    public long TotalDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public DateTime LastCallTimestamp { get; set; }

    public double AverageDurationMs { get; }
    public double SuccessRate { get; }
}
```

### SurveyCache

**Location**: `Services/SurveyCache.cs` (Lines 1-281)

In-memory cache for surveys and survey lists. Reduces API calls and improves response time.

**Features**:
- Thread-safe concurrent storage
- Configurable TTL (default: 5 minutes)
- Automatic expiration cleanup (every 2 minutes)
- Access count tracking
- Hit rate calculation

**Key Methods**:

1. **GetOrAddSurveyAsync** (Lines 47-83):
   ```csharp
   var survey = await _surveyCache.GetOrAddSurveyAsync(
       surveyId: 123,
       factory: async () => await FetchSurveyFromApi(123),
       ttl: TimeSpan.FromMinutes(5)
   );
   ```

   Cache hit: Returns cached survey (logs "Cache HIT")
   Cache miss: Calls factory, caches result, returns survey (logs "Cache MISS")

2. **InvalidateSurvey** (Lines 129-136):
   - Removes survey from cache
   - Called when survey is modified

3. **InvalidateUserSurveys** (Lines 152-157):
   - Removes all cached surveys for a user
   - Called when user creates/modifies surveys

4. **ClearAll** (Lines 171-176):
   - Clears entire cache
   - Used for testing or maintenance

**Cache Statistics** (Lines 181-199):

```csharp
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int SurveyEntries { get; set; }
    public int SurveyListEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public long TotalAccesses { get; set; }
    public double CacheHitRate { get; set; }
}
```

**When to Invalidate**:
- Survey created/updated/deleted → InvalidateSurvey(id)
- Survey activated/deactivated → InvalidateActiveSurveys()
- User's survey list changed → InvalidateUserSurveys(userId)

---

## Admin Authorization

### AdminAuthService

**Location**: `Services/AdminAuthService.cs` (Lines 1-81)

**Interface**: `IAdminAuthService`

Simple whitelist-based authorization using configured Telegram user IDs.

**Configuration**:

```json
{
  "BotConfiguration": {
    "AdminUserIds": [123456789, 987654321]
  }
}
```

**Key Methods**:

1. **IsAdmin** (Lines 38-48):
   ```csharp
   public bool IsAdmin(long telegramUserId)
   {
       var isAdmin = _adminUserIds.Contains(telegramUserId); // O(1) lookup
       _logger.LogDebug("Admin check for user {TelegramUserId}: {IsAdmin}",
           telegramUserId, isAdmin);
       return isAdmin;
   }
   ```

2. **RequireAdmin** (Lines 55-69):
   ```csharp
   public void RequireAdmin(long telegramUserId)
   {
       if (!IsAdmin(telegramUserId))
       {
           _logger.LogWarning("Unauthorized admin access attempt by user {TelegramUserId}",
               telegramUserId);
           throw new UnauthorizedAccessException("This command requires admin privileges.");
       }
   }
   ```

**Usage in Command Handlers**:

```csharp
public async Task HandleAsync(Message message, CancellationToken cancellationToken)
{
    var userId = message.From.Id;

    // Check admin authorization
    if (!_adminAuthService.IsAdmin(userId))
    {
        await _botClient.SendMessage(chatId,
            "This command requires admin privileges.");
        return;
    }

    // Execute admin command...
}
```

**Admin Commands**:
- /createsurvey
- /listsurveys
- /activate
- /deactivate
- /stats
- /adminhelp

---

## Error Handling

### QuestionErrorHandler

**Location**: `Services/QuestionErrorHandler.cs`

Displays validation errors to users in a friendly format.

**Usage**:

```csharp
await _errorHandler.ShowValidationErrorAsync(
    chatId,
    "Your answer is too long. Maximum 4000 characters allowed.",
    cancellationToken
);
```

**Output Format**:
```
❌ *Invalid Answer*

Your answer is too long. Maximum 4000 characters allowed.

Please try again.
```

### ApiErrorHandler

**Location**: `Services/ApiErrorHandler.cs`

Handles errors from API calls.

**Error Categories**:
- 400 Bad Request → Validation error, show message
- 404 Not Found → Survey/question not found
- 500 Internal Server Error → Generic error message
- Network errors → Connection problem message

### Error Handling Patterns

**Command Handler Pattern**:

```csharp
public async Task HandleAsync(Message message, CancellationToken cancellationToken)
{
    try
    {
        // Command logic
    }
    catch (SurveyNotFoundException ex)
    {
        _logger.LogWarning(ex, "Survey not found");
        await SendMessage(chatId, "Survey not found. Please check the ID.");
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Unauthorized access");
        await SendMessage(chatId, "You don't have permission to perform this action.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing command");
        await SendMessage(chatId, "An error occurred. Please try again later.");
    }
}
```

**Graceful Degradation**:

- Never crash the bot from exceptions
- Always log errors with context
- Send user-friendly error messages
- Continue processing other updates

---

## Webhook vs Polling

### Webhook Mode (Production)

**How It Works**:
1. Telegram sends updates to your HTTPS endpoint
2. API receives POST request at `/api/bot/webhook`
3. Validates webhook secret token
4. Queues update for background processing
5. Returns 200 OK immediately (< 100ms)
6. Background worker processes update

**Requirements**:
- Public HTTPS URL with valid SSL certificate
- Ports: 80, 88, 443, or 8443
- Webhook secret for security
- Fast endpoint response (< 5 seconds)

**Configuration**:
```json
{
  "UseWebhook": true,
  "WebhookUrl": "https://yourdomain.com",
  "WebhookPath": "/api/bot/webhook",
  "WebhookSecret": "your-secure-random-string"
}
```

**Setup**:
```csharp
await _botService.SetWebhookAsync();
```

**Advantages**:
- Real-time updates (no polling delay)
- Efficient (no constant API calls)
- Scalable (Telegram pushes to you)
- Required for production

**Verification**:
```bash
curl https://api.telegram.org/bot<TOKEN>/getWebhookInfo
```

### Polling Mode (Development)

**How It Works**:
1. Bot actively polls Telegram API for updates
2. Uses `getUpdates` API method
3. Long polling (waits up to 30 seconds for updates)
4. Processes updates as they arrive
5. Confirms updates to Telegram

**Configuration**:
```json
{
  "UseWebhook": false
}
```

**Setup**:
```csharp
// Remove webhook if it exists
await _botService.RemoveWebhookAsync();

// Start polling (handled by BotController)
```

**Advantages**:
- Easy for local development
- No HTTPS or public URL needed
- Works behind firewall/NAT
- Simple debugging

**Disadvantages**:
- Higher latency (polling interval)
- More resource intensive
- Not suitable for production

### Switching Between Modes

**Development → Production**:
1. Change `UseWebhook: false` → `true`
2. Configure WebhookUrl and WebhookSecret
3. Deploy to HTTPS server
4. Restart application
5. Verify webhook with getWebhookInfo

**Production → Development**:
1. Change `UseWebhook: true` → `false`
2. Restart application (automatically removes webhook)
3. Bot starts polling

---

## Message Templates & Keyboards

### Inline Keyboard Patterns

**Command Buttons**:
```csharp
var keyboard = new InlineKeyboardMarkup(new[]
{
    new[]
    {
        InlineKeyboardButton.WithCallbackData("Help", "cmd:help")
    }
});
```

**Survey List with Pagination**:
```csharp
var buttons = new List<InlineKeyboardButton[]>();

// Survey rows
foreach (var survey in surveys)
{
    buttons.Add(new[]
    {
        InlineKeyboardButton.WithCallbackData(
            $"{survey.Title} ({survey.ResponseCount} responses)",
            $"survey:view:{survey.Id}")
    });
}

// Pagination row
if (hasMultiplePages)
{
    buttons.Add(new[]
    {
        InlineKeyboardButton.WithCallbackData("⬅️ Prev", $"listsurveys:page:{page-1}"),
        InlineKeyboardButton.WithCallbackData($"Page {page}/{totalPages}", "listsurveys:noop"),
        InlineKeyboardButton.WithCallbackData("Next ➡️", $"listsurveys:page:{page+1}")
    });
}

var keyboard = new InlineKeyboardMarkup(buttons);
```

**Navigation Buttons**:
```csharp
var buttons = new List<InlineKeyboardButton[]>();

// Back button (if not first question)
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

var keyboard = new InlineKeyboardMarkup(buttons);
```

**Multiple Choice Selection**:
```csharp
var buttons = new List<InlineKeyboardButton[]>();

// Option rows (toggleable)
foreach (var option in options)
{
    var isSelected = selectedOptions.Contains(option);
    var emoji = isSelected ? "☑️" : "☐";
    buttons.Add(new[]
    {
        InlineKeyboardButton.WithCallbackData(
            $"{emoji} {option}",
            $"toggle:{questionId}:{option}")
    });
}

// Action row
buttons.Add(new[]
{
    InlineKeyboardButton.WithCallbackData("Submit Selected", $"submit:{questionId}"),
    InlineKeyboardButton.WithCallbackData("⬅️ Back", $"nav_back_q{questionId}")
});

var keyboard = new InlineKeyboardMarkup(buttons);
```

### Message Formatting

**Markdown Mode**:
```csharp
await _botClient.SendMessage(
    chatId: chatId,
    text: "*Bold* _italic_ `code` [link](http://example.com)",
    parseMode: ParseMode.Markdown,
    cancellationToken: cancellationToken
);
```

**HTML Mode**:
```csharp
await _botClient.SendMessage(
    chatId: chatId,
    text: "<b>Bold</b> <i>italic</i> <code>code</code> <a href='http://example.com'>link</a>",
    parseMode: ParseMode.Html,
    cancellationToken: cancellationToken
);
```

**Markdown Special Characters**: `*`, `_`, `` ` ``, `[`, `]`, `(`, `)`, `~`, `>`, `#`, `+`, `-`, `=`, `|`, `{`, `}`, `.`, `!`

Escape with `\` if needed in text.

---

## Integration with API

### HttpClient Configuration

**Location**: Configured in DI container

```csharp
services.AddHttpClient<NavigationHandler>()
    .ConfigureHttpClient((sp, client) =>
    {
        var config = sp.GetRequiredService<IOptions<BotConfiguration>>().Value;
        client.BaseAddress = new Uri(config.ApiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(config.RequestTimeout);
    });
```

### API Call Patterns

**Fetch Survey**:
```csharp
var response = await _httpClient.GetAsync($"/api/surveys/{surveyId}", cancellationToken);
if (!response.IsSuccessStatusCode)
{
    _logger.LogWarning("Failed to fetch survey {SurveyId}: {StatusCode}",
        surveyId, response.StatusCode);
    return null;
}

var apiResponse = await response.Content
    .ReadFromJsonAsync<ApiResponse<SurveyDto>>(cancellationToken);
return apiResponse?.Data;
```

**Submit Answer**:
```csharp
var submitDto = new
{
    questionId = questionId,
    answerText = text,
    selectedOptions = options,
    ratingValue = rating
};

var response = await _httpClient.PostAsJsonAsync(
    $"/api/responses/{responseId}/answers",
    submitDto,
    cancellationToken
);

if (!response.IsSuccessStatusCode)
{
    var error = await response.Content.ReadAsStringAsync();
    _logger.LogWarning("Failed to submit answer: {StatusCode} - {Error}",
        response.StatusCode, error);
    return false;
}

return true;
```

**Start Response**:
```csharp
var response = await _httpClient.PostAsync(
    $"/api/surveys/{surveyId}/responses/start",
    null,
    cancellationToken
);

var apiResponse = await response.Content
    .ReadFromJsonAsync<ApiResponse<ResponseDto>>(cancellationToken);
return apiResponse?.Data;
```

**Complete Response**:
```csharp
var response = await _httpClient.PostAsync(
    $"/api/responses/{responseId}/complete",
    null,
    cancellationToken
);
```

### ApiResponse<T> Wrapper

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
```

**Usage**:
```csharp
var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();
if (apiResponse?.Success == true && apiResponse.Data != null)
{
    var survey = apiResponse.Data;
    // Process survey
}
```

---

## Testing Strategy

### Unit Testing Command Handlers

**Mock Dependencies**:

```csharp
public class StartCommandHandlerTests
{
    private readonly Mock<IBotService> _botServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<StartCommandHandler>> _loggerMock;
    private readonly StartCommandHandler _handler;

    public StartCommandHandlerTests()
    {
        _botServiceMock = new Mock<IBotService>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<StartCommandHandler>>();

        _handler = new StartCommandHandler(
            _botServiceMock.Object,
            _userRepositoryMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task HandleAsync_NewUser_RegistersAndSendsWelcome()
    {
        // Arrange
        var message = CreateTestMessage(userId: 123, firstName: "John");
        var user = new User { Id = 1, TelegramId = 123, FirstName = "John" };

        _userRepositoryMock
            .Setup(x => x.CreateOrUpdateAsync(123, null, "John", null))
            .ReturnsAsync(user);

        // Act
        await _handler.HandleAsync(message, CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(
            x => x.CreateOrUpdateAsync(123, null, "John", null),
            Times.Once
        );

        _botServiceMock.Verify(
            x => x.Client.SendMessage(
                It.IsAny<ChatId>(),
                It.Is<string>(s => s.Contains("Welcome to SurveyBot")),
                It.IsAny<ParseMode>(),
                It.IsAny<IEnumerable<MessageEntity>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<IReplyMarkup>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }
}
```

### Integration Testing

**Test Survey Flow**:

```csharp
[Fact]
public async Task SurveyFlow_CompleteJourney_Success()
{
    // Arrange
    var testBot = CreateTestBot();
    var userId = 123L;
    var surveyCode = "ABC123";

    // Act & Assert

    // 1. Start survey
    await SimulateCommand(userId, $"/survey {surveyCode}");
    var state = await _stateManager.GetStateAsync(userId);
    Assert.Equal(ConversationStateType.InSurvey, state.CurrentState);

    // 2. Answer questions
    await SimulateMessage(userId, "My answer to question 1");
    await SimulateCallback(userId, "option_2");
    await SimulateCallback(userId, "rating_4");

    // 3. Complete survey
    state = await _stateManager.GetStateAsync(userId);
    Assert.Equal(ConversationStateType.ResponseComplete, state.CurrentState);

    // 4. Verify API calls
    _httpMock.Verify(x => x.PostAsync(
        It.Is<string>(s => s.Contains("/responses/")),
        It.IsAny<HttpContent>(),
        It.IsAny<CancellationToken>()
    ), Times.AtLeast(3)); // 3 answers submitted
}
```

### Mock Telegram API

Use `Moq` to mock `ITelegramBotClient`:

```csharp
var botMock = new Mock<ITelegramBotClient>();
botMock
    .Setup(x => x.SendMessage(
        It.IsAny<ChatId>(),
        It.IsAny<string>(),
        It.IsAny<ParseMode>(),
        It.IsAny<IEnumerable<MessageEntity>>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<int>(),
        It.IsAny<bool>(),
        It.IsAny<IReplyMarkup>(),
        It.IsAny<CancellationToken>()
    ))
    .ReturnsAsync(new Message { MessageId = 1, Chat = new Chat { Id = 123 } });
```

---

## Monitoring & Debugging

### Logging

**Log Levels**:

- **Trace**: Very detailed debugging (not used in Bot layer)
- **Debug**: Question display, answer processing, cache hits/misses
- **Information**: Command execution, survey start/complete, admin actions
- **Warning**: Slow operations, invalid input, auth failures
- **Error**: Exceptions, API failures, unhandled errors
- **Critical**: Bot initialization failures

**Structured Logging** (Serilog):

```csharp
_logger.LogInformation(
    "User {UserId} started survey {SurveyId} with response {ResponseId}",
    userId,
    surveyId,
    responseId
);
```

**Output**:
```json
{
  "Timestamp": "2025-11-12T14:30:00.123Z",
  "Level": "Information",
  "MessageTemplate": "User {UserId} started survey {SurveyId} with response {ResponseId}",
  "Properties": {
    "UserId": 123,
    "SurveyId": 456,
    "ResponseId": 789
  }
}
```

### Performance Metrics

**Access Metrics**:

```csharp
var metrics = _performanceMonitor.GetMetrics("HandleUpdate");
Console.WriteLine($"Average duration: {metrics.AverageDurationMs}ms");
Console.WriteLine($"Success rate: {metrics.SuccessRate}%");
Console.WriteLine($"Total calls: {metrics.TotalCalls}");
```

**Cache Statistics**:

```csharp
var stats = _surveyCache.GetStatistics();
Console.WriteLine($"Cache entries: {stats.TotalEntries}");
Console.WriteLine($"Hit rate: {stats.CacheHitRate}%");
Console.WriteLine($"Expired entries: {stats.ExpiredEntries}");
```

### Debugging Tips

**Enable Debug Logging**:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "SurveyBot.Bot": "Debug"
      }
    }
  }
}
```

**Test Commands**:

```bash
# Get bot info
curl https://api.telegram.org/bot<TOKEN>/getMe

# Get webhook info
curl https://api.telegram.org/bot<TOKEN>/getWebhookInfo

# Send test message
curl -X POST https://api.telegram.org/bot<TOKEN>/sendMessage \
  -d "chat_id=123&text=Test"

# Get updates (polling)
curl https://api.telegram.org/bot<TOKEN>/getUpdates
```

**Common Issues**:

1. **Bot not responding**: Check bot token, verify InitializeAsync succeeded
2. **Webhook not working**: Verify HTTPS, valid SSL, webhook secret
3. **Commands not working**: Check CommandRouter registration
4. **State lost**: Check session timeout (30 min), verify state manager
5. **API calls failing**: Check ApiBaseUrl, verify API is running

---

## Best Practices

### 1. Command Design

- **Single Responsibility**: One action per command
- **Clear Names**: Use descriptive command names (/start, /surveys, not /s, /srv)
- **Help Text**: Provide usage examples
- **Confirmation**: Ask before destructive actions (cancel survey)
- **Feedback**: Always acknowledge user actions

### 2. State Management

- **Validate State**: Always check state exists and is not expired
- **Update Activity**: Call UpdateActivity() on every interaction
- **Clean Up**: Clear state after survey completion/cancellation
- **Thread Safety**: Use locks for state transitions
- **Timeout Handling**: Show timeout message, suggest restarting

### 3. Performance

- **Cache Aggressively**: Cache surveys, question lists
- **Invalidate Smartly**: Only invalidate what changed
- **Monitor Operations**: Track all slow operations
- **Optimize API Calls**: Batch when possible, use includes
- **Target < 2 seconds**: Total response time for commands

### 4. User Experience

- **Progress Indicators**: Show "Question 3 of 10"
- **Navigation**: Always show Back button (except first question)
- **Validation**: Validate before API call, show clear error messages
- **Confirmation**: "✅ Answer saved" messages
- **Completion**: Thank user, show results if allowed

### 5. Error Handling

- **Never Crash**: Catch all exceptions at top level
- **Log with Context**: Include userId, surveyId, questionId
- **User-Friendly Messages**: No stack traces to users
- **Retry Logic**: For transient API failures
- **Graceful Degradation**: Continue processing other updates

### 6. Security

- **Validate Webhook Secret**: Always check secret token
- **Admin Authorization**: Check AdminUserIds for admin commands
- **Input Validation**: Validate all user input before processing
- **Rate Limiting**: Consider rate limiting for API calls
- **Sanitize Output**: Be careful with user input in messages

### 7. Testing

- **Unit Test Handlers**: Mock all dependencies
- **Integration Test Flows**: Test complete survey journeys
- **Mock Telegram API**: Don't call real API in tests
- **Test Error Paths**: Test exception handling
- **Performance Test**: Measure operation durations

---

## Quick Reference

### Sending Messages

```csharp
// Simple text
await _botClient.SendMessage(chatId, "Hello!");

// With formatting
await _botClient.SendMessage(
    chatId,
    "*Bold* _italic_ `code`",
    parseMode: ParseMode.Markdown
);

// With keyboard
await _botClient.SendMessage(
    chatId,
    "Choose:",
    replyMarkup: keyboard
);

// Edit message
await _botClient.EditMessageText(
    chatId,
    messageId,
    "Updated text"
);

// Delete message
await _botClient.DeleteMessage(chatId, messageId);
```

### Callback Queries

```csharp
// Answer callback (remove loading indicator)
await _botClient.AnswerCallbackQuery(callbackQuery.Id);

// Answer with popup message
await _botClient.AnswerCallbackQuery(
    callbackQuery.Id,
    text: "Action completed!",
    showAlert: true
);
```

### State Management

```csharp
// Start survey
await _stateManager.StartSurveyAsync(userId, surveyId, responseId, totalQuestions);

// Get state
var state = await _stateManager.GetStateAsync(userId);

// Answer question
await _stateManager.AnswerQuestionAsync(userId, questionIndex, answerJson);

// Navigate
await _stateManager.NextQuestionAsync(userId);
await _stateManager.PreviousQuestionAsync(userId);

// Complete/Cancel
await _stateManager.CompleteSurveyAsync(userId);
await _stateManager.CancelSurveyAsync(userId);

// Clear state
await _stateManager.ClearStateAsync(userId);
```

### API Calls

```csharp
// GET survey
var response = await _httpClient.GetAsync($"/api/surveys/{id}");
var survey = await response.Content.ReadFromJsonAsync<ApiResponse<SurveyDto>>();

// POST answer
await _httpClient.PostAsJsonAsync($"/api/responses/{id}/answers", dto);

// POST complete
await _httpClient.PostAsync($"/api/responses/{id}/complete", null);
```

---

## File Line References

Key implementations with line numbers:

- BotConfiguration: `Configuration/BotConfiguration.cs` (Lines 1-122)
- BotService: `Services/BotService.cs` (Lines 1-202)
- UpdateHandler: `Services/UpdateHandler.cs` (Lines 1-505)
- CommandRouter: `Services/CommandRouter.cs` (Lines 1-205)
- ConversationStateManager: `Services/ConversationStateManager.cs` (Lines 1-544)
- ConversationState: `Models/ConversationState.cs` (Lines 1-241)
- StartCommandHandler: `Handlers/Commands/StartCommandHandler.cs` (Lines 1-143)
- StatsCommandHandler: `Handlers/Commands/StatsCommandHandler.cs` (Lines 1-239)
- TextQuestionHandler: `Handlers/Questions/TextQuestionHandler.cs` (Lines 1-195)
- NavigationHandler: `Handlers/NavigationHandler.cs` (Lines 1-518)
- AdminAuthService: `Services/AdminAuthService.cs` (Lines 1-81)
- SurveyCache: `Services/SurveyCache.cs` (Lines 1-281)
- BotPerformanceMonitor: `Services/BotPerformanceMonitor.cs` (Lines 1-244)
- ServiceCollectionExtensions: `Extensions/ServiceCollectionExtensions.cs` (Lines 1-104)

---

**End of SurveyBot.Bot Documentation**

**Key Takeaway**: The Bot layer provides a conversational Telegram interface for survey management. It handles command routing, question display, answer validation, state management, and navigation while maintaining high performance (< 2s response time) and robust error handling.
