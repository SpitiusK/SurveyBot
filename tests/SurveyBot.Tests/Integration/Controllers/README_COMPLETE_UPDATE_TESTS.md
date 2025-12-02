# SurveysControllerCompleteUpdateTests - Test Documentation

**File**: `SurveysControllerCompleteUpdateTests.cs`
**Endpoint**: `PUT /api/surveys/{id}/complete`
**Purpose**: Integration tests for complete survey update with atomic question replacement

---

## Test Coverage Overview

### Total Tests: 23

| Category | Count | Status |
|----------|-------|--------|
| **Authentication** | 4 | ✓ Implemented |
| **Authorization** | 2 | ✓ Implemented |
| **Validation** | 5 | ✓ Implemented |
| **Success Scenarios** | 6 | ✓ Implemented |
| **Error Scenarios** | 3 | ✓ Implemented |
| **Complex Scenarios** | 3 | ✓ Implemented |

---

## Test Scenarios

### 1. Authentication Tests (4 tests)

Tests that verify JWT token validation and authentication requirements.

#### `UpdateSurveyComplete_WithoutAuthentication_Returns401Unauthorized`
- **Purpose**: Verify endpoint requires authentication
- **Setup**: No Authorization header
- **Expected**: 401 Unauthorized
- **Validates**: Anonymous access blocked

#### `UpdateSurveyComplete_WithInvalidToken_Returns401Unauthorized`
- **Purpose**: Verify invalid tokens are rejected
- **Setup**: Malformed Bearer token
- **Expected**: 401 Unauthorized
- **Validates**: Token validation works

#### `UpdateSurveyComplete_WithExpiredToken_Returns401Unauthorized`
- **Purpose**: Verify expired tokens are rejected
- **Setup**: Malformed JWT structure
- **Expected**: 401 Unauthorized
- **Validates**: Token expiration checking

#### `UpdateSurveyComplete_WithValidToken_ProceedsToNextStage`
- **Purpose**: Verify valid tokens allow access
- **Setup**: Valid JWT from login
- **Expected**: Not 401 (proceeds to business logic)
- **Validates**: Authentication pipeline works

---

### 2. Authorization Tests (2 tests)

Tests that verify ownership-based access control.

#### `UpdateSurveyComplete_WhenUserDoesNotOwnSurvey_Returns403Forbidden`
- **Purpose**: Prevent users from updating surveys they don't own
- **Setup**:
  - User A creates survey
  - User B attempts to update it
- **Expected**: 403 Forbidden
- **Validates**: Ownership verification
- **Error Message**: Contains "permission"

#### `UpdateSurveyComplete_WhenUserOwnsSurvey_Returns200OK`
- **Purpose**: Allow survey owners to update
- **Setup**: User updates their own survey
- **Expected**: 200 OK
- **Validates**: Owner access granted

---

### 3. Validation Tests (5 tests)

Tests that verify input validation rules.

#### `UpdateSurveyComplete_WithEmptyTitle_Returns400BadRequest`
- **Purpose**: Validate title required
- **Setup**: Empty string title
- **Expected**: 400 Bad Request
- **Validates**: Title presence check

#### `UpdateSurveyComplete_WithShortTitle_Returns400BadRequest`
- **Purpose**: Validate minimum title length
- **Setup**: Title = "AB" (< 3 characters)
- **Expected**: 400 Bad Request
- **Validates**: MinLength attribute

#### `UpdateSurveyComplete_WithEmptyQuestions_Returns400BadRequest`
- **Purpose**: Validate at least one question required
- **Setup**: Empty Questions array
- **Expected**: 400 Bad Request
- **Validates**: MinLength(1) on Questions
- **Error Message**: Contains "question"

#### `UpdateSurveyComplete_WithInvalidQuestionIndexReference_Returns400BadRequest`
- **Purpose**: Validate question indexes are within bounds
- **Setup**: DefaultNextQuestionIndex = 999 (only 2 questions)
- **Expected**: 400 Bad Request
- **Validates**: Index validation logic
- **Error Message**: Contains "out of bounds"

#### `UpdateSurveyComplete_WithSelfReferencingQuestion_Returns400BadRequest`
- **Purpose**: Prevent question from referencing itself
- **Setup**: Question at index 1 has DefaultNextQuestionIndex = 1
- **Expected**: 400 Bad Request
- **Validates**: Self-reference prevention
- **Error Message**: Contains "reference itself"

---

### 4. Success Scenarios (6 tests)

Tests that verify successful update operations.

#### `UpdateSurveyComplete_WithValidData_Returns200AndUpdatedSurvey`
- **Purpose**: Verify basic update flow works
- **Setup**: Valid DTO with 3 questions
- **Expected**: 200 OK with SurveyDto
- **Validates**:
  - Success flag true
  - Title updated
  - Description updated

