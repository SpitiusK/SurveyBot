using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace SurveyBot.API.Controllers;

/// <summary>
/// Controller for managing conditional question flow.
/// Handles flow configuration, validation, and navigation logic.
/// </summary>
[ApiController]
[Route("api/surveys/{surveyId}/questions")]
[Produces("application/json")]
[Authorize]
public class QuestionFlowController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly ISurveyService _surveyService;
    private readonly ISurveyRepository _surveyRepository;
    private readonly ISurveyValidationService _validationService;
    private readonly IMapper _mapper;
    private readonly ILogger<QuestionFlowController> _logger;

    /// <summary>
    /// Initializes a new instance of the QuestionFlowController.
    /// </summary>
    public QuestionFlowController(
        IQuestionService questionService,
        ISurveyService surveyService,
        ISurveyRepository surveyRepository,
        ISurveyValidationService validationService,
        IMapper mapper,
        ILogger<QuestionFlowController> logger)
    {
        _questionService = questionService;
        _surveyService = surveyService;
        _surveyRepository = surveyRepository;
        _validationService = validationService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Gets the flow configuration for a specific question.
    /// </summary>
    /// <param name="surveyId">Survey ID</param>
    /// <param name="questionId">Question ID</param>
    /// <returns>Conditional flow configuration</returns>
    /// <response code="200">Flow configuration retrieved successfully</response>
    /// <response code="404">Survey or question not found</response>
    /// <response code="401">Unauthorized - invalid or missing token</response>
    /// <response code="403">Forbidden - user does not own the survey</response>
    [HttpGet("{questionId}/flow")]
    [SwaggerOperation(
        Summary = "Get question flow configuration",
        Description = "Returns the conditional flow configuration for a specific question, including default next question and option-specific flows.",
        Tags = new[] { "Question Flow" }
    )]
    [ProducesResponseType(typeof(ApiResponse<ConditionalFlowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ConditionalFlowDto>>> GetQuestionFlow(
        int surveyId,
        int questionId)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation(
                "Getting flow configuration for question {QuestionId} in survey {SurveyId} by user {UserId}",
                questionId, surveyId, userId);

            // Verify ownership
            if (!await _surveyService.UserOwnsSurveyAsync(surveyId, userId))
            {
                _logger.LogWarning(
                    "User {UserId} attempted to access flow for question {QuestionId} in survey {SurveyId} they don't own",
                    userId, questionId, surveyId);
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
                {
                    Success = false,
                    Message = "You do not have permission to access this survey"
                });
            }

            // Get question
            var question = await _questionService.GetByIdAsync(questionId);
            if (question == null)
            {
                _logger.LogWarning("Question {QuestionId} not found", questionId);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Question with ID {questionId} not found"
                });
            }

            // Verify question belongs to survey
            if (question.SurveyId != surveyId)
            {
                _logger.LogWarning(
                    "Question {QuestionId} does not belong to survey {SurveyId}",
                    questionId, surveyId);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Question does not belong to this survey"
                });
            }

            // Map to DTO
            var flowDto = _mapper.Map<ConditionalFlowDto>(question);

            return Ok(ApiResponse<ConditionalFlowDto>.Ok(flowDto, "Flow configuration retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flow for question {QuestionId}", questionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving flow configuration"
            });
        }
    }

    /// <summary>
    /// Updates the flow configuration for a specific question.
    /// </summary>
    /// <param name="surveyId">Survey ID</param>
    /// <param name="questionId">Question ID</param>
    /// <param name="dto">Flow configuration update data</param>
    /// <returns>Updated flow configuration</returns>
    /// <response code="200">Flow configuration updated successfully</response>
    /// <response code="400">Invalid request data or would create cycle</response>
    /// <response code="404">Survey or question not found</response>
    /// <response code="401">Unauthorized - invalid or missing token</response>
    /// <response code="403">Forbidden - user does not own the survey</response>
    [HttpPut("{questionId}/flow")]
    [SwaggerOperation(
        Summary = "Update question flow configuration",
        Description = "Updates the conditional flow configuration for a question. Validates that no cycles are created.",
        Tags = new[] { "Question Flow" }
    )]
    [ProducesResponseType(typeof(ApiResponse<ConditionalFlowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ConditionalFlowDto>>> UpdateQuestionFlow(
        int surveyId,
        int questionId,
        [FromBody] UpdateQuestionFlowDto dto)
    {
        try
        {
            // ═══════════════════════════════════════════════════════════════════════
            // DEBUG LOGGING: Log incoming DTO structure
            // ═══════════════════════════════════════════════════════════════════════
            _logger.LogInformation("📨 UPDATE QUESTION FLOW REQUEST");
            _logger.LogInformation("  Survey ID: {SurveyId}", surveyId);
            _logger.LogInformation("  Question ID: {QuestionId}", questionId);
            _logger.LogInformation("  User ID: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "UNKNOWN");

            // Log DefaultNext details
            if (dto.DefaultNext != null)
            {
                _logger.LogInformation("  DefaultNext:");
                _logger.LogInformation("    - Type: {Type} ({TypeInt})",
                    dto.DefaultNext.Type,
                    (int)dto.DefaultNext.Type);
                _logger.LogInformation("    - NextQuestionId: {NextQuestionId}",
                    dto.DefaultNext.NextQuestionId?.ToString() ?? "NULL");
            }
            else
            {
                _logger.LogInformation("  DefaultNext: NULL (sequential flow)");
            }

            // Log OptionNextDeterminants details
            if (dto.OptionNextDeterminants != null && dto.OptionNextDeterminants.Any())
            {
                _logger.LogInformation("  OptionNextDeterminants: {Count} option(s)", dto.OptionNextDeterminants.Count);
                foreach (var kvp in dto.OptionNextDeterminants)
                {
                    _logger.LogInformation("    - Option {OptionId}:",
                        kvp.Key);
                    _logger.LogInformation("        Type: {Type} ({TypeInt})",
                        kvp.Value.Type,
                        (int)kvp.Value.Type);
                    _logger.LogInformation("        NextQuestionId: {NextQuestionId}",
                        kvp.Value.NextQuestionId?.ToString() ?? "NULL");
                }
            }
            else if (dto.OptionNextDeterminants != null)
            {
                _logger.LogInformation("  OptionNextDeterminants: Empty dictionary (non-branching question)");
            }
            else
            {
                _logger.LogInformation("  OptionNextDeterminants: NULL");
            }

            // ═══════════════════════════════════════════════════════════════════════
            // MODEL STATE VALIDATION
            // ═══════════════════════════════════════════════════════════════════════
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ MODEL STATE VALIDATION FAILED");
                _logger.LogWarning("  Total Errors: {ErrorCount}",
                    ModelState.Values.SelectMany(v => v.Errors).Count());

                // Log each validation error with field name
                foreach (var modelStateEntry in ModelState)
                {
                    if (modelStateEntry.Value?.Errors.Count > 0)
                    {
                        _logger.LogWarning("  Field '{FieldName}' has {ErrorCount} error(s):",
                            modelStateEntry.Key,
                            modelStateEntry.Value.Errors.Count);

                        foreach (var error in modelStateEntry.Value.Errors)
                        {
                            if (!string.IsNullOrEmpty(error.ErrorMessage))
                            {
                                _logger.LogWarning("    - {ErrorMessage}", error.ErrorMessage);
                            }
                            if (error.Exception != null)
                            {
                                _logger.LogWarning("    - Exception: {ExceptionMessage}",
                                    error.Exception.Message);
                            }
                        }
                    }
                }

                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Data = new { errors }
                });
            }

            _logger.LogInformation("✅ MODEL STATE VALIDATION PASSED");

            var userId = GetUserIdFromClaims();
            _logger.LogInformation(
                "Updating flow for question {QuestionId} in survey {SurveyId} by user {UserId}",
                questionId, surveyId, userId);

            // 2. Validate question exists and belongs to survey (WITH OPTIONS for validation)
            var questionEntity = await _questionService.GetByIdWithOptionsAsync(questionId);
            if (questionEntity == null)
            {
                _logger.LogWarning("Question {QuestionId} not found", questionId);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Question {questionId} not found"
                });
            }

            // Log loaded options for debugging
            _logger.LogInformation("✅ Question {QuestionId} loaded with {OptionCount} options",
                questionId, questionEntity.Options?.Count ?? 0);

            if (questionEntity.Options != null && questionEntity.Options.Any())
            {
                _logger.LogInformation("  Available option IDs: {OptionIds}",
                    string.Join(", ", questionEntity.Options.Select(o => o.Id)));
            }

            if (questionEntity.SurveyId != surveyId)
            {
                _logger.LogWarning(
                    "Question {QuestionId} does not belong to survey {SurveyId}",
                    questionId, surveyId);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Question {questionId} does not belong to survey {surveyId}"
                });
            }

            // 3. Verify ownership
            if (!await _surveyService.UserOwnsSurveyAsync(surveyId, userId))
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update flow for question {QuestionId} in survey {SurveyId} they don't own",
                    userId, questionId, surveyId);
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
                {
                    Success = false,
                    Message = "You do not have permission to modify this survey"
                });
            }

            // 4. Validate survey is editable (not active)
            var survey = await _surveyRepository.GetByIdAsync(surveyId);
            if (survey == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Survey {surveyId} not found"
                });
            }

            if (survey.IsActive)
            {
                _logger.LogWarning(
                    "Cannot update flow for active survey {SurveyId}",
                    surveyId);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Cannot modify flow for active surveys. Deactivate the survey first."
                });
            }

            // 5. Validate self-reference in DefaultNext
            if (dto.DefaultNext?.Type == Core.Enums.NextStepType.GoToQuestion &&
                dto.DefaultNext.NextQuestionId == questionId)
            {
                _logger.LogWarning(
                    "Self-reference detected: Question {QuestionId} references itself in DefaultNext",
                    questionId);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "A question cannot reference itself as the next question"
                });
            }

            // 6. Validate DefaultNext target question exists and belongs to survey
            if (dto.DefaultNext?.Type == Core.Enums.NextStepType.GoToQuestion &&
                dto.DefaultNext.NextQuestionId.HasValue)
            {
                var targetQuestion = await _questionService.GetByIdAsync(dto.DefaultNext.NextQuestionId.Value);
                if (targetQuestion == null)
                {
                    _logger.LogWarning(
                        "DefaultNext target question {NextQuestionId} does not exist",
                        dto.DefaultNext.NextQuestionId.Value);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Question {dto.DefaultNext.NextQuestionId.Value} does not exist"
                    });
                }

                if (targetQuestion.SurveyId != surveyId)
                {
                    _logger.LogWarning(
                        "DefaultNext target question {NextQuestionId} belongs to different survey",
                        dto.DefaultNext.NextQuestionId.Value);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Question {dto.DefaultNext.NextQuestionId.Value} belongs to a different survey"
                    });
                }
            }

            // 7. Validate OptionNextDeterminants references
            if (dto.OptionNextDeterminants != null && dto.OptionNextDeterminants.Any())
            {
                foreach (var kvp in dto.OptionNextDeterminants)
                {
                    var optionId = kvp.Key;
                    var determinant = kvp.Value;

                    // Validate option belongs to question (using questionEntity with loaded Options)
                    if (!questionEntity.Options.Any(o => o.Id == optionId))
                    {
                        _logger.LogWarning(
                            "❌ Option {OptionId} does not belong to question {QuestionId}",
                            optionId, questionId);
                        _logger.LogWarning("   Available option IDs: {AvailableIds}",
                            string.Join(", ", questionEntity.Options.Select(o => o.Id)));
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Option {optionId} does not belong to question {questionId}"
                        });
                    }

                    _logger.LogInformation("✅ Option {OptionId} validated successfully", optionId);

                    // Prevent self-reference in options
                    if (determinant?.Type == Core.Enums.NextStepType.GoToQuestion &&
                        determinant.NextQuestionId == questionId)
                    {
                        _logger.LogWarning(
                            "Self-reference in option {OptionId}: points to question {QuestionId}",
                            optionId, questionId);
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Option {optionId} cannot reference the same question"
                        });
                    }

                    // Validate next question exists
                    if (determinant?.Type == Core.Enums.NextStepType.GoToQuestion &&
                        determinant.NextQuestionId.HasValue)
                    {
                        var targetQuestion = await _questionService.GetByIdAsync(determinant.NextQuestionId.Value);
                        if (targetQuestion == null)
                        {
                            _logger.LogWarning(
                                "Next question {NextQuestionId} for option {OptionId} does not exist",
                                determinant.NextQuestionId.Value, optionId);
                            return BadRequest(new ApiResponse<object>
                            {
                                Success = false,
                                Message = $"Question {determinant.NextQuestionId.Value} (referenced by option {optionId}) does not exist"
                            });
                        }

                        if (targetQuestion.SurveyId != surveyId)
                        {
                            _logger.LogWarning(
                                "Next question {NextQuestionId} belongs to different survey",
                                determinant.NextQuestionId.Value);
                            return BadRequest(new ApiResponse<object>
                            {
                                Success = false,
                                Message = $"Question {determinant.NextQuestionId.Value} belongs to a different survey"
                            });
                        }
                    }
                }
            }

            // ═══════════════════════════════════════════════════════════════════════
            // AUTHORIZATION CHECK PASSED - Calling Service Layer
            // ═══════════════════════════════════════════════════════════════════════
            _logger.LogInformation("🔄 Calling QuestionService.UpdateQuestionFlowAsync");
            _logger.LogInformation("  Survey ID: {SurveyId}, Question ID: {QuestionId}",
                surveyId, questionId);

            // Update flow configuration via QuestionService
            var updatedQuestion = await _questionService.UpdateQuestionFlowAsync(questionId, dto);

            _logger.LogInformation("✅ Service layer completed successfully");

            // Detect cycles after update
            var cycleResult = await _validationService.DetectCycleAsync(surveyId);
            if (cycleResult.HasCycle)
            {
                _logger.LogWarning(
                    "Flow update for question {QuestionId} would create cycle. Path: {CyclePath}",
                    questionId, string.Join(" -> ", cycleResult.CyclePath!));
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"This flow configuration would create a cycle: {cycleResult.ErrorMessage}",
                    Data = new { cyclePath = cycleResult.CyclePath }
                });
            }

            // Map to DTO
            var flowDto = _mapper.Map<ConditionalFlowDto>(updatedQuestion);

            _logger.LogInformation("✅ UPDATE QUESTION FLOW COMPLETED SUCCESSFULLY");
            _logger.LogInformation("  Survey ID: {SurveyId}, Question ID: {QuestionId}", surveyId, questionId);
            _logger.LogInformation("  Returned ConditionalFlowDto:");
            _logger.LogInformation("    - SupportsBranching: {SupportsBranching}",
                flowDto.SupportsBranching);
            _logger.LogInformation("    - DefaultNext Type: {Type}",
                flowDto.DefaultNext?.Type.ToString() ?? "NULL");
            _logger.LogInformation("    - OptionFlows Count: {Count}",
                flowDto.OptionFlows.Count);

            return Ok(ApiResponse<ConditionalFlowDto>.Ok(flowDto, "Flow configuration updated successfully"));
        }
        catch (QuestionNotFoundException ex)
        {
            _logger.LogWarning("❌ Question not found: {Message}", ex.Message);
            _logger.LogWarning("  Survey ID: {SurveyId}, Question ID: {QuestionId}",
                surveyId, questionId);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
        catch (SurveyCycleException ex)
        {
            _logger.LogWarning("❌ Survey cycle detected: {Message}", ex.Message);
            _logger.LogWarning("  Cycle Path: {CyclePath}",
                string.Join(" → ", ex.CyclePath ?? new List<int>()));
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = new { cyclePath = ex.CyclePath }
            });
        }
        catch (QuestionValidationException ex)
        {
            _logger.LogWarning("❌ Validation error: {Message}", ex.Message);
            _logger.LogWarning("  Survey ID: {SurveyId}, Question ID: {QuestionId}",
                surveyId, questionId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "❌ DATABASE UPDATE EXCEPTION");
            _logger.LogError("  Survey ID: {SurveyId}, Question ID: {QuestionId}",
                surveyId, questionId);
            _logger.LogError("  DTO.DefaultNext: {DefaultNext}", dto.DefaultNext?.ToString() ?? "NULL");
            _logger.LogError("  DTO.OptionNextDeterminants: {@OptionNextDeterminants}",
                dto.OptionNextDeterminants?.Select(kvp => $"{kvp.Key}→{kvp.Value}") ?? Array.Empty<string>());
            _logger.LogError("  Inner Exception: {InnerException}", ex.InnerException?.Message);

            // Check for FK constraint violation
            if (ex.InnerException?.Message.Contains("23503") == true ||
                ex.InnerException?.Message.Contains("foreign key constraint") == true)
            {
                _logger.LogError("  ⚠️ FK CONSTRAINT VIOLATION DETECTED");
                _logger.LogError("  Constraint: fk_questions_default_next_question");
                _logger.LogError("  Attempted Value: {Value}", dto.DefaultNext?.ToString() ?? "NULL");
            }

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Database error while updating question flow",
                Data = new { details = ex.InnerException?.Message ?? ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ UNEXPECTED ERROR in UpdateQuestionFlow");
            _logger.LogError("  Survey ID: {SurveyId}, Question ID: {QuestionId}",
                surveyId, questionId);
            _logger.LogError("  Exception Type: {ExceptionType}", ex.GetType().Name);
            _logger.LogError("  Stack Trace: {StackTrace}", ex.StackTrace);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while updating flow configuration",
                Data = null
            });
        }
    }

    /// <summary>
    /// Validates the complete survey flow configuration.
    /// </summary>
    /// <param name="surveyId">Survey ID</param>
    /// <returns>Validation result with details</returns>
    /// <response code="200">Validation completed</response>
    /// <response code="404">Survey not found</response>
    /// <response code="401">Unauthorized - invalid or missing token</response>
    /// <response code="403">Forbidden - user does not own the survey</response>
    [HttpPost("validate")]
    [SwaggerOperation(
        Summary = "Validate survey flow",
        Description = "Validates the complete survey flow configuration, checking for cycles, endpoints, and unconfigured questions.",
        Tags = new[] { "Question Flow" }
    )]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> ValidateSurveyFlow(int surveyId)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation(
                "Validating flow for survey {SurveyId} by user {UserId}",
                surveyId, userId);

            // Verify ownership
            if (!await _surveyService.UserOwnsSurveyAsync(surveyId, userId))
            {
                _logger.LogWarning(
                    "User {UserId} attempted to validate flow for survey {SurveyId} they don't own",
                    userId, surveyId);
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
                {
                    Success = false,
                    Message = "You do not have permission to access this survey"
                });
            }

            var errors = new List<string>();

            // Check for cycles
            var cycleResult = await _validationService.DetectCycleAsync(surveyId);
            if (cycleResult.HasCycle)
            {
                errors.Add($"Cycle detected: {cycleResult.ErrorMessage}");
                _logger.LogWarning(
                    "Survey {SurveyId} has cycle: {CyclePath}",
                    surveyId, string.Join(" -> ", cycleResult.CyclePath!));
            }

            // Check for endpoints
            var endpoints = await _validationService.FindSurveyEndpointsAsync(surveyId);
            if (!endpoints.Any())
            {
                errors.Add("No questions lead to survey completion. At least one question must point to end of survey.");
                _logger.LogWarning("Survey {SurveyId} has no endpoints", surveyId);
            }

            // Prepare response
            var isValid = !errors.Any();
            var responseData = new
            {
                valid = isValid,
                errors = errors.Any() ? errors : null,
                cyclePath = cycleResult.HasCycle ? cycleResult.CyclePath : null,
                endpointCount = endpoints.Count
            };

            var message = isValid
                ? "Survey flow is valid"
                : "Survey flow has validation errors";

            _logger.LogInformation(
                "Survey {SurveyId} flow validation result: Valid={IsValid}, Errors={ErrorCount}",
                surveyId, isValid, errors.Count);

            return Ok(ApiResponse<object>.Ok(responseData, message));
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating flow for survey {SurveyId}", surveyId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while validating survey flow"
            });
        }
    }

    /// <summary>
    /// Extracts the user ID from JWT claims.
    /// </summary>
    private int GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            _logger.LogWarning("Invalid authentication: Unable to extract user ID from claims");
            throw new Core.Exceptions.UnauthorizedAccessException("Invalid authentication");
        }
        return userId;
    }
}
