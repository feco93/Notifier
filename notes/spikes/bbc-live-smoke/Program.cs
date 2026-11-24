using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifier.Core.Sources;
using Notifier.Sources.Bbc;

var sp = new ServiceCollection().AddBbcNewsSource(new ConfigurationBuilder().Build()).BuildServiceProvider();
var source = sp.GetRequiredService<IArticleSource>();
foreach (var topic in source.SupportedTopics)
{
    try
    {
        var r = await source.FetchAsync(topic, CancellationToken.None);
        var keys = r.Items.Select(i => i.Key).ToList();
        Console.WriteLine($"{topic,-20} items={r.Items.Count,3} unknownDates={r.Items.Count(i => i.PublishedAtUtc is null)} " +
            $"distinctKeys={keys.Distinct().Count() == keys.Count} " +
            $"allUtc={r.Items.All(i => i.PublishedAtUtc is null || i.PublishedAtUtc.Value.Kind == DateTimeKind.Utc)} " +
            $"keyShapeOk={keys.All(k => k.Length >= 8 && k.All(char.IsLetterOrDigit))} " +
            $"sections={r.Items.Select(i => i.Section).Distinct().Count()} leadSize={r.Items.Count(i => i.Section == 0)}");
    }
    catch (SourceException ex) { Console.WriteLine($"{topic,-20} FAILED: {ex.Message}"); }
}
