# TASK-021: Surveys Controller - Quick Reference

## Endpoints Overview

```
POST   /api/surveys              → Create new survey (201)
GET    /api/surveys              → List surveys with pagination (200)
GET    /api/surveys/{id}         → Get survey details (200)
PUT    /api/surveys/{id}         → Update survey (200)
DELETE /api/surveys/{id}         → Delete survey (204)
POST   /api/surveys/{id}/activate    → Activate survey (200)
POST   /api/surveys/{id}/deactivate  → Deactivate survey (200)
GET    /api/surveys/{id}/statistics  → Get statistics (200)
```

All endpoints require `Authorization: Bearer {token}` header.

---

## Quick Test Commands

### 1. Create Survey
```bash
curl -X POST https://localhost:5001/api/surveys \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Test Survey","description":"Test"}'
```

### 2. List Surveys
```bash
curl -X GET "https://localhost:5001/api/surveys?pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

### 3. Get Survey
```bash
curl -X GET https://localhost:5001/api/surveys/1 \
  -H "Authorization: Bearer $TOKEN"
```

### 4. Update Survey
```bash
curl -X PUT https://localhost:5001/api/surveys/1 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Updated","description":"New desc","allowMultipleResponses":true,"showResults":false}'
```

### 5. Activate Survey
```bash
curl -X POST https://localhost:5001/api/surveys/1/activate \
  -H "Authorization: Bearer $TOKEN"
```

### 6. Deactivate Survey
```bash
curl -X POST https://localhost:5001/api/surveys/1/deactivate \
  -H "Authorization: Bearer $TOKEN"
```

### 7. Get Statistics
```bash
curl -X GET https://localhost:5001/api/surveys/1/statistics \
  -H "Authorization: Bearer $TOKEN"
```

### 8. Delete Survey
```bash
curl -X DELETE https://localhost:5001/api/surveys/1 \
  -H "Authorization: Bearer $TOKEN"
```

---

## Status Codes

- **200** OK - Success (GET, PUT, POST activate/deactivate)
- **201** Created - Survey created
- **204** No Content - Survey deleted
- **400** Bad Request - Validation error or business rule violation
- **401** Unauthorized - Missing/invalid token
- **403** Forbidden - User doesn't own resource
- **404** Not Found - Survey not found
- **500** Internal Server Error - Unexpected error

---

## Key Business Rules

1. Surveys are created as **inactive** by default
2. Must have **at least 1 question** to activate
3. **Active surveys with responses cannot be modified** (deactivate first)
4. Delete = **soft delete** if responses exist, **hard delete** otherwise
5. Only **survey creator** can access/modify their surveys

---

## Query Parameters (List Endpoint)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| pageNumber | int | 1 | Page number (1-based) |
| pageSize | int | 10 | Items per page (max 100) |
| searchTerm | string | null | Search title/description |
| isActive | bool? | null | Filter by active status |
| sortBy | string | "createdat" | Sort field |
| sortDescending | bool | false | Sort direction |

**Sort options:** title, createdat, updatedat, isactive

---

## Response Format

### Success
```json
{
  "success": true,
  "data": { ... },
  "message": "Optional message",
  "timestamp": "2025-01-06T10:00:00Z"
}
```

### Error
```json
{
  "success": false,
  "message": "Error description",
  "timestamp": "2025-01-06T10:00:00Z"
}
```

---

## Common Errors & Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| 401 Unauthorized | No/invalid token | Login and use valid JWT |
| 403 Forbidden | Not your survey | Check survey ownership |
| 404 Not Found | Survey doesn't exist | Verify survey ID |
| 400 (Activate) | No questions | Add questions first |
| 400 (Update) | Active + has responses | Deactivate survey first |

---

## File Location

**Controller:** `src/SurveyBot.API/Controllers/SurveysController.cs`
**Lines:** 576
**Dependencies:** ISurveyService, ILogger

---

## Swagger UI

Access interactive docs: `https://localhost:5001/swagger`
- All endpoints under "Surveys" tag
- Try-it-out functionality available
- Request/response schemas documented

---

## Related Documentation

- Full Testing Guide: `docs/TASK-021-Testing-Guide.md`
- Implementation Summary: `docs/TASK-021-Implementation-Summary.md`
- API Overview: Check Swagger UI when running
