using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Notifier.Core.Domain;
using Notifier.Web.Alerts;
using Notifier.Web.Channels;
using Notifier.Web.Polling;
using Notifier.Web.Sending;

namespace Notifier.Web.Pages;

public sealed class IndexModel(AlertService alerts, ChannelRegistry channels, TestSendService testSend, IOptions<PollingOptions> polling)
    : PageModel
{
    public IReadOnlyList<Alert> Alerts { get; private set; } = [];
    [TempData] public string? StatusMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }
    public int HeadlineN => polling.Value.HeadlineN;

    private string Owner => User.Identity!.Name!;

    public string Describe(Destination destination) => AlertText.Destination(channels, destination);

    public async Task OnGetAsync(CancellationToken ct) => Alerts = await alerts.ListAsync(Owner, ct);

    public async Task<IActionResult> OnPostToggleAsync(int id, CancellationToken ct)
    {
        var alert = await alerts.GetAsync(Owner, id, ct);
        if (alert is null) return NotFound();
        var enable = !alert.Enabled;
        await alerts.SetEnabledAsync(Owner, id, enable, ct);
        StatusMessage = $"Alert '{alert.Name}' {(enable ? "enabled" : "disabled")}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTestAsync(int alertId, int destinationId, CancellationToken ct)
    {
        var alert = await alerts.GetAsync(Owner, alertId, ct);
        var destination = alert?.Destinations.FirstOrDefault(d => d.Id == destinationId);
        if (alert is null || destination is null) return NotFound();

        var result = await testSend.SendAsync(alert, destination, ct);
        if (result.Success) StatusMessage = $"Test message sent to {Describe(destination)}.";
        else ErrorMessage = $"Test to {Describe(destination)} failed: {result.Error}";
        return RedirectToPage();
    }
}
