using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Notifier.Tests.Infrastructure;
using Notifier.Web.Channels;
using static Notifier.Tests.Channels.ChannelTestData;

namespace Notifier.Tests.Channels;

public sealed class SlackChannelTests
{
    private const string Webhook = "https://hooks.slack.com/services/T000/B000/XXXXSECRETXXXX";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static SlackChannel NewChannel(FakeHttpHandler handler)
    {
        var services = new ServiceCollection();
        services.AddHttpClient(SlackChannel.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        return new SlackChannel(services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>());
    }

    private static string TextOf(string payload) => JsonDocument.Parse(payload).RootElement.GetProperty("text").GetString()!;

    // ---- Formatting --------------------------------------------------------

    [Fact]
    public void Single_item_links_title_and_shows_reason_summary_and_attribution()
    {
        var text = SlackFormatter.Text(Single());
        Assert.Contains("<https://www.bbc.co.uk/news/articles/a1|Story 1>", text);
        Assert.Contains("#1 in Top stories", text);
        Assert.Contains("Summary 1.", text);
        Assert.Contains("Source: BBC News", text);
    }

    [Fact]
    public void Control_characters_are_escaped_in_all_text()
    {
        var text = SlackFormatter.Text(Single(Item(title: "A <b> & C", summary: "x > y", reason: "<!channel>")));
        Assert.Contains("A &lt;b&gt; &amp; C", text);
        Assert.Contains("x &gt; y", text);
        Assert.Contains("&lt;!channel&gt;", text);
        Assert.DoesNotContain("<!channel>", text);
    }

    [Fact]
    public void Url_cannot_break_out_of_the_link_markup()
    {
        var item = new Notifier.Core.Channels.NotificationItem("T", "", new Uri("https://example.test/a?x=1&y=2|z>"), "r");
        var text = SlackFormatter.Text(Single(item));
        Assert.Contains("<https://example.test/a?x=1&amp;y=2%7Cz%3E|T>", text);
    }

    [Fact]
    public void Uri_already_percent_encodes_the_characters_that_would_break_slack_link_markup()
    {
        // The formatter relies on this instead of re-encoding by hand.
        var encoded = new Uri("https://example.test/a|b<c>d e").AbsoluteUri;
        Assert.Equal("https://example.test/a%7Cb%3Cc%3Ed%20e", encoded);
    }

    [Fact]
    public void Batch_lists_items_and_counts_the_rest()
    {
        var text = SlackFormatter.Text(Batch(3, more: 2));
        Assert.Contains("5 new stories for 'Tech headlines'", text);
        Assert.Contains("<https://www.bbc.co.uk/news/articles/a3|Story 3>", text);
        Assert.Contains("and 2 more", text);
    }

    [Fact]
    public void Batch_stays_within_slacks_recommended_length_by_moving_items_into_the_more_count()
    {
        var items = Enumerable.Range(1, 20).Select(i => Item(i, title: new string('t', 280))).ToList();
        var text = SlackFormatter.Text(new("Tech headlines", items, 3, "Source: BBC News", false, false));
        Assert.True(text.Length <= SlackFormatter.MaxTextLength, $"length {text.Length}");
        var shown = items.Count(i => text.Contains(i.Url.ToString()));
        Assert.True(shown < 20, "expected some items to be moved into the 'more' count");
        Assert.Contains($"and {20 - shown + 3} more", text);
    }

    [Theory]
    [InlineData(true, "[TEST]")]
    [InlineData(false, "[SYNTHETIC]")]
    public void Test_and_synthetic_messages_are_tagged(bool test, string tag)
    {
        Assert.StartsWith(tag, SlackFormatter.Text(Single(test: test, synthetic: !test)));
    }

    [Fact]
    public void Payload_is_json_with_text_and_disables_unfurling()
    {
        var root = JsonDocument.Parse(SlackFormatter.Payload(Single())).RootElement;
        Assert.Equal(JsonValueKind.String, root.GetProperty("text").ValueKind);
        Assert.False(root.GetProperty("unfurl_links").GetBoolean());
        Assert.False(root.GetProperty("unfurl_media").GetBoolean());
    }

    // ---- Validation (SSRF guard) and masking -------------------------------

    [Theory]
    [InlineData(Webhook, true)]
    [InlineData("https://HOOKS.SLACK.COM/services/T000/B000/abc", true)]
    [InlineData("http://hooks.slack.com/services/T000/B000/abc", false)]
    [InlineData("https://hooks.slack.com.evil.test/services/T000/B000/abc", false)]
    [InlineData("https://evil.test/hooks.slack.com/services/abc", false)]
    [InlineData("https://user@hooks.slack.com/services/T000/B000/abc", false)]
    [InlineData("https://hooks.slack.com:8443/services/T000/B000/abc", false)]
    [InlineData("https://hooks.slack.com/api/chat.postMessage", false)]
    [InlineData("https://169.254.169.254/services/x", false)]
    [InlineData("not a url", false)]
    [InlineData("", false)]
    public void Only_slack_incoming_webhook_urls_are_accepted(string url, bool valid)
    {
        var channel = NewChannel(FakeHttpHandler.Returning(HttpStatusCode.OK, "ok"));
        Assert.Equal(valid, channel.Validate(Config(("webhookUrl", url))).Count == 0);
    }

    [Fact]
    public void Webhook_url_is_declared_secret_and_never_shown()
    {
        var channel = NewChannel(FakeHttpHandler.Returning(HttpStatusCode.OK, "ok"));
        Assert.True(channel.ConfigFields.Single(f => f.Name == "webhookUrl").IsSecret);
        var described = channel.Describe(Config(("webhookUrl", Webhook)));
        Assert.DoesNotContain("T000", described);
        Assert.DoesNotContain("SECRET", described);
    }

    // ---- Sending -----------------------------------------------------------

    [Fact]
    public async Task Posts_json_payload_to_the_webhook()
    {
        var handler = FakeHttpHandler.Returning(HttpStatusCode.OK, "ok");
        var result = await NewChannel(handler).SendAsync(Single(), Config(("webhookUrl", Webhook)), Ct);

        Assert.True(result.Success);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(Webhook, request.RequestUri!.ToString());
        Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
        Assert.Contains("Story 1", TextOf(handler.Bodies.Single()!));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "invalid_payload")]
    [InlineData(HttpStatusCode.NotFound, "channel_not_found")]
    [InlineData(HttpStatusCode.Found, "")]
    public async Task Non_success_response_is_a_failure_that_does_not_leak_the_webhook(HttpStatusCode status, string body)
    {
        var result = await NewChannel(FakeHttpHandler.Returning(status, body)).SendAsync(Single(), Config(("webhookUrl", Webhook)), Ct);
        Assert.False(result.Success);
        Assert.Contains(((int)status).ToString(), result.Error);
        Assert.Contains(body, result.Error);
        Assert.DoesNotContain("SECRET", result.Error);
    }

