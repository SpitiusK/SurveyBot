# Telegram Bot Integration Guide

## User Registration and Authentication Flow

This guide explains how to integrate the User Registration and Authentication system with your Telegram bot.

## Overview

The system uses an **upsert pattern** for user registration:
- When a user interacts with the bot for the first time (e.g., `/start` command), they are automatically registered
- If the user already exists (by Telegram ID), their information is updated
- No duplicate users are created - Telegram ID is the unique identifier
- A JWT token is generated for each login/registration for API authentication

## Architecture

```
Telegram Bot (/start)
    -> Extract User Info (TelegramId, Username, FirstName, LastName)
    -> POST /api/auth/register
    -> UserService.RegisterAsync()
    -> UserRepository.CreateOrUpdateAsync() [UPSERT]
    -> Generate JWT Token
    -> Return UserWithTokenDto
```

## API Endpoints

### Register/Login Endpoint

**Endpoint:** `POST /api/auth/register`

**Description:** Registers a new user or updates existing user information (upsert pattern)

**Request Body:**
```json
{
  "telegramId": 123456789,
  "username": "john_doe",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "user": {
      "id": 1,
      "telegramId": 123456789,
      "username": "john_doe",
      "firstName": "John",
      "lastName": "Doe",
      "createdAt": "2025-01-15T10:30:00Z",
      "updatedAt": "2025-01-15T10:30:00Z",
      "lastLoginAt": "2025-01-15T10:30:00Z"
    },
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "base64EncodedRefreshToken...",
    "expiresAt": "2025-01-16T10:30:00Z"
  },
  "message": "Registration/login successful"
}
```

## Bot Implementation Examples

### 1. Basic C# Bot Handler (Using Telegram.Bot)

```csharp
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class StartCommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public StartCommandHandler(
        ITelegramBotClient botClient,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _botClient = botClient;
        _httpClient = httpClient;
        _apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5000";
    }

    public async Task HandleStartCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var user = message.From;

        if (user == null)
        {
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Sorry, I couldn't identify you. Please try again.",
                cancellationToken: cancellationToken);
            return;
        }

        // Register or login the user
        var registrationResult = await RegisterUserAsync(user, cancellationToken);

        if (registrationResult != null)
        {
            var welcomeMessage = registrationResult.IsNewUser
                ? $"Welcome to SurveyBot, {user.FirstName}! 🎉\\n\\nYou've been successfully registered."
                : $"Welcome back, {user.FirstName}! 👋";

            welcomeMessage += "\\n\\nYou can now create and manage surveys.\\n" +
                            "Use /help to see available commands.";

            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                welcomeMessage,
                cancellationToken: cancellationToken);
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Sorry, registration failed. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task<UserRegistrationResult?> RegisterUserAsync(
        User telegramUser,
        CancellationToken cancellationToken)
    {
        try
        {
            var registerDto = new
            {
                telegramId = telegramUser.Id,
                username = telegramUser.Username,
                firstName = telegramUser.FirstName,
                lastName = telegramUser.LastName
            };

            var json = JsonSerializer.Serialize(registerDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_apiBaseUrl}/api/auth/register",
                content,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<ApiResponse<UserWithTokenDto>>(
                    responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Data != null)
                {
                    // Store the token if needed for authenticated API calls
                    // For example, in a cache or database
                    await StoreUserTokenAsync(telegramUser.Id, result.Data.AccessToken);

                    return new UserRegistrationResult
                    {
                        IsNewUser = DateTime.UtcNow - result.Data.User.CreatedAt < TimeSpan.FromMinutes(1),
                        UserId = result.Data.User.Id,
                        Token = result.Data.AccessToken
                    };
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Error registering user: {ex.Message}");
            return null;
        }
    }

    private async Task StoreUserTokenAsync(long telegramId, string token)
    {
        // Store token in your preferred storage (cache, database, etc.)
        // This is optional - only needed if the bot makes authenticated API calls
        // For most bot operations, you can use TelegramId directly
    }
}

public class UserRegistrationResult
{
    public bool IsNewUser { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}

public class UserWithTokenDto
{
    public UserDto User { get; set; } = null!;
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public long TelegramId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
```

### 2. Simplified Bot Handler (Direct Registration)

If your bot is in the same solution and can directly access the services:

```csharp
public class StartCommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserService _userService;
    private readonly ILogger<StartCommandHandler> _logger;

    public StartCommandHandler(
        ITelegramBotClient botClient,
        IUserService userService,
        ILogger<StartCommandHandler> logger)
    {
        _botClient = botClient;
        _userService = userService;
        _logger = logger;
    }

    public async Task HandleStartCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var telegramUser = message.From;

        if (telegramUser == null)
        {
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Sorry, I couldn't identify you. Please try again.",
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            // Register or login user directly via service
            var registerDto = new RegisterDto
            {
                TelegramId = telegramUser.Id,
                Username = telegramUser.Username,
                FirstName = telegramUser.FirstName,
                LastName = telegramUser.LastName
            };

            var result = await _userService.RegisterAsync(registerDto);

            var isNewUser = DateTime.UtcNow - result.User.CreatedAt < TimeSpan.FromMinutes(1);

            var welcomeMessage = isNewUser
                ? $"Welcome to SurveyBot, {telegramUser.FirstName}! 🎉\\n\\nYou've been successfully registered."
                : $"Welcome back, {telegramUser.FirstName}! 👋";

            welcomeMessage += "\\n\\nYou can now create and manage surveys.\\n" +
                            "Use /help to see available commands.";

            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                welcomeMessage,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "User {TelegramId} successfully {Action}",
                telegramUser.Id,
                isNewUser ? "registered" : "logged in");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling /start command for user {TelegramId}", telegramUser.Id);

            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Sorry, something went wrong. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }
}
```

