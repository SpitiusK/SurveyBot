using SurveyBot.Core.Entities;

namespace SurveyBot.Tests.Fixtures;

/// <summary>
/// Builder for creating test entities with valid default values.
/// </summary>
public static class EntityBuilder
{
    /// <summary>
    /// Creates a valid test user.
    /// </summary>
    public static User CreateUser(
        long telegramId = 123456789,
        string? username = "testuser",
        string? firstName = "Test",
        string? lastName = "User")
    {
        return new User
        {
            TelegramId = telegramId,
            Username = username,
            FirstName = firstName,
            LastName = lastName
        };
    }

    /// <summary>
    /// Creates a valid test survey.
    /// </summary>
    public static Survey CreateSurvey(
        string title = "Test Survey",
        string? description = "Test Description",
        int creatorId = 1,
        bool isActive = true)
    {
        return new Survey
        {
            Title = title,
            Description = description,
            CreatorId = creatorId,
            IsActive = isActive,
            AllowMultipleResponses = false,
            ShowResults = true
        };
    }

    /// <summary>
    /// Creates a valid test question.
    /// </summary>
    public static Question CreateQuestion(
        int surveyId = 1,
        string questionText = "Test Question?",
        QuestionType questionType = QuestionType.Text,
        int orderIndex = 0,
        bool isRequired = true)
    {
        return new Question
        {
            SurveyId = surveyId,
            QuestionText = questionText,
            QuestionType = questionType,
            OrderIndex = orderIndex,
            IsRequired = isRequired
        };
    }

    /// <summary>
    /// Creates a valid test response.
    /// </summary>
    public static Response CreateResponse(
        int surveyId = 1,
        long respondentTelegramId = 987654321,
        bool isComplete = false)
    {
        return new Response
        {
            SurveyId = surveyId,
            RespondentTelegramId = respondentTelegramId,
            IsComplete = isComplete,
            StartedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a valid test answer.
    /// </summary>
    public static Answer CreateAnswer(
        int responseId = 1,
        int questionId = 1,
        string answerText = "Test Answer")
    {
        return new Answer
        {
            ResponseId = responseId,
            QuestionId = questionId,
            AnswerText = answerText
        };
    }
}
