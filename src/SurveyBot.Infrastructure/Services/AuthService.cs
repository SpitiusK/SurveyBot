using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SurveyBot.Core.Configuration;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;

namespace SurveyBot.Infrastructure.Services;

/// <summary>
/// Implementation of authentication service for JWT token generation and validation.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user by Telegram ID and generates JWT tokens.
    /// </summary>
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        _logger.LogInformation("Login attempt for Telegram ID: {TelegramId}", request.TelegramId);

        // Find or create user using the CreateOrUpdateAsync method
        var user = await _userRepository.CreateOrUpdateAsync(
            request.TelegramId,
            request.Username,
            null, // firstName - not provided in login request
            null  // lastName - not provided in login request
        );

        _logger.LogInformation("User authenticated with ID: {UserId}", user.Id);

        // Generate tokens
        var accessToken = GenerateAccessToken(user.Id, user.TelegramId, user.Username);
        var refreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.TokenLifetimeHours);

        _logger.LogInformation("Successfully generated tokens for user ID: {UserId}", user.Id);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            TelegramId = user.TelegramId,
            Username = user.Username
        };
    }

    /// <summary>
    /// Generates a JWT access token with user claims.
    /// </summary>
    public string GenerateAccessToken(int userId, long telegramId, string? username)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("TelegramId", telegramId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            claims.Add(new Claim(ClaimTypes.Name, username));
            claims.Add(new Claim("Username", username));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwtSettings.TokenLifetimeHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Validates a JWT token.
    /// </summary>
    public bool ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
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

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return false;
        }
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// Note: This is a simplified implementation for MVP.
    /// In production, you should store refresh tokens in the database.
    /// </summary>
    public async Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        _logger.LogInformation("Refresh token request received");

        // For MVP, we'll implement a basic refresh mechanism
        // In production, you should:
        // 1. Store refresh tokens in database with expiration
        // 2. Validate the refresh token against stored values
        // 3. Implement token rotation (invalidate old refresh token)

        // This is a placeholder implementation
        // You would typically decode the old token to get user info
        // and validate the refresh token from database

        throw new NotImplementedException(
            "Refresh token functionality is not fully implemented in MVP. " +
            "Users should login again when their access token expires. " +
            "Implement database-backed refresh token storage for production.");
    }
}
