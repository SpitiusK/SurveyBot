# Phase 2 API Reference

Complete API documentation for SurveyBot Phase 2 endpoints.

## Base URL

```
Development: http://localhost:5000
Production: https://api.surveybot.com
```

## Authentication

All endpoints except authentication endpoints require a valid JWT token in the Authorization header:

```
Authorization: Bearer <your_jwt_token>
```

## Response Format

All API responses follow a consistent format:

```json
{
  "success": true,
  "message": "Operation successful",
  "data": { }
}
```

Error responses:

```json
{
  "success": false,
  "message": "Error description",
  "data": null
}
```

## Status Codes

- `200 OK` - Successful GET/PUT/POST operation
- `201 Created` - Resource created successfully
- `204 No Content` - Successful DELETE operation
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Missing or invalid authentication token
- `403 Forbidden` - User doesn't have permission to access resource
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error occurred
- `501 Not Implemented` - Feature not yet implemented

---

## Authentication Endpoints

### POST /api/auth/login

Authenticates a user by Telegram ID and returns JWT token.

**Request:**
```json
{
  "telegramId": 123456789
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "userId": 1,
    "telegramId": 123456789,
    "username": "johndoe",
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-11-08T12:00:00Z"
  }
}
```

**Errors:**
- `400 Bad Request` - Invalid telegram ID
- `500 Internal Server Error` - Authentication service error

