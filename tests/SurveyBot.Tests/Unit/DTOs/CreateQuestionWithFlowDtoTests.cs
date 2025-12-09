using System.ComponentModel.DataAnnotations;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using Xunit;

namespace SurveyBot.Tests.Unit.DTOs;

/// <summary>
/// Unit tests for CreateQuestionWithFlowDto validation.
/// REGRESSION TEST: Verifies fix for Rating question conditional flow validation bug (v1.6.2).
///
/// Bug Fixed: Survey publishing was failing with 400 Bad Request when Rating questions
/// had conditional flow configured. The DTO validation incorrectly restricted
/// optionNextQuestionIndexes to SingleChoice questions only.
///
/// Fix: Updated validation to allow both SingleChoice AND Rating questions to use
/// optionNextQuestionIndexes.
/// </summary>
public class CreateQuestionWithFlowDtoTests
{
    #region Rating Question Validation Tests (Bug Fix v1.6.2)

    [Fact]
    public void Validate_RatingWithOptionNextQuestionIndexes_ShouldBeValid()
    {
        // Arrange: Rating question with conditional flow (the exact bug scenario)
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Rate our service",
            QuestionType = QuestionType.Rating,
            OrderIndex = 0,
            IsRequired = true,
            OptionNextQuestionIndexes = new Dictionary<int, int?>
            {
                { 0, 1 }, // 1 star → Question at index 1
                { 1, 2 }, // 2 stars → Question at index 2
                { 2, 3 }, // 3 stars → Question at index 3
                { 3, 4 }, // 4 stars → Question at index 4
                { 4, 5 }  // 5 stars → Question at index 5
            }
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = dto.Validate(validationContext).ToList();

        // Assert
        Assert.Empty(results); // Should have NO validation errors
    }

