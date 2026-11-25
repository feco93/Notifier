namespace Notifier.Web.Polling;

public sealed class PollingOptions
{
    public const string Section = "Notifier:Polling";

    public bool Enabled { get; set; } = true;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan ManualPollMinGap { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan FreshnessWindow { get; set; } = TimeSpan.FromHours(24);
    public int HeadlineN { get; set; } = 5;
    public int BatchCap { get; set; } = 20;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan DeliveryRetention { get; set; } = TimeSpan.FromDays(30);
    /// <summary>Unseen articles are purged after FreshnessWindow + this margin.</summary>
    public TimeSpan ArticleRetentionMargin { get; set; } = TimeSpan.FromDays(7);
}
