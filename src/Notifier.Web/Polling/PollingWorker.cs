using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Notifier.Web.Sending;

namespace Notifier.Web.Polling;

/// <summary>Lets the admin page request an immediate poll, at most once per <see cref="PollingOptions.ManualPollMinGap"/>.</summary>
public sealed class PollTrigger(TimeProvider time, IOptions<PollingOptions> options)
{
    private readonly Channel<bool> _requests = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });
    private readonly Lock _gate = new();
    private DateTime? _lastManualUtc;

    public (bool Accepted, DateTime? RetryAfterUtc) RequestPoll()
    {
        lock (_gate)
        {
            var now = time.GetUtcNow().UtcDateTime;
            var gap = options.Value.ManualPollMinGap;
            if (_lastManualUtc is { } last && now - last < gap)
                return (false, last + gap);
            _lastManualUtc = now;
        }
        _requests.Writer.TryWrite(true);
        return (true, null);
    }

    internal async Task WaitForNextPollAsync(TimeSpan interval, CancellationToken stop)
    {
        using var wake = CancellationTokenSource.CreateLinkedTokenSource(stop);
        var timer = Task.Delay(interval, time, wake.Token);
        var manual = _requests.Reader.WaitToReadAsync(wake.Token).AsTask();
        await Task.WhenAny(timer, manual);
        await wake.CancelAsync();
        while (_requests.Reader.TryRead(out _)) { }
    }
}

internal sealed class PollingWorker(
    IServiceScopeFactory scopes,
    PollTrigger trigger,
    TimeProvider time,
    IOptions<PollingOptions> options,
    ILogger<PollingWorker> log) : BackgroundService
{
    // A manual request that arrived during a cycle would otherwise start the next one immediately.
    internal static TimeSpan DelayBeforeNextCycle(DateTime lastStartUtc, DateTime nowUtc, TimeSpan minGap)
    {
        var wait = lastStartUtc + minGap - nowUtc;
        return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        if (!options.Value.Enabled)
        {
            log.LogInformation("Polling is disabled.");
            return;
        }

        DateTime? lastStart = null;
        while (!stop.IsCancellationRequested)
        {
            if (lastStart is { } last)
            {
                var wait = DelayBeforeNextCycle(last, time.GetUtcNow().UtcDateTime, options.Value.ManualPollMinGap);
                if (wait > TimeSpan.Zero) await Task.Delay(wait, time, stop);
            }
            lastStart = time.GetUtcNow().UtcDateTime;

            try
            {
                using var scope = scopes.CreateScope();
                var summary = await scope.ServiceProvider.GetRequiredService<PollService>().RunCycleAsync(stop);
                log.LogInformation("Poll cycle: {Summary}", summary);
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // One bad cycle must not stop polling for good.
                log.LogError(ex, "Poll cycle failed.");
            }

            await trigger.WaitForNextPollAsync(options.Value.PollInterval, stop);
        }
    }
}

public static class PollingServiceCollectionExtensions
{
    public static IServiceCollection AddNotifierPolling(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PollingOptions>()
            .Bind(configuration.GetSection(PollingOptions.Section))
            .Validate(o => o.PollInterval >= TimeSpan.FromMinutes(15), "Poll interval must be at least 15 minutes (the feed's declared TTL).")
            .Validate(o => o.ManualPollMinGap >= TimeSpan.FromSeconds(60), "Manual polls must be at least 60 seconds apart.")
            .Validate(o => o.FreshnessWindow > TimeSpan.Zero && o.HeadlineN >= 1, "Freshness window and headline count must be positive.")
            .Validate(o => o.BatchCap >= 1, "Batch cap must be at least 1.")
            .Validate(o => o.RetryDelay >= TimeSpan.Zero, "Retry delay cannot be negative.")
            // Retention may only remove what can no longer match.
            .Validate(o => o.DeliveryRetention > o.FreshnessWindow, "Delivery retention must exceed the freshness window.")
            .Validate(o => o.ArticleRetentionMargin > TimeSpan.Zero, "Article retention margin must be positive.")
            .ValidateOnStart();

        services.AddScoped<Dispatcher>();
        services.AddScoped<RetentionService>();
        services.AddScoped<PollService>();
        services.AddSingleton<PollTrigger>();
        services.AddHostedService<PollingWorker>();
        return services;
    }
}
