using System.Globalization;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using Notifier.Core.Sources;

namespace Notifier.Sources.Bbc;

internal static partial class BbcFeedParser
{
    private static readonly XmlReaderSettings ReaderSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersInDocument = 5_000_000,
    };

    public static IReadOnlyList<SourceItem> Parse(Stream xml)
    {
        using var reader = XmlReader.Create(xml, ReaderSettings);
        var formatter = new Rss20FeedFormatter { DateTimeParser = TryParseLenient };
        formatter.ReadFrom(reader);

        var items = new List<SourceItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? currentMarker = null;
        var section = 0;
        var position = -1;
        foreach (var item in formatter.Feed.Items)
        {
            // A BBC feed concatenates page modules; the guid's #n marks the module. A change of marker
            // starts a new section. Items without a marker stay in the current section.
            var marker = SectionMarker(item.Id);
            if (marker is not null && currentMarker is not null && marker != currentMarker)
            {
                section++;
                position = -1;
            }
            currentMarker = marker ?? currentMarker;
            position++; // counts skipped items too, so they don't shift the ranks of later ones

            var url = item.Links.FirstOrDefault()?.Uri
                      ?? (Uri.TryCreate(item.Id, UriKind.Absolute, out var fromGuid) ? fromGuid : null);
            // The URL ends up as a link in notifications, so only web links are accepted.
            if (url is not { IsAbsoluteUri: true } || (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp))
                continue;
            var key = BbcArticleKey.From(url);
            if (key is null || !seen.Add(key)) continue;

            var title = Clean(item.Title?.Text);
            items.Add(new SourceItem(
                key,
                title.Length > 0 ? title : "(untitled)",
                Clean(item.Summary?.Text),
                url,
                item.PublishDate == DateTimeOffset.MinValue ? null : item.PublishDate.UtcDateTime,
                section,
                position));
        }
        return items;
    }

    private static string? SectionMarker(string? guid)
    {
        var hash = guid?.LastIndexOf('#') ?? -1;
        return hash >= 0 && hash < guid!.Length - 1 ? guid[(hash + 1)..] : null;
    }

    // The default parser throws on one bad date and loses the whole feed; treat it as unknown instead.
    private static bool TryParseLenient(XmlDateTimeData data, out DateTimeOffset result)
    {
        var text = data.DateTimeString.Trim().Replace(" GMT", " +0000", StringComparison.OrdinalIgnoreCase);
        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result))
            result = DateTimeOffset.MinValue;
        return true;
    }

    private static string Clean(string? text) =>
        text is null ? "" : Whitespace().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
