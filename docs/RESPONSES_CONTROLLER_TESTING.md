# ResponsesController Testing Guide

## Overview
Complete testing guide for the ResponsesController endpoints with cURL examples.

**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\Controllers\ResponsesController.cs`

## Prerequisites
- API running on: `https://localhost:5001` (or your configured port)
- Valid JWT token for authenticated endpoints
- Active survey with at least one question
- SQL Server database running

---

## Endpoint 1: GET /api/surveys/{surveyId}/responses
**Purpose:** List all responses for a survey (Creator only)

### Authorization
Required: YES (Bearer token - must be survey creator)

### Query Parameters
- `pageNumber` (optional, default: 1) - Page number (1-based)
- `pageSize` (optional, default: 20) - Items per page (1-100)
- `completedOnly` (optional, default: false) - Show only completed responses

### Success Response (200 OK)
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "surveyId": 1,
        "respondentTelegramId": 123456789,
        "respondentUsername": "john_doe",
        "respondentFirstName": "John",
        "isComplete": true,
        "startedAt": "2025-11-07T10:00:00Z",
        "submittedAt": "2025-11-07T10:05:00Z",
        "answeredCount": 3,
        "totalQuestions": 3,
        "answers": []
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1
  },
  "timestamp": "2025-11-07T12:00:00Z"
}
```

### Error Responses
- **400 Bad Request** - Invalid pagination parameters
- **401 Unauthorized** - Missing or invalid token
- **403 Forbidden** - User doesn't own the survey
- **404 Not Found** - Survey doesn't exist

### cURL Examples

#### Get all responses (first page)
```bash
curl -X GET "https://localhost:5001/api/surveys/1/responses?pageNumber=1&pageSize=20" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  --insecure
```

#### Get only completed responses
```bash
curl -X GET "https://localhost:5001/api/surveys/1/responses?completedOnly=true" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  --insecure
```

#### Get with custom pagination
```bash
curl -X GET "https://localhost:5001/api/surveys/1/responses?pageNumber=2&pageSize=10" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  --insecure
```

---

## Endpoint 2: GET /api/responses/{id}
**Purpose:** Get a single response with all answers

### Authorization
Required: YES (Bearer token - must be survey creator)

### URL Parameters
- `id` - Response ID

### Success Response (200 OK)
```json
{
  "success": true,
  "data": {
    "id": 1,
    "surveyId": 1,
    "respondentTelegramId": 123456789,
    "respondentUsername": "john_doe",
    "respondentFirstName": "John",
    "isComplete": true,
    "startedAt": "2025-11-07T10:00:00Z",
    "submittedAt": "2025-11-07T10:05:00Z",
    "answeredCount": 3,
    "totalQuestions": 3,
    "answers": [
      {
        "id": 1,
        "questionId": 1,
        "questionText": "What is your name?",
        "questionType": "Text",
        "answerText": "John Doe",
        "selectedOptions": null,
        "ratingValue": null
      },
      {
        "id": 2,
        "questionId": 2,
        "questionText": "Select your favorite color",
        "questionType": "SingleChoice",
        "answerText": null,
        "selectedOptions": ["Blue"],
        "ratingValue": null
      }
    ]
  },
  "timestamp": "2025-11-07T12:00:00Z"
}
```

### Error Responses
- **401 Unauthorized** - Missing or invalid token
- **403 Forbidden** - User cannot access this response
- **404 Not Found** - Response doesn't exist

### cURL Example
```bash
curl -X GET "https://localhost:5001/api/responses/1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  --insecure
```

---

## Endpoint 3: POST /api/surveys/{surveyId}/responses
**Purpose:** Create a new response (start taking survey)

### Authorization
Required: NO (Public endpoint for Telegram bot)

### URL Parameters
- `surveyId` - Survey ID

### Request Body
```json
{
  "respondentTelegramId": 123456789,
  "respondentUsername": "john_doe",
  "respondentFirstName": "John"
}
```

### Success Response (201 Created)
```json
{
  "success": true,
  "data": {
    "id": 1,
    "surveyId": 1,
    "respondentTelegramId": 123456789,
    "respondentUsername": "john_doe",
    "respondentFirstName": "John",
    "isComplete": false,
    "startedAt": "2025-11-07T10:00:00Z",
    "submittedAt": null,
    "answeredCount": 0,
    "totalQuestions": 3,
    "answers": []
  },
  "message": "Response created successfully",
  "timestamp": "2025-11-07T12:00:00Z"
}
```

### Error Responses
- **400 Bad Request** - Invalid data or survey not active
- **404 Not Found** - Survey doesn't exist
- **409 Conflict** - User already completed this survey

### cURL Examples

#### Create new response
```bash
curl -X POST "https://localhost:5001/api/surveys/1/responses" \
  -H "Content-Type: application/json" \
  -d "{\"respondentTelegramId\":123456789,\"respondentUsername\":\"john_doe\",\"respondentFirstName\":\"John\"}" \
  --insecure
