# TASK-025: Bot Command Handlers - Testing Instructions

## Overview
This document provides comprehensive testing instructions for the basic bot command handlers implemented in TASK-025.

## Prerequisites

### 1. Telegram Bot Token
- You need a valid Telegram bot token from @BotFather
- Update `appsettings.json` or `appsettings.Development.json` with your bot token:

```json
{
  "BotSettings": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "BotUsername": "your_bot_username",
    "UseWebhook": false
  }
}
```

### 2. Database Setup
- Ensure PostgreSQL is running
- Database migrations should be applied
- Connection string should be configured in `appsettings.json`

### 3. Test Data
For comprehensive testing, you should have:
- At least one user in the database
- 2-3 active surveys with questions
- 1-2 inactive surveys
- Some surveys with responses

## Running the Application

### Start the API Server

```bash
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet run
```

The application will:
1. Start the API server
2. Initialize the Telegram Bot
3. Set up webhook (if enabled) or prepare for long polling

Watch the console output for:
- "Telegram Bot initialized successfully"
- Bot username and ID confirmation

## Testing Each Command Handler

### Test 1: /start Command (StartCommandHandler)

**Purpose:** Welcome new users, register them in the database, and display the main menu.

**Test Steps:**

1. **New User Registration:**
   - Open Telegram and find your bot
   - Send `/start` command

   **Expected Result:**
   - Welcome message with introduction
   - Message indicates it's a new user
   - Inline keyboard with 3 buttons:
     - "Find Surveys" (top row)
     - "My Surveys" (bottom left)
     - "Help" (bottom right)

   **Verify in Database:**
   - Check Users table for new user record
   - Verify TelegramId, Username, FirstName, LastName are populated
   - CreatedAt and UpdatedAt should be equal (new user)

2. **Existing User Return:**
   - Send `/start` command again
   -
   **Expected Result:**
   - "Welcome back" message
   - Same inline keyboard

   **Verify in Database:**
   - User record updated
   - UpdatedAt should be newer than CreatedAt

3. **User Information Update:**
   - Change your Telegram first name or username
   - Send `/start` command

   **Expected Result:**
   - User information updated in database
   - New username/name reflected in the system

**Error Cases:**
- Bot token invalid: Should see error in console logs
- Database connection issue: Error message sent to user

### Test 2: /help Command (HelpCommandHandler)

**Purpose:** Display all available commands and usage instructions.

**Test Steps:**

1. **View Help from Command:**
   - Send `/help` command

   **Expected Result:**
   - Formatted message with title "SurveyBot - Available Commands"
   - List of all commands:
     - /start - Start the bot and display main menu
     - /help - Show available commands and usage instructions
     - /mysurveys - View and manage your surveys
     - /surveys - Find and take available surveys
   - Usage instructions section
   - Each command has emoji and clear description

2. **View Help from Button:**
   - Send `/start` command
   - Click "Help" button

   **Expected Result:**
   - Same help message as above
   - Button callback acknowledged (loading indicator disappears)

**Verification:**
- All registered commands appear in the list
- Command descriptions are clear and accurate
- Markdown formatting renders correctly

### Test 3: /surveys Command (SurveysCommandHandler)

**Purpose:** Display all active surveys available for users to take.

**Test Steps:**

1. **View Active Surveys (With Surveys):**
   - Ensure there are active surveys in the database
   - Send `/surveys` command

   **Expected Result:**
   - Title: "Available Surveys"
   - Count of active surveys
   - Each survey shows:
     - Number and title
     - Description (truncated to 100 chars if long)
     - Question count
     - Response count
     - Last updated date
   - Inline keyboard with buttons for each survey (max 5 shown)
   - Each survey button has clipboard emoji
   - Bottom buttons: "Refresh" and "My Surveys"

2. **Pagination Test (If > 5 Surveys):**
   - If you have more than 5 active surveys:

   **Expected Result:**
   - Page indicator showing "Page 1 of N"
   - "Next" button appears
   - Click "Next"
   - See page 2 with different surveys
   - "Previous" button appears
   - Page indicator updates

