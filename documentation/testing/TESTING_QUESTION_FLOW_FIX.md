# Quick Testing Guide: Question Flow Options Validation Fix

**Issue Fixed**: Missing Options collection during question flow validation
**Testing Status**: ⏳ Pending Manual Verification

---

## Prerequisites

1. **API running**: `cd src/SurveyBot.API && dotnet run`
2. **Database up**: `docker-compose up -d`
3. **JWT token**: Login via `/api/auth/login` first

---

## Test Scenario 1: SingleChoice Question Flow (Happy Path)

### Step 1: Create Survey

```bash
POST http://localhost:5000/api/surveys
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "title": "Flow Test Survey",
  "description": "Testing question flow with options"
}
```

**Expected**: 201 Created
**Save**: `surveyId` from response

---

### Step 2: Create SingleChoice Question

```bash
POST http://localhost:5000/api/surveys/{surveyId}/questions
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "questionText": "What is your favorite color?",
  "questionType": 1,
  "isRequired": true,
  "options": ["Red", "Blue", "Green"]
}
```

**Expected**: 201 Created

**Response Structure**:
```json
{
  "success": true,
  "data": {
    "id": 57,
    "questionText": "What is your favorite color?",
    "questionType": 1,
    "options": ["Red", "Blue", "Green"],
    "optionDetails": [
      { "id": 58, "text": "Red", "orderIndex": 0 },
      { "id": 59, "text": "Blue", "orderIndex": 1 },
      { "id": 60, "text": "Green", "orderIndex": 2 }
    ]
  }
}
```

**CRITICAL**: Save option IDs from `optionDetails` array!
- `optionId1` = 58
- `optionId2` = 59
- `optionId3` = 60

---

### Step 3: Configure Question Flow (THE FIX TEST)

```bash
PUT http://localhost:5000/api/surveys/{surveyId}/questions/{questionId}/flow
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "defaultNext": null,
  "optionNextDeterminants": {
    "58": {
      "type": 1,
      "nextQuestionId": null
    },
    "59": {
      "type": 1,
      "nextQuestionId": null
    },
    "60": {
      "type": 1,
      "nextQuestionId": null
    }
  }
}
```

**BEFORE FIX**: ❌ 400 Bad Request
```json
{
  "success": false,
  "message": "Option 58 does not belong to question 57"
}
```

**AFTER FIX**: ✅ 200 OK
```json
{
  "success": true,
  "data": {
    "questionId": 57,
    "supportsBranching": true,
    "optionFlows": [
      {
        "optionId": 58,
        "optionText": "Red",
        "nextDeterminant": {
          "type": "EndSurvey",
          "nextQuestionId": null
        }
      },
      {
        "optionId": 59,
        "optionText": "Blue",
        "nextDeterminant": {
          "type": "EndSurvey",
          "nextQuestionId": null
        }
      },
      {
        "optionId": 60,
        "optionText": "Green",
        "nextDeterminant": {
          "type": "EndSurvey",
          "nextQuestionId": null
        }
      }
    ]
  },
  "message": "Flow configuration updated successfully"
}
```

---

## Test Scenario 2: Invalid Option ID (Error Case)

### Test with Non-Existent Option ID

```bash
PUT http://localhost:5000/api/surveys/{surveyId}/questions/{questionId}/flow
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "optionNextDeterminants": {
    "999": {
      "type": 1,
      "nextQuestionId": null
    }
  }
}
```

**Expected**: ❌ 400 Bad Request
```json
{
  "success": false,
  "message": "Option 999 does not belong to question 57"
}
```

**Log Output** (check console):
```
[WARN] ❌ Option 999 does not belong to question 57
[WARN]    Available option IDs: 58, 59, 60
```

---

## Test Scenario 3: Branching Flow (GoToQuestion)

### Step 1: Create Second Question

```bash
POST http://localhost:5000/api/surveys/{surveyId}/questions
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "questionText": "Additional question",
  "questionType": 0,
  "isRequired": false
}
```

**Expected**: 201 Created
**Save**: `question2Id` from response

---

### Step 2: Configure Branching Flow

```bash
PUT http://localhost:5000/api/surveys/{surveyId}/questions/{question1Id}/flow
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "optionNextDeterminants": {
    "58": {
      "type": 0,
      "nextQuestionId": {question2Id}
    },
    "59": {
      "type": 1,
      "nextQuestionId": null
    },
    "60": {
      "type": 1,
      "nextQuestionId": null
    }
  }
}
```

