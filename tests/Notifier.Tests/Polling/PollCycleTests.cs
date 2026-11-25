using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notifier.Core.Channels;
using Notifier.Core.Domain;
using Notifier.Web.Polling;
using static Notifier.Tests.Polling.PollHarness;

namespace Notifier.Tests.Polling;

public sealed class PollCycleTests : IDisposable
{
    private readonly PollHarness _h = new();
    public void Dispose() => _h.Dispose();

    // ---- Ingestion -----------------------------------------------------------

    [Fact]
    public async Task Feeds_are_fetched_one_after_another_and_articles_stored_with_topics()
    {
        _h.Source.Set(Topic.World, Item("w1"), Item("both", 1));
        _h.Source.Set(Topic.Technology, Item("both"));

        await _h.PollAsync();

        Assert.Equal([Topic.TopStories, Topic.World, Topic.Technology], _h.Source.Fetched);
        var both = _h.Db(db => db.Articles.Include(a => a.Topics).Single(a => a.Key == "both"));
        Assert.Equal([Topic.World, Topic.Technology], both.Topics.Select(t => t.Topic).Order());
    }

    [Fact]
    public async Task First_seen_is_set_once_and_last_seen_moves_on()
    {
        _h.Source.Set(Topic.World, Item("w1"));
        await _h.PollAsync();
        _h.Clock.Advance(TimeSpan.FromMinutes(15));
        await _h.PollAsync();

        var a = _h.Db(db => db.Articles.Single());
        Assert.Equal(Start, a.FirstSeenAtUtc);
        Assert.Equal(Start.AddMinutes(15), a.LastSeenAtUtc);
    }

    [Fact]
    public async Task Lead_section_gets_a_rank_and_later_sections_are_only_on_top_stories()
    {
        _h.Source.Set(Topic.TopStories, Item("lead1", 0), Item("lead2", 1), Item("later", 0, section: 1));
        await _h.PollAsync();

        var byKey = _h.Db(db => db.Articles.ToDictionary(a => a.Key));
        Assert.Equal((true, 1), (byKey["lead1"].OnTopStories, byKey["lead1"].TopStoriesRank));
        Assert.Equal((true, 2), (byKey["lead2"].OnTopStories, byKey["lead2"].TopStoriesRank));
        Assert.Equal((true, (int?)null), (byKey["later"].OnTopStories, byKey["later"].TopStoriesRank));
    }

    [Fact]
    public async Task Article_leaving_top_stories_loses_its_rank()
    {
        _h.Source.Set(Topic.TopStories, Item("a", 0), Item("b", 1));
        await _h.PollAsync();
        _h.Source.Set(Topic.TopStories, Item("b", 0));
        await _h.PollAsync();

        var a = _h.Db(db => db.Articles.Single(x => x.Key == "a"));
        Assert.False(a.OnTopStories);
        Assert.Null(a.TopStoriesRank);
        Assert.Equal(1, _h.Db(db => db.Articles.Single(x => x.Key == "b").TopStoriesRank));
    }

    [Fact]
    public async Task Failed_top_stories_fetch_keeps_existing_ranks()
    {
        _h.Source.Set(Topic.TopStories, Item("a", 0));
        await _h.PollAsync();
        _h.Source.Failing.Add(Topic.TopStories);
        await _h.PollAsync();

        Assert.Equal(1, _h.Db(db => db.Articles.Single(x => x.Key == "a").TopStoriesRank));
    }

    [Fact]
    public async Task One_failing_feed_does_not_stop_the_others_and_is_recorded()
    {
        _h.Source.Failing.Add(Topic.World);
        _h.Source.Set(Topic.Technology, Item("t1"));

        var summary = await _h.PollAsync();

        var status = _h.Db(db => db.FeedStatuses.ToDictionary(f => f.Topic));
        Assert.Equal("World is down", status[Topic.World].LastError);
        Assert.Null(status[Topic.World].LastSuccessAtUtc);
        Assert.Null(status[Topic.Technology].LastError);
        Assert.Equal(1, status[Topic.Technology].ItemsLastFetch);
        Assert.Equal(1, status[Topic.Technology].NewLastFetch);
        Assert.Equal(Start.AddMinutes(15), status[Topic.Technology].NextPollAtUtc);
        Assert.Single(_h.Db(db => db.Articles.ToList()));
        Assert.Equal(1, summary.FailedFeeds);

        _h.Clock.Advance(TimeSpan.FromMinutes(15));
        await _h.PollAsync();
        var again = _h.Db(db => db.FeedStatuses.Single(f => f.Topic == Topic.Technology));
        Assert.Equal((1, 0), (again.ItemsLastFetch, again.NewLastFetch));
    }

