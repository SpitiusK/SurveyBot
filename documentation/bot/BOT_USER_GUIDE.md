# SurveyBot User Guide

Welcome to SurveyBot - your friendly Telegram bot for creating, managing, and taking surveys!

## Table of Contents

1. [Getting Started](#getting-started)
2. [Finding Surveys](#finding-surveys)
3. [Taking a Survey](#taking-a-survey)
4. [Managing Your Surveys](#managing-your-surveys)
5. [Understanding Question Types](#understanding-question-types)
6. [Tips and Best Practices](#tips-and-best-practices)
7. [Troubleshooting](#troubleshooting)

---

## Getting Started

### Finding the Bot

1. Open Telegram on your device
2. Search for **@YourSurveyBot** (replace with actual bot username)
3. Click on the bot to open the chat
4. You'll see a "START" button at the bottom of the screen

### First Time Setup

**Step 1:** Start the bot
- Click the "START" button or type `/start`
- The bot will greet you with a welcome message

**Step 2:** Registration
- The bot automatically registers you in the system
- Your Telegram username and name are saved
- You're now ready to use all features!

**What you'll see:**
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

### Quick Navigation

The bot uses **inline buttons** for easy navigation:
- **Find Surveys** - Browse surveys you can take
- **My Surveys** - View surveys you've created
- **Help** - Get help and see all commands

**Pro tip:** Clicking buttons is faster than typing commands!

---

## Finding Surveys

### Browse Available Surveys

**Method 1: Using Buttons**
- Click the **"Find Surveys"** button from the main menu

**Method 2: Using Command**
- Type `/surveys` in the chat

### What You'll See

The bot shows you all active surveys with details:

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

Click on a survey below to start taking it.

[📋 Customer Satisfaction Survey]
[📋 Product Feedback]
[◀️ Previous] [1/3] [Next ▶️]
[🔄 Refresh] [📊 My Surveys]
```

### Survey Information

For each survey, you can see:
- **Title** - Name of the survey
- **Description** - Brief explanation of the survey's purpose
- **Questions** - How many questions to answer
- **Responses** - How many people have already taken it
- **Updated** - When the survey was last modified

### Navigation

**Pagination:**
- The bot shows 5 surveys per page
- Use **◀️ Previous** and **Next ▶️** to browse pages
- Current page is shown in the middle (e.g., "1/3")

**Quick Actions:**
- **🔄 Refresh** - Reload the survey list
- **📊 My Surveys** - Jump to your own surveys

---

## Taking a Survey

### Starting a Survey

**Step 1:** Click on a survey button
- Example: Click **[📋 Customer Satisfaction Survey]**

**Step 2:** Read the introduction

```
📋 Customer Satisfaction Survey

Help us improve our service

This survey has 5 questions and takes approximately 3 minutes to complete.

Instructions:
- Answer all required questions (marked with *)
- You can skip optional questions
- Your responses are confidential
- You can't go back to previous questions

Ready to start?

[▶️ Start Survey] [❌ Cancel]
```

**Step 3:** Click **▶️ Start Survey**

### Answering Questions

The bot presents questions **one at a time** in order.

#### Example: Text Question

```
Question 1 of 5 *

What did you like most about our service?

Type your answer below:

[⏭️ Skip] (only shows if question is optional)
```

**What to do:**
- Type your answer as a regular message
- Press Send
- The bot validates your answer and moves to the next question

**Example answer:**
```
The customer support was excellent and very responsive. I had an issue with my order and it was resolved within 24 hours.
```

#### Example: Single Choice Question

```
Question 2 of 5 *

How satisfied are you with our service?

[⭐⭐⭐⭐⭐ Very Satisfied]
[⭐⭐⭐⭐ Satisfied]
[⭐⭐⭐ Neutral]
[⭐⭐ Dissatisfied]
[⭐ Very Dissatisfied]
```

**What to do:**
- Click one button to select your answer
- The bot automatically saves your choice
- Moves to the next question immediately

**Confirmation:**
```
✓ Satisfied

Moving to next question...
```

#### Example: Multiple Choice Question

```
Question 3 of 5 *

Which features do you use most? (Select all that apply)

[ ] Online ordering
[ ] Mobile app
[ ] Email support
[ ] Live chat

[✓ Done] [⏭️ Skip]
```

**What to do:**
- Click multiple buttons to select/deselect options
- Checkmarks (✓) show selected items
- Click **✓ Done** when you've made all selections

**Confirmation:**
```
✓ Online ordering
✓ Live chat

Moving to next question...
```

#### Example: Rating Question

```
Question 4 of 5 *

Rate your overall experience:

[1⭐] [2⭐] [3⭐] [4⭐] [5⭐]
```

**What to do:**
- Click the star rating that matches your experience
- 1 star = Poor, 5 stars = Excellent
- Automatically moves to next question

**Confirmation:**
```
✓ 5⭐

Moving to next question...
```

### Progress Tracking

Throughout the survey, you'll see:
- **Question number:** "Question 3 of 5"
- **Required indicator:** Asterisk (*) means you must answer
- **Progress bar:** Shows how far you've progressed

```
Question 3 of 5 *

Progress: ▓▓▓▒▒ 60%
```

### Skipping Optional Questions

If a question is **optional** (no asterisk), you can skip it:

```
Question 5 of 5

What suggestions do you have for improvement?

Type your answer below:

[⏭️ Skip Question]
```

**To skip:**
- Click **⏭️ Skip Question** button

**Confirmation:**
```
Skipped.

Moving to next question...
```

**Note:** Required questions (marked with *) cannot be skipped.

### Completing the Survey

After answering all questions:

```
✅ Survey Completed!

Thank you for completing the Customer Satisfaction Survey!

Your responses have been recorded successfully.

Survey Summary:
- Questions answered: 5/5
- Time taken: 3 minutes
- Completion: 100%

[📊 View Results] [🔙 Back to Surveys]
```

**What's next:**
- **📊 View Results** - See aggregated statistics (if public)
- **🔙 Back to Surveys** - Take another survey

---

## Managing Your Surveys

### Viewing Your Surveys

**Method 1: Using Buttons**
- Click **"My Surveys"** button from main menu

**Method 2: Using Command**
- Type `/mysurveys` in the chat

### Your Survey List

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

### Survey Status Indicators

- **✅ Active** - Survey is accepting responses from users
- **🔴 Inactive** - Survey is paused and not accepting responses

### Managing Individual Surveys

**View Survey Details:**
- Click on the survey name button
- See questions, responses, and statistics

**Toggle Survey Status:**
- Click **▶️** to activate an inactive survey
- Click **⏸️** to pause an active survey

**Create New Survey:**
- Click **➕ Create New Survey**
- Follow the creation flow (future feature)
- Or create via web interface

### Viewing Survey Statistics

When you click on a survey, you'll see:
- Total responses received
- Response rate over time
- Individual question statistics
- Detailed answer analysis

---

## Understanding Question Types

### 1. Text Questions

**Purpose:** Collect open-ended responses in users' own words

**How it works:**
- User types their answer as a regular message
- Can include multiple sentences or paragraphs
- Maximum 1000 characters

**Best for:**
- Feedback and suggestions
- Detailed explanations
- Stories and experiences

**Example:**
```
What improvements would you like to see?
→ User types: "I would love to see more color options and faster shipping times."
```

### 2. Single Choice Questions

**Purpose:** Get one answer from multiple options

**How it works:**
- User clicks one button from the list
- Automatically advances to next question
- Cannot select multiple options

**Best for:**
- Yes/No questions
- Satisfaction ratings
- Demographic information
- Clear preferences

**Example:**
```
How did you hear about us?
[Social Media] [Friend] [Search Engine] [Advertisement]
→ User clicks: Friend
```

### 3. Multiple Choice Questions

**Purpose:** Allow selection of multiple answers

**How it works:**
- User clicks multiple buttons to toggle selections
- Checkmarks show selected options
- Must click "Done" to proceed
- Can select/deselect freely before clicking Done

**Best for:**
- Features used
- Interests and preferences
- Multiple applicable options
- "Select all that apply" scenarios

**Example:**
```
Which features do you use? (Select all that apply)
[ ] Feature A [ ] Feature B [ ] Feature C
→ User selects: Feature A ✓, Feature C ✓
```

### 4. Rating Questions

**Purpose:** Get numerical ratings or scores

**How it works:**
- User clicks one star rating (1-5)
- Visual representation with stars
- Automatically advances after selection

**Best for:**
- Overall satisfaction
- Quality ratings
- Likelihood to recommend
- Experience evaluation

**Example:**
```
Rate your experience:
[1⭐] [2⭐] [3⭐] [4⭐] [5⭐]
→ User clicks: 5⭐
```

**Rating Scale:**
- 1⭐ = Very Poor / Very Dissatisfied
- 2⭐ = Poor / Dissatisfied
- 3⭐ = Average / Neutral
- 4⭐ = Good / Satisfied
- 5⭐ = Excellent / Very Satisfied

---

## Tips and Best Practices

### For Survey Takers

**Before Starting:**
1. **Read the introduction** - Check how many questions and estimated time
2. **Find a quiet moment** - Complete surveys without interruptions
3. **Be honest** - Your genuine feedback is most valuable

**While Taking:**
1. **Read carefully** - Make sure you understand each question
2. **Answer thoughtfully** - Take time to provide quality responses
3. **Watch for asterisks** - * means the question is required
4. **Use Skip wisely** - Optional questions help but aren't mandatory

**After Completing:**
1. **Check results** - View aggregated statistics if available
2. **Share feedback** - Tell survey creators if you had issues
3. **Take more surveys** - Browse other available surveys

### For Survey Creators

**Planning:**
1. **Keep it short** - Aim for 5-10 questions maximum
2. **Have a goal** - Know what insights you're seeking
3. **Mix question types** - Variety keeps respondents engaged

**Writing Questions:**
1. **Be clear** - Avoid jargon and complex language
2. **Be specific** - Ask one thing per question
3. **Be unbiased** - Don't lead respondents to specific answers
4. **Add context** - Provide helpful descriptions when needed

**Survey Settings:**
1. **Test first** - Take your own survey before sharing
2. **Mark required** - Only require essential questions
3. **Enable results** - Let respondents see aggregated results
4. **Monitor responses** - Check progress regularly

**Sharing:**
1. **Activate survey** - Make sure it's active before sharing
2. **Provide context** - Explain why you're asking for feedback
3. **Thank respondents** - Show appreciation for their time
4. **Share results** - Let people know how their feedback helped

---

## Troubleshooting

### Common Issues and Solutions

#### "Bot doesn't respond to my commands"

**Possible causes:**
- Bot service is temporarily down
- Network connectivity issues
- Invalid command format

**Solutions:**
- Wait a moment and try again
- Check your internet connection
- Make sure you typed the command correctly (start with `/`)
- Try clicking buttons instead of typing commands
- Contact support if issue persists

#### "I can't see any surveys"

**Possible causes:**
- No active surveys available
- Bot couldn't load survey list
- Network error

**Solutions:**
- Click **🔄 Refresh** button to reload
- Try the `/surveys` command again
- Create your own survey to get started
- Contact survey organizer

#### "Can't skip a question"

**Possible causes:**
- Question is marked as required (*)
- You must provide an answer

**Solutions:**
- Required questions must be answered to proceed
- Provide any valid answer to continue
- For text questions, you can write "N/A" if truly not applicable
- Cancel survey if you don't want to answer

#### "My answer wasn't accepted"

**Possible causes:**
- Text too long (over 1000 characters)
- No option selected (multiple choice)
- Invalid format

**Solutions:**
- **Text questions:** Shorten your answer to under 1000 characters
- **Multiple choice:** Make sure you selected at least one option
- **Check error message:** Bot explains what went wrong
- Try again with corrected input

#### "Survey shows as already completed"

**Possible causes:**
- You've already taken this survey
- Survey doesn't allow multiple responses

**Solutions:**
- Each survey can typically only be taken once
- Check if it's a different survey you haven't taken
- Contact survey creator if you need to update your response
- Look for other available surveys

#### "Survey session expired"

**Possible causes:**
- Too much time between answers (30+ minutes)
- Survey was deactivated while you were taking it

**Solutions:**
- Click **▶️ Restart Survey** to start over
- Complete surveys in one session when possible
- Your previous answers were not saved
- Contact survey creator if survey disappeared

#### "Can't view survey results"

**Possible causes:**
- Creator disabled public results
- Survey doesn't have enough responses yet
- You haven't completed the survey

**Solutions:**
- Public results are controlled by survey creator
- Complete the survey first before viewing results
- Ask creator to enable public results
- Wait for more responses to be collected

### Getting Additional Help

**Built-in Help:**
- Type `/help` to see all available commands
- Click **Help** button for quick reference
- Use `/start` to reset and see main menu

**Need More Assistance:**
- Contact survey creator for survey-specific issues
- Reach out to bot support team
- Visit documentation website
- Report bugs or issues

### Error Messages Explained

**"Sorry, an error occurred while processing your request. Please try again later."**
- Temporary technical issue
- Try again in a few moments
- Contact support if persists

**"You are not registered yet. Please use /start to register."**
- Your account isn't set up
- Type `/start` to register
- Should only happen on first use

**"This survey is not accepting responses at this time."**
- Survey was deactivated
- Contact survey creator
- Look for other active surveys

**"Your answer is too long. Please keep your answer under 1000 characters."**
- Text answer exceeds limit
- Shorten your response
- Current length shown in message

**"This question is required. Please provide an answer to continue."**
- Can't skip required questions
- Marked with asterisk (*)
- Provide valid answer to proceed

---

## Quick Reference Card

```
╔════════════════════════════════════════════════════════════╗
║              SURVEYBOT QUICK REFERENCE                     ║
╠════════════════════════════════════════════════════════════╣
║  GETTING STARTED:                                          ║
║  /start       - Register and show main menu                ║
║  /help        - Show all commands                          ║
║                                                            ║
║  TAKING SURVEYS:                                           ║
║  /surveys     - Browse available surveys                   ║
║  • Click survey button to view details                    ║
║  • Click "Start Survey" to begin                          ║
║  • Answer each question                                    ║
║  • Complete to submit responses                           ║
║                                                            ║
║  MANAGING SURVEYS:                                         ║
║  /mysurveys   - View your created surveys                  ║
║  • Click survey to view details                           ║
║  • Use ▶️ to activate, ⏸️ to pause                       ║
║  • View statistics and responses                          ║
║                                                            ║
║  QUESTION TYPES:                                           ║
║  Text         - Type your answer                           ║
║  Single       - Click one option                           ║
║  Multiple     - Click options, then "Done"                ║
║  Rating       - Click star rating (1-5)                    ║
║                                                            ║
║  TIPS:                                                     ║
║  • Buttons are faster than commands                       ║
║  • * means required question                              ║
║  • Complete surveys in one session                        ║
║  • Read all instructions first                            ║
╚════════════════════════════════════════════════════════════╝
```

---

## Privacy and Data

### Your Information

**What we collect:**
- Telegram username and name
- Survey responses you submit
- Survey creation and management activity

**How we use it:**
- Provide bot functionality
- Store and analyze survey responses
- Improve service quality

**Your privacy:**
- Responses are confidential
- Only survey creators see individual responses
- Aggregated statistics may be public
- No personal information shared without consent

### Data Security

- Encrypted connections
- Secure database storage
- Access controls
- Regular backups

---

## Keyboard Shortcuts and Tips

### Telegram App Features

**Desktop:**
- `Ctrl + K` - Search for SurveyBot
- `Ctrl + Enter` - Send message
- `↑ Arrow` - Edit last message

**Mobile:**
- Long press on bot name - Pin chat to top
- Swipe left - Delete message
- Tap and hold - Reply to message

### SurveyBot Navigation

**Fastest ways to common actions:**
1. **Take a survey:** Main menu → Find Surveys → Click survey
2. **View your surveys:** Main menu → My Surveys
3. **Get help:** Main menu → Help (or type `/help`)
4. **Start over:** Type `/start`

---

## Frequently Asked Questions

See [BOT_FAQ.md](BOT_FAQ.md) for comprehensive FAQ.

**Quick answers:**

**Q: Can I change my answers after submitting?**
A: No, responses are final once submitted. Review carefully before completing.

**Q: Can I take the same survey twice?**
A: Usually no, but depends on survey settings. Most surveys allow one response per user.

**Q: How long do I have to complete a survey?**
A: Sessions timeout after 30 minutes of inactivity. Try to complete in one sitting.

**Q: Are my answers anonymous?**
A: Survey creators can see who responded, but can configure surveys for anonymous responses.

**Q: How do I create a survey?**
A: Click **➕ Create New Survey** in My Surveys, or use the web interface.

---

## Additional Resources

- **Command Reference:** [BOT_COMMAND_REFERENCE.md](BOT_COMMAND_REFERENCE.md)
- **FAQ:** [BOT_FAQ.md](BOT_FAQ.md)
- **Troubleshooting:** [BOT_TROUBLESHOOTING.md](BOT_TROUBLESHOOTING.md)
- **Quick Start:** [BOT_QUICK_START.md](BOT_QUICK_START.md)

---

**Need help?** Type `/help` in the chat or contact support.

**Enjoying SurveyBot?** Share it with friends and colleagues!

_SurveyBot - Making surveys simple and fun!_ 🎉
