# Authentication Flow Documentation

Complete guide to JWT authentication in SurveyBot Phase 2.

## Overview

SurveyBot uses JWT (JSON Web Token) based authentication for API access. Users are authenticated via their Telegram ID, which is automatically captured when they interact with the bot.

## Authentication Methods

### 1. Telegram Bot Authentication (Primary)

When a user starts the bot with `/start`, they are automatically registered in the system:

```
User -> /start command -> Bot -> Register/Login API -> JWT Token -> Stored for subsequent requests
```

### 2. Direct API Login

For testing or external integrations, you can authenticate directly with the API:

```
POST /api/auth/login
{
  "telegramId": 123456789
}
```

## JWT Token Structure

### Token Claims

The JWT token contains the following claims:

```json
{
  "sub": "1",                    // User ID (NameIdentifier claim)
  "TelegramId": "123456789",     // Telegram user ID
  "Username": "johndoe",         // Telegram username
  "nbf": 1699358400,             // Not valid before
  "exp": 1699444800,             // Expiration time
  "iat": 1699358400              // Issued at
}
```

### Token Expiration

- **Default Expiration:** 24 hours from issuance
- **Configurable in:** `appsettings.json` under `JwtSettings.ExpiresInHours`
- **Refresh:** Not implemented in Phase 2 MVP (users must login again)

## Authentication Endpoints

### 1. Login

**Endpoint:** `POST /api/auth/login`

**Purpose:** Authenticate user by Telegram ID and get JWT token

**Request:**
```json
{
  "telegramId": 123456789
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "userId": 1,
    "telegramId": 123456789,
    "username": "johndoe",
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiVGVsZWdyYW1JZCI6IjEyMzQ1Njc4OSIsIlVzZXJuYW1lIjoiam9obmRvZSIsIm5iZiI6MTY5OTM1ODQwMCwiZXhwIjoxNjk5NDQ0ODAwLCJpYXQiOjE2OTkzNTg0MDB9.signature",
    "expiresAt": "2025-11-08T12:00:00Z"
  }
}
```

**Error Responses:**
- `400 Bad Request` - Invalid telegram ID format
- `500 Internal Server Error` - Authentication service error

**cURL Example:**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 123456789}'
```

---

### 2. Register

**Endpoint:** `POST /api/auth/register`

**Purpose:** Register new user or update existing user (upsert pattern)

**Request:**
```json
{
  "telegramId": 123456789,
  "username": "johndoe",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Registration/login successful",
  "data": {
    "user": {
      "id": 1,
      "telegramId": 123456789,
      "username": "johndoe",
      "firstName": "John",
      "lastName": "Doe",
      "createdAt": "2025-11-07T10:00:00Z"
    },
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-11-08T12:00:00Z"
  }
}
```

**Error Responses:**
- `400 Bad Request` - Invalid request data
- `500 Internal Server Error` - Registration error

**cURL Example:**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "telegramId": 123456789,
    "username": "johndoe",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

---

### 3. Validate Token

**Endpoint:** `GET /api/auth/validate`

**Purpose:** Check if current token is valid

**Headers:**
```
Authorization: Bearer <your_jwt_token>
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Token is valid",
  "data": {
    "valid": true,
    "userId": "1",
    "telegramId": "123456789",
    "username": "johndoe"
  }
}
```

**Error Responses:**
- `401 Unauthorized` - Token is invalid or expired

**cURL Example:**
```bash
curl -X GET http://localhost:5000/api/auth/validate \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

### 4. Get Current User

**Endpoint:** `GET /api/auth/me`

**Purpose:** Get current authenticated user information

**Headers:**
```
Authorization: Bearer <your_jwt_token>
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Current user information",
  "data": {
    "userId": "1",
    "telegramId": "123456789",
    "username": "johndoe"
  }
}
```

**Error Responses:**
- `401 Unauthorized` - Token is invalid or missing

**cURL Example:**
```bash
curl -X GET http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

### 5. Refresh Token (Not Implemented)

**Endpoint:** `POST /api/auth/refresh`

**Status:** Not implemented in Phase 2 MVP

**Response (501 Not Implemented):**
```json
{
  "success": false,
  "message": "Token refresh is not implemented in MVP. Please login again.",
  "data": null
}
```

**Workaround:** When token expires, call `/api/auth/login` again to get a new token.

---

## Using Authentication Tokens

### Authorization Header Format

All protected endpoints require the JWT token in the Authorization header:

```
Authorization: Bearer <your_jwt_token>
```

### Example Protected Request

```bash
# Get user's surveys
curl -X GET http://localhost:5000/api/surveys \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### JavaScript/TypeScript Example

