using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.DTOs.Media;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SurveyBot.Bot.Handlers.Commands;

/// <summary>
/// Handles the /preview command.
/// Displays survey details with media indicators for each question.
/// </summary>
public class PreviewCommand : ICommandHandler
{
    private readonly IBotService _botService;
    private readonly IUserRepository _userRepository;
    private readonly ISurveyRepository _surveyRepository;
    private readonly ILogger<PreviewCommand> _logger;

    public string Command => "preview";

    public PreviewCommand(
        IBotService botService,
        IUserRepository userRepository,
        ISurveyRepository surveyRepository,
        ILogger<PreviewCommand> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _surveyRepository = surveyRepository ?? throw new ArgumentNullException(nameof(surveyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null)
        {
            _logger.LogWarning("Received /preview command with null From user");
            return;
        }

        var telegramId = message.From.Id;
        var chatId = message.Chat.Id;

        try
        {
            _logger.LogInformation(
                "Processing /preview command from user {TelegramId}",
                telegramId);

            // Parse survey ID from command
            var commandText = message.Text ?? string.Empty;
            var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                await SendUsageMessage(chatId, cancellationToken);
                return;
            }

            if (!int.TryParse(parts[1], out var surveyId) || surveyId <= 0)
            {
                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "Invalid survey ID. Please provide a valid number.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Get user from database
            var user = await _userRepository.GetByTelegramIdAsync(telegramId);
            if (user == null)
            {
                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "You are not registered yet. Please use /start to register.",
                    cancellationToken: cancellationToken);
                return;
            }

            _logger.LogInformation(
                "Fetching survey {SurveyId} for preview by user {UserId}",
                surveyId,
                user.Id);

            // Get survey with questions
            var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);
            if (survey == null)
            {
                throw new SurveyNotFoundException(surveyId);
            }

            // Check if user owns the survey
            if (survey.CreatorId != user.Id)
            {
                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "You don't have permission to preview this survey. You can only preview surveys you created.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Build and send preview message
            var previewMessage = BuildPreviewMessage(survey);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: previewMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Preview sent for survey {SurveyId} to user {TelegramId}",
                surveyId,
                telegramId);
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Survey not found for preview by user {TelegramId}",
                telegramId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Survey not found. Please check the survey ID and try again.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing /preview command for user {TelegramId}",
                telegramId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, an error occurred while generating the preview. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    public string GetDescription()
    {
        return "Preview a survey with media information";
    }

    private async Task SendUsageMessage(long chatId, CancellationToken cancellationToken)
    {
        var usageMessage = "<b>Preview Survey</b>\n\n" +
                          "Usage: /preview &lt;survey_id&gt;\n\n" +
                          "Example: /preview 123\n\n" +
                          "This command shows a text preview of your survey including:\n" +
                          "- Survey title and status\n" +
                          "- All questions\n" +
                          "- Media count and types for each question\n\n" +
                          "Tip: Use /mysurveys to see your survey IDs.";

        await _botService.Client.SendMessage(
            chatId: chatId,
            text: usageMessage,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private string BuildPreviewMessage(Core.Entities.Survey survey)
    {
        var preview = new StringBuilder();

        // Survey header
        var statusEmoji = survey.IsActive ? "✅" : "🔴";
        var statusText = survey.IsActive ? "Active" : "Draft";

        preview.AppendLine($"<b>{EscapeHtml(survey.Title)}</b>");
        preview.AppendLine($"Status: {statusEmoji} {statusText}");
        preview.AppendLine($"Questions: {survey.Questions.Count}");

        if (!string.IsNullOrWhiteSpace(survey.Code))
        {
            preview.AppendLine($"Code: <code>{survey.Code}</code>");
        }

        if (!string.IsNullOrWhiteSpace(survey.Description))
        {
            preview.AppendLine($"\n<i>{EscapeHtml(survey.Description)}</i>");
        }

        preview.AppendLine();

        // Questions with media indicators
        var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

        foreach (var question in questions)
        {
            preview.AppendLine($"<b>Question {question.OrderIndex + 1}:</b> {EscapeHtml(question.QuestionText)}");

            // Question type
            var typeText = GetQuestionTypeText(question.QuestionType);
            preview.AppendLine($"   Type: {typeText}");

            // Required indicator
            var requiredText = question.IsRequired ? "Required" : "Optional";
            preview.AppendLine($"   {requiredText}");

            // Media indicator
            var mediaIndicator = FormatMediaIndicator(question.MediaContent);
            preview.AppendLine($"   {mediaIndicator}");

            // Show options for choice questions
            if (question.QuestionType == Core.Entities.QuestionType.SingleChoice ||
                question.QuestionType == Core.Entities.QuestionType.MultipleChoice)
            {
                var options = DeserializeOptions(question.OptionsJson);
                if (options != null && options.Count > 0)
                {
                    preview.AppendLine($"   Options: {string.Join(", ", options.Select(EscapeHtml))}");
                }
            }

            preview.AppendLine();
        }

        // Footer
        preview.AppendLine($"Created: {survey.CreatedAt:MMM dd, yyyy}");

        if (survey.Questions.Count == 0)
        {
            preview.AppendLine("\n<i>No questions added yet. Add questions to activate this survey.</i>");
        }

        return preview.ToString();
    }

    private string FormatMediaIndicator(string? mediaContentJson)
    {
        if (string.IsNullOrWhiteSpace(mediaContentJson))
        {
            return "📎 Media: None";
        }

        try
        {
            var mediaContent = DeserializeMediaContent(mediaContentJson);

            if (mediaContent?.Items == null || mediaContent.Items.Count == 0)
            {
                return "📎 Media: None";
            }

            // Group by type and count
            var typeGroups = mediaContent.Items
                .GroupBy(m => m.Type.ToLowerInvariant())
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .OrderBy(g => g.Type)
                .ToList();

            if (typeGroups.Count == 0)
            {
                return "📎 Media: None";
            }

            // Build media summary with emojis
            var mediaParts = new List<string>();
            foreach (var group in typeGroups)
            {
                var emoji = GetMediaTypeEmoji(group.Type);
                var pluralType = group.Count == 1 ? group.Type : GetPluralType(group.Type);
                mediaParts.Add($"{group.Count} {pluralType} {emoji}");
            }

            return $"📎 Media: {string.Join(", ", mediaParts)}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to deserialize media content: {MediaContent}",
                mediaContentJson);
            return "📎 Media: Error reading media data";
        }
    }

    private MediaContentDto? DeserializeMediaContent(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<MediaContentDto>(json, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to deserialize MediaContent JSON: {Json}",
                json);
            return null;
        }
    }

    private List<string>? DeserializeOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to deserialize options JSON: {Json}",
                json);
            return null;
        }
    }

    private static string GetQuestionTypeText(Core.Entities.QuestionType questionType)
    {
        return questionType switch
        {
            Core.Entities.QuestionType.Text => "Text",
            Core.Entities.QuestionType.SingleChoice => "Single Choice",
            Core.Entities.QuestionType.MultipleChoice => "Multiple Choice",
            Core.Entities.QuestionType.Rating => "Rating (1-5)",
            _ => "Unknown"
        };
    }

    private static string GetMediaTypeEmoji(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "image" => "📷",
            "video" => "🎬",
            "audio" => "🎵",
            "document" => "📄",
            _ => "📎"
        };
    }

    private static string GetPluralType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "image" => "images",
            "video" => "videos",
            "audio" => "audios",
            "document" => "documents",
            _ => type + "s"
        };
    }

    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
