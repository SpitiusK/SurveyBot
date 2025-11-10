# SurveyBot Command Reference

Complete reference for all Telegram bot commands available in SurveyBot.

## Command Overview

| Command | Description | Access Level | Usage |
|---------|-------------|--------------|-------|
| `/start` | Register and show main menu | All users | `/start` |
| `/help` | Display available commands | All users | `/help` |
| `/surveys` | Browse active surveys | All users | `/surveys` |
| `/mysurveys` | View and manage your surveys | Registered users | `/mysurveys` |

---

## /start

### Description
Start the bot, register user account, and display the main menu.

### Access Level
All users (including first-time users)

### Syntax
```
/start
```

### What It Does

**For New Users:**
1. Creates user account in the system
2. Captures Telegram user information:
   - Telegram ID
   - Username
   - First name
   - Last name
3. Displays welcome message
4. Shows main menu with action buttons

**For Returning Users:**
1. Updates user information (name changes, etc.)
2. Refreshes last login timestamp
3. Displays welcome back message
4. Shows main menu

### Example Output

**New User Response:**
```
Hello, John!

Welcome to SurveyBot! I can help you create and manage surveys,
or participate in surveys created by others.

What would you like to do?
- Create and manage your own surveys
- Take surveys shared by others
- View results and statistics

Use the buttons below to get started, or type /help to see all available commands.

[Find Surveys]
[My Surveys] [Help]
```

**Returning User Response:**
```
Hello, John!

Welcome back to SurveyBot!

What would you like to do today?
Use the buttons below or type /help for assistance.

[Find Surveys]
[My Surveys] [Help]
```

### Inline Buttons

- **Find Surveys** - Opens `/surveys` command
- **My Surveys** - Opens `/mysurveys` command
- **Help** - Opens `/help` command

### When to Use
- First time using the bot
- Need to register or re-register
- Want to return to main menu
- Reset conversation state

### Technical Details
- **Handler:** `StartCommandHandler.cs`
- **Repository:** `IUserRepository.CreateOrUpdateAsync()`
- **Side Effects:** Creates or updates user record in database
- **Response Time:** Typically under 1 second

### Error Handling

**If registration fails:**
```
Sorry, an error occurred while processing your request. Please try again later.
```

**Common Issues:**
- Database connection error
- Network timeout
- Invalid Telegram user data

---

## /help

### Description
Display list of all available commands with descriptions and usage instructions.

### Access Level
All users

### Syntax
```
/help
```

### What It Does
1. Lists all available bot commands
2. Provides description for each command
3. Shows usage instructions for main features
4. Directs to additional resources

### Example Output

```
SurveyBot - Available Commands

Here are all the commands you can use:

/mysurveys - View and manage your surveys
/start - Start the bot and display main menu
/surveys - Find and take available surveys
/help - Show available commands and usage instructions

How to use SurveyBot:

1. Create Surveys
   Use /mysurveys to manage your surveys. You can create new surveys,
   edit existing ones, and view results.

2. Take Surveys
   When someone shares a survey link with you, simply click it to start
   taking the survey.

3. View Results
   Survey creators can view detailed statistics and responses for their surveys.

Need more help?
Contact support or visit our documentation for detailed guides.

SurveyBot - Making surveys simple and fun!
```

### Sections Included
1. **Command List** - All available commands with brief descriptions
2. **How to Create Surveys** - Guide to survey management
3. **How to Take Surveys** - Guide to participating in surveys
4. **How to View Results** - Accessing statistics
5. **Additional Help** - Support and documentation links

### When to Use
- First time using the bot
- Forgot available commands
- Need usage instructions
- Want to learn about features

### Technical Details
- **Handler:** `HelpCommandHandler.cs`
- **Data Source:** Dynamically generated from registered command handlers
- **Static Content:** Usage instructions and help text
- **Response Time:** Instant (no API calls)

### Related Commands
- `/start` - Return to main menu
- All other commands - Use help to learn about them

