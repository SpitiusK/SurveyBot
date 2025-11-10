using System.Security.Cryptography;

namespace SurveyBot.Core.Utilities;

/// <summary>
/// Generates unique, URL-safe codes for surveys.
/// </summary>
public static class SurveyCodeGenerator
{
    // Use only alphanumeric characters (Base36: A-Z, 0-9)
    private const string ValidChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CodeLength = 6;

    /// <summary>
    /// Generates a random survey code.
    /// </summary>
    /// <returns>A 6-character alphanumeric code.</returns>
    public static string GenerateCode()
    {
        var code = new char[CodeLength];

        // Use cryptographically secure random number generator
        for (int i = 0; i < CodeLength; i++)
        {
            code[i] = ValidChars[RandomNumberGenerator.GetInt32(ValidChars.Length)];
        }

        return new string(code);
    }

    /// <summary>
    /// Generates a unique survey code by checking against existing codes.
    /// </summary>
    /// <param name="codeExistsAsync">Function to check if a code already exists.</param>
    /// <param name="maxAttempts">Maximum number of generation attempts (default: 10).</param>
    /// <returns>A unique survey code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when unable to generate unique code after max attempts.</exception>
    public static async Task<string> GenerateUniqueCodeAsync(
        Func<string, Task<bool>> codeExistsAsync,
        int maxAttempts = 10)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var code = GenerateCode();

            if (!await codeExistsAsync(code))
            {
                return code;
            }
        }

        throw new InvalidOperationException(
            $"Unable to generate unique survey code after {maxAttempts} attempts. " +
            "This is extremely unlikely and may indicate a system issue.");
    }

    /// <summary>
    /// Validates if a code has the correct format.
    /// </summary>
    /// <param name="code">The code to validate.</param>
    /// <returns>True if the code is valid, otherwise false.</returns>
    public static bool IsValidCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        if (code.Length != CodeLength)
            return false;

        return code.All(c => ValidChars.Contains(c));
    }
}
