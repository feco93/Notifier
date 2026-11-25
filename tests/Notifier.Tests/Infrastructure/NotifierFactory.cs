using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Notifier.Web.Auth;

namespace Notifier.Tests.Infrastructure;

public sealed class NotifierFactory : WebApplicationFactory<Program>
{
    public const string UserName = "test-user";
    public const string UserPassword = "user-pass-for-tests";
    public const string AdminName = "test-admin";
    public const string AdminPassword = "admin-pass-for-tests";

    public string DatabasePath { get; } =
        Path.Combine(Path.GetTempPath(), $"notifier-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // UseSetting: Program reads the DB path before Build().
        builder.UseSetting("Notifier:DatabasePath", DatabasePath);
        builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // Web tests must never reach the real BBC feeds.
            ["Notifier:Polling:Enabled"] = "false",
            ["Notifier:Users:Users:0:Username"] = UserName,
            ["Notifier:Users:Users:0:Role"] = Roles.User,
            ["Notifier:Users:Users:0:PasswordHash"] = ConfigUserStore.Hash(UserPassword),
            ["Notifier:Users:Users:1:Username"] = AdminName,
            ["Notifier:Users:Users:1:Role"] = Roles.Admin,
            ["Notifier:Users:Users:1:PasswordHash"] = ConfigUserStore.Hash(AdminPassword),
        }));
    }

    public HttpClient CreateNoRedirectClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(DatabasePath)) File.Delete(DatabasePath);
    }
}
