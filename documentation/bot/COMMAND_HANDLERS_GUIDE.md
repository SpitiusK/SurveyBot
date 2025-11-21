# Command Handlers Implementation Guide

## Overview

This document describes the bot command handlers implementation for SurveyBot. The command system provides a flexible, extensible architecture for handling Telegram bot commands.

## Architecture

### Components

1. **ICommandHandler** - Interface for command handlers
2. **Command Handlers** - Individual command implementations
3. **CommandRouter** - Routes commands to appropriate handlers
4. **UpdateHandler** - Main update processor for bot
5. **IUpdateHandler** - Interface for update handling

### Flow Diagram

```
Telegram Update → UpdateHandler → CommandRouter → Specific Command Handler
                                ↓
                           Callback Handler
                                ↓
                           Message Handler
```

## Command Handlers

### 1. StartCommandHandler (`/start`)

**Purpose:** Welcome users and register them in the system

**Features:**
- Registers new users automatically
- Updates existing user information
- Displays welcome message with main menu
- Provides inline keyboard for quick actions

**Implementation:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Handlers\Commands\StartCommandHandler.cs`

**User Experience:**
- New users see comprehensive welcome message
- Returning users see "Welcome back" message
- Both receive inline keyboard with quick actions

### 2. HelpCommandHandler (`/help`)

**Purpose:** Display available commands and usage instructions

**Features:**
- Lists all registered commands dynamically
- Shows command descriptions
- Provides usage instructions
- Links to support resources

**Implementation:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Handlers\Commands\HelpCommandHandler.cs`

**User Experience:**
- Clear command list with descriptions
- Organized help text with markdown formatting
- Usage examples and guidance

### 3. MySurveysCommandHandler (`/mysurveys`)

**Purpose:** Display user's created surveys with status and statistics

**Features:**
- Lists all surveys created by user
- Shows survey status (Active/Inactive)
- Displays response count for each survey
- Provides inline keyboard for survey management
- Handles users with no surveys

**Implementation:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Handlers\Commands\MySurveysCommandHandler.cs`

**User Experience:**
- Clear survey list with key information
- Quick action buttons for each survey
- Create new survey option
- Informative message for first-time creators

## Command Router

**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Services\CommandRouter.cs`

**Responsibilities:**
- Parse commands from messages
- Route to appropriate handler
- Handle unknown commands gracefully
- Log all command executions
- Provide error handling

**Features:**
- Case-insensitive command matching
- Handles commands with bot username (@botname)
- Handles commands with parameters
- Dynamic handler registration
- Command validation

## Update Handler

**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Services\UpdateHandler.cs`

**Responsibilities:**
- Process all incoming Telegram updates
- Route updates to appropriate handlers
- Handle different update types (messages, callbacks, etc.)
- Error handling and logging
- Callback query processing

**Supported Update Types:**
- **Message** - Text messages and commands
- **CallbackQuery** - Inline keyboard button clicks
- **EditedMessage** - Message edits (acknowledged but not processed)

**Callback Query Routing:**
- `cmd:command` - Execute command via callback
- `survey:action:id` - Survey-related actions
- `action:name` - Generic actions

## Service Registration

**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Extensions\BotServiceExtensions.cs`

### Registration Method

```csharp
services.AddBotHandlers();
```

This registers:
- All command handlers (Transient)
- CommandRouter (Singleton)
- UpdateHandler (Singleton)

## Usage in API Project

### 1. Add to Program.cs

```csharp
using SurveyBot.Bot.Extensions;

// Add bot handlers
builder.Services.AddBotHandlers();
```

### 2. Use in Controllers

```csharp
public class BotWebhookController : ControllerBase
{
    private readonly IUpdateHandler _updateHandler;

    public BotWebhookController(IUpdateHandler updateHandler)
    {
        _updateHandler = updateHandler;
    }

    [HttpPost]
    public async Task<IActionResult> HandleUpdate([FromBody] Update update)
    {
        await _updateHandler.HandleUpdateAsync(update);
        return Ok();
    }
}
```

### 3. Use for Long Polling

