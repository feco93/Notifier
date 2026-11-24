using System.Text;
using Notifier.Core.Channels;

namespace Notifier.Web.Channels;

internal sealed class SlackChannel(IHttpClientFactory httpClients) : INotificationChannel
{
    public const string HttpClientName = "slack";
    private const string UrlField = "webhookUrl";

    public string Key => "slack";
    public string DisplayName => "Slack";
    public IReadOnlyList<ChannelField> ConfigFields { get; } =
        [new(UrlField, "Slack incoming webhook URL", FieldKind.Url, IsSecret: true)];

    public IReadOnlyList<string> Validate(IReadOnlyDictionary<string, string> config) =>
        TryGetWebhook(config, out _) ? [] : ["Enter a Slack incoming webhook URL starting with https://hooks.slack.com/services/."];

    public string Describe(IReadOnlyDictionary<string, string> config) =>
        TryGetWebhook(config, out _) ? "Slack webhook (hooks.slack.com/services/…)" : "(invalid Slack webhook)";

    public async Task<SendResult> SendAsync(Notification notification, IReadOnlyDictionary<string, string> config, CancellationToken ct)
    {
        // Re-checked at send time: the stored URL is the only thing deciding where the request goes.
        if (!TryGetWebhook(config, out var webhook))
            return SendResult.Failed("Invalid Slack destination.");

        try
        {
            using var content = new StringContent(SlackFormatter.Payload(notification), Encoding.UTF8, "application/json");
            using var response = await httpClients.CreateClient(HttpClientName).PostAsync(webhook, content, ct);
            if (response.IsSuccessStatusCode)
                return SendResult.Ok();

            var body = TextSanitizer.SingleLine(await response.Content.ReadAsStringAsync(ct), 200);
            return SendResult.Failed($"Slack returned HTTP {(int)response.StatusCode}: {body}");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return SendResult.Failed($"Slack request failed: {ex.Message}");
        }
    }

    internal static bool IsAllowedWebhook(string? raw, out Uri webhook)
    {
        webhook = null!;
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 500 || raw.Any(char.IsControl) || raw.Trim().Contains(' '))
            return false;
        if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var url))
            return false;
        var ok = url.Scheme == Uri.UriSchemeHttps
                 && string.Equals(url.Host, "hooks.slack.com", StringComparison.OrdinalIgnoreCase)
                 && url.IsDefaultPort
                 && url.UserInfo.Length == 0
                 && url.AbsolutePath.StartsWith("/services/", StringComparison.Ordinal)
                 && url.Query.Length == 0
                 && url.Fragment.Length == 0;
        if (ok) webhook = url;
        return ok;
    }

    private static bool TryGetWebhook(IReadOnlyDictionary<string, string> config, out Uri webhook)
    {
        webhook = null!;
        return config.TryGetValue(UrlField, out var raw) && IsAllowedWebhook(raw, out webhook);
    }
}
