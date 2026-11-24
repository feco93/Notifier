using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Notifier.Core.Channels;
using Notifier.Core.Domain;
using Notifier.Core.Sources;
using Notifier.Web.Channels;
using Notifier.Web.Data;
using Notifier.Web.Sending;

namespace Notifier.Tests.Sending;

public sealed class TestSendServiceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private readonly SqliteConnection _conn = new("Data Source=:memory:");
    private readonly NotifierDbContext _db;
    private readonly RecordingChannel _channel = new();

    public TestSendServiceTests()
    {
        _conn.Open();
        _db = new NotifierDbContext(new DbContextOptionsBuilder<NotifierDbContext>().UseSqlite(_conn).Options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    private sealed class RecordingChannel : INotificationChannel
    {
        public List<(Notification N, IReadOnlyDictionary<string, string> Config)> Sent { get; } = [];
        public SendResult Result { get; set; } = SendResult.Ok();
        public Exception? Throw { get; set; }
        public string Key => "rec";
        public string DisplayName => "Recorder";
        public IReadOnlyList<ChannelField> ConfigFields => [];
        public IReadOnlyList<string> Validate(IReadOnlyDictionary<string, string> config) => [];
        public string Describe(IReadOnlyDictionary<string, string> config) => "rec";
        public Task<SendResult> SendAsync(Notification n, IReadOnlyDictionary<string, string> config, CancellationToken ct)
        {
            if (Throw is not null) throw Throw;
            Sent.Add((n, config));
            return Task.FromResult(Result);
        }
    }

    private sealed class StubSource : IArticleSource
    {
        public string Attribution => "Source: Stub";
        public IReadOnlyList<Topic> SupportedTopics => [];
        public Task<SourceFetchResult> FetchAsync(Topic topic, CancellationToken ct) => throw new NotSupportedException();
    }

    private (TestSendService service, Alert alert) Setup(string channelKey = "rec", string configJson = """{"to":"x"}""")
    {
        var alert = new Alert { OwnerUsername = "u", Name = "My alert", Tier = Tier.Headline, CreatedAtUtc = Now,
            Destinations = [new Destination { ChannelKey = channelKey, ConfigJson = configJson }] };
        _db.Alerts.Add(alert);
        _db.SaveChanges();
        var service = new TestSendService(new ChannelRegistry([_channel]), _db, new FakeTimeProvider(new DateTimeOffset(Now)), new StubSource());
        return (service, alert);
    }

    [Fact]
    public async Task Sends_a_tagged_test_notification_through_the_channel()
    {
        var (service, alert) = Setup();
        var result = await service.SendAsync(alert, alert.Destinations[0], Ct);

        Assert.True(result.Success);
        var (n, config) = Assert.Single(_channel.Sent);
        Assert.True(n.IsTest);
        Assert.Equal("My alert", n.AlertName);
        Assert.Equal("Source: Stub", n.Attribution);
        Assert.Equal("x", config["to"]);
    }

    [Fact]
    public async Task Records_a_sent_test_delivery_without_an_article()
    {
        var (service, alert) = Setup();
        await service.SendAsync(alert, alert.Destinations[0], Ct);

        var d = Assert.Single(_db.Deliveries);
        Assert.Equal(DeliveryKind.Test, d.Kind);
        Assert.Equal(DeliveryStatus.Sent, d.Status);
        Assert.Null(d.ArticleKey);
        Assert.Equal(Now, d.CreatedAtUtc);
    }

    [Fact]
    public async Task Records_the_channel_error_on_failure()
    {
        var (service, alert) = Setup();
        _channel.Result = SendResult.Failed("Slack returned HTTP 404: channel_not_found");
        var result = await service.SendAsync(alert, alert.Destinations[0], Ct);

        Assert.False(result.Success);
        var d = Assert.Single(_db.Deliveries);
        Assert.Equal(DeliveryStatus.Failed, d.Status);
        Assert.Equal("Slack returned HTTP 404: channel_not_found", d.LastError);
    }

    [Theory]
    [InlineData("unknown-channel", """{"to":"x"}""")]
    [InlineData("rec", "not json")]
    [InlineData("rec", "null")]
    public async Task Bad_destinations_fail_cleanly_and_are_recorded(string channelKey, string configJson)
    {
        var (service, alert) = Setup(channelKey, configJson);
        var result = await service.SendAsync(alert, alert.Destinations[0], Ct);

        Assert.False(result.Success);
        Assert.Empty(_channel.Sent);
        Assert.Equal(DeliveryStatus.Failed, Assert.Single(_db.Deliveries).Status);
    }

    [Fact]
    public async Task Destination_of_another_alert_is_rejected()
    {
        var (service, alert) = Setup();
        var (_, other) = Setup();
        await Assert.ThrowsAsync<ArgumentException>(() => service.SendAsync(alert, other.Destinations[0], Ct));
        Assert.Empty(_channel.Sent);
        Assert.Empty(_db.Deliveries);
    }

    [Fact]
    public async Task Deleted_alert_cannot_be_test_sent()
    {
        var (service, alert) = Setup();
        alert.DeletedAtUtc = Now;
        _db.SaveChanges();

        var result = await service.SendAsync(alert, alert.Destinations[0], Ct);
        Assert.False(result.Success);
        Assert.Empty(_channel.Sent);
    }

    [Fact]
    public async Task Channel_that_throws_still_yields_a_recorded_failure()
    {
        var (service, alert) = Setup();
        _channel.Throw = new InvalidOperationException("boom");

        var result = await service.SendAsync(alert, alert.Destinations[0], Ct);
        Assert.False(result.Success);
        Assert.Contains("boom", result.Error);
        Assert.Equal(DeliveryStatus.Failed, Assert.Single(_db.Deliveries).Status);
    }

    [Fact]
    public async Task Test_sends_can_be_repeated()
    {
        var (service, alert) = Setup();
        await service.SendAsync(alert, alert.Destinations[0], Ct);
        await service.SendAsync(alert, alert.Destinations[0], Ct);
        Assert.Equal(2, _db.Deliveries.Count());
    }
}
