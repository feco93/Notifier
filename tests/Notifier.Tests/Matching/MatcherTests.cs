using Notifier.Core.Domain;
using Notifier.Core.Matching;

namespace Notifier.Tests.Matching;

public sealed class MatcherTests
{
    private static readonly DateTime Now = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime AlertCreated = Now.AddHours(-2);
    private static readonly MatchSettings Settings = new(FreshnessWindow: TimeSpan.FromHours(24), HeadlineN: 5);

    private static Alert NewAlert(Tier tier, string? keyword = null, params Topic[] topics) => new()
    {
        Id = 1, OwnerUsername = "u", Name = "A", Tier = tier, Keyword = keyword, Enabled = true,
        CreatedAtUtc = AlertCreated,
        Topics = [.. topics.Select(t => new AlertTopic { AlertId = 1, Topic = t })],
    };

    private static Article NewArticle(int? rank = null, bool onTopStories = false, params Topic[] topics) => new()
    {
        Key = "k1", Title = "Chip maker Nvidia beats forecasts", Summary = "Shares rose after results.",
        Url = "https://example.test/k1",
        PublishedAtUtc = Now.AddHours(-1), FirstSeenAtUtc = Now.AddMinutes(-30), LastSeenAtUtc = Now,
        OnTopStories = onTopStories || rank is not null, TopStoriesRank = rank,
        Topics = [.. topics.Select(t => new ArticleTopic { ArticleKey = "k1", Topic = t, LastSeenAtUtc = Now })],
    };

    private static MatchResult Eval(Alert alert, Article article) => Matcher.Evaluate(alert, article, Now, Settings);

    // ---- Tiers -------------------------------------------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Headline_matches_lead_section_rank_up_to_n(int rank)
    {
        var r = Eval(NewAlert(Tier.Headline), NewArticle(rank));
        Assert.True(r.IsMatch);
        Assert.Equal($"#{rank} in Top stories", r.Reason);
    }

    [Fact]
    public void Headline_rejects_rank_just_below_the_cut()
    {
        Assert.Equal(Rejection.NotHeadline, Eval(NewAlert(Tier.Headline), NewArticle(rank: 6)).Rejection);
    }

    [Fact]
    public void Headline_rejects_front_page_article_outside_the_lead_section()
    {
        Assert.Equal(Rejection.NotHeadline, Eval(NewAlert(Tier.Headline), NewArticle(rank: null, onTopStories: true)).Rejection);
    }

    [Fact]
    public void Headline_ignores_topics()
    {
        var r = Eval(NewAlert(Tier.Headline, null, Topic.Technology), NewArticle(rank: 2, topics: Topic.World));
        Assert.True(r.IsMatch);
    }

    [Fact]
    public void FrontPage_matches_any_section_of_top_stories()
    {
        var r = Eval(NewAlert(Tier.FrontPage), NewArticle(rank: null, onTopStories: true));
        Assert.True(r.IsMatch);
        Assert.Equal("In Top stories", r.Reason);
    }

    [Fact]
    public void FrontPage_reports_the_rank_when_there_is_one()
    {
        Assert.Equal("#9 in Top stories", Eval(NewAlert(Tier.FrontPage), NewArticle(rank: 9)).Reason);
    }

    [Fact]
    public void FrontPage_rejects_article_not_on_top_stories()
    {
        Assert.Equal(Rejection.NotOnFrontPage, Eval(NewAlert(Tier.FrontPage), NewArticle(topics: Topic.World)).Rejection);
    }

    [Fact]
    public void All_matches_article_in_one_of_the_alert_topics()
    {
        var r = Eval(NewAlert(Tier.All, null, Topic.Business, Topic.World), NewArticle(topics: Topic.World));
        Assert.True(r.IsMatch);
        Assert.Equal("New in World", r.Reason);
    }