    [Fact]
    public async Task Invalid_webhook_is_refused_without_any_request()
    {
        var handler = FakeHttpHandler.Returning(HttpStatusCode.OK, "ok");
        var result = await NewChannel(handler).SendAsync(Single(), Config(("webhookUrl", "https://evil.test/x")), Ct);
        Assert.False(result.Success);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Network_failure_becomes_a_failed_result()
    {
        var handler = new FakeHttpHandler((_, _) => throw new HttpRequestException("No such host is known."));
        var result = await NewChannel(handler).SendAsync(Single(), Config(("webhookUrl", Webhook)), Ct);
        Assert.False(result.Success);
        Assert.Contains("No such host", result.Error);
    }

    [Fact]
    public void Registered_slack_client_does_not_follow_redirects()
    {
        var services = new ServiceCollection();
        services.AddNotifierChannels(new ConfigurationBuilder().Build());
        var factory = services.BuildServiceProvider().GetRequiredService<IHttpMessageHandlerFactory>();

        HttpMessageHandler? handler = factory.CreateHandler(SlackChannel.HttpClientName);
        while (handler is DelegatingHandler d) handler = d.InnerHandler;
        var sockets = Assert.IsType<SocketsHttpHandler>(handler);
        Assert.False(sockets.AllowAutoRedirect);
    }
}
