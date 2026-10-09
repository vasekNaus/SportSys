using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.sport.Pages.Training.Requirement;

public class EditTrainingModel : PageModel
{
    private readonly TrainingRequirementService _service;

    public EditTrainingModel(TrainingRequirementService service)
    {
        _service = service;
    }

    [BindProperty]
    public TrainingRequirementEditDto Input { get; set; } = new();

    public RequirementEditContextDto<TrainingRequirementEditDto> Context { get; private set; } = null!;

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

    public async Task<IActionResult> OnPostAddCoachAsync(CancellationToken ct)
    {
        Input.CoachAssignments.Add(new RequirementCoachAssignmentInput());
        ModelState.Clear();
        return await ReloadAsync(ct);
    }

    public async Task<IActionResult> OnPostRemoveCoachAsync(int index, CancellationToken ct)
    {
        if (index >= 0 && index < Input.CoachAssignments.Count)
            Input.CoachAssignments.RemoveAt(index);
        ModelState.Clear();
        return await ReloadAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return await ReloadAsync(ct);

        var result = await _service.UpdateAsync(Input, ct);
        switch (result.Status)
        {
            case RequirementEditStatus.Success:
                StatusMessage = "Požadavek byl uložen.";
                return RedirectToPage(new { id = Input.Id });
            case RequirementEditStatus.NotFound:
                return NotFound();
            case RequirementEditStatus.DuplicateCoach:
                ModelState.AddModelError(string.Empty, result.Message ?? "Trenér je již přiřazen.");
                return await ReloadAsync(ct);
            default:
                ModelState.AddModelError(string.Empty, "Zadané údaje požadavku nejsou platné.");
                return await ReloadAsync(ct);
        }
    }

    private async Task<IActionResult> ReloadAsync(CancellationToken ct)
    {
        var context = await _service.GetEditAsync(Input.Id, ct);
        if (context is null)
            return NotFound();

        Context = context;
        return Page();
    }
}
