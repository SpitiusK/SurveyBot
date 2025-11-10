# SurveyBot Help Messages

This file contains all help text and messages used within the Telegram bot.

## Purpose

- Centralized location for all user-facing bot messages
- Easy to update without changing code
- Consistent messaging across the bot
- Supports localization preparation

---

## Welcome Messages

### First Time User Welcome

```
Hello, {FirstName}!

Welcome to SurveyBot! 👋 I can help you create and manage surveys, or participate in surveys created by others.

*What would you like to do?*
- Create and manage your own surveys
- Take surveys shared by others
- View results and statistics

Use the buttons below to get started, or type /help to see all available commands.
```

### Returning User Welcome

```
Hello, {FirstName}!

Welcome back to SurveyBot! 👋

*What would you like to do today?*
Use the buttons below or type /help for assistance.
```

---

## Help Command Messages

### Main Help Text

```
*SurveyBot - Available Commands*

Here are all the commands you can use:

/start - Start the bot and display main menu
/help - Show available commands and usage instructions
/surveys - Find and take available surveys
/mysurveys - View and manage your surveys

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

---

## Survey List Messages

### Active Surveys Available

```
*Available Surveys*

Found {Count} active {survey/surveys} (Page {CurrentPage} of {TotalPages}):

{SurveyList}

_Click on a survey below to start taking it._
```

### No Active Surveys

```
*Available Surveys*

There are no active surveys available at the moment.

Check back later or create your own survey!
```

---

## My Surveys Messages

### User Has Surveys

```
*My Surveys*

You have created {Count} {survey/surveys}:

{SurveyList}

_Use the buttons below to manage your surveys._
```

### User Has No Surveys

```
*My Surveys*

You haven't created any surveys yet.

Ready to create your first survey?
Use the button below or create one through the web interface.
```

---

## Survey Introduction Message

```
📋 *{SurveyTitle}*

{SurveyDescription}

This survey has {QuestionCount} {question/questions} and takes approximately {EstimatedMinutes} {minute/minutes} to complete.

*Instructions:*
- Answer all required questions (marked with *)
- You can skip optional questions
- Your responses are confidential
- You can't go back to previous questions

Ready to start?
```

---

## Question Presentation Messages

### Text Question

```
Question {CurrentQuestion} of {TotalQuestions} {*}

{QuestionText}

Type your answer below:
```

### Single Choice Question

```
Question {CurrentQuestion} of {TotalQuestions} {*}

{QuestionText}

{Options as buttons}
```

### Multiple Choice Question

```
Question {CurrentQuestion} of {TotalQuestions} {*}

{QuestionText} (Select all that apply)

{Options as checkboxes}
```

### Rating Question

```
Question {CurrentQuestion} of {TotalQuestions} {*}

{QuestionText}

{Rating buttons 1-5 stars}
```

---

## Answer Confirmation Messages

### Text Answer Accepted

```
✓ Answer recorded.

Moving to next question...
```

### Choice Selected

```
✓ {SelectedOption}

Moving to next question...
```

### Multiple Choices Selected

```
✓ {Option1}
✓ {Option2}

Moving to next question...
```

### Rating Selected

```
✓ {Rating}⭐

Moving to next question...
```

### Question Skipped

```
Skipped.

Moving to next question...
```

---

## Validation Error Messages

### Required Question Skip Attempt

```
⚠️ This question is required.

Please provide an answer to continue.
```

### Text Answer Too Long

```
⚠️ Your answer is too long.

Please keep your answer under 1000 characters.
Current length: {CharacterCount} characters
```

### Text Answer Empty (for required)

```
⚠️ Answer cannot be empty.

Please provide an answer to this required question.
```

### No Options Selected (Multiple Choice)

```
⚠️ Please select at least one option.

Click on the options you want, then click "Done".
```

---

## Survey Completion Messages

### Survey Completed Successfully

```
✅ *Survey Completed!*

Thank you for completing the {SurveyTitle}!

Your responses have been recorded successfully.

*Survey Summary:*
- Questions answered: {AnsweredCount}/{TotalQuestions}
- Time taken: {Duration} minutes
- Completion: {Percentage}%
```

---

## Error Messages

### Generic Error

```
Sorry, an error occurred while processing your request. Please try again later.
```

### User Not Registered

```
You are not registered yet. Please use /start to register.
```

### Survey Not Found

```
❌ Survey not found

This survey may have been deleted or deactivated.
```

### Survey Inactive

```
❌ Survey is no longer active

This survey is not accepting responses at this time.
```

### Already Completed Survey

```
✅ You've already completed this survey

Thank you for your previous response!

You can only take each survey once.
```

### Network Error

```
❌ Connection error

Unable to submit your answer. Please check your connection and try again.
```

### Session Expired

```
⏱️ Survey session expired

Your session has timed out due to inactivity.

Would you like to start over?
```

### Permission Denied

```
❌ Permission denied

You don't have permission to perform this action.
```

### Survey Retrieval Error

```
Sorry, an error occurred while retrieving surveys. Please try again later.
```

### User Surveys Retrieval Error

```
Sorry, an error occurred while retrieving your surveys. Please try again later.
```

---

## Status Messages

### Survey Activated

```
✅ Survey activated successfully!

