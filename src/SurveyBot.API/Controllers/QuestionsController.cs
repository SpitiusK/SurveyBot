using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using QuestionNotFoundException = SurveyBot.Core.Exceptions.QuestionNotFoundException;
using UnauthorizedAccessException = SurveyBot.Core.Exceptions.UnauthorizedAccessException;

namespace SurveyBot.API.Controllers;

/// <summary>
/// Controller for managing survey questions.
/// Handles adding, updating, deleting, listing, and reordering questions within surveys.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly ILogger<QuestionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the QuestionsController.
    /// </summary>
    /// <param name="questionService">Question service for business logic.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    public QuestionsController(IQuestionService questionService, ILogger<QuestionsController> logger)
    {
        _questionService = questionService;
        _logger = logger;
    }

    /// <summary>
    /// Adds a new question to a survey.
    /// </summary>
    /// <param name="surveyId">Survey ID to add the question to.</param>
    /// <param name="dto">Question creation data.</param>
    /// <returns>The created question.</returns>
    /// <response code="201">Question created successfully.</response>
    /// <response code="400">Invalid request data or validation failure.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey not found.</response>
    [HttpPost("surveys/{surveyId}/questions")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Add question to survey",
        Description = "Creates a new question for the specified survey. User must own the survey. Options are required for choice-based questions.",
        Tags = new[] { "Questions" }
    )]
    [ProducesResponseType(typeof(ApiResponse<QuestionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> CreateQuestion(
        int surveyId,
        [FromBody] CreateQuestionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for question creation in survey {SurveyId}", surveyId);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Data = ModelState
                });
            }

            // Validate custom validation logic in DTO
            var validationResults = dto.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(dto)).ToList();
            if (validationResults.Any())
            {
                _logger.LogWarning("Validation failed for question creation: {Errors}",
                    string.Join(", ", validationResults.Select(v => v.ErrorMessage)));

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = validationResults.First().ErrorMessage ?? "Validation failed"
                });
            }

            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Creating question for survey {SurveyId} by user {UserId}", surveyId, userId);

            var question = await _questionService.AddQuestionAsync(surveyId, userId, dto);

            return CreatedAtAction(
                nameof(GetQuestionsBySurvey),
                new { surveyId = question.SurveyId },
                ApiResponse<QuestionDto>.Ok(question, "Question created successfully"));
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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized question creation attempt for survey {SurveyId}", surveyId);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (SurveyValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error during question creation");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (SurveyOperationException ex)
        {
            _logger.LogWarning(ex, "Operation error during question creation");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question for survey {SurveyId}", surveyId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while creating the question"
            });
        }
    }

    /// <summary>
    /// Updates an existing question.
    /// </summary>
    /// <param name="id">Question ID to update.</param>
    /// <param name="dto">Updated question data.</param>
    /// <returns>The updated question.</returns>
    /// <response code="200">Question updated successfully.</response>
    /// <response code="400">Invalid request data or question cannot be modified.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own the survey.</response>
    /// <response code="404">Question not found.</response>
    [HttpPut("questions/{id}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Update question",
        Description = "Updates an existing question. User must own the survey. Questions with responses cannot be modified.",
        Tags = new[] { "Questions" }
    )]
    [ProducesResponseType(typeof(ApiResponse<QuestionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> UpdateQuestion(
        int id,
        [FromBody] UpdateQuestionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for question {QuestionId} update", id);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Data = ModelState
                });
            }

            // Validate custom validation logic in DTO
            var validationResults = dto.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(dto)).ToList();
            if (validationResults.Any())
            {
                _logger.LogWarning("Validation failed for question update: {Errors}",
                    string.Join(", ", validationResults.Select(v => v.ErrorMessage)));

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = validationResults.First().ErrorMessage ?? "Validation failed"
                });
            }

            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Updating question {QuestionId} by user {UserId}", id, userId);

            var question = await _questionService.UpdateQuestionAsync(id, userId, dto);

            return Ok(ApiResponse<QuestionDto>.Ok(question, "Question updated successfully"));
        }
        catch (QuestionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Question {QuestionId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Survey for question {QuestionId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized update attempt for question {QuestionId}", id);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (SurveyOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot update question {QuestionId}", id);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (SurveyValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error during question update");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question {QuestionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while updating the question"
            });
        }
    }

    /// <summary>
    /// Deletes a question from a survey.
    /// </summary>
    /// <param name="id">Question ID to delete.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Question deleted successfully.</response>
    /// <response code="400">Question cannot be deleted (has responses).</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own the survey.</response>
    /// <response code="404">Question not found.</response>
    [HttpDelete("questions/{id}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Delete question",
        Description = "Deletes a question from a survey. Questions with responses cannot be deleted. User must own the survey.",
        Tags = new[] { "Questions" }
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Deleting question {QuestionId} by user {UserId}", id, userId);

            await _questionService.DeleteQuestionAsync(id, userId);

            return NoContent();
        }
        catch (QuestionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Question {QuestionId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Survey for question {QuestionId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized delete attempt for question {QuestionId}", id);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (SurveyOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete question {QuestionId}", id);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question {QuestionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting the question"
            });
        }
    }

    /// <summary>
    /// Gets all questions for a survey.
    /// </summary>
    /// <param name="surveyId">Survey ID.</param>
    /// <returns>List of questions ordered by OrderIndex.</returns>
    /// <response code="200">Successfully retrieved questions.</response>
    /// <response code="401">Unauthorized - required for inactive surveys.</response>
    /// <response code="403">Forbidden - user doesn't own inactive survey.</response>
    /// <response code="404">Survey not found.</response>
    [HttpGet("surveys/{surveyId}/questions")]
    [SwaggerOperation(
        Summary = "List survey questions",
        Description = "Gets all questions for a survey ordered by OrderIndex. Active surveys are publicly accessible, inactive surveys require ownership.",
        Tags = new[] { "Questions" }
    )]
    [ProducesResponseType(typeof(ApiResponse<List<QuestionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<QuestionDto>>>> GetQuestionsBySurvey(int surveyId)
    {
        try
        {
            // Try to get userId if authenticated, but allow null for public access to active surveys
            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedUserId))
                {
                    userId = parsedUserId;
                }
            }

            _logger.LogInformation("Getting questions for survey {SurveyId}", surveyId);

            var questions = await _questionService.GetBySurveyIdAsync(surveyId);

            return Ok(ApiResponse<List<QuestionDto>>.Ok(questions));
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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to questions for survey {SurveyId}", surveyId);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions for survey {SurveyId}", surveyId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving questions"
            });
        }
    }

    /// <summary>
    /// Reorders questions within a survey.
    /// </summary>
    /// <param name="surveyId">Survey ID.</param>
    /// <param name="dto">Array of question IDs in the new desired order.</param>
    /// <returns>Success confirmation.</returns>
    /// <response code="200">Questions reordered successfully.</response>
    /// <response code="400">Invalid request data or validation failure.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey not found.</response>
    [HttpPost("surveys/{surveyId}/questions/reorder")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Reorder questions",
        Description = "Changes the order of questions within a survey. All question IDs must belong to the survey. User must own the survey.",
        Tags = new[] { "Questions" }
    )]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> ReorderQuestions(
        int surveyId,
        [FromBody] ReorderQuestionsDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for reordering questions in survey {SurveyId}", surveyId);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Data = ModelState
                });
            }

            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Reordering questions for survey {SurveyId} by user {UserId}", surveyId, userId);

            await _questionService.ReorderQuestionsAsync(surveyId, userId, dto.QuestionIds.ToArray());

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Questions reordered successfully"
            });
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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized reorder attempt for survey {SurveyId}", surveyId);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (SurveyValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error during question reordering");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering questions for survey {SurveyId}", surveyId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while reordering questions"
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
            throw new UnauthorizedAccessException("Invalid or missing user authentication");
        }

        return userId;
    }

    #endregion
}
