# Bot Commands Reference

Complete reference for all Telegram bot commands in SurveyBot Phase 2.

## Overview

SurveyBot provides an interactive Telegram bot interface for creating, managing, and taking surveys. This document describes all available commands and their usage.

## Command List

| Command | Description | Access |
|---------|-------------|--------|
| `/start` | Start the bot and register user | All users |
| `/help` | Display available commands and usage instructions | All users |
| `/surveys` | Find and browse active surveys | All users |
| `/mysurveys` | View and manage your created surveys | Registered users |

---

## /start

**Purpose:** Welcome new users, register them in the system, and display the main menu.

**Access:** All users

**Usage:**
```
/start
```

### First-Time User Flow

When a new user starts the bot:

1. Bot captures Telegram user information (ID, username, first name, last name)
2. Calls API to register user: `POST /api/auth/register`
3. Stores authentication token for subsequent API calls
4. Displays welcome message with introduction
5. Shows main menu with action buttons

### Returning User Flow

When an existing user starts the bot:

1. Bot updates user information in database
2. Refreshes authentication token
3. Displays welcome back message
4. Shows main menu with action buttons

### Response Examples

**New User:**
```
Hello, John!

Welcome to SurveyBot! I can help you create and manage surveys,
or participate in surveys created by others.

*What would you like to do?*
- Create and manage your own surveys
- Take surveys shared by others
- View results and statistics

Use the buttons below to get started, or type /help to see all available commands.

[Find Surveys]
[My Surveys] [Help]
```

**Returning User:**
```
Hello, John!

Welcome back to SurveyBot!

*What would you like to do today?*
Use the buttons below or type /help for assistance.

[Find Surveys]
[My Surveys] [Help]
```

### Inline Keyboard Buttons

- **Find Surveys** - Opens `/surveys` command to browse active surveys
- **My Surveys** - Opens `/mysurveys` command to manage your surveys
- **Help** - Opens `/help` command to show available commands

### Technical Details

- **Handler:** `StartCommandHandler.cs`
- **API Endpoint:** `POST /api/auth/register`
- **Database Action:** Create or update user record
- **Stores:** JWT token for authenticated API requests

### Error Handling

If registration fails:
```
Sorry, an error occurred while processing your request. Please try again later.
```

---

## /help

**Purpose:** Display list of available commands and usage instructions.

**Access:** All users

**Usage:**
```
/help
```

### Response

```
*SurveyBot - Available Commands*

Here are all the commands you can use:

/mysurveys - View and manage your surveys
/start - Start the bot and display main menu
/surveys - Find and take available surveys
/help - Show available commands and usage instructions

*How to use SurveyBot:*

1. *Create Surveys*
   Use /mysurveys to manage your surveys. You can create new surveys, edit existing ones, and view results.

2. *Take Surveys*
   When someone shares a survey link with you, simply click it to start taking the survey.

3. *View Results*
   Survey creators can view detailed statistics and responses for their surveys.

*Need more help?*
Contact support or visit our documentation for detailed guides.

_SurveyBot - Making surveys simple and fun!_
```

### Features

- Lists all available commands with descriptions
- Provides usage instructions for main features
- Offers guidance on creating and taking surveys
- Directs users to additional help resources

### Technical Details

- **Handler:** `HelpCommandHandler.cs`
- **API Endpoint:** None (static content)
- **Dynamic Content:** Command list is generated from registered command handlers

---

## /surveys

**Purpose:** Find and browse all active surveys available to take.

**Access:** All users

**Usage:**
```
/surveys
```

### Response - With Active Surveys

```
*Available Surveys*

Found 12 active surveys (Page 1 of 3):

1. *Customer Satisfaction Survey*
   Help us improve our service
   Questions: 5
   Responses: 42
   Updated: Nov 07, 2025

2. *Product Feedback*
   Share your thoughts on our new product
   Questions: 8
   Responses: 156
   Updated: Nov 06, 2025

3. *Employee Engagement Survey*
   Annual engagement survey
   Questions: 12
   Responses: 89
   Updated: Nov 05, 2025

_Click on a survey below to start taking it._

[📋 Customer Satisfaction Survey]
[📋 Product Feedback]
[📋 Employee Engagement Survey]
[◀️ Previous] [1/3] [Next ▶️]
[🔄 Refresh] [📊 My Surveys]
```

### Response - No Active Surveys

```
*Available Surveys*

There are no active surveys available at the moment.

Check back later or create your own survey!

[My Surveys] [Help]
```

### Features

#### Pagination
- Shows 5 surveys per page
- Navigation buttons for previous/next pages
- Page indicator shows current page and total pages

#### Survey Information
- Title and description
- Number of questions
- Number of responses received
- Last updated date

#### Inline Keyboard Buttons
- **Survey Buttons** - Click to start taking the survey
- **Previous/Next** - Navigate between pages
- **Refresh** - Reload survey list
- **My Surveys** - Jump to your surveys

### Callback Actions

