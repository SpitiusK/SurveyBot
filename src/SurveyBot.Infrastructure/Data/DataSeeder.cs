using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SurveyBot.Core.Entities;
using System.Text.Json;

namespace SurveyBot.Infrastructure.Data;

/// <summary>
/// Seeds the database with development and testing data.
/// </summary>
public class DataSeeder
{
    private readonly SurveyBotDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(SurveyBotDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all development data. Safe to call multiple times - will skip if data already exists.
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Check if data already exists
            if (await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Database already contains data. Skipping seed.");
                return;
            }

            _logger.LogInformation("Starting database seeding...");

            // Seed in order due to foreign key relationships
            await SeedUsersAsync();
            await SeedSurveysAsync();
            await SeedResponsesAsync();

            _logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    /// <summary>
    /// Seeds sample users.
    /// </summary>
    private async Task SeedUsersAsync()
    {
        var user1 = User.Create(123456789, "john_doe", "John", "Doe");
        var user2 = User.Create(987654321, "jane_smith", "Jane", "Smith");
        var user3 = User.Create(555555555, "test_user", "Test", "User");

        var users = new List<User> { user1, user2, user3 };

        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} users.", users.Count);
    }

    /// <summary>
    /// Seeds sample surveys with questions covering all question types.
    /// </summary>
    private async Task SeedSurveysAsync()
    {
        // Get the first user to be the creator
        var creator = await _context.Users.FirstAsync();

        // Create and save surveys first (without questions)
        var customerSurvey = CreateCustomerSatisfactionSurvey(creator.Id);
        var productSurvey = CreateProductFeedbackSurvey(creator.Id);
        var eventSurvey = CreateEventRegistrationSurvey(creator.Id);

        _context.Surveys.AddRange(new[] { customerSurvey, productSurvey, eventSurvey });
        await _context.SaveChangesAsync();

        // Now create questions for each survey
        CreateCustomerSatisfactionQuestions(customerSurvey.Id);
        CreateProductFeedbackQuestions(productSurvey.Id);
        CreateEventRegistrationQuestions(eventSurvey.Id);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded 3 surveys with questions.");
    }

    /// <summary>
    /// Creates a customer satisfaction survey (without questions).
    /// </summary>
    private Survey CreateCustomerSatisfactionSurvey(int creatorId)
    {
        return Survey.Create(
            "Customer Satisfaction Survey",
            creatorId,
            "Help us improve our service by sharing your feedback!",
            code: null,
            isActive: true,
            allowMultipleResponses: false,
            showResults: true);
    }

    /// <summary>
    /// Creates questions for customer satisfaction survey.
    /// </summary>
    private void CreateCustomerSatisfactionQuestions(int surveyId)
    {
        var q1 = Question.Create(
            surveyId,
            "How would you rate our service overall?",
            QuestionType.Rating,
            orderIndex: 0,
            isRequired: true,
            optionsJson: JsonSerializer.Serialize(new
            {
                MinValue = 1,
                MaxValue = 5,
                MinLabel = "Poor",
                MaxLabel = "Excellent"
            }));

        var q2 = Question.CreateMultipleChoiceQuestion(
            surveyId,
            "Which of our services have you used?",
            orderIndex: 1,
            optionsJson: JsonSerializer.Serialize(new[]
            {
                "Online Support",
                "Phone Support",
                "In-Person Service",
                "Mobile App",
                "Website"
            }),
            isRequired: true);

        var q3 = Question.CreateSingleChoiceQuestion(
            surveyId,
            "How did you hear about us?",
            orderIndex: 2,
            optionsJson: JsonSerializer.Serialize(new[]
            {
                "Social Media",
                "Search Engine",
                "Friend Referral",
                "Advertisement",
                "Other"
            }),
            isRequired: true);

        var q4 = Question.CreateTextQuestion(
            surveyId,
            "Please share any additional feedback or suggestions:",
            orderIndex: 3,
            isRequired: false);

        _context.Questions.AddRange(new[] { q1, q2, q3, q4 });
    }

