# API Reference Documentation
## Telegram Survey Bot MVP

### Version: 1.0.0
### Last Updated: 2025-11-06

---

## Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Base URL](#base-url)
4. [Response Format](#response-format)
5. [Error Handling](#error-handling)
6. [Endpoints](#endpoints)
   - [Health](#health-endpoints)
   - [Users](#users-endpoints)
   - [Surveys](#surveys-endpoints)
   - [Questions](#questions-endpoints)
   - [Responses](#responses-endpoints)
7. [Data Models](#data-models)
8. [Examples](#examples)

---

## Overview

The Survey Bot API is a RESTful API that provides endpoints for managing surveys, questions, responses, and user data. The API follows standard HTTP conventions and returns JSON responses.

### Key Features

- RESTful design with standard HTTP methods
- JSON request/response format
- Consistent error handling
- Interactive documentation via Swagger UI
- Health check endpoint for monitoring

### API Characteristics

- **Protocol**: HTTP/HTTPS
- **Content-Type**: `application/json`
- **Authentication**: None (MVP) - JWT planned for future
- **Rate Limiting**: None (MVP) - planned for future
- **Versioning**: None (MVP) - planned for future

---

## Authentication

### Current (MVP)

No authentication required. All endpoints are public.

### Future Enhancement

JWT Bearer token authentication will be implemented:

```http
Authorization: Bearer <your-jwt-token>
```

---

## Base URL

### Development
```
http://localhost:5000/api
```

### Production (when deployed)
```
https://your-domain.com/api
```

---

## Response Format

### Success Response

All successful responses are wrapped in a standard format:

```json
{
  "success": true,
  "data": {
    // Response data here
  },
  "message": "Optional success message",
  "timestamp": "2025-11-06T10:30:00Z"
}
```

### Error Response

All error responses follow this format:

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": {
      // Additional error details (optional)
    }
  },
  "timestamp": "2025-11-06T10:30:00Z"
}
```

---

## Error Handling

### HTTP Status Codes

| Status Code | Description | When Used |
|------------|-------------|-----------|
| 200 OK | Success | GET, PUT successful |
| 201 Created | Resource created | POST successful |
| 204 No Content | Success, no content | DELETE successful |
| 400 Bad Request | Invalid input | Validation failed |
| 404 Not Found | Resource not found | ID doesn't exist |
| 409 Conflict | Resource conflict | Duplicate entry |
| 500 Internal Server Error | Server error | Unhandled exception |

### Error Codes

| Error Code | HTTP Status | Description |
|-----------|-------------|-------------|
| VALIDATION_ERROR | 400 | Input validation failed |
| NOT_FOUND | 404 | Resource not found |
| DUPLICATE_ENTRY | 409 | Resource already exists |
| INTERNAL_ERROR | 500 | Internal server error |

### Example Error Response

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid survey data",
    "details": {
      "title": ["Title is required"],
      "description": ["Description must be at least 10 characters"]
    }
  },
  "timestamp": "2025-11-06T10:30:00Z"
}
```

---

## Endpoints

### Health Endpoints

#### Check API Health

Check the health status of the API and database connection.

```http
GET /api/health
```

**Response 200 OK**
```json
{
  "status": "Healthy",
  "database": "Connected",
  "timestamp": "2025-11-06T10:30:00Z"
}
```

**Response 503 Service Unavailable**
```json
{
  "status": "Unhealthy",
  "database": "Disconnected",
  "error": "Connection timeout",
  "timestamp": "2025-11-06T10:30:00Z"
}
```

---

### Users Endpoints

#### Get All Users

Retrieve a list of all users.

```http
GET /api/users
```

**Query Parameters**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| page | integer | No | Page number (default: 1) |
| pageSize | integer | No | Items per page (default: 50) |

**Response 200 OK**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "telegramId": 123456789,
      "username": "john_doe",
      "firstName": "John",
      "lastName": "Doe",
      "createdAt": "2025-11-01T10:00:00Z"
    }
  ]
}
```

#### Get User by ID

Retrieve a specific user by their ID.

```http
GET /api/users/{id}
```

**Path Parameters**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | User ID |

**Response 200 OK**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "telegramId": 123456789,
    "username": "john_doe",
    "firstName": "John",
    "lastName": "Doe",
    "createdAt": "2025-11-01T10:00:00Z",
    "surveysCreated": 5,
    "responsesCount": 12
  }
}
```

**Response 404 Not Found**
```json
{
  "success": false,
  "error": {
    "code": "NOT_FOUND",
    "message": "User with ID 999 not found"
  }
}
```

#### Create User

Create a new user.

```http
POST /api/users
```

**Request Body**
```json
{
  "telegramId": 123456789,
  "username": "john_doe",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response 201 Created**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "telegramId": 123456789,
    "username": "john_doe",
    "firstName": "John",
    "lastName": "Doe",
    "createdAt": "2025-11-06T10:30:00Z"
  }
}
```

**Response 409 Conflict**
```json
{
  "success": false,
  "error": {
    "code": "DUPLICATE_ENTRY",
    "message": "User with Telegram ID 123456789 already exists"
  }
}
```

---

### Surveys Endpoints

#### Get All Surveys

Retrieve a list of all surveys.

```http
GET /api/surveys
```

**Query Parameters**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userId | integer | No | Filter by creator user ID |
| isActive | boolean | No | Filter by active status |
| page | integer | No | Page number (default: 1) |
| pageSize | integer | No | Items per page (default: 50) |

**Response 200 OK**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "title": "Customer Satisfaction Survey",
      "description": "We value your feedback",
      "creatorId": 1,
      "isActive": true,
      "allowMultipleResponses": false,
      "showResults": true,
      "createdAt": "2025-11-01T10:00:00Z",
      "updatedAt": "2025-11-01T10:00:00Z",
      "questionCount": 5,
      "responseCount": 42
    }
  ]
}
```

#### Get Survey by ID

Retrieve a specific survey with all its questions.

```http
GET /api/surveys/{id}
```

**Path Parameters**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Survey ID |

**Response 200 OK**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "title": "Customer Satisfaction Survey",
    "description": "We value your feedback",
    "creatorId": 1,
    "creator": {
      "id": 1,
      "username": "john_doe",
      "firstName": "John",
      "lastName": "Doe"
    },
    "isActive": true,
    "allowMultipleResponses": false,
    "showResults": true,
    "createdAt": "2025-11-01T10:00:00Z",
    "updatedAt": "2025-11-01T10:00:00Z",
    "questions": [
      {
        "id": 1,
        "surveyId": 1,
        "questionText": "How satisfied are you with our service?",
        "questionType": "rating",
        "orderIndex": 0,
        "isRequired": true,
        "options": null
      },
      {
        "id": 2,
        "surveyId": 1,
        "questionText": "What could we improve?",
        "questionType": "text",
        "orderIndex": 1,
        "isRequired": false,
        "options": null
      }
    ],
    "statistics": {
      "totalResponses": 42,
      "uniqueRespondents": 38,
      "completionRate": 0.91
    }
  }
}
```

#### Create Survey

Create a new survey with questions.

```http
POST /api/surveys
```

**Request Body**
```json
{
  "title": "Customer Satisfaction Survey",
  "description": "We value your feedback",
  "creatorId": 1,
  "isActive": true,
  "allowMultipleResponses": false,
  "showResults": true,
  "questions": [
    {
      "questionText": "How satisfied are you with our service?",
      "questionType": "rating",
      "orderIndex": 0,
      "isRequired": true,
      "options": null
    },
    {
      "questionText": "Which features do you use most?",
      "questionType": "multiple_choice",
      "orderIndex": 1,
      "isRequired": true,
      "options": ["Feature A", "Feature B", "Feature C"]
    }
  ]
}
```

**Response 201 Created**
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
    "createdAt": "2025-11-06T10:30:00Z",
    "updatedAt": "2025-11-06T10:30:00Z",
    "questions": [
      {
        "id": 1,
        "questionText": "How satisfied are you with our service?",
        "questionType": "rating",
        "orderIndex": 0,
        "isRequired": true
      },
      {
        "id": 2,
        "questionText": "Which features do you use most?",
        "questionType": "multiple_choice",
        "orderIndex": 1,
        "isRequired": true,
        "options": ["Feature A", "Feature B", "Feature C"]
      }
    ]
  }
}
```

**Validation Rules**
- `title`: Required, 3-200 characters
- `description`: Optional, max 1000 characters
- `creatorId`: Required, must be valid user ID
- `questions`: At least 1 question required
- `questionText`: Required, 5-500 characters
- `questionType`: Must be one of: text, multiple_choice, single_choice, yes_no, rating
- `orderIndex`: Must be unique within survey

#### Update Survey

Update an existing survey.

```http
PUT /api/surveys/{id}
```

**Path Parameters**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Survey ID |

**Request Body**
```json
{
  "title": "Updated Survey Title",
  "description": "Updated description",
  "isActive": true,
  "allowMultipleResponses": true,
  "showResults": false
}
```

**Response 200 OK**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "title": "Updated Survey Title",
    "description": "Updated description",
    "isActive": true,
    "allowMultipleResponses": true,
    "showResults": false,
    "updatedAt": "2025-11-06T11:00:00Z"
  }
}
```

