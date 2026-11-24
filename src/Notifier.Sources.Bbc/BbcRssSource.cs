using Microsoft.Extensions.Options;
using Notifier.Core.Domain;
using Notifier.Core.Sources;

namespace Notifier.Sources.Bbc;

internal sealed class BbcRssSource(HttpClient http, IOptions<BbcOptions> options) : IArticleSource
{
    public string Attribution => "Source: BBC News";

    public IReadOnlyList<Topic> SupportedTopics => [.. options.Value.Feeds.Keys];

    public async Task<SourceFetchResult> FetchAsync(Topic topic, CancellationToken ct)
    {
        if (!options.Value.Feeds.TryGetValue(topic, out var feedUrl))
            throw new ArgumentOutOfRangeException(nameof(topic), topic, "No BBC feed configured for this topic.");

        byte[] body;
        try
        {
            using var response = await http.GetAsync(feedUrl, ct);
            if (!response.IsSuccessStatusCode)
                throw new SourceException($"BBC feed for {topic} returned HTTP {(int)response.StatusCode}.");
            body = await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // TaskCanceledException without caller cancellation means the HttpClient timeout fired.
            throw new SourceException($"BBC feed for {topic} could not be fetched: {ex.Message}", ex);
        }

        try
        {
            return new SourceFetchResult(topic, BbcFeedParser.Parse(new MemoryStream(body)));
        }
        catch (Exception ex)
        {
            // Untrusted input: the parser throws XmlException, NotSupportedException and others.
            throw new SourceException($"BBC feed for {topic} could not be parsed: {ex.Message}", ex);
        }
    }
}