    /// <summary>
    /// Creates a product feedback survey (without questions).
    /// </summary>
    private Survey CreateProductFeedbackSurvey(int creatorId)
    {
        return Survey.Create(
            "New Product Feature Feedback",
            creatorId,
            "We're developing new features and would love your input!",
            code: null,
            isActive: true,
            allowMultipleResponses: true,
            showResults: false);
    }

    /// <summary>
    /// Creates questions for product feedback survey.
    /// </summary>
    private void CreateProductFeedbackQuestions(int surveyId)
    {
        var q1 = Question.CreateSingleChoiceQuestion(
            surveyId,
            "What is your primary use case?",
            orderIndex: 0,
            optionsJson: JsonSerializer.Serialize(new[]
            {
                "Personal Use",
                "Small Business",
                "Enterprise",
                "Education",
                "Non-Profit"
            }),
            isRequired: true);

        var q2 = Question.Create(
            surveyId,
            "How easy is the product to use?",
            QuestionType.Rating,
            orderIndex: 1,
            isRequired: true,
            optionsJson: JsonSerializer.Serialize(new
            {
                MinValue = 1,
                MaxValue = 5,
                MinLabel = "Very Difficult",
                MaxLabel = "Very Easy"
            }));

        var q3 = Question.CreateMultipleChoiceQuestion(
            surveyId,
            "Which features do you find most valuable?",
            orderIndex: 2,
            optionsJson: JsonSerializer.Serialize(new[]
            {
                "Real-time Analytics",
                "Custom Reports",
                "Mobile Access",
                "Integration Support",
                "Automation Tools"
            }),
            isRequired: true);

        var q4 = Question.CreateTextQuestion(
            surveyId,
            "Describe a feature you wish we had:",
            orderIndex: 3,
            isRequired: false);

        _context.Questions.AddRange(new[] { q1, q2, q3, q4 });
    }

    /// <summary>
    /// Creates an event registration survey (without questions).
    /// </summary>
    private Survey CreateEventRegistrationSurvey(int creatorId)
    {
        return Survey.Create(
            "Tech Conference 2025 - Registration",
            creatorId,
            "Register for our upcoming tech conference and let us know your preferences!",
            code: null,
            isActive: false, // Inactive survey for testing
            allowMultipleResponses: false,
            showResults: true);
    }

    /// <summary>
    /// Creates questions for event registration survey.
    /// </summary>
    private void CreateEventRegistrationQuestions(int surveyId)
    {
        var q1 = Question.CreateTextQuestion(
            surveyId,
            "What is your full name?",
            orderIndex: 0,
            isRequired: true);

        var q2 = Question.CreateSingleChoiceQuestion(
            surveyId,
            "Which track are you most interested in?",
            orderIndex: 1,
            optionsJson: JsonSerializer.Serialize(new[]
            {
                "Web Development",
                "Mobile Development",
                "DevOps & Cloud",
                "Data Science & AI",
                "Security"
            }),
            isRequired: true);

        var q3 = Question.CreateMultipleChoiceQuestion(
            surveyId,
            "Which workshops would you like to attend?",
            orderIndex: 2,
            optionsJson: JsonSerializer.Serialize(new[]
            {
                "Docker & Kubernetes",
                "React Advanced Patterns",
                "Machine Learning 101",
                "Microservices Architecture",
                "Testing Best Practices"
            }),
            isRequired: false);

        var q4 = Question.Create(
            surveyId,
            "How would you rate your technical expertise?",
            QuestionType.Rating,
            orderIndex: 3,
            isRequired: true,
            optionsJson: JsonSerializer.Serialize(new
            {
                MinValue = 1,
                MaxValue = 5,
                MinLabel = "Beginner",
                MaxLabel = "Expert"
            }));

        _context.Questions.AddRange(new[] { q1, q2, q3, q4 });
    }