    [Fact]
    public void Validate_RatingWithPartialOptionNextQuestionIndexes_ShouldBeValid()
    {
        // Arrange: Rating with only some ratings having conditional flow
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "How satisfied are you?",
            QuestionType = QuestionType.Rating,
            OrderIndex = 0,
            IsRequired = true,
            OptionNextQuestionIndexes = new Dictionary<int, int?>
            {
                { 0, 1 }, // 1 star → Question 1 (feedback)
                { 4, null } // 5 stars → End survey
                // 2-4 stars: No explicit flow (will use DefaultNextQuestionIndex)
            }
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = dto.Validate(validationContext).ToList();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_RatingWithEndSurveyFlow_ShouldBeValid()
    {
        // Arrange: Rating where some ratings end the survey (null)
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Quick rating",
            QuestionType = QuestionType.Rating,
            OrderIndex = 0,
            IsRequired = true,
            OptionNextQuestionIndexes = new Dictionary<int, int?>
            {
                { 0, 1 },    // 1 star → Question 1
                { 1, 1 },    // 2 stars → Question 1
                { 2, null }, // 3 stars → End survey
                { 3, null }, // 4 stars → End survey
                { 4, null }  // 5 stars → End survey
            }
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = dto.Validate(validationContext).ToList();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_RatingWithSequentialFlow_ShouldBeValid()
    {
        // Arrange: Rating with sequential flow (-1 for some options)
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Rate us",
            QuestionType = QuestionType.Rating,
            OrderIndex = 0,
            IsRequired = true,
            OptionNextQuestionIndexes = new Dictionary<int, int?>
            {
                { 0, 1 },  // 1 star → specific question
                { 4, -1 }  // 5 stars → sequential (next by order)
            }
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = dto.Validate(validationContext).ToList();

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region SingleChoice Validation Tests (Regression - ensure still works)

    [Fact]
    public void Validate_SingleChoiceWithOptionNextQuestionIndexes_ShouldBeValid()
    {
        // Arrange: SingleChoice question with conditional flow (should still work)
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Do you like surveys?",
            QuestionType = QuestionType.SingleChoice,
            OrderIndex = 0,
            IsRequired = true,
            Options = new List<string> { "Yes", "No", "Maybe" },
            OptionNextQuestionIndexes = new Dictionary<int, int?>
            {
                { 0, 1 },    // "Yes" → Question 1
                { 1, null }, // "No" → End survey
                { 2, 2 }     // "Maybe" → Question 2
            }
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = dto.Validate(validationContext).ToList();

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region Negative Tests (Invalid Question Types)

    [Fact]
    public void Validate_TextWithOptionNextQuestionIndexes_ShouldBeInvalid()
    {
        // Arrange: Text question with optionNextQuestionIndexes (invalid)
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "What is your name?",
            QuestionType = QuestionType.Text,
            OrderIndex = 0,
            IsRequired = true,
            OptionNextQuestionIndexes = new Dictionary<int, int?>
            {
                { 0, 1 } // Text questions don't have options!
            }
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = dto.Validate(validationContext).ToList();

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r =>
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("SingleChoice and Rating", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_MultipleChoiceWithOptionNextQuestionIndexes_ShouldBeInvalid()
    {
        // Arrange: MultipleChoice with optionNextQuestionIndexes (not supported)
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Select all that apply",
            QuestionType = QuestionType.MultipleChoice,
            OrderIndex = 0,
            IsRequired = true,
            Options = new List<string> { "A", "B", "C" },
            OptionNextQuestionIndexes = new Dictionary<int, int?>
            {
                { 0, 1 } // MultipleChoice doesn't support branching
            }
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = dto.Validate(validationContext).ToList();

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r =>
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("SingleChoice and Rating", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_NumberWithOptionNextQuestionIndexes_ShouldBeInvalid()
    {
        // Arrange: Number question with optionNextQuestionIndexes (invalid)
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Enter a number",
            QuestionType = QuestionType.Number,
            OrderIndex = 0,
            IsRequired = true,
            OptionNextQuestionIndexes = new Dictionary<int, int?>
            {
                { 0, 1 } // Number questions don't have options
            }
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = dto.Validate(validationContext).ToList();

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void Validate_DateWithOptionNextQuestionIndexes_ShouldBeInvalid()
    {
        // Arrange: Date question with optionNextQuestionIndexes (invalid)
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Enter a date",
            QuestionType = QuestionType.Date,
            OrderIndex = 0,
            IsRequired = true,
            OptionNextQuestionIndexes = new Dictionary<int, int?>
            {
                { 0, 1 } // Date questions don't have options
            }
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = dto.Validate(validationContext).ToList();

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void Validate_LocationWithOptionNextQuestionIndexes_ShouldBeInvalid()
    {
        // Arrange: Location question with optionNextQuestionIndexes (invalid)
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Share your location",
            QuestionType = QuestionType.Location,
            OrderIndex = 0,
            IsRequired = true,
            OptionNextQuestionIndexes = new Dictionary<int, int?>
            {
                { 0, 1 } // Location questions don't have options
            }
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = dto.Validate(validationContext).ToList();

        // Assert
        Assert.NotEmpty(results);
    }

    #endregion

    #region SupportsBranching Property Tests

    [Fact]
    public void SupportsBranching_Rating_ShouldReturnTrue()
    {
        // Arrange
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Rate us",
            QuestionType = QuestionType.Rating,
            OrderIndex = 0
        };

        // Act
        var supportsBranching = dto.SupportsBranching;

        // Assert
        Assert.True(supportsBranching);
    }

    [Fact]
    public void SupportsBranching_SingleChoice_ShouldReturnTrue()
    {
        // Arrange
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Choose one",
            QuestionType = QuestionType.SingleChoice,
            OrderIndex = 0
        };

        // Act
        var supportsBranching = dto.SupportsBranching;

        // Assert
        Assert.True(supportsBranching);
    }

    [Fact]
    public void SupportsBranching_Text_ShouldReturnFalse()
    {
        // Arrange
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Enter text",
            QuestionType = QuestionType.Text,
            OrderIndex = 0
        };

        // Act
        var supportsBranching = dto.SupportsBranching;

        // Assert
        Assert.False(supportsBranching);
    }

    [Fact]
    public void SupportsBranching_MultipleChoice_ShouldReturnFalse()
    {
        // Arrange
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Select all",
            QuestionType = QuestionType.MultipleChoice,
            OrderIndex = 0
        };

        // Act
        var supportsBranching = dto.SupportsBranching;

        // Assert
        Assert.False(supportsBranching);
    }

    [Fact]
    public void SupportsBranching_Number_ShouldReturnFalse()
    {
        // Arrange
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Enter number",
            QuestionType = QuestionType.Number,
            OrderIndex = 0
        };

        // Act
        var supportsBranching = dto.SupportsBranching;

        // Assert
        Assert.False(supportsBranching);
    }

    [Fact]
    public void SupportsBranching_Date_ShouldReturnFalse()
    {
        // Arrange
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Enter date",
            QuestionType = QuestionType.Date,
            OrderIndex = 0
        };

        // Act
        var supportsBranching = dto.SupportsBranching;

        // Assert
        Assert.False(supportsBranching);
    }

    [Fact]
    public void SupportsBranching_Location_ShouldReturnFalse()
    {
        // Arrange
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Share location",
            QuestionType = QuestionType.Location,
            OrderIndex = 0
        };

        // Act
        var supportsBranching = dto.SupportsBranching;

        // Assert
        Assert.False(supportsBranching);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_RatingWithInvalidOptionIndex_ShouldBeInvalid()
    {
        // Arrange: Rating has only 5 options (0-4), but referencing index 5
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Rate us",
            QuestionType = QuestionType.Rating,
            OrderIndex = 0,
            IsRequired = true,
            Options = new List<string> { "1", "2", "3", "4", "5" },
            OptionNextQuestionIndexes = new Dictionary<int, int?>
            {
                { 5, 1 } // Invalid: Only indexes 0-4 are valid for 5 options
            }
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = dto.Validate(validationContext).ToList();

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r =>
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("Invalid option index", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RatingWithEmptyOptionNextQuestionIndexes_ShouldBeValid()
    {
        // Arrange: Rating with empty flow configuration (valid - will use DefaultNextQuestionIndex)
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Rate us",
            QuestionType = QuestionType.Rating,
            OrderIndex = 0,
            IsRequired = true,
            OptionNextQuestionIndexes = new Dictionary<int, int?>()
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = dto.Validate(validationContext).ToList();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_RatingWithNullOptionNextQuestionIndexes_ShouldBeValid()
    {
        // Arrange: Rating with null flow configuration (valid)
        var dto = new CreateQuestionWithFlowDto
        {
            QuestionText = "Rate us",
            QuestionType = QuestionType.Rating,
            OrderIndex = 0,
            IsRequired = true,
            OptionNextQuestionIndexes = null
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = dto.Validate(validationContext).ToList();

        // Assert
        Assert.Empty(results);
    }

    #endregion
}