Your survey is now accepting responses.
```

### Survey Deactivated

```
⏸️ Survey paused successfully!

Your survey is no longer accepting responses. Existing responses are preserved.
```

### Survey Toggle Error

```
❌ Failed to update survey status

Please try again later.
```

---

## Progress Indicators

### Question Progress

```
Question {Current} of {Total}

Progress: {ProgressBar} {Percentage}%
```

### Progress Bar Symbols

```
▓▓▓▓▓ - Filled
▒▒▒▒▒ - Empty
```

---

## Button Labels

### Main Menu Buttons

```
Find Surveys
My Surveys
Help
```

### Survey Action Buttons

```
▶️ Start Survey
❌ Cancel
⏭️ Skip Question
✓ Done
🔄 Retry
```

### Survey Management Buttons

```
▶️ (Activate)
⏸️ (Pause)
📋 {SurveyName}
➕ Create New Survey
```

### Navigation Buttons

```
◀️ Previous
Next ▶️
🔄 Refresh
📊 My Surveys
📊 View Results
🔙 Back to Surveys
```

---

## Status Indicators

### Active Status

```
✅ Status: Active
```

### Inactive Status

```
🔴 Status: Inactive
```

---

## Survey Information Format

### Survey List Item

```
{Number}. *{Title}*
   {Description}
   Questions: {QuestionCount}
   Responses: {ResponseCount}
   Updated: {UpdateDate}
```

### My Survey List Item

```
{Number}. *{Title}*
   {Status}
   📊 Responses: {ResponseCount}
   📅 Created: {CreatedDate}
```

---

## Confirmation Messages

### Action Confirmed

```
✓ Done!
```

### Action Cancelled

```
❌ Cancelled.
```

---

## Loading Messages

### Loading Surveys

```
Loading surveys...
```

### Processing

```
Processing...
```

### Submitting

```
Submitting your answer...
```

---

## Tips and Hints

### First Survey Tip

```
💡 Tip: Complete surveys in one sitting to avoid session timeout (30 minutes).
```

### Required Question Tip

```
💡 Tip: Questions marked with * are required and must be answered.
```

### Multiple Choice Tip

```
💡 Tip: Click multiple options, then press "Done" when you're ready to proceed.
```

---

## Footer Text

### Standard Footer

```
_SurveyBot - Making surveys simple and fun!_
```

### Need Help Footer

```
_Need help? Type /help or contact support._
```

---

## Usage Guidelines

### For Developers

1. **Use placeholders:** `{VariableName}` for dynamic content
2. **Use emojis:** Add visual appeal and clarity
3. **Keep it concise:** Telegram users expect brief messages
4. **Be friendly:** Conversational tone
5. **Use formatting:**
   - `*Bold*` for emphasis
   - `_Italic_` for de-emphasis
   - `` `Code` `` for technical terms

### Message Length Limits

- **Single message:** 4096 characters (Telegram limit)
- **Recommended:** Under 500 characters per message
- **Long content:** Split into multiple messages

### Emoji Guidelines

**Use emojis for:**
- Status indicators (✅ 🔴)
- Actions (▶️ ⏸️ ⏭️)
- Feedback (✓ ❌ ⚠️)
- Visual hierarchy (📋 📊 📅)

**Don't overuse:**
- One emoji per line maximum
- Only use when it adds clarity
- Avoid decorative-only emojis

### Formatting Best Practices

**Bold (*text*):**
- Survey titles
- Section headers
- Important information

**Italic (_text_):**
- Footers
- Hints
- Less important details

**Code (`text`):**
- Commands (/start)
- Technical terms
- Examples

---

## Localization Preparation

### Strings to Extract

All messages in this file should be extracted for translation:
- Welcome messages
- Help text
- Error messages
- Button labels
- Status messages

### Variables to Preserve

Keep these unchanged in translations:
- `{FirstName}`
- `{Count}`
- `{SurveyTitle}`
- `/commands`
- Technical terms

### Cultural Considerations

- Date formats may vary
- Time formats (12h vs 24h)
- Number formats
- Currency symbols
- Measurement units

---

## Message Priority Levels

### Critical (Must be clear)

- Error messages
- Required actions
- Data loss warnings

### Important (Should be clear)

- Help text
- Instructions
- Confirmations

### Informational (Nice to have)

- Tips
- Hints
- Footer text

---

## Testing Checklist

When updating messages:

- [ ] Check character length (under 4096)
- [ ] Verify all placeholders present
- [ ] Test formatting renders correctly
- [ ] Ensure emojis display properly
- [ ] Verify tone is consistent
- [ ] Check for typos
- [ ] Test on mobile and desktop
- [ ] Verify button labels fit

---

## Update History

**v1.0 - November 2025**
- Initial message definitions
- English only
- Core bot functionality

---

**Note to Developers:**

These messages are used throughout the bot code. When making changes:

1. Update this file first
2. Update bot code to use new messages
3. Test thoroughly
4. Update user documentation if needed
5. Consider impact on localization

**Message Loading:**

Messages should be loaded from this file or a configuration system, not hardcoded in handlers. This enables:
- Easy updates without code changes
- Localization support
- A/B testing of messages
- Consistent messaging

---

_SurveyBot Help Messages v1.0 - Last Updated: November 2025_