```csharp
var updateHandler = serviceProvider.GetRequiredService<IUpdateHandler>();

botClient.StartReceiving(
    updateHandler: (client, update, ct) => updateHandler.HandleUpdateAsync(update, ct),
    errorHandler: (client, ex, ct) => updateHandler.HandleErrorAsync(ex, ct)
);
```

## Testing Instructions

### Prerequisites

1. **Database Setup**
   - Ensure PostgreSQL is running
   - Database migrations are applied
   - User and Survey tables exist

2. **Bot Configuration**
   - Valid bot token in appsettings.json
   - Bot registered with BotFather
   - Bot username configured (optional)

3. **Dependencies**
   - All NuGet packages installed
   - Project builds successfully
   - Infrastructure layer properly configured

### Manual Testing Steps

#### Test 1: /start Command

1. **First Time User**
   ```
   Action: Send /start to bot
   Expected:
   - User registered in database
   - Welcome message displayed
   - Inline keyboard with "My Surveys" and "Help" buttons
   - Message indicates new user
   ```

2. **Returning User**
   ```
   Action: Send /start again
   Expected:
   - User information updated in database
   - "Welcome back" message displayed
   - Same inline keyboard
   - CreatedAt != UpdatedAt in database
   ```

3. **Database Verification**
   ```sql
   SELECT * FROM "Users" WHERE "TelegramId" = YOUR_TELEGRAM_ID;
   ```
   Expected fields populated:
   - TelegramId
   - Username (if set)
   - FirstName
   - LastName (if set)
   - CreatedAt
   - UpdatedAt

#### Test 2: /help Command

1. **Basic Help**
   ```
   Action: Send /help to bot
   Expected:
   - List of all commands displayed
   - Each command has description
   - Formatted with markdown
   - Usage instructions included
   ```

2. **Help from Inline Keyboard**
   ```
   Action: Click "Help" button from /start response
   Expected:
   - Same help message as /help command
   - Callback query answered (no loading state)
   ```

#### Test 3: /mysurveys Command

1. **User with No Surveys**
   ```
   Action: Send /mysurveys (new user)
   Expected:
   - Message: "You haven't created any surveys yet"
   - "Create Survey" button displayed
   - Friendly, encouraging message
   ```

2. **User with Surveys**
   ```
   Preparation: Create surveys via API for test user
   Action: Send /mysurveys
   Expected:
   - List of all user's surveys
   - Each survey shows:
     * Title (escaped markdown)
     * Status (Active/Inactive with emoji)
     * Response count
     * Created date
   - Inline buttons for survey actions
   - "Create New Survey" button at bottom
   ```

3. **Survey List Ordering**
   ```
   Preparation: Create multiple surveys at different times
   Action: Send /mysurveys
   Expected:
   - Surveys ordered by creation date (newest first)
   - Maximum 5 surveys show action buttons
   ```

#### Test 4: Unknown Commands

1. **Invalid Command**
   ```
   Action: Send /invalid
   Expected:
   - "Unknown command" message
   - Suggestion to use /help
   - Command logged as unknown
   ```

2. **Command with Typo**
   ```
   Action: Send /strart
   Expected:
   - Same as invalid command
   - Graceful error handling
   ```

#### Test 5: Command Variations

1. **Command with Bot Username**
   ```
   Action: Send /start@yourbotname
   Expected:
   - Command processed correctly
   - Same behavior as /start
   ```

2. **Command with Parameters**
   ```
   Action: Send /start param1 param2
   Expected:
   - Command processed (parameters ignored for now)
   - Same behavior as /start
   ```

3. **Case Sensitivity**
   ```
   Action: Send /START or /Start
   Expected:
   - Command processed correctly
   - Case-insensitive matching works
   ```

#### Test 6: Error Handling

1. **Database Connection Error**
   ```
   Preparation: Stop database
   Action: Send /start
   Expected:
   - Error logged
   - User receives friendly error message
   - Bot continues functioning
   ```

2. **API Exception**
   ```
   Preparation: Revoke bot token temporarily
   Action: Send message
   Expected:
   - API exception logged
   - Error handler called
   - Specific error details logged
   ```

#### Test 7: Callback Queries

1. **Help Button**
   ```
   Action: Click "Help" button from /start
   Expected:
   - Help message displayed
   - Callback answered (loading state removed)
   - Same result as /help command
   ```

