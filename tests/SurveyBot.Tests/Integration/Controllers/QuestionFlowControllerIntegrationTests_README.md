# QuestionFlowControllerIntegrationTests - TEST-003

## Overview

This file contains **14 comprehensive API integration tests** for the Conditional Question Flow feature (TEST-003). These tests verify HTTP endpoints, status codes, request/response serialization, authorization, and error handling with cycle detection.

**Test File**: `QuestionFlowControllerIntegrationTests.cs`
**Location**: `tests/SurveyBot.Tests/Integration/Controllers/`
**Framework**: xUnit + FluentAssertions + WebApplicationFactory

---

## Test Coverage Summary

### Total Tests: 14

1. **QuestionFlowController.GetQuestionFlow**: 3 tests
2. **QuestionFlowController.UpdateQuestionFlow**: 3 tests
3. **QuestionFlowController.ValidateSurveyFlow**: 2 tests
4. **ResponsesController.GetNextQuestion**: 2 tests
5. **SurveysController.ActivateSurvey**: 4 tests

---

## Test Breakdown by Controller

### 1. QuestionFlowController - GetQuestionFlow (3 tests)

#### `GetQuestionFlow_ValidQuestion_Returns200WithFlowConfiguration`
- **Endpoint**: `GET /api/surveys/{surveyId}/questions/{questionId}/flow`
- **Auth**: Required (Bearer JWT)
- **Expected**: 200 OK
- **Verifies**:
  - Returns `ConditionalFlowDto`
  - Includes `QuestionId`, `SupportsBranching`
  - For branching questions: includes `OptionFlows` with 2 options
  - Response structure matches DTO specification

#### `GetQuestionFlow_NonExistentQuestion_Returns404NotFound`
- **Endpoint**: `GET /api/surveys/{surveyId}/questions/{99999}/flow`
- **Auth**: Required
- **Expected**: 404 Not Found
- **Verifies**:
  - Non-existent question returns 404
  - Error response includes `Success: false`
  - Appropriate error message

#### `GetQuestionFlow_WithoutAuthorization_Returns401Unauthorized`
- **Endpoint**: `GET /api/surveys/{surveyId}/questions/{questionId}/flow`
- **Auth**: None (intentionally omitted)
- **Expected**: 401 Unauthorized
- **Verifies**:
  - Protected endpoint requires authentication
  - Returns 401 when no Bearer token provided

---

### 2. QuestionFlowController - UpdateQuestionFlow (3 tests)

#### `UpdateQuestionFlow_ValidBranchingUpdate_Returns200WithUpdatedFlow`
- **Endpoint**: `PUT /api/surveys/{surveyId}/questions/{questionId}/flow`
- **Auth**: Required
- **Payload**: `UpdateQuestionFlowDto` with `OptionNextQuestions`
- **Expected**: 200 OK
- **Verifies**:
  - Updates branching flow (Option 0 → Q2, Option 1 → Q4)
  - Returns updated `ConditionalFlowDto`
  - OptionFlows reflect new NextQuestionId values
  - Changes persisted correctly

#### `UpdateQuestionFlow_ValidNonBranchingUpdate_Returns200WithUpdatedFlow`
- **Endpoint**: `PUT /api/surveys/{surveyId}/questions/{questionId}/flow`
- **Auth**: Required
- **Payload**: `UpdateQuestionFlowDto` with `DefaultNextQuestionId`
- **Expected**: 200 OK
- **Verifies**:
  - Updates non-branching text question flow
  - Sets `DefaultNextQuestionId` to skip questions
  - Returns updated DTO with new default

#### `UpdateQuestionFlow_CycleCausingUpdate_Returns400WithCycleDetails`
- **Endpoint**: `PUT /api/surveys/{surveyId}/questions/{questionId}/flow`
- **Auth**: Required
- **Payload**: DTO that would create Q2 → Q1 cycle
- **Expected**: 400 Bad Request
- **Verifies**:
  - Cycle detection prevents invalid flow update
  - Error message contains "cycle"
  - Response includes `cyclePath` in Data property
  - Changes rolled back (not persisted)

