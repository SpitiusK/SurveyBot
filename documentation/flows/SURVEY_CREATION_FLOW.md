# Survey Creation Flow

Complete guide to creating and managing surveys in SurveyBot Phase 2.

## Overview

Survey creation in Phase 2 MVP is primarily done through the API endpoints. Future phases will include a conversational bot flow and web interface.

## Creation Methods

### 1. API Endpoints (Phase 2 - Available Now)
Direct API calls for programmatic survey creation

### 2. Web Interface (Future)
User-friendly web dashboard for survey management

### 3. Bot Conversation Flow (Future)
Interactive bot conversation for creating surveys

---

## API-Based Survey Creation

### Step 1: Authenticate

```bash
# Login to get JWT token
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 123456789}'

# Response
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "userId": 1
  }
}

# Save token for subsequent requests
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Step 2: Create Survey

```bash
curl -X POST http://localhost:5000/api/surveys \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Customer Satisfaction Survey",
    "description": "Help us improve our service"
  }'

# Response
{
  "success": true,
  "message": "Survey created successfully",
  "data": {
    "id": 1,
    "title": "Customer Satisfaction Survey",
    "description": "Help us improve our service",
    "isActive": false,
    "createdBy": 1,
    "createdAt": "2025-11-07T10:00:00Z",
    "questions": []
  }
}

# Save survey ID
SURVEY_ID=1
```

### Step 3: Add Questions

#### Add Text Question

```bash
curl -X POST http://localhost:5000/api/surveys/$SURVEY_ID/questions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "What did you like most about our service?",
    "type": "Text",
    "isRequired": false
  }'
```

#### Add Single Choice Question

```bash
curl -X POST http://localhost:5000/api/surveys/$SURVEY_ID/questions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "How satisfied are you with our service?",
    "type": "SingleChoice",
    "isRequired": true,
    "options": [
      "Very Satisfied",
      "Satisfied",
      "Neutral",
      "Dissatisfied",
      "Very Dissatisfied"
    ]
  }'
```

#### Add Multiple Choice Question

```bash
curl -X POST http://localhost:5000/api/surveys/$SURVEY_ID/questions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Which features do you use? (Select all that apply)",
    "type": "MultipleChoice",
    "isRequired": true,
    "options": [
      "Feature A",
      "Feature B",
      "Feature C",
      "Feature D"
    ]
  }'
```

#### Add Rating Question

```bash
curl -X POST http://localhost:5000/api/surveys/$SURVEY_ID/questions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Rate your overall experience",
    "type": "Rating",
    "isRequired": true
  }'
```

### Step 4: Review Survey

```bash
# Get survey with all questions
curl -X GET http://localhost:5000/api/surveys/$SURVEY_ID \
  -H "Authorization: Bearer $TOKEN"

# Response shows complete survey structure
{
  "success": true,
  "data": {
    "id": 1,
    "title": "Customer Satisfaction Survey",
    "description": "Help us improve our service",
    "isActive": false,
    "questions": [
      {
        "id": 1,
        "text": "What did you like most about our service?",
        "type": "Text",
        "isRequired": false,
        "orderIndex": 0
      },
      {
        "id": 2,
        "text": "How satisfied are you with our service?",
        "type": "SingleChoice",
        "isRequired": true,
        "orderIndex": 1,
        "options": ["Very Satisfied", "Satisfied", "Neutral", "Dissatisfied", "Very Dissatisfied"]
      }
    ]
  }
}
```

### Step 5: Activate Survey

```bash
curl -X POST http://localhost:5000/api/surveys/$SURVEY_ID/activate \
  -H "Authorization: Bearer $TOKEN"

