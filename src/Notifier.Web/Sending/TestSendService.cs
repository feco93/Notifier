using System.Text.Json;
using Notifier.Core.Channels;
using Notifier.Core.Domain;
using Notifier.Core.Sources;
using Notifier.Web.Channels;
using Notifier.Web.Data;

namespace Notifier.Web.Sending;

public sealed class TestSendService(ChannelRegistry channels, NotifierDbContext db, TimeProvider time, IArticleSource source)
{
    public async Task<SendResult> SendAsync(Alert alert, Destination destination, CancellationToken ct)
    {
        if (destination.AlertId != alert.Id)
            throw new ArgumentException("The destination does not belong to this alert.", nameof(destination));
        if (alert.DeletedAtUtc is not null)
            return SendResult.Failed("This alert has been deleted.");

        SendResult result;
        try
        {
            result = await TrySendAsync(alert, destination, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            result = SendResult.Failed($"Unexpected error: {ex.Message}");
        }

        var now = time.GetUtcNow().UtcDateTime;
        db.Deliveries.Add(new Delivery
        {
            AlertId = alert.Id,
            DestinationId = destination.Id,
            Kind = DeliveryKind.Test,
            Status = result.Success ? DeliveryStatus.Sent : DeliveryStatus.Failed,
            Attempts = 1,
            LastError = result.Error,
            CreatedAtUtc = now,
            CompletedAtUtc = now,
        });
        await db.SaveChangesAsync(ct);
        return result;
    }

    private async Task<SendResult> TrySendAsync(Alert alert, Destination destination, CancellationToken ct)
    {
        var channel = channels.Get(destination.ChannelKey);
        if (channel is null)
            return SendResult.Failed($"Unknown channel '{destination.ChannelKey}'.");

        Dictionary<string, string>? config;
        try
        {
            config = JsonSerializer.Deserialize<Dictionary<string, string>>(destination.ConfigJson);
        }
        catch (JsonException)
        {
            config = null;
        }
        if (config is null)
            return SendResult.Failed("Destination settings are unreadable.");

        var notification = new Notification(
            alert.Name,
            [new NotificationItem(
                "This is a test notification from Notifier",
                "If you can read this, this destination is set up correctly.",
                new Uri("https://www.bbc.co.uk/news"),
                "Test send")],
            MoreCount: 0,
            source.Attribution,
            IsTest: true,
            IsSynthetic: false);

        return await channel.SendAsync(notification, config, ct);
    }
}