---

### 3. QuestionFlowController - ValidateSurveyFlow (2 tests)

#### `ValidateSurveyFlow_ValidSurvey_Returns200WithSuccess`
- **Endpoint**: `POST /api/surveys/{surveyId}/questions/validate`
- **Auth**: Required
- **Expected**: 200 OK
- **Verifies**:
  - Valid survey passes validation
  - Response includes `valid: true`
  - Response includes `endpointCount > 0`
  - No errors or cyclePath in response

#### `ValidateSurveyFlow_SurveyWithCycle_Returns200WithCycleError`
- **Endpoint**: `POST /api/surveys/{surveyId}/questions/validate`
- **Auth**: Required
- **Expected**: 200 OK (validation endpoint returns 200 even with errors)
- **Verifies**:
  - Survey with cycle (Q1 → Q2 → Q1) detected
  - Response includes `valid: false`
  - Response includes `cyclePath` array
  - Response includes `errors` array with cycle description

---

### 4. ResponsesController - GetNextQuestion (2 tests)

#### `GetNextQuestion_ValidResponse_Returns200WithNextQuestion`
- **Endpoint**: `GET /api/responses/{id}/next-question`
- **Auth**: **None** (public endpoint for bot)
- **Expected**: 200 OK
- **Verifies**:
  - Returns `QuestionDto` for next unanswered question
  - Public endpoint accessible without auth
  - Question ID matches expected (Q1 or Q2)
  - DTO includes question text, type, options

#### `GetNextQuestion_SurveyComplete_Returns204NoContent`
- **Endpoint**: `GET /api/responses/{id}/next-question`
- **Auth**: None (public)
- **Expected**: 204 No Content
- **Verifies**:
  - Completed response (all questions answered) returns 204
  - No response body
  - Indicates survey completion

---

### 5. SurveysController - ActivateSurvey (4 tests)

#### `ActivateSurvey_ValidSurvey_Returns200WithActivatedSurvey`
- **Endpoint**: `POST /api/surveys/{id}/activate`
- **Auth**: Required
- **Expected**: 200 OK
- **Verifies**:
  - Valid survey activates successfully
  - Response includes `SurveyDto`
  - `IsActive` flag set to `true`
  - Success message returned

#### `ActivateSurvey_SurveyWithCycle_Returns400WithCycleError`
- **Endpoint**: `POST /api/surveys/{id}/activate`
- **Auth**: Required
- **Expected**: 400 Bad Request
- **Verifies**:
  - Survey with cycle cannot be activated
  - Error message contains "cycle"
  - Response includes `cyclePath` in Data
  - Survey remains inactive

#### `ActivateSurvey_NonExistentSurvey_Returns404NotFound`
- **Endpoint**: `POST /api/surveys/{99999}/activate`
- **Auth**: Required
- **Expected**: 404 Not Found
- **Verifies**:
  - Non-existent survey returns 404
  - Error response includes `Success: false`
  - Appropriate error message

#### `ActivateSurvey_WithoutAuthorization_Returns401Unauthorized`
- **Endpoint**: `POST /api/surveys/{id}/activate`
- **Auth**: None
- **Expected**: 401 Unauthorized
- **Verifies**:
  - Protected endpoint requires authentication
  - Returns 401 when no Bearer token

---

## Helper Methods

### `GetAuthTokenAsync(long telegramId = 123456789)`
- Calls `/api/auth/login` with Telegram ID
- Returns JWT token from `LoginResponseDto.Token`
- Used by all authenticated tests

### `SeedTestSurvey()`
- Creates user, survey, 4 questions with branching flow
- Sets up SingleChoice question (Q1) with 2 options:
  - "Yes" → Q2 (follow-up)
  - "No" → Q3 (different path)
