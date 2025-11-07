# QuestionsController Testing Guide

This document provides comprehensive cURL examples for testing all QuestionsController endpoints.

## Prerequisites

1. API must be running (typically on `https://localhost:5001` or `http://localhost:5000`)
2. You need a valid JWT token (obtain via `/api/auth/login`)
3. You need an existing survey ID (create one via `/api/surveys`)

## Environment Variables

Set these for easier testing:

```bash
# Windows PowerShell
$API_URL = "https://localhost:5001"
$TOKEN = "your_jwt_token_here"
$SURVEY_ID = "1"

# Linux/Mac Bash
export API_URL="https://localhost:5001"
export TOKEN="your_jwt_token_here"
export SURVEY_ID="1"
```

---

## 1. Add Question to Survey

**Endpoint:** `POST /api/surveys/{surveyId}/questions`

### Example 1.1: Add Text Question

```bash
curl -X POST "$API_URL/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "questionText": "What is your name?",
    "questionType": 0,
    "isRequired": true
  }'
```

### Example 1.2: Add Single Choice Question

```bash
curl -X POST "$API_URL/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "questionText": "What is your preferred programming language?",
    "questionType": 1,
    "isRequired": true,
    "options": ["C#", "Python", "JavaScript", "Java"]
  }'
```

### Example 1.3: Add Multiple Choice Question

```bash
curl -X POST "$API_URL/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "questionText": "Which technologies do you use? (Select all that apply)",
    "questionType": 2,
    "isRequired": false,
    "options": ["ASP.NET Core", "Entity Framework", "Docker", "Kubernetes", "Azure"]
  }'
```

### Example 1.4: Add Rating Question

```bash
curl -X POST "$API_URL/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "questionText": "How would you rate your experience?",
    "questionType": 3,
    "isRequired": true
  }'
```

### Expected Response (201 Created)

```json
{
  "success": true,
  "message": "Question created successfully",
  "data": {
    "id": 1,
    "surveyId": 1,
    "questionText": "What is your name?",
    "questionType": 0,
    "orderIndex": 1,
    "isRequired": true,
    "options": null,
    "createdAt": "2025-11-06T18:30:00Z",
    "updatedAt": "2025-11-06T18:30:00Z"
  },
  "timestamp": "2025-11-06T18:30:00Z"
}
```

### Error Cases

**400 Bad Request - Missing options for choice question:**
```json
{
  "success": false,
  "message": "Choice-based questions must have at least 2 options",
  "timestamp": "2025-11-06T18:30:00Z"
}
```

**403 Forbidden - User doesn't own survey:**
```json
{
  "success": false,
  "message": "You do not have permission to modify this survey",
  "timestamp": "2025-11-06T18:30:00Z"
}
```

**404 Not Found - Survey doesn't exist:**
```json
{
  "success": false,
  "message": "Survey with ID 999 was not found",
  "timestamp": "2025-11-06T18:30:00Z"
}
```

---

## 2. Update Question

**Endpoint:** `PUT /api/questions/{id}`

### Example 2.1: Update Text Question

```bash
curl -X PUT "$API_URL/api/questions/1" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "questionText": "What is your full name?",
    "questionType": 0,
    "isRequired": true
  }'
```

### Example 2.2: Update Single Choice Question with New Options

```bash
curl -X PUT "$API_URL/api/questions/2" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "questionText": "What is your preferred programming language?",
    "questionType": 1,
    "isRequired": true,
    "options": ["C#", "Python", "JavaScript", "Java", "Go", "Rust"]
  }'
```

### Expected Response (200 OK)

```json
{
  "success": true,
  "message": "Question updated successfully",
  "data": {
    "id": 1,
    "surveyId": 1,
    "questionText": "What is your full name?",
    "questionType": 0,
    "orderIndex": 1,
    "isRequired": true,
    "options": null,
    "createdAt": "2025-11-06T18:30:00Z",
    "updatedAt": "2025-11-06T18:35:00Z"
  },
  "timestamp": "2025-11-06T18:35:00Z"
}
```

