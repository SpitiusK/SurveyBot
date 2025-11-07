# TASK-013: Setup Logging and Error Handling Middleware - COMPLETED

## Task Overview
Successfully implemented comprehensive logging and error handling infrastructure for the SurveyBot API using Serilog and custom middleware.

## Files Created

### 1. Error Response Models (`src/SurveyBot.API/Models/`)
- **ErrorResponse.cs** - Standardized error response model with:
  - Status code, message, details
  - Validation errors dictionary
  - Timestamp and trace ID
  - Conditional details (development only)

- **ApiResponse.cs** - Generic success response wrapper
  - Generic and non-generic versions
  - Consistent JSON format
  - Helper methods (Ok, etc.)

### 2. Custom Exception Classes (`src/SurveyBot.API/Exceptions/`)
- **ApiException.cs** - Base exception with HTTP status code
- **NotFoundException.cs** - HTTP 404 errors
- **ValidationException.cs** - HTTP 400 with field-level validation errors
- **BadRequestException.cs** - HTTP 400 for invalid requests
- **UnauthorizedException.cs** - HTTP 401 for authentication
- **ForbiddenException.cs** - HTTP 403 for authorization
- **ConflictException.cs** - HTTP 409 for conflicts

### 3. Middleware (`src/SurveyBot.API/Middleware/`)
- **GlobalExceptionMiddleware.cs** - Global exception handler
  - Catches all unhandled exceptions
  - Maps to appropriate HTTP status codes
  - Returns standardized error responses
  - Logs with appropriate levels
  - Environment-aware detail exposure

- **RequestLoggingMiddleware.cs** - HTTP request/response logging
  - Logs request method and path
  - Tracks request duration
  - Status code-based log levels

### 4. Extension Methods (`src/SurveyBot.API/Extensions/`)
- **MiddlewareExtensions.cs** - Middleware registration helpers
  - UseGlobalExceptionHandler()
  - UseRequestLogging()

### 5. Test Controller (`src/SurveyBot.API/Controllers/`)
- **TestErrorsController.cs** - Error handling test endpoints
  - Test logging levels
  - Test each exception type
  - Test error response formats

### 6. Configuration Files
- **appsettings.json** - Production Serilog configuration
  - Console, File, and Seq sinks
  - Information level logging
  - 30-day log retention

- **appsettings.Development.json** - Development Serilog configuration
  - Debug level logging
  - 7-day log retention
  - Enhanced console output

### 7. Updated Files
- **Program.cs** - Serilog bootstrap and middleware registration
  - Early Serilog initialization
  - Request logging enrichment
  - Middleware pipeline configuration
  - Graceful shutdown handling

- **SurveyBot.API.csproj** - Added NuGet packages
  - Serilog.AspNetCore (9.0.0)
  - Serilog.Sinks.Seq (9.0.0)

### 8. Documentation
- **LOGGING-ERROR-HANDLING.md** - Comprehensive documentation
  - Component descriptions
  - Usage examples
  - Configuration guide
  - Best practices

## Serilog Configuration

### Sinks Configured
1. **Console Sink**
   - Color-coded output
   - Structured formatting
   - Development-friendly display

2. **File Sink**
   - Daily rolling logs
   - Size-based rolling (10MB limit)
   - Configurable retention (7/30 days)
   - Full timestamp and context

3. **Seq Sink** (Optional)
   - Structured log server
   - URL: http://localhost:5341
   - Rich querying capabilities

### Log Levels
- **Production**: Information and above
- **Development**: Debug and above
- **EF Core SQL**: Information level
- **Microsoft/System**: Warning level

### Enrichers
- FromLogContext
- WithMachineName
- WithThreadId
- Application name
- Environment name

## Error Response Format

### Success Response
```json
{
  "success": true,
  "data": { /* response data */ },
  "message": "Optional message",
  "timestamp": "2025-11-06T10:30:00Z"
}
```

### Error Response
```json
{
  "success": false,
  "statusCode": 400,
  "message": "One or more validation errors occurred.",
  "errors": {
    "Title": ["Title is required"],
    "Description": ["Description cannot be empty"]
  },
  "timestamp": "2025-11-06T10:30:00Z",
  "traceId": "0HMVFE3234D5E:00000001"
}
```

## Test Endpoints

All available at `/api/testerrors/`:
- `/logging` - Test log levels
- `/error` - Test unhandled exception (500)
- `/not-found` - Test NotFoundException (404)
- `/validation` - Test ValidationException (400)
- `/bad-request` - Test BadRequestException (400)
- `/unauthorized` - Test UnauthorizedException (401)
- `/forbidden` - Test ForbiddenException (403)
- `/conflict` - Test ConflictException (409)

## Middleware Pipeline Order

1. UseSerilogRequestLogging()
2. UseRequestLogging()
3. UseGlobalExceptionHandler()
4. UseSwagger() (dev only)
5. UseHttpsRedirection()
6. MapControllers()

## Build Status

✅ Build: **SUCCESS**
- 0 Errors
- 1 Warning (nullable reference in Serilog enrichment - non-critical)

## Key Features

1. **Standardized Error Handling**
   - Consistent JSON error responses
   - Proper HTTP status codes
   - Environment-aware detail exposure

2. **Structured Logging**
   - Rich contextual information
   - Multiple sinks (console, file, Seq)
   - Configurable log levels

3. **Request Tracking**
   - Trace IDs for correlation
   - Request duration monitoring
   - IP address and host tracking

4. **Development Experience**
   - Detailed error messages in dev mode
   - Color-coded console output
   - Test endpoints for validation

5. **Production Ready**
   - Limited error details in production
   - Log file rotation and retention
   - Performance monitoring

## Usage Example

```csharp
// In a controller or service
public async Task<Survey> GetSurvey(int id)
{
    _logger.LogInformation("Fetching survey with ID: {SurveyId}", id);

    var survey = await _repository.GetByIdAsync(id);

    if (survey == null)
    {
        throw new NotFoundException("Survey", id);
    }

    return survey;
}
```

## Log File Locations

- **Production**: `logs/surveybot-YYYY-MM-DD.log`
- **Development**: `logs/surveybot-dev-YYYY-MM-DD.log`

## Benefits

1. Automatic exception to HTTP status code mapping
2. Centralized error handling logic
3. Consistent error response format
4. Rich structured logging for debugging
5. Request/response tracking with trace IDs
6. Environment-specific behavior
7. Easy testing with dedicated endpoints

## Next Steps

1. Integrate error handling in controllers
2. Add logging throughout the application
3. Configure Seq for centralized logging (optional)
4. Set up log monitoring and alerting
5. Add application metrics

## Status: COMPLETED ✅

All components have been successfully created, configured, and tested. The logging and error handling infrastructure is ready for use throughout the SurveyBot API.