---

## /surveys

### Description
Browse and find all active surveys available to take.

### Access Level
All users (including new users)

### Syntax
```
/surveys
```

### What It Does
1. Fetches all active surveys from database
2. Displays surveys with details (title, description, question count, response count)
3. Paginates results (5 surveys per page)
4. Provides buttons to start taking surveys
5. Shows navigation controls

### Example Output

**With Active Surveys:**
```
Available Surveys

Found 12 active surveys (Page 1 of 3):

1. Customer Satisfaction Survey
   Help us improve our service
   Questions: 5
   Responses: 42
   Updated: Nov 07, 2025

2. Product Feedback
   Share your thoughts on our new product
   Questions: 8
   Responses: 156
   Updated: Nov 06, 2025

3. Employee Engagement Survey
   Annual engagement survey for team members
   Questions: 12
   Responses: 89
   Updated: Nov 05, 2025

4. New Feature Ideas
   What features would you like to see?
   Questions: 3
   Responses: 234
   Updated: Nov 04, 2025

5. Training Needs Assessment
   Help us plan future training sessions
   Questions: 7
   Responses: 67
   Updated: Nov 03, 2025

Click on a survey below to start taking it.

[📋 Customer Satisfaction Survey]
[📋 Product Feedback]
[📋 Employee Engagement Survey]
[📋 New Feature Ideas]
[📋 Training Needs Assessment]
[◀️ Previous] [1/3] [Next ▶️]
[🔄 Refresh] [📊 My Surveys]
```

**No Active Surveys:**
```
Available Surveys

There are no active surveys available at the moment.

Check back later or create your own survey!

[My Surveys] [Help]
```

### Survey Information Displayed

For each survey:
- **Position number** - Numbered list position (1, 2, 3...)
- **Title** - Survey name (bold)
- **Description** - Brief explanation (truncated to 100 chars if longer)
- **Questions** - Number of questions in survey
- **Responses** - How many users have completed it
- **Updated** - Last update date (format: MMM dd, yyyy)

### Inline Buttons

**Survey Buttons:**
- One button per survey showing survey title
- Click to view survey introduction and start taking it
- Callback data: `survey:take:<survey_id>`

**Pagination Buttons:**
- **◀️ Previous** - Go to previous page (only if not on first page)
- **Page Indicator** - Shows current/total pages (e.g., "1/3")
- **Next ▶️** - Go to next page (only if not on last page)

**Action Buttons:**
- **🔄 Refresh** - Reload survey list (re-runs `/surveys` command)
- **📊 My Surveys** - Jump to `/mysurveys` command

### Features

**Pagination:**
- Shows 5 surveys per page
- Automatic page calculation
- Navigation buttons only show when needed
- Page indicator always visible when multiple pages

**Sorting:**
- Sorted by last updated date (newest first)
- Ensures users see most recent surveys

**Markdown Escaping:**
- Special characters in titles/descriptions are escaped
- Prevents Markdown injection
- Ensures proper display

**Real-time Data:**
- Response counts fetched from database
- Shows current data on each load

### When to Use
- Want to find surveys to take
- Browse available surveys
- See what surveys are active
- Find specific survey by name

### Navigation Flow

```
User: /surveys
Bot: Shows survey list with buttons

User: Clicks survey button
Bot: Shows survey introduction

User: Clicks "Start Survey"
Bot: Begins question flow

User: Answers all questions
Bot: Shows completion message

User: Clicks "Back to Surveys"
Bot: Returns to /surveys list
```

### Technical Details
- **Handler:** `SurveysCommandHandler.cs`
- **Repository:** `ISurveyRepository.GetActiveSurveysAsync()`
- **Pagination:** 5 surveys per page (configurable)
- **Sorting:** By `UpdatedAt` descending
- **Response Time:** Under 2 seconds (includes database queries)

### Error Handling

**If error loading surveys:**
```
Sorry, an error occurred while retrieving surveys. Please try again later.
```