    /// <summary>
    /// Seeds sample responses with answers covering all question types.
    /// </summary>
    private async Task SeedResponsesAsync()
    {
        // Get surveys and users
        var surveys = await _context.Surveys
            .Include(s => s.Questions)
            .ToListAsync();

        var users = await _context.Users.ToListAsync();

        // Create responses for the customer satisfaction survey
        var customerSurvey = surveys.First(s => s.Title == "Customer Satisfaction Survey");
        CreateCustomerSatisfactionResponse1(customerSurvey, users[1].TelegramId);
        CreateCustomerSatisfactionResponse2(customerSurvey, users[2].TelegramId);

        // Create responses for the product feedback survey
        var productSurvey = surveys.First(s => s.Title == "New Product Feature Feedback");
        CreateProductFeedbackResponse1(productSurvey, users[0].TelegramId);
        CreateProductFeedbackResponse2(productSurvey, users[1].TelegramId);

        // Create an incomplete response for testing
        var incompleteSurvey = surveys.First(s => s.Title == "Tech Conference 2025 - Registration");
        CreateIncompleteResponse(incompleteSurvey, users[2].TelegramId);

        // Save all answers
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded 5 responses with answers.");
    }

    /// <summary>
    /// Creates a complete customer satisfaction response (positive feedback).
    /// </summary>
    private Response CreateCustomerSatisfactionResponse1(Survey survey, long telegramId)
    {
        var startTime = DateTime.UtcNow.AddDays(-2);
        var submitTime = startTime.AddMinutes(5);

        var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

        var response = Response.Create(
            survey.Id,
            telegramId,
            startedAt: startTime,
            isComplete: true,
            submittedAt: submitTime);

        _context.Responses.Add(response);
        _context.SaveChanges(); // Save to get response ID

        var a1 = Answer.CreateJsonAnswer(
            response.Id,
            questions[0].Id, // Rating
            JsonSerializer.Serialize(new { Value = 5 }));

        var a2 = Answer.CreateJsonAnswer(
            response.Id,
            questions[1].Id, // Multiple choice
            JsonSerializer.Serialize(new[] { "Online Support", "Mobile App", "Website" }));

        var a3 = Answer.CreateJsonAnswer(
            response.Id,
            questions[2].Id, // Single choice
            JsonSerializer.Serialize(new { SelectedOption = "Friend Referral" }));

        var a4 = Answer.CreateTextAnswer(
            response.Id,
            questions[3].Id, // Text
            "Great service! Very satisfied with the quick response times and helpful support staff.");

        _context.Answers.AddRange(new[] { a1, a2, a3, a4 });

        return response;
    }

    /// <summary>
    /// Creates a complete customer satisfaction response (mixed feedback).
    /// </summary>
    private Response CreateCustomerSatisfactionResponse2(Survey survey, long telegramId)
    {
        var startTime = DateTime.UtcNow.AddDays(-1);
        var submitTime = startTime.AddMinutes(7);

        var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

        var response = Response.Create(
            survey.Id,
            telegramId,
            startedAt: startTime,
            isComplete: true,
            submittedAt: submitTime);

        _context.Responses.Add(response);
        _context.SaveChanges(); // Save to get response ID

        var a1 = Answer.CreateJsonAnswer(
            response.Id,
            questions[0].Id, // Rating
            JsonSerializer.Serialize(new { Value = 3 }));

        var a2 = Answer.CreateJsonAnswer(
            response.Id,
            questions[1].Id, // Multiple choice
            JsonSerializer.Serialize(new[] { "Phone Support", "Website" }));

        var a3 = Answer.CreateJsonAnswer(
            response.Id,
            questions[2].Id, // Single choice
            JsonSerializer.Serialize(new { SelectedOption = "Search Engine" }));

        var a4 = Answer.CreateTextAnswer(
            response.Id,
            questions[3].Id, // Text
            "Service is good but response times could be improved. The website needs better mobile optimization.");

        _context.Answers.AddRange(new[] { a1, a2, a3, a4 });

        return response;
    }