**Expected**: ✅ 200 OK
**Result**:
- Option "Red" → Goes to Question 2
- Options "Blue" and "Green" → End Survey

---

## Log Verification

### Expected Log Output (Success)

When testing with **valid option IDs**:

```
[INFO] 📨 UPDATE QUESTION FLOW REQUEST
[INFO]   Survey ID: 1
[INFO]   Question ID: 57
[INFO]   User ID: 1
[INFO]   OptionNextDeterminants: 3 option(s)
[INFO]     - Option 58:
[INFO]         Type: EndSurvey (1)
[INFO]         NextQuestionId: NULL
[INFO]     - Option 59:
[INFO]         Type: EndSurvey (1)
[INFO]         NextQuestionId: NULL
[INFO]     - Option 60:
[INFO]         Type: EndSurvey (1)
[INFO]         NextQuestionId: NULL
[INFO] ✅ MODEL STATE VALIDATION PASSED
[INFO] Getting question entity 57 with Options
[INFO] ✅ Question 57 loaded with 2 options
[INFO]   Available option IDs: 58, 59, 60
[INFO] ✅ Option 58 validated successfully
[INFO] ✅ Option 59 validated successfully
[INFO] ✅ Option 60 validated successfully
[INFO] 🔄 Calling QuestionService.UpdateQuestionFlowAsync
[INFO] ✅ Service layer completed successfully
[INFO] ✅ UPDATE QUESTION FLOW COMPLETED SUCCESSFULLY
```

### Expected Log Output (Validation Error)

When testing with **invalid option ID** (999):

```
[INFO] 📨 UPDATE QUESTION FLOW REQUEST
[INFO]   OptionNextDeterminants: 1 option(s)
[INFO]     - Option 999:
[INFO] ✅ MODEL STATE VALIDATION PASSED
[INFO] Getting question entity 57 with Options
[INFO] ✅ Question 57 loaded with 2 options
[INFO]   Available option IDs: 58, 59, 60
[WARN] ❌ Option 999 does not belong to question 57
[WARN]    Available option IDs: 58, 59, 60
```

---

## Database Verification

### Query 1: Verify Question Exists

```sql
SELECT id, question_text, question_type, survey_id
FROM questions
WHERE id = 57;
```

**Expected**: 1 row with question details

---

### Query 2: Verify Options Exist

```sql
SELECT id, question_id, text, order_index
FROM question_options
WHERE question_id = 57
ORDER BY order_index;
```

**Expected**: 3 rows (options for the question)

```
| id | question_id | text   | order_index |
|----|-------------|--------|-------------|
| 58 | 57          | Red    | 0           |
| 59 | 57          | Blue   | 1           |
| 60 | 57          | Green  | 2           |
```

---

### Query 3: Verify Flow Configuration (After Update)

```sql
SELECT id, question_id, text, next_step_type, next_question_id
FROM question_options
WHERE question_id = 57
ORDER BY order_index;
```

**Expected** (EndSurvey type):
```
| id | question_id | text   | next_step_type | next_question_id |
|----|-------------|--------|----------------|------------------|
| 58 | 57          | Red    | EndSurvey      | NULL             |
| 59 | 57          | Blue   | EndSurvey      | NULL             |
| 60 | 57          | Green  | EndSurvey      | NULL             |
```

---

## API Performance Benchmark

### Test Setup

1. Create survey with 1 SingleChoice question (5 options)
2. Measure flow update time

### Expected Performance

| Metric | Before Fix | After Fix | Improvement |
|--------|------------|-----------|-------------|
| SQL Queries | 6 (N+1) | 1 (JOIN) | **6x reduction** |
| Response Time | ~30ms | ~10ms | **3x faster** |
| Database Load | High (6 roundtrips) | Low (1 roundtrip) | **Significantly reduced** |

### Measurement Tool

Use browser DevTools Network tab or `curl` with timing:

```bash
curl -w "@curl-format.txt" -o /dev/null -s \
  -X PUT "http://localhost:5000/api/surveys/{surveyId}/questions/{questionId}/flow" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{...}'
```

**curl-format.txt**:
```
time_namelookup:  %{time_namelookup}\n
time_connect:  %{time_connect}\n
time_starttransfer:  %{time_starttransfer}\n
time_total:  %{time_total}\n
```

---

## Swagger UI Testing

### Using Swagger UI

1. Navigate to `http://localhost:5000/swagger`
2. Click **Authorize** button
3. Enter: `Bearer YOUR_JWT_TOKEN`
4. Find **Question Flow** section
5. Test endpoint: `PUT /api/surveys/{surveyId}/questions/{questionId}/flow`