**Common Issues:**
- Database connection error
- Network timeout
- Invalid survey data

### Related Commands
- `/mysurveys` - View your created surveys
- `/start` - Return to main menu
- `/help` - Get help with commands

---

## /mysurveys

### Description
View and manage surveys you have created.

### Access Level
Registered users only (must have used `/start` first)

### Syntax
```
/mysurveys
```

### What It Does
1. Finds user account by Telegram ID
2. Fetches all surveys created by user
3. Retrieves response counts for each survey
4. Displays survey list with management options
5. Provides buttons for survey actions

### Example Output

**With Surveys:**
```
My Surveys

You have created 3 surveys:

1. Customer Satisfaction Survey
   ✅ Status: Active
   📊 Responses: 42
   📅 Created: Nov 07, 2025

2. Product Feedback
   🔴 Status: Inactive
   📊 Responses: 0
   📅 Created: Nov 05, 2025

3. Team Building Ideas
   ✅ Status: Active
   📊 Responses: 23
   📅 Created: Nov 03, 2025

Use the buttons below to manage your surveys.

[📋 Customer Satisfaction Survey] [⏸️]
[📋 Product Feedback] [▶️]
[📋 Team Building Ideas] [⏸️]
[➕ Create New Survey]
```

**No Surveys:**
```
My Surveys

You haven't created any surveys yet.

Ready to create your first survey?
Use the button below or create one through the web interface.

[Create Survey]
```

### Survey Information Displayed

For each survey:
- **Position number** - Numbered list position (1, 2, 3...)
- **Title** - Survey name (bold)
- **Status** - Active (✅) or Inactive (🔴)
- **Responses** - Number of completed responses
- **Created** - Survey creation date (format: MMM dd, yyyy)

### Status Indicators

- **✅ Active** - Survey is accepting responses from users
- **🔴 Inactive** - Survey is paused and not accepting new responses

### Inline Buttons

**Survey Action Buttons:**
- **📋 Survey Name** - View details and statistics
  - Callback data: `survey:view:<survey_id>`
- **▶️ Play** - Activate inactive survey
  - Only shown for inactive surveys
  - Callback data: `survey:toggle:<survey_id>`
- **⏸️ Pause** - Deactivate active survey
  - Only shown for active surveys
  - Callback data: `survey:toggle:<survey_id>`

**Create Button:**
- **➕ Create New Survey** - Start survey creation flow
  - Callback data: `action:create_survey`
  - Future feature: conversational survey creation

### Features

**Survey Management:**
- View all your created surveys
- See real-time response counts
- Toggle survey active/inactive status
- Quick access to survey details

**Limited Button Display:**
- Shows action buttons for first 5 surveys only
- Prevents exceeding Telegram's inline keyboard size limits
- All surveys shown in text list
- Most recent 5 get interactive buttons

**Sorting:**
- Sorted by creation date (newest first)
- Your most recent surveys appear at the top

### When to Use
- Want to see surveys you created
- Need to activate/deactivate a survey
- View response count for your surveys
- Access survey management features

### Survey Actions Explained

**View Survey Details:**
- Click survey name button
- See full survey information:
  - All questions
  - Individual responses
  - Aggregated statistics
  - Response charts and graphs

**Activate Survey:**
- Click **▶️** button next to inactive survey
- Survey becomes active and accepts responses
- Status changes to ✅ Active
- Confirmation message shown

**Deactivate Survey:**
- Click **⏸️** button next to active survey
- Survey stops accepting new responses
- Existing responses preserved
- Status changes to 🔴 Inactive
- Confirmation message shown

**Create New Survey:**
- Click **➕ Create New Survey** button
- Opens survey creation flow (future bot feature)
- Or redirects to web interface
- Guide through title, questions, settings