**Note**: Cannot update questions through this endpoint. Use dedicated question endpoints.

#### Delete Survey

Delete a survey and all its associated data.

```http
DELETE /api/surveys/{id}
```

**Path Parameters**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Survey ID |

**Response 204 No Content**

**Warning**: This will cascade delete:
- All questions
- All responses
- All answers

This operation cannot be undone.

#### Toggle Survey Status

Activate or deactivate a survey.

```http
POST /api/surveys/{id}/toggle-status
```

**Path Parameters**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Survey ID |

**Response 200 OK**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "isActive": false,
    "message": "Survey deactivated successfully"
  }
}
```

#### Get Survey Statistics

Get detailed statistics for a survey.

```http
GET /api/surveys/{id}/statistics
```

**Response 200 OK**
```json
{
  "success": true,
  "data": {
    "surveyId": 1,
    "totalResponses": 42,
    "completeResponses": 38,
    "incompleteResponses": 4,
    "uniqueRespondents": 38,
    "completionRate": 0.91,
    "averageCompletionTime": 180,
    "lastResponseAt": "2025-11-06T09:30:00Z",
    "questionStatistics": [
      {
        "questionId": 1,
        "questionText": "How satisfied are you?",
        "responseCount": 38,
        "averageRating": 4.5
      }
    ]
  }
}
```

---

### Questions Endpoints

#### Get Question by ID

Retrieve a specific question.

```http
GET /api/questions/{id}
```

**Response 200 OK**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "surveyId": 1,
    "questionText": "How satisfied are you?",
    "questionType": "rating",
    "orderIndex": 0,
    "isRequired": true,
    "options": null
  }
}
```

