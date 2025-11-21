# SurveyBot Troubleshooting Guide

Complete troubleshooting guide for common issues with SurveyBot Telegram bot.

## Table of Contents

1. [Bot Not Responding](#bot-not-responding)
2. [Registration and Login Issues](#registration-and-login-issues)
3. [Survey Loading Problems](#survey-loading-problems)
4. [Answer Submission Failures](#answer-submission-failures)
5. [Survey Completion Issues](#survey-completion-issues)
6. [Session and Timeout Problems](#session-and-timeout-problems)
7. [Inline Button Problems](#inline-button-problems)
8. [Display and Formatting Issues](#display-and-formatting-issues)
9. [Permission and Access Errors](#permission-and-access-errors)
10. [Performance and Speed Issues](#performance-and-speed-issues)

---

## Bot Not Responding

### Symptom
Bot doesn't reply to commands or messages

### Possible Causes
- Bot service is offline
- Network connectivity issues
- Telegram API problems
- Bot is processing previous command
- Invalid command format

### Solutions

**Step 1: Check Basic Connectivity**
- Verify your internet connection
- Try opening Telegram web version
- Send message to another bot to test Telegram

**Step 2: Verify Command Format**
- Commands must start with `/`
- Example: `/start` not `start`
- No extra spaces: `/start` not `/ start`
- Check spelling carefully

**Step 3: Wait and Retry**
- Wait 5-10 seconds
- Bot may be processing previous request
- High load can cause delays

**Step 4: Restart Conversation**
- Type `/start` to reset
- Clear chat history (optional)
- Start fresh conversation

**Step 5: Use Buttons Instead**
- Click inline buttons instead of typing
- Buttons less prone to errors
- Navigate using main menu

**Step 6: Check Bot Status**
```
Try these test commands in order:
1. /start
2. /help
3. /surveys
```

If all fail, bot service may be down.

### When to Contact Support
- Bot not responding for 5+ minutes
- All commands fail
- Other users report same issue
- Error persists after restart

---

## Registration and Login Issues

### Issue: "You are not registered yet"

**What it means:**
- Your user account not found in system
- Need to complete registration

**Solution:**
```
1. Type /start
2. Wait for welcome message
3. Click any button to confirm registration
4. Try your original command again
```

### Issue: Registration Fails

**Symptoms:**
- `/start` shows error message
- "An error occurred" after registration
- Can't access `/mysurveys`

**Possible Causes:**
- Database connection error
- Network timeout
- Telegram user data incomplete

**Solutions:**

**Step 1: Verify Telegram Account**
- Make sure you have Telegram username set
- Go to Settings → Edit Profile → Username
- Set username if not already set

**Step 2: Clear and Retry**
- Type `/start` again
- Wait for full response
- Don't click multiple times

**Step 3: Check Internet**
- Verify stable connection
- Try on different network (Wi-Fi vs Mobile data)

**Step 4: Wait and Retry**
- Wait 1-2 minutes
- Database may be temporarily busy
- Try `/start` again

### Issue: Lost Login Session

**Symptoms:**
- Bot forgets who you are
- Need to use `/start` repeatedly
- "Not registered" errors

**Causes:**
- Database cleared
- User record deleted
- Bot system updated

**Solution:**
- Use `/start` to re-register
- Your surveys remain intact
- Response history preserved

---

## Survey Loading Problems

### Issue: "No active surveys available"

**What it means:**
- No surveys currently accepting responses
- All surveys are inactive or deleted

**Solutions:**

**If you expected surveys:**
1. Click **🔄 Refresh** button
2. Wait a moment and try `/surveys` again
3. Contact survey creator to activate survey
4. Check if surveys were deactivated

**If starting fresh:**
1. Create your own survey (web interface)
2. Activate survey
3. Return to bot and check `/surveys`

### Issue: Survey List Won't Load

**Symptoms:**
- `/surveys` shows error
- Loading takes too long (30+ seconds)
- "Error retrieving surveys" message

**Possible Causes:**
- Database connection error
- Network issues
- Corrupt survey data
- Server overload

**Solutions:**

**Quick Fixes:**
1. Wait 30 seconds
2. Type `/surveys` again
3. Click **🔄 Refresh** if available

**If persists:**
1. Check internet connection
2. Try `/start` to reset
3. Try different command like `/help`
4. Wait 5 minutes and retry

**Verify bot is working:**
- Try `/help` command (no database needed)
- If `/help` works, database issue
- If `/help` fails, bot offline

### Issue: Surveys Show Incorrect Information

**Symptoms:**
- Old response counts
- Surveys marked active when inactive
- Missing surveys

**Solutions:**
1. Click **🔄 Refresh** to update
2. Survey may have been just updated
3. Check web interface for accurate data
4. Contact survey creator to verify status

---

## Answer Submission Failures

### Issue: "Your answer is too long"

**What it means:**
- Text answer exceeds 1000 character limit
- Need to shorten response

**Solutions:**
1. Count current characters in message
2. Edit to under 1000 characters
3. Break into multiple sentences
4. Remove unnecessary words
5. Focus on key points

**Character count tip:**
- Copy answer to notes app
- Check character count
- Edit and try again

### Issue: "Please select at least one option"

**For Multiple Choice Questions:**

**What it means:**
- No options selected
- Must choose at least one

**Solutions:**
1. Click at least one option button
2. Checkmarks (✓) show selected
3. Then click **✓ Done** button

**Common mistakes:**
- Clicking Done without selecting options
- Expecting auto-submit like single choice
- Not seeing checkmarks appear

### Issue: Answer Not Accepted

**Symptoms:**
- Submit answer but no confirmation
- Bot doesn't move to next question
- Error message appears

**Possible Causes:**
- Validation failed
- Network error during submission
- Answer format incorrect
- Required question skipped

**Solutions:**

**For Text Questions:**
- Check length (1-1000 characters)
- Make sure not empty for required questions
- Type actual text, not commands

**For Choice Questions:**
- Make sure option selected (checkmark visible)
- Click Done button for multiple choice
- Don't type answer, click button

**For Rating Questions:**
- Click one star rating button
- Don't type number

**Network Issues:**
1. Check connection indicator in Telegram
2. Wait a moment for bot to respond
3. Don't submit multiple times
4. If timeout, may need to restart survey

### Issue: Can't Skip Question

**Symptoms:**
- Skip button doesn't work
- "This question is required" message
- Can't proceed without answering

**What it means:**
- Question marked as required (*)
- Must provide valid answer to continue

**Solutions:**
1. Check for asterisk (*) next to question number
2. Required questions must be answered
3. Provide any valid answer
4. For text questions, can write "N/A" if truly not applicable
5. For choice questions, select closest option

**If you truly can't answer:**
- Consider canceling survey
- Contact survey creator
- Leave feedback about question

---

## Survey Completion Issues

### Issue: Survey Won't Complete

**Symptoms:**
- Answered all questions but no completion message
- Stuck on last question
- No "Submit" or "Complete" button

**Possible Causes:**
- Not all required questions answered
- Last answer didn't submit
- Network error
- Session expired

**Solutions:**

**Step 1: Verify All Answered**
- Check question counter (e.g., "Question 5 of 5")
- Make sure you submitted last answer
- Look for confirmation after last answer

**Step 2: Check Last Answer**
- For multiple choice, did you click Done?
- For text, did you send message?
- For rating, did you click stars?

**Step 3: Wait for Processing**
- Allow 5-10 seconds after last answer
- Bot may be processing completion
- Completion message should appear

**Step 4: Network Check**
- Verify internet connection
- Check Telegram connection status
- May need to restart if connection dropped

### Issue: "Survey not found"

**Symptoms:**
- Mid-survey error about survey not found
- Survey was there but now gone

**Causes:**
- Survey was deleted by creator
- Survey was deactivated
- Database error

**Solutions:**
1. Return to `/surveys` to find active surveys
2. Contact survey creator about issue
3. Your partial progress likely lost
4. Choose different survey

### Issue: "Already completed this survey"

**What it means:**
- You've already submitted responses
- Survey doesn't allow multiple responses

**Solutions:**
1. Check if you previously completed it
2. Contact creator if you need to update responses
3. Look for other available surveys
4. Creator may be able to reset your response

**If you didn't complete it:**
- Possible someone else used your account
- Check with family/friends who have access
- Change your Telegram password

---

## Session and Timeout Problems

### Issue: "Survey session expired"

**What it means:**
- 30 minutes passed without activity
- Your in-progress survey was terminated
- Answers not saved

**Solutions:**
1. Click **▶️ Restart Survey** button
2. Complete survey in one session
3. Set aside uninterrupted time
4. Bookmark survey for later

**Prevention:**
- Check estimated time before starting
- Complete in single sitting
- Don't start survey if busy

### Issue: Session Lost Mid-Survey

**Symptoms:**
- Bot forgets you were taking survey
- Returns to main menu unexpectedly
- Progress disappeared

**Causes:**
- Timeout (30 minutes inactive)
- Bot restart/update
- Database connection lost
- Telegram connection dropped

**Recovery:**
1. Type `/start` to reset
2. Go to `/surveys`
3. Start survey again from beginning
4. Previous answers likely not saved

**Prevention:**
- Complete surveys without long breaks
- Maintain stable internet connection
- Don't switch between apps frequently

---

## Inline Button Problems

### Issue: Buttons Don't Work

**Symptoms:**
- Clicking buttons does nothing
- No response after button click
- Buttons appear but unresponsive

**Possible Causes:**
- Callback handler error
- Network latency
- Bot processing previous action
- Telegram app issue

**Solutions:**

**Quick Fixes:**
1. Wait 5 seconds and try again
2. Try different button
3. Use command instead (`/start`, `/surveys`)

**If persists:**
1. Restart Telegram app
2. Update Telegram to latest version
3. Try on different device
4. Use commands instead of buttons

### Issue: Wrong Action Triggered

**Symptoms:**
- Button does something unexpected
- Wrong survey opens
- Unintended action occurs

**Causes:**
- Clicked wrong button
- Bot state confusion
- Display cached incorrectly

**Solutions:**
1. Type `/start` to reset
2. Navigate back to desired action
3. Read button labels carefully
4. Use text commands for precision

### Issue: Buttons Show Wrong Labels

**Symptoms:**
- Button text doesn't match expected
- Old labels from previous message
- Overlapping text

**Causes:**
- Telegram app cache
- Display refresh issue
- Long text truncation

**Solutions:**
1. Scroll up and back down
2. Close and reopen chat
3. Restart Telegram app
4. Use commands if labels unclear

---

## Display and Formatting Issues

### Issue: Text Appears Scrambled

**Symptoms:**
- Markdown not rendering correctly
- Strange characters appearing
- Broken formatting

**Causes:**
- Special characters in survey text
- Markdown escaping issues
- Unicode problems

**Solutions:**
1. Usually doesn't affect functionality
2. Can still read content and answer
3. Report to survey creator
4. Screenshot and send to support

### Issue: Buttons Too Small/Large

**Symptoms:**
- Hard to click buttons
- Text cut off
- Layout issues

**Causes:**
- Telegram app settings
- Font size settings
- Device screen size

**Solutions:**
1. Adjust Telegram font size in settings
2. Update Telegram app
3. Use landscape mode on phone
4. Use tablet or desktop if available

### Issue: Messages Not Showing

**Symptoms:**
- Bot sent message but can't see it
- Missing responses in conversation
- Gaps in chat history

**Solutions:**
1. Scroll up to find message
2. Check if message was deleted
3. Restart Telegram app
4. Clear app cache (Settings → Data and Storage)

---

## Permission and Access Errors

### Issue: "You don't have permission"

**For `/mysurveys` command:**

**What it means:**
- Trying to access surveys you didn't create
- Account mismatch

**Solutions:**
1. Make sure you created surveys on this account
2. Check you're logged into correct Telegram account
3. Verify with `/start` that registration is correct
4. If surveys on different account, switch accounts

### Issue: Can't Activate/Deactivate Survey

**Symptoms:**
- Toggle button doesn't work
- Status doesn't change
- Error message appears

**Causes:**
- Not the survey creator
- Database error
- Network issue

**Solutions:**
1. Verify you created this survey
2. Check with `/mysurveys` for your surveys
3. Try again with stable connection
4. Use web interface as alternative

### Issue: Can't View Survey Results

**Symptoms:**
- "Results not available" message
- Blank statistics page
- Error loading results

**Causes:**
- Creator disabled public results
- Not enough responses yet (minimum threshold)
- You haven't completed survey
- Database error

**Solutions:**
1. Complete survey first before viewing results
2. Contact creator to enable public results
3. Wait for more responses to be collected
4. Try again later

---

## Performance and Speed Issues

### Issue: Bot Responds Slowly

**Symptoms:**
- 5-10 second delays
- Loading indicators for long time
- Timeouts frequently

**Causes:**
- High server load
- Slow internet connection
- Database performance issues
- Complex survey with many questions

**Solutions:**

**Immediate:**
1. Wait patiently for response
2. Don't send multiple commands
3. Check your internet speed

**Long-term:**
1. Use bot during off-peak hours
2. Ensure stable high-speed connection
3. Close other bandwidth-heavy apps
4. Report persistent slow performance

### Issue: Survey Takes Too Long to Load

**Symptoms:**
- Opening survey has long delay
- Question loading is slow
- Each answer submission is slow

**Causes:**
- Survey has many questions
- Large option lists
- Database query performance
- Network latency

**Solutions:**
1. Be patient, let it load completely
2. Don't click buttons repeatedly
3. Check internet connection
4. Try again later if consistently slow

### Issue: Frequent Timeouts

**Symptoms:**
- "Connection timeout" errors
- "Request failed" messages
- Actions fail to complete

**Causes:**
- Unstable internet connection
- Server overload
- Bot service issues

**Solutions:**
1. Switch to more stable network
2. Try mobile data if on Wi-Fi (or vice versa)
3. Move to area with better signal
4. Wait and retry later
5. Report if persistent

---

## Error Messages Explained

### "Sorry, an error occurred while processing your request. Please try again later."

**Meaning:** Generic server error

**Actions:**
- Wait 1-2 minutes
- Try command again
- If persists, report to support

---

### "You are not registered yet. Please use /start to register."

**Meaning:** User account not found

**Actions:**
- Type `/start`
- Complete registration
- Try original action again

---

### "This survey is not accepting responses at this time."

**Meaning:** Survey deactivated or deleted

**Actions:**
- Return to `/surveys` for active surveys
- Contact survey creator
- Try different survey

---

### "Your answer is too long. Please keep your answer under 1000 characters."

**Meaning:** Text answer exceeds limit

**Actions:**
- Count characters (shown in error)
- Edit answer to be shorter
- Submit again

---

### "This question is required. Please provide an answer to continue."

**Meaning:** Can't skip required question

**Actions:**
- Look for asterisk (*) on question
- Provide valid answer
- Use closest option if not sure

---

### "Please select at least one option."

**Meaning:** No options selected in multiple choice

**Actions:**
- Click at least one option button
- Look for checkmarks (✓)
- Click **✓ Done** when ready

---

### "Survey session expired. Would you like to start over?"

**Meaning:** 30 minutes passed without activity

**Actions:**
- Click **▶️ Restart Survey**
- Complete survey in one session next time

---

### "You've already completed this survey."

**Meaning:** Response already submitted

**Actions:**
- Can't take survey again (usually)
- Contact creator to reset if needed
- Find other surveys

---

### "Survey not found. This survey may have been deleted or deactivated."

**Meaning:** Survey no longer exists or active

**Actions:**
- Return to `/surveys`
- Look for other active surveys
- Contact creator for information

---

### "Connection error. Unable to submit your answer."

**Meaning:** Network problem during submission

**Actions:**
- Check internet connection
- Wait a moment
- Click **🔄 Retry** if available
- Or start survey over

---

## Diagnostic Steps

### Is the problem with the bot or Telegram?

**Test Telegram:**
1. Send message to another user
2. Try another bot
3. Check Telegram status page

**Test Bot:**
1. Try `/help` command (simplest)
2. Try `/start` command
3. Try `/surveys` command

**Results:**
- Telegram doesn't work → Telegram issue
- Other bots work but not SurveyBot → Bot issue
- `/help` works but `/surveys` fails → Database issue

### Is it a network problem?

**Check:**
1. Wi-Fi or mobile data connected?
2. Can you load websites in browser?
3. Do other apps work?
4. Speed test shows good connection?

**Try:**
1. Toggle airplane mode on/off
2. Switch Wi-Fi to mobile data
3. Restart router
4. Move to different location

### Is it a specific survey problem?

**Test:**
1. Does problem happen with all surveys?
2. Or just one specific survey?
3. Do other users report same issue?

**Actions:**
- All surveys: Bot/database issue
- One survey: Survey data issue, contact creator
- Only you: Account/device issue

---

## Advanced Troubleshooting

### Clear Bot Data (Telegram Desktop)

1. Right-click on bot chat
2. Select "Clear History"
3. Confirm
4. Type `/start` to restart

### Reinstall Telegram App

**Backup first:**
- Media in chats
- Important conversations

**Steps:**
1. Uninstall Telegram
2. Restart device
3. Reinstall from app store
4. Login to account
5. Find bot and start fresh

### Use Different Device

**Test if issue is device-specific:**
1. Try bot on different phone
2. Or use Telegram Desktop/Web
3. Or use tablet

**If works on other device:**
- Issue is device-specific
- Reinstall app on problem device
- Check device settings

### Check Bot Username

Make sure you're using correct bot:
- Official bot: **@YourSurveyBot** (verify with support)
- Watch for imposter bots
- Check bot profile for verification

---

## Prevention Tips

### Avoid Common Issues

**Before Taking Survey:**
1. Check estimated time
2. Ensure stable internet
3. Set aside uninterrupted time
4. Read instructions fully

**During Survey:**
1. Answer promptly (don't wait too long)
2. Don't close Telegram app
3. Maintain active connection
4. Save complex answers externally first

**For Survey Creators:**
1. Test survey before sharing
2. Keep surveys short (5-10 questions)
3. Activate survey before sharing
4. Monitor for issues

### Maintain Good Performance

1. Update Telegram regularly
2. Clear app cache monthly
3. Use stable internet connection
4. Avoid peak usage times
5. Report persistent issues

---

## When to Contact Support

### Definitely Contact Support If:

- Bot not responding for 30+ minutes
- Error persists after all troubleshooting
- Multiple users report same issue
- Data loss or corruption
- Security concerns
- Bug that prevents core functionality

### Information to Include:

1. **Your Telegram username**
2. **Exact error message** (screenshot)
3. **Steps to reproduce**
4. **When it started happening**
5. **What you've tried**
6. **Device and Telegram version**

### How to Contact Support:

- Email: support@surveybot.example.com
- In bot: Type `/support` (if available)
- Web: https://surveybot.example.com/support
- Include screenshots of errors

---

## Quick Troubleshooting Flowchart

```
Bot not responding?
├─ Try /start → Works? → Use bot normally
├─ Try /help → Works? → Database issue, report
├─ All fail? → Telegram/Bot offline
    ├─ Wait 5 minutes
    ├─ Try again
    └─ Contact support if persists

Survey won't load?
├─ Click refresh → Works? → Continue
├─ Try /surveys again → Works? → Continue
├─ All fail? → Network/Database issue
    ├─ Check connection
    ├─ Wait and retry
    └─ Report if persists

Can't submit answer?
├─ Check format → Correct? → Network issue
├─ Check length → Too long? → Shorten
├─ Check selection → Selected? → Click done
└─ Error message → Follow message instructions

Session expired?
├─ Restart survey
├─ Complete in one session
└─ Set aside time before starting
```

---

## Additional Resources

- **User Guide:** [BOT_USER_GUIDE.md](BOT_USER_GUIDE.md)
- **Command Reference:** [BOT_COMMAND_REFERENCE.md](BOT_COMMAND_REFERENCE.md)
- **FAQ:** [BOT_FAQ.md](BOT_FAQ.md)
- **Quick Start:** [BOT_QUICK_START.md](BOT_QUICK_START.md)

---

**Still having issues?** Contact support with detailed information about your problem.

_SurveyBot Troubleshooting Guide - Last Updated: November 2025_
