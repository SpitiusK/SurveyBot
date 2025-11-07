# Quick Reference - Logging & Error Handling

## Throw Custom Exceptions

```csharp
// 404 Not Found
throw new NotFoundException("Survey", surveyId);

// 400 Bad Request
throw new BadRequestException("Invalid input");

// 400 Validation Error
throw new ValidationException("Title", "Title is required");

// 401 Unauthorized
throw new UnauthorizedException();

// 403 Forbidden
throw new ForbiddenException();

// 409 Conflict
throw new ConflictException("Survey already exists");
```

## Logging

```csharp
// Information
_logger.LogInformation("Survey {SurveyId} created", id);

// Warning
_logger.LogWarning("Invalid attempt: {Reason}", reason);

// Error
_logger.LogError(ex, "Failed to save survey {SurveyId}", id);

// Debug
_logger.LogDebug("Processing {Count} items", count);

// Structured object
_logger.LogInformation("Created: {@Survey}", survey);
```

## Return Success Responses

```csharp
// With data
return Ok(ApiResponse.Ok(survey));

// With data and message
return Ok(ApiResponse.Ok(survey, "Survey created successfully"));

// Just message
return Ok(ApiResponse.Ok("Operation completed"));

// Direct data
return Ok(new ApiResponse<Survey>(survey));
```

## Test Endpoints

```
GET /api/testerrors/logging       - Test log levels
GET /api/testerrors/error         - Test 500 error
GET /api/testerrors/not-found     - Test 404 error
GET /api/testerrors/validation    - Test validation error
GET /api/testerrors/bad-request   - Test 400 error
GET /api/testerrors/unauthorized  - Test 401 error
GET /api/testerrors/forbidden     - Test 403 error
GET /api/testerrors/conflict      - Test 409 error
```

## Configuration

### Adjust Log Level

In `appsettings.json`:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "YourNamespace": "Debug"
      }
    }
  }
}
```

### Disable Seq

Comment out in `appsettings.json`:
```json
// {
//   "Name": "Seq",
//   "Args": { "serverUrl": "http://localhost:5341" }
// }
```
