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
        var users = new List<User>
        {
            new User
            {
                TelegramId = 123456789,
                Username = "john_doe",
                FirstName = "John",
                LastName = "Doe"
            },
            new User
            {
                TelegramId = 987654321,
                Username = "jane_smith",
                FirstName = "Jane",
                LastName = "Smith"
            },
            new User
            {
                TelegramId = 555555555,
                Username = "test_user",
                FirstName = "Test",
                LastName = "User"
            }
        };

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

        var surveys = new List<Survey>
        {
            CreateCustomerSatisfactionSurvey(creator.Id),
            CreateProductFeedbackSurvey(creator.Id),
            CreateEventRegistrationSurvey(creator.Id)
        };

        _context.Surveys.AddRange(surveys);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} surveys with questions.", surveys.Count);
    }

    /// <summary>
    /// Creates a customer satisfaction survey with all question types.
    /// </summary>
    private Survey CreateCustomerSatisfactionSurvey(int creatorId)
    {
        return new Survey
        {
            Title = "Customer Satisfaction Survey",
            Description = "Help us improve our service by sharing your feedback!",
            CreatorId = creatorId,
            IsActive = true,
            AllowMultipleResponses = false,
            ShowResults = true,
            Questions = new List<Question>
            {
                new Question
                {
                    QuestionText = "How would you rate our service overall?",
                    QuestionType = QuestionType.Rating,
                    OrderIndex = 0,
                    IsRequired = true,
                    OptionsJson = JsonSerializer.Serialize(new
                    {
                        MinValue = 1,
                        MaxValue = 5,
                        MinLabel = "Poor",
                        MaxLabel = "Excellent"
                    })
                },
                new Question
                {
                    QuestionText = "Which of our services have you used?",
                    QuestionType = QuestionType.MultipleChoice,
                    OrderIndex = 1,
                    IsRequired = true,
                    OptionsJson = JsonSerializer.Serialize(new[]
                    {
                        "Online Support",
                        "Phone Support",
                        "In-Person Service",
                        "Mobile App",
                        "Website"
                    })
                },
                new Question
                {
                    QuestionText = "How did you hear about us?",
                    QuestionType = QuestionType.SingleChoice,
                    OrderIndex = 2,
                    IsRequired = true,
                    OptionsJson = JsonSerializer.Serialize(new[]
                    {
                        "Social Media",
                        "Search Engine",
                        "Friend Referral",
                        "Advertisement",
                        "Other"
                    })
                },
                new Question
                {
                    QuestionText = "Please share any additional feedback or suggestions:",
                    QuestionType = QuestionType.Text,
                    OrderIndex = 3,
                    IsRequired = false,
                    OptionsJson = null
                }
            }
        };
    }

    /// <summary>
    /// Creates a product feedback survey.
    /// </summary>
    private Survey CreateProductFeedbackSurvey(int creatorId)
    {
        return new Survey
        {
            Title = "New Product Feature Feedback",
            Description = "We're developing new features and would love your input!",
            CreatorId = creatorId,
            IsActive = true,
            AllowMultipleResponses = true,
            ShowResults = false,
            Questions = new List<Question>
            {
                new Question
                {
                    QuestionText = "What is your primary use case?",
                    QuestionType = QuestionType.SingleChoice,
                    OrderIndex = 0,
                    IsRequired = true,
                    OptionsJson = JsonSerializer.Serialize(new[]
                    {
                        "Personal Use",
                        "Small Business",
                        "Enterprise",
                        "Education",
                        "Non-Profit"
                    })
                },
                new Question
                {
                    QuestionText = "How easy is the product to use?",
                    QuestionType = QuestionType.Rating,
                    OrderIndex = 1,
                    IsRequired = true,
                    OptionsJson = JsonSerializer.Serialize(new
                    {
                        MinValue = 1,
                        MaxValue = 5,
                        MinLabel = "Very Difficult",
                        MaxLabel = "Very Easy"
                    })
                },
                new Question
                {
                    QuestionText = "Which features do you find most valuable?",
                    QuestionType = QuestionType.MultipleChoice,
                    OrderIndex = 2,
                    IsRequired = true,
                    OptionsJson = JsonSerializer.Serialize(new[]
                    {
                        "Real-time Analytics",
                        "Custom Reports",
                        "Mobile Access",
                        "Integration Support",
                        "Automation Tools"
                    })
                },
                new Question
                {
                    QuestionText = "Describe a feature you wish we had:",
                    QuestionType = QuestionType.Text,
                    OrderIndex = 3,
                    IsRequired = false,
                    OptionsJson = null
                }
            }
        };
    }

    /// <summary>
    /// Creates an event registration survey.
    /// </summary>
    private Survey CreateEventRegistrationSurvey(int creatorId)
    {
        return new Survey
        {
            Title = "Tech Conference 2025 - Registration",
            Description = "Register for our upcoming tech conference and let us know your preferences!",
            CreatorId = creatorId,
            IsActive = false, // Inactive survey for testing
            AllowMultipleResponses = false,
            ShowResults = true,
            Questions = new List<Question>
            {
                new Question
                {
                    QuestionText = "What is your full name?",
                    QuestionType = QuestionType.Text,
                    OrderIndex = 0,
                    IsRequired = true,
                    OptionsJson = null
                },
                new Question
                {
                    QuestionText = "Which track are you most interested in?",
                    QuestionType = QuestionType.SingleChoice,
                    OrderIndex = 1,
                    IsRequired = true,
                    OptionsJson = JsonSerializer.Serialize(new[]
                    {
                        "Web Development",
                        "Mobile Development",
                        "DevOps & Cloud",
                        "Data Science & AI",
                        "Security"
                    })
                },
                new Question
                {
                    QuestionText = "Which workshops would you like to attend?",
                    QuestionType = QuestionType.MultipleChoice,
                    OrderIndex = 2,
                    IsRequired = false,
                    OptionsJson = JsonSerializer.Serialize(new[]
                    {
                        "Docker & Kubernetes",
                        "React Advanced Patterns",
                        "Machine Learning 101",
                        "Microservices Architecture",
                        "Testing Best Practices"
                    })
                },
                new Question
                {
                    QuestionText = "How would you rate your technical expertise?",
                    QuestionType = QuestionType.Rating,
                    OrderIndex = 3,
                    IsRequired = true,
                    OptionsJson = JsonSerializer.Serialize(new
                    {
                        MinValue = 1,
                        MaxValue = 5,
                        MinLabel = "Beginner",
                        MaxLabel = "Expert"
                    })
                }
            }
        };
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

        var responses = new List<Response>();

        // Create responses for the customer satisfaction survey
        var customerSurvey = surveys.First(s => s.Title == "Customer Satisfaction Survey");
        responses.Add(CreateCustomerSatisfactionResponse1(customerSurvey, users[1].TelegramId));
        responses.Add(CreateCustomerSatisfactionResponse2(customerSurvey, users[2].TelegramId));

        // Create responses for the product feedback survey
        var productSurvey = surveys.First(s => s.Title == "New Product Feature Feedback");
        responses.Add(CreateProductFeedbackResponse1(productSurvey, users[0].TelegramId));
        responses.Add(CreateProductFeedbackResponse2(productSurvey, users[1].TelegramId));

        // Create an incomplete response for testing
        var incompleteSurvey = surveys.First(s => s.Title == "Tech Conference 2025 - Registration");
        responses.Add(CreateIncompleteResponse(incompleteSurvey, users[2].TelegramId));

        _context.Responses.AddRange(responses);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} responses with answers.", responses.Count);
    }

    /// <summary>
    /// Creates a complete customer satisfaction response (positive feedback).
    /// </summary>
    private Response CreateCustomerSatisfactionResponse1(Survey survey, long telegramId)
    {
        var startTime = DateTime.UtcNow.AddDays(-2);
        var submitTime = startTime.AddMinutes(5);

        var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

        return new Response
        {
            SurveyId = survey.Id,
            RespondentTelegramId = telegramId,
            IsComplete = true,
            StartedAt = startTime,
            SubmittedAt = submitTime,
            Answers = new List<Answer>
            {
                new Answer
                {
                    QuestionId = questions[0].Id, // Rating
                    AnswerJson = JsonSerializer.Serialize(new { Value = 5 }),
                    CreatedAt = startTime.AddMinutes(1)
                },
                new Answer
                {
                    QuestionId = questions[1].Id, // Multiple choice
                    AnswerJson = JsonSerializer.Serialize(new[] { "Online Support", "Mobile App", "Website" }),
                    CreatedAt = startTime.AddMinutes(2)
                },
                new Answer
                {
                    QuestionId = questions[2].Id, // Single choice
                    AnswerJson = JsonSerializer.Serialize(new { SelectedOption = "Friend Referral" }),
                    CreatedAt = startTime.AddMinutes(3)
                },
                new Answer
                {
                    QuestionId = questions[3].Id, // Text
                    AnswerText = "Great service! Very satisfied with the quick response times and helpful support staff.",
                    CreatedAt = startTime.AddMinutes(4)
                }
            }
        };
    }

    /// <summary>
    /// Creates a complete customer satisfaction response (mixed feedback).
    /// </summary>
    private Response CreateCustomerSatisfactionResponse2(Survey survey, long telegramId)
    {
        var startTime = DateTime.UtcNow.AddDays(-1);
        var submitTime = startTime.AddMinutes(7);

        var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

        return new Response
        {
            SurveyId = survey.Id,
            RespondentTelegramId = telegramId,
            IsComplete = true,
            StartedAt = startTime,
            SubmittedAt = submitTime,
            Answers = new List<Answer>
            {
                new Answer
                {
                    QuestionId = questions[0].Id, // Rating
                    AnswerJson = JsonSerializer.Serialize(new { Value = 3 }),
                    CreatedAt = startTime.AddMinutes(1)
                },
                new Answer
                {
                    QuestionId = questions[1].Id, // Multiple choice
                    AnswerJson = JsonSerializer.Serialize(new[] { "Phone Support", "Website" }),
                    CreatedAt = startTime.AddMinutes(3)
                },
                new Answer
                {
                    QuestionId = questions[2].Id, // Single choice
                    AnswerJson = JsonSerializer.Serialize(new { SelectedOption = "Search Engine" }),
                    CreatedAt = startTime.AddMinutes(5)
                },
                new Answer
                {
                    QuestionId = questions[3].Id, // Text
                    AnswerText = "Service is good but response times could be improved. The website needs better mobile optimization.",
                    CreatedAt = startTime.AddMinutes(6)
                }
            }
        };
    }

    /// <summary>
    /// Creates a complete product feedback response (power user).
    /// </summary>
    private Response CreateProductFeedbackResponse1(Survey survey, long telegramId)
    {
        var startTime = DateTime.UtcNow.AddDays(-3);
        var submitTime = startTime.AddMinutes(8);

        var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

        return new Response
        {
            SurveyId = survey.Id,
            RespondentTelegramId = telegramId,
            IsComplete = true,
            StartedAt = startTime,
            SubmittedAt = submitTime,
            Answers = new List<Answer>
            {
                new Answer
                {
                    QuestionId = questions[0].Id, // Single choice
                    AnswerJson = JsonSerializer.Serialize(new { SelectedOption = "Enterprise" }),
                    CreatedAt = startTime.AddMinutes(1)
                },
                new Answer
                {
                    QuestionId = questions[1].Id, // Rating
                    AnswerJson = JsonSerializer.Serialize(new { Value = 4 }),
                    CreatedAt = startTime.AddMinutes(3)
                },
                new Answer
                {
                    QuestionId = questions[2].Id, // Multiple choice
                    AnswerJson = JsonSerializer.Serialize(new[]
                    {
                        "Real-time Analytics",
                        "Custom Reports",
                        "Integration Support",
                        "Automation Tools"
                    }),
                    CreatedAt = startTime.AddMinutes(5)
                },
                new Answer
                {
                    QuestionId = questions[3].Id, // Text
                    AnswerText = "Would love to see better API rate limits and support for webhooks. Also, batch operations would be incredibly useful for enterprise deployments.",
                    CreatedAt = startTime.AddMinutes(7)
                }
            }
        };
    }

    /// <summary>
    /// Creates a complete product feedback response (small business).
    /// </summary>
    private Response CreateProductFeedbackResponse2(Survey survey, long telegramId)
    {
        var startTime = DateTime.UtcNow.AddHours(-12);
        var submitTime = startTime.AddMinutes(6);

        var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

        return new Response
        {
            SurveyId = survey.Id,
            RespondentTelegramId = telegramId,
            IsComplete = true,
            StartedAt = startTime,
            SubmittedAt = submitTime,
            Answers = new List<Answer>
            {
                new Answer
                {
                    QuestionId = questions[0].Id, // Single choice
                    AnswerJson = JsonSerializer.Serialize(new { SelectedOption = "Small Business" }),
                    CreatedAt = startTime.AddMinutes(1)
                },
                new Answer
                {
                    QuestionId = questions[1].Id, // Rating
                    AnswerJson = JsonSerializer.Serialize(new { Value = 5 }),
                    CreatedAt = startTime.AddMinutes(2)
                },
                new Answer
                {
                    QuestionId = questions[2].Id, // Multiple choice
                    AnswerJson = JsonSerializer.Serialize(new[] { "Mobile Access", "Custom Reports" }),
                    CreatedAt = startTime.AddMinutes(4)
                },
                new Answer
                {
                    QuestionId = questions[3].Id, // Text
                    AnswerText = "The mobile app is fantastic! Maybe add offline mode for when internet is spotty.",
                    CreatedAt = startTime.AddMinutes(5)
                }
            }
        };
    }

    /// <summary>
    /// Creates an incomplete response for testing incomplete state.
    /// </summary>
    private Response CreateIncompleteResponse(Survey survey, long telegramId)
    {
        var startTime = DateTime.UtcNow.AddHours(-2);
        var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

        return new Response
        {
            SurveyId = survey.Id,
            RespondentTelegramId = telegramId,
            IsComplete = false,
            StartedAt = startTime,
            SubmittedAt = null,
            Answers = new List<Answer>
            {
                new Answer
                {
                    QuestionId = questions[0].Id, // Text - name
                    AnswerText = "Alice Johnson",
                    CreatedAt = startTime.AddMinutes(1)
                },
                new Answer
                {
                    QuestionId = questions[1].Id, // Single choice - track
                    AnswerJson = JsonSerializer.Serialize(new { SelectedOption = "Web Development" }),
                    CreatedAt = startTime.AddMinutes(2)
                }
                // User stopped here - incomplete response
            }
        };
    }
}
