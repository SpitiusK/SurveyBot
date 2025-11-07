using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Unit.Entities;

public class EntityValidationTests
{
    private static List<ValidationResult> ValidateEntity<T>(T entity) where T : class
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(entity);
        Validator.TryValidateObject(entity, validationContext, validationResults, validateAllProperties: true);
        return validationResults;
    }

    #region Survey Validation Tests

    [Fact]
    public void Survey_ValidEntity_PassesValidation()
    {
        // Arrange
        var survey = EntityBuilder.CreateSurvey();

        // Act
        var errors = ValidateEntity(survey);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Survey_EmptyTitle_FailsValidation()
    {
        // Arrange
        var survey = EntityBuilder.CreateSurvey(title: "");

        // Act
        var errors = ValidateEntity(survey);

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(Survey.Title)));
    }

    [Fact]
    public void Survey_TitleExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var longTitle = new string('A', 501); // MaxLength is 500
        var survey = EntityBuilder.CreateSurvey(title: longTitle);

        // Act
        var errors = ValidateEntity(survey);

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(Survey.Title)));
    }

    [Fact]
    public void Survey_NullDescription_PassesValidation()
    {
        // Arrange
        var survey = EntityBuilder.CreateSurvey(description: null);

        // Act
        var errors = ValidateEntity(survey);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region Question Validation Tests

    [Fact]
    public void Question_ValidEntity_PassesValidation()
    {
        // Arrange
        var question = EntityBuilder.CreateQuestion();

        // Act
        var errors = ValidateEntity(question);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Question_EmptyQuestionText_FailsValidation()
    {
        // Arrange
        var question = EntityBuilder.CreateQuestion(questionText: "");

        // Act
        var errors = ValidateEntity(question);

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(Question.QuestionText)));
    }

    [Fact]
    public void Question_ValidQuestionType_PassesValidation()
    {
        // Arrange
        var textQuestion = EntityBuilder.CreateQuestion(questionType: QuestionType.Text);
        var singleChoiceQuestion = EntityBuilder.CreateQuestion(questionType: QuestionType.SingleChoice);
        var multipleChoiceQuestion = EntityBuilder.CreateQuestion(questionType: QuestionType.MultipleChoice);

        // Act
        var textErrors = ValidateEntity(textQuestion);
        var singleErrors = ValidateEntity(singleChoiceQuestion);
        var multipleErrors = ValidateEntity(multipleChoiceQuestion);

        // Assert
        textErrors.Should().BeEmpty();
        singleErrors.Should().BeEmpty();
        multipleErrors.Should().BeEmpty();
    }

    [Fact]
    public void Question_NegativeOrderIndex_FailsValidation()
    {
        // Arrange
        var question = EntityBuilder.CreateQuestion(orderIndex: -1);

        // Act
        var errors = ValidateEntity(question);

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(Question.OrderIndex)));
    }

    [Fact]
    public void Question_ValidOrderIndex_PassesValidation()
    {
        // Arrange
        var question = EntityBuilder.CreateQuestion(orderIndex: 0);

        // Act
        var errors = ValidateEntity(question);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region User Validation Tests

    [Fact]
    public void User_ValidEntity_PassesValidation()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();

        // Act
        var errors = ValidateEntity(user);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void User_UsernameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var longUsername = new string('A', 256); // MaxLength is 255
        var user = EntityBuilder.CreateUser(username: longUsername);

        // Act
        var errors = ValidateEntity(user);

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(User.Username)));
    }

    [Fact]
    public void User_FirstNameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var longFirstName = new string('A', 256); // MaxLength is 255
        var user = EntityBuilder.CreateUser(firstName: longFirstName);

        // Act
        var errors = ValidateEntity(user);

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(User.FirstName)));
    }

    [Fact]
    public void User_LastNameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var longLastName = new string('A', 256); // MaxLength is 255
        var user = EntityBuilder.CreateUser(lastName: longLastName);

        // Act
        var errors = ValidateEntity(user);

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(User.LastName)));
    }

    [Fact]
    public void User_NullOptionalFields_PassesValidation()
    {
        // Arrange
        var user = EntityBuilder.CreateUser(username: null, firstName: null, lastName: null);

        // Act
        var errors = ValidateEntity(user);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region Response Validation Tests

    [Fact]
    public void Response_ValidEntity_PassesValidation()
    {
        // Arrange
        var response = EntityBuilder.CreateResponse();

        // Act
        var errors = ValidateEntity(response);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Response_IncompleteResponse_PassesValidation()
    {
        // Arrange
        var response = EntityBuilder.CreateResponse(isComplete: false);

        // Act
        var errors = ValidateEntity(response);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region Answer Validation Tests

    [Fact]
    public void Answer_ValidEntity_PassesValidation()
    {
        // Arrange
        var answer = EntityBuilder.CreateAnswer();

        // Act
        var errors = ValidateEntity(answer);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Answer_EmptyAnswerText_PassesValidation()
    {
        // Arrange - Empty answer text might be valid for some question types
        var answer = EntityBuilder.CreateAnswer(answerText: "");

        // Act
        var errors = ValidateEntity(answer);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region QuestionType Enum Tests

    [Fact]
    public void QuestionType_AllValidValues_AreDefined()
    {
        // Assert
        Enum.IsDefined(typeof(QuestionType), QuestionType.Text).Should().BeTrue();
        Enum.IsDefined(typeof(QuestionType), QuestionType.SingleChoice).Should().BeTrue();
        Enum.IsDefined(typeof(QuestionType), QuestionType.MultipleChoice).Should().BeTrue();
    }

    [Fact]
    public void QuestionType_EnumValues_AreCorrect()
    {
        // Assert
        ((int)QuestionType.Text).Should().Be(0);
        ((int)QuestionType.SingleChoice).Should().Be(1);
        ((int)QuestionType.MultipleChoice).Should().Be(2);
    }

    #endregion

    #region BaseEntity Tests

    [Fact]
    public void BaseEntity_InitialValues_AreCorrect()
    {
        // Arrange & Act
        var user = new User { TelegramId = 12345 };

        // Assert
        user.Id.Should().Be(0); // Default value before saving
        // Note: Timestamps will be set by DbContext when SaveChangesAsync is called
        // This test just verifies the entity can be created
    }

    #endregion
}
