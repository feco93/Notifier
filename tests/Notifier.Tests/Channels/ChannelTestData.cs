using Notifier.Core.Channels;

namespace Notifier.Tests.Channels;

internal static class ChannelTestData
{
    public static NotificationItem Item(int i = 1, string? title = null, string? summary = null, string? reason = null) =>
        new(title ?? $"Story {i}", summary ?? $"Summary {i}.", new Uri($"https://www.bbc.co.uk/news/articles/a{i}"), reason ?? $"#{i} in Top stories");

    public static Notification Single(NotificationItem? item = null, bool test = false, bool synthetic = false) =>
        new("Tech headlines", [item ?? Item()], 0, "Source: BBC News", test, synthetic);

    public static Notification Batch(int count, int more = 0) =>
        new("Tech headlines", [.. Enumerable.Range(1, count).Select(i => Item(i))], more, "Source: BBC News", false, false);

    public static Dictionary<string, string> Config(params (string Key, string Value)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => p.Value);
}