### Test Data

**Valid Request**:
```json
{
  "defaultNext": null,
  "optionNextDeterminants": {
    "58": {
      "type": 1,
      "nextQuestionId": null
    }
  }
}
```

**Expected**: 200 OK with flow configuration

---

## Integration Test Update

### Recommended Test Case

**File**: `tests/SurveyBot.Tests/Integration/Controllers/QuestionFlowControllerIntegrationTests.cs`

**Add Test**:
```csharp
[Fact]
public async Task UpdateQuestionFlow_WithValidOptionIds_ReturnsSuccess()
{
    // Arrange
    var survey = await CreateSurveyAsync();
    var question = await CreateSingleChoiceQuestionAsync(survey.Id, 3); // 3 options

    var optionIds = question.OptionDetails.Select(o => o.Id).ToList();
    var updateDto = new UpdateQuestionFlowDto
    {
        OptionNextDeterminants = optionIds.ToDictionary(
            id => id,
            id => new NextQuestionDeterminantDto
            {
                Type = NextStepType.EndSurvey,
                NextQuestionId = null
            })
    };

    // Act
    var response = await _client.PutAsJsonAsync(
        $"/api/surveys/{survey.Id}/questions/{question.Id}/flow",
        updateDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var result = await response.Content.ReadFromJsonAsync<ApiResponse<ConditionalFlowDto>>();
    result.Success.Should().BeTrue();
    result.Data.OptionFlows.Should().HaveCount(3);
}

[Fact]
public async Task UpdateQuestionFlow_WithInvalidOptionId_ReturnsBadRequest()
{
    // Arrange
    var survey = await CreateSurveyAsync();
    var question = await CreateSingleChoiceQuestionAsync(survey.Id, 2);

    var updateDto = new UpdateQuestionFlowDto
    {
        OptionNextDeterminants = new Dictionary<int, NextQuestionDeterminantDto>
        {
            { 999, new NextQuestionDeterminantDto { Type = NextStepType.EndSurvey } }
        }
    };

    // Act
    var response = await _client.PutAsJsonAsync(
        $"/api/surveys/{survey.Id}/questions/{question.Id}/flow",
        updateDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
    result.Success.Should().BeFalse();
    result.Message.Should().Contain("does not belong to question");
}
```

---

## Checklist

### Pre-Testing
- [ ] API running (`dotnet run`)
- [ ] Database up (`docker ps`)
- [ ] JWT token obtained (`/api/auth/login`)
- [ ] Swagger UI accessible (`http://localhost:5000/swagger`)

### Test Execution
- [ ] Test 1: Valid option IDs (happy path) → ✅ 200 OK
- [ ] Test 2: Invalid option ID → ❌ 400 Bad Request
- [ ] Test 3: Branching flow (GoToQuestion) → ✅ 200 OK
- [ ] Check logs for diagnostic output
- [ ] Verify database records

### Post-Testing
- [ ] Performance benchmark (response time < 20ms)
- [ ] SQL query verification (single LEFT JOIN)
- [ ] Integration tests updated
- [ ] Documentation updated

---

## Success Criteria

✅ **Fix is working if**:
1. Valid option IDs are accepted (200 OK)
2. Invalid option IDs are rejected with clear error message (400 Bad Request)
3. Options collection is loaded (log shows: "Question X loaded with Y options")
4. Response time < 20ms (single SQL query)
5. No N+1 query problem (verify with SQL profiler)

❌ **Fix failed if**:
1. Valid option IDs still rejected
2. Options collection empty in logs
3. Multiple SQL queries (N+1 problem persists)
4. Errors in build/runtime

---

## Troubleshooting

### Issue: Still Getting 400 Bad Request

**Check**:
1. Are you using correct option IDs from `optionDetails`?
2. Is the question a SingleChoice/Rating type? (branching supported)
3. Check logs: Does it show "Question X loaded with Y options"?

### Issue: No Logs Appearing

**Check**:
1. Is Serilog configured correctly?
2. Is log level set to Information or lower?
3. Check `appsettings.Development.json` for logging config

### Issue: Database Connection Error

**Check**:
1. `docker ps` - Is PostgreSQL container running?
2. Connection string in `appsettings.json`
3. Run migrations: `dotnet ef database update`

---

## Contact

**Issue Report**: If testing reveals problems, report with:
- Request payload (JSON)
- Response status + body
- Log output (full context)
- Database query results
- API version (`dotnet --version`)

---

**End of Testing Guide**
