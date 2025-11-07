# TASK-021: Surveys Controller Testing Guide

## Overview
This guide provides testing instructions for the SurveysController API endpoints implemented in TASK-021.

## Prerequisites
1. API is running (typically at `https://localhost:5001` or `http://localhost:5000`)
2. You have a valid JWT token (obtain from `/api/auth/login`)
3. Database is migrated and running

## Getting a JWT Token

First, login to get a JWT token:

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "telegramId": 123456789,
    "username": "testuser",
    "firstName": "Test",
    "lastName": "User"
  }'
```

Response:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "userId": 1,
    "telegramId": 123456789,
    "username": "testuser"
  }
}
```

**Save the `accessToken` for subsequent requests.**

---

## 1. Create Survey (POST /api/surveys)

Creates a new survey for the authenticated user.

### cURL Example

```bash
curl -X POST https://localhost:5001/api/surveys \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "title": "Customer Satisfaction Survey",
    "description": "We value your feedback",
    "isActive": false,
    "allowMultipleResponses": false,
    "showResults": true
  }'
```

### Expected Response (201 Created)

```json
{
  "success": true,
  "data": {
    "id": 1,
    "title": "Customer Satisfaction Survey",
    "description": "We value your feedback",
    "creatorId": 1,
    "isActive": false,
    "allowMultipleResponses": false,
    "showResults": true,
    "questions": [],
    "totalResponses": 0,
    "completedResponses": 0,
    "createdAt": "2025-01-06T10:00:00Z",
    "updatedAt": "2025-01-06T10:00:00Z"
  },
  "message": "Survey created successfully"
}
```

---

## 2. Get All Surveys (GET /api/surveys)

Retrieves a paginated list of surveys for the authenticated user.

### cURL Example

```bash
# Basic request
curl -X GET "https://localhost:5001/api/surveys" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# With pagination
curl -X GET "https://localhost:5001/api/surveys?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# With search
curl -X GET "https://localhost:5001/api/surveys?searchTerm=customer" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# With active filter
curl -X GET "https://localhost:5001/api/surveys?isActive=true" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# With sorting
curl -X GET "https://localhost:5001/api/surveys?sortBy=title&sortDescending=false" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Combined
curl -X GET "https://localhost:5001/api/surveys?pageNumber=1&pageSize=5&searchTerm=survey&sortBy=createdat&sortDescending=true" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Expected Response (200 OK)

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "title": "Customer Satisfaction Survey",
        "description": "We value your feedback",
        "isActive": false,
        "questionCount": 0,
        "totalResponses": 0,
        "completedResponses": 0,
        "createdAt": "2025-01-06T10:00:00Z",
        "updatedAt": "2025-01-06T10:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

---

## 3. Get Survey by ID (GET /api/surveys/{id})

Retrieves detailed information about a specific survey.

### cURL Example

```bash
curl -X GET https://localhost:5001/api/surveys/1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Expected Response (200 OK)

```json
{
  "success": true,
  "data": {
    "id": 1,
    "title": "Customer Satisfaction Survey",
    "description": "We value your feedback",
    "creatorId": 1,
    "isActive": false,
    "allowMultipleResponses": false,
    "showResults": true,
    "questions": [],
    "totalResponses": 0,
    "completedResponses": 0,
    "createdAt": "2025-01-06T10:00:00Z",
    "updatedAt": "2025-01-06T10:00:00Z"
  }
}
```

### Error Response (404 Not Found)

```json
{
  "success": false,
  "message": "Survey with ID 999 was not found."
}
```

---

## 4. Update Survey (PUT /api/surveys/{id})

Updates an existing survey. Note: Active surveys with responses cannot be modified.

### cURL Example

```bash
curl -X PUT https://localhost:5001/api/surveys/1 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "title": "Updated Customer Satisfaction Survey",
    "description": "Updated description",
    "allowMultipleResponses": true,
    "showResults": false
  }'
```

### Expected Response (200 OK)

