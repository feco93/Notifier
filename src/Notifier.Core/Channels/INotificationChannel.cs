namespace Notifier.Core.Channels;

public interface INotificationChannel
{
    /// <summary>Stable identifier stored with each destination, e.g. "email".</summary>
    string Key { get; }

    string DisplayName { get; }

    /// <summary>Describes the destination settings so a form can be rendered without channel-specific pages.</summary>
    IReadOnlyList<ChannelField> ConfigFields { get; }

    /// <returns>Validation errors; empty when the configuration is valid.</returns>
    IReadOnlyList<string> Validate(IReadOnlyDictionary<string, string> config);

    /// <summary>Human-readable destination with secrets masked.</summary>
    string Describe(IReadOnlyDictionary<string, string> config);

    Task<SendResult> SendAsync(Notification notification, IReadOnlyDictionary<string, string> config, CancellationToken ct);
}

public enum FieldKind
{
    Text,
    Email,
    Url,
}

public sealed record ChannelField(string Name, string Label, FieldKind Kind, bool IsSecret);

public sealed record NotificationItem(string Title, string Summary, Uri Url, string Reason);

/// <param name="MoreCount">Matches left out of <paramref name="Items"/> because of the batch size cap.</param>
public sealed record Notification(
    string AlertName,
    IReadOnlyList<NotificationItem> Items,
    int MoreCount,
    string Attribution,
    bool IsTest,
    bool IsSynthetic);

public sealed record SendResult(bool Success, string? Error)
{
    public static SendResult Ok() => new(true, null);
    public static SendResult Failed(string error) => new(false, error);
}