### Technical Details
- **Handler:** `MySurveysCommandHandler.cs`
- **Repositories:**
  - `IUserRepository.GetByTelegramIdAsync()` - Find user
  - `ISurveyRepository.GetByCreatorIdAsync()` - Get user's surveys
  - `ISurveyRepository.GetResponseCountAsync()` - Get response counts
- **Sorting:** By `CreatedAt` descending
- **Response Time:** Under 2 seconds

### Error Handling

**If user not registered:**
```
You are not registered yet. Please use /start to register.
```

**If error retrieving surveys:**
```
Sorry, an error occurred while retrieving your surveys. Please try again later.
```

**Common Issues:**
- User not found in database
- Database connection error
- Network timeout

### Related Commands
- `/surveys` - Browse surveys to take
- `/start` - Register or return to main menu
- `/help` - Get help with commands

### Survey Creation Flow

**Current (MVP):**
- Create surveys via web interface
- Manage via Telegram bot

**Future:**
- Full conversational creation in Telegram
- Step-by-step question builder
- Preview before publishing
- Share directly from bot

---

## Future Commands (Planned)

These commands are planned for future releases:

### /cancel

**Purpose:** Cancel current survey in progress

**Syntax:** `/cancel`

**Description:** Stop taking current survey and return to main menu. Progress will not be saved.

---

### /createsurvey

**Purpose:** Create new survey through conversational flow

**Syntax:** `/createsurvey`

**Description:** Step-by-step guided survey creation directly in Telegram. Bot asks for title, description, questions, and settings.

**Flow:**
1. Ask for survey title
2. Ask for description (optional)
3. Add questions one by one
4. For each question:
   - Question text
   - Question type (text, choice, rating)
   - Required or optional
   - Options (for choice questions)
5. Review and publish

---

### /stats [survey_code]

**Purpose:** View detailed statistics for a survey

**Syntax:** `/stats ABC123`

**Description:** Display comprehensive analytics for specified survey. Shows response rate, answer distribution, charts, and trends.

**Access:** Survey creators only

---

### /share [survey_code]

**Purpose:** Generate shareable link for survey

**Syntax:** `/share ABC123`

**Description:** Create a deep link that starts survey directly when clicked. Easy sharing via Telegram, email, or social media.

**Example Link:** `https://t.me/YourBot?start=survey_ABC123`

---

### /responses [survey_code]

**Purpose:** View individual responses for a survey

**Syntax:** `/responses ABC123`

**Description:** Browse all responses to survey. See individual user answers, export data, filter by date.

**Access:** Survey creators only

---

### /export [survey_code] [format]

**Purpose:** Export survey responses

**Syntax:** `/export ABC123 csv`

**Description:** Download survey data as CSV, Excel, or PDF. Useful for offline analysis and reporting.

**Formats:**
- `csv` - Comma-separated values
- `excel` - Microsoft Excel
- `pdf` - PDF report with charts

---

### /settings

**Purpose:** Configure bot preferences

**Syntax:** `/settings`

**Description:** Manage notification preferences, language settings, privacy options.

**Settings:**
- Notification preferences
- Default survey settings
- Privacy controls
- Language selection

---

### /notifications

**Purpose:** Manage survey notifications

**Syntax:** `/notifications`

**Description:** Control when and how you receive notifications about survey responses, new surveys, and updates.

**Options:**
- New response notifications (on/off)
- Daily summary (on/off)
- Survey completion alerts (on/off)
- Quiet hours configuration

---

## Command Usage Tips

### Best Practices

**Use Buttons When Possible:**
- Faster than typing commands
- Fewer typos
- Visual interface
- Context-aware options

**Command Shortcuts:**
- `/start` - Reset everything
- `/help` - When confused
- `/surveys` - Find something to do
- `/mysurveys` - Check your work

**Combine with Web Interface:**
- Create surveys on web (easier)
- Manage via bot (convenient)
- View detailed stats on web
- Quick checks on bot

### Command Comparison

