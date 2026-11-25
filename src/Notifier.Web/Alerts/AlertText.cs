using System.Text.Json;
using Notifier.Core.Domain;
using Notifier.Web.Channels;

namespace Notifier.Web.Alerts;

public static class AlertText
{
    public static string Tier(Tier tier, int headlineN) => tier switch
    {
        Core.Domain.Tier.Headline => $"Headline (top {headlineN} of Top stories)",
        Core.Domain.Tier.FrontPage => "Front page (anywhere in Top stories)",
        Core.Domain.Tier.All => "All new articles in the chosen topics",
        _ => tier.ToString(),
    };

    /// <summary>Channel name plus the masked destination; secrets are never included.</summary>
    public static string Destination(ChannelRegistry channels, Destination destination)
    {
        var channel = channels.Get(destination.ChannelKey);
        if (channel is null) return $"Unknown channel '{destination.ChannelKey}'";
        Dictionary<string, string>? config;
        try { config = JsonSerializer.Deserialize<Dictionary<string, string>>(destination.ConfigJson); }
        catch (JsonException) { config = null; }
        return config is null ? $"{channel.DisplayName}: (unreadable settings)" : $"{channel.DisplayName}: {channel.Describe(config)}";
    }
}