    /// <summary>
    /// Creates a complete product feedback response (power user).
    /// </summary>
    private Response CreateProductFeedbackResponse1(Survey survey, long telegramId)
    {
        var startTime = DateTime.UtcNow.AddDays(-3);
        var submitTime = startTime.AddMinutes(8);

        var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

        var response = Response.Create(
            survey.Id,
            telegramId,
            startedAt: startTime,
            isComplete: true,
            submittedAt: submitTime);

        _context.Responses.Add(response);
        _context.SaveChanges(); // Save to get response ID

        var a1 = Answer.CreateJsonAnswer(
            response.Id,
            questions[0].Id, // Single choice
            JsonSerializer.Serialize(new { SelectedOption = "Enterprise" }));

        var a2 = Answer.CreateJsonAnswer(
            response.Id,
            questions[1].Id, // Rating
            JsonSerializer.Serialize(new { Value = 4 }));

        var a3 = Answer.CreateJsonAnswer(
            response.Id,
            questions[2].Id, // Multiple choice
            JsonSerializer.Serialize(new[]
            {
                "Real-time Analytics",
                "Custom Reports",
                "Integration Support",
                "Automation Tools"
            }));

        var a4 = Answer.CreateTextAnswer(
            response.Id,
            questions[3].Id, // Text
            "Would love to see better API rate limits and support for webhooks. Also, batch operations would be incredibly useful for enterprise deployments.");

        _context.Answers.AddRange(new[] { a1, a2, a3, a4 });

        return response;
    }

    /// <summary>
    /// Creates a complete product feedback response (small business).
    /// </summary>
    private Response CreateProductFeedbackResponse2(Survey survey, long telegramId)
    {
        var startTime = DateTime.UtcNow.AddHours(-12);
        var submitTime = startTime.AddMinutes(6);

        var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

        var response = Response.Create(
            survey.Id,
            telegramId,
            startedAt: startTime,
            isComplete: true,
            submittedAt: submitTime);

        _context.Responses.Add(response);
        _context.SaveChanges(); // Save to get response ID

        var a1 = Answer.CreateJsonAnswer(
            response.Id,
            questions[0].Id, // Single choice
            JsonSerializer.Serialize(new { SelectedOption = "Small Business" }));

        var a2 = Answer.CreateJsonAnswer(
            response.Id,
            questions[1].Id, // Rating
            JsonSerializer.Serialize(new { Value = 5 }));

        var a3 = Answer.CreateJsonAnswer(
            response.Id,
            questions[2].Id, // Multiple choice
            JsonSerializer.Serialize(new[] { "Mobile Access", "Custom Reports" }));

        var a4 = Answer.CreateTextAnswer(
            response.Id,
            questions[3].Id, // Text
            "The mobile app is fantastic! Maybe add offline mode for when internet is spotty.");

        _context.Answers.AddRange(new[] { a1, a2, a3, a4 });

        return response;
    }

    /// <summary>
    /// Creates an incomplete response for testing incomplete state.
    /// </summary>
    private Response CreateIncompleteResponse(Survey survey, long telegramId)
    {
        var startTime = DateTime.UtcNow.AddHours(-2);
        var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

        var response = Response.Create(
            survey.Id,
            telegramId,
            startedAt: startTime,
            isComplete: false,
            submittedAt: null);

        _context.Responses.Add(response);
        _context.SaveChanges(); // Save to get response ID

        var a1 = Answer.CreateTextAnswer(
            response.Id,
            questions[0].Id, // Text - name
            "Alice Johnson");

        var a2 = Answer.CreateJsonAnswer(
            response.Id,
            questions[1].Id, // Single choice - track
            JsonSerializer.Serialize(new { SelectedOption = "Web Development" }));

        // User stopped here - incomplete response
        _context.Answers.AddRange(new[] { a1, a2 });

        return response;
    }
}
