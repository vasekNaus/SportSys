using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.sport.Pages.Training;

public class EditModel : PageModel
{
    private readonly TrainingService _service;

    public EditModel(TrainingService service)
    {
        _service = service;
    }

    [BindProperty]
    public TrainingEditDto Input { get; set; } = new();

    public TrainingEditContextDto Context { get; private set; } = null!;

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
        return Page();
    }
}
