using System.Net;
using Notifier.Tests.Infrastructure;

namespace Notifier.Tests.Web;

public sealed class AuthTests : IDisposable
{
    private readonly NotifierFactory _factory = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Uses_isolated_test_database()
    {
        using var client = _factory.CreateNoRedirectClient();
        await client.GetAsync("/Login", Ct);
        Assert.True(File.Exists(_factory.DatabasePath), "App did not create the per-test database file");
    }

    [Fact]
    public async Task Anonymous_request_redirects_to_login()
    {
        using var client = _factory.CreateNoRedirectClient();
        var res = await client.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.StartsWith("/Login", res.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task Valid_user_logs_in_and_sees_home()
    {
        using var client = _factory.CreateNoRedirectClient();
        var res = await LoginHelper.PostLoginAsync(client, NotifierFactory.UserName, NotifierFactory.UserPassword);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Equal("/", res.Headers.Location!.OriginalString);

        var home = await client.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains(NotifierFactory.UserName, await home.Content.ReadAsStringAsync(Ct));
    }

    [Theory]
    [InlineData(NotifierFactory.UserName, "wrong-password")]
    [InlineData("no-such-user", "whatever")]
    public async Task Bad_credentials_show_same_error_and_do_not_sign_in(string user, string password)
    {
        using var client = _factory.CreateNoRedirectClient();
        var res = await LoginHelper.PostLoginAsync(client, user, password);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("Invalid username or password.", await res.Content.ReadAsStringAsync(Ct));

        var home = await client.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.Redirect, home.StatusCode); // still anonymous
    }

    [Fact]
    public async Task User_requesting_admin_page_gets_403()
    {
        using var client = _factory.CreateNoRedirectClient();
        await LoginHelper.PostLoginAsync(client, NotifierFactory.UserName, NotifierFactory.UserPassword);
        var res = await client.GetAsync("/Admin", Ct);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Admin_can_open_admin_page()
    {
        using var client = _factory.CreateNoRedirectClient();
        await LoginHelper.PostLoginAsync(client, NotifierFactory.AdminName, NotifierFactory.AdminPassword);
        var res = await client.GetAsync("/Admin", Ct);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task External_return_url_is_ignored()
    {
        using var client = _factory.CreateNoRedirectClient();
        var res = await LoginHelper.PostLoginAsync(client, NotifierFactory.UserName, NotifierFactory.UserPassword,
            returnUrl: "https://evil.example/phish");
        Assert.Equal("/", res.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Logout_button_in_layout_logs_the_user_out()
    {
        using var client = _factory.CreateNoRedirectClient();
        await LoginHelper.PostLoginAsync(client, NotifierFactory.UserName, NotifierFactory.UserPassword);

        var html = await client.GetStringAsync("/", Ct);
        var form = System.Text.RegularExpressions.Regex.Match(html, "<form[^>]*/Logout[^>]*>(.*?)</form>",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.True(form.Success, "Logout form not found in layout");
        var token = System.Text.RegularExpressions.Regex.Match(form.Groups[1].Value,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "Logout form has no antiforgery token, so the POST would be rejected");

        var res = await client.PostAsync("/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = System.Net.WebUtility.HtmlDecode(token.Groups[1].Value),
        }), Ct);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

        var home = await client.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.Redirect, home.StatusCode); // anonymous again
    }

    [Fact]
    public async Task Logout_via_get_does_not_log_out()
    {
        using var client = _factory.CreateNoRedirectClient();
        await LoginHelper.PostLoginAsync(client, NotifierFactory.UserName, NotifierFactory.UserPassword);
        await client.GetAsync("/Logout", Ct);
        var home = await client.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
    }

    [Fact]
    public async Task Login_post_without_antiforgery_token_is_rejected()
    {
        using var client = _factory.CreateNoRedirectClient();
        var res = await client.PostAsync("/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = NotifierFactory.UserName,
            ["Password"] = NotifierFactory.UserPassword,
        }), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
