using System.Text;
using Notifier.Core.Channels;

namespace Notifier.Web.Channels;

internal static class EmailFormatter
{
    public const int MaxSubjectLength = 200;

    public static (string Subject, string Body) Format(Notification n)
    {
        var tag = n.IsTest ? "[TEST] " : n.IsSynthetic ? "[SYNTHETIC] " : "";
        var alert = TextSanitizer.SingleLine(n.AlertName, 100);
        var body = new StringBuilder();
        string subject;

        if (n.Items.Count == 1 && n.MoreCount == 0)
        {
            var item = n.Items[0];
            var title = TextSanitizer.SingleLine(item.Title, 500);
            var reason = TextSanitizer.SingleLine(item.Reason, 200);
            subject = $"[Notifier] {tag}{reason}: {title}";
            body.AppendLine(title).AppendLine(reason).AppendLine();
            if (!string.IsNullOrWhiteSpace(item.Summary))
                body.AppendLine(TextSanitizer.SingleLine(item.Summary, 2000)).AppendLine();
            body.AppendLine($"Read more: {item.Url.AbsoluteUri}");
        }
        else
        {
            var total = n.Items.Count + n.MoreCount;
            subject = $"[Notifier] {tag}{total} new stories for '{alert}'";
            body.AppendLine($"{total} new stories for your alert \"{alert}\":").AppendLine();
            for (var i = 0; i < n.Items.Count; i++)
            {
                var item = n.Items[i];
                body.AppendLine($"{i + 1}. {TextSanitizer.SingleLine(item.Title, 300)}")
                    .AppendLine($"   {TextSanitizer.SingleLine(item.Reason, 200)}")
                    .AppendLine($"   {item.Url.AbsoluteUri}")
                    .AppendLine();
            }
            if (n.MoreCount > 0)
                body.AppendLine($"...and {n.MoreCount} more.").AppendLine();
        }

        body.AppendLine("--")
            .AppendLine(TextSanitizer.SingleLine(n.Attribution, 100))
            .AppendLine($"You receive this because of your alert \"{alert}\".");

        return (TextSanitizer.SingleLine(subject, MaxSubjectLength), body.ToString());
    }
}
