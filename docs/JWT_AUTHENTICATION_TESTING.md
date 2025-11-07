# JWT Authentication Testing Guide

This document provides comprehensive testing instructions for the JWT Authentication System implemented in SurveyBot API.

## Overview

The JWT authentication system has been successfully implemented with the following components:

- JWT Bearer token authentication
- Login endpoint for token generation
- Token validation middleware
- Protected endpoints requiring authentication
- Refresh token mechanism (placeholder for MVP)

## Authentication Endpoints

### 1. Login (POST /api/auth/login)

Authenticates a user by Telegram ID and returns a JWT access token.

**Request:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "telegramId": 123456789,
  "username": "testuser"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "base64-encoded-random-token",
    "expiresAt": "2025-11-07T12:00:00Z",
    "userId": 1,
    "telegramId": 123456789,
    "username": "testuser"
  },
  "message": "Login successful",
  "timestamp": "2025-11-06T12:00:00Z"
}
```

### 2. Validate Token (GET /api/auth/validate)

Validates the current JWT token. Requires authentication.

**Request:**
```http
GET /api/auth/validate
Authorization: Bearer {your-jwt-token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "valid": true,
    "userId": "1",
    "telegramId": "123456789",
    "username": "testuser"
  },
  "message": "Token is valid",
  "timestamp": "2025-11-06T12:00:00Z"
}
```

### 3. Get Current User (GET /api/auth/me)

Returns the current authenticated user's information extracted from the JWT token.

**Request:**
```http
GET /api/auth/me
Authorization: Bearer {your-jwt-token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "userId": "1",
    "telegramId": "123456789",
    "username": "testuser"
  },
  "message": "Current user information",
  "timestamp": "2025-11-06T12:00:00Z"
}
```

### 4. Refresh Token (POST /api/auth/refresh)

Refreshes an access token using a refresh token (NOT IMPLEMENTED IN MVP).

**Request:**
```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "base64-encoded-refresh-token"
}
```

**Response (501 Not Implemented):**
```json
{
  "success": false,
  "message": "Refresh token functionality is not fully implemented in MVP...",
  "timestamp": "2025-11-06T12:00:00Z"
}
```

## Testing with cURL

### Step 1: Start the API

```bash
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet run
```

The API will start on `https://localhost:5001` (or the configured port).

### Step 2: Login and Get Token

```bash
curl -X POST "https://localhost:5001/api/auth/login" ^
  -H "Content-Type: application/json" ^
  -d "{\"telegramId\": 123456789, \"username\": \"testuser\"}"
```

Save the `accessToken` from the response.

### Step 3: Test Protected Endpoint

Try accessing a protected endpoint without a token (should return 401):

```bash
curl -X GET "https://localhost:5001/api/auth/validate" -v
```

Now test with the token:

```bash
curl -X GET "https://localhost:5001/api/auth/validate" ^
  -H "Authorization: Bearer {your-access-token}"
```

### Step 4: Test Current User Endpoint

```bash
curl -X GET "https://localhost:5001/api/auth/me" ^
  -H "Authorization: Bearer {your-access-token}"
```

### Step 5: Test Token Validation

```bash
curl -X GET "https://localhost:5001/api/auth/validate" ^
  -H "Authorization: Bearer {your-access-token}"
```

## Testing with Postman

### Collection Setup

1. Create a new collection named "SurveyBot API"
2. Add the base URL as a collection variable: `{{baseUrl}}` = `https://localhost:5001`

### Request 1: Login

- Method: `POST`
- URL: `{{baseUrl}}/api/auth/login`
- Headers: `Content-Type: application/json`
- Body (raw JSON):
```json
{
  "telegramId": 123456789,
  "username": "testuser"
}
```
- Tests (to save token):
```javascript
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    pm.environment.set("jwt_token", jsonData.data.accessToken);
}
```

### Request 2: Validate Token

- Method: `GET`
- URL: `{{baseUrl}}/api/auth/validate`
- Headers: `Authorization: Bearer {{jwt_token}}`

### Request 3: Get Current User

- Method: `GET`
- URL: `{{baseUrl}}/api/auth/me`
- Headers: `Authorization: Bearer {{jwt_token}}`

### Request 4: Test Protected Endpoint (Surveys)

- Method: `GET`
- URL: `{{baseUrl}}/api/surveys`
- Headers: `Authorization: Bearer {{jwt_token}}`

This should return 401 if no token or invalid token, but the endpoint itself is not yet implemented.

## Testing with Swagger UI

1. Start the API in Development mode
2. Navigate to `https://localhost:5001/swagger`
3. Find the `/api/auth/login` endpoint
4. Click "Try it out"
5. Enter test data:
```json
{
  "telegramId": 123456789,
  "username": "testuser"
}
```
6. Click "Execute"
7. Copy the `accessToken` from the response
8. Click the "Authorize" button at the top of the Swagger UI
9. Enter: `Bearer {your-access-token}` (replace with actual token)
10. Click "Authorize"
11. Now you can test any protected endpoint