### Error Cases

**400 Bad Request - Question has responses:**
```json
{
  "success": false,
  "message": "Cannot modify question that has existing responses",
  "timestamp": "2025-11-06T18:35:00Z"
}
```

**404 Not Found - Question doesn't exist:**
```json
{
  "success": false,
  "message": "Question with ID 999 was not found",
  "timestamp": "2025-11-06T18:35:00Z"
}
```

---

## 3. Delete Question

**Endpoint:** `DELETE /api/questions/{id}`

### Example 3.1: Delete Question

```bash
curl -X DELETE "$API_URL/api/questions/1" \
  -H "Authorization: Bearer $TOKEN"
```

### Expected Response (204 No Content)

No body returned on success.

### Error Cases

**400 Bad Request - Question has responses:**
```json
{
  "success": false,
  "message": "Cannot delete question that has existing responses",
  "timestamp": "2025-11-06T18:40:00Z"
}
```

**403 Forbidden:**
```json
{
  "success": false,
  "message": "You do not have permission to delete this question",
  "timestamp": "2025-11-06T18:40:00Z"
}
```

**404 Not Found:**
```json
{
  "success": false,
  "message": "Question with ID 999 was not found",
  "timestamp": "2025-11-06T18:40:00Z"
}
```

---

## 4. List Survey Questions

**Endpoint:** `GET /api/surveys/{surveyId}/questions`

### Example 4.1: Get All Questions (Authenticated)

```bash
curl -X GET "$API_URL/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN"
```

### Example 4.2: Get Questions (Public - Active Survey Only)

```bash
curl -X GET "$API_URL/api/surveys/$SURVEY_ID/questions"
```

### Expected Response (200 OK)

```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "surveyId": 1,
      "questionText": "What is your name?",
      "questionType": 0,
      "orderIndex": 1,
      "isRequired": true,
      "options": null,
      "createdAt": "2025-11-06T18:30:00Z",
      "updatedAt": "2025-11-06T18:30:00Z"
    },
    {
      "id": 2,
      "surveyId": 1,
      "questionText": "What is your preferred programming language?",
      "questionType": 1,
      "orderIndex": 2,
      "isRequired": true,
      "options": ["C#", "Python", "JavaScript", "Java"],
      "createdAt": "2025-11-06T18:31:00Z",
      "updatedAt": "2025-11-06T18:31:00Z"
    },
    {
      "id": 3,
      "surveyId": 1,
      "questionText": "How would you rate your experience?",
      "questionType": 3,
      "orderIndex": 3,
      "isRequired": true,
      "options": null,
      "createdAt": "2025-11-06T18:32:00Z",
      "updatedAt": "2025-11-06T18:32:00Z"
    }
  ],
  "timestamp": "2025-11-06T18:45:00Z"
}
```

### Error Cases

**403 Forbidden - Inactive survey without ownership:**
```json
{
  "success": false,
  "message": "You do not have permission to view questions for this inactive survey",
  "timestamp": "2025-11-06T18:45:00Z"
}
```

**404 Not Found:**
```json
{
  "success": false,
  "message": "Survey with ID 999 was not found",
  "timestamp": "2025-11-06T18:45:00Z"
}
```

---

## 5. Reorder Questions

**Endpoint:** `POST /api/surveys/{surveyId}/questions/reorder`

### Example 5.1: Reorder Questions

```bash
curl -X POST "$API_URL/api/surveys/$SURVEY_ID/questions/reorder" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "questionIds": [3, 1, 2]
  }'
```

This changes the order from [1, 2, 3] to [3, 1, 2], so:
- Question 3 becomes first (orderIndex = 1)
- Question 1 becomes second (orderIndex = 2)
- Question 2 becomes third (orderIndex = 3)