    // ---- Dispatch ------------------------------------------------------------

    [Fact]
    public async Task New_matching_article_is_sent_once_and_never_again()
    {
        _h.AddAlert(Tier.All, topics: Topic.World);
        _h.Source.Set(Topic.World, Item("w1"));

        await _h.PollAsync();
        await _h.PollAsync();

        var (n, to) = Assert.Single(_h.Channel.Sent);
        Assert.Equal("d1", to);
        Assert.Equal("Title w1", Assert.Single(n.Items).Title);
        Assert.Equal("New in World", n.Items[0].Reason);
        Assert.Equal("Source: Scripted", n.Attribution);
        var d = _h.Db(db => db.Deliveries.Single());
        Assert.Equal((DeliveryStatus.Sent, 1, "New in World"), (d.Status, d.Attempts, d.Reason));
    }

    [Fact]
    public async Task Article_seen_before_the_alert_existed_is_not_sent()
    {
        _h.Source.Set(Topic.World, Item("w1"));
        await _h.PollAsync();
        _h.AddAlert(Tier.All, topics: Topic.World);
        await _h.PollAsync();

        Assert.Empty(_h.Channel.Sent);
    }

    [Fact]
    public async Task Article_climbing_into_the_headline_cut_fires_on_that_cycle()
    {
        _h.AddAlert(Tier.Headline);
        _h.Source.Set(Topic.TopStories, Item("x0", 0), Item("x1", 1), Item("x2", 2), Item("x3", 3), Item("x4", 4), Item("climber", 5));
        await _h.PollAsync();
        var afterFirst = _h.Channel.Sent.Single().N.Items.Select(i => i.Title).ToList();
        Assert.DoesNotContain("Title climber", afterFirst);

        _h.Source.Set(Topic.TopStories, Item("climber", 0), Item("x0", 1), Item("x1", 2), Item("x2", 3), Item("x3", 4), Item("x4", 5));
        await _h.PollAsync();

        Assert.Equal(2, _h.Channel.Sent.Count);
        Assert.Equal("Title climber", Assert.Single(_h.Channel.Sent[1].N.Items).Title);
    }

    [Fact]
    public async Task Several_matches_in_one_cycle_arrive_as_one_batch()
    {
        _h.AddAlert(Tier.All, topics: Topic.World);
        _h.Source.Set(Topic.World, Item("a"), Item("b", 1), Item("c", 2));

        await _h.PollAsync();

        var (n, _) = Assert.Single(_h.Channel.Sent);
        Assert.Equal(3, n.Items.Count);
        Assert.Equal(0, n.MoreCount);
        var batches = _h.Db(db => db.Deliveries.Select(d => d.BatchId).Distinct().ToList());
        Assert.NotNull(Assert.Single(batches));
    }

    [Fact]
    public async Task Batch_is_capped_and_every_row_is_marked_sent()
    {
        _h.AddAlert(Tier.All, topics: Topic.World);
        _h.Source.Set(Topic.World, [.. Enumerable.Range(0, 25).Select(i => Item($"k{i}", i))]);

        await _h.PollAsync();
        await _h.PollAsync();

        var (n, _) = Assert.Single(_h.Channel.Sent);
        Assert.Equal((20, 5), (n.Items.Count, n.MoreCount));
        Assert.All(_h.Db(db => db.Deliveries.ToList()), d => Assert.Equal(DeliveryStatus.Sent, d.Status));
    }

    [Fact]
    public async Task Headline_batch_lists_items_in_rank_order()
    {
        _h.AddAlert(Tier.Headline);
        // Keys and publish times deliberately sort against the rank.
        _h.Source.Set(Topic.TopStories,
            Item("zz", 0, published: Start.AddHours(-3)),
            Item("mm", 1, published: Start.AddHours(-2)),
            Item("aa", 2, published: Start.AddHours(-1)));
        await _h.PollAsync();

        Assert.Equal(["#1 in Top stories", "#2 in Top stories", "#3 in Top stories"],
            _h.Channel.Sent.Single().N.Items.Select(i => i.Reason));
    }

    [Fact]
    public async Task Each_destination_gets_its_own_message()
    {
        _h.AddAlert(Tier.All, destinations: 2, topics: Topic.World);
        _h.Source.Set(Topic.World, Item("w1"));
        await _h.PollAsync();

        Assert.Equal(["d1", "d2"], _h.Channel.Sent.Select(s => s.To).Order());
        Assert.Equal(2, _h.Db(db => db.Deliveries.Count()));
    }

