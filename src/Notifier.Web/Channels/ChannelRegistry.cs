using MailKit.Security;
using Notifier.Core.Channels;

namespace Notifier.Web.Channels;

public sealed class ChannelRegistry
{
    private readonly Dictionary<string, INotificationChannel> _byKey;

    public ChannelRegistry(IEnumerable<INotificationChannel> channels)
    {
        _byKey = new Dictionary<string, INotificationChannel>(StringComparer.Ordinal);
        foreach (var channel in channels)
            if (!_byKey.TryAdd(channel.Key, channel))
                throw new InvalidOperationException($"Two notification channels use the key '{channel.Key}'.");
    }

    public IReadOnlyList<INotificationChannel> All => [.. _byKey.Values.OrderBy(c => c.DisplayName, StringComparer.Ordinal)];

    public INotificationChannel? Get(string key) => _byKey.GetValueOrDefault(key);
}

public static class ChannelServiceCollectionExtensions
{
    public static IServiceCollection AddNotifierChannels(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.Section))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Host), "Email host is required.")
            .Validate(o => o.Port is > 0 and <= 65535, "Email port must be 1-65535.")
            .Validate(o => Enum.TryParse<SecureSocketOptions>(o.Security, out _), "Email security must be a MailKit SecureSocketOptions name.")
            .Validate(o => EmailChannel.IsValidAddress(o.From, out _), "Email 'From' must be a valid address.")
            .ValidateOnStart();
        services.AddSingleton<IEmailTransport, MailKitEmailTransport>();
        services.AddSingleton<INotificationChannel, EmailChannel>();

        services.AddHttpClient(SlackChannel.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(10))
            // A redirect could send the payload to a host the URL check never saw.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        services.AddSingleton<INotificationChannel, SlackChannel>();

        services.AddSingleton<ChannelRegistry>();
        return services;
    }
}
