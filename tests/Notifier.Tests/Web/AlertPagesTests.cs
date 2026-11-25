using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Notifier.Core.Channels;
using Notifier.Core.Domain;
using Notifier.Tests.Infrastructure;
using Notifier.Web.Data;

namespace Notifier.Tests.Web;

public sealed class AlertPagesTests : IDisposable
{
    private const string Webhook = "https://hooks.slack.com/services/T000/B000/XXXXSECRETXXXX";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private readonly PageTestChannel _fake = new();
    private readonly NotifierFactory _factory;

    public AlertPagesTests() =>
        _factory = new NotifierFactory { ExtraServices = s => s.AddSingleton<INotificationChannel>(_fake) };

    public void Dispose() => _factory.Dispose();

    private sealed class PageTestChannel : INotificationChannel
    {
        public List<Notification> Sent { get; } = [];
        public SendResult Result { get; set; } = SendResult.Ok();
        public string Key => "fake";
        public string DisplayName => "Fake";
        public IReadOnlyList<ChannelField> ConfigFields => [new("target", "Target", FieldKind.Text, false)];
        public IReadOnlyList<string> Validate(IReadOnlyDictionary<string, string> config) =>
            config.TryGetValue("target", out var t) && t.Length > 0 ? [] : ["Target required."];
        public string Describe(IReadOnlyDictionary<string, string> config) => $"fake:{config["target"]}";
        public Task<SendResult> SendAsync(Notification n, IReadOnlyDictionary<string, string> config, CancellationToken ct)
        {
            Sent.Add(n);
            return Task.FromResult(Result);
        }
    }

    private async Task<HttpClient> LoginAsync(string user = NotifierFactory.UserName, string password = NotifierFactory.UserPassword)
    {
        var client = _factory.CreateNoRedirectClient();
        await LoginHelper.PostLoginAsync(client, user, password);
        return client;
    }

    private static List<KeyValuePair<string, string>> Form(string name = "Tech news", string tier = "All", string? topic = "Technology",
        string? email = "alice@example.com", string? webhook = null, string? fakeTarget = null)
    {
        var f = new List<KeyValuePair<string, string>> { new("Input.Name", name), new("Input.Tier", tier) };
        if (topic is not null) f.Add(new("Input.Topics", topic));
        if (email is not null) { f.Add(new("Input.Destinations[email].Use", "true")); f.Add(new("Input.Destinations[email].Fields[address]", email)); }
        if (webhook is not null) { f.Add(new("Input.Destinations[slack].Use", "true")); f.Add(new("Input.Destinations[slack].Fields[webhookUrl]", webhook)); }
        if (fakeTarget is not null) { f.Add(new("Input.Destinations[fake].Use", "true")); f.Add(new("Input.Destinations[fake].Fields[target]", fakeTarget)); }
        return f;
    }

    private Task<HttpResponseMessage> CreateAsync(HttpClient client, List<KeyValuePair<string, string>> form) =>
        LoginHelper.PostFormAsync(client, "/Alerts/Create", "/Alerts/Create", form);

