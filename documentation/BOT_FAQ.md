# SurveyBot - Frequently Asked Questions (FAQ)

Complete answers to common questions about using SurveyBot Telegram bot.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Taking Surveys](#taking-surveys)
3. [Creating Surveys](#creating-surveys)
4. [Responses and Results](#responses-and-results)
5. [Privacy and Security](#privacy-and-security)
6. [Technical Questions](#technical-questions)
7. [Account and Settings](#account-and-settings)
8. [Troubleshooting](#troubleshooting)

---

## Getting Started

### How do I start using SurveyBot?

1. Open Telegram
2. Search for **@YourSurveyBot** (replace with actual bot username)
3. Click "START" or type `/start`
4. Bot will register you automatically
5. Start browsing surveys or create your own

**That's it!** No signup forms, passwords, or verification emails needed.

---

### Do I need to create an account?

**No separate account needed.** When you type `/start`, the bot automatically registers your Telegram account. Your Telegram username and name are used.

---

### Is SurveyBot free to use?

**Yes!** SurveyBot is completely free for:
- Taking unlimited surveys
- Creating surveys (with reasonable limits)
- Viewing results and statistics
- All core features

---

### What languages does SurveyBot support?

**Current:** English only

**Future:** Planned support for:
- Spanish
- French
- German
- Chinese
- Portuguese
- Arabic

---

### Can I use SurveyBot on multiple devices?

**Yes!** Use the same Telegram account on:
- Mobile phone (iOS/Android)
- Tablet
- Desktop (Telegram Desktop)
- Web (Telegram Web)

Your surveys and responses sync across all devices automatically.

---

## Taking Surveys

### How do I find surveys to take?

**Method 1: Browse Available Surveys**
- Type `/surveys` or click "Find Surveys"
- Browse list of active surveys
- Click on survey you want to take

**Method 2: Direct Link**
- Someone shares a survey link with you
- Click the link to open in Telegram
- Survey starts automatically

---

### Can I change my answers after submitting?

**No.** Once you complete a survey, your responses are final and cannot be edited.

**Why?** To ensure data integrity and prevent manipulation of results.

**Tip:** Review your answers carefully before submitting!

---

### Can I take a survey multiple times?

**Usually no.** Most surveys only allow one response per user to prevent duplicate data.

**Survey creator controls this setting:**
- Single response (default): Can only take once
- Multiple responses (rare): Can take multiple times

**If you need to update your response:**
- Contact the survey creator
- They may be able to reset your response
- Or create a new version of the survey

---

### How long do I have to complete a survey?

**Session timeout: 30 minutes**

If you don't answer a question within 30 minutes, your session expires and progress is lost.

**Best practice:**
- Check estimated time before starting
- Complete in one sitting
- Set aside uninterrupted time
- Don't start if you're busy

---

### Can I skip questions?

**It depends on the question:**

**Required questions (marked with *):**
- Cannot skip
- Must provide valid answer
- Necessary to proceed

**Optional questions:**
- Can skip with "⏭️ Skip" button
- Not required for completion
- But helpful for survey creator

---

### What if I don't know the answer to a question?

**For required questions:**
- Choose closest answer
- Type "N/A" for text questions (if truly not applicable)
- Select "Not sure" or "Prefer not to answer" if available

**For optional questions:**
- Click "⏭️ Skip Question" button
- No penalty for skipping

**Best practice:** Provide honest answers even if uncertain. "I don't know" is better than guessing wildly.

---

### Can I go back to a previous question?

**No.** The current bot doesn't support going back to previous questions.

**This is a known limitation** planned for future updates.

**Workaround:**
- Read questions carefully before answering
- Take your time on each question
- If you realize you made a mistake, you may need to:
  - Contact survey creator
  - Or complete survey and note the error

---

### Can I see my answers before submitting?

**Not currently.** The bot shows questions one at a time and doesn't provide a review screen before final submission.

**Future feature:** Review screen showing all your answers before completing the survey.

**Current workaround:**
- Take notes in another app
- Screenshot questions if needed
- Be careful when answering

---

### What types of questions can I expect?

**Text Questions:**
- Type your answer as a message
- Up to 1000 characters
- Open-ended responses

**Single Choice:**
- Select one option from list
- Click button to choose
- Auto-advances to next question

**Multiple Choice:**
- Select multiple options
- Checkmarks show selections
- Click "Done" when finished

**Rating Questions:**
- Rate from 1 to 5 stars
- 1 = Very Poor, 5 = Excellent
- Click star rating

---

### Will I be notified when new surveys are available?

**Currently:** No automatic notifications

**Future feature:** Optional notifications for:
- New surveys matching your interests
- Surveys from specific creators
- Survey reminders

**For now:** Check `/surveys` regularly to see new surveys.

---

### Can I save a survey to take later?

**Not currently.** No bookmark or save feature.

**Workarounds:**
- Screenshot the survey list
- Note the survey title
- Ask creator to share direct link
- Pin the bot chat in Telegram

**Future feature:** Bookmark surveys for later.

---

## Creating Surveys

### How do I create a survey?

**Current (MVP):**
1. Use the web interface at [surveybot.example.com](https://surveybot.example.com)
2. Login with your Telegram account
3. Create survey with full editor
4. Activate survey
5. Manage via Telegram bot

**Future:**
- Full survey creation in Telegram bot
- Conversational flow
- Type `/createsurvey` to start

---

### How many questions can I add?

**Recommended: 5-10 questions**
- Better completion rates
- Less respondent fatigue
- Easier to analyze

**Technical limit: Up to 50 questions**
- But not recommended
- Consider breaking into multiple surveys

**Best practice:** Keep surveys short and focused for best results.

---

### Can I edit a survey after creating it?

**Yes,** but with limitations:

**Can edit:**
- Survey title and description
- Question text
- Question options
- Required/optional status
- Active/inactive status

**Cannot edit after responses received:**
- Question type (text to choice, etc.)
- Option order (confuses data)
- Deleting questions (orphans responses)

**Best practice:** Test thoroughly before activating and sharing!

---

### How do I share my survey?

**Method 1: Via Bot**
- Users type `/surveys` to see your active survey
- Shows in public survey list

**Method 2: Direct Link (Future)**
- Generate shareable link
- Share via Telegram, email, social media
- Link starts survey directly

**Method 3: Survey Code (Future)**
- Users type `/survey ABC123`
- Starts specific survey

---

### Can I make a survey private?

**Not in current MVP.**

All active surveys appear in the public `/surveys` list.

**Future features:**
- Private surveys (link-only)
- Password-protected surveys
- Invite-only surveys
- User group restrictions

**Current workaround:**
- Share via external link when available
- Keep survey inactive except when needed
- Use web platform for advanced privacy

---

### Can I limit who can take my survey?

**Not currently.**

Any Telegram user with the bot can take active surveys.

**Future features:**
- User group filtering (e.g., only company employees)
- Geographic restrictions
- Demographic targeting
- Custom access lists

---

### How do I stop accepting responses?

**Simple: Deactivate the survey**

1. Type `/mysurveys`
2. Find your survey
3. Click the **⏸️ Pause** button
4. Survey stops accepting new responses

**Your existing responses are preserved.** You can reactivate anytime with the **▶️ Play** button.

---

### Can I delete a survey?

**Via web interface:** Yes
- Delete surveys you created
- All responses are also deleted
- Cannot be undone!

**Via bot:** Not currently
- Can deactivate (stops new responses)
- Use web interface to fully delete

**Best practice:** Deactivate instead of delete to preserve data.

---

### What question types are supported?

**Current:**
- **Text** - Open-ended answers
- **Single Choice** - Select one option
- **Multiple Choice** - Select multiple options
- **Rating** - 1-5 star scale

**Planned:**
- Yes/No (specialized single choice)
- Number input
- Date/time picker
- File upload
- Image/video questions
- Matrix/grid questions
- Ranking questions

---

### Can I randomize question order?

**Not currently.**

Questions appear in the order you created them.

**Future feature:**
- Randomize all questions
- Randomize within sections
- Randomize answer options

---

## Responses and Results

### How do I see how many people responded?

**Quick view:**
1. Type `/mysurveys`
2. See response count for each survey
3. Example: "📊 Responses: 42"

**Detailed view:**
1. Click on survey name
2. See full statistics
3. View individual responses

---

### Can I see who responded?

**Yes** (as survey creator):
- View list of respondents
- See Telegram usernames
- View individual responses
- Export data with identities

**Privacy note:** Respondents know their identity is visible to creators.

**Future option:** Anonymous surveys where creator doesn't see identities.

---

### Can I see results as they come in?

**Yes!** Results update in real-time:
- Response counts update immediately
- Statistics recalculated with each response
- Graphs and charts update automatically

**How to view:**
1. Type `/mysurveys`
2. Click on survey name
3. View live statistics

---

### Can respondents see the results?

**Creator controls this setting:**

**Public results (default):**
- Respondents see aggregated statistics
- After completing survey
- No individual responses shown
- Only overall trends and numbers

**Private results:**
- Only creator can see results
- Respondents see "Results are private" message
- Better for sensitive topics

---

### Can I export survey results?

**Future feature:**
- Export as CSV (spreadsheet)
- Export as Excel
- Export as PDF report
- Download all responses

**Current workaround:**
- View in web interface
- Copy/paste data manually
- Take screenshots

---

### How is data analyzed?

**Automatic analysis includes:**
- Response rate and count
- Answer distribution (percentages)
- Average ratings
- Most/least common answers
- Time-based trends
- Completion rate

**Text question analysis:**
- Word clouds (future)
- Sentiment analysis (future)
- Common themes (future)

---

### Can I filter responses?

**Future features:**
- Filter by date range
- Filter by user group
- Filter by answer to specific question
- Cross-tabulation

**Current:** View all responses together.

---

### How long is response data kept?

**Forever** (or until you delete):
- Responses stored permanently
- Available for analysis anytime
- Not auto-deleted

**Your control:**
- Delete individual responses (future)
- Delete entire survey (deletes all responses)
- Export before deleting

---

## Privacy and Security

### Are my survey responses private?

**It depends:**

**Survey creator can see:**
- Your Telegram username
- Your individual answers
- When you responded

**Other respondents cannot see:**
- Your individual answers
- Your identity
- Any of your data

**Public statistics:**
- Aggregated data may be public (if creator enables)
- No individual responses shown
- Only percentages and trends

---

### Is my personal information stored?

**We store:**
- Telegram ID (number)
- Telegram username
- First and last name (from Telegram)
- Survey responses you submit
- Survey creation activity

**We don't store:**
- Phone number
- Email address
- Password (you login via Telegram)
- Messages outside of surveys
- Location data

---

### Can survey creators see my phone number?

**No.** Phone numbers are not shared with survey creators.

Only information visible:
- Telegram username (if you have one)
- First and last name (from Telegram profile)
- Telegram ID (meaningless number)

---

### Is my data encrypted?

**Yes:**
- HTTPS for all connections
- Database encryption at rest
- Secure authentication tokens
- Encrypted backups

---

### Can I delete my account?

**Yes.** Contact support to request account deletion.

**What gets deleted:**
- Your user account
- Surveys you created
- Your responses to surveys
- All personal information

**Cannot be undone!**

---

### Who has access to my data?

**Access is limited to:**
- You (your own data)
- Survey creators (your responses to their surveys)
- System administrators (for technical support only)

**Not accessible to:**
- Other users
- Third parties
- Advertisers
- Marketing companies

---

### Is my data sold to third parties?

**Absolutely not.**

We never:
- Sell your data
- Share with advertisers
- Provide to third parties (except as legally required)
- Use for purposes other than survey functionality

---

### What if a survey asks for sensitive information?

**Your choice:**
- Don't have to answer
- Skip optional questions
- Cancel survey
- Contact survey creator with concerns

**Best practice:**
- Never share passwords
- Don't share financial information
- Be cautious with personal details
- Report suspicious surveys

---

## Technical Questions

### What Telegram version do I need?

**Minimum:**
- Telegram 7.0 or higher
- Works on: iOS, Android, Desktop, Web

**Recommended:**
- Latest version for best experience
- Update from app store

---

### Does SurveyBot work offline?

**No.** Active internet connection required:
- To load surveys
- To submit answers
- To receive messages from bot

**Why?** Responses must be saved to database immediately for data integrity.

---

### How much data does SurveyBot use?

**Very little:**
- Text messages: Less than 1 KB each
- Typical survey: 10-50 KB total
- Browsing surveys: 5-20 KB per page

**Data-friendly** even on limited mobile plans.

---

### Can I use SurveyBot in a group chat?

**Not currently.**

SurveyBot only works in direct messages (one-on-one chat with bot).

**Future feature:** Group survey features:
- Survey announcements in groups
- Group-wide surveys
- Collaborative survey creation

---

### What happens if bot goes offline?

**Your data is safe:**
- All responses saved in database
- Surveys preserved
- Nothing lost

**Temporary unavailability:**
- Bot may be down for maintenance
- Usually brief (minutes)
- Try again shortly

---

### Can I host my own instance of SurveyBot?

**Yes!** SurveyBot is open source.

**Requirements:**
- .NET 8.0 runtime
- PostgreSQL database
- Telegram bot token
- Server/hosting

**Documentation:**
- See GitHub repository
- Follow setup instructions
- Join developer community

---

### What technologies is SurveyBot built with?

**Backend:**
- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL database

**Bot:**
- Telegram.Bot library
- Clean Architecture
- Dependency Injection

**Infrastructure:**
- Docker support
- Cloud-ready
- Scalable architecture

---

## Account and Settings

### Can I change my username?

**Your Telegram username is used.**

To change:
1. Go to Telegram Settings
2. Edit Profile
3. Change Username
4. Type `/start` in bot to update

Bot will use your new username automatically.

---

### Can I have multiple accounts?

**No need!** One Telegram account = one SurveyBot account.

**If you have multiple Telegram accounts:**
- Each is separate in SurveyBot
- Surveys don't sync between accounts
- Responses tracked per account

---

### How do I change notification settings?

**Currently:** No notification settings (no notifications sent yet).

**Future feature:**
- `/settings` command
- Control notification preferences
- Set quiet hours
- Choose notification types

---

### Can I delete a survey I took?

**No.** Once submitted, you cannot delete your response.

**Why?** Preserves data integrity for survey creators.

**If needed:**
- Contact survey creator
- Explain why you want it deleted
- They may be able to remove it

---

### Can I merge two accounts?

**Not currently.**

If you accidentally created multiple accounts (using different Telegram accounts), contact support. Manual merge may be possible.

---

## Troubleshooting

### The bot doesn't respond to my commands. What should I do?

**Quick fixes:**
1. Wait 10 seconds
2. Type `/start`
3. Check internet connection
4. Try clicking buttons instead of typing

**See full guide:** [BOT_TROUBLESHOOTING.md](BOT_TROUBLESHOOTING.md)

---

### I get "You are not registered yet" errors.

**Solution:** Type `/start` to register.

This registers your Telegram account with SurveyBot.

---

### My survey responses didn't save.

**Possible causes:**
- Session timed out (30 minutes inactive)
- Lost internet connection during submission
- Bot error during processing

**What to do:**
1. Check if survey shows as completed (try taking again)
2. If not completed, retake survey
3. Maintain stable connection
4. Complete in one session

---

### I can't find a survey I was just taking.

**Possible reasons:**
- Survey was deactivated
- Survey was deleted
- Your session expired

**What to do:**
1. Check `/surveys` for active surveys
2. Contact survey creator
3. Your progress likely lost if session expired

---

### How do I report a bug or issue?

**Report via:**
- Email: support@surveybot.example.com
- In-bot: Type `/support` (if available)
- GitHub: Open issue on repository

**Include:**
- Description of problem
- Steps to reproduce
- Screenshots
- Your Telegram username
- When it started

---

### How do I request a new feature?

**Feature requests welcome!**

**Submit via:**
- Email: features@surveybot.example.com
- GitHub: Feature request issue
- Community forum

**Include:**
- What feature you want
- Why it's useful
- How it should work
- Examples from other tools

---

## Additional Questions

### Can I use SurveyBot for business/commercial purposes?

**Yes!** SurveyBot is free for:
- Personal use
- Business use
- Commercial purposes
- Educational purposes
- Research

**No premium tier or licensing required** (in current MVP).

---

### Is there a limit on number of surveys I can create?

**Current:** No hard limit

**Reasonable use policy:**
- Don't spam surveys
- Don't create thousands of surveys
- Don't abuse system

**Future:** Possible tiered limits:
- Free: 10 active surveys
- Pro: Unlimited surveys

---

### Can I collaborate with others on a survey?

**Not currently.**

Only the creator can:
- Edit survey
- View responses
- Manage settings

**Future feature:** Collaborative surveys with multiple owners/editors.

---

### Can I use SurveyBot for academic research?

**Yes!** Perfect for:
- Academic surveys
- Research studies
- Student projects
- Data collection

**Tips:**
- Include IRB approval info in description
- Explain research purpose
- Provide contact information
- Follow research ethics guidelines

---

### How do I cite SurveyBot in my research?

**Suggested citation:**

```
SurveyBot. (2025). Telegram-based survey platform [Computer software].
Retrieved from https://github.com/yourusername/surveybot
```

Or:

```
Data collected via SurveyBot, a Telegram-based survey platform
(https://github.com/yourusername/surveybot), November 2025.
```

---

### Can I customize the bot's appearance?

**Not currently.**

Bot uses standard Telegram message formatting:
- Standard text and emoji
- Inline keyboards
- Markdown formatting

**No custom:**
- Colors
- Fonts
- Themes
- Branding

**Future:** Possible custom bot profiles per survey.

---

### Does SurveyBot support accessibility features?

**Yes:**
- Screen reader compatible
- Keyboard navigation
- Clear text labels
- Logical tab order

**Telegram's built-in accessibility features work with SurveyBot:**
- VoiceOver (iOS)
- TalkBack (Android)
- Screen readers (Desktop)
- High contrast modes

---

### Can I integrate SurveyBot with other tools?

**Future feature:** API integration with:
- Google Sheets
- Microsoft Excel
- Slack
- Discord
- Webhooks
- Zapier

**Current:** Export functionality (planned).

---

### How often is SurveyBot updated?

**Regular updates:**
- Bug fixes as needed
- Feature updates monthly
- Security patches immediately

**Stay informed:**
- Check bot messages for announcements
- Follow GitHub repository
- Join community channels

---

### Where can I find more help?

**Documentation:**
- [User Guide](BOT_USER_GUIDE.md)
- [Command Reference](BOT_COMMAND_REFERENCE.md)
- [Troubleshooting Guide](BOT_TROUBLESHOOTING.md)
- [Quick Start](BOT_QUICK_START.md)

**Support:**
- Email: support@surveybot.example.com
- GitHub Issues
- Community forum
- In-bot: Type `/help`

---

### Can I contribute to SurveyBot development?

**Yes! Contributions welcome:**

**Ways to contribute:**
- Report bugs
- Suggest features
- Write documentation
- Submit code (GitHub)
- Help other users
- Translate to other languages

**Start here:**
- GitHub repository
- CONTRIBUTING.md
- Developer documentation

---

## Quick Answers

**Q: Is it free?**
A: Yes, completely free!

**Q: Do I need an account?**
A: Just your Telegram account, no separate signup.

**Q: Can I edit answers after submitting?**
A: No, answers are final.

**Q: How long do surveys stay active?**
A: Until creator deactivates them.

**Q: Are my responses private?**
A: Creator can see your responses; other users cannot.

**Q: Can I see who else responded?**
A: No, only survey creators see respondent list.

**Q: Why did my session expire?**
A: 30 minutes of inactivity times out sessions.

**Q: Can I go back to previous questions?**
A: Not currently, planned for future.

**Q: How do I report issues?**
A: Email support or open GitHub issue.

**Q: Is my data secure?**
A: Yes, encrypted connections and secure storage.

---

**Still have questions?**

- Type `/help` in the bot
- Check [User Guide](BOT_USER_GUIDE.md)
- Contact support: support@surveybot.example.com

_SurveyBot FAQ - Last Updated: November 2025_