```

#### Create response with minimal data
```bash
curl -X POST "https://localhost:5001/api/surveys/1/responses" \
  -H "Content-Type: application/json" \
  -d "{\"respondentTelegramId\":987654321}" \
  --insecure
```

---

## Endpoint 4: POST /api/responses/{id}/answers
**Purpose:** Save an answer to a question

### Authorization
Required: NO (Public endpoint for Telegram bot)

### URL Parameters
- `id` - Response ID

### Request Body
```json
{
  "answer": {
    "questionId": 1,
    "answerText": "This is my text answer",
    "selectedOptions": null,
    "ratingValue": null
  }
}
```

### Success Response (200 OK)
```json
{
  "success": true,
  "data": {
    "id": 1,
    "surveyId": 1,
    "respondentTelegramId": 123456789,
    "respondentUsername": "john_doe",
    "respondentFirstName": "John",
    "isComplete": false,
    "startedAt": "2025-11-07T10:00:00Z",
    "submittedAt": null,
    "answeredCount": 1,
    "totalQuestions": 3,
    "answers": [
      {
        "id": 1,
        "questionId": 1,
        "questionText": "What is your name?",
        "questionType": "Text",
        "answerText": "This is my text answer",
        "selectedOptions": null,
        "ratingValue": null
      }
    ]
  },
  "message": "Answer saved successfully",
  "timestamp": "2025-11-07T12:00:00Z"
}
```

### Error Responses
- **400 Bad Request** - Invalid answer format
- **404 Not Found** - Response or question not found

### cURL Examples

#### Save text answer
```bash
curl -X POST "https://localhost:5001/api/responses/1/answers" \
  -H "Content-Type: application/json" \
  -d "{\"answer\":{\"questionId\":1,\"answerText\":\"John Doe\"}}" \
  --insecure
```

#### Save single choice answer
```bash
curl -X POST "https://localhost:5001/api/responses/1/answers" \
  -H "Content-Type: application/json" \
  -d "{\"answer\":{\"questionId\":2,\"selectedOptions\":[\"Blue\"]}}" \
  --insecure
```

#### Save multiple choice answer
```bash
curl -X POST "https://localhost:5001/api/responses/1/answers" \
  -H "Content-Type: application/json" \
  -d "{\"answer\":{\"questionId\":3,\"selectedOptions\":[\"Red\",\"Blue\",\"Green\"]}}" \
  --insecure
```

#### Save rating answer
```bash
curl -X POST "https://localhost:5001/api/responses/1/answers" \
  -H "Content-Type: application/json" \
  -d "{\"answer\":{\"questionId\":4,\"ratingValue\":5}}" \
  --insecure
```

---

## Endpoint 5: POST /api/responses/{id}/complete
**Purpose:** Mark response as completed

### Authorization
Required: NO (Public endpoint for Telegram bot)

### URL Parameters
- `id` - Response ID

### Request Body
None

### Success Response (200 OK)
```json
{
  "success": true,
  "data": {
    "id": 1,
    "surveyId": 1,
    "respondentTelegramId": 123456789,
    "respondentUsername": "john_doe",
    "respondentFirstName": "John",
    "isComplete": true,
    "startedAt": "2025-11-07T10:00:00Z",
    "submittedAt": "2025-11-07T10:05:00Z",
    "answeredCount": 3,
    "totalQuestions": 3,
    "answers": [...]
  },
  "message": "Response completed successfully",
  "timestamp": "2025-11-07T12:00:00Z"
}
```

### Error Responses
- **404 Not Found** - Response doesn't exist
- **409 Conflict** - Response already completed

### cURL Example
```bash
curl -X POST "https://localhost:5001/api/responses/1/complete" \
  -H "Content-Type: application/json" \
  --insecure
