using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Notifier.Core.Channels;
using Notifier.Core.Domain;
using Notifier.Core.Sources;
using Notifier.Web.Channels;
using Notifier.Web.Data;
using Notifier.Web.Polling;

namespace Notifier.Tests.Polling;

/// <summary>Real poll/dispatch/retention services over in-memory SQLite, a scripted source and a recording channel.</summary>
public sealed class PollHarness : IDisposable
{
    public static readonly DateTime Start = new(2026, 11, 25, 8, 0, 0, DateTimeKind.Utc);
    private readonly SqliteConnection _conn = new("Data Source=:memory:");
    private readonly ServiceProvider _sp;

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(Start));
    public ScriptedSource Source { get; } = new();
    public RecordingChannel Channel { get; } = new();

    public PollHarness(Dictionary<string, string?>? config = null)
    {
        _conn.Open();
        var settings = new Dictionary<string, string?>
        {
            ["Notifier:Polling:Enabled"] = "false",
            ["Notifier:Polling:RetryDelay"] = "00:00:00",
        };
        foreach (var (k, v) in config ?? []) settings[k] = v;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<NotifierDbContext>(o => o.UseSqlite(_conn));
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<IArticleSource>(Source);
        services.AddSingleton<INotificationChannel>(Channel);
        services.AddSingleton<ChannelRegistry>();
        services.AddNotifierPolling(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        _sp = services.BuildServiceProvider();

        using var scope = _sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<NotifierDbContext>().Database.EnsureCreated();
    }

    public void Dispose()
    {
        _sp.Dispose();
        _conn.Dispose();
    }

    public async Task<PollSummary> PollAsync()
    {
        using var scope = _sp.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PollService>().RunCycleAsync(TestContext.Current.CancellationToken);
    }

    public T Db<T>(Func<NotifierDbContext, T> query)
    {
        using var scope = _sp.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<NotifierDbContext>());
    }

    public T Service<T>() where T : notnull => _sp.GetRequiredService<T>();

    public Alert AddAlert(Tier tier, string? keyword = null, int destinations = 1, params Topic[] topics)
    {
        var alert = new Alert
        {
            OwnerUsername = "u", Name = $"{tier} alert", Tier = tier, Keyword = keyword,
            CreatedAtUtc = Clock.GetUtcNow().UtcDateTime,
            Topics = [.. topics.Select(t => new AlertTopic { Topic = t })],
            Destinations = [.. Enumerable.Range(1, destinations).Select(i => new Destination
                { ChannelKey = RecordingChannel.ChannelKey, ConfigJson = $$"""{"to":"d{{i}}"}""" })],
        };
        Db(db => { db.Alerts.Add(alert); db.SaveChanges(); return 0; });
        Clock.Advance(TimeSpan.FromSeconds(1)); // articles seen from now on are "after" the alert
        return alert;
    }

    public static SourceItem Item(string key, int position = 0, int section = 0, DateTime? published = null, string? title = null) =>
        new(key, title ?? $"Title {key}", $"Summary {key}", new Uri($"https://www.bbc.co.uk/news/articles/{key}"),
            published ?? Start, section, position);
}

public sealed class ScriptedSource : IArticleSource
{
    public Dictionary<Topic, IReadOnlyList<SourceItem>> Feeds { get; } = [];
    public HashSet<Topic> Failing { get; } = [];
    public List<Topic> Fetched { get; } = [];
    public string Attribution => "Source: Scripted";
    public IReadOnlyList<Topic> SupportedTopics => [Topic.TopStories, Topic.World, Topic.Technology];

    public Task<SourceFetchResult> FetchAsync(Topic topic, CancellationToken ct)
    {
        Fetched.Add(topic);
        if (Failing.Contains(topic)) throw new SourceException($"{topic} is down");
        return Task.FromResult(new SourceFetchResult(topic, Feeds.GetValueOrDefault(topic) ?? []));
    }

    public void Set(Topic topic, params SourceItem[] items) => Feeds[topic] = items;
}

public sealed class RecordingChannel : INotificationChannel
{
    public const string ChannelKey = "rec";
    public List<(Notification N, string To)> Sent { get; } = [];
    public Queue<SendResult> Results { get; } = [];
    public string Key => ChannelKey;
    public string DisplayName => "Recorder";
    public IReadOnlyList<ChannelField> ConfigFields => [];
    public IReadOnlyList<string> Validate(IReadOnlyDictionary<string, string> config) => [];
    public string Describe(IReadOnlyDictionary<string, string> config) => "rec";

    public Task<SendResult> SendAsync(Notification n, IReadOnlyDictionary<string, string> config, CancellationToken ct)
    {
        var result = Results.Count > 0 ? Results.Dequeue() : SendResult.Ok();
        if (result.Success) Sent.Add((n, config["to"]));
        return Task.FromResult(result);
    }
}
