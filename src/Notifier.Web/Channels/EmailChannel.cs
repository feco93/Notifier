using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Notifier.Core.Channels;

namespace Notifier.Web.Channels;

public sealed class EmailOptions
{
    public const string Section = "Notifier:Email";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 2525;
    /// <summary>A MailKit SecureSocketOptions name: None, Auto, StartTls, StartTlsWhenAvailable, SslOnConnect.</summary>
    public string Security { get; set; } = "None";
    public string From { get; set; } = "notifier@localhost.test";
    public string? Username { get; set; }
    public string? Password { get; set; }
}

internal interface IEmailTransport
{
    Task SendAsync(MimeMessage message, CancellationToken ct);
}

internal sealed class MailKitEmailTransport(IOptions<EmailOptions> options) : IEmailTransport
{
    public async Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        var o = options.Value;
        using var client = new SmtpClient { Timeout = 10_000 };
        await client.ConnectAsync(o.Host, o.Port, Enum.Parse<SecureSocketOptions>(o.Security), ct);
        if (!string.IsNullOrEmpty(o.Username))
            await client.AuthenticateAsync(o.Username, o.Password ?? "", ct);
        await client.SendAsync(new MailKit.SendRequest(message), ct);
        await client.DisconnectAsync(true, ct);
    }
}

internal sealed class EmailChannel(IEmailTransport transport, IOptions<EmailOptions> options) : INotificationChannel
{
    private const string AddressField = "address";

    public string Key => "email";
    public string DisplayName => "Email";
    public IReadOnlyList<ChannelField> ConfigFields { get; } = [new(AddressField, "Email address", FieldKind.Email, IsSecret: false)];

    public IReadOnlyList<string> Validate(IReadOnlyDictionary<string, string> config) =>
        TryGetAddress(config, out _) ? [] : ["Enter a single valid email address, e.g. name@example.com."];

    public string Describe(IReadOnlyDictionary<string, string> config)
    {
        if (!TryGetAddress(config, out var address)) return "(invalid email address)";
        var at = address.IndexOf('@');
        return $"{address[0]}***{address[at..]}";
    }

    public async Task<SendResult> SendAsync(Notification notification, IReadOnlyDictionary<string, string> config, CancellationToken ct)
    {
        if (!TryGetAddress(config, out var address))
            return SendResult.Failed("Invalid email destination.");

        var (subject, body) = EmailFormatter.Format(notification);
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Notifier", options.Value.From));
        message.To.Add(MailboxAddress.Parse(address));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        try
        {
            await transport.SendAsync(message, ct);
            return SendResult.Ok();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return SendResult.Failed($"Email could not be sent: {ex.Message}");
        }
    }

    internal static bool IsValidAddress(string? raw, out string address)
    {
        address = raw?.Trim() ?? "";
        if (address.Length is 0 or > 254 || address.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
            return false;
        if (!MailboxAddress.TryParse(address, out var mailbox) || mailbox.Address != address)
            return false;
        var at = address.IndexOf('@');
        return at > 0 && at < address.Length - 1 && address.IndexOf('@', at + 1) < 0;
    }

    private static bool TryGetAddress(IReadOnlyDictionary<string, string> config, out string address)
    {
        address = "";
        return config.TryGetValue(AddressField, out var raw) && IsValidAddress(raw, out address);
    }
}