#### `UpdateSurveyComplete_CreatesNewQuestionIds_DifferentFromTemporaryIds`
- **Purpose**: Verify questions get new database IDs
- **Setup**: Create 3 questions
- **Expected**: 200 OK
- **Validates**:
  - All question IDs > 0
  - All question IDs unique
  - Questions count = 3

#### `UpdateSurveyComplete_UpdatesSurveyMetadata_Correctly`
- **Purpose**: Verify survey properties updated
- **Setup**: Update all survey fields
- **Expected**: 200 OK
- **Validates**:
  - Title updated
  - Description updated
  - AllowMultipleResponses updated
  - ShowResults updated

#### `UpdateSurveyComplete_WithQuestionsInCorrectOrder_MaintainsOrderIndex`
- **Purpose**: Verify question ordering preserved
- **Setup**: 3 questions with OrderIndex 0, 1, 2
- **Expected**: 200 OK
- **Validates**:
  - Questions count = 3
  - Question order matches input
  - OrderIndex values correct

#### `UpdateSurveyComplete_WithActivateAfterUpdate_ActivatesSurvey`
- **Purpose**: Verify activation flag works
- **Setup**: ActivateAfterUpdate = true
- **Expected**: 200 OK
- **Validates**: Survey.IsActive = true

#### `UpdateSurveyComplete_WithMultipleQuestionTypes_CreatesAllCorrectly`
- **Purpose**: Verify all question types supported
- **Setup**: Create Text, SingleChoice, MultipleChoice, Rating questions
- **Expected**: 200 OK
- **Validates**:
  - All 4 questions created
  - Question types match input

---

### 5. Error Scenarios (3 tests)

Tests that verify error handling.