3. **No Active Surveys:**
   - Deactivate all surveys in database
   - Send `/surveys` command

   **Expected Result:**
   - Message: "There are no active surveys available at the moment"
   - Helpful text suggesting to check back later
   - Inline keyboard with "My Surveys" and "Help" buttons

4. **Survey Button Click:**
   - Click any survey button from the list

   **Expected Result:**
   - "This feature is coming soon!" alert
   - (Survey taking flow will be implemented in future tasks)

5. **Refresh Button:**
   - Click "Refresh" button

   **Expected Result:**
   - Message refreshes with updated survey list
   - Latest survey information displayed

**Verification:**
- Only active surveys are shown
- Survey counts are accurate
- Dates are formatted correctly (MMM dd, yyyy)
- Markdown special characters are escaped properly

### Test 4: /mysurveys Command (MySurveysCommandHandler)

**Purpose:** Display surveys created by the current user.

**Test Steps:**

1. **User With Surveys:**
   - Ensure current user has created surveys in database
   - Send `/mysurveys` command

   **Expected Result:**
   - Title: "My Surveys"
   - Count of user's surveys
   - Each survey shows:
     - Number and title
     - Status with emoji (Active or Inactive)
     - Response count
     - Creation date
   - Inline keyboard with buttons for each survey (max 5)
   - Toggle button for each survey (play/pause emoji)
   - "Create New Survey" button at bottom

2. **User With No Surveys:**
   - Test with user who hasn't created surveys
   - Send `/mysurveys` command

   **Expected Result:**
   - Message: "You haven't created any surveys yet"
   - Encouraging message about creating first survey
   - "Create Survey" button

3. **Survey Status Display:**
   - Verify active surveys show:
     - Green checkmark emoji
     - "Status: Active"
   - Verify inactive surveys show:
     - Red circle emoji
     - "Status: Inactive"

4. **Survey Actions:**
   - Click a survey button

   **Expected Result:**
   - "This feature is coming soon!" alert
   - (Survey management will be implemented later)

5. **Toggle Button:**
   - Click toggle button (play/pause)

   **Expected Result:**
   - "This feature is coming soon!" alert
   - (Toggle functionality will be implemented later)

6. **Create Survey Button:**
   - Click "Create New Survey" button

   **Expected Result:**
   - "This feature is coming soon!" alert
   - (Survey creation flow will be added later)

**Verification:**
- Only user's own surveys are displayed
- Response counts match database
- Surveys ordered by creation date (newest first)
- Long titles are truncated properly

### Test 5: Command Router (CommandRouter)

**Purpose:** Route commands to appropriate handlers and handle errors.

**Test Steps:**

1. **Valid Commands:**
   - Test each command: `/start`, `/help`, `/surveys`, `/mysurveys`
   - All should route correctly to their handlers

2. **Command with Bot Username:**
   - Send `/start@your_bot_username`

   **Expected Result:**
   - Command processed correctly
   - Bot username stripped from command

3. **Command with Parameters:**
   - Send `/start param1 param2`

   **Expected Result:**
   - Command processed correctly
   - Parameters ignored (for now)

4. **Unknown Command:**
   - Send `/unknown` or `/notexist`

   **Expected Result:**
   - Error message: "Unknown command: /unknown"
   - Suggestion to use /help

5. **Case Insensitive:**
   - Send `/START`, `/Help`, `/SURVEYS`

   **Expected Result:**
   - All commands processed correctly
   - Case is ignored

**Verification:**
- Check console logs for command routing messages
- Verify handler execution logs
- Error messages are user-friendly

### Test 6: Update Handler (UpdateHandler)

**Purpose:** Process different types of Telegram updates.

**Test Steps:**

1. **Text Messages (Non-Commands):**
   - Send any text message (not starting with /)

   **Expected Result:**
   - Helpful message about using commands
   - Suggestion to use inline keyboard buttons

2. **Callback Queries:**
   - Click any inline keyboard button

   **Expected Result:**
   - Loading indicator appears briefly
   - Appropriate action taken
   - Callback query answered

3. **Edited Messages:**
   - Send a command, then edit it

   **Expected Result:**
   - Edit is logged but ignored
   - No action taken on edited message

**Verification:**
- All update types are logged
- No crashes on unexpected update types
- Error handling works gracefully

## Integration Testing

### Test Scenario 1: New User Journey