```

---

## Complete Test Workflow

### Step 1: Get JWT Token (for creator endpoints)
```bash
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"test@example.com\",\"password\":\"Test123!\"}" \
  --insecure
```
Save the token from the response.

### Step 2: Create a survey (if needed)
```bash
curl -X POST "https://localhost:5001/api/surveys" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Customer Satisfaction Survey\",\"description\":\"Please share your feedback\"}" \
  --insecure
```

### Step 3: Add questions to survey
```bash
# Add Text question
curl -X POST "https://localhost:5001/api/surveys/1/questions" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"text\":\"What is your name?\",\"type\":\"Text\",\"isRequired\":true,\"order\":1}" \
  --insecure

# Add SingleChoice question
curl -X POST "https://localhost:5001/api/surveys/1/questions" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"text\":\"Select your favorite color\",\"type\":\"SingleChoice\",\"isRequired\":true,\"order\":2,\"optionsJson\":\"[\\\"Red\\\",\\\"Blue\\\",\\\"Green\\\"]\"}" \
  --insecure

# Add Rating question
curl -X POST "https://localhost:5001/api/surveys/1/questions" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"text\":\"Rate our service\",\"type\":\"Rating\",\"isRequired\":true,\"order\":3}" \
  --insecure
```

### Step 4: Activate survey
```bash
curl -X POST "https://localhost:5001/api/surveys/1/activate" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  --insecure
```

### Step 5: Start a response (as respondent)
```bash
curl -X POST "https://localhost:5001/api/surveys/1/responses" \
  -H "Content-Type: application/json" \
  -d "{\"respondentTelegramId\":123456789,\"respondentUsername\":\"test_user\",\"respondentFirstName\":\"Test\"}" \
  --insecure
```
Save the response ID from the result (e.g., id: 1).

### Step 6: Submit answers
```bash
# Answer question 1 (Text)
curl -X POST "https://localhost:5001/api/responses/1/answers" \
  -H "Content-Type: application/json" \
  -d "{\"answer\":{\"questionId\":1,\"answerText\":\"Test User\"}}" \
  --insecure

# Answer question 2 (SingleChoice)
curl -X POST "https://localhost:5001/api/responses/1/answers" \
  -H "Content-Type: application/json" \
  -d "{\"answer\":{\"questionId\":2,\"selectedOptions\":[\"Blue\"]}}" \
  --insecure

# Answer question 3 (Rating)
curl -X POST "https://localhost:5001/api/responses/1/answers" \
  -H "Content-Type: application/json" \
  -d "{\"answer\":{\"questionId\":3,\"ratingValue\":5}}" \
  --insecure
```

### Step 7: Complete the response
```bash
curl -X POST "https://localhost:5001/api/responses/1/complete" \
  -H "Content-Type: application/json" \
  --insecure
```

### Step 8: View responses (as creator)
```bash
# Get all responses for the survey
curl -X GET "https://localhost:5001/api/surveys/1/responses" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  --insecure

# Get specific response details
curl -X GET "https://localhost:5001/api/responses/1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  --insecure
```

---

## Validation Test Cases

### Test Case 1: Invalid Pagination
```bash
# Page size too large
curl -X GET "https://localhost:5001/api/surveys/1/responses?pageSize=200" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  --insecure
# Expected: 400 Bad Request

# Page number less than 1
curl -X GET "https://localhost:5001/api/surveys/1/responses?pageNumber=0" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  --insecure
# Expected: 400 Bad Request
```

### Test Case 2: Unauthorized Access
```bash
# Try to view responses without token
curl -X GET "https://localhost:5001/api/surveys/1/responses" \
  --insecure
# Expected: 401 Unauthorized

