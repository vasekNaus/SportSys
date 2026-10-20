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
    public List<SelectListItem> CoachItems { get; private set; } = [];
    public List<SelectListItem> TrainingItems { get; private set; } = [];
    public List<SelectListItem> ParticipationTypeItems { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        var context = await _service.GetEditAsync(id, ct);
        if (context is null)
            return NotFound();

        Context = context;
        Input = context.Input;
        LoadCoachAssignmentItems();
        await LoadLocationsAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Input.CoachAssignments ??= [];
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

            case TrainingUpdateResult.DuplicateCoachAssignment:
                await ReloadPageAsync(Input.Id, ct);
                var duplicateCoachId = Input.CoachAssignments
                    .Select(assignment => assignment.ExistingCoachId ?? assignment.CoachId)
                    .OfType<int>()
                    .GroupBy(coachId => coachId)
                    .FirstOrDefault(group => group.Count() > 1)
                    ?.Key;
                var duplicateCoachName = duplicateCoachId.HasValue
                    ? Context.AvailableCoaches
                        .FirstOrDefault(coach => coach.Id == duplicateCoachId.Value)
                        ?.DisplayName ?? duplicateCoachId.Value.ToString()
                    : "Vybraný trenér";
                ModelState.AddModelError(
                    string.Empty,
                    $"Trenér {duplicateCoachName} je již k tomuto tréninku přiřazen.");
                return Page();

            case TrainingUpdateResult.CoachAssignmentRemovalNotAllowed:
                await ReloadPageAsync(Input.Id, ct);
                ModelState.AddModelError(
                    string.Empty,
                    "Existující přiřazení trenéra nelze odebrat.");
                return Page();

            case TrainingUpdateResult.Conflict:
                ModelState.Clear();
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
        else
            RestoreMissingExistingCoachAssignments();
        LoadCoachAssignmentItems();
        await LoadLocationsAsync(ct);
        return Page();
    }

    private void RestoreMissingExistingCoachAssignments()
    {
        Input.CoachAssignments ??= [];
        var submittedKeys = Input.CoachAssignments
            .Where(assignment =>
                assignment.ExistingCoachId.HasValue &&
                assignment.ExistingTrainingId.HasValue &&
                assignment.ExistingParticipationTypeId.HasValue)
            .Select(assignment => (
                assignment.ExistingCoachId,
                assignment.ExistingTrainingId,
                assignment.ExistingParticipationTypeId))
            .ToHashSet();

        foreach (var assignment in Context.CoachAssignments)
        {
            var key = (
                (int?)assignment.CoachId,
                (int?)assignment.TrainingId,
                (int?)assignment.ParticipationTypeId);
            if (submittedKeys.Contains(key))
                continue;

            Input.CoachAssignments.Add(new TrainingCoachAssignmentInputDto
            {
                ExistingCoachId = assignment.CoachId,
                ExistingTrainingId = assignment.TrainingId,
                ExistingParticipationTypeId = assignment.ParticipationTypeId,
                ParticipationTypeId = assignment.ParticipationTypeId,
                Note = assignment.Note,
            });
        }
    }

    private void LoadCoachAssignmentItems()
    {
        CoachItems =
        [
            new SelectListItem("— vyberte trenéra —", string.Empty),
            .. Context.AvailableCoaches.Select(
                coach => new SelectListItem(coach.DisplayName, coach.Id.ToString())),
        ];

        TrainingItems =
        [
            new SelectListItem("— vyberte kategorii —", string.Empty),
            .. Context.AvailableCategories.Select(
                category => new SelectListItem(
                    category.CategoryCode,
                    category.TrainingId.ToString())),
        ];

        ParticipationTypeItems =
        [
            new SelectListItem("— vyberte stav docházky —", string.Empty),
            .. Context.ParticipationTypes.Select(
                participationType => new SelectListItem(
                    participationType.DisplayName,
                    participationType.Id.ToString(),
                    participationType.IsDefault)),
        ];
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
