// Spike: send a notification-shaped email via MailKit to a local smtp4dev (localhost:2525),
// including a header-injection attempt in the subject to see how MimeKit handles it.
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

var msg = new MimeMessage();
msg.From.Add(new MailboxAddress("Notifier", "notifier@localhost.test"));
msg.To.Add(new MailboxAddress("User", "user@localhost.test"));
// Article titles are untrusted input: try to smuggle an extra header via CR/LF.
msg.Subject = "[Notifier] #2 on BBC Top stories: Example title\r\nBcc: attacker@evil.test";
msg.Body = new TextPart("plain")
{
    Text = "Example title\nWhy: #2 on BBC Top stories\nhttps://www.bbc.co.uk/news/articles/abc123\nSource: BBC News"
};

using var client = new SmtpClient();
await client.ConnectAsync("localhost", 2525, SecureSocketOptions.None);
// NOTE: first attempt used the MailKit 4.x API SendAsync(MimeMessage) from model memory -> CS1503.
// MailKit 5.0 (verified from its XML docs) takes an ISendRequest; SendRequest wraps the message.
await client.SendAsync(new MailKit.SendRequest(msg));
await client.DisconnectAsync(true);
Console.WriteLine("sent");