## JWT Token Claims

The JWT tokens generated contain the following claims:

- `nameid` (NameIdentifier): User ID (database ID)
- `TelegramId`: Telegram user ID
- `name` (Name): Username
- `Username`: Username (duplicate for convenience)
- `jti` (JWT ID): Unique token identifier (GUID)
- `iat` (Issued At): Token creation timestamp
- `exp` (Expiration): Token expiration timestamp
- `iss` (Issuer): "SurveyBot.API"
- `aud` (Audience): "SurveyBot.Clients"

## Token Configuration

Current settings (from `appsettings.json`):

```json
{
  "JwtSettings": {
    "SecretKey": "SurveyBot-Super-Secret-Key-For-JWT-Token-Generation-2025-Change-In-Production",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Clients",
    "TokenLifetimeHours": 24,
    "RefreshTokenLifetimeDays": 7
  }
}
```

**IMPORTANT:** Change the `SecretKey` in production! This key should be:
- At least 32 characters long
- Stored securely (environment variables, Azure Key Vault, etc.)
- Never committed to source control

## Protected Endpoints

The following controllers are now protected with `[Authorize]` attribute:

- `/api/surveys` - SurveysController
- `/api/questions` - QuestionsController
- `/api/responses` - ResponsesController
- `/api/users` - UsersController

Public endpoints (no authentication required):

- `/api/auth/login` - Login
- `/api/auth/refresh` - Refresh token (not implemented)
- `/health` - Health checks
- `/api/testerrors` - Error testing (for development)

## Error Scenarios

### 1. No Token Provided

**Request:**
```http
GET /api/auth/validate
```

**Response (401 Unauthorized):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

### 2. Invalid Token

**Request:**
```http
GET /api/auth/validate
Authorization: Bearer invalid-token
```

**Response (401 Unauthorized):**
Similar to scenario 1.

### 3. Expired Token

After 24 hours, the token will expire automatically.

**Response (401 Unauthorized):**
Similar to scenario 1.

### 4. Invalid Login Request

**Request:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "telegramId": -1
}
```

**Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Invalid request data",
  "timestamp": "2025-11-06T12:00:00Z"
}
```

## Integration Testing

Here's a sample integration test for the authentication system:

```csharp
[Fact]
public async Task Login_WithValidTelegramId_ReturnsToken()
{
    // Arrange
    var client = _factory.CreateClient();
    var loginRequest = new
    {
        telegramId = 123456789,
        username = "testuser"
    };

    // Act
    var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();

    Assert.NotNull(result);
    Assert.True(result.Success);
    Assert.NotNull(result.Data.AccessToken);
    Assert.Equal(123456789, result.Data.TelegramId);
}

[Fact]
public async Task ProtectedEndpoint_WithoutToken_Returns401()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/auth/validate");

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task ProtectedEndpoint_WithValidToken_Returns200()
{
    // Arrange
    var client = _factory.CreateClient();

    // Login first
    var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
    {
        telegramId = 123456789,
        username = "testuser"
    });
    var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();

    // Add token to request
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", loginResult.Data.AccessToken);

    // Act
    var response = await client.GetAsync("/api/auth/validate");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

## Security Considerations

1. **HTTPS Only**: In production, set `RequireHttpsMetadata = true` in Program.cs
2. **Secret Key**: Change the secret key and store it securely
3. **Token Lifetime**: Consider shorter token lifetimes for production
4. **Refresh Tokens**: Implement proper refresh token storage in database
5. **Rate Limiting**: Add rate limiting to login endpoint to prevent brute force
6. **Audit Logging**: Log all authentication attempts for security monitoring

## Next Steps

1. Implement refresh token storage in database
2. Add token revocation mechanism
3. Implement role-based authorization
4. Add rate limiting to authentication endpoints
5. Set up proper secret management for production
6. Add comprehensive integration tests
7. Implement token blacklisting for logout

## Troubleshooting

### Token Not Working

1. Check if token is properly formatted in Authorization header: `Bearer {token}`
2. Verify token hasn't expired (24 hours by default)
3. Ensure JWT settings are properly configured in appsettings.json
4. Check server logs for JWT validation errors

### 401 Unauthorized on Protected Endpoints

1. Verify you're sending the Authorization header
2. Check token format (should start with "Bearer ")
3. Ensure token is valid and not expired
4. Check if the endpoint requires authentication

### Database Connection Issues

If you see errors related to user creation:

1. Ensure PostgreSQL is running
2. Verify connection string in appsettings.json
3. Run database migrations if needed
4. Check IUserRepository implementation

## Summary

The JWT Authentication System has been successfully implemented with:

- Secure token generation and validation
- User authentication via Telegram ID
- Protected API endpoints
- Comprehensive error handling
- Swagger UI integration for testing
- Logging of authentication events

All components are working and ready for integration with the Telegram bot and admin panel.
