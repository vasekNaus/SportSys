using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.sport.Pages.Training.Plan;

public class EditModel : PageModel
{
  private readonly TrainingPlanService _service;
  private readonly SportLocationService _locationService;

  public EditModel(TrainingPlanService service, SportLocationService locationService)
  {
    _service = service;
    _locationService = locationService;
  }

  [BindProperty]
  public TrainingPlanEditDto Input { get; set; } = new();

  public TrainingPlanEditContextDto Context { get; private set; } = null!;

  public List<SelectListItem> LocationIdItems { get; private set; } = [];

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

      case TrainingPlanUpdateResult.DuplicateCoachAssignment:
        ModelState.AddModelError(
            string.Empty,
            "Stejný trenér je v rámci jednoho tréninkového plánu přiřazen vícekrát.");
        return await ReloadPageAsync(Input.Id, ct);

      case TrainingPlanUpdateResult.LocationUnavailable:
        ModelState.AddModelError(
            nameof(Input.LocationId),
            "Vybraná lokalita neexistuje nebo není aktivní.");
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

  public async Task<IActionResult> OnPostAddCoachAsync(
      int? memberIndex,
      CancellationToken ct)
  {
    Input.CoachAssignments ??= [];
    Input.MemberCoachAssignments ??= [];

    if (memberIndex.HasValue)
    {
      if (Input.MemberCoachAssignments is null ||
          memberIndex.Value < 0 ||
          memberIndex.Value >= Input.MemberCoachAssignments.Count)
      {
        return BadRequest();
      }

      Input.MemberCoachAssignments[memberIndex.Value].CoachAssignments.Add(new());
    }
    else
    {
      Input.CoachAssignments.Add(new());
    }

    ModelState.Clear();
    return await ReloadPageAsync(Input.Id, ct);
  }

  public async Task<IActionResult> OnPostRemoveCoachAsync(
      int assignmentIndex,
      int? memberIndex,
      CancellationToken ct)
  {
    if (assignmentIndex < 0)
      return BadRequest();

    Input.CoachAssignments ??= [];
    Input.MemberCoachAssignments ??= [];

    if (memberIndex.HasValue)
    {
      if (Input.MemberCoachAssignments is null ||
          memberIndex.Value < 0 ||
          memberIndex.Value >= Input.MemberCoachAssignments.Count)
      {
        return BadRequest();
      }

      var assignments = Input.MemberCoachAssignments[memberIndex.Value].CoachAssignments;
      if (assignmentIndex < assignments.Count)
        assignments.RemoveAt(assignmentIndex);
    }
    else if (assignmentIndex < Input.CoachAssignments.Count)
    {
      Input.CoachAssignments.RemoveAt(assignmentIndex);
    }

    ModelState.Clear();
    return await ReloadPageAsync(Input.Id, ct);
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
    EnsureCoachAssignmentsMatchMembers();
    await LoadLocationsAsync(ct);
    return Page();
  }

  private void EnsureCoachAssignmentsMatchMembers()
  {
    Input.CoachAssignments ??= [];
    Input.MemberCoachAssignments ??= [];
    if (!Context.IsGrouped)
      return;

    Input.MemberCoachAssignments = Context.Members
        .Select(member =>
        {
          var assignment = Input.MemberCoachAssignments
              .FirstOrDefault(item => item.TrainingPlanId == member.Id);
          return assignment is null || assignment.CoachAssignments is null
              ? new TrainingPlanMemberCoachInputDto { TrainingPlanId = member.Id }
              : assignment;
        })
        .ToList();
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
