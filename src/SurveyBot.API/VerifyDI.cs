using Microsoft.EntityFrameworkCore;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;

namespace SurveyBot.API;

/// <summary>
/// Utility class to verify Dependency Injection configuration.
/// </summary>
public static class DIVerifier
{
    /// <summary>
    /// Verifies that all required services can be resolved from the DI container.
    /// </summary>
    /// <param name="serviceProvider">The service provider to test.</param>
    /// <returns>A tuple indicating success and a list of any errors.</returns>
    public static (bool Success, List<string> Errors) VerifyServiceResolution(IServiceProvider serviceProvider)
    {
        var errors = new List<string>();

        try
        {
            // Create a scope to test scoped services
            using var scope = serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            // Test DbContext resolution
            try
            {
                var dbContext = scopedProvider.GetRequiredService<SurveyBotDbContext>();
                if (dbContext == null)
                    errors.Add("DbContext resolved to null");
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to resolve SurveyBotDbContext: {ex.Message}");
            }

            // Test Repository resolutions
            var repositories = new[]
            {
                ("ISurveyRepository", typeof(ISurveyRepository)),
                ("IQuestionRepository", typeof(IQuestionRepository)),
                ("IResponseRepository", typeof(IResponseRepository)),
                ("IUserRepository", typeof(IUserRepository)),
                ("IAnswerRepository", typeof(IAnswerRepository))
            };

            foreach (var (name, type) in repositories)
            {
                try
                {
                    var service = scopedProvider.GetRequiredService(type);
                    if (service == null)
                        errors.Add($"{name} resolved to null");
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to resolve {name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to create scope: {ex.Message}");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Prints the verification results to console.
    /// </summary>
    public static void PrintVerificationResults(bool success, List<string> errors)
    {
        Console.WriteLine("\n=== Dependency Injection Verification ===");

        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ All services resolved successfully!");
            Console.ResetColor();
            Console.WriteLine("\nRegistered Services:");
            Console.WriteLine("  - SurveyBotDbContext (Scoped)");
            Console.WriteLine("  - ISurveyRepository -> SurveyRepository (Scoped)");
            Console.WriteLine("  - IQuestionRepository -> QuestionRepository (Scoped)");
            Console.WriteLine("  - IResponseRepository -> ResponseRepository (Scoped)");
            Console.WriteLine("  - IUserRepository -> UserRepository (Scoped)");
            Console.WriteLine("  - IAnswerRepository -> AnswerRepository (Scoped)");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Service resolution failed!");
            Console.ResetColor();
            Console.WriteLine("\nErrors:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }

        Console.WriteLine("==========================================\n");
    }
}