#### Create Question

Add a question to an existing survey.

```http
POST /api/questions
```

**Request Body**
```json
{
  "surveyId": 1,
  "questionText": "What is your age group?",
  "questionType": "single_choice",
  "orderIndex": 2,
  "isRequired": true,
  "options": ["18-25", "26-35", "36-45", "46+"]
}
```

**Response 201 Created**
```json
{
  "success": true,
  "data": {
    "id": 3,
    "surveyId": 1,
    "questionText": "What is your age group?",
    "questionType": "single_choice",
    "orderIndex": 2,
    "isRequired": true,
    "options": ["18-25", "26-35", "36-45", "46+"]
  }
}
```

#### Update Question

Update an existing question.

```http
PUT /api/questions/{id}
```

**Request Body**
```json
{
  "questionText": "Updated question text",
  "isRequired": false,
  "orderIndex": 3
}
```

**Response 200 OK**

#### Delete Question

Delete a question.

```http
DELETE /api/questions/{id}
```

**Response 204 No Content**

**Warning**: This will cascade delete all answers to this question.

---

### Responses Endpoints

#### Get Response by ID

Retrieve a specific survey response with all answers.

```http
GET /api/responses/{id}
```

**Response 200 OK**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "surveyId": 1,
    "respondentTelegramId": 987654321,
    "isComplete": true,
    "createdAt": "2025-11-06T10:00:00Z",
    "completedAt": "2025-11-06T10:03:00Z",
    "answers": [
      {
        "id": 1,
        "questionId": 1,
        "question": {
          "questionText": "How satisfied are you?",
          "questionType": "rating"
        },
        "answerText": "5",
        "answerJson": null
      },
      {
        "id": 2,
        "questionId": 2,
        "question": {
          "questionText": "What could we improve?",
          "questionType": "text"
        },
        "answerText": "Better mobile app",
        "answerJson": null
      }
    ]
  }
}
```

#### Create Response

Submit a survey response.

```http
POST /api/responses
```

**Request Body**
```json
{
  "surveyId": 1,
  "respondentTelegramId": 987654321,
  "answers": [
    {
      "questionId": 1,
      "answerText": "5"
    },
    {
      "questionId": 2,
      "answerText": "Better mobile app"
    }
  ]
}
```

**Response 201 Created**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "surveyId": 1,
    "respondentTelegramId": 987654321,
    "isComplete": true,
    "createdAt": "2025-11-06T10:30:00Z",
    "completedAt": "2025-11-06T10:30:00Z"
  }
}
```

