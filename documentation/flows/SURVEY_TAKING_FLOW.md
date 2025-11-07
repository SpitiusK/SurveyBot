# Survey Taking Flow

Complete guide to taking surveys in SurveyBot Phase 2.

## Overview

Users can take surveys through the Telegram bot interface. This document describes the complete flow from finding a survey to submitting responses.

## Flow Diagram

```
User browses surveys (/surveys)
    |
    v
User selects survey from list
    |
    v
Bot displays survey introduction
    |
    v
Bot presents first question
    |
    v
User answers question
    |
    v
Bot validates answer
    |
    +-- Invalid --> Show error, retry
    |
    +-- Valid --> Save answer
         |
         v
     More questions?
         |
         +-- Yes --> Present next question
         |
         +-- No --> Display completion message
              |
              v
          Survey completed
```

---

## Step 1: Finding Surveys

### Via /surveys Command

User types `/surveys` or clicks "Find Surveys" button.

**Bot Response:**
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

[📋 Customer Satisfaction Survey]
[📋 Product Feedback]
[◀️ Previous] [1/3] [Next ▶️]
```

### Via Direct Link (Future)

User receives a direct survey link:
```
https://t.me/YourBot?start=survey_123
```

Bot automatically starts that survey.

---

## Step 2: Survey Introduction

When user clicks a survey button, bot displays survey introduction.

**Callback Data:** `survey:take:123`

**Bot Response:**
```
📋 *Customer Satisfaction Survey*

Help us improve our service

This survey has 5 questions and takes approximately 3 minutes to complete.

*Instructions:*
- Answer all required questions (marked with *)
- You can skip optional questions
- Your responses are confidential
- You can't go back to previous questions

Ready to start?

[▶️ Start Survey] [❌ Cancel]
```

### Survey Information Displayed
- Survey title
- Description
- Number of questions
- Estimated completion time
- Instructions and guidelines

---

## Step 3: Question Presentation

Bot presents questions one at a time in order.

### Question Types

#### 1. Text Question

```
Question 1 of 5 *

What did you like most about our service?

Type your answer below:

[⏭️ Skip]  (if optional)
```

**User Input:** Free text message

**Example Answer:**
```
The customer support was excellent and very responsive.
```

#### 2. Single Choice Question

```
Question 2 of 5 *

How satisfied are you with our service?

[⭐⭐⭐⭐⭐ Very Satisfied]
[⭐⭐⭐⭐ Satisfied]
[⭐⭐⭐ Neutral]
[⭐⭐ Dissatisfied]
[⭐ Very Dissatisfied]
```

**User Input:** Click one button

**Selected:**
```
✓ Satisfied

Moving to next question...
```

#### 3. Multiple Choice Question

```
Question 3 of 5 *

Which features do you use most? (Select all that apply)

[ ] Feature A
[ ] Feature B
[ ] Feature C
[ ] Feature D

[✓ Done] [⏭️ Skip]
```

**User Input:** Click multiple buttons, then click "Done"

**Selected:**
```
✓ Feature A
✓ Feature C

Moving to next question...
```

#### 4. Rating Question

```
Question 4 of 5 *

Rate your overall experience:

[1⭐] [2⭐] [3⭐] [4⭐] [5⭐]
```

**User Input:** Click rating button

**Selected:**
```
✓ 5⭐

Moving to next question...
```

---

## Step 4: Answer Validation

Bot validates each answer before proceeding.

### Required Questions

If question is required and user tries to skip:

```
⚠️ This question is required.

Please provide an answer to continue.
```

### Text Question Validation

- **Minimum length:** 1 character
- **Maximum length:** 1000 characters
- **Empty answer:** Rejected if required

```
⚠️ Your answer is too long.

Please keep your answer under 1000 characters.
Current length: 1245 characters
```

### Choice Question Validation

- **Single choice:** Must select exactly one option
- **Multiple choice:** Must select at least one option

```
⚠️ Please select at least one option.
```

### Rating Validation

- **Valid values:** 1-5
- **Must select:** One rating value

---

## Step 5: Progress Tracking

Bot shows progress throughout the survey.

**Progress Indicator:**
```
Question 3 of 5

Progress: ▓▓▓▒▒ 60%
```

### Skip Functionality

For optional questions:

```
Question 3 of 5

What suggestions do you have for improvement?

Type your answer below:

[⏭️ Skip Question]
```

**If user clicks Skip:**
```
Skipped.

Moving to next question...
```

---

## Step 6: Survey Completion

After answering all questions, bot displays completion message.

```
✅ *Survey Completed!*

