using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Notifier.Core.Channels;
using Notifier.Core.Domain;
using Notifier.Web.Alerts;
using Notifier.Web.Channels;
using Notifier.Web.Polling;

namespace Notifier.Web.Pages.Alerts;

public sealed class AlertInput
{
    public string? Name { get; set; }
    public Tier Tier { get; set; } = Tier.Headline;
    public List<Topic> Topics { get; set; } = [];
    public string? Keyword { get; set; }
    public Dictionary<string, DestinationInput> Destinations { get; set; } = [];
}

public sealed class DestinationInput
{
    public bool Use { get; set; }
    public Dictionary<string, string?> Fields { get; set; } = [];
}

public sealed class CreateModel(AlertService alerts, ChannelRegistry channels, IOptions<PollingOptions> polling) : PageModel
{
    [BindProperty] public AlertInput Input { get; set; } = new();
    public IReadOnlyList<INotificationChannel> Channels => channels.All;
    public int HeadlineN => polling.Value.HeadlineN;
    private readonly Dictionary<string, string> _errors = [];

    public string? ErrorFor(string key) => _errors.GetValueOrDefault(key);

    public string? FieldValue(INotificationChannel channel, ChannelField field) =>
        field.IsSecret ? null : Input.Destinations.GetValueOrDefault(channel.Key)?.Fields.GetValueOrDefault(field.Name);

    public bool Uses(INotificationChannel channel) => Input.Destinations.GetValueOrDefault(channel.Key)?.Use == true;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var destinations = Input.Destinations
            .Where(d => d.Value.Use)
            .ToDictionary(
                d => d.Key,
                d => (IReadOnlyDictionary<string, string>)d.Value.Fields.ToDictionary(f => f.Key, f => f.Value ?? ""));
        var draft = new AlertDraft(Input.Name, Input.Tier, Input.Topics, Input.Keyword, destinations);

        var result = await alerts.CreateAsync(User.Identity!.Name!, draft, ct);
        if (result.Id is null)
        {
            foreach (var (key, message) in result.Errors) _errors[key] = message;
            return Page();
        }

        TempData["StatusMessage"] = $"Alert '{Input.Name!.Trim()}' created.";
        return RedirectToPage("/Index");
    }
}