    [Fact]
    public async Task Failed_send_is_retried_once_and_then_succeeds()
    {
        _h.AddAlert(Tier.All, topics: Topic.World);
        _h.Source.Set(Topic.World, Item("w1"));
        _h.Channel.Results.Enqueue(SendResult.Failed("temporary"));

        await _h.PollAsync();

        var d = _h.Db(db => db.Deliveries.Single());
        Assert.Equal((DeliveryStatus.Sent, 2), (d.Status, d.Attempts));
        Assert.Single(_h.Channel.Sent);
    }

    [Fact]
    public async Task Second_failure_marks_the_batch_failed_and_it_is_not_retried_next_cycle()
    {
        _h.AddAlert(Tier.All, topics: Topic.World);
        _h.Source.Set(Topic.World, Item("w1"));
        _h.Channel.Results.Enqueue(SendResult.Failed("first"));
        _h.Channel.Results.Enqueue(SendResult.Failed("second"));

        await _h.PollAsync();
        await _h.PollAsync();

        var d = _h.Db(db => db.Deliveries.Single());
        Assert.Equal((DeliveryStatus.Failed, 2, "second"), (d.Status, d.Attempts, d.LastError));
        Assert.Empty(_h.Channel.Sent);
    }

    [Fact]
    public async Task Pending_rows_left_by_a_crash_are_sent_on_the_next_cycle_with_their_reason()
    {
        var alert = _h.AddAlert(Tier.All, topics: Topic.World);
        _h.Source.Set(Topic.World, Item("w1"));
        await _h.PollAsync();
        _h.Db(db =>
        {
            var d = db.Deliveries.Single();
            d.Status = DeliveryStatus.Pending;
            d.Reason = "Reason stored before the crash";
            db.SaveChanges();
            return 0;
        });

        _h.Clock.Advance(TimeSpan.FromHours(30)); // the article is no longer fresh, but the row was already due
        await _h.PollAsync();

        Assert.Equal(2, _h.Channel.Sent.Count);
        Assert.Equal("Reason stored before the crash", _h.Channel.Sent[1].N.Items.Single().Reason);
        Assert.Equal(DeliveryStatus.Sent, _h.Db(db => db.Deliveries.Single()).Status);
    }

    [Fact]
    public async Task Pending_rows_of_a_deleted_alert_fail_without_sending()
    {
        var alert = _h.AddAlert(Tier.All, topics: Topic.World);
        _h.Source.Set(Topic.World, Item("w1"));
        await _h.PollAsync();
        _h.Db(db =>
        {
            db.Deliveries.Single().Status = DeliveryStatus.Pending;
            db.Alerts.Single().DeletedAtUtc = _h.Clock.GetUtcNow().UtcDateTime;
            db.SaveChanges();
            return 0;
        });

        await _h.PollAsync();

        Assert.Single(_h.Channel.Sent);
        Assert.Equal(DeliveryStatus.Failed, _h.Db(db => db.Deliveries.Single()).Status);
    }

    [Fact]
    public async Task Unreadable_destination_settings_fail_cleanly()
    {
        _h.AddAlert(Tier.All, topics: Topic.World);
        _h.Db(db => { db.Destinations.Single().ConfigJson = "not json"; db.SaveChanges(); return 0; });
        _h.Source.Set(Topic.World, Item("w1"));

        await _h.PollAsync();

        Assert.Empty(_h.Channel.Sent);
        Assert.Equal(DeliveryStatus.Failed, _h.Db(db => db.Deliveries.Single()).Status);
    }

    // ---- Retention ---------------------------------------------------------

    [Fact]
    public async Task Retention_never_causes_a_resend_of_an_article_still_in_the_feed()
    {
        _h.AddAlert(Tier.All, topics: Topic.World);
        _h.Source.Set(Topic.World, Item("w1"));
        await _h.PollAsync();

        for (var day = 0; day < 40; day++)
        {
            _h.Clock.Advance(TimeSpan.FromDays(1));
            await _h.PollAsync();
        }

        Assert.Single(_h.Channel.Sent);
        Assert.Empty(_h.Db(db => db.Deliveries.ToList()));          // delivery purged after 30 days
        Assert.Single(_h.Db(db => db.Articles.ToList()));           // article kept: still seen every cycle
    }

    [Fact]
    public async Task Future_dated_article_cannot_come_back_after_its_delivery_is_purged()
    {
        _h.AddAlert(Tier.All, topics: Topic.World);
        _h.Source.Set(Topic.World, Item("future", published: Start.AddYears(70)));
        await _h.PollAsync();

        for (var day = 0; day < 40; day++)
        {
            _h.Clock.Advance(TimeSpan.FromDays(1));
            await _h.PollAsync();
        }

        Assert.Single(_h.Channel.Sent);
    }

