using Microsoft.AspNetCore.Mvc;
using SurveyBot.API.Exceptions;
using SurveyBot.API.Models;

namespace SurveyBot.API.Controllers;

/// <summary>
/// Controller for testing error handling and logging
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TestErrorsController : ControllerBase
{
    private readonly ILogger<TestErrorsController> _logger;

    public TestErrorsController(ILogger<TestErrorsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint for logging different log levels
    /// </summary>
    [HttpGet("logging")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public IActionResult TestLogging()
    {
        _logger.LogTrace("This is a TRACE log message");
        _logger.LogDebug("This is a DEBUG log message");
        _logger.LogInformation("This is an INFORMATION log message");
        _logger.LogWarning("This is a WARNING log message");
        _logger.LogError("This is an ERROR log message");
        _logger.LogCritical("This is a CRITICAL log message");

        return Ok(ApiResponse.Ok("Logging test completed. Check logs for output."));
    }

    /// <summary>
    /// Test endpoint for throwing a generic exception
    /// </summary>
    [HttpGet("error")]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult TestError()
    {
        _logger.LogInformation("Testing unhandled exception");
        throw new InvalidOperationException("This is a test exception to verify error handling");
    }

    /// <summary>
    /// Test endpoint for NotFoundException
    /// </summary>
    [HttpGet("not-found")]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult TestNotFound()
    {
        _logger.LogInformation("Testing NotFoundException");
        throw new NotFoundException("Survey", 999);
    }

    /// <summary>
    /// Test endpoint for ValidationException
    /// </summary>
    [HttpGet("validation")]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult TestValidation()
    {
        _logger.LogInformation("Testing ValidationException");

        var errors = new Dictionary<string, string[]>
        {
            { "Title", new[] { "Title is required", "Title must be at least 3 characters" } },
            { "Description", new[] { "Description cannot be empty" } }
        };

        throw new ValidationException(errors);
    }

    /// <summary>
    /// Test endpoint for BadRequestException
    /// </summary>
    [HttpGet("bad-request")]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult TestBadRequest()
    {
        _logger.LogInformation("Testing BadRequestException");
        throw new BadRequestException("Invalid request parameters", "The survey ID format is invalid");
    }

    /// <summary>
    /// Test endpoint for UnauthorizedException
    /// </summary>
    [HttpGet("unauthorized")]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult TestUnauthorized()
    {
        _logger.LogInformation("Testing UnauthorizedException");
        throw new UnauthorizedException("You must be logged in to access this resource");
    }

    /// <summary>
    /// Test endpoint for ForbiddenException
    /// </summary>
    [HttpGet("forbidden")]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public IActionResult TestForbidden()
    {
        _logger.LogInformation("Testing ForbiddenException");
        throw new ForbiddenException("You do not have permission to access this resource");
    }

    /// <summary>
    /// Test endpoint for ConflictException
    /// </summary>
    [HttpGet("conflict")]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public IActionResult TestConflict()
    {
        _logger.LogInformation("Testing ConflictException");
        throw new ConflictException("Survey with this title already exists", "A survey named 'Customer Satisfaction' already exists");
    }
}
