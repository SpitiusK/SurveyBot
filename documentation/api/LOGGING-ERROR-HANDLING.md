# Logging and Error Handling Infrastructure

## Overview

This document describes the logging and error handling infrastructure implemented for the SurveyBot API.

## Components

### 1. Error Response Models

Located in `Models/` directory:

- **ErrorResponse.cs**: Standardized error response format
  - Includes status code, message, details, validation errors
  - Timestamp and trace ID for request tracking
  - Conditional details exposure (development only)

- **ApiResponse.cs**: Standardized success response wrapper
  - Generic and non-generic versions
  - Consistent format across all endpoints
  - Optional message field

### 2. Custom Exception Classes

Located in `Exceptions/` directory:

- **ApiException.cs**: Base exception class with HTTP status code support
- **NotFoundException.cs**: HTTP 404 - Resource not found
- **ValidationException.cs**: HTTP 400 - Validation errors with field-level details
- **BadRequestException.cs**: HTTP 400 - Invalid request
- **UnauthorizedException.cs**: HTTP 401 - Authentication required
- **ForbiddenException.cs**: HTTP 403 - Insufficient permissions
- **ConflictException.cs**: HTTP 409 - Resource conflict

### 3. Middleware

Located in `Middleware/` directory:

- **GlobalExceptionMiddleware.cs**
  - Catches all unhandled exceptions
  - Maps exceptions to appropriate HTTP status codes
  - Returns standardized error responses
  - Logs exceptions with appropriate log levels
  - Includes detailed stack traces in development mode only

- **RequestLoggingMiddleware.cs**
  - Logs HTTP request/response information
  - Tracks request duration
  - Adjusts log level based on response status code

### 4. Serilog Configuration

Configured in `Program.cs` and `appsettings.json`:

#### Sinks
- **Console**: Color-coded output for development
- **File**: Rolling file logging with size limits
  - Daily rotation
  - 30-day retention (production)
  - 7-day retention (development)
  - 10MB file size limit
- **Seq**: Structured logging server (optional)
  - Default URL: http://localhost:5341

#### Log Levels
- **Production**: Information and above
- **Development**: Debug and above
- **Entity Framework**: Information for SQL commands

#### Enrichers
- FromLogContext
- WithMachineName
- WithThreadId
- Custom properties: Application name and Environment

## Usage

### Throwing Custom Exceptions

```csharp
// Not Found
throw new NotFoundException("Survey", surveyId);

// Validation Error
var errors = new Dictionary<string, string[]>
{
    { "Title", new[] { "Title is required" } },
    { "Description", new[] { "Description is too long" } }
};
throw new ValidationException(errors);

// Bad Request
throw new BadRequestException("Invalid survey ID format", "ID must be a valid integer");

// Unauthorized
throw new UnauthorizedException("Authentication token is required");

// Forbidden
throw new ForbiddenException("You do not have permission to delete this survey");

// Conflict
throw new ConflictException("Survey already exists", "A survey with this title already exists");
```

### Using Structured Logging

```csharp
// Simple logging
_logger.LogInformation("Survey created with ID: {SurveyId}", surveyId);

// With multiple properties
_logger.LogWarning("Failed login attempt for user {Username} from IP {IpAddress}",
    username, ipAddress);

// Error logging
_logger.LogError(ex, "Failed to save survey {SurveyId}", surveyId);

// Structured data
_logger.LogInformation("Survey created: {@Survey}", survey);
```

### Response Format

#### Success Response
```json
{
  "success": true,
  "data": { /* response data */ },
  "message": "Optional message",
  "timestamp": "2025-11-06T10:30:00Z"
}
```

#### Error Response
```json
{
  "success": false,
  "statusCode": 400,
  "message": "One or more validation errors occurred.",
  "errors": {
    "Title": ["Title is required", "Title must be at least 3 characters"],
    "Description": ["Description cannot be empty"]
  },
  "timestamp": "2025-11-06T10:30:00Z",
  "traceId": "0HMVFE3234D5E:00000001"
}
```

## Testing Error Handling

Test endpoints are available in `TestErrorsController`:

- `GET /api/testerrors/logging` - Test different log levels
- `GET /api/testerrors/error` - Test unhandled exception
- `GET /api/testerrors/not-found` - Test 404 error
- `GET /api/testerrors/validation` - Test validation errors
- `GET /api/testerrors/bad-request` - Test 400 error
- `GET /api/testerrors/unauthorized` - Test 401 error
- `GET /api/testerrors/forbidden` - Test 403 error
- `GET /api/testerrors/conflict` - Test 409 error

## Log File Locations

- **Production**: `logs/surveybot-YYYY-MM-DD.log`
- **Development**: `logs/surveybot-dev-YYYY-MM-DD.log`

## Middleware Order

The middleware is registered in this order in `Program.cs`:

1. UseSerilogRequestLogging() - Serilog's built-in request logging
2. UseRequestLogging() - Custom request logging middleware
3. UseGlobalExceptionHandler() - Global exception handling
4. UseSwagger() / UseSwaggerUI() - Development only
5. UseHttpsRedirection()
6. MapControllers()

## Benefits

1. **Consistent Error Responses**: All errors follow the same JSON structure
2. **Proper HTTP Status Codes**: Automatic mapping of exceptions to status codes
3. **Structured Logging**: Rich, queryable logs with Serilog
4. **Development vs Production**: Different detail levels based on environment
5. **Request Tracking**: Trace IDs for debugging across distributed systems
6. **Performance Monitoring**: Request duration logging
7. **Centralized Logging**: Optional Seq integration for log aggregation

## Best Practices

1. Use custom exceptions instead of returning error responses directly
2. Include contextual information in log messages
3. Use structured logging with properties instead of string interpolation
4. Log at appropriate levels (Information, Warning, Error, etc.)
5. Include trace IDs in error responses for easier debugging
6. Test error handling with the provided test endpoints

## Configuration

### Enable/Disable Seq

Edit `appsettings.json` and comment out the Seq sink if not using:

```json
{
  "Serilog": {
    "WriteTo": [
      // Comment out if Seq is not available
      // {
      //   "Name": "Seq",
      //   "Args": {
      //     "serverUrl": "http://localhost:5341"
      //   }
      // }
    ]
  }
}
```

### Adjust Log Levels

Modify minimum levels in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "YourNamespace": "Debug"
      }
    }
  }
}
```
