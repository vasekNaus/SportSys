using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.sport.Pages.Training.Plan;

public class EditModel : PageModel
{
    private readonly TrainingPlanService _service;

    public EditModel(TrainingPlanService service)
    {
        _service = service;
    }

    [BindProperty]
    public TrainingPlanEditDto Input { get; set; } = new();

    public TrainingPlanEditContextDto Context { get; private set; } = null!;

    public IReadOnlyList<SelectListItem> DayNameItems { get; } =
    [
        new("Pondělí", nameof(DayOfWeek.Monday)),
        new("Úterý", nameof(DayOfWeek.Tuesday)),
        new("Středa", nameof(DayOfWeek.Wednesday)),
        new("Čtvrtek", nameof(DayOfWeek.Thursday)),
        new("Pátek", nameof(DayOfWeek.Friday)),
        new("Sobota", nameof(DayOfWeek.Saturday)),
        new("Neděle", nameof(DayOfWeek.Sunday)),
    ];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        var context = await _service.GetEditAsync(id, ct);
        if (context is null)
            return NotFound();

        Context = context;
        Input = context.Input;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return await ReloadPageAsync(Input.Id, ct);

        var result = await _service.UpdateAsync(Input, ct);
        switch (result)
        {
            case TrainingPlanUpdateResult.Success:
                StatusMessage = "Tréninkový plán byl uložen.";
                return RedirectToPage(new { id = Input.Id });

            case TrainingPlanUpdateResult.NotFound:
                return NotFound();

            case TrainingPlanUpdateResult.GroupInconsistent:
                ModelState.AddModelError(
                    string.Empty,
                    "Spojené tréninkové plány nemají stejné hodnoty a nelze je společně editovat.");
                return await ReloadPageAsync(Input.Id, ct);

            case TrainingPlanUpdateResult.Conflict:
                ModelState.Clear();
                ModelState.AddModelError(
                    string.Empty,
                    "Tréninkový plán nebo jeho skupina se mezitím změnily. Zkontrolujte aktuální údaje a odešlete formulář znovu.");
                return await ReloadPageAsync(Input.Id, ct, useCurrentInput: true);

            case TrainingPlanUpdateResult.InvalidInput:
                ModelState.AddModelError(
                    string.Empty,
                    "Zadané údaje tréninkového plánu nejsou platné.");
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
        return Page();
    }
}
