using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.sport.Pages.Training.Schedule;

public class EditModel : PageModel
{
    private readonly TrainingService _service;
    private readonly SportLocationService _locationService;

    public EditModel(TrainingService service, SportLocationService locationService)
    {
        _service = service;
        _locationService = locationService;
    }

    [BindProperty]
    public TrainingEditDto Input { get; set; } = new();

    public TrainingEditContextDto Context { get; private set; } = null!;
    public List<SelectListItem> LocationIdItems { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        var context = await _service.GetEditAsync(id, ct);
        if (context is null)
            return NotFound();

        Context = context;
        Input = context.Input;
        await LoadLocationsAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return await ReloadPageAsync(Input.Id, ct);

        var result = await _service.UpdateAsync(Input, ct);
        switch (result)
        {
            case TrainingUpdateResult.Success:
                StatusMessage = "Trénink byl uložen.";
                return RedirectToPage(new { id = Input.Id });

            case TrainingUpdateResult.NotFound:
                return NotFound();

            case TrainingUpdateResult.GroupInconsistent:
                ModelState.AddModelError(
                    string.Empty,
                    "Spojené tréninky nemají stejné hodnoty a nelze je společně editovat.");
                return await ReloadPageAsync(Input.Id, ct);

            case TrainingUpdateResult.LocationUnavailable:
                ModelState.AddModelError(
                    nameof(Input.LocationId),
                    "Vybraná lokalita neexistuje nebo není aktivní.");
                return await ReloadPageAsync(Input.Id, ct);

            case TrainingUpdateResult.Conflict:
                ModelState.AddModelError(
                    string.Empty,
                    "Trénink nebo jeho skupina se mezitím změnily. Zkontrolujte aktuální údaje a odešlete formulář znovu.");
                return await ReloadPageAsync(Input.Id, ct, useCurrentInput: true);

            case TrainingUpdateResult.InvalidInput:
                ModelState.AddModelError(
                    string.Empty,
                    "Zadané údaje tréninku nejsou platné.");
                return await ReloadPageAsync(Input.Id, ct);

            default:
                throw new ArgumentOutOfRangeException(nameof(result), result, null);
        }
    }

    private async Task<IActionResult> ReloadPageAsync(
        int id,
        CancellationToken ct,
        bool useCurrentInput = false)
    {
        var context = await _service.GetEditAsync(id, ct);
        if (context is null)
            return NotFound();

        Context = context;
        if (useCurrentInput)
            Input = context.Input;
        await LoadLocationsAsync(ct);
        return Page();
    }

    private async Task LoadLocationsAsync(CancellationToken ct)
    {
        var locations = await _locationService.GetSelectListAsync(Input.LocationId, ct);
        LocationIdItems =
        [
            new SelectListItem("— vyberte lokalitu —", string.Empty),
            .. locations.Select(location => new SelectListItem(location.Name, location.Id.ToString())),
        ];
    }
}
