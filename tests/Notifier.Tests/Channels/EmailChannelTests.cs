using Microsoft.Extensions.Options;
using MimeKit;
using Notifier.Web.Channels;
using static Notifier.Tests.Channels.ChannelTestData;

namespace Notifier.Tests.Channels;

public sealed class EmailChannelTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class CapturingTransport : IEmailTransport
    {
        public List<MimeMessage> Sent { get; } = [];
        public Exception? Throw { get; set; }
        public Task SendAsync(MimeMessage message, CancellationToken ct)
        {
            if (Throw is not null) throw Throw;
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private static (EmailChannel channel, CapturingTransport transport) NewChannel()
    {
        var transport = new CapturingTransport();
        return (new EmailChannel(transport, Options.Create(new EmailOptions { From = "notifier@localhost.test" })), transport);
    }

    // ---- Formatting --------------------------------------------------------

    [Fact]
    public void Single_item_subject_has_reason_and_title()
    {
        var (subject, _) = EmailFormatter.Format(Single());
        Assert.Equal("[Notifier] #1 in Top stories: Story 1", subject);
    }

    [Fact]
    public void Single_item_body_has_title_reason_summary_link_attribution_and_alert()
    {
        var (_, body) = EmailFormatter.Format(Single());
        foreach (var part in new[] { "Story 1", "#1 in Top stories", "Summary 1.", "https://www.bbc.co.uk/news/articles/a1", "Source: BBC News", "Tech headlines" })
            Assert.Contains(part, body);
    }

    [Fact]
    public void Batch_subject_counts_all_matches_including_the_ones_left_out()
    {
        var (subject, body) = EmailFormatter.Format(Batch(3, more: 2));
        Assert.Equal("[Notifier] 5 new stories for 'Tech headlines'", subject);
        Assert.Contains("1. Story 1", body);
        Assert.Contains("3. Story 3", body);
        Assert.Contains("and 2 more", body);
    }

    [Theory]
    [InlineData(true, false, "[Notifier] [TEST] ")]
    [InlineData(false, true, "[Notifier] [SYNTHETIC] ")]
    public void Test_and_synthetic_notifications_are_tagged(bool test, bool synthetic, string prefix)
    {
        var (subject, _) = EmailFormatter.Format(Single(test: test, synthetic: synthetic));
        Assert.StartsWith(prefix, subject);
    }

    [Fact]
    public void Subject_cannot_carry_line_breaks_or_control_characters()
    {
        var (subject, _) = EmailFormatter.Format(Single(Item(title: "Evil\r\nBcc: x@evil.test\tdone\u0007")));
        Assert.DoesNotContain('\r', subject);
        Assert.DoesNotContain('\n', subject);
        Assert.DoesNotContain('\t', subject);
        Assert.DoesNotContain('\u0007', subject);
        Assert.EndsWith("Evil Bcc: x@evil.test done", subject);
    }

    [Fact]
    public void Long_subject_is_truncated()
    {
        var (subject, _) = EmailFormatter.Format(Single(Item(title: new string('x', 500))));
        Assert.True(subject.Length <= 200, $"subject length {subject.Length}");
        Assert.EndsWith("…", subject);
    }

    // ---- Validation and masking -------------------------------------------

    [Theory]
    [InlineData("alice@example.com", true)]
    [InlineData("  alice@example.com  ", true)]
    [InlineData("not-an-email", false)]
    [InlineData("", false)]
    [InlineData("Alice <alice@example.com>", false)]
    [InlineData("Alice<alice@example.com>", false)]
    [InlineData("\"a@b\"@example.com", false)]
    [InlineData("alice@example.com\r\nBcc: x@evil.test", false)]
    [InlineData("alice@example.com, bob@example.com", false)]
    public void Validates_a_single_plain_address(string address, bool valid)
    {
        var (channel, _) = NewChannel();
        Assert.Equal(valid, channel.Validate(Config(("address", address))).Count == 0);
    }

    [Fact]
    public void Missing_address_is_invalid()
    {
        var (channel, _) = NewChannel();
        Assert.NotEmpty(channel.Validate(Config()));
    }

    [Fact]
    public void Describe_masks_the_local_part()
    {
        var (channel, _) = NewChannel();
        Assert.Equal("a***@example.com", channel.Describe(Config(("address", "alice@example.com"))));
    }

    // ---- Sending -----------------------------------------------------------

    [Fact]
    public async Task Sends_one_plain_text_message_to_the_destination()
    {
        var (channel, transport) = NewChannel();
        var result = await channel.SendAsync(Single(), Config(("address", "alice@example.com")), Ct);

        Assert.True(result.Success);
        var msg = Assert.Single(transport.Sent);
        Assert.Equal("alice@example.com", Assert.Single(msg.To.Mailboxes).Address);
        Assert.Equal("notifier@localhost.test", Assert.Single(msg.From.Mailboxes).Address);
        Assert.Empty(msg.Bcc);
        Assert.Equal("[Notifier] #1 in Top stories: Story 1", msg.Subject);
        Assert.IsType<TextPart>(msg.Body);
        Assert.True(((TextPart)msg.Body).IsPlain);
    }

    [Fact]
    public async Task Invalid_destination_is_refused_without_sending()
    {
        var (channel, transport) = NewChannel();
        var result = await channel.SendAsync(Single(), Config(("address", "nope")), Ct);
        Assert.False(result.Success);
        Assert.Empty(transport.Sent);
    }

    [Fact]
    public async Task Transport_failure_becomes_a_failed_result()
    {
        var (channel, transport) = NewChannel();
        transport.Throw = new InvalidOperationException("SMTP server unavailable");
        var result = await channel.SendAsync(Single(), Config(("address", "alice@example.com")), Ct);
        Assert.False(result.Success);
        Assert.Contains("SMTP server unavailable", result.Error);
    }
}
