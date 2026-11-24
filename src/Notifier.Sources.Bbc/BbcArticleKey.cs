namespace Notifier.Sources.Bbc;

/// <summary>
/// The last path segment identifies a BBC article across feeds; the query (tracking) and the
/// fragment (a per-feed suffix such as #0, #1) differ between feeds for the same article.
/// </summary>
internal static class BbcArticleKey
{
    public static string? From(Uri? url)
    {
        if (url is null || !url.IsAbsoluteUri) return null;
        var segments = url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0 ? null : segments[^1];
    }

    public static string? From(string? urlText) =>
        Uri.TryCreate(urlText, UriKind.Absolute, out var url) ? From(url) : null;
}
