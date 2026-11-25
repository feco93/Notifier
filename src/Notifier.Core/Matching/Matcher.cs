using System.Text.RegularExpressions;
using Notifier.Core.Domain;

namespace Notifier.Core.Matching;

public enum Rejection
{
    None,
    AlertInactive,
    SeenBeforeAlert,
    UnknownPublishDate,
    NotFresh,
    TopicMismatch,
    NotOnFrontPage,
    NotHeadline,
    KeywordMismatch,
}

public sealed record MatchResult(bool IsMatch, string? Reason, Rejection Rejection)
{
    public static MatchResult Match(string reason) => new(true, reason, Rejection.None);
    public static MatchResult Reject(Rejection why) => new(false, null, why);
}

public sealed record MatchSettings
{
    public MatchSettings(TimeSpan FreshnessWindow, int HeadlineN)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(FreshnessWindow, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(HeadlineN, 1);
        this.FreshnessWindow = FreshnessWindow;
        this.HeadlineN = HeadlineN;
    }

    public TimeSpan FreshnessWindow { get; }
    public int HeadlineN { get; }
}

public static class Matcher
{
    public static MatchResult Evaluate(Alert alert, Article article, DateTime nowUtc, MatchSettings settings)
    {
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Must be a UTC DateTime.", nameof(nowUtc));

        if (!alert.Enabled || alert.DeletedAtUtc is not null)
            return MatchResult.Reject(Rejection.AlertInactive);

        // No backfill: only articles discovered after the alert became active (created or re-enabled).
        if (article.FirstSeenAtUtc <= alert.ActiveSinceUtc)
            return MatchResult.Reject(Rejection.SeenBeforeAlert);

        if (article.PublishedAtUtc is not { } published)
            return MatchResult.Reject(Rejection.UnknownPublishDate);
        // A future date (bad data) would otherwise keep the article fresh forever.
        var age = nowUtc - (published < article.FirstSeenAtUtc ? published : article.FirstSeenAtUtc);
        if (age > settings.FreshnessWindow)
            return MatchResult.Reject(Rejection.NotFresh);

        string reason;
        switch (alert.Tier)
        {
            case Tier.Headline:
                if (article.TopStoriesRank is not { } rank || rank > settings.HeadlineN)
                    return MatchResult.Reject(Rejection.NotHeadline);
                reason = $"#{rank} in {TopicNames.Display(Topic.TopStories)}";
                break;

            case Tier.FrontPage:
                if (!article.OnTopStories)
                    return MatchResult.Reject(Rejection.NotOnFrontPage);
                reason = article.TopStoriesRank is { } r
                    ? $"#{r} in {TopicNames.Display(Topic.TopStories)}"
                    : $"In {TopicNames.Display(Topic.TopStories)}";
                break;

            case Tier.All:
                var alertTopics = alert.Topics.Select(t => t.Topic).ToHashSet();
                var articleTopics = article.Topics.Select(t => t.Topic).ToHashSet();
                var shared = Enum.GetValues<Topic>().Where(t => alertTopics.Contains(t) && articleTopics.Contains(t)).ToList();
                if (shared.Count == 0)
                    return MatchResult.Reject(Rejection.TopicMismatch);
                reason = $"New in {TopicNames.Display(shared[0])}";
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(alert), alert.Tier, "Unknown tier.");
        }

        var keyword = alert.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            var pattern = WholeWordOrPhrase(keyword);
            if (!IsMatch(article.Title, pattern) && !IsMatch(article.Summary, pattern))
                return MatchResult.Reject(Rejection.KeywordMismatch);
            reason += $" · keyword: '{keyword}'";
        }

        return MatchResult.Match(reason);
    }

    // Substring matching fires on parts of words ("AI" in "again"), so the keyword must stand alone.
    private static string WholeWordOrPhrase(string keyword)
    {
        var words = keyword.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(Regex.Escape);
        return $@"(?<!\w){string.Join(@"\s+", words)}(?!\w)";
    }

    private static bool IsMatch(string text, string pattern) =>
        Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
}
