using System.Text;

namespace Notifier.Web.Channels;

internal static class TextSanitizer
{
    /// <summary>Replaces control characters (including line breaks) with spaces, collapses whitespace and truncates.</summary>
    public static string SingleLine(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new StringBuilder(text.Length);
        var lastWasSpace = false;
        foreach (var c in text)
        {
            var isSpace = char.IsWhiteSpace(c) || char.IsControl(c);
            if (isSpace && lastWasSpace) continue;
            sb.Append(isSpace ? ' ' : c);
            lastWasSpace = isSpace;
        }
        var result = sb.ToString().Trim();
        return result.Length <= maxLength ? result : result[..(maxLength - 1)] + "…";
    }
}
