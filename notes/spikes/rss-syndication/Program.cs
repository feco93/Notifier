// Spike: does System.ServiceModel.Syndication handle BBC News RSS correctly?
// Usage: dotnet run -- <dir containing bbc-*.xml>
// Checks: item count & order preserved, pubDate -> UTC, guid/link/summary access,
// media:thumbnail extension visible, and behaviour on malformed synthetic input.
using System.ServiceModel.Syndication;
using System.Xml;

var dir = args.Length > 0 ? args[0] : ".";
foreach (var path in Directory.GetFiles(dir, "bbc-*.xml").Order())
{
    using var reader = XmlReader.Create(path);
    var feed = SyndicationFeed.Load(reader);
    var items = feed.Items.ToList();
    var first = items[0];
    Console.WriteLine($"{Path.GetFileName(path),-40} items={items.Count,3} feedTitle='{feed.Title?.Text}'");
    Console.WriteLine($"   first: id='{first.Id}'");
    Console.WriteLine($"          link='{first.Links.FirstOrDefault()?.Uri}'");
    Console.WriteLine($"          published={first.PublishDate:O} (offset {first.PublishDate.Offset})");
    Console.WriteLine($"          summaryLen={first.Summary?.Text?.Length} extensions=[{string.Join(",", first.ElementExtensions.Select(e => e.OuterNamespace + ":" + e.OuterName))}]");
}

Console.WriteLine("\n--- synthetic edge cases ---");
string Wrap(string item) =>
    $"<?xml version=\"1.0\"?><rss version=\"2.0\" xmlns:media=\"http://search.yahoo.com/mrss/\"><channel><title>T</title><link>https://example.test</link><description>d</description>{item}</channel></rss>";

var cases = new Dictionary<string, string>
{
    ["valid GMT date"] = "<item><title>a</title><link>https://www.bbc.co.uk/news/articles/abc123?at_medium=RSS#0</link><guid isPermaLink=\"false\">https://www.bbc.co.uk/news/articles/abc123#0</guid><pubDate>Tue, 24 Nov 2026 11:38:52 GMT</pubDate></item>",
    ["numeric offset date"] = "<item><title>b</title><pubDate>Tue, 24 Nov 2026 11:38:52 +0100</pubDate></item>",
    ["missing pubDate"] = "<item><title>c</title></item>",
    ["garbage pubDate"] = "<item><title>d</title><pubDate>yesterday-ish</pubDate></item>",
    ["missing title"] = "<item><pubDate>Tue, 24 Nov 2026 11:38:52 GMT</pubDate></item>",
};
Console.WriteLine("\n--- same cases with a lenient DateTimeParser on Rss20FeedFormatter ---");
foreach (var (name, item) in cases)
{
    try
    {
        var formatter = new Rss20FeedFormatter();
        // Lenient: unparseable dates become MinValue (= "unknown") instead of failing the whole feed.
        // NOTE: first attempt used a 4-arg signature from model memory -> CS1593. Real signature
        // (from the package's XML docs): bool (XmlDateTimeData data, out DateTimeOffset result).
        formatter.DateTimeParser = (XmlDateTimeData data, out DateTimeOffset result) =>
        {
            var ok = DateTimeOffset.TryParse(data.DateTimeString.Replace("GMT", "+0000"), System.Globalization.CultureInfo.InvariantCulture,
                                             System.Globalization.DateTimeStyles.AssumeUniversal, out result);
            if (!ok) result = DateTimeOffset.MinValue;
            return true;
        };
        formatter.ReadFrom(XmlReader.Create(new StringReader(Wrap(item))));
        var it = formatter.Feed.Items.Single();
        Console.WriteLine($"{name,-22} OK   published={it.PublishDate:O}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{name,-22} THROWS {ex.GetType().Name}: {ex.Message.Split('\n')[0]}");
    }
}

Console.WriteLine("\n--- default parser ---");
foreach (var (name, item) in cases)
{
    try
    {
        var feed = SyndicationFeed.Load(XmlReader.Create(new StringReader(Wrap(item))));
        var it = feed.Items.Single();
        Console.WriteLine($"{name,-22} OK   title='{it.Title?.Text}' published={it.PublishDate:O} id='{it.Id}'");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{name,-22} THROWS {ex.GetType().Name}: {ex.Message.Split('\n')[0]}");
    }
}
