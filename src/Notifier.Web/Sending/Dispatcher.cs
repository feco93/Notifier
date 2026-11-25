using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notifier.Core.Channels;
using Notifier.Core.Domain;
using Notifier.Core.Matching;
using Notifier.Core.Sources;
using Notifier.Web.Channels;
using Notifier.Web.Data;
using Notifier.Web.Polling;

namespace Notifier.Web.Sending;

public sealed record DispatchSummary(int NewDeliveries, int Batches, int SentBatches, int FailedBatches);

public sealed class Dispatcher(
    NotifierDbContext db,
    ChannelRegistry channels,
    IArticleSource source,
    TimeProvider time,
    IOptions<PollingOptions> options,
    ILogger<Dispatcher> log)
{
    public async Task<DispatchSummary> DispatchAsync(IReadOnlyCollection<string> candidateKeys, CancellationToken ct)
    {
        var created = candidateKeys.Count == 0 ? 0 : await RecordMatchesAsync(candidateKeys, ct);
        return await SendPendingAsync(created, ct);
    }

    private async Task<int> RecordMatchesAsync(IReadOnlyCollection<string> candidateKeys, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var settings = new MatchSettings(options.Value.FreshnessWindow, options.Value.HeadlineN);
        var articles = await db.Articles.Include(a => a.Topics).Where(a => candidateKeys.Contains(a.Key)).ToListAsync(ct);
        var alerts = await db.Alerts.Include(a => a.Topics).Include(a => a.Destinations)
            .Where(a => a.Enabled && a.DeletedAtUtc == null).ToListAsync(ct);
        var existing = (await db.Deliveries
                .Where(d => d.ArticleKey != null && candidateKeys.Contains(d.ArticleKey))
                .Select(d => new { d.ArticleKey, d.AlertId, d.DestinationId })
                .ToListAsync(ct))
            .Select(d => (d.ArticleKey!, d.AlertId, d.DestinationId))
            .ToHashSet();

        var created = 0;
        foreach (var alert in alerts)
        foreach (var article in articles)
        {
            var match = Matcher.Evaluate(alert, article, now, settings);
            if (!match.IsMatch) continue;
            foreach (var destination in alert.Destinations)
            {
                // Pre-check instead of relying on the unique index, which stays as the safety net.
                if (!existing.Add((article.Key, alert.Id, destination.Id))) continue;
                db.Deliveries.Add(new Delivery
                {
                    ArticleKey = article.Key, AlertId = alert.Id, DestinationId = destination.Id,
                    Kind = DeliveryKind.Real, Status = DeliveryStatus.Pending, Reason = match.Reason, CreatedAtUtc = now,
                });
                created++;
            }
        }
        await db.SaveChangesAsync(ct);
        return created;
    }

    private async Task<DispatchSummary> SendPendingAsync(int created, CancellationToken ct)
    {
        var pending = await db.Deliveries.Where(d => d.Kind == DeliveryKind.Real && d.Status == DeliveryStatus.Pending).ToListAsync(ct);
        int sent = 0, failed = 0;

        foreach (var group in pending.GroupBy(d => (d.AlertId, d.DestinationId)))
        {
            var rows = group.ToList();
            var alert = await db.Alerts.FindAsync([group.Key.AlertId], ct);
            var destination = await db.Destinations.FindAsync([group.Key.DestinationId], ct);
            var keys = rows.Select(r => r.ArticleKey!).ToList();
            var articles = await db.Articles.Where(a => keys.Contains(a.Key)).ToDictionaryAsync(a => a.Key, ct);

            rows = [.. rows
                .OrderBy(r => articles.GetValueOrDefault(r.ArticleKey!)?.TopStoriesRank ?? int.MaxValue)
                .ThenByDescending(r => articles.GetValueOrDefault(r.ArticleKey!)?.PublishedAtUtc)
                .ThenBy(r => r.ArticleKey, StringComparer.Ordinal)];

            var batchId = Guid.NewGuid();
            var (result, attempts) = alert is null || destination is null || !alert.Enabled || alert.DeletedAtUtc is not null
                ? (SendResult.Failed("The alert is no longer active."), 0)
                : await SendBatchAsync(alert, destination, rows, articles, ct);

            var now = time.GetUtcNow().UtcDateTime;
            foreach (var row in rows)
            {
                row.BatchId = batchId;
                row.Status = result.Success ? DeliveryStatus.Sent : DeliveryStatus.Failed;
                row.Attempts += attempts;
                row.LastError = result.Error;
                row.CompletedAtUtc = now;
            }
            if (result.Success) sent++;
            else
            {
                failed++;
                log.LogWarning("Delivery batch {BatchId} for alert {AlertId} failed: {Error}", batchId, group.Key.AlertId, result.Error);
            }
        }

        await db.SaveChangesAsync(ct);
        return new DispatchSummary(created, sent + failed, sent, failed);
    }

    private async Task<(SendResult Result, int Attempts)> SendBatchAsync(
        Alert alert, Destination destination, List<Delivery> rows, Dictionary<string, Article> articles, CancellationToken ct)
    {
        var channel = channels.Get(destination.ChannelKey);
        if (channel is null)
            return (SendResult.Failed($"Unknown channel '{destination.ChannelKey}'."), 0);

        Dictionary<string, string>? config;
        try { config = JsonSerializer.Deserialize<Dictionary<string, string>>(destination.ConfigJson); }
        catch (JsonException) { config = null; }
        if (config is null)
            return (SendResult.Failed("Destination settings are unreadable."), 0);

        var cap = options.Value.BatchCap;
        var shown = rows.Where(r => articles.ContainsKey(r.ArticleKey!)).Take(cap).ToList();
        var items = shown.Select(r =>
        {
            var a = articles[r.ArticleKey!];
            return new NotificationItem(a.Title, a.Summary, new Uri(a.Url), r.Reason ?? "New article");
        }).ToList();
        var notification = new Notification(
            alert.Name, items, rows.Count - items.Count, source.Attribution,
            IsTest: false, IsSynthetic: shown.Any(r => articles[r.ArticleKey!].IsSynthetic));

        var attempts = 0;
        while (true)
        {
            attempts++;
            SendResult result;
            try { result = await channel.SendAsync(notification, config, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { result = SendResult.Failed($"Unexpected error: {ex.Message}"); }

            if (result.Success || attempts == 2) return (result, attempts);
            await Task.Delay(options.Value.RetryDelay, time, ct);
        }
    }
}