When user clicks a survey button:
```
Callback: survey:take:<survey_id>
Action: Start survey taking flow
```

When user clicks pagination:
```
Callback: surveys:page:<page_number>
Action: Show specified page of surveys
```

### Technical Details

- **Handler:** `SurveysCommandHandler.cs`
- **API Endpoint:** `ISurveyRepository.GetActiveSurveysAsync()`
- **Pagination:** 5 surveys per page
- **Sorting:** By last updated date (newest first)

### Special Markdown Handling

Survey titles and descriptions are escaped to prevent Markdown injection:
- Special characters are escaped: `_`, `*`, `[`, `]`, `(`, `)`, etc.
- Long descriptions are truncated to 100 characters

---

## /mysurveys

**Purpose:** View and manage surveys created by the user.

**Access:** Registered users only

**Usage:**
```
/mysurveys
```

### Response - With Surveys

```
*My Surveys*

You have created 3 surveys:

1. *Customer Satisfaction Survey*
   ✅ Status: Active
   📊 Responses: 42
   📅 Created: Nov 07, 2025

2. *Product Feedback*
   🔴 Status: Inactive
   📊 Responses: 0
   📅 Created: Nov 05, 2025

3. *Team Building Ideas*
   ✅ Status: Active
   📊 Responses: 23
   📅 Created: Nov 03, 2025

_Use the buttons below to manage your surveys._

[📋 Customer Satisfaction Survey] [⏸️]
[📋 Product Feedback] [▶️]
[📋 Team Building Ideas] [⏸️]
[➕ Create New Survey]
```

### Response - No Surveys

```
*My Surveys*

You haven't created any surveys yet.

Ready to create your first survey?
Use the button below or create one through the web interface.

[Create Survey]
```

### Features

#### Survey Status
- **✅ Active** - Survey is accepting responses
- **🔴 Inactive** - Survey is not accepting responses

#### Survey Information
- Title
- Status (Active/Inactive)
- Response count
- Creation date

#### Inline Keyboard Buttons
- **Survey Button** - View survey details and statistics
- **▶️ Play Button** - Activate inactive survey
- **⏸️ Pause Button** - Deactivate active survey
- **Create New Survey** - Start survey creation flow

### Callback Actions

When user clicks survey details:
```
Callback: survey:view:<survey_id>
Action: Display survey details, questions, and statistics
```

When user toggles survey status:
```
Callback: survey:toggle:<survey_id>
Action: Activate or deactivate survey
```

When user creates new survey:
```
Callback: action:create_survey
Action: Start survey creation flow (future implementation)
```

### Button Limits

- Shows first 5 surveys with action buttons
- This prevents keyboard from exceeding Telegram's size limits
- Full list is shown in text, but only first 5 have interactive buttons

### Technical Details

- **Handler:** `MySurveysCommandHandler.cs`
- **API Endpoints:**
  - `IUserRepository.GetByTelegramIdAsync()` - Get user
  - `ISurveyRepository.GetByCreatorIdAsync()` - Get user's surveys
  - `ISurveyRepository.GetResponseCountAsync()` - Get response counts
- **Sorting:** By creation date (newest first)

### Error Handling

If user not registered:
```
You are not registered yet. Please use /start to register.
```

If error retrieving surveys:
```
Sorry, an error occurred while retrieving your surveys. Please try again later.
```

---

## Inline Keyboard Navigation

### Button Callback Format

All inline keyboard buttons use callback data in the format:
```
<action>:<subaction>:<parameter>
```

### Command Callbacks

```
cmd:surveys      - Execute /surveys command
cmd:mysurveys    - Execute /mysurveys command
cmd:help         - Execute /help command
```

### Survey Action Callbacks

```
survey:take:<id>      - Start taking survey with given ID
survey:view:<id>      - View survey details and statistics
survey:toggle:<id>    - Toggle survey active status
```

### Pagination Callbacks

```
surveys:page:<number>  - Navigate to specific page in /surveys
surveys:noop          - No-op (used for page indicator button)
```

### Other Action Callbacks

```
action:create_survey  - Start survey creation flow
```

---

## Bot Behavior

### Message Types

#### Text Messages
- Commands are processed when they start with `/`
- Non-command text may trigger help message or be ignored

#### Callback Queries
- Inline keyboard button clicks send callback queries
- Bot processes callback and updates message accordingly

### Response Patterns

#### Immediate Response
- Bot acknowledges command within 1-2 seconds
- Shows typing indicator while processing

#### Error Messages
- Clear, user-friendly error descriptions
- Suggestions for resolution
- Option to retry or get help

#### Success Messages
- Confirmation of successful actions
- Next steps or available actions
- Relevant inline keyboard buttons

---

## Future Commands (Planned)

These commands are planned for future phases:

### /createsurvey
Create a new survey through conversational flow

### /responses {survey_id}
View responses for a specific survey

### /share {survey_id}
Generate shareable link for survey

### /settings
Configure user preferences and notifications

### /export {survey_id}
Export survey responses as CSV

---

## Bot Configuration