## Key Features

### 1. Upsert Pattern (No Duplicate Users)

The `UserRepository.CreateOrUpdateAsync()` method ensures:
- If user exists by `TelegramId`, it **updates** their information
- If user doesn't exist, it **creates** a new user
- No duplicate users are ever created
- `TelegramId` is the unique identifier (not username)

```csharp
public async Task<User> CreateOrUpdateAsync(
    long telegramId,
    string? username,
    string? firstName,
    string? lastName)
{
    var existingUser = await GetByTelegramIdAsync(telegramId);

    if (existingUser != null)
    {
        // Update existing user
        existingUser.Username = username;
        existingUser.FirstName = firstName;
        existingUser.LastName = lastName;
        existingUser.UpdatedAt = DateTime.UtcNow;

        await UpdateAsync(existingUser);
        return existingUser;
    }

    // Create new user
    var newUser = new User
    {
        TelegramId = telegramId,
        Username = username,
        FirstName = firstName,
        LastName = lastName
    };

    await AddAsync(newUser);
    return newUser;
}
```

### 2. Automatic Last Login Tracking

Every time a user registers or logs in:
- `LastLoginAt` timestamp is automatically updated
- This helps track user activity and engagement

### 3. JWT Token Generation

The registration endpoint returns a JWT token that can be used for:
- Authenticated API calls from the admin panel
- Optional bot authentication (if needed)
- Token contains: `UserId`, `TelegramId`, `Username`

### 4. Comprehensive Logging

All registration/login attempts are logged with:
- Telegram ID
- Username
- Success/failure status
- Timestamps

## Error Handling

The bot should handle these scenarios:

1. **API Unavailable**
   ```csharp
   catch (HttpRequestException ex)
   {
       await _botClient.SendTextMessageAsync(
           message.Chat.Id,
           "Service temporarily unavailable. Please try again later.");
   }
   ```

2. **Invalid User Data**
   ```csharp
   if (user.Id <= 0)
   {
       await _botClient.SendTextMessageAsync(
           message.Chat.Id,
           "Invalid user information. Please restart the bot.");
   }
   ```

3. **Registration Failure**
   ```csharp
   if (!response.IsSuccessStatusCode)
   {
       await _botClient.SendTextMessageAsync(
           message.Chat.Id,
           "Registration failed. Please try again.");
   }
   ```

## Testing

### Manual Testing via Swagger

1. Navigate to `http://localhost:5000/swagger`
2. Find `POST /api/auth/register` endpoint
3. Test with sample data:

```json
{
  "telegramId": 123456789,
  "username": "test_user",
  "firstName": "Test",
  "lastName": "User"
}
```

4. Verify response contains:
   - User information
   - JWT token
   - Expiration timestamp

### Testing with Telegram Bot

1. Start your bot
2. Send `/start` command
3. Verify:
   - Welcome message received
   - User created in database
   - Token generated
   - Last login timestamp updated

4. Send `/start` again
5. Verify:
   - "Welcome back" message (not "Welcome" for new user)
   - User information updated
   - New token generated
   - No duplicate user created

## Database Schema

The User entity includes:

```sql
CREATE TABLE "Users" (
    "Id" SERIAL PRIMARY KEY,
    "TelegramId" BIGINT NOT NULL UNIQUE,  -- Unique constraint prevents duplicates
    "Username" VARCHAR(255),
    "FirstName" VARCHAR(255),
    "LastName" VARCHAR(255),
    "LastLoginAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_Users_TelegramId" ON "Users" ("TelegramId");
CREATE INDEX "IX_Users_Username" ON "Users" ("Username");
```

## Security Considerations

1. **TelegramId Validation**: Always validate that the Telegram ID is a positive number
2. **Username Sanitization**: Usernames may contain special characters - ensure proper escaping
3. **Token Storage**: If storing tokens, use secure storage and implement expiration
4. **Rate Limiting**: Consider implementing rate limiting for the registration endpoint
5. **HTTPS**: Always use HTTPS in production for API calls

## Next Steps

After implementing user registration:

1. **Survey Creation**: Users can create surveys via bot commands
2. **Survey Management**: Users can view/edit their surveys
3. **Response Collection**: Bot collects responses from other users
4. **Admin Panel**: Users can log in to web admin panel with their token
5. **Analytics**: Track user engagement via `LastLoginAt` timestamps

## Troubleshooting

### User Not Being Created

- Check database connection
- Verify TelegramId is valid (positive number)
- Check logs for detailed error messages

### Duplicate Users

- Should not happen due to upsert pattern
- If it does, check database unique constraint on `TelegramId`

### Token Issues

- Verify JWT settings in `appsettings.json`
- Check token expiration (default: 24 hours)
- Ensure secret key is properly configured

## Support

For issues or questions:
- Check application logs: `logs/` directory
- Review Swagger documentation: `/swagger`
- Examine database records directly for debugging
