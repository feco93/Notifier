using Notifier.Core.Domain;

namespace Notifier.Sources.Bbc;

internal sealed class BbcOptions
{
    public const string Section = "Notifier:Bbc";

    public Dictionary<Topic, string> Feeds { get; set; } = new()
    {
        [Topic.TopStories] = "https://feeds.bbci.co.uk/news/rss.xml",
        [Topic.World] = "https://feeds.bbci.co.uk/news/world/rss.xml",
        [Topic.Business] = "https://feeds.bbci.co.uk/news/business/rss.xml",
        [Topic.ScienceEnvironment] = "https://feeds.bbci.co.uk/news/science_and_environment/rss.xml",
        [Topic.Technology] = "https://feeds.bbci.co.uk/news/technology/rss.xml",
    };

    public string UserAgent { get; set; } = "NotifierPrototype/0.1 (personal non-commercial use)";

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(20);
}