**Response 409 Conflict** (if multiple responses not allowed)
```json
{
  "success": false,
  "error": {
    "code": "DUPLICATE_ENTRY",
    "message": "You have already responded to this survey"
  }
}
```

#### Get Survey Responses

Get all responses for a survey.

```http
GET /api/surveys/{surveyId}/responses
```

**Query Parameters**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| includeIncomplete | boolean | No | Include incomplete responses (default: false) |
| page | integer | No | Page number |
| pageSize | integer | No | Items per page |

**Response 200 OK**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "respondentTelegramId": 987654321,
      "isComplete": true,
      "createdAt": "2025-11-06T10:00:00Z",
      "completedAt": "2025-11-06T10:03:00Z",
      "answerCount": 5
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalPages": 1,
    "totalItems": 42
  }
}
```

---

## Data Models

### User

```json
{
  "id": "integer",
  "telegramId": "long (unique)",
  "username": "string (nullable)",
  "firstName": "string (nullable)",
  "lastName": "string (nullable)",
  "createdAt": "datetime"
}
```

### Survey

```json
{
  "id": "integer",
  "title": "string (required, 3-200 chars)",
  "description": "string (optional, max 1000 chars)",
  "creatorId": "integer (required)",
  "isActive": "boolean",
  "allowMultipleResponses": "boolean",
  "showResults": "boolean",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

### Question

```json
{
  "id": "integer",
  "surveyId": "integer (required)",
  "questionText": "string (required, 5-500 chars)",
  "questionType": "enum (text|multiple_choice|single_choice|yes_no|rating)",
  "orderIndex": "integer (required)",
  "isRequired": "boolean",
  "options": "string[] (required for choice types)"
}
```

### Response

```json
{
  "id": "integer",
  "surveyId": "integer (required)",
  "respondentTelegramId": "long (required)",
  "isComplete": "boolean",
  "createdAt": "datetime",
  "completedAt": "datetime (nullable)"
}
```

### Answer

```json
{
  "id": "integer",
  "responseId": "integer (required)",
  "questionId": "integer (required)",
  "answerText": "string (nullable)",
  "answerJson": "json (nullable, for multiple choice)"
}
```

---

## Examples

### Complete Survey Creation Workflow

1. **Create User** (if not exists)
```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "telegramId": 123456789,
    "username": "john_doe",
    "firstName": "John"
  }'
```

2. **Create Survey with Questions**
```bash
curl -X POST http://localhost:5000/api/surveys \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Product Feedback",
    "description": "Help us improve",
    "creatorId": 1,
    "isActive": true,
    "questions": [
      {
        "questionText": "Rate our product",
        "questionType": "rating",
        "orderIndex": 0,
        "isRequired": true
      }
    ]
  }'
```

3. **Submit Response**
```bash
curl -X POST http://localhost:5000/api/responses \
  -H "Content-Type: application/json" \
  -d '{
    "surveyId": 1,
    "respondentTelegramId": 987654321,
    "answers": [
      {
        "questionId": 1,
        "answerText": "5"
      }
    ]
  }'
```

4. **Get Statistics**
```bash
curl http://localhost:5000/api/surveys/1/statistics
```

---

## Interactive Documentation

### Swagger UI

The API includes interactive Swagger documentation:

**URL**: http://localhost:5000/swagger

**Features**:
- Try out endpoints directly from browser
- View request/response schemas
- See validation rules
- Test with different parameters

### Using Swagger

1. Run the application: `dotnet run`
2. Open browser: http://localhost:5000/swagger
3. Expand any endpoint
4. Click "Try it out"
5. Fill in parameters
6. Click "Execute"
7. View response

---

## Rate Limiting (Future)

Rate limiting will be implemented in future versions:

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1699282800
```

---

## Changelog

### v1.0.0 (Current - MVP)
- Basic CRUD operations for all entities
- Health check endpoint
- Swagger documentation
- Global exception handling
- Request logging

### Planned (Future Versions)
- JWT authentication
- Rate limiting
- API versioning
- Pagination improvements
- Filtering and sorting
- Bulk operations
- Webhooks for events

---

## Support

For API issues or questions:
- Check Swagger UI: http://localhost:5000/swagger
- Review this documentation
- Check application logs
- Consult [Troubleshooting Guide](../TROUBLESHOOTING.md)

---

**Document Status**: Complete for MVP implementation
**Last Updated**: 2025-11-06
