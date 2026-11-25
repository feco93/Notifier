using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Notifier.Core.Domain;
using Notifier.Web.Channels;
using Notifier.Web.Data;

namespace Notifier.Web.Alerts;

/// <param name="Destinations">Channel key → field values, for the destinations the user chose.</param>
public sealed record AlertDraft(
    string? Name,
    Tier Tier,
    IReadOnlyList<Topic> Topics,
    string? Keyword,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Destinations);

/// <param name="Errors">Field key → message; empty on success.</param>
public sealed record CreateResult(int? Id, IReadOnlyDictionary<string, string> Errors);

/// <summary>All alert reads and writes are scoped to the owner; other users' alerts behave as if they don't exist.</summary>
public sealed class AlertService(NotifierDbContext db, ChannelRegistry channels, TimeProvider time)
{
    public const int MaxNameLength = 100;
    public const int MaxKeywordLength = 100;

    public async Task<CreateResult> CreateAsync(string owner, AlertDraft draft, CancellationToken ct)
    {
        var errors = new Dictionary<string, string>();

        var name = draft.Name?.Trim() ?? "";
        if (name.Length == 0) errors["Name"] = "Enter a name.";
        else if (name.Length > MaxNameLength) errors["Name"] = $"Use at most {MaxNameLength} characters.";
        else if (name.Any(char.IsControl)) errors["Name"] = "The name can't contain line breaks.";

        if (!Enum.IsDefined(draft.Tier)) errors["Tier"] = "Choose an importance level.";

        var topics = draft.Tier == Tier.All ? draft.Topics.Where(Enum.IsDefined).Distinct().ToList() : [];
        if (draft.Tier == Tier.All && topics.Count == 0) errors["Topics"] = "Choose at least one topic.";

        var keyword = string.IsNullOrWhiteSpace(draft.Keyword) ? null : draft.Keyword.Trim();
        if (keyword is { Length: > MaxKeywordLength }) errors["Keyword"] = $"Use at most {MaxKeywordLength} characters.";
        else if (keyword is not null && (!keyword.Any(char.IsLetterOrDigit) || keyword.Any(char.IsControl)))
            errors["Keyword"] = "The keyword must contain a letter or digit and no line breaks.";

        var destinations = new List<Destination>();
        if (draft.Destinations.Count == 0) errors["Destinations"] = "Choose at least one destination.";
        foreach (var (key, fields) in draft.Destinations)
        {
            var channel = channels.Get(key);
            if (channel is null)
            {
                errors[$"Destinations[{key}]"] = "Unknown destination type.";
                continue;
            }
            // Only declared fields are stored, so a crafted form can't smuggle extra settings in.
            var config = channel.ConfigFields.ToDictionary(f => f.Name, f => fields.GetValueOrDefault(f.Name)?.Trim() ?? "");
            var problems = channel.Validate(config);
            if (problems.Count > 0)
            {
                errors[$"Destinations[{key}]"] = string.Join(" ", problems);
                continue;
            }
            destinations.Add(new Destination { ChannelKey = key, ConfigJson = JsonSerializer.Serialize(config) });
        }

        if (errors.Count > 0) return new CreateResult(null, errors);

        var now = time.GetUtcNow().UtcDateTime;
        var alert = new Alert
        {
            OwnerUsername = owner, Name = name, Tier = draft.Tier, Keyword = keyword, Enabled = true,
            CreatedAtUtc = now, ActiveSinceUtc = now,
            Topics = [.. topics.Select(t => new AlertTopic { Topic = t })],
            Destinations = destinations,
        };
        db.Alerts.Add(alert);
        await db.SaveChangesAsync(ct);
        return new CreateResult(alert.Id, errors);
    }

    public Task<List<Alert>> ListAsync(string owner, CancellationToken ct) =>
        Owned(owner).OrderBy(a => a.Name).ThenBy(a => a.Id).ToListAsync(ct);

    public Task<Alert?> GetAsync(string owner, int id, CancellationToken ct) =>
        Owned(owner).FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<bool> SetEnabledAsync(string owner, int id, bool enabled, CancellationToken ct)
    {
        var alert = await GetAsync(owner, id, ct);
        if (alert is null) return false;
        // Articles first seen while the alert was off are not sent after re-enabling.
        if (enabled && !alert.Enabled) alert.ActiveSinceUtc = time.GetUtcNow().UtcDateTime;
        alert.Enabled = enabled;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(string owner, int id, CancellationToken ct)
    {
        var alert = await GetAsync(owner, id, ct);
        if (alert is null) return false;
        alert.DeletedAtUtc = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // Single query: at most 5 topics × one destination per channel per alert, so the join stays tiny.
    private IQueryable<Alert> Owned(string owner) =>
        db.Alerts.Include(a => a.Topics).Include(a => a.Destinations).AsSingleQuery()
            .Where(a => a.OwnerUsername == owner && a.DeletedAtUtc == null);
}
