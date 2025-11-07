using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SurveyBot.Core.Configuration;
using SurveyBot.Core.DTOs.User;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;

namespace SurveyBot.Infrastructure.Services;

/// <summary>
/// Implementation of user management and authentication service.
/// Handles user registration, login, and profile management with Telegram integration.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IAuthService authService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _authService = authService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user or updates an existing user (upsert pattern).
    /// This is designed for Telegram bot integration where users are automatically
    /// registered on first interaction via /start command.
    /// </summary>
    public async Task<UserWithTokenDto> RegisterAsync(RegisterDto registerDto)
    {
        _logger.LogInformation(
            "User registration/login attempt for Telegram ID: {TelegramId}, Username: {Username}",
            registerDto.TelegramId,
            registerDto.Username);

        // Use the repository's CreateOrUpdateAsync method for upsert pattern
        // This ensures no duplicate users are created
        var user = await _userRepository.CreateOrUpdateAsync(
            registerDto.TelegramId,
            registerDto.Username,
            registerDto.FirstName,
            registerDto.LastName);

        // Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation(
            "User {Action} successfully: UserId={UserId}, TelegramId={TelegramId}",
            user.CreatedAt == user.UpdatedAt ? "registered" : "updated",
            user.Id,
            user.TelegramId);

        // Generate JWT token
        var accessToken = _authService.GenerateAccessToken(
            user.Id,
            user.TelegramId,
            user.Username);

        var refreshToken = _authService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.TokenLifetimeHours);

        _logger.LogInformation("Generated JWT token for user ID: {UserId}", user.Id);

        // Map to DTOs
        var userDto = MapToUserDto(user);

        return new UserWithTokenDto
        {
            User = userDto,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Gets a user by their Telegram ID.
    /// </summary>
    public async Task<UserDto?> GetUserByTelegramIdAsync(long telegramId)
    {
        _logger.LogDebug("Retrieving user by Telegram ID: {TelegramId}", telegramId);

        var user = await _userRepository.GetByTelegramIdAsync(telegramId);

        if (user == null)
        {
            _logger.LogDebug("User not found for Telegram ID: {TelegramId}", telegramId);
            return null;
        }

        return MapToUserDto(user);
    }

    /// <summary>
    /// Gets a user by their internal database ID.
    /// </summary>
    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        _logger.LogDebug("Retrieving user by ID: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            _logger.LogDebug("User not found for ID: {UserId}", userId);
            return null;
        }

        return MapToUserDto(user);
    }

    /// <summary>
    /// Updates user information.
    /// </summary>
    public async Task<UserDto> UpdateUserAsync(int userId, UpdateUserDto updateDto)
    {
        _logger.LogInformation("Updating user: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("User not found for update: {UserId}", userId);
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Update fields if provided
        if (updateDto.Username != null)
            user.Username = updateDto.Username;

        if (updateDto.FirstName != null)
            user.FirstName = updateDto.FirstName;

        if (updateDto.LastName != null)
            user.LastName = updateDto.LastName;

        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User updated successfully: {UserId}", userId);

        return MapToUserDto(user);
    }

    /// <summary>
    /// Gets the current authenticated user by their ID.
    /// </summary>
    public async Task<UserDto> GetCurrentUserAsync(int userId)
    {
        _logger.LogDebug("Retrieving current user: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("Current user not found: {UserId}", userId);
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        return MapToUserDto(user);
    }

    /// <summary>
    /// Validates a JWT token and returns the claims principal.
    /// </summary>
    public ClaimsPrincipal? ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogDebug("Token validation failed: token is null or empty");
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = securityKey,
                ClockSkew = TimeSpan.Zero
            }, out _);

            _logger.LogDebug("Token validated successfully");
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    /// <summary>
    /// Updates the last login timestamp for a user.
    /// </summary>
    public async Task UpdateLastLoginAsync(int userId)
    {
        _logger.LogDebug("Updating last login timestamp for user: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("User not found for last login update: {UserId}", userId);
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        _logger.LogDebug("Last login timestamp updated for user: {UserId}", userId);
    }

    /// <summary>
    /// Checks if a user exists by their Telegram ID.
    /// </summary>
    public async Task<bool> UserExistsAsync(long telegramId)
    {
        _logger.LogDebug("Checking if user exists for Telegram ID: {TelegramId}", telegramId);

        return await _userRepository.ExistsByTelegramIdAsync(telegramId);
    }

    /// <summary>
    /// Gets all users (for admin purposes).
    /// </summary>
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        _logger.LogDebug("Retrieving all users");

        var users = await _userRepository.GetAllAsync();

        return users.Select(MapToUserDto);
    }

    /// <summary>
    /// Searches users by name (first name or last name).
    /// </summary>
    public async Task<IEnumerable<UserDto>> SearchUsersAsync(string searchTerm)
    {
        _logger.LogDebug("Searching users with term: {SearchTerm}", searchTerm);

        var users = await _userRepository.SearchByNameAsync(searchTerm);

        return users.Select(MapToUserDto);
    }

    /// <summary>
    /// Maps User entity to UserDto.
    /// </summary>
    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            TelegramId = user.TelegramId,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
