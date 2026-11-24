using Notifier.Core.Domain;

namespace Notifier.Core.Sources;

public interface IArticleSource
{
    /// <summary>Shown on every notification, e.g. "Source: BBC News".</summary>
    string Attribution { get; }

    IReadOnlyList<Topic> SupportedTopics { get; }

    /// <exception cref="SourceException">The feed could not be fetched or parsed.</exception>
    Task<SourceFetchResult> FetchAsync(Topic topic, CancellationToken ct);
}

/// <param name="Key">Stable identity defined by the source.</param>
/// <param name="PublishedAtUtc">Null when missing or unparseable.</param>
/// <param name="Section">0-based feed section; 0 is the lead section, which carries the ranking.</param>
/// <param name="Position">0-based order within the section.</param>
public sealed record SourceItem(
    string Key,
    string Title,
    string Summary,
    Uri Url,
    DateTime? PublishedAtUtc,
    int Section,
    int Position);

public sealed record SourceFetchResult(Topic Topic, IReadOnlyList<SourceItem> Items);

public sealed class SourceException(string message, Exception? inner = null) : Exception(message, inner);