```json
{
  "success": true,
  "data": {
    "id": 1,
    "title": "Updated Customer Satisfaction Survey",
    "description": "Updated description",
    "creatorId": 1,
    "isActive": false,
    "allowMultipleResponses": true,
    "showResults": false,
    "questions": [],
    "totalResponses": 0,
    "completedResponses": 0,
    "createdAt": "2025-01-06T10:00:00Z",
    "updatedAt": "2025-01-06T10:05:00Z"
  },
  "message": "Survey updated successfully"
}
```

---

## 5. Delete Survey (DELETE /api/surveys/{id})

Deletes a survey. Performs soft delete (deactivation) if it has responses, hard delete otherwise.

### cURL Example

```bash
curl -X DELETE https://localhost:5001/api/surveys/1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Expected Response (204 No Content)

No response body.

---

## 6. Activate Survey (POST /api/surveys/{id}/activate)

Activates a survey to make it available for responses. Survey must have at least one question.

### cURL Example

```bash
curl -X POST https://localhost:5001/api/surveys/1/activate \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Expected Response (200 OK)

```json
{
  "success": true,
  "data": {
    "id": 1,
    "title": "Customer Satisfaction Survey",
    "description": "We value your feedback",
    "creatorId": 1,
    "isActive": true,
    "allowMultipleResponses": false,
    "showResults": true,
    "questions": [
      {
        "id": 1,
        "questionText": "How satisfied are you?",
        "questionType": "Rating"
      }
    ],
    "totalResponses": 0,
    "completedResponses": 0,
    "createdAt": "2025-01-06T10:00:00Z",
    "updatedAt": "2025-01-06T10:10:00Z"
  },
  "message": "Survey activated successfully"
}
```

### Error Response (400 Bad Request - No Questions)

```json
{
  "success": false,
  "message": "Cannot activate a survey with no questions. Please add at least one question before activating."
}
```

---

## 7. Deactivate Survey (POST /api/surveys/{id}/deactivate)

Deactivates a survey to stop accepting new responses.

### cURL Example

```bash
curl -X POST https://localhost:5001/api/surveys/1/deactivate \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Expected Response (200 OK)

```json
{
  "success": true,
  "data": {
    "id": 1,
    "title": "Customer Satisfaction Survey",
    "description": "We value your feedback",
    "creatorId": 1,
    "isActive": false,
    "allowMultipleResponses": false,
    "showResults": true,
    "questions": [],
    "totalResponses": 5,
    "completedResponses": 3,
    "createdAt": "2025-01-06T10:00:00Z",
    "updatedAt": "2025-01-06T10:15:00Z"
  },
  "message": "Survey deactivated successfully"
}
```

---

## 8. Get Survey Statistics (GET /api/surveys/{id}/statistics)

Retrieves comprehensive statistics for a survey including response rates and question-level analytics.

### cURL Example

```bash
curl -X GET https://localhost:5001/api/surveys/1/statistics \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Expected Response (200 OK)

```json
{
  "success": true,
  "data": {
    "surveyId": 1,
    "title": "Customer Satisfaction Survey",
    "totalResponses": 10,
    "completedResponses": 8,
    "incompleteResponses": 2,
    "completionRate": 80.0,
    "averageCompletionTime": 120.5,
    "uniqueRespondents": 10,
    "firstResponseAt": "2025-01-06T11:00:00Z",
    "lastResponseAt": "2025-01-06T15:00:00Z",
    "questionStatistics": [
      {
        "questionId": 1,
        "questionText": "How satisfied are you?",
        "questionType": "Rating",
        "totalAnswers": 8,
        "skippedCount": 0,
        "responseRate": 100.0,
        "ratingStatistics": {
          "averageRating": 4.5,
          "minRating": 3,
          "maxRating": 5,
          "totalRatings": 8,
          "distribution": [
            { "rating": 3, "count": 1, "percentage": 12.5 },
            { "rating": 4, "count": 3, "percentage": 37.5 },
            { "rating": 5, "count": 4, "percentage": 50.0 }
          ]
        }
      }
    ]
  }
}
```

---

## Common HTTP Status Codes

