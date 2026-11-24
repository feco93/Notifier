using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifier.Core.Channels;
using Notifier.Web.Channels;
using static Notifier.Tests.Channels.ChannelTestData;

namespace Notifier.Tests.Channels;

/// <summary>
/// Runs only when a local smtp4dev is up and NOTIFIER_E2E_SMTP4DEV is set to its web URL,
/// e.g. http://localhost:5080 (SMTP expected on localhost:2525).
/// </summary>
public sealed class EmailEndToEndTests
{
    [Fact]
    public async Task Real_email_reaches_the_local_mail_catcher()
    {
        var web = Environment.GetEnvironmentVariable("NOTIFIER_E2E_SMTP4DEV");
        Assert.SkipWhen(string.IsNullOrEmpty(web), "Set NOTIFIER_E2E_SMTP4DEV to run against a local smtp4dev.");
        var ct = TestContext.Current.CancellationToken;

        var services = new ServiceCollection();
        services.AddNotifierChannels(new ConfigurationBuilder().Build());
        var email = services.BuildServiceProvider().GetRequiredService<ChannelRegistry>().Get("email")!;
        var marker = Guid.NewGuid().ToString("N")[..8];

        var result = await email.SendAsync(Single(Item(title: $"E2E {marker}")), Config(("address", "e2e@localhost.test")), ct);
        Assert.True(result.Success, result.Error);

        using var http = new HttpClient { BaseAddress = new Uri(web!) };
        var messages = JsonDocument.Parse(await http.GetStringAsync("/api/messages", ct)).RootElement;
        var list = messages.ValueKind == JsonValueKind.Array ? messages : messages.GetProperty("results");
        Assert.Contains(list.EnumerateArray(), m => m.GetProperty("subject").GetString()!.Contains(marker));
    }
}