# Try to view another user's survey responses
curl -X GET "https://localhost:5001/api/surveys/999/responses" \
  -H "Authorization: Bearer ANOTHER_USER_TOKEN" \
  --insecure
# Expected: 403 Forbidden (if survey exists but not owned)
```

### Test Case 3: Inactive Survey
```bash
# Try to start response on inactive survey
curl -X POST "https://localhost:5001/api/surveys/1/responses" \
  -H "Content-Type: application/json" \
  -d "{\"respondentTelegramId\":123456789}" \
  --insecure
# Expected: 400 Bad Request (if survey is inactive)
```

### Test Case 4: Duplicate Response
```bash
# Try to start another response after completing one
curl -X POST "https://localhost:5001/api/surveys/1/responses" \
  -H "Content-Type: application/json" \
  -d "{\"respondentTelegramId\":123456789}" \
  --insecure
# Expected: 409 Conflict
```

### Test Case 5: Invalid Answer Format
```bash
# Text question with rating value
curl -X POST "https://localhost:5001/api/responses/1/answers" \
  -H "Content-Type: application/json" \
  -d "{\"answer\":{\"questionId\":1,\"ratingValue\":5}}" \
  --insecure
# Expected: 400 Bad Request

# Rating out of range
curl -X POST "https://localhost:5001/api/responses/1/answers" \
  -H "Content-Type: application/json" \
  -d "{\"answer\":{\"questionId\":3,\"ratingValue\":10}}" \
  --insecure
# Expected: 400 Bad Request
```

### Test Case 6: Complete Already Completed Response
```bash
# Try to complete response twice
curl -X POST "https://localhost:5001/api/responses/1/complete" \
  -H "Content-Type: application/json" \
  --insecure
# Expected: 409 Conflict (if already completed)
```

---

## PowerShell Examples (Windows)

### Get Responses
```powershell
$token = "YOUR_JWT_TOKEN"
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Invoke-RestMethod -Uri "https://localhost:5001/api/surveys/1/responses" `
    -Method Get `
    -Headers $headers `
    -SkipCertificateCheck
```

### Create Response
```powershell
$body = @{
    respondentTelegramId = 123456789
    respondentUsername = "test_user"
    respondentFirstName = "Test"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/api/surveys/1/responses" `
    -Method Post `
    -Body $body `
    -ContentType "application/json" `
    -SkipCertificateCheck
```

### Save Answer
```powershell
$body = @{
    answer = @{
        questionId = 1
        answerText = "Test Answer"
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/api/responses/1/answers" `
    -Method Post `
    -Body $body `
    -ContentType "application/json" `
    -SkipCertificateCheck
```

---

## Key Implementation Features

### Authorization Strategy
- **GET /api/surveys/{surveyId}/responses** - Requires authentication (creator only)
- **GET /api/responses/{id}** - Requires authentication (creator only)
- **POST /api/surveys/{surveyId}/responses** - Public (for bot)
- **POST /api/responses/{id}/answers** - Public (for bot)
- **POST /api/responses/{id}/complete** - Public (for bot)

### Pagination
- Default page size: 20
- Maximum page size: 100
- Minimum page number: 1
- Returns total count and total pages

### Answer Validation
Controller validates answer format based on question type:
- **Text:** Requires answerText
- **SingleChoice:** Requires selectedOptions (one item)
- **MultipleChoice:** Requires selectedOptions (multiple items)
- **Rating:** Requires ratingValue (1-5)

### Error Handling
All endpoints have comprehensive error handling:
- Model validation errors (400)
- Not found errors (404)
- Unauthorized access (401, 403)
- Conflict errors (409)
- Internal server errors (500)

### Logging
All operations are logged with:
- Information level for successful operations
- Warning level for validation failures
- Error level for exceptions

---

## Notes

1. Replace `YOUR_JWT_TOKEN` with actual JWT token from login
2. Replace survey/response/question IDs with actual IDs from your database
3. Use `--insecure` for local HTTPS testing (development only)
4. For production, always use valid SSL certificates
5. The `completedOnly` filter helps creators see only finished responses
6. All timestamps are in UTC format
7. Public endpoints (POST) are designed for Telegram bot integration
8. Protected endpoints (GET) are for the admin panel/web interface
