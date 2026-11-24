using System.Text;
using System.Xml;
using Notifier.Sources.Bbc;

namespace Notifier.Tests.Sources;

public sealed class BbcFeedParserTests
{
    private static readonly IReadOnlyList<Notifier.Core.Sources.SourceItem> Items = ParseFixture();

    private static IReadOnlyList<Notifier.Core.Sources.SourceItem> ParseFixture()
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "bbc-shaped-feed.xml"));
        return BbcFeedParser.Parse(stream);
    }

    private static Notifier.Core.Sources.SourceItem Item(string key) => Items.Single(i => i.Key == key);

    [Fact]
    public void Splits_sections_on_guid_marker_changes_and_keeps_raw_positions_within_each()
    {
        // The duplicate art222 and the unidentifiable item in section 1 are skipped but still counted.
        Assert.Equal(
            [("vid111", 0, 0), ("art222", 0, 1), ("wea333", 0, 2), ("art444", 0, 3), ("art555", 0, 4), ("art666", 0, 5),
             ("art777", 1, 0), ("art999", 2, 0)],
            Items.Select(i => (i.Key, i.Section, i.Position)));
    }

    [Fact]
    public void Duplicate_keeps_the_copy_from_the_earlier_section()
    {
        Assert.Equal(0, Item("art222").Section);
    }

    [Fact]
    public void Feed_without_section_markers_is_a_single_section()
    {
        const string xml = """
            <rss version="2.0"><channel><title>t</title><link>https://example.test</link><description>d</description>
            <item><title>A</title><link>https://www.bbc.co.uk/news/articles/a1</link><guid>https://www.bbc.co.uk/news/articles/a1</guid></item>
            <item><title>B</title><link>https://www.bbc.co.uk/news/articles/b2</link></item>
            <item><title>C</title><link>https://www.bbc.co.uk/news/articles/c3</link><guid>c3-no-marker</guid></item>
            </channel></rss>
            """;
        var items = BbcFeedParser.Parse(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
        Assert.Equal([("a1", 0, 0), ("b2", 0, 1), ("c3", 0, 2)], items.Select(i => (i.Key, i.Section, i.Position)));
    }

    [Fact]
    public void Gmt_dates_are_utc()
    {
        var published = Item("vid111").PublishedAtUtc!.Value;
        Assert.Equal(new DateTime(2026, 11, 24, 1, 21, 41, DateTimeKind.Utc), published);
        Assert.Equal(DateTimeKind.Utc, published.Kind);
    }

    [Fact]
    public void Numeric_offsets_are_converted_to_utc()
    {
        Assert.Equal(new DateTime(2026, 11, 24, 10, 38, 52, DateTimeKind.Utc), Item("art444").PublishedAtUtc);
    }

    [Theory]
    [InlineData("art555")]
    [InlineData("art666")]
    public void Missing_or_unparseable_dates_become_null_without_failing_the_feed(string key)
    {
        Assert.Null(Item(key).PublishedAtUtc);
    }

    [Fact]
    public void Item_without_link_uses_its_guid()
    {
        Assert.Equal("https://www.bbc.co.uk/news/articles/art777#1", Item("art777").Url.OriginalString);
    }

    [Fact]
    public void Url_keeps_the_feed_link()
    {
        Assert.Equal("https://www.bbc.co.uk/news/articles/art222?at_medium=RSS&at_campaign=rss", Item("art222").Url.OriginalString);
    }

    [Fact]
    public void Whitespace_in_text_is_collapsed()
    {
        var item = Item("art999");
        Assert.Equal("Multi-line title with spaces", item.Title);
        Assert.Equal("", item.Summary);
    }

    [Theory]
    [InlineData("<link>javascript:alert(1)</link>")]
    [InlineData("<guid isPermaLink=\"false\">urn:uuid:6e8bc430-9c3a-11d9-9669-0800200c9a66</guid>")]
    [InlineData("<link>ftp://www.bbc.co.uk/news/articles/abc123</link>")]
    public void Items_without_an_http_url_are_skipped(string urlElement)
    {
        var xml = $"""
            <rss version="2.0"><channel><title>t</title><link>https://example.test</link><description>d</description>
            <item><title>Bad</title>{urlElement}<pubDate>Tue, 24 Nov 2026 10:00:00 GMT</pubDate></item>
            <item><title>Good</title><link>https://www.bbc.co.uk/news/articles/good1</link></item>
            </channel></rss>
            """;
        var items = BbcFeedParser.Parse(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
        Assert.Equal([("good1", 0, 1)], items.Select(i => (i.Key, i.Section, i.Position)));
    }

    [Fact]
    public void Dtd_is_rejected()
    {
        const string xml = """
            <?xml version="1.0"?>
            <!DOCTYPE rss [ <!ENTITY x "expanded"> ]>
            <rss version="2.0"><channel><title>&x;</title><link>https://example.test</link><description>d</description></channel></rss>
            """;
        Assert.Throws<XmlException>(() => BbcFeedParser.Parse(new MemoryStream(Encoding.UTF8.GetBytes(xml))));
    }

    [Fact]
    public void Non_rss_document_is_rejected()
    {
        Assert.Throws<XmlException>(() =>
            BbcFeedParser.Parse(new MemoryStream(Encoding.UTF8.GetBytes("<html><body>Not a feed</body></html>"))));
    }
}