    [Fact]
    public async Task Unseen_article_without_deliveries_is_purged_after_window_plus_margin()
    {
        _h.Source.Set(Topic.World, Item("gone"));
        await _h.PollAsync();
        _h.Source.Set(Topic.World);

        _h.Clock.Advance(TimeSpan.FromDays(8)); // 24 h + 7 d = 8 d: not yet past the cutoff
        await _h.PollAsync();
        Assert.Single(_h.Db(db => db.Articles.ToList()));

        _h.Clock.Advance(TimeSpan.FromMinutes(1));
        await _h.PollAsync();
        Assert.Empty(_h.Db(db => db.Articles.ToList()));
        Assert.Empty(_h.Db(db => db.ArticleTopics.ToList()));
    }

    [Fact]
    public async Task Article_that_left_the_feed_is_kept_while_it_still_has_delivery_history()
    {
        _h.AddAlert(Tier.All, topics: Topic.World);
        _h.Source.Set(Topic.World, Item("w1"));
        await _h.PollAsync();
        _h.Source.Set(Topic.World);

        _h.Clock.Advance(TimeSpan.FromDays(9)); // past the article cutoff, but the delivery is only 9 days old
        await _h.PollAsync();
        Assert.Single(_h.Db(db => db.Articles.ToList()));
        Assert.Single(_h.Db(db => db.Deliveries.ToList()));

        _h.Clock.Advance(TimeSpan.FromDays(22)); // delivery now past 30 days: both go
        await _h.PollAsync();
        Assert.Empty(_h.Db(db => db.Deliveries.ToList()));
        Assert.Empty(_h.Db(db => db.Articles.ToList()));
    }

    [Fact]
    public async Task Soft_deleted_alert_is_removed_once_its_history_is_gone()
    {
        _h.AddAlert(Tier.All, topics: Topic.World);
        _h.Source.Set(Topic.World, Item("w1"));
        await _h.PollAsync();
        _h.Db(db => { db.Alerts.Single().DeletedAtUtc = _h.Clock.GetUtcNow().UtcDateTime; db.SaveChanges(); return 0; });

        _h.Clock.Advance(TimeSpan.FromDays(29));
        await _h.PollAsync();
        Assert.Single(_h.Db(db => db.Alerts.ToList()));

        _h.Clock.Advance(TimeSpan.FromDays(2));
        await _h.PollAsync();
        Assert.Empty(_h.Db(db => db.Alerts.ToList()));
        Assert.Empty(_h.Db(db => db.Destinations.ToList()));
    }

    // ---- Configuration -------------------------------------------------------

    [Theory]
    [InlineData("Notifier:Polling:PollInterval", "00:05:00")]
    [InlineData("Notifier:Polling:ManualPollMinGap", "00:00:10")]
    [InlineData("Notifier:Polling:DeliveryRetention", "1.00:00:00")]
    [InlineData("Notifier:Polling:ArticleRetentionMargin", "00:00:00")]
    [InlineData("Notifier:Polling:BatchCap", "0")]
    public void Unsafe_settings_are_rejected(string key, string value)
    {
        var services = new ServiceCollection();
        services.AddNotifierPolling(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [key] = value }).Build());
        using var sp = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<PollingOptions>>().Value);
    }

    [Theory]
    [InlineData(0, 60)]    // manual request right after a cycle started: wait the full gap
    [InlineData(45, 15)]
    [InlineData(60, 0)]
    [InlineData(900, 0)]   // scheduled wake-up long after the last cycle
    public void Next_cycle_never_starts_within_the_minimum_gap_of_the_previous_one(int secondsSinceLastStart, int expectedWait)
    {
        var lastStart = Start;
        var now = Start.AddSeconds(secondsSinceLastStart);
        Assert.Equal(TimeSpan.FromSeconds(expectedWait), PollingWorker.DelayBeforeNextCycle(lastStart, now, TimeSpan.FromSeconds(60)));
    }

    [Fact]
    public void Manual_poll_requests_respect_the_minimum_gap()
    {
        var trigger = _h.Service<PollTrigger>();
        Assert.True(trigger.RequestPoll().Accepted);

        var second = trigger.RequestPoll();
        Assert.False(second.Accepted);
        Assert.Equal(_h.Clock.GetUtcNow().UtcDateTime.AddSeconds(60), second.RetryAfterUtc);

        _h.Clock.Advance(TimeSpan.FromSeconds(60));
        Assert.True(trigger.RequestPoll().Accepted);
    }
}
