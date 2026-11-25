using System.Net;
using System.Text.RegularExpressions;

namespace Notifier.Tests.Infrastructure;

public static partial class LoginHelper
{
    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path, TestContext.Current.CancellationToken);
        var m = TokenRegex().Match(html);
        Assert.True(m.Success, $"No antiforgery token found on {path}");
        return WebUtility.HtmlDecode(m.Groups[1].Value);
    }

    public static async Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string user, string password, string? returnUrl = null)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/Login");
        var fields = new Dictionary<string, string>
        {
            ["Username"] = user,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token,
        };
        if (returnUrl is not null) fields["ReturnUrl"] = returnUrl;
        return await client.PostAsync("/Login", new FormUrlEncodedContent(fields), TestContext.Current.CancellationToken);
    }

    /// <summary>Posts a form with the antiforgery token taken from <paramref name="formPage"/>.</summary>
    public static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string formPage, string postUrl,
        IEnumerable<KeyValuePair<string, string>> fields)
    {
        var token = await GetAntiforgeryTokenAsync(client, formPage);
        var all = fields.Append(new("__RequestVerificationToken", token));
        return await client.PostAsync(postUrl, new FormUrlEncodedContent(all), TestContext.Current.CancellationToken);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();
}
