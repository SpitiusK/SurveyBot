using SurveyBot.Core.DTOs.Question;
using Telegram.Bot.Types;

namespace SurveyBot.Bot.Interfaces;

/// <summary>
/// Interface for handling different question types in surveys.
/// Implementations handle display, validation, and answer collection for each question type.
/// </summary>
public interface IQuestionHandler
{
    /// <summary>
    /// Gets the question type this handler processes.
    /// </summary>
    Core.Entities.QuestionType QuestionType { get; }

    /// <summary>
    /// Displays the question to the user and sets up appropriate input method.
    /// </summary>
    /// <param name="chatId">The Telegram chat ID.</param>
    /// <param name="question">The question to display.</param>
    /// <param name="currentIndex">Current question index (0-based).</param>
    /// <param name="totalQuestions">Total number of questions in survey.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Message ID of the sent question message.</returns>
    Task<int> DisplayQuestionAsync(
        long chatId,
        QuestionDto question,
        int currentIndex,
        int totalQuestions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and processes the user's answer.
    /// </summary>
    /// <param name="message">The message containing the answer (for text-based answers).</param>
    /// <param name="callbackQuery">The callback query (for button-based answers).</param>
    /// <param name="question">The question being answered.</param>
    /// <param name="userId">The user ID for state management.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Answer JSON string if valid, null if invalid.</returns>
    Task<string?> ProcessAnswerAsync(
        Message? message,
        CallbackQuery? callbackQuery,
        QuestionDto question,
        long userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if the answer is complete and meets requirements.
    /// </summary>
    /// <param name="answerJson">The answer JSON to validate.</param>
    /// <param name="question">The question being answered.</param>
    /// <returns>True if answer is valid, false otherwise.</returns>
    bool ValidateAnswer(string? answerJson, QuestionDto question);
}