    [Fact]
    public void All_names_the_first_matching_alert_topic_in_topic_order()
    {
        var r = Eval(NewAlert(Tier.All, null, Topic.Technology, Topic.Business),
                     NewArticle(topics: [Topic.Technology, Topic.Business]));
        Assert.Equal("New in Business", r.Reason);
    }

    [Fact]
    public void All_with_top_stories_topic_uses_the_top_stories_feed()
    {
        var r = Eval(NewAlert(Tier.All, null, Topic.TopStories), NewArticle(topics: Topic.TopStories));
        Assert.Equal("New in Top stories", r.Reason);
    }

    [Fact]
    public void All_rejects_article_outside_the_alert_topics()
    {
        Assert.Equal(Rejection.TopicMismatch,
            Eval(NewAlert(Tier.All, null, Topic.Business), NewArticle(rank: 1, topics: Topic.World)).Rejection);
    }

    [Fact]
    public void All_without_topics_matches_nothing()
    {
        Assert.Equal(Rejection.TopicMismatch, Eval(NewAlert(Tier.All), NewArticle(topics: Topic.World)).Rejection);
    }

    // ---- Keyword -----------------------------------------------------------

    [Theory]
    [InlineData("nvidia")]       // title, different case
    [InlineData("SHARES ROSE")]  // summary
    [InlineData("  Nvidia  ")]   // surrounding whitespace is not part of the keyword
    public void Keyword_matches_title_or_summary_ignoring_case(string keyword)
    {
        var r = Eval(NewAlert(Tier.Headline, keyword), NewArticle(rank: 1));
        Assert.True(r.IsMatch);
        Assert.Equal($"#1 in Top stories · keyword: '{keyword.Trim()}'", r.Reason);
    }

    [Theory]
    [InlineData("vid")]        // inside "Nvidia"
    [InlineData("ai")]         // inside "again"
    [InlineData("aker")]       // end of "maker": only the leading boundary rejects it
    [InlineData("forecast")]   // "forecasts" is a different word
    [InlineData("shares ros")] // phrase must end on a word boundary
    public void Keyword_inside_a_longer_word_does_not_match(string keyword)
    {
        var article = NewArticle(rank: 1);
        article.Title = "Chip maker Nvidia beats forecasts again";
        Assert.Equal(Rejection.KeywordMismatch, Eval(NewAlert(Tier.Headline, keyword), article).Rejection);
    }

    [Theory]
    [InlineData("Nvidia's rivals react", "Nvidia")]
    [InlineData("AI-powered chips surge", "AI")]
    [InlineData("Talks on the U.S. economy", "U.S.")]
    [InlineData("Why C++ still matters", "C++")]
    [InlineData("Shares   rose sharply", "shares rose")]
    [InlineData("Ends with the word (Nvidia)", "nvidia")]
    public void Keyword_matches_on_word_boundaries_including_punctuation(string title, string keyword)
    {
        var article = NewArticle(rank: 1);
        article.Title = title;
        article.Summary = "";
        Assert.True(Eval(NewAlert(Tier.Headline, keyword), article).IsMatch);
    }

