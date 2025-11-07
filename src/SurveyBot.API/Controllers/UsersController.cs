using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SurveyBot.API.Controllers;

/// <summary>
/// Controller for managing users.
/// Handles user operations including Telegram users and admin users.
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize] // Require authentication for all user endpoints
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;

    /// <summary>
    /// Initializes a new instance of the UsersController.
    /// </summary>
    /// <param name="logger">Logger instance for logging operations.</param>
    public UsersController(ILogger<UsersController> logger)
    {
        _logger = logger;
    }

    // TODO: Implement endpoints:
    // GET /api/users - List users
    // GET /api/users/{id} - Get user details
    // POST /api/users - Create user (for admin)
    // PUT /api/users/{id} - Update user
    // DELETE /api/users/{id} - Delete user
}
