using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.sport.Pages.Team;

public class EditModel : PageModel
{
    private readonly TeamService _service;
    private readonly SportLocationService _locationService;

    public EditModel(TeamService service, SportLocationService locationService)
    {
        _service = service;
        _locationService = locationService;
    }

    [BindProperty]
    public TeamDto Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public List<SelectListItem> HomeLocationIdItems { get; private set; } = [];

    public bool IsNew => Input.Id == 0;

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken ct)
    {
        if (id is not null)
        {
            var dto = await _service.GetByIdAsync(id.Value, ct);
            if (dto is null)
                return NotFound();

            Input = dto;
        }

        await LoadLocationsAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadLocationsAsync(ct);
            return Page();
        }

        try
        {
            if (Input.Id == 0)
            {
                await _service.CreateAsync(Input, ct);
                StatusMessage = "Tým byl vytvořen.";
            }
            else
            {
                await _service.UpdateAsync(Input, ct);
                StatusMessage = "Tým byl uložen.";
            }
        }
        catch (HomeLocationUnavailableException exception)
        {
            ModelState.AddModelError(nameof(Input.HomeLocationId), exception.Message);
            await LoadLocationsAsync(ct);
            return Page();
        }

        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostSetActiveAsync(bool isActive, CancellationToken ct)
    {
        await _service.SetActiveAsync(Input.Id, isActive, ct);
        StatusMessage = isActive ? "Tým byl aktivován." : "Tým byl zneaktivněn.";
        return RedirectToPage("Index");
    }

    private async Task LoadLocationsAsync(CancellationToken ct)
    {
        var locations = await _locationService.GetSelectListAsync(Input.HomeLocationId, ct);
        HomeLocationIdItems =
        [
            new SelectListItem("— nevybráno —", string.Empty),
            .. locations.Select(location => new SelectListItem(location.Name, location.Id.ToString())),
        ];
    }
}