    [Fact]
    public void Keyword_not_found_rejects()
    {
        Assert.Equal(Rejection.KeywordMismatch, Eval(NewAlert(Tier.Headline, "earthquake"), NewArticle(rank: 1)).Rejection);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_keyword_is_ignored(string keyword)
    {
        var r = Eval(NewAlert(Tier.Headline, keyword), NewArticle(rank: 1));
        Assert.True(r.IsMatch);
        Assert.Equal("#1 in Top stories", r.Reason);
    }

    // ---- Alert state -------------------------------------------------------

    [Fact]
    public void Disabled_alert_never_matches()
    {
        var alert = NewAlert(Tier.Headline);
        alert.Enabled = false;
        Assert.Equal(Rejection.AlertInactive, Eval(alert, NewArticle(rank: 1)).Rejection);
    }

    [Fact]
    public void Deleted_alert_never_matches()
    {
        var alert = NewAlert(Tier.Headline);
        alert.DeletedAtUtc = Now.AddMinutes(-1);
        Assert.Equal(Rejection.AlertInactive, Eval(alert, NewArticle(rank: 1)).Rejection);
    }

    // ---- First seen (no backfill) -----------------------------------------

    [Fact]
    public void Article_first_seen_at_the_moment_the_alert_was_created_is_not_new()
    {
        var article = NewArticle(rank: 1);
        article.FirstSeenAtUtc = AlertCreated;
        Assert.Equal(Rejection.SeenBeforeAlert, Eval(NewAlert(Tier.Headline), article).Rejection);
    }

    [Fact]
    public void Article_first_seen_just_after_alert_creation_is_new()
    {
        var article = NewArticle(rank: 1);
        article.FirstSeenAtUtc = AlertCreated.AddTicks(1);
        Assert.True(Eval(NewAlert(Tier.Headline), article).IsMatch);
    }

    // ---- Freshness ---------------------------------------------------------

    [Fact]
    public void Article_published_exactly_at_the_window_edge_is_fresh()
    {
        var article = NewArticle(rank: 1);
        article.PublishedAtUtc = Now - Settings.FreshnessWindow;
        Assert.True(Eval(NewAlert(Tier.Headline), article).IsMatch);
    }

    [Fact]
    public void Article_published_just_outside_the_window_is_stale()
    {
        var article = NewArticle(rank: 1);
        article.PublishedAtUtc = Now - Settings.FreshnessWindow - TimeSpan.FromTicks(1);
        Assert.Equal(Rejection.NotFresh, Eval(NewAlert(Tier.Headline), article).Rejection);
    }

    [Fact]
    public void Article_with_unknown_publish_date_is_never_eligible()
    {
        var article = NewArticle(rank: 1);
        article.PublishedAtUtc = null;
        Assert.Equal(Rejection.UnknownPublishDate, Eval(NewAlert(Tier.Headline), article).Rejection);
    }

    [Fact]
    public void Article_dated_in_the_future_counts_as_fresh()
    {
        var article = NewArticle(rank: 1);
        article.PublishedAtUtc = Now.AddHours(3);
        Assert.True(Eval(NewAlert(Tier.Headline), article).IsMatch);
    }

    [Fact]
    public void Future_dated_article_ages_from_when_it_was_first_seen()
    {
        var article = NewArticle(rank: 1);
        article.PublishedAtUtc = Now.AddYears(70);
        article.FirstSeenAtUtc = AlertCreated.AddMinutes(1);
        var later = Now + Settings.FreshnessWindow;
        Assert.Equal(Rejection.NotFresh, Matcher.Evaluate(NewAlert(Tier.Headline), article, later, Settings).Rejection);
    }

    // ---- Contract ----------------------------------------------------------

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Now_must_be_utc(DateTimeKind kind)
    {
        var localNow = DateTime.SpecifyKind(Now, kind);
        Assert.Throws<ArgumentException>(() => Matcher.Evaluate(NewAlert(Tier.Headline), NewArticle(rank: 1), localNow, Settings));
    }

    [Theory]
    [InlineData("Shares rose after results.", false)] // a full stop after a word is not standalone
    [InlineData("Wait ... what happened", true)]
    public void Punctuation_only_keyword_matches_only_standalone_punctuation(string summary, bool expected)
    {
        var article = NewArticle(rank: 1);
        article.Summary = summary;
        Assert.Equal(expected, Eval(NewAlert(Tier.Headline, "."), article).IsMatch);
    }

    [Fact]
    public void No_match_has_no_reason_text()
    {
        var r = Eval(NewAlert(Tier.Headline), NewArticle());
        Assert.False(r.IsMatch);
        Assert.Null(r.Reason);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 5)]
    [InlineData(24, 0)]
    public void Settings_reject_non_positive_values(int windowHours, int headlineN)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MatchSettings(TimeSpan.FromHours(windowHours), headlineN));
    }
}