- Returns tuple: `(surveyId, q1Id, q2Id, q3Id, q4Id)`

### `SeedSurveyWithCycle()`
- Creates survey with intentional cycle (Q1 → Q2 → Q1)
- Used for cycle detection tests
- Returns tuple: `(surveyId, q1Id, q2Id)`

---

## Key Testing Patterns

### 1. HTTP Status Code Verification
```csharp
response.StatusCode.Should().Be(HttpStatusCode.OK);
response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
```

### 2. Response Deserialization
```csharp
var content = await response.Content.ReadFromJsonAsync<ApiResponse<ConditionalFlowDto>>();
content.Should().NotBeNull();
content!.Success.Should().BeTrue();
content.Data.Should().NotBeNull();
```

### 3. Cycle Detection Verification
```csharp
var dataElement = ((JsonElement)content.Data!);
dataElement.TryGetProperty("cyclePath", out var cyclePath).Should().BeTrue();
cyclePath.ValueKind.Should().NotBe(JsonValueKind.Null);
```

### 4. Authorization Testing
```csharp
// WITH auth
_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

// WITHOUT auth
var clientNoAuth = _factory.CreateClient();
```

---

## Test Data Structure

### Sample Survey Flow

```
Q1 (SingleChoice): "Do you like this feature?"
├─ Option 0 ("Yes") → Q2
└─ Option 1 ("No") → Q3

Q2 (Text): "What do you like most?"
└─ → Q4

Q3 (Text): "What would you improve?"
└─ → Q4

Q4 (Text): "Any additional comments?"
└─ → [End of Survey]
```

### Sample Cycle

```
Q1 → Q2 → Q1 (CYCLE!)
```

---

## Success Criteria Met

- ✅ **10+ tests**: 14 tests total
- ✅ **WebApplicationFactory**: Real HTTP integration testing
- ✅ **Success scenarios**: Valid flows return 200/204
- ✅ **Error scenarios**: Cycles return 400, not found returns 404
- ✅ **Status codes**: 200, 204, 400, 401, 404 verified
- ✅ **Response DTOs**: ConditionalFlowDto, QuestionDto, SurveyDto verified
- ✅ **Authorization**: Protected endpoints return 401 without token
- ✅ **Cycle detection**: Error responses include cyclePath details
- ✅ **FluentAssertions**: Readable assertion syntax used throughout

---

## Running the Tests

### Run all flow tests
```bash
dotnet test --filter "FullyQualifiedName~QuestionFlowControllerIntegrationTests"
```

### Run specific test
```bash
dotnet test --filter "FullyQualifiedName~GetQuestionFlow_ValidQuestion_Returns200"
```

### Run with detailed output
```bash
dotnet test --filter "QuestionFlowControllerIntegrationTests" --logger "console;verbosity=detailed"
```

---

## Dependencies

**NuGet Packages**:
- `xUnit` - Test framework
- `FluentAssertions` - Readable assertions
- `Microsoft.AspNetCore.Mvc.Testing` - WebApplicationFactory for integration tests
- `System.Net.Http.Json` - JSON extension methods

**Test Fixtures**:
- `WebApplicationFactoryFixture<Program>` - In-memory test server
- `EntityBuilder` - Test entity creation helpers
- `ApiResponse<T>` - Standardized API response wrapper

---

## Notes

1. **Public Endpoints**: `GetNextQuestion` is intentionally public (no auth) for bot integration
2. **Validation Endpoint**: Returns 200 OK even with validation errors (errors in body)
3. **Cycle Detection**: Validates before saving, rolls back on cycle
4. **In-Memory Database**: Each test uses isolated in-memory database
5. **JWT Tokens**: Generated via real login endpoint for authentic testing

---

**Last Updated**: 2025-11-21
**TEST-003 Status**: ✅ Complete - 14 API endpoint tests implemented
