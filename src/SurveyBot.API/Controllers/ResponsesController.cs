using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Common;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace SurveyBot.API.Controllers;

/// <summary>
/// Controller for managing survey responses.
/// Handles submission and retrieval of survey responses from Telegram users.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class ResponsesController : ControllerBase
{
    private readonly IResponseService _responseService;
    private readonly ISurveyRepository _surveyRepository;
    private readonly IQuestionService _questionService;
    private readonly ILogger<ResponsesController> _logger;

    /// <summary>
    /// Initializes a new instance of the ResponsesController.
    /// </summary>
    /// <param name="responseService">Response service for business logic.</param>
    /// <param name="surveyRepository">Survey repository for validation.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    public ResponsesController(
        IResponseService responseService,
        ISurveyRepository surveyRepository,
        IQuestionService questionService,
        ILogger<ResponsesController> logger)
    {
        _responseService = responseService;
        _surveyRepository = surveyRepository;
        _questionService = questionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all responses for a specific survey with pagination and filtering.
    /// </summary>
    /// <param name="surveyId">Survey ID.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (1-100).</param>
    /// <param name="completedOnly">Filter to show only completed responses.</param>
    /// <returns>Paginated list of responses.</returns>
    /// <response code="200">Successfully retrieved responses.</response>
    /// <response code="400">Invalid query parameters.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey not found.</response>
    [HttpGet("surveys/{surveyId}/responses")]
    [Authorize]
    [SwaggerOperation(
        Summary = "List survey responses",
        Description = "Gets a paginated list of responses for a survey. Only accessible by the survey creator.",
        Tags = new[] { "Responses" }
    )]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDto<ResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResultDto<ResponseDto>>>> GetSurveyResponses(
        int surveyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool completedOnly = false)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1)
            {
                _logger.LogWarning("Invalid page number: {PageNumber}", pageNumber);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Page number must be at least 1"
                });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                _logger.LogWarning("Invalid page size: {PageSize}", pageSize);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Page size must be between 1 and 100"
                });
            }

            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Getting responses for survey {SurveyId} by user {UserId}", surveyId, userId);

            var responses = await _responseService.GetSurveyResponsesAsync(
                surveyId,
                userId,
                pageNumber,
                pageSize,
                completedOnly ? true : null);

            return Ok(ApiResponse<PagedResultDto<ResponseDto>>.Ok(responses));
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Survey {SurveyId} not found", surveyId);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Core.Exceptions.UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to survey {SurveyId} responses", surveyId);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting responses for survey {SurveyId}", surveyId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving responses"
            });
        }
    }

    /// <summary>
    /// Gets a single response with all answers.
    /// </summary>
    /// <param name="id">Response ID.</param>
    /// <returns>Response details with all answers.</returns>
    /// <response code="200">Successfully retrieved response.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user cannot access this response.</response>
    /// <response code="404">Response not found.</response>
    [HttpGet("responses/{id}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get response by ID",
        Description = "Gets detailed information about a specific response including all answers. User must be the survey creator or the respondent.",
        Tags = new[] { "Responses" }
    )]
    [ProducesResponseType(typeof(ApiResponse<ResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ResponseDto>>> GetResponseById(int id)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Getting response {ResponseId} for user {UserId}", id, userId);

            var response = await _responseService.GetResponseAsync(id, userId);

            return Ok(ApiResponse<ResponseDto>.Ok(response));
        }
        catch (ResponseNotFoundException ex)
        {
            _logger.LogWarning(ex, "Response {ResponseId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Core.Exceptions.UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to response {ResponseId}", id);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting response {ResponseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving the response"
            });
        }
    }

    /// <summary>
    /// Creates a new survey response (starts taking a survey).
    /// </summary>
    /// <param name="surveyId">Survey ID.</param>
    /// <param name="dto">Response creation data.</param>
    /// <returns>The created response.</returns>
    /// <response code="201">Response created successfully.</response>
    /// <response code="400">Invalid request data or survey is not active.</response>
    /// <response code="404">Survey not found.</response>
    /// <response code="409">User has already completed this survey.</response>
    [HttpPost("surveys/{surveyId}/responses")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Create new response",
        Description = "Starts a new response for a survey. Public endpoint used by the Telegram bot when a user begins a survey.",
        Tags = new[] { "Responses" }
    )]
    [ProducesResponseType(typeof(ApiResponse<ResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ResponseDto>>> CreateResponse(
        int surveyId,
        [FromBody] CreateResponseDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for response creation");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Data = ModelState
                });
            }

            _logger.LogInformation(
                "Creating response for survey {SurveyId} by Telegram user {TelegramUserId}",
                surveyId,
                dto.RespondentTelegramId);

            // Validate survey exists and is active
            var survey = await _surveyRepository.GetByIdAsync(surveyId);
            if (survey == null)
            {
                _logger.LogWarning("Survey {SurveyId} not found", surveyId);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Survey with ID {surveyId} not found"
                });
            }

            if (!survey.IsActive)
            {
                _logger.LogWarning("Attempt to respond to inactive survey {SurveyId}", surveyId);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "This survey is not currently active"
                });
            }

            var response = await _responseService.StartResponseAsync(
                surveyId,
                dto.RespondentTelegramId,
                dto.RespondentUsername,
                dto.RespondentFirstName);

            return CreatedAtAction(
                nameof(GetResponseById),
                new { id = response.Id },
                ApiResponse<ResponseDto>.Ok(response, "Response created successfully"));
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Survey {SurveyId} not found", surveyId);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (DuplicateResponseException ex)
        {
            _logger.LogWarning(ex, "Duplicate response attempt for survey {SurveyId}", surveyId);
            return Conflict(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (SurveyOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot create response for survey {SurveyId}", surveyId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating response for survey {SurveyId}", surveyId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while creating the response"
            });
        }
    }

    /// <summary>
    /// Saves an answer to a question within a response.
    /// </summary>
    /// <param name="id">Response ID.</param>
    /// <param name="dto">Answer submission data.</param>
    /// <returns>Updated response with the new answer.</returns>
    /// <response code="200">Answer saved successfully.</response>
    /// <response code="400">Invalid answer format or response is already completed.</response>
    /// <response code="404">Response or question not found.</response>
    [HttpPost("responses/{id}/answers")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Save answer to question",
        Description = "Saves an individual answer to a question within an ongoing response. Public endpoint used by the Telegram bot.",
        Tags = new[] { "Responses" }
    )]
    [ProducesResponseType(typeof(ApiResponse<ResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ResponseDto>>> SaveAnswer(
        int id,
        [FromBody] SubmitAnswerDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for answer submission");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Data = ModelState
                });
            }

            _logger.LogInformation(
                "Saving answer for response {ResponseId}, question {QuestionId}",
                id,
                dto.Answer.QuestionId);

            // Validate answer format before saving
            var validationResult = await _responseService.ValidateAnswerFormatAsync(
                dto.Answer.QuestionId,
                dto.Answer.AnswerText,
                dto.Answer.SelectedOptions,
                dto.Answer.RatingValue);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Invalid answer format for question {QuestionId}: {Error}",
                    dto.Answer.QuestionId,
                    validationResult.ErrorMessage);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = validationResult.ErrorMessage
                });
            }

            var response = await _responseService.SaveAnswerAsync(
                id,
                dto.Answer.QuestionId,
                dto.Answer.AnswerText,
                dto.Answer.SelectedOptions,
                dto.Answer.RatingValue);

            return Ok(ApiResponse<ResponseDto>.Ok(response, "Answer saved successfully"));
        }
        catch (ResponseNotFoundException ex)
        {
            _logger.LogWarning(ex, "Response {ResponseId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (QuestionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Question {QuestionId} not found", dto.Answer.QuestionId);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidAnswerFormatException ex)
        {
            _logger.LogWarning(ex, "Invalid answer format for question {QuestionId}", dto.Answer.QuestionId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving answer for response {ResponseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while saving the answer"
            });
        }
    }

    /// <summary>
    /// Marks a response as completed.
    /// </summary>
    /// <param name="id">Response ID.</param>
    /// <returns>Completed response details.</returns>
    /// <response code="200">Response completed successfully.</response>
    /// <response code="404">Response not found.</response>
    /// <response code="409">Response is already completed.</response>
    [HttpPost("responses/{id}/complete")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Complete response",
        Description = "Marks a response as completed with timestamp. Public endpoint used by the Telegram bot when a user finishes a survey.",
        Tags = new[] { "Responses" }
    )]
    [ProducesResponseType(typeof(ApiResponse<ResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ResponseDto>>> CompleteResponse(int id)
    {
        try
        {
            _logger.LogInformation("Completing response {ResponseId}", id);

            var response = await _responseService.CompleteResponseAsync(id);

            return Ok(ApiResponse<ResponseDto>.Ok(response, "Response completed successfully"));
        }
        catch (ResponseNotFoundException ex)
        {
            _logger.LogWarning(ex, "Response {ResponseId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot complete response {ResponseId}", id);
            return Conflict(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing response {ResponseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while completing the response"
            });
        }
    }

    /// <summary>
    /// Gets the next question in the survey flow for a response.
    /// </summary>
    /// <param name="id">Response ID.</param>
    /// <returns>Next question to be answered, or 204 No Content if survey is complete.</returns>
    /// <response code="200">Next question retrieved successfully.</response>
    /// <response code="204">No more questions - survey is complete.</response>
    /// <response code="404">Response or next question not found.</response>
    [HttpGet("responses/{id}/next-question")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Get next question",
        Description = "Returns the next question to show in the survey flow based on previous answers and conditional flow logic. Returns 204 No Content if survey is complete. Public endpoint used by the Telegram bot.",
        Tags = new[] { "Responses" }
    )]
    [ProducesResponseType(typeof(ApiResponse<Core.DTOs.Question.QuestionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Core.DTOs.Question.QuestionDto>>> GetNextQuestion(int id)
    {
        try
        {
            _logger.LogInformation("Getting next question for response {ResponseId}", id);

            // Get response
            var response = await _responseService.GetResponseAsync(id);
            if (response == null)
            {
                _logger.LogWarning("Response {ResponseId} not found", id);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Response with ID {id} not found"
                });
            }

            // Get next question ID
            var nextQuestionId = await _responseService.GetNextQuestionAsync(id);

            if (nextQuestionId == null)
            {
                // Survey complete - return 204 No Content
                _logger.LogInformation("No more questions for response {ResponseId} - survey complete", id);
                return NoContent();
            }

            // Get the question DTO
            var questionDto = await _questionService.GetQuestionAsync(nextQuestionId.Value);

            _logger.LogInformation(
                "Retrieved next question {QuestionId} for response {ResponseId}",
                nextQuestionId, id);

            return Ok(ApiResponse<Core.DTOs.Question.QuestionDto>.Ok(questionDto, "Next question retrieved successfully"));
        }
        catch (ResponseNotFoundException ex)
        {
            _logger.LogWarning(ex, "Response {ResponseId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next question for response {ResponseId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving the next question"
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
