using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Notifier.Core.Domain;
using Notifier.Web.Alerts;
using Notifier.Web.Channels;
using Notifier.Web.Data;

namespace Notifier.Tests.Alerts;

public sealed class AlertServiceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
    private const string Webhook = "https://hooks.slack.com/services/T000/B000/XXXX";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private readonly SqliteConnection _conn = new("Data Source=:memory:");
    private readonly NotifierDbContext _db;
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(Now));
    private readonly AlertService _alerts;

    public AlertServiceTests()
    {
        _conn.Open();
        _db = new NotifierDbContext(new DbContextOptionsBuilder<NotifierDbContext>().UseSqlite(_conn).Options);
        _db.Database.EnsureCreated();
        var services = new ServiceCollection();
        services.AddNotifierChannels(new ConfigurationBuilder().Build());
        var registry = services.BuildServiceProvider().GetRequiredService<ChannelRegistry>();
        _alerts = new AlertService(_db, registry, _clock);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    private static Dictionary<string, IReadOnlyDictionary<string, string>> Email(string address = "alice@example.com") =>
        new() { ["email"] = new Dictionary<string, string> { ["address"] = address } };

    private static AlertDraft Draft(string name = "Tech news", Tier tier = Tier.All, Topic[]? topics = null, string? keyword = null,
        Dictionary<string, IReadOnlyDictionary<string, string>>? destinations = null) =>
        new(name, tier, topics ?? [Topic.Technology], keyword, destinations ?? Email());

    private async Task<int> CreateOkAsync(string owner = "alice", AlertDraft? draft = null)
    {
        var result = await _alerts.CreateAsync(owner, draft ?? Draft(), Ct);
        Assert.Empty(result.Errors);
        return result.Id!.Value;
    }

    // ---- Create: success ----------------------------------------------------

    [Fact]
    public async Task Valid_draft_creates_an_active_alert_starting_now()
    {
        var id = await CreateOkAsync();

        var alert = _db.Alerts.Include(a => a.Topics).Include(a => a.Destinations).AsSingleQuery().Single(a => a.Id == id);
        Assert.Equal(("alice", "Tech news", Tier.All, true), (alert.OwnerUsername, alert.Name, alert.Tier, alert.Enabled));
        Assert.Equal((Now, Now), (alert.CreatedAtUtc, alert.ActiveSinceUtc));
        Assert.Equal([Topic.Technology], alert.Topics.Select(t => t.Topic));
        var destination = Assert.Single(alert.Destinations);
        Assert.Equal("email", destination.ChannelKey);
        Assert.Equal("alice@example.com", JsonSerializer.Deserialize<Dictionary<string, string>>(destination.ConfigJson)!["address"]);
    }

    [Fact]
    public async Task Values_are_trimmed_and_undeclared_fields_are_dropped()
    {
        var destinations = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["email"] = new Dictionary<string, string> { ["address"] = "  alice@example.com ", ["bcc"] = "x@evil.test" },
        };
        var id = await CreateOkAsync(draft: Draft(name: "  Padded  ", keyword: "  nvidia ", destinations: destinations));

        var alert = _db.Alerts.Include(a => a.Destinations).Single(a => a.Id == id);
        Assert.Equal(("Padded", "nvidia"), (alert.Name, alert.Keyword));
        var config = JsonSerializer.Deserialize<Dictionary<string, string>>(alert.Destinations.Single().ConfigJson)!;
        Assert.Equal(["address"], config.Keys);
        Assert.Equal("alice@example.com", config["address"]);
    }

    [Theory]
    [InlineData(Tier.Headline)]
    [InlineData(Tier.FrontPage)]
    public async Task Topics_are_not_stored_for_front_page_tiers(Tier tier)
    {
        var id = await CreateOkAsync(draft: Draft(tier: tier, topics: [Topic.World]));
        Assert.Empty(_db.AlertTopics.Where(t => t.AlertId == id));
    }

    [Fact]
    public async Task Blank_keyword_is_stored_as_none()
    {
        var id = await CreateOkAsync(draft: Draft(keyword: "   "));
        Assert.Null(_db.Alerts.Single(a => a.Id == id).Keyword);
    }

    [Fact]
    public async Task Email_and_slack_can_both_be_used()
    {
        var destinations = Email();
        destinations["slack"] = new Dictionary<string, string> { ["webhookUrl"] = Webhook };
        var id = await CreateOkAsync(draft: Draft(destinations: destinations));
        Assert.Equal(["email", "slack"], _db.Destinations.Where(d => d.AlertId == id).Select(d => d.ChannelKey).Order());
    }

    // ---- Create: validation ------------------------------------------------

    public static TheoryData<string, AlertDraft> InvalidDrafts => new()
    {
        { "Name", Draft(name: "") },
        { "Name", Draft(name: "   ") },
        { "Name", Draft(name: new string('n', 101)) },
        { "Name", Draft(name: "Line\nbreak") },
        { "Topics", Draft(tier: Tier.All, topics: []) },
        { "Keyword", Draft(keyword: "...") },
        { "Keyword", Draft(keyword: new string('k', 101)) },
        { "Destinations", Draft(destinations: []) },
        { "Destinations[email]", Draft(destinations: Email("not-an-email")) },
        { "Destinations[slack]", Draft(destinations: new() { ["slack"] = new Dictionary<string, string> { ["webhookUrl"] = "https://evil.test/hook" } }) },
        { "Destinations[pigeon]", Draft(destinations: new() { ["pigeon"] = new Dictionary<string, string>() }) },
        { "Tier", Draft(tier: (Tier)42) },
    };

    [Theory]
    [MemberData(nameof(InvalidDrafts))]
    public async Task Invalid_draft_reports_the_field_and_saves_nothing(string field, AlertDraft draft)
    {
        var result = await _alerts.CreateAsync("alice", draft, Ct);

        Assert.Null(result.Id);
        Assert.Contains(field, result.Errors.Keys);
        Assert.Empty(_db.Alerts);
        Assert.Empty(_db.Destinations);
    }

    // ---- List / ownership --------------------------------------------------

    [Fact]
    public async Task List_shows_only_my_alerts_that_are_not_deleted()
    {
        var mine = await CreateOkAsync("alice");
        var deleted = await CreateOkAsync("alice");
        await CreateOkAsync("bob");
        await _alerts.DeleteAsync("alice", deleted, Ct);

        Assert.Equal([mine], (await _alerts.ListAsync("alice", Ct)).Select(a => a.Id));
    }

    [Fact]
    public async Task Other_users_alerts_cannot_be_read_changed_or_deleted()
    {
        var bobs = await CreateOkAsync("bob");

        Assert.Null(await _alerts.GetAsync("alice", bobs, Ct));
        Assert.False(await _alerts.SetEnabledAsync("alice", bobs, false, Ct));
        Assert.False(await _alerts.DeleteAsync("alice", bobs, Ct));
        var alert = _db.Alerts.Single();
        Assert.True(alert.Enabled);
        Assert.Null(alert.DeletedAtUtc);
    }

    // ---- Enable / disable / delete -------------------------------------------

    [Fact]
    public async Task Re_enabling_moves_active_since_so_nothing_is_backfilled()
    {
        var id = await CreateOkAsync();
        _clock.Advance(TimeSpan.FromHours(1));
        await _alerts.SetEnabledAsync("alice", id, false, Ct);
        _clock.Advance(TimeSpan.FromHours(2));
        await _alerts.SetEnabledAsync("alice", id, true, Ct);

        var alert = _db.Alerts.Single();
        Assert.True(alert.Enabled);
        Assert.Equal(Now, alert.CreatedAtUtc);
        Assert.Equal(Now.AddHours(3), alert.ActiveSinceUtc);
    }

    [Fact]
    public async Task Enabling_an_already_enabled_alert_does_not_move_active_since()
    {
        var id = await CreateOkAsync();
        _clock.Advance(TimeSpan.FromHours(1));
        await _alerts.SetEnabledAsync("alice", id, true, Ct);
        Assert.Equal(Now, _db.Alerts.Single().ActiveSinceUtc);
    }

    [Fact]
    public async Task Delete_is_soft_and_deleted_alerts_cannot_be_changed()
    {
        var id = await CreateOkAsync();
        Assert.True(await _alerts.DeleteAsync("alice", id, Ct));

        Assert.Equal(Now, _db.Alerts.Single().DeletedAtUtc);
        Assert.Null(await _alerts.GetAsync("alice", id, Ct));
        Assert.False(await _alerts.SetEnabledAsync("alice", id, true, Ct));
        Assert.False(await _alerts.DeleteAsync("alice", id, Ct));
    }
}