#### `UpdateSurveyComplete_WithNonExistentSurvey_Returns404NotFound`
- **Purpose**: Handle missing survey gracefully
- **Setup**: Update surveyId = 99999 (doesn't exist)
- **Expected**: 404 Not Found
- **Validates**:
  - Error response
  - Message contains "not found"

#### `UpdateSurveyComplete_WithCycleInFlow_Returns409Conflict`
- **Purpose**: Prevent cyclic question flows
- **Setup**:
  - Q0 -> Q1
  - Q1 -> Q2
  - Q2 -> Q0 (cycle!)
  - ActivateAfterUpdate = true
- **Expected**: 400 Bad Request or 409 Conflict
- **Validates**:
  - Cycle detection works
  - Error message mentions "cycle"

#### `UpdateSurveyComplete_WithSingleChoiceAndInvalidOptionFlow_Returns400BadRequest`
- **Purpose**: Validate option flow indexes
- **Setup**: SingleChoice with OptionNextQuestionIndexes[0] = 999
- **Expected**: 400 Bad Request
- **Validates**: Option flow validation

---

### 6. Complex Scenarios (3 tests)

Tests that verify end-to-end workflows.

#### `UpdateSurveyComplete_WithMultipleQuestionTypes_CreatesAllCorrectly`
- **Purpose**: Test mixed question types
- **Setup**: Text, SingleChoice, MultipleChoice, Rating
- **Expected**: 200 OK
- **Validates**:
  - All types created
  - All types present in result

#### `UpdateSurveyComplete_ReplacesOldQuestions_WithNewOnes`
- **Purpose**: Verify old questions deleted and replaced
- **Setup**:
  - Create survey with 2 old questions
  - Update with 3 new questions
- **Expected**: 200 OK
- **Validates**:
  - Question count = 3 (not 5)
  - All questions have "New" prefix
  - No questions have "Old" prefix

---

## Test Patterns Used

### 1. WebApplicationFactory Pattern
```csharp
public class SurveysControllerCompleteUpdateTests :
    IClassFixture<WebApplicationFactoryFixture<Program>>
```
- In-memory test server
- Real HTTP requests/responses
- Full middleware pipeline

### 2. FluentAssertions
```csharp
response.StatusCode.Should().Be(HttpStatusCode.OK);
result.Data!.Title.Should().Be("Updated Title");
```
- Readable assertions
- Better error messages

### 3. Helper Methods
```csharp
private async Task<string> GetAuthTokenAsync(long telegramId = 123456789)
private Task<int> CreateTestSurveyAsync(int userId, string title = "Test Survey")
private UpdateSurveyWithQuestionsDto BuildValidUpdateDto(...)
```
- Reduce code duplication
- Improve test readability
- Consistent test data creation

### 4. Arrange-Act-Assert (AAA)
```csharp
// Arrange
_factory.ClearDatabase();
var dto = BuildValidUpdateDto();

// Act
var response = await _client.PutAsJsonAsync($"/api/surveys/{id}/complete", dto);

// Assert
response.StatusCode.Should().Be(HttpStatusCode.OK);
```
- Clear test structure
- Easy to understand
- Industry standard

---

## Running the Tests

### Run All Complete Update Tests
```bash
dotnet test --filter "FullyQualifiedName~SurveysControllerCompleteUpdateTests"
```

### Run Specific Test Category
```bash
# Authentication tests
dotnet test --filter "FullyQualifiedName~SurveysControllerCompleteUpdateTests.UpdateSurveyComplete_WithoutAuthentication"

# Authorization tests
dotnet test --filter "FullyQualifiedName~SurveysControllerCompleteUpdateTests.UpdateSurveyComplete_WhenUserDoesNotOwnSurvey"

# Validation tests
dotnet test --filter "FullyQualifiedName~SurveysControllerCompleteUpdateTests.UpdateSurveyComplete_WithEmptyQuestions"
```

### Run With Detailed Output
```bash
dotnet test --filter "FullyQualifiedName~SurveysControllerCompleteUpdateTests" --logger "console;verbosity=detailed"
```

---

## Test Data Patterns

### Default Valid DTO
```csharp
{
    Title = "Updated Survey",
    Description = "Updated description",
    AllowMultipleResponses = false,
    ShowResults = true,
    ActivateAfterUpdate = false,
    Questions = [
        {
            QuestionText = "Question 1",
            QuestionType = QuestionType.Text,
            IsRequired = true,
            OrderIndex = 0,
            DefaultNextQuestionIndex = 1
        },
        {
            QuestionText = "Question 2",
            QuestionType = QuestionType.Text,
            IsRequired = true,
            OrderIndex = 1,
            DefaultNextQuestionIndex = null // End survey
        }
    ]
}
```

### Test Users
```csharp
// Default test user
TelegramId: 123456789
Username: "testuser"

// Owner user (for authorization tests)
TelegramId: 111111111
Username: "owner"

// Other user (for forbidden tests)
TelegramId: 222222222
Username: "otherUser"
```

---

## Assertions Reference

### HTTP Status Codes
```csharp
response.StatusCode.Should().Be(HttpStatusCode.OK);              // 200
response.StatusCode.Should().Be(HttpStatusCode.BadRequest);      // 400
response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);    // 401
response.StatusCode.Should().Be(HttpStatusCode.Forbidden);       // 403
response.StatusCode.Should().Be(HttpStatusCode.NotFound);        // 404
response.StatusCode.Should().Be(HttpStatusCode.Conflict);        // 409
```

### Response Content
```csharp
result.Should().NotBeNull();
result!.Success.Should().BeTrue();
result.Data.Should().NotBeNull();
result.Message.Should().Contain("success");
```

### Collections
```csharp
result.Data!.Questions.Should().HaveCount(3);
result.Data.Questions.Should().AllSatisfy(q => q.Id > 0);
result.Data.Questions.Should().OnlyHaveUniqueItems();
```

---

## Known Limitations

### Cycle Detection
- Tests assume cycle detection is implemented in service layer
- May return 400 or 409 depending on implementation
- Test uses `Should().BeOneOf()` to allow both status codes

### Database Cleanup
- `_factory.ClearDatabase()` called in each test
- Tests are independent
- No cross-test data pollution

### Token Expiration
- No actual expiration testing (requires time manipulation)
- Malformed token used as proxy for expired token

---

## Future Test Additions

### Recommended Additional Tests

1. **Performance Tests**
   - Large survey update (100 questions)
   - Update with complex flow (many branches)

2. **Concurrency Tests**
   - Multiple users updating same survey
   - Race condition handling

3. **Data Integrity Tests**
   - Verify old responses deleted (if cascade)
   - Verify survey code unchanged

4. **Edge Cases**
   - Maximum title length (500 chars)
   - Maximum description length (2000 chars)
   - Maximum questions (100)

---

## Maintenance Notes

### When to Update These Tests

1. **API Contract Changes**
   - New validation rules added
   - DTO properties changed
   - Response format modified

2. **Business Logic Changes**
   - Ownership rules changed
   - Question flow logic updated
   - Activation logic modified

3. **Error Handling Changes**
   - New exception types
   - Different HTTP status codes
   - Error message format changes

### Test Stability

- Tests use in-memory database (fast, isolated)
- No external dependencies (no real PostgreSQL)
- Deterministic (same input = same output)
- Can run in parallel (with fixture isolation)

---

**Last Updated**: 2025-12-01
**Version**: 1.0.0
**Test Framework**: xUnit 2.9.2
**Assertion Library**: FluentAssertions 7.0.0-alpha.4
**Test Runner**: dotnet test