**Example:**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 123456789}'
```

---

### POST /api/auth/register

Registers a new user or updates existing user (upsert pattern). Returns user data with JWT token.

**Request:**
```json
{
  "telegramId": 123456789,
  "username": "johndoe",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Registration/login successful",
  "data": {
    "user": {
      "id": 1,
      "telegramId": 123456789,
      "username": "johndoe",
      "firstName": "John",
      "lastName": "Doe",
      "createdAt": "2025-11-07T10:00:00Z"
    },
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-11-08T12:00:00Z"
  }
}
```

**Errors:**
- `400 Bad Request` - Invalid request data
- `500 Internal Server Error` - Registration error

---

### GET /api/auth/me

Returns current authenticated user information from JWT token.

**Headers:**
```
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Current user information",
  "data": {
    "userId": "1",
    "telegramId": "123456789",
    "username": "johndoe"
  }
}
```

**Errors:**
- `401 Unauthorized` - Invalid or missing token

---

### GET /api/auth/validate

Validates the current JWT token.

**Headers:**
```
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Token is valid",
  "data": {
    "valid": true,
    "userId": "1",
    "telegramId": "123456789",
    "username": "johndoe"
  }
}
```

**Errors:**
- `401 Unauthorized` - Invalid or expired token

---

### POST /api/auth/refresh

Refreshes an access token (MVP placeholder - not fully implemented).

**Request:**
```json
{
  "refreshToken": "refresh_token_here"
}
```

**Response:** `501 Not Implemented`
```json
{
  "success": false,
  "message": "Token refresh is not implemented in MVP. Please login again.",
  "data": null
}
```

---

## Survey Endpoints

### POST /api/surveys

Creates a new survey for the authenticated user.

**Headers:**
```
Authorization: Bearer <token>
```

**Request:**
```json
{
  "title": "Customer Satisfaction Survey",
  "description": "Help us improve our service"
}
```

**Response:** `201 Created`
```json
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
    "updatedAt": "2025-11-07T10:00:00Z",
    "questions": []
  }
}
```

**Errors:**
- `400 Bad Request` - Validation error (title required, max 200 chars)
- `401 Unauthorized` - Invalid or missing token
- `500 Internal Server Error` - Server error

**Validation Rules:**
- Title: Required, 1-200 characters
- Description: Optional, max 1000 characters

---

### GET /api/surveys

Gets a paginated list of surveys for the authenticated user with optional filtering and sorting.

**Headers:**
```
Authorization: Bearer <token>
```

**Query Parameters:**
- `pageNumber` (int, default: 1) - Page number (1-based)
- `pageSize` (int, default: 10) - Items per page (1-100)
- `searchTerm` (string, optional) - Search in title/description
- `isActive` (bool, optional) - Filter by active status
- `sortBy` (string, optional) - Sort field: title, createdat, updatedat, isactive
- `sortDescending` (bool, default: false) - Sort direction

**Response:** `200 OK`
```json
{
  "success": true,
  "message": null,
  "data": {
    "items": [
      {
        "id": 1,
        "title": "Customer Satisfaction Survey",
        "description": "Help us improve our service",
        "isActive": true,
        "questionCount": 5,
        "responseCount": 42,
        "createdAt": "2025-11-07T10:00:00Z",
        "updatedAt": "2025-11-07T11:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1
  }
}
```

**Example with Filtering:**
```bash
curl -X GET "http://localhost:5000/api/surveys?pageNumber=1&pageSize=10&isActive=true&searchTerm=customer&sortBy=createdat&sortDescending=true" \
  -H "Authorization: Bearer <token>"
```

---

### GET /api/surveys/{id}

Gets detailed information about a specific survey including all questions.

**Headers:**
```
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": null,
  "data": {
    "id": 1,
    "title": "Customer Satisfaction Survey",
    "description": "Help us improve our service",
    "isActive": true,
    "createdBy": 1,
    "createdAt": "2025-11-07T10:00:00Z",
    "updatedAt": "2025-11-07T11:00:00Z",
    "questions": [
      {
        "id": 1,
        "surveyId": 1,
        "text": "How satisfied are you?",
        "type": "SingleChoice",
        "isRequired": true,
        "orderIndex": 0,
        "options": ["Very Satisfied", "Satisfied", "Neutral", "Dissatisfied"]
      }
    ]
  }
}
```

**Errors:**
- `401 Unauthorized` - Invalid or missing token
- `403 Forbidden` - User doesn't own this survey
- `404 Not Found` - Survey not found

---

### PUT /api/surveys/{id}

Updates an existing survey. Active surveys with responses cannot be modified.

**Headers:**
```
Authorization: Bearer <token>
```

**Request:**
```json
{
  "title": "Updated Survey Title",
  "description": "Updated description"
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Survey updated successfully",
  "data": {
    "id": 1,
    "title": "Updated Survey Title",
    "description": "Updated description",
    "isActive": false,
    "createdBy": 1,
    "createdAt": "2025-11-07T10:00:00Z",
    "updatedAt": "2025-11-07T12:00:00Z",
    "questions": []
  }
}
```

**Errors:**
- `400 Bad Request` - Validation error or survey cannot be modified (has responses)
- `401 Unauthorized` - Invalid or missing token
- `403 Forbidden` - User doesn't own this survey
- `404 Not Found` - Survey not found

---

### DELETE /api/surveys/{id}

Deletes a survey. Soft delete (deactivate) if it has responses, hard delete otherwise.

**Headers:**
```
Authorization: Bearer <token>
```

**Response:** `204 No Content`

**Errors:**
- `401 Unauthorized` - Invalid or missing token
- `403 Forbidden` - User doesn't own this survey
- `404 Not Found` - Survey not found

**Example:**
```bash
curl -X DELETE http://localhost:5000/api/surveys/1 \
  -H "Authorization: Bearer <token>"
```

---

### POST /api/surveys/{id}/activate

Activates a survey to make it available for responses. Survey must have at least one question.

**Headers:**
```
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Survey activated successfully",
  "data": {
    "id": 1,
    "title": "Customer Satisfaction Survey",
    "isActive": true,
    "updatedAt": "2025-11-07T12:00:00Z"
  }
}
```

**Errors:**
- `400 Bad Request` - Survey cannot be activated (no questions)
- `401 Unauthorized` - Invalid or missing token
- `403 Forbidden` - User doesn't own this survey
- `404 Not Found` - Survey not found

---

### POST /api/surveys/{id}/deactivate

Deactivates a survey to stop accepting new responses. Existing responses are preserved.

**Headers:**
```
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Survey deactivated successfully",
  "data": {
    "id": 1,
    "title": "Customer Satisfaction Survey",
    "isActive": false,
    "updatedAt": "2025-11-07T12:00:00Z"
  }
}
```

**Errors:**
- `401 Unauthorized` - Invalid or missing token
- `403 Forbidden` - User doesn't own this survey
- `404 Not Found` - Survey not found

---

### GET /api/surveys/{id}/statistics

Gets comprehensive statistics for a survey including response rates and question-level analytics.

**Headers:**
```
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": null,
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
            }
          ]
        }
      }
    ]
  }
}
```

**Errors:**
- `401 Unauthorized` - Invalid or missing token
- `403 Forbidden` - User doesn't own this survey
- `404 Not Found` - Survey not found

---

## Question Endpoints

### POST /api/surveys/{surveyId}/questions

Adds a new question to a survey.

**Headers:**
```
Authorization: Bearer <token>
```

**Request:**
```json
{
  "text": "How satisfied are you with our service?",
  "type": "SingleChoice",
  "isRequired": true,
  "options": ["Very Satisfied", "Satisfied", "Neutral", "Dissatisfied", "Very Dissatisfied"]
}
```

**Question Types:**
- `Text` - Free text answer
- `SingleChoice` - Radio buttons (options required)
- `MultipleChoice` - Checkboxes (options required)
- `Rating` - 1-5 star rating

**Response:** `201 Created`
```json
{
  "success": true,
  "message": "Question created successfully",
  "data": {
    "id": 1,
    "surveyId": 1,
    "text": "How satisfied are you with our service?",
    "type": "SingleChoice",
    "isRequired": true,
    "orderIndex": 0,
    "options": ["Very Satisfied", "Satisfied", "Neutral", "Dissatisfied", "Very Dissatisfied"]
  }
}
```

**Errors:**
- `400 Bad Request` - Validation error (options required for choice questions)
- `401 Unauthorized` - Invalid or missing token
- `403 Forbidden` - User doesn't own the survey
- `404 Not Found` - Survey not found

**Validation Rules:**
- Text: Required, 1-500 characters
- Type: Must be valid QuestionType enum
- Options: Required for SingleChoice/MultipleChoice (2-10 options)
- Each option: 1-200 characters

---

### PUT /api/questions/{id}

Updates an existing question. Questions with responses cannot be modified.

**Headers:**
```
Authorization: Bearer <token>
```

**Request:**
```json
{
  "text": "Updated question text",
  "type": "SingleChoice",
  "isRequired": false,
  "options": ["Option 1", "Option 2", "Option 3"]
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Question updated successfully",
  "data": {
    "id": 1,
    "surveyId": 1,
    "text": "Updated question text",
    "type": "SingleChoice",
    "isRequired": false,
    "orderIndex": 0,
    "options": ["Option 1", "Option 2", "Option 3"]
  }
}
```

**Errors:**
- `400 Bad Request` - Validation error or question has responses
- `401 Unauthorized` - Invalid or missing token
- `403 Forbidden` - User doesn't own the survey
- `404 Not Found` - Question not found

---

### DELETE /api/questions/{id}

Deletes a question from a survey. Questions with responses cannot be deleted.

**Headers:**
```
Authorization: Bearer <token>
```

**Response:** `204 No Content`

**Errors:**
- `400 Bad Request` - Question has responses
- `401 Unauthorized` - Invalid or missing token
- `403 Forbidden` - User doesn't own the survey
- `404 Not Found` - Question not found

---

### GET /api/surveys/{surveyId}/questions

Gets all questions for a survey ordered by OrderIndex.

**Headers:**
```
Authorization: Bearer <token> (only for inactive surveys)
```

**Note:** Active surveys are publicly accessible without authentication.

**Response:** `200 OK`
```json
{
  "success": true,
  "message": null,
  "data": [
    {
      "id": 1,
      "surveyId": 1,
      "text": "How satisfied are you?",
      "type": "SingleChoice",
      "isRequired": true,
      "orderIndex": 0,
      "options": ["Very Satisfied", "Satisfied", "Neutral"]
    },
    {
      "id": 2,
      "surveyId": 1,
      "text": "Any comments?",
      "type": "Text",
      "isRequired": false,
      "orderIndex": 1,
      "options": null
    }
  ]
}
```

**Errors:**
- `403 Forbidden` - Trying to access inactive survey without ownership
- `404 Not Found` - Survey not found

---

### POST /api/surveys/{surveyId}/questions/reorder

Changes the order of questions within a survey.

**Headers:**
```
Authorization: Bearer <token>
```

**Request:**
```json
{
  "questionIds": [3, 1, 2]
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Questions reordered successfully",
  "data": null
}
```

**Errors:**
- `400 Bad Request` - Invalid question IDs or IDs don't belong to survey
- `401 Unauthorized` - Invalid or missing token
- `403 Forbidden` - User doesn't own the survey
- `404 Not Found` - Survey not found

**Example:**
```bash
curl -X POST http://localhost:5000/api/surveys/1/questions/reorder \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"questionIds": [3, 1, 2]}'
```

---

## Response Endpoints

### GET /api/responses

Lists responses with filtering options. (Note: Implementation pending in Phase 2)

**Headers:**
```
Authorization: Bearer <token>
```

**Query Parameters:**
- `surveyId` (int, optional) - Filter by survey
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 10)

---

### GET /api/responses/{id}

Gets a specific response. (Note: Implementation pending in Phase 2)

**Headers:**
```
Authorization: Bearer <token>
```

---

## Error Response Examples

### 400 Bad Request
```json
{
  "success": false,
  "message": "Invalid request data",
  "data": {
    "Title": ["The Title field is required."],
    "Options": ["Options are required for choice-based questions."]
  }
}
```

### 401 Unauthorized
```json
{
  "success": false,
  "message": "Invalid or missing user authentication",
  "data": null
}
```

### 403 Forbidden
```json
{
  "success": false,
  "message": "You don't have permission to access this survey",
  "data": null
}
```

### 404 Not Found
```json
{
  "success": false,
  "message": "Survey with ID 123 not found",
  "data": null
}
```

### 500 Internal Server Error
```json
{
  "success": false,
  "message": "An error occurred while processing your request",
  "data": null
}
```

---

## Pagination

All list endpoints support pagination with consistent query parameters:

- `pageNumber` (int, default: 1) - 1-based page number
- `pageSize` (int, default: 10, max: 100) - Items per page

Paginated responses include:

```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5
}
```

---

## Filtering and Sorting

### Survey Filtering

```bash
# Active surveys only
GET /api/surveys?isActive=true

