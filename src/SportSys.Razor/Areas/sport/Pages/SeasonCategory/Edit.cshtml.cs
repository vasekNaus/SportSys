using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.sport.Pages.SeasonCategory;

public class EditModel : PageModel
{
    private readonly SeasonCategoryService _service;
    private readonly SeasonService _seasonService;

    public EditModel(SeasonCategoryService service, SeasonService seasonService)
    {
        _service = service;
        _seasonService = seasonService;
    }

    [BindProperty]
    public SeasonCategoryEditDto Input { get; set; } = new();

    [BindProperty]
    public bool Creating { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public List<SelectListItem> SeasonItems { get; private set; } = [];

    public bool IsNew => Creating;

    public async Task<IActionResult> OnGetAsync(
        int? seasonId,
        string? code,
        CancellationToken ct)
    {
        if (seasonId is null && code is null)
        {
            Creating = true;
            await LoadSeasonsAsync(ct);
            return Page();
        }

        if (seasonId is null || string.IsNullOrWhiteSpace(code))
            return BadRequest();

        var dto = await _service.GetByIdAsync(seasonId.Value, code, ct);
        if (dto is null)
            return NotFound();

        Input = dto;
        await LoadSeasonsAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadSeasonsAsync(ct);
            return Page();
        }

        if (Creating)
        {
            await _service.CreateAsync(Input, ct);
            StatusMessage = "Kategorie sezóny byla vytvořena.";
        }
        else
        {
            await _service.UpdateAsync(Input, ct);
            StatusMessage = "Kategorie sezóny byla uložena.";
        }

        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostSetActiveAsync(bool isActive, CancellationToken ct)
    {
        await _service.SetActiveAsync(Input.SeasonId, Input.Code!, isActive, ct);
        StatusMessage = isActive
            ? "Kategorie sezóny byla aktivována."
            : "Kategorie sezóny byla zneaktivněna.";
        return RedirectToPage("Index");
    }

    private async Task LoadSeasonsAsync(CancellationToken ct)
    {
        var seasons = await _seasonService.GetSelectListAsync(
            Creating ? null : Input.SeasonId,
            ct);

        SeasonItems =
        [
            new SelectListItem("— vyberte sezónu —", string.Empty),
            .. seasons.Select(s => new SelectListItem(s.Name, s.Id.ToString())),
        ];
    }
}
