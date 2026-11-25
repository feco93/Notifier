using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notifier.Web.Data;

namespace Notifier.Web.Polling;

public sealed record PurgeSummary(int Deliveries, int Articles, int Alerts);

/// <summary>Deletes only data that can no longer cause a match, so purging never leads to a re-send.</summary>
public sealed class RetentionService(NotifierDbContext db, TimeProvider time, IOptions<PollingOptions> options)
{
    public async Task<PurgeSummary> PurgeAsync(CancellationToken ct)
    {
        var o = options.Value;
        var now = time.GetUtcNow().UtcDateTime;
        var deliveryCutoff = now - o.DeliveryRetention;
        var articleCutoff = now - (o.FreshnessWindow + o.ArticleRetentionMargin);

        // Order matters: deliveries reference articles and alerts.
        var deliveries = await db.Deliveries.Where(d => d.CreatedAtUtc < deliveryCutoff).ExecuteDeleteAsync(ct);
        var articles = await db.Articles
            .Where(a => a.LastSeenAtUtc < articleCutoff && !db.Deliveries.Any(d => d.ArticleKey == a.Key))
            .ExecuteDeleteAsync(ct);
        var alerts = await db.Alerts
            .Where(a => a.DeletedAtUtc != null && !db.Deliveries.Any(d => d.AlertId == a.Id))
            .ExecuteDeleteAsync(ct);

        return new PurgeSummary(deliveries, articles, alerts);
    }
}