```javascript
const token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";

const response = await fetch('http://localhost:5000/api/surveys', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});

const data = await response.json();
```

### C# Example

```csharp
using System.Net.Http.Headers;

var client = new HttpClient();
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

var response = await client.GetAsync("http://localhost:5000/api/surveys");
var data = await response.Content.ReadAsStringAsync();
```

---

## Protected vs Public Endpoints

### Protected Endpoints (Require Authentication)

All endpoints except the following require a valid JWT token:

**Survey Management:**
- POST /api/surveys - Create survey
- PUT /api/surveys/{id} - Update survey
- DELETE /api/surveys/{id} - Delete survey
- POST /api/surveys/{id}/activate - Activate survey
- POST /api/surveys/{id}/deactivate - Deactivate survey
- GET /api/surveys - List user's surveys
- GET /api/surveys/{id} - Get survey details (if inactive)
- GET /api/surveys/{id}/statistics - Get statistics

**Question Management:**
- POST /api/surveys/{surveyId}/questions - Add question
- PUT /api/questions/{id} - Update question
- DELETE /api/questions/{id} - Delete question
- POST /api/surveys/{surveyId}/questions/reorder - Reorder questions

**Response Management:**
- GET /api/responses - List responses
- GET /api/responses/{id} - Get response details

### Public Endpoints (No Authentication Required)

**Authentication:**
- POST /api/auth/login - Login
- POST /api/auth/register - Register

**Survey Access:**
- GET /api/surveys/{id}/questions - Get questions (only for active surveys)

---

## Authorization Rules

### Resource Ownership

Users can only access/modify resources they own:

1. **Surveys:** User must be the creator (createdBy = userId)
2. **Questions:** User must own the parent survey
3. **Responses:** Users can only view responses to their own surveys

### Ownership Verification Flow

```
1. Extract userId from JWT token claims
2. Load resource from database
3. Check if resource.createdBy == userId
4. If not, return 403 Forbidden
5. If yes, allow operation
```

### Error Response for Unauthorized Access

```json
{
  "success": false,
  "message": "You don't have permission to access this survey",
  "data": null
}
```

---

## Authentication Flow Diagrams

### Bot User Registration Flow

```
User starts bot (/start)
    |
    v
Bot captures Telegram user info
    |
    v
Bot calls POST /api/auth/register
    {
      telegramId: user.id,
      username: user.username,
      firstName: user.first_name,
      lastName: user.last_name
    }
    |
    v
API creates/updates user
    |
    v
API generates JWT token
    |
    v
Bot stores token for user session
    |
    v
User can now access protected features
```

### API Request with Authentication

```
Client makes request with token
    |
    v
ASP.NET Core JWT Middleware validates token
    |
    +-- Invalid/Expired --> 401 Unauthorized
    |
    +-- Valid --> Extract claims (userId, telegramId, username)
                     |
                     v
                 Controller action executes
                     |
                     v
                 Authorization check (user owns resource?)
                     |
                     +-- No --> 403 Forbidden
                     |
                     +-- Yes --> Process request
```

### Token Expiration Handling

```
Client makes request
    |
    v
Token validation
    |
    +-- Token expired --> 401 Unauthorized
           |
           v
       Client detects 401
           |
           v
       Client calls /api/auth/login
           |
           v
       Client receives new token
           |
           v
       Client retries original request
```

---

## Security Considerations

### Token Security

1. **HTTPS Required:** Always use HTTPS in production to protect tokens in transit
2. **Secure Storage:** Store tokens securely (never in localStorage for sensitive apps)
3. **Token Transmission:** Only send tokens in Authorization header, never in URL
4. **Token Expiration:** Tokens expire after 24 hours to limit exposure window

### JWT Configuration

Located in `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-characters-long",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Client",
    "ExpiresInHours": 24
  }
}
```

**Production Security:**
- Use strong, randomly generated secret key (min 32 characters)
- Store secret key in environment variables or Azure Key Vault
- Never commit secret keys to source control
- Rotate keys periodically

### Best Practices