# Response
{
  "success": true,
  "message": "Survey activated successfully",
  "data": {
    "id": 1,
    "isActive": true
  }
}
```

---

## Question Types

### 1. Text Question

**Purpose:** Free-form text responses

**Configuration:**
```json
{
  "text": "What suggestions do you have?",
  "type": "Text",
  "isRequired": false
}
```

**Validation:**
- Text: 1-500 characters
- No options field required
- User can enter any text

**Best For:**
- Open-ended feedback
- Suggestions
- Comments
- Detailed explanations

### 2. Single Choice Question

**Purpose:** Select one option from multiple choices

**Configuration:**
```json
{
  "text": "What is your age group?",
  "type": "SingleChoice",
  "isRequired": true,
  "options": [
    "18-24",
    "25-34",
    "35-44",
    "45-54",
    "55+"
  ]
}
```

**Validation:**
- Options: Required, 2-10 options
- Each option: 1-200 characters
- User must select exactly one

**Best For:**
- Demographics
- Satisfaction scales
- Yes/No questions
- Category selection

### 3. Multiple Choice Question

**Purpose:** Select multiple options from a list

**Configuration:**
```json
{
  "text": "Which benefits are important to you?",
  "type": "MultipleChoice",
  "isRequired": true,
  "options": [
    "Health Insurance",
    "Remote Work",
    "Flexible Hours",
    "Professional Development",
    "Gym Membership"
  ]
}
```

**Validation:**
- Options: Required, 2-10 options
- Each option: 1-200 characters
- User must select at least one

**Best For:**
- Feature preferences
- Multiple selections
- "Select all that apply" questions
- Interest surveys

### 4. Rating Question

**Purpose:** Rate on a 1-5 scale

**Configuration:**
```json
{
  "text": "Rate your overall experience",
  "type": "Rating",
  "isRequired": true
}
```

**Validation:**
- No options field needed
- Always 1-5 scale
- User must select one rating

**Best For:**
- Satisfaction ratings
- Quality assessments
- Experience evaluation
- NPS-style questions

---

## Survey Management Operations

### Update Survey

```bash
curl -X PUT http://localhost:5000/api/surveys/$SURVEY_ID \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Updated Survey Title",
    "description": "Updated description"
  }'
```

**Restrictions:**
- Cannot update if survey has responses and is active
- Can update title and description anytime if inactive

### Update Question

```bash
curl -X PUT http://localhost:5000/api/questions/$QUESTION_ID \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Updated question text",
    "type": "SingleChoice",
    "isRequired": true,
    "options": ["Option 1", "Option 2", "Option 3"]
  }'
```

**Restrictions:**
- Cannot update if question has responses
- Must provide all fields (text, type, isRequired, options if applicable)

### Reorder Questions

```bash
curl -X POST http://localhost:5000/api/surveys/$SURVEY_ID/questions/reorder \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "questionIds": [3, 1, 2, 4]
  }'
```

**Rules:**
- Must include all question IDs from the survey
- New order is applied based on array position
- Can reorder at any time

### Delete Question

```bash
curl -X DELETE http://localhost:5000/api/questions/$QUESTION_ID \
  -H "Authorization: Bearer $TOKEN"

# Response: 204 No Content
```

**Restrictions:**
- Cannot delete if question has responses
- Question is permanently removed

### Deactivate Survey

```bash
curl -X POST http://localhost:5000/api/surveys/$SURVEY_ID/deactivate \
  -H "Authorization: Bearer $TOKEN"

# Response
{
  "success": true,
  "message": "Survey deactivated successfully",
  "data": {
    "id": 1,
    "isActive": false
  }
}
```

**Effect:**
- Stops accepting new responses
- Existing responses preserved
- Survey not visible in /surveys command

### Delete Survey

```bash
curl -X DELETE http://localhost:5000/api/surveys/$SURVEY_ID \
  -H "Authorization: Bearer $TOKEN"