# Search by title/description
GET /api/surveys?searchTerm=customer

# Sort by creation date (newest first)
GET /api/surveys?sortBy=createdat&sortDescending=true

# Combined filters
GET /api/surveys?isActive=true&searchTerm=satisfaction&sortBy=updatedat&sortDescending=true&pageSize=20
```

---

## Rate Limiting

- No rate limiting implemented in Phase 2 MVP
- Consider implementing rate limiting in future phases

---

## CORS

CORS is configured to allow requests from:
- Localhost (development)
- Configured frontend domains (production)

---

## API Versioning

Current version: v1 (implicit, no version in URL for MVP)

Future versions may use:
- URL versioning: `/api/v2/surveys`
- Header versioning: `Accept: application/vnd.surveybot.v2+json`

---

## Best Practices

1. **Always include Authorization header** for protected endpoints
2. **Handle token expiration** - login again when receiving 401
3. **Use pagination** for list endpoints to avoid large responses
4. **Validate data client-side** before sending to API
5. **Handle errors gracefully** - display user-friendly messages
6. **Cache survey data** when appropriate to reduce API calls
7. **Use appropriate HTTP methods** - GET for reads, POST for creates, PUT for updates, DELETE for deletes

---

## Complete Request Example

```bash
# 1. Login
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 123456789}' | jq -r '.data.accessToken')

# 2. Create Survey
SURVEY_ID=$(curl -s -X POST http://localhost:5000/api/surveys \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"My Survey","description":"Test survey"}' | jq -r '.data.id')

# 3. Add Question
curl -X POST http://localhost:5000/api/surveys/$SURVEY_ID/questions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"text":"Rate our service","type":"Rating","isRequired":true}'

# 4. Activate Survey
curl -X POST http://localhost:5000/api/surveys/$SURVEY_ID/activate \
  -H "Authorization: Bearer $TOKEN"

# 5. Get Survey Statistics
curl -X GET http://localhost:5000/api/surveys/$SURVEY_ID/statistics \
  -H "Authorization: Bearer $TOKEN"
```