1. **Token Lifetime:** Keep tokens short-lived (24 hours default)
2. **No Sensitive Data:** Don't store sensitive data in JWT claims (they're not encrypted)
3. **Logout:** Implement token blacklist or short expiration for logout functionality
4. **API Rate Limiting:** Implement rate limiting to prevent brute force attacks
5. **Monitor Failed Attempts:** Log and monitor failed authentication attempts

---

## Testing Authentication

### Postman Setup

1. **Login and Get Token:**
   - POST `/api/auth/login`
   - Copy `accessToken` from response

2. **Configure Authorization:**
   - Select "Bearer Token" in Authorization tab
   - Paste token in Token field

3. **Test Protected Endpoint:**
   - GET `/api/surveys`
   - Should return 200 OK with user's surveys

### Testing Token Expiration

```bash
# Get token
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 123456789}' | jq -r '.data.accessToken')

# Use token (should work)
curl -X GET http://localhost:5000/api/surveys \
  -H "Authorization: Bearer $TOKEN"

# Wait for token to expire (or modify ExpiresInHours to 0.0001 for testing)
# Then retry (should get 401)
curl -X GET http://localhost:5000/api/surveys \
  -H "Authorization: Bearer $TOKEN"
```

### Testing Without Token

```bash
# Should return 401 Unauthorized
curl -X GET http://localhost:5000/api/surveys
```

### Testing Invalid Token

```bash
# Should return 401 Unauthorized
curl -X GET http://localhost:5000/api/surveys \
  -H "Authorization: Bearer invalid.token.here"
```

---

## Common Authentication Errors

### 401 Unauthorized

**Cause:** Missing, invalid, or expired token

**Response:**
```json
{
  "success": false,
  "message": "Invalid or missing user authentication",
  "data": null
}
```

**Solutions:**
- Verify token is included in Authorization header
- Check token format: `Bearer <token>`
- Login again to get new token if expired
- Verify JWT secret key matches in appsettings.json

### 403 Forbidden

**Cause:** User doesn't own the requested resource

**Response:**
```json
{
  "success": false,
  "message": "You don't have permission to access this survey",
  "data": null
}
```

**Solutions:**
- Verify user is accessing their own resources
- Check userId in token matches resource owner

---

## Implementation Details

### JWT Middleware Configuration

Located in `Program.cs`:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });
```

### Token Generation

Located in `AuthService`:

```csharp
private string GenerateJwtToken(User user)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim("TelegramId", user.TelegramId.ToString()),
        new Claim("Username", user.Username ?? string.Empty)
    };

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
    var credentials = new SigningCredentials(
        key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _jwtSettings.Issuer,
        audience: _jwtSettings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpiresInHours),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### Extracting User ID in Controllers

```csharp
private int GetUserIdFromClaims()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) ||
        !int.TryParse(userIdClaim, out int userId))
    {
        throw new UnauthorizedAccessException(
            "Invalid or missing user authentication");
    }

    return userId;
}
```

---

## Future Enhancements (Post-MVP)

1. **Refresh Tokens:** Implement refresh token flow for seamless token renewal
2. **Token Revocation:** Implement token blacklist for logout functionality
3. **Role-Based Access:** Add admin role for survey moderation
4. **OAuth Integration:** Support Google/Facebook login alongside Telegram
5. **Two-Factor Authentication:** Add 2FA for sensitive operations
6. **Session Management:** Track active sessions per user
7. **Password-Based Auth:** Add optional password authentication for web users

---

## Quick Reference

### Authentication Checklist

- [ ] User logs in via `/api/auth/login` or `/api/auth/register`
- [ ] Store received JWT token securely
- [ ] Include token in `Authorization: Bearer <token>` header for all protected requests
- [ ] Handle 401 errors by re-authenticating
- [ ] Handle 403 errors by checking resource ownership
- [ ] Validate token before expiration (check `expiresAt` field)
- [ ] Logout by discarding stored token

### Common Header Patterns

```bash
# Authentication
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

# Content negotiation
Content-Type: application/json
Accept: application/json
```

### HTTP Status Codes

- `200 OK` - Successful request with token validation
- `201 Created` - Resource created with valid authentication
- `401 Unauthorized` - Missing, invalid, or expired token
- `403 Forbidden` - Valid token but no permission for resource
- `500 Internal Server Error` - Server error during authentication
