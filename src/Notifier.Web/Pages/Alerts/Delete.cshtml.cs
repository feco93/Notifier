using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Notifier.Core.Domain;
using Notifier.Web.Alerts;

namespace Notifier.Web.Pages.Alerts;

public sealed class DeleteModel(AlertService alerts) : PageModel
{
    public Alert? Alert { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        Alert = await alerts.GetAsync(User.Identity!.Name!, id, ct);
        return Alert is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken ct)
    {
        var alert = await alerts.GetAsync(User.Identity!.Name!, id, ct);
        if (alert is null || !await alerts.DeleteAsync(User.Identity!.Name!, id, ct)) return NotFound();
        TempData["StatusMessage"] = $"Alert '{alert.Name}' deleted.";
        return RedirectToPage("/Index");
    }
}
