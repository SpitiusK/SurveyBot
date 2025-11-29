using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SurveyBot.Bot.Utilities;

/// <summary>
/// Converts ReactQuill HTML to Telegram-compatible HTML format.
///
/// Telegram supports limited HTML tags:
/// - Bold: <b>, <strong>
/// - Italic: <i>, <em>
/// - Underline: <u>, <ins>
/// - Strikethrough: <s>, <strike>, <del>
/// - Links: <a href="...">
/// - Code: <code>, <pre>
///
/// NOT supported (must be converted):
/// - <p>, <div>, <span> - removed, content preserved
/// - <br> - converted to newline
/// - <h1>-<h6> - converted to bold
/// - <ul>, <ol>, <li> - converted to text lists
/// - <blockquote> - converted to > prefixed lines
/// - class attributes - removed
/// </summary>
public static class HtmlToTelegramConverter
{
    /// <summary>
    /// Converts ReactQuill HTML to Telegram-compatible HTML.
    /// </summary>
    /// <param name="html">ReactQuill HTML content</param>
    /// <returns>Telegram-compatible HTML string</returns>
    public static string Convert(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var result = html;

        // Step 1: Remove class attributes (Telegram doesn't support them)
        result = Regex.Replace(result, @"\s+class=""[^""]*""", string.Empty, RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\s+spellcheck=""[^""]*""", string.Empty, RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\s+rel=""[^""]*""", string.Empty, RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\s+target=""[^""]*""", string.Empty, RegexOptions.IgnoreCase);

        // Step 2: Convert headers to bold
        result = Regex.Replace(result, @"<h[1-6][^>]*>", "<b>", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"</h[1-6]>", "</b>\n", RegexOptions.IgnoreCase);

        // Step 3: Convert blockquote to > prefixed text
        result = ConvertBlockquotes(result);

        // Step 4: Convert lists to text format
        result = ConvertLists(result);

        // Step 5: Convert <br> to newlines
        result = Regex.Replace(result, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);

        // Step 6: Convert <p> tags to newlines
        result = Regex.Replace(result, @"<p[^>]*>", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"</p>", "\n", RegexOptions.IgnoreCase);

        // Step 7: Remove <div> and <span> tags but keep content
        result = Regex.Replace(result, @"</?div[^>]*>", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"</?span[^>]*>", "", RegexOptions.IgnoreCase);

        // Step 8: Clean up <pre> tags (remove class, keep tag)
        result = Regex.Replace(result, @"<pre[^>]*>", "<pre>", RegexOptions.IgnoreCase);

        // Step 9: Normalize multiple newlines to maximum 2
        result = Regex.Replace(result, @"\n{3,}", "\n\n");

        // Step 10: Trim whitespace
        result = result.Trim();

        return result;
    }

    /// <summary>
    /// Converts blockquote tags to > prefixed text lines.
    /// </summary>
    private static string ConvertBlockquotes(string html)
    {
        // Match blockquote content
        var pattern = @"<blockquote[^>]*>(.*?)</blockquote>";

        return Regex.Replace(html, pattern, match =>
        {
            var content = match.Groups[1].Value;
            // Strip inner HTML tags for simplicity and prefix with >
            var plainContent = StripHtmlTags(content);
            var lines = plainContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var quotedLines = lines.Select(line => $"❝ {line.Trim()}");
            return string.Join("\n", quotedLines) + "\n";
        }, RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

    /// <summary>
    /// Converts ordered and unordered lists to text format.
    /// </summary>
    private static string ConvertLists(string html)
    {
        var result = html;

        // Convert ordered lists <ol>
        result = Regex.Replace(result, @"<ol[^>]*>(.*?)</ol>", match =>
        {
            var listContent = match.Groups[1].Value;
            var items = Regex.Matches(listContent, @"<li[^>]*>(.*?)</li>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var sb = new StringBuilder();
            int index = 1;
            foreach (Match item in items)
            {
                var itemContent = StripHtmlTags(item.Groups[1].Value).Trim();
                sb.AppendLine($"{index}. {itemContent}");
                index++;
            }
            return sb.ToString();
        }, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Convert unordered lists <ul>
        result = Regex.Replace(result, @"<ul[^>]*>(.*?)</ul>", match =>
        {
            var listContent = match.Groups[1].Value;
            var items = Regex.Matches(listContent, @"<li[^>]*>(.*?)</li>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var sb = new StringBuilder();
            foreach (Match item in items)
            {
                var itemContent = StripHtmlTags(item.Groups[1].Value).Trim();
                sb.AppendLine($"• {itemContent}");
            }
            return sb.ToString();
        }, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return result;
    }

    /// <summary>
    /// Strips all HTML tags from text, leaving only plain text content.
    /// Used internally for list items and blockquotes.
    /// </summary>
    private static string StripHtmlTags(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Remove all HTML tags
        var result = Regex.Replace(html, @"<[^>]*>", string.Empty);

        // Decode HTML entities
        result = WebUtility.HtmlDecode(result);

        return result;
    }

    /// <summary>
    /// Escapes special HTML characters that could break Telegram parsing.
    /// Use this for user-generated content that should NOT be interpreted as HTML.
    /// </summary>
    /// <param name="text">Plain text to escape</param>
    /// <returns>HTML-escaped string safe for Telegram</returns>
    public static string EscapeHtml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    /// <summary>
    /// Checks if the text appears to contain HTML tags.
    /// </summary>
    public static bool ContainsHtml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return Regex.IsMatch(text, @"<[^>]+>");
    }
}
