using System.Text.Json;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SurveyBot.Bot.Handlers.Questions;

/// <summary>
/// Handles text-based questions that accept free-form text input.
/// </summary>
public class TextQuestionHandler : IQuestionHandler
{
    private readonly IBotService _botService;
    private readonly ILogger<TextQuestionHandler> _logger;

    public QuestionType QuestionType => QuestionType.Text;

    public TextQuestionHandler(
        IBotService botService,
        ILogger<TextQuestionHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays the text question to the user.
    /// </summary>
    public async Task<int> DisplayQuestionAsync(
        long chatId,
        QuestionDto question,
        int currentIndex,
        int totalQuestions,
        CancellationToken cancellationToken = default)
    {
        var progressText = $"Question {currentIndex + 1} of {totalQuestions}";
        var requiredText = question.IsRequired ? "(Required)" : "(Optional - reply /skip to skip)";

        var message = $"{progressText}\n\n" +
                      $"*{question.QuestionText}*\n\n" +
                      $"{requiredText}\n\n" +
                      $"Please type your answer below:";

        _logger.LogDebug(
            "Displaying text question {QuestionId} in chat {ChatId}",
            question.Id,
            chatId);

        var sentMessage = await _botService.Client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);

        return sentMessage.MessageId;
    }

    /// <summary>
    /// Processes text answer from user's message.
    /// </summary>
    public async Task<string?> ProcessAnswerAsync(
        Message? message,
        CallbackQuery? callbackQuery,
        QuestionDto question,
        long userId,
        CancellationToken cancellationToken = default)
    {
        // Text questions only accept message input, not callback queries
        if (message == null || string.IsNullOrWhiteSpace(message.Text))
        {
            _logger.LogDebug("Text question requires text message input");
            return null;
        }

        var text = message.Text.Trim();

        // Check if user is trying to skip
        if (text.Equals("/skip", StringComparison.OrdinalIgnoreCase))
        {
            if (question.IsRequired)
            {
                await _botService.Client.SendMessage(
                    chatId: message.Chat.Id,
                    text: "This question is required. Please provide an answer.",
                    cancellationToken: cancellationToken);
                return null;
            }

            // Allow skip for optional questions - return empty answer
            return JsonSerializer.Serialize(new { text = "" });
        }

        // Validate minimum length (at least 1 character)
        if (string.IsNullOrWhiteSpace(text))
        {
            await _botService.Client.SendMessage(
                chatId: message.Chat.Id,
                text: "Please provide a text answer.",
                cancellationToken: cancellationToken);
            return null;
        }

        // Create answer JSON
        var answerJson = JsonSerializer.Serialize(new { text });

        _logger.LogDebug(
            "Text answer processed for question {QuestionId} from user {UserId}: length={Length}",
            question.Id,
            userId,
            text.Length);

        return answerJson;
    }

    /// <summary>
    /// Validates text answer format and content.
    /// </summary>
    public bool ValidateAnswer(string? answerJson, QuestionDto question)
    {
        if (string.IsNullOrWhiteSpace(answerJson))
            return !question.IsRequired; // Empty answer OK for optional questions

        try
        {
            var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

            if (!answer.TryGetProperty("text", out var textElement))
                return false;

            var text = textElement.GetString();

            // Required questions must have non-empty text
            if (question.IsRequired && string.IsNullOrWhiteSpace(text))
                return false;

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON format for text answer");
            return false;
        }
    }
}