    private (int AlertId, int DestinationId) Latest()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotifierDbContext>();
        var alert = db.Alerts.OrderByDescending(a => a.Id).First();
        return (alert.Id, db.Destinations.First(d => d.AlertId == alert.Id).Id);
    }

    private T Db<T>(Func<NotifierDbContext, T> query)
    {
        using var scope = _factory.Services.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<NotifierDbContext>());
    }

    [Fact]
    public async Task Create_form_has_a_block_for_every_registered_channel()
    {
        using var client = await LoginAsync();
        var html = await client.GetStringAsync("/Alerts/Create", Ct);
        Assert.Contains("name=\"Input.Destinations[email].Fields[address]\"", html);
        Assert.Contains("name=\"Input.Destinations[slack].Fields[webhookUrl]\"", html);
        Assert.Contains("name=\"Input.Destinations[fake].Fields[target]\"", html);
    }

    [Fact]
    public async Task Valid_form_creates_the_alert_and_the_list_shows_it_masked()
    {
        using var client = await LoginAsync();
        var res = await CreateAsync(client, Form(webhook: Webhook));
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

        var list = await client.GetStringAsync("/", Ct);
        Assert.Contains("Tech news", list);
        Assert.Contains("a***@example.com", list);
        Assert.DoesNotContain("alice@example.com", list);
        Assert.DoesNotContain("SECRET", list);
        Assert.DoesNotContain("T000", list);
    }

    [Fact]
    public async Task Invalid_form_shows_field_errors_and_saves_nothing()
    {
        using var client = await LoginAsync();
        var res = await CreateAsync(client, Form(name: "", topic: null, email: "nope"));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync(Ct);
        Assert.Contains("Enter a name", html);
        Assert.Contains("Choose at least one topic", html);
        Assert.Contains("valid email address", html);
        Assert.Empty(Db(db => db.Alerts.ToList()));
    }

    [Fact]
    public async Task Rejected_form_does_not_echo_secret_fields_back()
    {
        using var client = await LoginAsync();
        var res = await CreateAsync(client, Form(name: "", webhook: Webhook));
        Assert.DoesNotContain("SECRET", await res.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task User_text_is_html_encoded_in_the_list_and_messages()
    {
        using var client = await LoginAsync();
        var form = Form(name: "<script>alert(1)</script>", tier: "Headline", topic: null);
        form.Add(new("Input.Keyword", "<b>x</b>"));
        var res = await CreateAsync(client, form);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

        var list = await client.GetStringAsync("/", Ct);
        Assert.DoesNotContain("<script>alert(1)</script>", list);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", list);
        Assert.DoesNotContain("<b>x</b>", list);
    }

    [Fact]
    public async Task Toggle_message_names_the_new_state()
    {
        using var client = await LoginAsync();
        await CreateAsync(client, Form());
        var (id, _) = Latest();

        await LoginHelper.PostFormAsync(client, "/", $"/?handler=Toggle&id={id}", []);
        Assert.Contains("Alert &#x27;Tech news&#x27; disabled.", await client.GetStringAsync("/", Ct));
        await LoginHelper.PostFormAsync(client, "/", $"/?handler=Toggle&id={id}", []);
        Assert.Contains("Alert &#x27;Tech news&#x27; enabled.", await client.GetStringAsync("/", Ct));
    }

    [Fact]
    public async Task Disable_and_enable_from_the_list()
    {
        using var client = await LoginAsync();
        await CreateAsync(client, Form());
        var (id, _) = Latest();

        await LoginHelper.PostFormAsync(client, "/", $"/?handler=Toggle&id={id}", []);
        Assert.False(Db(db => db.Alerts.Single().Enabled));
        Assert.Contains("Disabled", await client.GetStringAsync("/", Ct));

        await LoginHelper.PostFormAsync(client, "/", $"/?handler=Toggle&id={id}", []);
        Assert.True(Db(db => db.Alerts.Single().Enabled));
    }

    [Fact]
    public async Task Delete_asks_for_confirmation_then_hides_the_alert()
    {
        using var client = await LoginAsync();
        await CreateAsync(client, Form());
        var (id, _) = Latest();

        var confirm = await client.GetAsync($"/Alerts/Delete/{id}", Ct);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Contains("Tech news", await confirm.Content.ReadAsStringAsync(Ct));
        Assert.Null(Db(db => db.Alerts.Single().DeletedAtUtc)); // GET only asks; nothing deleted yet

        var res = await LoginHelper.PostFormAsync(client, $"/Alerts/Delete/{id}", $"/Alerts/Delete/{id}", []);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(Db(db => db.Alerts.Single().DeletedAtUtc));
        var list = await client.GetStringAsync("/", Ct);
        Assert.Contains("You have no alerts yet.", list); // the name still appears in the "deleted" message
    }

    [Fact]
    public async Task Another_users_alert_is_404_everywhere()
    {
        using (var admin = await LoginAsync(NotifierFactory.AdminName, NotifierFactory.AdminPassword))
            await CreateAsync(admin, Form(name: "Admin's alert", fakeTarget: "x"));
        var (id, destinationId) = Latest();

        using var client = await LoginAsync();
        Assert.DoesNotContain("Admin's alert", await client.GetStringAsync("/", Ct));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Alerts/Delete/{id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await LoginHelper.PostFormAsync(client, "/", $"/?handler=Toggle&id={id}", [])).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await LoginHelper.PostFormAsync(client, "/", $"/?handler=Test&alertId={id}&destinationId={destinationId}", [])).StatusCode);
        Assert.Empty(_fake.Sent);
        Assert.True(Db(db => db.Alerts.Single().Enabled));
    }

    [Fact]
    public async Task Test_button_sends_a_test_message_and_shows_the_result()
    {
        using var client = await LoginAsync();
        await CreateAsync(client, Form(email: null, fakeTarget: "inbox"));
        var (id, destinationId) = Latest();

        var res = await LoginHelper.PostFormAsync(client, "/", $"/?handler=Test&alertId={id}&destinationId={destinationId}", []);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

        Assert.True(Assert.Single(_fake.Sent).IsTest);
        Assert.Contains("Test message sent", await client.GetStringAsync("/", Ct));
    }

    [Fact]
    public async Task Failed_test_shows_the_channel_error()
    {
        _fake.Result = SendResult.Failed("Slack returned HTTP 404: channel_not_found");
        using var client = await LoginAsync();
        await CreateAsync(client, Form(email: null, fakeTarget: "inbox"));
        var (id, destinationId) = Latest();

        await LoginHelper.PostFormAsync(client, "/", $"/?handler=Test&alertId={id}&destinationId={destinationId}", []);
        Assert.Contains("channel_not_found", await client.GetStringAsync("/", Ct));
    }
}