### Expected Response (200 OK)

```json
{
  "success": true,
  "message": "Questions reordered successfully",
  "timestamp": "2025-11-06T18:50:00Z"
}
```

### Error Cases

**400 Bad Request - Not all questions included:**
```json
{
  "success": false,
  "message": "All questions must be included in the reorder operation",
  "timestamp": "2025-11-06T18:50:00Z"
}
```

**400 Bad Request - Question doesn't belong to survey:**
```json
{
  "success": false,
  "message": "Question with ID 999 does not belong to this survey",
  "timestamp": "2025-11-06T18:50:00Z"
}
```

**403 Forbidden:**
```json
{
  "success": false,
  "message": "You do not have permission to modify this survey",
  "timestamp": "2025-11-06T18:50:00Z"
}
```

---

## Question Type Values

When creating or updating questions, use these integer values for `questionType`:

| Type | Value | Options Required? |
|------|-------|-------------------|
| Text | 0 | No |
| SingleChoice | 1 | Yes (2-10 options) |
| MultipleChoice | 2 | Yes (2-10 options) |
| Rating | 3 | No (always 1-5) |

---

## Validation Rules

### Question Text
- Required
- Min length: 3 characters
- Max length: 1000 characters

### Options (for choice-based questions)
- Required for SingleChoice and MultipleChoice
- Must have 2-10 options
- Each option max length: 200 characters
- All options must have text (no empty strings)
- Should NOT be provided for Text or Rating questions

### IsRequired
- Boolean field
- Defaults to true if not specified

---

## Complete Testing Workflow

```bash
# 1. Login and get token
TOKEN=$(curl -X POST "$API_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' \
  | jq -r '.data.token')

# 2. Create a survey
SURVEY_ID=$(curl -X POST "$API_URL/api/surveys" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Test Survey","description":"Testing questions"}' \
  | jq -r '.data.id')

# 3. Add text question
Q1_ID=$(curl -X POST "$API_URL/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"questionText":"What is your name?","questionType":0,"isRequired":true}' \
  | jq -r '.data.id')

# 4. Add single choice question
Q2_ID=$(curl -X POST "$API_URL/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"questionText":"Favorite language?","questionType":1,"isRequired":true,"options":["C#","Python","Java"]}' \
  | jq -r '.data.id')

# 5. Add rating question
Q3_ID=$(curl -X POST "$API_URL/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"questionText":"Rate your experience","questionType":3,"isRequired":true}' \
  | jq -r '.data.id')

# 6. List all questions
curl -X GET "$API_URL/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN"

# 7. Reorder questions (3, 1, 2)
curl -X POST "$API_URL/api/surveys/$SURVEY_ID/questions/reorder" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"questionIds\":[$Q3_ID,$Q1_ID,$Q2_ID]}"

# 8. Update question
curl -X PUT "$API_URL/api/questions/$Q1_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"questionText":"What is your full name?","questionType":0,"isRequired":true}'

# 9. Delete question
curl -X DELETE "$API_URL/api/questions/$Q3_ID" \
  -H "Authorization: Bearer $TOKEN"

# 10. Verify deletion
curl -X GET "$API_URL/api/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN"
```

---

## Notes

1. **Authorization**: All write operations (POST, PUT, DELETE) require authentication
2. **Public Access**: GET questions endpoint is public for active surveys
3. **Validation**: Custom validation runs for options based on question type
4. **Responses**: Once a question has responses, it cannot be modified or deleted
5. **Order**: Questions are always returned in orderIndex order
6. **Survey State**: Cannot add questions to completed surveys

---

## Swagger Documentation

Once the API is running, visit:
- Swagger UI: `https://localhost:5001/swagger`
- OpenAPI JSON: `https://localhost:5001/swagger/v1/swagger.json`

All endpoints are fully documented with:
- Request/response schemas
- Example values
- HTTP status codes
- Error responses