1. User starts bot with `/start`
2. Registers as new user
3. Clicks "Find Surveys" button
4. Views available surveys
5. Clicks "Help" to learn more
6. Returns to start with `/start`

**Expected:** Smooth flow with no errors

### Test Scenario 2: Survey Creator Journey

1. User (who created surveys) sends `/start`
2. Clicks "My Surveys" button
3. Views their surveys with statistics
4. Checks active and inactive surveys
5. Uses `/surveys` to see all active surveys
6. Compares their surveys with others

**Expected:** All data displays correctly

### Test Scenario 3: Error Recovery

1. Disconnect database temporarily
2. Send any command
3. Verify error message sent to user
4. Reconnect database
5. Retry command
6. Verify command works

**Expected:** Graceful error handling

## Console Log Verification

Monitor the console output for:

### Successful Command Execution:
```
[Information] Processing /start command from user {TelegramId}
[Information] User {UserId} registered/updated successfully
[Information] Welcome message sent to user {TelegramId}
[Information] Command 'start' handled successfully
```

### Command Routing:
```
[Information] CommandRouter initialized with 4 command handlers: start, help, mysurveys, surveys
[Information] Routing command 'start' from user {TelegramId}
```

### Error Handling:
```
[Error] Error processing /surveys command for user {TelegramId}
[Information] Error message sent to user
```

## Database Verification

### Users Table:
```sql
SELECT * FROM "Users" ORDER BY "UpdatedAt" DESC LIMIT 10;
```
Verify:
- New users are created with correct TelegramId
- User information is updated on each /start
- Timestamps are accurate

### Surveys Table:
```sql
SELECT "Id", "Title", "IsActive", "CreatorId", "CreatedAt"
FROM "Surveys"
WHERE "IsActive" = true;
```
Verify:
- Active surveys appear in /surveys list
- Inactive surveys don't appear
- Survey counts match command output

## Known Limitations (To Be Implemented Later)

1. **Survey Taking:** Clicking survey buttons shows "coming soon" message
2. **Survey Management:** Toggle and edit functions not yet implemented
3. **Survey Creation:** Create survey button not functional
4. **Pagination Callbacks:** Pagination for /surveys is prepared but not fully tested
5. **Response State:** User response state tracking not implemented

## Troubleshooting

### Bot Not Responding
- Check bot token in appsettings.json
- Verify bot is initialized in console logs
- Ensure no firewall blocking

### Commands Not Working
- Verify handlers are registered in DI
- Check CommandRouter initialization
- Review console error logs

### Database Errors
- Check connection string
- Ensure database is running
- Verify migrations are applied
- Check user permissions

### Update Handler Issues
- Verify UpdateHandler is registered
- Check if webhook/polling is configured correctly
- Review Telegram API connectivity

## Success Criteria

All tests pass if:
- ✅ /start registers new users and updates existing users
- ✅ /help displays all commands with descriptions
- ✅ /surveys shows active surveys with pagination
- ✅ /mysurveys shows user's surveys with statistics
- ✅ Unknown commands are handled gracefully
- ✅ All commands are logged properly
- ✅ Bot builds successfully
- ✅ Integration with existing API works
- ✅ Error handling provides user-friendly messages
- ✅ Database operations complete successfully

## Next Steps

After verifying all tests pass:
1. Test with multiple users simultaneously
2. Test with large numbers of surveys (pagination)
3. Test with surveys containing special characters
4. Test error scenarios (network issues, database failures)
5. Monitor performance and response times
6. Prepare for next task: Survey taking flow implementation

## Test Completion Checklist

- [ ] All 4 command handlers tested individually
- [ ] Command router handles valid and invalid commands
- [ ] Update handler processes all update types
- [ ] Integration scenarios work end-to-end
- [ ] Console logs show expected output
- [ ] Database records are correct
- [ ] Error handling works gracefully
- [ ] Bot responds quickly to commands
- [ ] Inline keyboards function properly
- [ ] Markdown formatting renders correctly
- [ ] No memory leaks or crashes during testing
- [ ] Ready for production deployment

---

**Document Version:** 1.0
**Task:** TASK-025
**Date:** 2025-11-06
**Status:** Ready for Testing
