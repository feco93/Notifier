using System.Text;
using System.Text.Json;
using Notifier.Core.Channels;

namespace Notifier.Web.Channels;

internal static class SlackFormatter
{
    /// <summary>Slack recommends at most 4,000 characters of text; longer messages may be split.</summary>
    public const int MaxTextLength = 4000;
    private const int FooterReserve = 200;

    public static string Payload(Notification n) =>
        // No unfurling: previews would pull in publisher images and branding.
        JsonSerializer.Serialize(new { text = Text(n), unfurl_links = false, unfurl_media = false });

    public static string Text(Notification n)
    {
        var tag = n.IsTest ? "[TEST] " : n.IsSynthetic ? "[SYNTHETIC] " : "";
        var footer = $"_{Escape(TextSanitizer.SingleLine(n.Attribution, 100))}_";

        if (n.Items.Count == 1 && n.MoreCount == 0)
        {
            var item = n.Items[0];
            var sb = new StringBuilder()
                .Append(tag).Append('*').Append(Link(item.Url, TextSanitizer.SingleLine(item.Title, 300))).AppendLine("*")
                .AppendLine(Escape(TextSanitizer.SingleLine(item.Reason, 200)));
            if (!string.IsNullOrWhiteSpace(item.Summary))
                sb.AppendLine(Escape(TextSanitizer.SingleLine(item.Summary, 1000)));
            return sb.Append(footer).ToString();
        }

        var total = n.Items.Count + n.MoreCount;
        var text = new StringBuilder()
            .Append(tag).Append($"*{total} new stories for '{Escape(TextSanitizer.SingleLine(n.AlertName, 100))}'*").AppendLine();
        var shown = 0;
        foreach (var item in n.Items)
        {
            var line = $"• {Link(item.Url, TextSanitizer.SingleLine(item.Title, 150))} — {Escape(TextSanitizer.SingleLine(item.Reason, 120))}";
            if (text.Length + line.Length + 1 + FooterReserve > MaxTextLength) break;
            text.AppendLine(line);
            shown++;
        }
        var more = total - shown;
        if (more > 0) text.AppendLine($"…and {more} more");
        return text.Append(footer).ToString();
    }

    // Slack treats &, < and > as control characters; only these three are decoded for display.
    private static string Escape(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    // AbsoluteUri already percent-encodes spaces, '|', '<' and '>'; '&' is legal in URLs and must be escaped.
    private static string Link(Uri url, string label) =>
        $"<{url.AbsoluteUri.Replace("&", "&amp;")}|{Escape(label)}>";
}
