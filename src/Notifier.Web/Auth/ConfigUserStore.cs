using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Notifier.Web.Auth;

public sealed class UsersOptions
{
    public const string Section = "Notifier:Users";
    public List<ConfiguredUser> Users { get; set; } = [];
}

public sealed class ConfiguredUser
{
    public string Username { get; set; } = "";
    public string Role { get; set; } = Roles.User;
    /// <summary>Generate with <c>dotnet run -- hash-password</c>.</summary>
    public string PasswordHash { get; set; } = "";
}

public static class Roles
{
    public const string User = "User";
    public const string Admin = "Admin";
}

public sealed class ConfigUserStore(IOptions<UsersOptions> options)
{
    private static readonly PasswordHasher<ConfiguredUser> Hasher = new();
    // Keeps the unknown-user path as slow as the wrong-password path.
    private static readonly string DummyHash = Hasher.HashPassword(new ConfiguredUser(), Guid.NewGuid().ToString());

    public ConfiguredUser? Validate(string username, string password)
    {
        var user = options.Value.Users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        var hash = user?.PasswordHash is { Length: > 0 } h ? h : DummyHash;
        var result = Hasher.VerifyHashedPassword(user ?? new ConfiguredUser(), hash, password);
        return user is not null && result != PasswordVerificationResult.Failed ? user : null;
    }

    public static string Hash(string password) => Hasher.HashPassword(new ConfiguredUser(), password);
}
