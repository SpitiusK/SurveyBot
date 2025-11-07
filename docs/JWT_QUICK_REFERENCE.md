# JWT Authentication Quick Reference

## Quick Start

### 1. Login and Get Token
```bash
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 123456789, "username": "testuser"}'
```

### 2. Use Token in Requests
```bash
curl -X GET "https://localhost:5001/api/auth/me" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## API Endpoints

| Endpoint | Method | Auth Required | Description |
|----------|--------|---------------|-------------|
| `/api/auth/login` | POST | No | Login and get JWT token |
| `/api/auth/validate` | GET | Yes | Validate current token |
| `/api/auth/me` | GET | Yes | Get current user info |
| `/api/auth/refresh` | POST | No | Refresh token (not implemented) |
| `/api/surveys` | * | Yes | All survey operations |
| `/api/questions` | * | Yes | All question operations |
| `/api/responses` | * | Yes | All response operations |
| `/api/users` | * | Yes | All user operations |

## Token Configuration

```json
{
  "TokenLifetime": "24 hours",
  "Issuer": "SurveyBot.API",
  "Audience": "SurveyBot.Clients",
  "Algorithm": "HS256"
}
```

## Common HTTP Status Codes

- `200 OK` - Success
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Missing or invalid token
- `500 Internal Server Error` - Server error
- `501 Not Implemented` - Refresh token not implemented

## Token Claims

- `nameid` - User ID
- `TelegramId` - Telegram user ID
- `name` / `Username` - Username
- `exp` - Expiration time
- `iss` - Issuer
- `aud` - Audience

## Code Examples

### C# - Add Token to HttpClient
```csharp
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);
```

### JavaScript - Fetch with Token
```javascript
fetch('https://localhost:5001/api/auth/me', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
})
```

### Python - Requests with Token
```python
headers = {'Authorization': f'Bearer {token}'}
response = requests.get('https://localhost:5001/api/auth/me', headers=headers)
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| 401 Unauthorized | Check token format: `Bearer <token>` |
| Token expired | Login again to get new token |
| Invalid token | Ensure token is complete and unmodified |
| No token | Add Authorization header with Bearer token |

## Important Files

- **Configuration:** `src/SurveyBot.API/appsettings.json`
- **Auth Service:** `src/SurveyBot.Infrastructure/Services/AuthService.cs`
- **Auth Controller:** `src/SurveyBot.API/Controllers/AuthController.cs`
- **JWT Settings:** `src/SurveyBot.Core/Configuration/JwtSettings.cs`

## Security Checklist

- [ ] Change secret key in production
- [ ] Enable HTTPS (`RequireHttpsMetadata = true`)
- [ ] Store secret in secure location (not appsettings)
- [ ] Implement rate limiting on auth endpoints
- [ ] Add comprehensive logging
- [ ] Implement token revocation for logout

## For Production

**Critical Changes Required:**

1. Update `SecretKey` - Use 64+ character random string
2. Set `RequireHttpsMetadata = true`
3. Store secret in environment variable or Azure Key Vault
4. Reduce token lifetime to 1-2 hours
5. Implement refresh token storage in database
6. Add rate limiting to prevent brute force
7. Enable comprehensive audit logging

## Testing URLs

- **Swagger UI:** `https://localhost:5001/swagger`
- **Health Check:** `https://localhost:5001/health`
- **Login:** `https://localhost:5001/api/auth/login`

## Support

For detailed testing instructions, see: `docs/JWT_AUTHENTICATION_TESTING.md`
For implementation details, see: `docs/TASK-017-JWT-IMPLEMENTATION-SUMMARY.md`
