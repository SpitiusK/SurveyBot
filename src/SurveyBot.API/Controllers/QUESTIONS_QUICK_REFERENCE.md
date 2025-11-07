# QuestionsController Quick Reference

## Endpoints Overview

```
POST   /api/surveys/{surveyId}/questions       Add question
PUT    /api/questions/{id}                     Update question
DELETE /api/questions/{id}                     Delete question
GET    /api/surveys/{surveyId}/questions       List questions
POST   /api/surveys/{surveyId}/questions/reorder   Reorder questions
```

## Quick Test Examples

### 1. Add Text Question
```bash
curl -X POST "https://localhost:5001/api/surveys/1/questions" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"questionText":"Your name?","questionType":0,"isRequired":true}'
```

### 2. Add Single Choice Question
```bash
curl -X POST "https://localhost:5001/api/surveys/1/questions" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"questionText":"Favorite language?","questionType":1,"isRequired":true,"options":["C#","Python","Java"]}'
```

### 3. Add Rating Question
```bash
curl -X POST "https://localhost:5001/api/surveys/1/questions" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"questionText":"Rate us","questionType":3,"isRequired":true}'
```

### 4. List Questions
```bash
curl "https://localhost:5001/api/surveys/1/questions"
```

### 5. Reorder Questions
```bash
curl -X POST "https://localhost:5001/api/surveys/1/questions/reorder" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"questionIds":[3,1,2]}'
```

## Question Types

| Type | Value | Options? |
|------|-------|----------|
| Text | 0 | No |
| SingleChoice | 1 | Yes (2-10) |
| MultipleChoice | 2 | Yes (2-10) |
| Rating | 3 | No |

## Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success (GET, PUT) |
| 201 | Created (POST) |
| 204 | Deleted (DELETE) |
| 400 | Bad Request / Validation |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 500 | Server Error |

## Common Errors

**"Choice-based questions must have at least 2 options"**
- Add `options` array with 2-10 items for SingleChoice/MultipleChoice

**"Text and Rating questions should not have options"**
- Remove `options` field for Text (0) and Rating (3) questions

**"Cannot modify question that has existing responses"**
- Question has responses, cannot be modified or deleted

**"You do not have permission to modify this survey"**
- User doesn't own the survey

## Authorization

- **Required:** POST, PUT, DELETE, reorder
- **Optional:** GET (public for active surveys)

## See Also

- Full Testing Guide: `QUESTIONS_CONTROLLER_TESTING.md`
- Swagger UI: `https://localhost:5001/swagger`
