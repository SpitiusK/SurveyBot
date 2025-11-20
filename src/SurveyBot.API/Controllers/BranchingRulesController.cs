using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Branching;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using System.Text.Json;

namespace SurveyBot.API.Controllers;

/// <summary>
/// Controller for managing question branching rules.
/// Handles creating, updating, deleting, and retrieving branching logic for survey questions.
/// </summary>
[ApiController]
[Route("api/surveys/{surveyId}/questions/{sourceQuestionId}/branches")]
[Produces("application/json")]
[Authorize]
public class BranchingRulesController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly IQuestionBranchingRuleRepository _branchingRuleRepository;
    private readonly ISurveyRepository _surveyRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly ILogger<BranchingRulesController> _logger;

    public BranchingRulesController(
        IQuestionService questionService,
        IQuestionBranchingRuleRepository branchingRuleRepository,
        ISurveyRepository surveyRepository,
        IQuestionRepository questionRepository,
        ILogger<BranchingRulesController> logger)
    {
        _questionService = questionService;
        _branchingRuleRepository = branchingRuleRepository;
        _surveyRepository = surveyRepository;
        _questionRepository = questionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new branching rule for a question.
    /// </summary>
    /// <param name="surveyId">Survey ID.</param>
    /// <param name="sourceQuestionId">Source question ID.</param>
    /// <param name="dto">Branching rule creation data.</param>
    /// <returns>The created branching rule.</returns>
    /// <response code="201">Branching rule created successfully.</response>
    /// <response code="400">Invalid request data or validation failure.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey or question not found.</response>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create branching rule",
        Description = "Creates a new branching rule for a question. User must own the survey. Validates that both questions exist and belong to the survey, and checks for circular dependencies.",
        Tags = new[] { "Branching Rules" }
    )]
    [ProducesResponseType(typeof(ApiResponse<BranchingRuleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BranchingRuleDto>>> CreateBranchingRule(
        int surveyId,
        int sourceQuestionId,
        [FromBody] CreateBranchingRuleDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for branching rule creation");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Data = ModelState
                });
            }

            var userId = GetUserIdFromClaims();
            _logger.LogInformation(
                "Creating branching rule for survey {SurveyId}, source question {SourceQuestionId} by user {UserId}",
                surveyId, sourceQuestionId, userId);

            // 1. Verify survey exists and user owns it
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

            if (survey.CreatorId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to create branching rule for survey {SurveyId} owned by {CreatorId}",
                    userId, surveyId, survey.CreatorId);
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
                {
                    Success = false,
                    Message = "You don't have permission to modify this survey"
                });
            }

            // 2. Validate both questions exist and belong to survey
            var sourceQuestion = await _questionRepository.GetByIdAsync(dto.SourceQuestionId);
            if (sourceQuestion == null || sourceQuestion.SurveyId != surveyId)
            {
                _logger.LogWarning("Source question {QuestionId} not found or doesn't belong to survey {SurveyId}",
                    dto.SourceQuestionId, surveyId);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Source question with ID {dto.SourceQuestionId} not found in this survey"
                });
            }

            var targetQuestion = await _questionRepository.GetByIdAsync(dto.TargetQuestionId);
            if (targetQuestion == null || targetQuestion.SurveyId != surveyId)
            {
                _logger.LogWarning("Target question {QuestionId} not found or doesn't belong to survey {SurveyId}",
                    dto.TargetQuestionId, surveyId);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Target question with ID {dto.TargetQuestionId} not found in this survey"
                });
            }

            // 3. Create branching rule entity
            var branchingRule = new QuestionBranchingRule
            {
                SourceQuestionId = dto.SourceQuestionId,
                TargetQuestionId = dto.TargetQuestionId,
                ConditionJson = JsonSerializer.Serialize(dto.Condition),
                CreatedAt = DateTime.UtcNow
            };

            // 4. Validate the rule (business logic validation)
            await _questionService.ValidateBranchingRuleAsync(branchingRule);

            // 5. Create the rule
            var createdRule = await _branchingRuleRepository.CreateAsync(branchingRule);

            // 6. Map to DTO
            var ruleDto = new BranchingRuleDto
            {
                Id = createdRule.Id,
                SourceQuestionId = createdRule.SourceQuestionId,
                TargetQuestionId = createdRule.TargetQuestionId,
                Condition = JsonSerializer.Deserialize<BranchingConditionDto>(createdRule.ConditionJson)!,
                CreatedAt = createdRule.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetBranchingRule),
                new { surveyId, sourceQuestionId, targetQuestionId = createdRule.TargetQuestionId },
                ApiResponse<BranchingRuleDto>.Ok(ruleDto, "Branching rule created successfully"));
        }
        catch (QuestionValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating branching rule");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating branching rule");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while creating the branching rule"
            });
        }
    }

    /// <summary>
    /// Gets all branching rules from a source question.
    /// </summary>
    /// <param name="surveyId">Survey ID.</param>
    /// <param name="sourceQuestionId">Source question ID.</param>
    /// <returns>List of branching rules.</returns>
    /// <response code="200">Successfully retrieved branching rules.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey or question not found.</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "List branching rules for question",
        Description = "Gets all branching rules where the specified question is the source. User must own the survey.",
        Tags = new[] { "Branching Rules" }
    )]
    [ProducesResponseType(typeof(ApiResponse<List<BranchingRuleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<BranchingRuleDto>>>> GetBranchingRules(
        int surveyId,
        int sourceQuestionId)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Getting branching rules for question {QuestionId} in survey {SurveyId}",
                sourceQuestionId, surveyId);

            // Verify survey exists and user owns it
            var survey = await _surveyRepository.GetByIdAsync(surveyId);
            if (survey == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Survey with ID {surveyId} not found"
                });
            }

            if (survey.CreatorId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
                {
                    Success = false,
                    Message = "You don't have permission to view this survey's branching rules"
                });
            }

            // Get all rules where this is the source
            var rules = await _branchingRuleRepository.GetBySourceQuestionAsync(sourceQuestionId);

            var ruleDtos = rules.Select(r => new BranchingRuleDto
            {
                Id = r.Id,
                SourceQuestionId = r.SourceQuestionId,
                TargetQuestionId = r.TargetQuestionId,
                Condition = JsonSerializer.Deserialize<BranchingConditionDto>(r.ConditionJson)!,
                CreatedAt = r.CreatedAt
            }).ToList();

            return Ok(ApiResponse<List<BranchingRuleDto>>.Ok(ruleDtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branching rules");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving branching rules"
            });
        }
    }

    /// <summary>
    /// Gets a specific branching rule by source and target question IDs.
    /// </summary>
    /// <param name="surveyId">Survey ID.</param>
    /// <param name="sourceQuestionId">Source question ID.</param>
    /// <param name="targetQuestionId">Target question ID.</param>
    /// <returns>The branching rule.</returns>
    /// <response code="200">Successfully retrieved branching rule.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey, question, or branching rule not found.</response>
    [HttpGet("{targetQuestionId}")]
    [SwaggerOperation(
        Summary = "Get specific branching rule",
        Description = "Gets a specific branching rule by source and target question IDs. User must own the survey.",
        Tags = new[] { "Branching Rules" }
    )]
    [ProducesResponseType(typeof(ApiResponse<BranchingRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BranchingRuleDto>>> GetBranchingRule(
        int surveyId,
        int sourceQuestionId,
        int targetQuestionId)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation(
                "Getting branching rule from question {SourceQuestionId} to {TargetQuestionId} in survey {SurveyId}",
                sourceQuestionId, targetQuestionId, surveyId);

            // Verify survey exists and user owns it
            var survey = await _surveyRepository.GetByIdAsync(surveyId);
            if (survey == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Survey with ID {surveyId} not found"
                });
            }

            if (survey.CreatorId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
                {
                    Success = false,
                    Message = "You don't have permission to view this survey's branching rules"
                });
            }

            // Get the specific rule
            var rule = await _branchingRuleRepository.GetBySourceAndTargetAsync(sourceQuestionId, targetQuestionId);
            if (rule == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"No branching rule found from question {sourceQuestionId} to {targetQuestionId}"
                });
            }

            var ruleDto = new BranchingRuleDto
            {
                Id = rule.Id,
                SourceQuestionId = rule.SourceQuestionId,
                TargetQuestionId = rule.TargetQuestionId,
                Condition = JsonSerializer.Deserialize<BranchingConditionDto>(rule.ConditionJson)!,
                CreatedAt = rule.CreatedAt
            };

            return Ok(ApiResponse<BranchingRuleDto>.Ok(ruleDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branching rule");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving the branching rule"
            });
        }
    }

    /// <summary>
    /// Updates an existing branching rule.
    /// </summary>
    /// <param name="surveyId">Survey ID.</param>
    /// <param name="sourceQuestionId">Source question ID.</param>
    /// <param name="targetQuestionId">Current target question ID.</param>
    /// <param name="dto">Updated branching rule data.</param>
    /// <returns>The updated branching rule.</returns>
    /// <response code="200">Branching rule updated successfully.</response>
    /// <response code="400">Invalid request data or validation failure.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey, question, or branching rule not found.</response>
    [HttpPut("{targetQuestionId}")]
    [SwaggerOperation(
        Summary = "Update branching rule",
        Description = "Updates an existing branching rule's target question and/or condition. User must own the survey.",
        Tags = new[] { "Branching Rules" }
    )]
    [ProducesResponseType(typeof(ApiResponse<BranchingRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BranchingRuleDto>>> UpdateBranchingRule(
        int surveyId,
        int sourceQuestionId,
        int targetQuestionId,
        [FromBody] UpdateBranchingRuleDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for branching rule update");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Data = ModelState
                });
            }

            var userId = GetUserIdFromClaims();
            _logger.LogInformation(
                "Updating branching rule from question {SourceQuestionId} to {TargetQuestionId} in survey {SurveyId}",
                sourceQuestionId, targetQuestionId, surveyId);

            // Verify survey exists and user owns it
            var survey = await _surveyRepository.GetByIdAsync(surveyId);
            if (survey == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Survey with ID {surveyId} not found"
                });
            }

            if (survey.CreatorId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
                {
                    Success = false,
                    Message = "You don't have permission to modify this survey"
                });
            }

            // Get existing rule
            var existingRule = await _branchingRuleRepository.GetBySourceAndTargetAsync(sourceQuestionId, targetQuestionId);
            if (existingRule == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"No branching rule found from question {sourceQuestionId} to {targetQuestionId}"
                });
            }

            // Validate new target question exists and belongs to survey
            var newTargetQuestion = await _questionRepository.GetByIdAsync(dto.TargetQuestionId);
            if (newTargetQuestion == null || newTargetQuestion.SurveyId != surveyId)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Target question with ID {dto.TargetQuestionId} not found in this survey"
                });
            }

            // Update rule
            existingRule.TargetQuestionId = dto.TargetQuestionId;
            existingRule.ConditionJson = JsonSerializer.Serialize(dto.Condition);

            // Validate updated rule
            await _questionService.ValidateBranchingRuleAsync(existingRule);

            // Save changes
            var updatedRule = await _branchingRuleRepository.UpdateAsync(existingRule);

            var ruleDto = new BranchingRuleDto
            {
                Id = updatedRule.Id,
                SourceQuestionId = updatedRule.SourceQuestionId,
                TargetQuestionId = updatedRule.TargetQuestionId,
                Condition = JsonSerializer.Deserialize<BranchingConditionDto>(updatedRule.ConditionJson)!,
                CreatedAt = updatedRule.CreatedAt
            };

            return Ok(ApiResponse<BranchingRuleDto>.Ok(ruleDto, "Branching rule updated successfully"));
        }
        catch (QuestionValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating branching rule");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating branching rule");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while updating the branching rule"
            });
        }
    }

    /// <summary>
    /// Deletes a branching rule.
    /// </summary>
    /// <param name="surveyId">Survey ID.</param>
    /// <param name="sourceQuestionId">Source question ID.</param>
    /// <param name="targetQuestionId">Target question ID.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Branching rule deleted successfully.</response>
    /// <response code="401">Unauthorized - invalid or missing token.</response>
    /// <response code="403">Forbidden - user doesn't own this survey.</response>
    /// <response code="404">Survey, question, or branching rule not found.</response>
    [HttpDelete("{targetQuestionId}")]
    [SwaggerOperation(
        Summary = "Delete branching rule",
        Description = "Deletes a branching rule. User must own the survey.",
        Tags = new[] { "Branching Rules" }
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBranchingRule(
        int surveyId,
        int sourceQuestionId,
        int targetQuestionId)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation(
                "Deleting branching rule from question {SourceQuestionId} to {TargetQuestionId} in survey {SurveyId}",
                sourceQuestionId, targetQuestionId, surveyId);

            // Verify survey exists and user owns it
            var survey = await _surveyRepository.GetByIdAsync(surveyId);
            if (survey == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Survey with ID {surveyId} not found"
                });
            }

            if (survey.CreatorId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
                {
                    Success = false,
                    Message = "You don't have permission to modify this survey"
                });
            }

            // Get the rule
            var rule = await _branchingRuleRepository.GetBySourceAndTargetAsync(sourceQuestionId, targetQuestionId);
            if (rule == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"No branching rule found from question {sourceQuestionId} to {targetQuestionId}"
                });
            }

            // Delete the rule
            await _branchingRuleRepository.DeleteAsync(rule.Id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting branching rule");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting the branching rule"
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
