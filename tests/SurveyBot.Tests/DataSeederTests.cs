using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SurveyBot.Core.Entities;
using SurveyBot.Infrastructure.Data;
using Xunit;
using Xunit.Abstractions;

namespace SurveyBot.Infrastructure.Tests;

/// <summary>
/// Tests for the DataSeeder class.
/// </summary>
public class DataSeederTests : IDisposable
{
    private readonly SurveyBotDbContext _context;
    private readonly ILogger<DataSeeder> _logger;
    private readonly ITestOutputHelper _output;

    public DataSeederTests(ITestOutputHelper output)
    {
        _output = output;

        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new SurveyBotDbContext(options);

        // Create logger
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        _logger = loggerFactory.CreateLogger<DataSeeder>();
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateUsers()
    {
        // Arrange
        var seeder = new DataSeeder(_context, _logger);

        // Act
        await seeder.SeedAsync();

        // Assert
        var users = await _context.Users.ToListAsync();
        Assert.NotEmpty(users);
        Assert.Equal(3, users.Count);
        Assert.Contains(users, u => u.Username == "john_doe");
        Assert.Contains(users, u => u.Username == "jane_smith");
        Assert.Contains(users, u => u.Username == "test_user");

        _output.WriteLine($"Seeded {users.Count} users");
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateSurveysWithQuestions()
    {
        // Arrange
        var seeder = new DataSeeder(_context, _logger);

        // Act
        await seeder.SeedAsync();

        // Assert
        var surveys = await _context.Surveys
            .Include(s => s.Questions)
            .ToListAsync();

        Assert.NotEmpty(surveys);
        Assert.Equal(3, surveys.Count);

        // Verify each survey has questions
        foreach (var survey in surveys)
        {
            Assert.NotEmpty(survey.Questions);
            _output.WriteLine($"Survey: {survey.Title} - {survey.Questions.Count} questions");
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateAllQuestionTypes()
    {
        // Arrange
        var seeder = new DataSeeder(_context, _logger);

        // Act
        await seeder.SeedAsync();

        // Assert
        var questions = await _context.Questions.ToListAsync();

        // Check that all question types are represented
        Assert.Contains(questions, q => q.QuestionType == QuestionType.Text);
        Assert.Contains(questions, q => q.QuestionType == QuestionType.SingleChoice);
        Assert.Contains(questions, q => q.QuestionType == QuestionType.MultipleChoice);
        Assert.Contains(questions, q => q.QuestionType == QuestionType.Rating);

        _output.WriteLine($"Total questions seeded: {questions.Count}");
        foreach (var questionType in Enum.GetValues<QuestionType>())
        {
            var count = questions.Count(q => q.QuestionType == questionType);
            _output.WriteLine($"  {questionType}: {count}");
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateResponses()
    {
        // Arrange
        var seeder = new DataSeeder(_context, _logger);

        // Act
        await seeder.SeedAsync();

        // Assert
        var responses = await _context.Responses
            .Include(r => r.Answers)
            .ToListAsync();

        Assert.NotEmpty(responses);
        Assert.Equal(5, responses.Count);

        // Verify complete and incomplete responses
        var completeResponses = responses.Where(r => r.IsComplete).ToList();
        var incompleteResponses = responses.Where(r => !r.IsComplete).ToList();

        Assert.Equal(4, completeResponses.Count);
        Assert.Single(incompleteResponses);

        _output.WriteLine($"Complete responses: {completeResponses.Count}");
        _output.WriteLine($"Incomplete responses: {incompleteResponses.Count}");
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateAnswersForAllQuestionTypes()
    {
        // Arrange
        var seeder = new DataSeeder(_context, _logger);

        // Act
        await seeder.SeedAsync();

        // Assert
        var answers = await _context.Answers
            .Include(a => a.Question)
            .ToListAsync();

        Assert.NotEmpty(answers);

        // Check answers for each question type
        var textAnswers = answers.Where(a => a.Question.QuestionType == QuestionType.Text).ToList();
        var singleChoiceAnswers = answers.Where(a => a.Question.QuestionType == QuestionType.SingleChoice).ToList();
        var multipleChoiceAnswers = answers.Where(a => a.Question.QuestionType == QuestionType.MultipleChoice).ToList();
        var ratingAnswers = answers.Where(a => a.Question.QuestionType == QuestionType.Rating).ToList();

        Assert.NotEmpty(textAnswers);
        Assert.NotEmpty(singleChoiceAnswers);
        Assert.NotEmpty(multipleChoiceAnswers);
        Assert.NotEmpty(ratingAnswers);

        _output.WriteLine($"Total answers: {answers.Count}");
        _output.WriteLine($"  Text answers: {textAnswers.Count}");
        _output.WriteLine($"  Single choice answers: {singleChoiceAnswers.Count}");
        _output.WriteLine($"  Multiple choice answers: {multipleChoiceAnswers.Count}");
        _output.WriteLine($"  Rating answers: {ratingAnswers.Count}");
    }

    [Fact]
    public async Task SeedAsync_ShouldNotDuplicateDataOnSecondRun()
    {
        // Arrange
        var seeder = new DataSeeder(_context, _logger);

        // Act
        await seeder.SeedAsync();
        var firstUserCount = await _context.Users.CountAsync();

        // Run again
        await seeder.SeedAsync();
        var secondUserCount = await _context.Users.CountAsync();

        // Assert
        Assert.Equal(firstUserCount, secondUserCount);
        _output.WriteLine($"User count after first seed: {firstUserCount}");
        _output.WriteLine($"User count after second seed: {secondUserCount}");
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateActiveAndInactiveSurveys()
    {
        // Arrange
        var seeder = new DataSeeder(_context, _logger);

        // Act
        await seeder.SeedAsync();

        // Assert
        var activeSurveys = await _context.Surveys.Where(s => s.IsActive).ToListAsync();
        var inactiveSurveys = await _context.Surveys.Where(s => !s.IsActive).ToListAsync();

        Assert.NotEmpty(activeSurveys);
        Assert.NotEmpty(inactiveSurveys);

        _output.WriteLine($"Active surveys: {activeSurveys.Count}");
        _output.WriteLine($"Inactive surveys: {inactiveSurveys.Count}");
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateQuestionsInCorrectOrder()
    {
        // Arrange
        var seeder = new DataSeeder(_context, _logger);

        // Act
        await seeder.SeedAsync();

        // Assert
        var surveys = await _context.Surveys
            .Include(s => s.Questions)
            .ToListAsync();

        foreach (var survey in surveys)
        {
            var orderedQuestions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

            // Verify OrderIndex is sequential starting from 0
            for (int i = 0; i < orderedQuestions.Count; i++)
            {
                Assert.Equal(i, orderedQuestions[i].OrderIndex);
            }

            _output.WriteLine($"Survey '{survey.Title}' has {orderedQuestions.Count} questions in correct order");
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
