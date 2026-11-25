using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Notifier.Sources.Bbc;
using Notifier.Web.Auth;
using Notifier.Web.Channels;
using Notifier.Web.Data;
using Notifier.Web.Polling;
using Notifier.Web.Sending;

// `hash-password`: reads a password from stdin and prints its hash for appsettings.
if (args.Length > 0 && args[0] == "hash-password")
{
    Console.Write("Password: ");
    var pw = Console.ReadLine() ?? "";
    Console.WriteLine(ConfigUserStore.Hash(pw));
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<UsersOptions>(builder.Configuration.GetSection(UsersOptions.Section));
builder.Services.AddSingleton<ConfigUserStore>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddBbcNewsSource(builder.Configuration);
builder.Services.AddNotifierChannels(builder.Configuration);
builder.Services.AddScoped<TestSendService>();
builder.Services.AddNotifierPolling(builder.Configuration);

var dbPath = Path.Combine(builder.Environment.ContentRootPath,
    builder.Configuration["Notifier:DatabasePath"] ?? "notifier.db");
builder.Services.AddDbContext<NotifierDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Login";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
        o.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole(Roles.Admin));

builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/");
    o.Conventions.AllowAnonymousToPage("/Login");
    o.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NotifierDbContext>().Database.EnsureCreated();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();

public partial class Program;