Thank you for completing the Customer Satisfaction Survey!

Your responses have been recorded successfully.

*Survey Summary:*
- Questions answered: 5/5
- Time taken: 3 minutes
- Completion: 100%

[📊 View Results] [🔙 Back to Surveys]
```

### Completion Actions

**View Results Button:**
- Shows aggregated statistics (if creator enables public results)
- Or shows message: "Results are private to survey creator"

**Back to Surveys Button:**
- Returns to `/surveys` command to take another survey

---

## Question Type Interaction Details

### Text Question Flow

```
1. Bot: Displays text question with input prompt
2. User: Types text message
3. Bot: Validates length and content
4. Bot: Saves answer
5. Bot: Moves to next question
```

**Edge Cases:**
- User sends photo/video instead of text: Bot prompts for text
- User sends multiple messages: Bot uses first message
- User types too much: Bot shows character limit error

### Single Choice Flow

```
1. Bot: Displays question with choice buttons
2. User: Clicks one button
3. Bot: Highlights selected choice
4. Bot: Saves answer automatically
5. Bot: Moves to next question (auto-advance)
```

**Features:**
- Auto-advance after selection (no "Next" button needed)
- Visual confirmation of selection with checkmark
- Can't select multiple options (buttons are radio buttons)

### Multiple Choice Flow

```
1. Bot: Displays question with checkboxes
2. User: Clicks multiple buttons to toggle selections
3. Bot: Updates checkmarks in real-time
4. User: Clicks "Done" button
5. Bot: Validates at least one selected
6. Bot: Saves answers
7. Bot: Moves to next question
```

**Features:**
- Toggle buttons (click to select/deselect)
- Visual checkmarks show selected state
- Must click "Done" to proceed (no auto-advance)
- Can select/deselect freely before clicking "Done"

### Rating Flow

```
1. Bot: Displays question with star buttons (1-5)
2. User: Clicks rating
3. Bot: Shows selected rating with stars
4. Bot: Saves rating
5. Bot: Moves to next question (auto-advance)
```

**Display:**
- 1 star: ⭐
- 2 stars: ⭐⭐
- 3 stars: ⭐⭐⭐
- 4 stars: ⭐⭐⭐⭐
- 5 stars: ⭐⭐⭐⭐⭐

---

## Error Handling

### Survey Not Found

```
❌ Survey not found

This survey may have been deleted or deactivated.

[🔙 Back to Surveys]
```

### Survey Inactive

```
❌ Survey is no longer active

This survey is not accepting responses at this time.

[🔙 Back to Surveys]
```

### Already Completed

```
✅ You've already completed this survey

Thank you for your previous response!

You can only take each survey once.

[📊 View Your Response] [🔙 Back to Surveys]
```

### Network Error

```
❌ Connection error

Unable to submit your answer. Please check your connection and try again.

[🔄 Retry] [❌ Cancel]
```

### Timeout

If user doesn't respond for 30 minutes:

```
⏱️ Survey session expired

Your session has timed out due to inactivity.

Would you like to start over?

[▶️ Restart Survey] [❌ Cancel]
```

---

## Data Storage Flow

### Answer Submission

Each answer is submitted to the API immediately after validation:

```
POST /api/responses/{responseId}/answers
{
  "questionId": 123,
  "answerText": "User's answer here",
  "selectedOptions": ["Option 1", "Option 2"]  // For choice questions
}
```

### Response Tracking

**Initial Response Creation:**
```
POST /api/responses
{
  "surveyId": 456,
  "userId": 789,
  "startedAt": "2025-11-07T10:00:00Z"
}

Response:
{
  "responseId": 111,
  "status": "InProgress"
}
```

**Complete Response:**
```
POST /api/responses/{responseId}/complete
{
  "completedAt": "2025-11-07T10:03:00Z"
}

