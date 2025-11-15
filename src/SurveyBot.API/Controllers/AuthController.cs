using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Core.DTOs.User;
using SurveyBot.Core.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SurveyBot.API.Controllers;

/// <summary>
/// Controller for authentication and JWT token management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IUserService userService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns JWT tokens.
    /// </summary>
    /// <param name="request">Login request containing Telegram ID.</param>
    /// <returns>JWT access token and user information.</returns>
    /// <response code="200">Login successful, returns access token.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="500">Internal server error during authentication.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Login and get JWT token",
        Description = "Authenticates a user by Telegram ID and returns a JWT access token. Creates a new user if one doesn't exist.",
        Tags = new[] { "Authentication" }
    )]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            _logger.LogInformation("Login request received for Telegram ID: {TelegramId}", request.TelegramId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid login request model state");
                var errorResponse = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request data"
                };
                return BadRequest(errorResponse);
            }

            var result = await _authService.LoginAsync(request);

            _logger.LogInformation("Login successful for user ID: {UserId}", result.User.Id);

            return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for Telegram ID: {TelegramId}", request.TelegramId);
            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred during login"
            };
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <returns>New JWT access token.</returns>
    /// <response code="200">Token refresh successful.</response>
    /// <response code="400">Invalid refresh token.</response>
    /// <response code="501">Not implemented in MVP.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Refresh access token (MVP placeholder)",
        Description = "Refreshes an access token using a refresh token. Note: This is not fully implemented in MVP. Users should login again when their token expires.",
        Tags = new[] { "Authentication" }
    )]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status501NotImplemented)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            _logger.LogInformation("Refresh token request received");

            if (!ModelState.IsValid)
            {
                var errorResponse = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request data"
                };
                return BadRequest(errorResponse);
            }

            var result = await _authService.RefreshTokenAsync(request);

            return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Token refreshed successfully"));
        }
        catch (NotImplementedException ex)
        {
            _logger.LogWarning(ex, "Refresh token not implemented");
            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            };
            return StatusCode(StatusCodes.Status501NotImplemented, errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred during token refresh"
            };
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Validates the current JWT token.
    /// </summary>
    /// <returns>Token validation result.</returns>
    /// <response code="200">Token is valid.</response>
    /// <response code="401">Token is invalid or expired.</response>
    [HttpGet("validate")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Validate JWT token",
        Description = "Validates the current JWT token. This endpoint requires authentication and will return 401 if the token is invalid.",
        Tags = new[] { "Authentication" }
    )]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponse<object>> ValidateToken()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var telegramId = User.FindFirst("TelegramId")?.Value;
        var username = User.FindFirst("Username")?.Value;

        _logger.LogInformation("Token validated for user ID: {UserId}", userId);

        return Ok(ApiResponse<object>.Ok(
            new
            {
                valid = true,
                userId = userId,
                telegramId = telegramId,
                username = username
            },
            "Token is valid"
        ));
    }

    /// <summary>
    /// Registers a new user or updates an existing user on login (upsert pattern).
    /// This endpoint is designed for Telegram bot integration where users are automatically
    /// registered on first interaction via /start command.
    /// </summary>
    /// <param name="registerDto">Registration data containing Telegram user information.</param>
    /// <returns>User information with JWT token.</returns>
    /// <response code="200">Registration/login successful, returns user with token.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="500">Internal server error during registration.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Register or login user (upsert)",
        Description = "Registers a new user or updates existing user information. Returns user data with JWT token. " +
                      "This endpoint uses an upsert pattern - if the user exists by Telegram ID, it updates their info; " +
                      "otherwise, it creates a new user. Designed for Telegram bot /start command integration.",
        Tags = new[] { "Authentication" }
    )]
    [ProducesResponseType(typeof(ApiResponse<UserWithTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UserWithTokenDto>>> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            _logger.LogInformation(
                "Registration/login request received for Telegram ID: {TelegramId}, Username: {Username}",
                registerDto.TelegramId,
                registerDto.Username);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid registration request model state");
                var errorResponse = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request data"
                };
                return BadRequest(errorResponse);
            }

            var result = await _userService.RegisterAsync(registerDto);

            _logger.LogInformation(
                "Registration/login successful for user ID: {UserId}, Telegram ID: {TelegramId}",
                result.User.Id,
                result.User.TelegramId);

            return Ok(ApiResponse<UserWithTokenDto>.Ok(
                result,
                "Registration/login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error during registration for Telegram ID: {TelegramId}",
                registerDto.TelegramId);

            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred during registration"
            };
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Test endpoint to verify authentication is working.
    /// </summary>
    /// <returns>User information from token claims.</returns>
    /// <response code="200">Returns authenticated user information.</response>
    /// <response code="401">Unauthorized - token missing or invalid.</response>
    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get current user info",
        Description = "Returns the current authenticated user's information extracted from the JWT token claims.",
        Tags = new[] { "Authentication" }
    )]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponse<object>> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var telegramId = User.FindFirst("TelegramId")?.Value;
        var username = User.FindFirst("Username")?.Value;

        return Ok(ApiResponse<object>.Ok(
            new
            {
                userId = userId,
                telegramId = telegramId,
                username = username
            },
            "Current user information"
        ));
    }
}
