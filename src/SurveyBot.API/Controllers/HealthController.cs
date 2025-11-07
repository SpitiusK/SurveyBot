using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace SurveyBot.API.Controllers;

/// <summary>
/// Controller for health check endpoints.
/// Provides basic health status and readiness information.
/// </summary>
[ApiController]
[Route("health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    /// <summary>
    /// Initializes a new instance of the HealthController.
    /// </summary>
    /// <param name="logger">Logger instance for logging operations.</param>
    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint.
    /// Returns 200 OK if the service is running.
    /// </summary>
    /// <returns>Health status information.</returns>
    /// <response code="200">Service is healthy and running.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        var response = new
        {
            success = true,
            status = "healthy",
            service = "SurveyBot API",
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Health check requested - Status: Healthy");

        return Ok(response);
    }

    /// <summary>
    /// Readiness check endpoint.
    /// Indicates if the service is ready to accept requests.
    /// </summary>
    /// <returns>Readiness status.</returns>
    /// <response code="200">Service is ready.</response>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetReadiness()
    {
        var response = new
        {
            success = true,
            ready = true,
            timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Liveness check endpoint.
    /// Indicates if the service is alive and operational.
    /// </summary>
    /// <returns>Liveness status.</returns>
    /// <response code="200">Service is alive.</response>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetLiveness()
    {
        var response = new
        {
            success = true,
            alive = true,
            timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }
}