Response:
{
  "status": "Completed",
  "answers": 5,
  "duration": 180  // seconds
}
```

---

## User Experience Enhancements

### Progress Saving

- Answers are saved immediately after each question
- User can close chat and resume later (future feature)
- No "Save Draft" button needed

### Quick Navigation

- Skip optional questions with one tap
- Auto-advance for single-select questions
- Clear visual feedback for all actions

### Accessibility

- Screen reader friendly button labels
- Clear question numbering
- Progress indicators
- Estimated completion time

### Mobile Optimization

- Large tap targets for buttons
- Minimal typing required
- Quick selection with inline keyboards
- Vertical scrolling for long option lists

---

## Testing Survey Taking Flow

### Manual Test Checklist

- [ ] Find survey via /surveys command
- [ ] Click survey to see introduction
- [ ] Start survey
- [ ] Answer text question
- [ ] Answer single choice question
- [ ] Answer multiple choice question (select 2+ options)
- [ ] Answer rating question
- [ ] Skip optional question
- [ ] Try to skip required question (should fail)
- [ ] Complete survey
- [ ] Verify completion message
- [ ] Try to take same survey again (should prevent)

### Test Data Setup

1. Create test survey with all question types
2. Activate survey
3. Test as different users
4. Verify responses in database

### Automated Testing

```csharp
[Fact]
public async Task SurveyTakingFlow_CompleteSurvey_ShouldSaveAllAnswers()
{
    // Arrange
    var survey = CreateTestSurvey();
    var user = CreateTestUser();

    // Act
    var response = await StartSurvey(survey.Id, user.Id);
    await AnswerQuestion(response.Id, question1.Id, "Text answer");
    await AnswerQuestion(response.Id, question2.Id, "Option A");
    await AnswerQuestion(response.Id, question3.Id, "5");
    await CompleteSurvey(response.Id);

    // Assert
    var savedResponse = await GetResponse(response.Id);
    Assert.Equal(ResponseStatus.Completed, savedResponse.Status);
    Assert.Equal(3, savedResponse.Answers.Count);
}
```

---

## API Endpoints Used

### Survey Discovery
- `GET /api/surveys` - List active surveys

### Survey Details
- `GET /api/surveys/{id}` - Get survey details
- `GET /api/surveys/{id}/questions` - Get questions

### Response Submission
- `POST /api/responses` - Start response
- `POST /api/responses/{id}/answers` - Submit answer
- `POST /api/responses/{id}/complete` - Complete survey
- `GET /api/responses/{id}` - Get response status

---

## Future Enhancements

### Planned Features (Post-MVP)

1. **Survey Resume**
   - Save progress and resume later
   - Send reminder if incomplete

2. **Answer Review**
   - Review answers before submission
   - Edit previous answers
   - Back button to previous question

3. **Rich Media**
   - Image questions
   - Video responses
   - Audio questions

4. **Conditional Logic**
   - Skip questions based on previous answers
   - Show different questions for different user segments

5. **Time Limits**
   - Per-question time limits
   - Survey-wide time limit
   - Show countdown timer

6. **Offline Support**
   - Cache questions
   - Submit when connection restored

7. **Multi-Language**
   - Detect user language
   - Translate surveys automatically

---

## Best Practices for Survey Takers

1. **Read Instructions** - Check survey introduction for special instructions
2. **Answer Honestly** - Provide genuine, thoughtful responses
3. **Complete in One Session** - Try to finish without long breaks
4. **Check Progress** - Monitor progress bar to see how far you've come
5. **Review Optional Questions** - Consider answering optional questions for better insights

---

## Best Practices for Survey Creators

1. **Keep It Short** - Aim for 5-10 questions max
2. **Clear Questions** - Write unambiguous, simple questions
3. **Logical Order** - Arrange questions in logical sequence
4. **Mix Question Types** - Use variety to maintain engagement
5. **Test First** - Take your own survey before sharing
6. **Mark Optional** - Clearly indicate which questions are optional
7. **Provide Context** - Add helpful descriptions to questions
8. **Thank Respondents** - Include completion message with gratitude

---

## Troubleshooting

### Problem: Bot Doesn't Show Questions

**Solution:**
- Verify survey is active
- Check survey has questions
- Verify bot has database access
- Check API connectivity

### Problem: Can't Submit Answer

**Solution:**
- Check answer meets validation rules
- Verify network connection
- Check API endpoint is accessible
- Review error message for specifics

### Problem: Survey Shows as Completed

**Solution:**
- Check if you've already taken the survey
- Verify with survey creator if multiple responses are allowed
- Try different survey

---

## Quick Reference

### Survey Taking Steps

1. Type `/surveys`
2. Browse available surveys
3. Click survey to view details
4. Click "Start Survey"
5. Answer each question
6. Click "Submit" or "Done" for each answer
7. Review completion message

### Question Type Actions

| Type | Action |
|------|--------|
| Text | Type message |
| Single Choice | Click one button (auto-advances) |
| Multiple Choice | Click multiple buttons + "Done" |
| Rating | Click star rating (auto-advances) |

### Progress Tracking

- Question counter: "Question 3 of 5"
- Progress bar: ▓▓▓▒▒ 60%
- Estimated time remaining (future)