### Environment Variables

```env
TELEGRAM_BOT_TOKEN=your_bot_token_here
TELEGRAM_BOT_USERNAME=YourBotUsername
```

### Webhook vs Polling

**Phase 2 MVP:**
- Uses long polling for simplicity
- Suitable for development and testing

**Future Phases:**
- Webhook for production deployment
- Better performance and scalability

---

## Command Implementation Details

### Command Handler Interface

All commands implement `ICommandHandler`:

```csharp
public interface ICommandHandler
{
    string Command { get; }
    Task HandleAsync(Message message, CancellationToken cancellationToken);
    string GetDescription();
}
```

### Command Registration

Commands are automatically registered via dependency injection:

```csharp
services.AddScoped<ICommandHandler, StartCommandHandler>();
services.AddScoped<ICommandHandler, HelpCommandHandler>();
services.AddScoped<ICommandHandler, SurveysCommandHandler>();
services.AddScoped<ICommandHandler, MySurveysCommandHandler>();
```

### Command Routing

`CommandRouter` service routes incoming commands to appropriate handlers:

```csharp
public async Task RouteCommandAsync(Message message)
{
    var commandText = message.Text?.TrimStart('/').Split(' ')[0];
    var handler = _handlers.FirstOrDefault(h =>
        h.Command.Equals(commandText, StringComparison.OrdinalIgnoreCase));

    if (handler != null)
        await handler.HandleAsync(message);
    else
        await SendUnknownCommandMessage(message);
}
```

---

## Testing Bot Commands

### BotFather Setup

1. Create bot with BotFather: `/newbot`
2. Get bot token
3. Set commands list: `/setcommands`
```
start - Start the bot and display main menu
help - Show available commands
surveys - Find and take available surveys
mysurveys - View and manage your surveys
```

### Manual Testing

1. Start conversation with bot
2. Send `/start` - Verify registration
3. Send `/help` - Verify command list
4. Send `/surveys` - Verify survey list
5. Send `/mysurveys` - Verify user's surveys
6. Click inline buttons - Verify callbacks work

### Testing with Telegram Bot API

```bash
# Send command via API
curl -X POST "https://api.telegram.org/bot<TOKEN>/sendMessage" \
  -H "Content-Type: application/json" \
  -d '{
    "chat_id": 123456789,
    "text": "/start"
  }'
```

---

## Best Practices

### For Users

1. **Start with /start** - Always begin by registering with the bot
2. **Use buttons** - Inline keyboard buttons are faster than typing commands
3. **Check /help** - Review available commands and usage instructions
4. **Report issues** - Contact support if commands don't work as expected

### For Developers

1. **Handle errors gracefully** - Always catch exceptions and show user-friendly messages
2. **Log interactions** - Log all commands for debugging and analytics
3. **Validate input** - Always validate data before API calls
4. **Test thoroughly** - Test all commands and callback actions
5. **Keep messages concise** - Telegram messages should be short and clear
6. **Escape Markdown** - Always escape special characters in user-generated content

---

## Troubleshooting

### Command Not Responding

**Symptoms:** Bot doesn't respond to commands

**Solutions:**
- Verify bot token is correct
- Check bot service is running
- Verify webhook/polling is active
- Check API is accessible
- Review logs for errors

### Registration Fails

**Symptoms:** /start command shows error message

**Solutions:**
- Verify API is running
- Check database connection
- Verify JWT configuration
- Review AuthService logs

### Inline Buttons Not Working

**Symptoms:** Clicking buttons does nothing

**Solutions:**
- Verify callback handler is registered
- Check callback data format
- Review UpdateHandler logs
- Verify API endpoints are accessible

### Surveys Not Showing

**Symptoms:** /surveys shows no surveys despite having active surveys

**Solutions:**
- Verify surveys are marked as active in database
- Check ISurveyRepository implementation
- Review database connection
- Check survey service logs

---

## Quick Reference Card

```
╔════════════════════════════════════════════════════════════╗
║              SURVEYBOT COMMAND REFERENCE                   ║
╠════════════════════════════════════════════════════════════╣
║  /start       - Register and show main menu                ║
║  /help        - Show this command list                     ║
║  /surveys     - Browse and take surveys                    ║
║  /mysurveys   - Manage your surveys                        ║
╠════════════════════════════════════════════════════════════╣
║  TIPS:                                                     ║
║  • Use inline buttons for faster navigation               ║
║  • Check /help for detailed usage instructions            ║
║  • Create surveys via web interface or future bot flow    ║
╚════════════════════════════════════════════════════════════╝
```

---

## Additional Resources

- **API Documentation:** See `PHASE2_API_REFERENCE.md`
- **Authentication Flow:** See `AUTHENTICATION_FLOW.md`
- **Survey Taking Flow:** See `SURVEY_TAKING_FLOW.md`
- **Survey Creation Flow:** See `SURVEY_CREATION_FLOW.md`
- **Troubleshooting Guide:** See `PHASE2_TROUBLESHOOTING.md`