# Response: 204 No Content
```

**Behavior:**
- Soft delete if survey has responses (deactivates)
- Hard delete if survey has no responses (permanently removes)

---

## Best Practices

### Survey Design

1. **Clear Title**
   - Descriptive and concise (max 200 chars)
   - Indicates survey purpose
   - Example: "Q4 2025 Employee Satisfaction Survey"

2. **Helpful Description**
   - Explain survey purpose
   - Mention estimated time
   - State confidentiality policy
   - Example: "Help us improve our workplace. This takes 5 minutes. Your responses are confidential."

3. **Question Ordering**
   - Start with easy, non-sensitive questions
   - Group related questions together
   - End with demographics if needed
   - Save open-ended questions for last

4. **Question Clarity**
   - One concept per question
   - Avoid jargon and technical terms
   - Keep questions short (under 100 chars)
   - Avoid double negatives

5. **Response Options**
   - Provide balanced scales (equal positive/negative)
   - Include "Other" or "N/A" when appropriate
   - Order logically (chronologically, by frequency, etc.)
   - Keep option text concise

6. **Survey Length**
   - MVP recommendation: 5-10 questions
   - Maximum engagement: Under 5 minutes
   - Consider response fatigue
   - Test completion rate

### Technical Best Practices

1. **Create Draft First**
   - Create survey in inactive state
   - Add all questions
   - Review and test
   - Then activate

2. **Test Before Launch**
   - Take your own survey
   - Verify question flow
   - Check all question types work
   - Test on mobile device

3. **Version Control**
   - Don't modify active surveys with responses
   - Create new version if major changes needed
   - Document survey changes

4. **Data Collection**
   - Mark critical questions as required
   - Make most questions optional for higher completion
   - Balance data needs with user experience

---

## Validation Rules

### Survey Validation

**Title:**
- Required
- 1-200 characters
- Cannot be empty or whitespace

**Description:**
- Optional
- Maximum 1000 characters

**Activation:**
- Must have at least one question
- Survey must exist and user must own it

### Question Validation

**Text:**
- Required
- 1-500 characters
- Cannot be empty

**Type:**
- Must be valid enum value: Text, SingleChoice, MultipleChoice, Rating
- Case-insensitive

**Options:**
- Required for SingleChoice and MultipleChoice
- Not allowed for Text and Rating
- 2-10 options
- Each option: 1-200 characters
- No duplicate options

**IsRequired:**
- Boolean value
- Defaults to true if not specified

---

## Survey Statistics

### Get Statistics

```bash
curl -X GET http://localhost:5000/api/surveys/$SURVEY_ID/statistics \
  -H "Authorization: Bearer $TOKEN"

# Response
{
  "success": true,
  "data": {
    "surveyId": 1,
    "surveyTitle": "Customer Satisfaction Survey",
    "totalResponses": 42,
    "completedResponses": 40,
    "averageCompletionTime": 180,
    "questionStatistics": [
      {
        "questionId": 1,
        "questionText": "How satisfied are you?",
        "questionType": "SingleChoice",
        "totalAnswers": 40,
        "choiceStatistics": {
          "totalResponses": 40,
          "choices": [
            {
              "choiceText": "Very Satisfied",
              "count": 25,
              "percentage": 62.5
            },
            {
              "choiceText": "Satisfied",
              "count": 10,
              "percentage": 25.0
            },
            {
              "choiceText": "Neutral",
              "count": 5,
              "percentage": 12.5
            }
          ]
        }
      }
    ]
  }
}
```

### Statistics Available

**Survey-Level:**
- Total responses received
- Completed vs incomplete responses
- Average completion time
- Response rate over time

**Question-Level:**
- Total answers per question
- For choice questions: Distribution of selections
- For rating questions: Average rating and distribution
- For text questions: Count of responses

---

## Complete Example: Create Survey Script

```bash
#!/bin/bash

# Configuration
API_BASE="http://localhost:5000"
TELEGRAM_ID=123456789

# Step 1: Login
echo "Logging in..."
LOGIN_RESPONSE=$(curl -s -X POST "$API_BASE/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"telegramId\": $TELEGRAM_ID}")

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.data.accessToken')
echo "Token: $TOKEN"