| Status Code | Meaning | When It Occurs |
|-------------|---------|----------------|
| 200 OK | Success | Get, Update, Activate, Deactivate successful |
| 201 Created | Resource created | Survey created successfully |
| 204 No Content | Success, no response | Survey deleted successfully |
| 400 Bad Request | Invalid input | Validation failure, survey can't be modified |
| 401 Unauthorized | Not authenticated | Missing or invalid JWT token |
| 403 Forbidden | Not authorized | User doesn't own the survey |
| 404 Not Found | Resource not found | Survey with given ID doesn't exist |
| 500 Internal Server Error | Server error | Unexpected error occurred |

---

## Postman Collection

You can import the following Postman collection for easier testing:

### Environment Variables
```json
{
  "baseUrl": "https://localhost:5001",
  "token": "YOUR_JWT_TOKEN_HERE"
}
```

### Collection Structure
1. **Auth** → Login
2. **Surveys** →
   - Create Survey
   - Get All Surveys
   - Get Survey by ID
   - Update Survey
   - Delete Survey
   - Activate Survey
   - Deactivate Survey
   - Get Survey Statistics

Each request should use `{{baseUrl}}` for the base URL and `{{token}}` for the Authorization header.

---

## Testing Workflow

### Typical Test Scenario

1. **Login** to get JWT token
2. **Create a survey** (POST /api/surveys)
3. **Get survey list** to verify creation (GET /api/surveys)
4. **Get survey by ID** to see details (GET /api/surveys/1)
5. **Try to activate** (should fail - no questions) (POST /api/surveys/1/activate)
6. **Add questions** (use Questions controller - separate task)
7. **Activate survey** (POST /api/surveys/1/activate)
8. **Update survey** (should fail - active survey) (PUT /api/surveys/1)
9. **Deactivate survey** (POST /api/surveys/1/deactivate)
10. **Update survey** (should succeed now) (PUT /api/surveys/1)
11. **Get statistics** (GET /api/surveys/1/statistics)
12. **Delete survey** (DELETE /api/surveys/1)

---

## Validation Test Cases

### Create Survey Validation

1. **Missing title** (400 Bad Request)
```bash
curl -X POST https://localhost:5001/api/surveys \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{"description": "Test"}'
```

2. **Title too short** (400 Bad Request)
```bash
curl -X POST https://localhost:5001/api/surveys \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{"title": "AB"}'
```

3. **Title too long** (400 Bad Request)
```bash
curl -X POST https://localhost:5001/api/surveys \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{"title": "'$(python -c 'print("A"*501)')'"}'
```

### Authorization Test Cases

1. **No token** (401 Unauthorized)
```bash
curl -X GET https://localhost:5001/api/surveys
```

2. **Invalid token** (401 Unauthorized)
```bash
curl -X GET https://localhost:5001/api/surveys \
  -H "Authorization: Bearer invalid_token"
```

3. **Access other user's survey** (403 Forbidden)
```bash
# Login as user1, create survey, then login as user2 and try to access
curl -X GET https://localhost:5001/api/surveys/1 \
  -H "Authorization: Bearer USER2_TOKEN"
```

---

## Swagger/OpenAPI Documentation

Once the API is running, you can access the interactive Swagger documentation at:

```
https://localhost:5001/swagger
```

This provides:
- Interactive API testing
- Request/response schemas
- Example values
- Try-it-out functionality

All 8 endpoints will be visible under the "Surveys" tag.

---

## Notes

1. All timestamps are in UTC
2. Survey IDs are auto-incrementing integers
3. Pagination defaults: pageNumber=1, pageSize=10
4. Search is case-insensitive and searches both title and description
5. Soft delete preserves survey data when it has responses
6. Active surveys with responses cannot be modified (must deactivate first)
7. Surveys must have at least one question to be activated

---

## Troubleshooting

### Issue: 401 Unauthorized
- Verify JWT token is valid and not expired
- Check Authorization header format: `Bearer YOUR_TOKEN`
- Ensure user exists in database

### Issue: 403 Forbidden
- Verify you're accessing your own surveys
- Check userId in token matches survey creator

### Issue: 404 Not Found
- Verify survey ID exists
- Check database for survey record

### Issue: 400 Bad Request (Activation)
- Ensure survey has at least one question
- Verify survey is not already active

### Issue: 400 Bad Request (Update)
- Check if survey is active with responses
- Deactivate survey before modifying
