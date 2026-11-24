using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifier.Core.Channels;
using Notifier.Web.Channels;

namespace Notifier.Tests.Channels;

public sealed class ChannelRegistryTests
{
    private sealed class PagerChannel : INotificationChannel
    {
        public string Key => "pager";
        public string DisplayName => "Pager";
        public IReadOnlyList<ChannelField> ConfigFields => [new("number", "Pager number", FieldKind.Text, false)];
        public IReadOnlyList<string> Validate(IReadOnlyDictionary<string, string> config) => [];
        public string Describe(IReadOnlyDictionary<string, string> config) => "pager";
        public Task<SendResult> SendAsync(Notification n, IReadOnlyDictionary<string, string> config, CancellationToken ct) =>
            Task.FromResult(SendResult.Ok());
    }

    private static ChannelRegistry Build(Action<IServiceCollection>? extra = null)
    {
        var services = new ServiceCollection();
        services.AddNotifierChannels(new ConfigurationBuilder().Build());
        extra?.Invoke(services);
        return services.BuildServiceProvider().GetRequiredService<ChannelRegistry>();
    }

    [Fact]
    public void Email_and_slack_are_registered()
    {
        Assert.Equal(["email", "slack"], Build().All.Select(c => c.Key).Order());
    }

    [Fact]
    public void A_new_channel_needs_only_a_class_and_one_registration()
    {
        var registry = Build(s => s.AddSingleton<INotificationChannel, PagerChannel>());
        Assert.Equal("Pager", registry.Get("pager")!.DisplayName);
    }

    [Fact]
    public void Unknown_key_returns_null()
    {
        Assert.Null(Build().Get("carrier-pigeon"));
    }

    [Fact]
    public void Duplicate_keys_are_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => new ChannelRegistry([new PagerChannel(), new PagerChannel()]));
    }
}
