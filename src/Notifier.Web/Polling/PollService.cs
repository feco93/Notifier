using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notifier.Core.Domain;
using Notifier.Core.Sources;
using Notifier.Web.Data;
using Notifier.Web.Sending;

namespace Notifier.Web.Polling;

public sealed record PollSummary(int FetchedFeeds, int FailedFeeds, int NewArticles, DispatchSummary Dispatch, PurgeSummary Purge);

public sealed class PollService(
    NotifierDbContext db,
    IArticleSource source,
    Dispatcher dispatcher,
    RetentionService retention,
    TimeProvider time,
    IOptions<PollingOptions> options,
    ILogger<PollService> log)
{
    public async Task<PollSummary> RunCycleAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var fetched = new Dictionary<Topic, IReadOnlyList<SourceItem>>();
        var statuses = await db.FeedStatuses.ToDictionaryAsync(f => f.Topic, ct);

        // Sequential on purpose: polite to the source.
        foreach (var topic in source.SupportedTopics)
        {
            if (!statuses.TryGetValue(topic, out var status))
                db.FeedStatuses.Add(statuses[topic] = status = new FeedStatus { Topic = topic });
            status.LastPollAtUtc = now;
            status.NextPollAtUtc = now + options.Value.PollInterval;
            try
            {
                var result = await source.FetchAsync(topic, ct);
                fetched[topic] = result.Items;
                status.LastSuccessAtUtc = now;
                status.LastError = null;
                status.ItemsLastFetch = result.Items.Count;
            }
            catch (SourceException ex)
            {
                status.LastError = ex.Message;
                log.LogWarning("Feed {Topic} failed: {Error}", topic, ex.Message);
            }
        }

        var keys = fetched.Values.SelectMany(items => items).Select(i => i.Key).Distinct().ToList();
        var articles = await db.Articles.Include(a => a.Topics).Where(a => keys.Contains(a.Key)).ToDictionaryAsync(a => a.Key, ct);
        var newKeys = new HashSet<string>();

        foreach (var (topic, items) in fetched)
        {
            foreach (var item in items)
            {
                if (!articles.TryGetValue(item.Key, out var article))
                {
                    article = new Article { Key = item.Key, Title = item.Title, Url = item.Url.AbsoluteUri, FirstSeenAtUtc = now };
                    db.Articles.Add(article);
                    articles[item.Key] = article;
                    newKeys.Add(item.Key);
                }
                article.Title = item.Title;
                article.Summary = item.Summary;
                article.Url = item.Url.AbsoluteUri;
                article.PublishedAtUtc = item.PublishedAtUtc ?? article.PublishedAtUtc;
                article.LastSeenAtUtc = now;

                var articleTopic = article.Topics.FirstOrDefault(t => t.Topic == topic);
                if (articleTopic is null)
                    article.Topics.Add(new ArticleTopic { ArticleKey = item.Key, Topic = topic, LastSeenAtUtc = now });
                else
                    articleTopic.LastSeenAtUtc = now;
            }
            statuses[topic].NewLastFetch = items.Count(i => newKeys.Contains(i.Key));
        }

        // Only a successful Top stories fetch may change ranks; a failed one must not wipe them.
        if (fetched.TryGetValue(Topic.TopStories, out var top))
        {
            var ranks = top.ToDictionary(i => i.Key, i => i.Section == 0 ? i.Position + 1 : (int?)null);
            foreach (var (key, rank) in ranks)
            {
                articles[key].OnTopStories = true;
                articles[key].TopStoriesRank = rank;
            }
            var dropped = await db.Articles.Where(a => a.OnTopStories && !ranks.Keys.Contains(a.Key)).ToListAsync(ct);
            foreach (var article in dropped)
            {
                article.OnTopStories = false;
                article.TopStoriesRank = null;
            }
        }

        await db.SaveChangesAsync(ct);

        var dispatch = await dispatcher.DispatchAsync(keys, ct);
        var purge = await retention.PurgeAsync(ct);
        return new PollSummary(fetched.Count, source.SupportedTopics.Count - fetched.Count, newKeys.Count, dispatch, purge);
    }
}
