using SurveyBot.Core.Entities;
using SurveyBot.Core.ValueObjects;

namespace SurveyBot.Tests.Fixtures;

/// <summary>
/// Builder for creating test entities with valid default values.
/// Uses factory methods following DDD pattern with private constructors.
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
        return User.Create(telegramId, username, firstName, lastName);
    }

    /// <summary>
    /// Creates a valid test survey.
    /// </summary>
    public static Survey CreateSurvey(
        string title = "Test Survey",
        string? description = "Test Description",
        int creatorId = 1,
        bool isActive = false,
        bool allowMultipleResponses = false,
        bool showResults = true)
    {
        return Survey.Create(
            title,
            creatorId,
            description,
            code: null,
            isActive: isActive,
            allowMultipleResponses: allowMultipleResponses,
            showResults: showResults);
    }

    /// <summary>
    /// Creates a valid test question.
    /// </summary>
    public static Question CreateQuestion(
        int surveyId = 1,
        string questionText = "Test Question?",
        QuestionType questionType = QuestionType.Text,
        int orderIndex = 0,
        bool isRequired = true,
        string? optionsJson = null,
        string? mediaContent = null,
        NextQuestionDeterminant? defaultNext = null)
    {
        return Question.Create(
            surveyId,
            questionText,
            questionType,
            orderIndex,
            isRequired,
            optionsJson,
            mediaContent,
            defaultNext);
    }

    /// <summary>
    /// Creates a valid test text question.
    /// </summary>
    public static Question CreateTextQuestion(
        int surveyId = 1,
        string questionText = "Test Text Question?",
        int orderIndex = 0,
        bool isRequired = true)
    {
        return Question.CreateTextQuestion(surveyId, questionText, orderIndex, isRequired);
    }

    /// <summary>
    /// Creates a valid test single choice question.
    /// </summary>
    public static Question CreateSingleChoiceQuestion(
        int surveyId = 1,
        string questionText = "Test Single Choice?",
        int orderIndex = 0,
        string optionsJson = "[\"Option 1\", \"Option 2\", \"Option 3\"]",
        bool isRequired = true)
    {
        return Question.CreateSingleChoiceQuestion(surveyId, questionText, orderIndex, optionsJson, isRequired);
    }

    /// <summary>
    /// Creates a valid test multiple choice question.
    /// </summary>
    public static Question CreateMultipleChoiceQuestion(
        int surveyId = 1,
        string questionText = "Test Multiple Choice?",
        int orderIndex = 0,
        string optionsJson = "[\"Option A\", \"Option B\", \"Option C\"]",
        bool isRequired = true)
    {
        return Question.CreateMultipleChoiceQuestion(surveyId, questionText, orderIndex, optionsJson, isRequired);
    }

    /// <summary>
    /// Creates a valid test rating question.
    /// </summary>
    public static Question CreateRatingQuestion(
        int surveyId = 1,
        string questionText = "Rate this?",
        int orderIndex = 0,
        bool isRequired = true)
    {
        return Question.CreateRatingQuestion(surveyId, questionText, orderIndex, isRequired);
    }

    /// <summary>
    /// Creates a valid test location question.
    /// </summary>
    public static Question CreateLocationQuestion(
        int surveyId = 1,
        string questionText = "Where are you located?",
        int orderIndex = 0,
        bool isRequired = true)
    {
        return Question.Create(
            surveyId,
            questionText,
            QuestionType.Location,
            orderIndex,
            isRequired,
            optionsJson: null,
            mediaContent: null,
            defaultNext: null);
    }

    /// <summary>
    /// Creates a valid test response.
    /// </summary>
    public static Response CreateResponse(
        int surveyId = 1,
        long respondentTelegramId = 987654321,
        bool isComplete = false,
        DateTime? startedAt = null,
        DateTime? submittedAt = null)
    {
        if (isComplete && submittedAt == null)
        {
            submittedAt = DateTime.UtcNow;
        }

        return Response.Create(
            surveyId,
            respondentTelegramId,
            startedAt ?? DateTime.UtcNow,
            isComplete,
            isComplete ? submittedAt : null);
    }

    /// <summary>
    /// Creates a started test response (not complete).
    /// </summary>
    public static Response StartResponse(
        int surveyId = 1,
        long respondentTelegramId = 987654321)
    {
        return Response.Start(surveyId, respondentTelegramId);
    }

    /// <summary>
    /// Creates a valid test answer with text.
    /// </summary>
    public static Answer CreateTextAnswer(
        int responseId = 1,
        int questionId = 1,
        string? answerText = "Test Answer",
        NextQuestionDeterminant? next = null)
    {
        return Answer.CreateTextAnswer(responseId, questionId, answerText, next);
    }

    /// <summary>
    /// Creates a valid test answer with JSON.
    /// </summary>
    public static Answer CreateJsonAnswer(
        int responseId = 1,
        int questionId = 1,
        string? answerJson = "{\"value\": \"test\"}",
        NextQuestionDeterminant? next = null)
    {
        return Answer.CreateJsonAnswer(responseId, questionId, answerJson, next);
    }

    /// <summary>
    /// Creates a valid test answer (backward-compatible).
    /// </summary>
    public static Answer CreateAnswer(
        int responseId = 1,
        int questionId = 1,
        string? answerText = "Test Answer",
        string? answerJson = null,
        NextQuestionDeterminant? next = null)
    {
        return Answer.Create(responseId, questionId, answerText, answerJson, next);
    }

    /// <summary>
    /// Creates a valid test question option.
    /// </summary>
    public static QuestionOption CreateQuestionOption(
        int questionId = 1,
        string text = "Option 1",
        int orderIndex = 0,
        NextQuestionDeterminant? next = null)
    {
        return QuestionOption.Create(questionId, text, orderIndex, next);
    }

    /// <summary>
    /// Creates a test question option that ends the survey when selected.
    /// </summary>
    public static QuestionOption CreateEndSurveyOption(
        int questionId = 1,
        string text = "End Survey",
        int orderIndex = 0)
    {
        return QuestionOption.CreateWithEndSurvey(questionId, text, orderIndex);
    }

    /// <summary>
    /// Creates a test question option that navigates to a specific question.
    /// </summary>
    public static QuestionOption CreateNextQuestionOption(
        int questionId = 1,
        string text = "Next",
        int orderIndex = 0,
        int nextQuestionId = 2)
    {
        return QuestionOption.CreateWithNextQuestion(questionId, text, orderIndex, nextQuestionId);
    }
}
