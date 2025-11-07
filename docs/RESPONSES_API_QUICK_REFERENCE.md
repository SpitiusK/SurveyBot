# Responses API - Quick Reference Card

## Base URL
```
https://localhost:5001/api
```

## Endpoints Overview

| Method | Endpoint | Auth | Purpose |
|--------|----------|------|---------|
| GET | `/surveys/{surveyId}/responses` | Required | List responses |
| GET | `/responses/{id}` | Required | Get one response |
| POST | `/surveys/{surveyId}/responses` | Public | Start response |
| POST | `/responses/{id}/answers` | Public | Save answer |
| POST | `/responses/{id}/complete` | Public | Complete response |

---

## 1. List Survey Responses

```bash
GET /api/surveys/{surveyId}/responses?pageNumber=1&pageSize=20&completedOnly=false
Authorization: Bearer {token}
```

**Response:** Paginated list of responses

---

## 2. Get Single Response

```bash
GET /api/responses/{id}
Authorization: Bearer {token}
```

**Response:** Response with all answers

---

## 3. Start Response

```bash
POST /api/surveys/{surveyId}/responses
Content-Type: application/json

{
  "respondentTelegramId": 123456789,
  "respondentUsername": "john_doe",
  "respondentFirstName": "John"
}
```

**Response:** Created response (201)

---

## 4. Save Answer

```bash
POST /api/responses/{id}/answers
Content-Type: application/json

# Text Answer
{
  "answer": {
    "questionId": 1,
    "answerText": "My answer"
  }
}

# Single Choice
{
  "answer": {
    "questionId": 2,
    "selectedOptions": ["Option A"]
  }
}

# Multiple Choice
{
  "answer": {
    "questionId": 3,
    "selectedOptions": ["Option A", "Option B"]
  }
}

# Rating
{
  "answer": {
    "questionId": 4,
    "ratingValue": 5
  }
}
```

**Response:** Updated response (200)

---

## 5. Complete Response

```bash
POST /api/responses/{id}/complete
Content-Type: application/json
```

**Response:** Completed response (200)

---

## Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request (validation error) |
| 401 | Unauthorized (missing token) |
| 403 | Forbidden (not owner) |
| 404 | Not Found |
| 409 | Conflict (duplicate/already completed) |
| 500 | Server Error |

---

## Common Errors

### Invalid Pagination
```json
{
  "success": false,
  "message": "Page size must be between 1 and 100"
}
```

### Survey Not Active
```json
{
  "success": false,
  "message": "This survey is not currently active"
}
```

### Invalid Answer Format
```json
{
  "success": false,
  "message": "Text answer is required for Text question type"
}
```

### Already Completed
```json
{
  "success": false,
  "message": "User has already completed this survey"
}
```

---

## Answer Validation Rules

| Question Type | Required Field | Valid Values |
|--------------|----------------|--------------|
| Text | `answerText` | Non-empty string (max 5000 chars) |
| SingleChoice | `selectedOptions` | Array with exactly 1 item |
| MultipleChoice | `selectedOptions` | Array with 1+ items |
| Rating | `ratingValue` | Integer between 1 and 5 |

---

## Quick Test Script

```bash
# 1. Login
TOKEN=$(curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}' \
  --insecure | jq -r '.data.token')

# 2. Start response
RESPONSE_ID=$(curl -X POST "https://localhost:5001/api/surveys/1/responses" \
  -H "Content-Type: application/json" \
  -d '{"respondentTelegramId":123456789}' \
  --insecure | jq -r '.data.id')

# 3. Save answers
curl -X POST "https://localhost:5001/api/responses/$RESPONSE_ID/answers" \
  -H "Content-Type: application/json" \
  -d '{"answer":{"questionId":1,"answerText":"Test"}}' \
  --insecure

# 4. Complete
curl -X POST "https://localhost:5001/api/responses/$RESPONSE_ID/complete" \
  --insecure

# 5. View results
curl -X GET "https://localhost:5001/api/surveys/1/responses" \
  -H "Authorization: Bearer $TOKEN" \
  --insecure
```

---

## Response Structure

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
    "answeredCount": 2,
    "totalQuestions": 5,
    "answers": [...]
  },
  "message": "Success message",
  "timestamp": "2025-11-07T12:00:00Z"
}
```

---

## Authorization

### Protected Endpoints (Require JWT)
- List responses (creator only)
- Get single response (creator only)

### Public Endpoints (No Auth)
- Start response (Telegram bot)
- Save answer (Telegram bot)
- Complete response (Telegram bot)

### Get JWT Token
```bash
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}' \
  --insecure
```

Use token in header:
```
Authorization: Bearer YOUR_TOKEN_HERE
```

---

## Pagination

Default values:
- `pageNumber`: 1 (min: 1)
- `pageSize`: 20 (min: 1, max: 100)
- `completedOnly`: false

Response includes:
```json
{
  "items": [...],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 45,
  "totalPages": 3
}
```

---

## Tips

1. **Always check survey is active** before starting responses
2. **Validate answer format** matches question type
3. **Use pagination** for large result sets
4. **Check completion status** before allowing answers
5. **Handle 409 Conflict** for duplicate responses
6. **Use completedOnly filter** to view finished surveys only
7. **Store response ID** after creation for subsequent calls

---

## Need More Details?

See full documentation:
- `RESPONSES_CONTROLLER_TESTING.md` - Complete testing guide
- `TASK-029-SUMMARY.md` - Implementation details