2. **My Surveys Button**
   ```
   Action: Click "My Surveys" button from /start
   Expected:
   - Survey list displayed
   - Same result as /mysurveys command
   ```

3. **Survey Action Buttons**
   ```
   Action: Click survey action buttons
   Expected:
   - "Coming soon" message
   - Callback answered with alert
   - Logged as not yet implemented
   ```

#### Test 8: Regular Messages

1. **Non-Command Text**
   ```
   Action: Send regular text message
   Expected:
   - Helpful response about using commands
   - Suggestion to use keyboard buttons
   - Message logged
   ```

### Automated Testing

#### Unit Tests (Recommended)

Create unit tests for:

1. **CommandRouter Tests**
   ```csharp
   - ParseCommand_ValidCommand_ReturnsCorrectCommand
   - ParseCommand_WithBotUsername_RemovesUsername
   - ParseCommand_WithParameters_ExtractsCommand
   - RouteCommand_RegisteredCommand_ReturnsTrue
   - RouteCommand_UnknownCommand_SendsHelpMessage
   ```

2. **StartCommandHandler Tests**
   ```csharp
   - Handle_NewUser_CreatesUser
   - Handle_ExistingUser_UpdatesUser
   - Handle_ValidUser_SendsWelcomeMessage
   - Handle_NullFrom_LogsWarning
   ```

3. **UpdateHandler Tests**
   ```csharp
   - HandleUpdate_MessageType_RoutesToMessageHandler
   - HandleUpdate_CallbackQuery_RoutesToCallbackHandler
   - HandleUpdate_Command_RoutesToCommandRouter
   - HandleError_LogsException
   ```

### Integration Tests

1. **Full Flow Test**
   ```csharp
   - User sends /start
   - User created in database
   - User sends /mysurveys
   - Empty survey list returned
   - Create survey via API
   - User sends /mysurveys again
   - Survey appears in list
   ```

### Load Testing

Test bot performance with:
- Multiple simultaneous users
- Rapid command execution
- Large survey lists
- Concurrent database operations

### Monitoring in Production

1. **Logs to Monitor**
   - Command execution times
   - Error rates
   - Unknown command frequency
   - User registration rate
   - Database operation times

2. **Metrics to Track**
   - Commands per minute
   - Active users
   - Error percentage
   - Response times
   - Callback query handling time

## Troubleshooting

### Common Issues

1. **User Not Found**
   - Ensure user sent /start first
   - Check database connection
   - Verify repository implementation

2. **Commands Not Working**
   - Check bot token validity
   - Verify webhook/polling setup
   - Check DI registration
   - Review logs for exceptions

3. **Inline Keyboard Not Appearing**
   - Check Telegram API version
   - Verify keyboard markup format
   - Check for API exceptions in logs

4. **Markdown Formatting Issues**
   - Escape special characters
   - Use EscapeMarkdown helper
   - Test with Telegram markdown tester

## Future Enhancements

1. **Additional Commands**
   - /settings - User preferences
   - /stats - User statistics
   - /about - Bot information

2. **Survey Interaction**
   - Take surveys via bot
   - View results inline
   - Share surveys directly

3. **Admin Commands**
   - /broadcast - Send to all users
   - /stats_admin - System statistics
   - /users - User management

4. **Callback Actions**
   - Implement survey view/edit
   - Toggle survey status
   - Delete surveys
   - View detailed statistics

## Code Quality

### Logging Standards

All handlers should log:
- Command execution start
- Success/failure
- User information
- Execution time (for performance)
- Errors with full context

### Error Handling

- Catch specific exceptions
- Log with appropriate level
- Send user-friendly messages
- Never expose internal details

### Code Style

- Follow C# conventions
- Use meaningful names
- Add XML documentation
- Keep methods focused
- Avoid magic strings

## Support

For issues or questions:
- Check logs first
- Review this guide
- Check Telegram Bot API documentation
- Review code comments
- Test in isolation

## Summary

The command handler system provides a robust, extensible foundation for bot functionality. All commands follow consistent patterns, handle errors gracefully, and provide excellent user experience. The architecture supports easy addition of new commands and features.
