using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Common;
using SurveyBot.Core.DTOs.Statistics;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace SurveyBot.API.Controllers;

/// <summary>
/// Controller for managing surveys.
/// Handles CRUD operations for surveys, status toggling, and related operations.
/// </summary>
[ApiController]
[Route("api/surveys")]
[Produces("application/json")]
[Authorize] // Require authentication for all survey endpoints
public class SurveysController : ControllerBase
{
    private readonly ISurveyService _surveyService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SurveysController> _logger;

    /// <summary>
    /// Initializes a new instance of the SurveysController.
    /// </summary>
    /// <param name="surveyService">Survey service for business logic.</param>
    /// <param name="userRepository">User repository for user validation.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    public SurveysController(ISurveyService surveyService, IUserRepository userRepository, ILogger<SurveysController> logger)
    {
        _surveyService = surveyService;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new survey.
    /// </summary>
    /// <param name="dto">Survey creation data.</param>
    /// <returns>The created survey.</returns>
    /// <response code="201">Survey created successfully.</response>
    /// <response code="400">Invalid request data or validation failure.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new survey",
        Description = "Creates a new survey for the authenticated user. Survey is created as inactive by default.",
        Tags = new[] { "Surveys" }
    )]
    [ProducesResponseType(typeof(ApiResponse<SurveyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateSurvey([FromBody] CreateSurveyDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for survey creation");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Data = ModelState
                });
            }

            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Creating survey for user {UserId}", userId);

            // Ensure user exists in database (foreign key requirement)
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User {UserId} not found in database but has valid JWT token", userId);
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User account not found. Please log in again."
                });
            }

            var survey = await _surveyService.CreateSurveyAsync(userId, dto);

            return CreatedAtAction(
                nameof(GetSurveyById),
                new { id = survey.Id },
                ApiResponse<SurveyDto>.Ok(survey, "Survey created successfully"));
        }
        catch (SurveyValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error during survey creation");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating survey");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while creating the survey"
            });
        }
    }

    /// <summary>
    /// Gets a paginated list of surveys for the authenticated user.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-100).</param>
    /// <param name="searchTerm">Optional search term to filter surveys.</param>
    /// <param name="isActive">Optional filter by active status.</param>
    /// <param name="sortBy">Optional sort field (title, createdat, updatedat, isactive).</param>
    /// <param name="sortDescending">Sort in descending order.</param>
    /// <returns>Paginated list of surveys.</returns>
    /// <response code="200">Successfully retrieved surveys.</response>
    /// <response code="400">Invalid query parameters.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "List user's surveys",
        Description = "Gets a paginated list of surveys created by the authenticated user with optional filtering and sorting.",
        Tags = new[] { "Surveys" }
    )]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDto<SurveyListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResultDto<SurveyListDto>>>> GetSurveys(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Getting surveys for user {UserId}", userId);

            var query = new PaginationQueryDto
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortDescending = sortDescending
            };

            var result = await _surveyService.GetAllSurveysAsync(userId, query);

            // Apply isActive filter if specified
            if (isActive.HasValue)
            {
                result.Items = result.Items.Where(s => s.IsActive == isActive.Value).ToList();
                result.TotalCount = result.Items.Count;
                result.TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize);
            }

            return Ok(ApiResponse<PagedResultDto<SurveyListDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting surveys");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving surveys"
            });
        }
    }

    /// <summary>
    /// Gets a specific survey by ID with all questions.
    /// </summary>
    /// <param name="id">Survey ID.</param>
    /// <returns>Survey details with questions.</returns>
    /// <response code="200">Successfully retrieved survey.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey not found.</response>
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Get survey by ID",
        Description = "Gets detailed information about a specific survey including all questions. User must own the survey.",
        Tags = new[] { "Surveys" }
    )]
    [ProducesResponseType(typeof(ApiResponse<SurveyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> GetSurveyById(int id)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Getting survey {SurveyId} for user {UserId}", id, userId);

            var survey = await _surveyService.GetSurveyByIdAsync(id, userId);

            return Ok(ApiResponse<SurveyDto>.Ok(survey));
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Survey {SurveyId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Core.Exceptions.UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to survey {SurveyId}", id);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting survey {SurveyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving the survey"
            });
        }
    }

    /// <summary>
    /// Updates an existing survey.
    /// </summary>
    /// <param name="id">Survey ID.</param>
    /// <param name="dto">Updated survey data.</param>
    /// <returns>Updated survey details.</returns>
    /// <response code="200">Survey updated successfully.</response>
    /// <response code="400">Invalid request data or survey cannot be modified.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey not found.</response>
    [HttpPut("{id}")]
    [SwaggerOperation(
        Summary = "Update survey",
        Description = "Updates an existing survey. Active surveys with responses cannot be modified.",
        Tags = new[] { "Surveys" }
    )]
    [ProducesResponseType(typeof(ApiResponse<SurveyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> UpdateSurvey(int id, [FromBody] UpdateSurveyDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for survey update");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Data = ModelState
                });
            }

            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Updating survey {SurveyId} for user {UserId}", id, userId);

            var survey = await _surveyService.UpdateSurveyAsync(id, userId, dto);

            return Ok(ApiResponse<SurveyDto>.Ok(survey, "Survey updated successfully"));
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Survey {SurveyId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Core.Exceptions.UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized update attempt for survey {SurveyId}", id);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (SurveyOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot update survey {SurveyId}", id);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating survey {SurveyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while updating the survey"
            });
        }
    }

    /// <summary>
    /// Deletes a survey.
    /// </summary>
    /// <param name="id">Survey ID.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Survey deleted successfully.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey not found.</response>
    [HttpDelete("{id}")]
    [SwaggerOperation(
        Summary = "Delete survey",
        Description = "Permanently deletes a survey and all associated questions, responses, and answers. This action cannot be undone.",
        Tags = new[] { "Surveys" }
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSurvey(int id)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Deleting survey {SurveyId} for user {UserId}", id, userId);

            await _surveyService.DeleteSurveyAsync(id, userId);

            return NoContent();
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Survey {SurveyId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Core.Exceptions.UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized delete attempt for survey {SurveyId}", id);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting survey {SurveyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting the survey"
            });
        }
    }

    /// <summary>
    /// Activates a survey to start accepting responses.
    /// </summary>
    /// <param name="id">Survey ID.</param>
    /// <returns>Updated survey details.</returns>
    /// <response code="200">Survey activated successfully.</response>
    /// <response code="400">Survey cannot be activated (e.g., no questions).</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey not found.</response>
    [HttpPost("{id}/activate")]
    [SwaggerOperation(
        Summary = "Activate survey",
        Description = "Activates a survey to make it available for responses. Survey must have at least one question.",
        Tags = new[] { "Surveys" }
    )]
    [ProducesResponseType(typeof(ApiResponse<SurveyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> ActivateSurvey(int id)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Activating survey {SurveyId} for user {UserId}", id, userId);

            var survey = await _surveyService.ActivateSurveyAsync(id, userId);

            return Ok(ApiResponse<SurveyDto>.Ok(survey, "Survey activated successfully"));
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Survey {SurveyId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Core.Exceptions.UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized activation attempt for survey {SurveyId}", id);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (SurveyValidationException ex)
        {
            _logger.LogWarning(ex, "Cannot activate survey {SurveyId}", id);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating survey {SurveyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while activating the survey"
            });
        }
    }

    /// <summary>
    /// Deactivates a survey to stop accepting new responses.
    /// </summary>
    /// <param name="id">Survey ID.</param>
    /// <returns>Updated survey details.</returns>
    /// <response code="200">Survey deactivated successfully.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey not found.</response>
    [HttpPost("{id}/deactivate")]
    [SwaggerOperation(
        Summary = "Deactivate survey",
        Description = "Deactivates a survey to stop accepting new responses. Existing responses are preserved.",
        Tags = new[] { "Surveys" }
    )]
    [ProducesResponseType(typeof(ApiResponse<SurveyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> DeactivateSurvey(int id)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Deactivating survey {SurveyId} for user {UserId}", id, userId);

            var survey = await _surveyService.DeactivateSurveyAsync(id, userId);

            return Ok(ApiResponse<SurveyDto>.Ok(survey, "Survey deactivated successfully"));
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Survey {SurveyId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Core.Exceptions.UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized deactivation attempt for survey {SurveyId}", id);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating survey {SurveyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deactivating the survey"
            });
        }
    }

    /// <summary>
    /// Gets a survey by its unique code (public endpoint - no authentication required).
    /// </summary>
    /// <param name="code">Survey code (6-8 alphanumeric characters).</param>
    /// <returns>Survey details with questions.</returns>
    /// <response code="200">Successfully retrieved survey.</response>
    /// <response code="404">Survey not found or not active.</response>
    [HttpGet("code/{code}")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Get survey by code",
        Description = "Gets survey details by its unique code. This is a public endpoint that doesn't require authentication. Only returns active surveys.",
        Tags = new[] { "Surveys" }
    )]
    [ProducesResponseType(typeof(ApiResponse<SurveyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> GetSurveyByCode(string code)
    {
        try
        {
            _logger.LogInformation("Getting survey by code: {Code}", code);

            var survey = await _surveyService.GetSurveyByCodeAsync(code);

            return Ok(ApiResponse<SurveyDto>.Ok(survey));
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Survey with code {Code} not found", code);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting survey by code {Code}", code);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving the survey"
            });
        }
    }

    /// <summary>
    /// Gets comprehensive statistics for a survey.
    /// </summary>
    /// <param name="id">Survey ID.</param>
    /// <returns>Survey statistics including question-level data.</returns>
    /// <response code="200">Successfully retrieved statistics.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey not found.</response>
    [HttpGet("{id}/statistics")]
    [SwaggerOperation(
        Summary = "Get survey statistics",
        Description = "Gets comprehensive statistics for a survey including response rates, completion times, and question-level analytics.",
        Tags = new[] { "Surveys" }
    )]
    [ProducesResponseType(typeof(ApiResponse<SurveyStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SurveyStatisticsDto>>> GetSurveyStatistics(int id)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Getting statistics for survey {SurveyId} for user {UserId}", id, userId);

            var statistics = await _surveyService.GetSurveyStatisticsAsync(id, userId);

            return Ok(ApiResponse<SurveyStatisticsDto>.Ok(statistics));
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Survey {SurveyId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Core.Exceptions.UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized statistics access for survey {SurveyId}", id);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for survey {SurveyId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving survey statistics"
            });
        }
    }

    /// <summary>
    /// Exports survey responses to CSV file.
    /// </summary>
    /// <param name="id">Survey ID.</param>
    /// <param name="filter">Response filter: "all", "completed", "incomplete" (default: "completed").</param>
    /// <param name="includeMetadata">Include metadata columns (Response ID, Respondent ID, Status).</param>
    /// <param name="includeTimestamps">Include timestamp columns (Started At, Submitted At).</param>
    /// <returns>CSV file download.</returns>
    /// <response code="200">CSV file generated and downloaded successfully.</response>
    /// <response code="400">Invalid filter parameter.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey not found.</response>
    [HttpGet("{id}/export")]
    [SwaggerOperation(
        Summary = "Export survey responses to CSV",
        Description = "Exports survey responses to a CSV file. User must own the survey. Supports filtering by response status and optional metadata/timestamp columns.",
        Tags = new[] { "Surveys" }
    )]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportSurveyToCSV(
        int id,
        [FromQuery] string filter = "completed",
        [FromQuery] bool includeMetadata = true,
        [FromQuery] bool includeTimestamps = true)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation(
                "Exporting survey {SurveyId} to CSV for user {UserId} with filter '{Filter}'",
                id, userId, filter);

            var csvContent = await _surveyService.ExportSurveyToCSVAsync(
                id, userId, filter, includeMetadata, includeTimestamps);

            // Generate filename with survey ID and timestamp
            var fileName = $"survey_{id}_responses_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            // Convert string to byte array with UTF-8 encoding (with BOM for Excel compatibility)
            var preamble = System.Text.Encoding.UTF8.GetPreamble();
            var content = System.Text.Encoding.UTF8.GetBytes(csvContent);
            var csvBytes = new byte[preamble.Length + content.Length];
            preamble.CopyTo(csvBytes, 0);
            content.CopyTo(csvBytes, preamble.Length);

            _logger.LogInformation(
                "CSV export successful for survey {SurveyId}. File size: {Size} bytes",
                id, csvBytes.Length);

            // Return file with proper content type and disposition headers
            return File(csvBytes, "text/csv", fileName);
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Survey {SurveyId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Core.Exceptions.UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized export attempt for survey {SurveyId}", id);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (SurveyValidationException ex)
        {
            _logger.LogWarning(ex, "Invalid filter parameter for survey {SurveyId}", id);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting survey {SurveyId} to CSV", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while exporting the survey to CSV"
            });
        }
    }

    #region Helper Methods

    /// <summary>
    /// Extracts the user ID from JWT claims.
    /// </summary>
    /// <returns>User ID.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user ID cannot be extracted.</exception>
    private int GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            _logger.LogError("Failed to extract user ID from claims");
            throw new Core.Exceptions.UnauthorizedAccessException("Invalid or missing user authentication");
        }

        return userId;
    }

    #endregion
}