| Task | Bot Command | Alternative |
|------|-------------|-------------|
| Find survey | `/surveys` → Click survey | Direct link |
| Take survey | Click survey button | Deep link |
| View your surveys | `/mysurveys` | Web dashboard |
| Toggle status | Click ▶️ or ⏸️ | Web interface |
| View stats | Click survey name | Web analytics |
| Create survey | Future feature | Web interface |

### When to Use Each Command

**Use `/start` when:**
- First time using bot
- Need to reset state
- Want main menu
- Bot seems stuck

**Use `/help` when:**
- Forgot available commands
- Need usage instructions
- Want to learn features
- Exploring capabilities

**Use `/surveys` when:**
- Looking for surveys to take
- Want to see what's available
- Browsing for interesting topics
- Have time to complete surveys

**Use `/mysurveys` when:**
- Checking response counts
- Need to activate/pause surveys
- Want to see survey performance
- Managing your content

---

## Command Troubleshooting

### "Command not recognized"

**Symptoms:** Bot says "Unknown command"

**Solutions:**
- Check spelling (commands start with `/`)
- Try clicking buttons instead
- Use `/help` to see valid commands
- Use `/start` to reset

### "Bot doesn't respond"

**Symptoms:** No response after sending command

**Solutions:**
- Wait a few seconds (processing time)
- Check internet connection
- Try again
- Use `/start` to restart
- Contact support if persists

### "Not authorized"

**Symptoms:** "You are not registered" message

**Solutions:**
- Use `/start` to register
- Check you're using correct bot
- Clear chat and start over

### "Error occurred"

**Symptoms:** Generic error message

**Solutions:**
- Try command again
- Check your connection
- Use different command
- Report to support with screenshot

---

## Technical Reference

### Command Handler Interface

All commands implement:
```csharp
public interface ICommandHandler
{
    string Command { get; }
    Task HandleAsync(Message message, CancellationToken cancellationToken);
    string GetDescription();
}
```

### Command Registration

Commands registered in DI container:
```csharp
services.AddScoped<ICommandHandler, StartCommandHandler>();
services.AddScoped<ICommandHandler, HelpCommandHandler>();
services.AddScoped<ICommandHandler, SurveysCommandHandler>();
services.AddScoped<ICommandHandler, MySurveysCommandHandler>();
```

### Command Routing

`CommandRouter` service routes commands:
```csharp
// Parse: /start@botname → "start"
var command = message.Text
    .TrimStart('/')
    .Split(' ')[0]
    .Split('@')[0]
    .ToLower();

// Find and execute handler
var handler = _handlers.FirstOrDefault(h =>
    h.Command.Equals(command, StringComparison.OrdinalIgnoreCase));

if (handler != null)
    await handler.HandleAsync(message);
else
    await HandleUnknownCommand(message);
```

---

## Quick Command Reference

```
╔════════════════════════════════════════════════════════════╗
║              SURVEYBOT COMMANDS                            ║
╠════════════════════════════════════════════════════════════╣
║  /start       - Register and show main menu                ║
║  /help        - Show all commands                          ║
║  /surveys     - Browse and take surveys                    ║
║  /mysurveys   - Manage your surveys                        ║
╠════════════════════════════════════════════════════════════╣
║  TIPS:                                                     ║
║  • Commands start with /                                   ║
║  • Buttons are faster than typing                         ║
║  • Use /help when stuck                                   ║
║  • Use /start to reset                                    ║
╚════════════════════════════════════════════════════════════╝
```

---

## Additional Resources

- **User Guide:** [BOT_USER_GUIDE.md](BOT_USER_GUIDE.md)
- **FAQ:** [BOT_FAQ.md](BOT_FAQ.md)
- **Troubleshooting:** [BOT_TROUBLESHOOTING.md](BOT_TROUBLESHOOTING.md)
- **Quick Start:** [BOT_QUICK_START.md](BOT_QUICK_START.md)

---

**Questions?** Type `/help` in the bot or contact support.

_SurveyBot Command Reference v1.0 - Last Updated: November 2025_
