using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notifier.Core.Domain;
using Notifier.Core.Sources;
using Notifier.Sources.Bbc;
using Notifier.Tests.Infrastructure;

namespace Notifier.Tests.Sources;

public sealed class BbcRssSourceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly string FixtureXml =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "bbc-shaped-feed.xml"));

    private static IArticleSource Resolve(FakeHttpHandler handler, Dictionary<string, string?>? config = null)
    {
        var services = new ServiceCollection();
        services.AddBbcNewsSource(new ConfigurationBuilder().AddInMemoryCollection(config ?? []).Build());
        services.AddHttpClient<IArticleSource, BbcRssSource>().ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider().GetRequiredService<IArticleSource>();
    }

    [Fact]
    public async Task Fetches_the_topic_feed_with_an_identifying_user_agent()
    {
        var handler = FakeHttpHandler.Returning(HttpStatusCode.OK, FixtureXml);
        var result = await Resolve(handler).FetchAsync(Topic.World, Ct);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://feeds.bbci.co.uk/news/world/rss.xml", request.RequestUri!.ToString());
        Assert.Contains("NotifierPrototype", request.Headers.UserAgent.ToString());
        Assert.Equal(Topic.World, result.Topic);
        Assert.Equal(8, result.Items.Count);
    }

    [Fact]
    public async Task Feed_urls_can_be_overridden_in_configuration()
    {
        var handler = FakeHttpHandler.Returning(HttpStatusCode.OK, FixtureXml);
        var source = Resolve(handler, new() { ["Notifier:Bbc:Feeds:Technology"] = "https://example.test/tech.xml" });

        await source.FetchAsync(Topic.Technology, Ct);

        Assert.Equal("https://example.test/tech.xml", handler.Requests.Single().RequestUri!.ToString());
    }

    [Fact]
    public async Task Http_error_becomes_source_exception()
    {
        var source = Resolve(FakeHttpHandler.Returning(HttpStatusCode.ServiceUnavailable));
        var ex = await Assert.ThrowsAsync<SourceException>(() => source.FetchAsync(Topic.TopStories, Ct));
        Assert.Contains("503", ex.Message);
    }

    [Theory]
    [InlineData("<rss><channel>")]
    [InlineData("<rss version=\"2.0\"><channel><title>t</title>")]
    [InlineData("<html><body>Service temporarily unavailable</body></html>")]
    [InlineData("")]
    public async Task Unreadable_body_becomes_source_exception(string body)
    {
        var source = Resolve(FakeHttpHandler.Returning(HttpStatusCode.OK, body));
        await Assert.ThrowsAsync<SourceException>(() => source.FetchAsync(Topic.TopStories, Ct));
    }

    [Fact]
    public async Task Timeout_becomes_source_exception()
    {
        var handler = new FakeHttpHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var source = Resolve(handler, new() { ["Notifier:Bbc:Timeout"] = "00:00:00.200" });

        await Assert.ThrowsAsync<SourceException>(() => source.FetchAsync(Topic.TopStories, Ct));
    }

    [Fact]
    public async Task Caller_cancellation_is_not_reported_as_a_feed_failure()
    {
        var handler = new FakeHttpHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Resolve(handler).FetchAsync(Topic.TopStories, cts.Token));
    }

    [Fact]
    public async Task Oversized_response_becomes_source_exception()
    {
        var source = Resolve(FakeHttpHandler.Returning(HttpStatusCode.OK, new string('x', 6_000_000)));
        await Assert.ThrowsAsync<SourceException>(() => source.FetchAsync(Topic.TopStories, Ct));
    }

    [Theory]
    [InlineData("Notifier:Bbc:Feeds:World", "not a url")]
    [InlineData("Notifier:Bbc:Feeds:World", "file:///etc/passwd")]
    [InlineData("Notifier:Bbc:Timeout", "00:00:00")]
    [InlineData("Notifier:Bbc:UserAgent", "bad user agent ((")]
    public void Invalid_configuration_is_rejected_when_the_source_is_created(string key, string value)
    {
        var services = new ServiceCollection();
        services.AddBbcNewsSource(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [key] = value }).Build());
        using var sp = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IArticleSource>());
    }

    [Fact]
    public async Task Unconfigured_topic_is_rejected()
    {
        var options = Options.Create(new BbcOptions { Feeds = new() { [Topic.World] = "https://example.test/w.xml" } });
        var source = new BbcRssSource(new HttpClient(FakeHttpHandler.Returning(HttpStatusCode.OK, FixtureXml)), options);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => source.FetchAsync(Topic.Business, Ct));
        Assert.Equal([Topic.World], source.SupportedTopics);
    }

    [Fact]
    public void Only_the_registration_method_is_public()
    {
        var exported = typeof(BbcServiceCollectionExtensions).Assembly.GetExportedTypes();
        Assert.Equal([typeof(BbcServiceCollectionExtensions)], exported);
    }
}