# Step 2: Create Survey
echo "Creating survey..."
SURVEY_RESPONSE=$(curl -s -X POST "$API_BASE/api/surveys" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Product Feedback Survey",
    "description": "Help us improve our product"
  }')

SURVEY_ID=$(echo $SURVEY_RESPONSE | jq -r '.data.id')
echo "Survey ID: $SURVEY_ID"

# Step 3: Add Questions
echo "Adding text question..."
curl -s -X POST "$API_BASE/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "What features would you like to see?",
    "type": "Text",
    "isRequired": false
  }'

echo "Adding single choice question..."
curl -s -X POST "$API_BASE/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "How likely are you to recommend us?",
    "type": "SingleChoice",
    "isRequired": true,
    "options": ["Very Likely", "Likely", "Neutral", "Unlikely", "Very Unlikely"]
  }'

echo "Adding rating question..."
curl -s -X POST "$API_BASE/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Rate your overall experience",
    "type": "Rating",
    "isRequired": true
  }'

# Step 4: Activate Survey
echo "Activating survey..."
curl -s -X POST "$API_BASE/api/surveys/$SURVEY_ID/activate" \
  -H "Authorization: Bearer $TOKEN"

echo "Survey created and activated successfully!"
echo "Survey ID: $SURVEY_ID"
```

---

## Troubleshooting

### Problem: Can't Create Survey

**Error:** `401 Unauthorized`

**Solution:**
- Verify JWT token is valid
- Check Authorization header format: `Bearer <token>`
- Re-login to get fresh token

---

**Error:** `400 Bad Request - Title is required`

**Solution:**
- Provide title in request body
- Ensure title is not empty
- Check title length (max 200 chars)

---

### Problem: Can't Add Question

**Error:** `400 Bad Request - Options are required for choice-based questions`

**Solution:**
- Add options array for SingleChoice/MultipleChoice
- Provide 2-10 options
- Don't include options for Text/Rating questions

---

**Error:** `403 Forbidden - You don't have permission`

**Solution:**
- Verify you own the survey
- Check userId in token matches survey creator
- Don't try to modify other users' surveys

---

### Problem: Can't Activate Survey

**Error:** `400 Bad Request - Survey must have at least one question`

**Solution:**
- Add at least one question before activating
- Verify questions were created successfully
- Check GET /api/surveys/{id} to see questions

---

### Problem: Can't Update Survey

**Error:** `400 Bad Request - Cannot modify active survey with responses`

**Solution:**
- Deactivate survey first
- Or create new version of survey
- Active surveys with responses are locked

---

## Future Features (Post-MVP)

### Bot-Based Creation Flow

Conversational interface for creating surveys:

```
Bot: What would you like to name your survey?
User: Customer Feedback Survey

Bot: Great! Now add a description (or type /skip)
User: We want to improve our service

Bot: Perfect! Now let's add questions. What's the first question?
User: How satisfied are you with our service?

Bot: What type of question is this?
[Text] [Single Choice] [Multiple Choice] [Rating]

User: [Single Choice]

Bot: Please provide the answer options (one per message, then type /done)
User: Very Satisfied
User: Satisfied
User: Neutral
User: Dissatisfied
User: /done

Bot: Question added! Add another question or /finish to complete
```

### Web Interface

Drag-and-drop survey builder with:
- Visual question editor
- Real-time preview
- Template library
- Question bank
- Logic builder
- Theme customization

### Advanced Features

- **Question Logic:** Show/hide questions based on answers
- **Question Piping:** Reference previous answers
- **Custom Validation:** Advanced validation rules
- **Question Branching:** Different paths for different users
- **File Upload:** Allow file attachments as answers
- **Matrix Questions:** Grid of questions with same options
- **Ranking Questions:** Drag to rank items
