namespace Notifier.Core.Domain;

public sealed class Article
{
    public required string Key { get; set; }
    public required string Title { get; set; }
    public string Summary { get; set; } = "";
    public required string Url { get; set; }
    /// <summary>Null when unknown; such articles never match.</summary>
    public DateTime? PublishedAtUtc { get; set; }
    /// <summary>Set on insert only.</summary>
    public DateTime FirstSeenAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
    /// <summary>1-based; null when not in Top stories.</summary>
    public int? TopStoriesRank { get; set; }
    public bool IsSynthetic { get; set; }
    public List<ArticleTopic> Topics { get; set; } = [];
}

public sealed class ArticleTopic
{
    public required string ArticleKey { get; set; }
    public Topic Topic { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
}

public sealed class Alert
{
    public int Id { get; set; }
    public required string OwnerUsername { get; set; }
    public required string Name { get; set; }
    public Tier Tier { get; set; }
    public string? Keyword { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public List<AlertTopic> Topics { get; set; } = [];
    public List<Destination> Destinations { get; set; } = [];
}

public sealed class AlertTopic
{
    public int AlertId { get; set; }
    public Topic Topic { get; set; }
}

/// <summary>ConfigJson may contain secrets; never log it.</summary>
public sealed class Destination
{
    public int Id { get; set; }
    public int AlertId { get; set; }
    public required string ChannelKey { get; set; }
    public required string ConfigJson { get; set; }
}

public sealed class Delivery
{
    public long Id { get; set; }
    /// <summary>Null for test sends.</summary>
    public string? ArticleKey { get; set; }
    public int AlertId { get; set; }
    public int DestinationId { get; set; }
    public Guid? BatchId { get; set; }
    public DeliveryKind Kind { get; set; }
    public DeliveryStatus Status { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class FeedStatus
{
    public Topic Topic { get; set; }
    public DateTime? LastPollAtUtc { get; set; }
    public DateTime? LastSuccessAtUtc { get; set; }
    public string? LastError { get; set; }
    public int ItemsLastFetch { get; set; }
    public int NewLastFetch { get; set; }
    public DateTime? NextPollAtUtc { get; set; }
}
