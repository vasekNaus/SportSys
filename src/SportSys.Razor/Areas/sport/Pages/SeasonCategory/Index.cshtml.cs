using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.sport.Pages.SeasonCategory;

public class IndexModel : PageModel
{
    private readonly SeasonCategoryService _service;

    public IndexModel(SeasonCategoryService service)
    {
        _service = service;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsActive { get; set; } = true;

    public List<SeasonCategoryListItem> Categories { get; set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Categories = await _service.GetAllAsync(Search, IsActive, ct);
    }

    public async Task<IActionResult> OnPostSetActiveAsync(
        int seasonId,
        string code,
        bool isActive,
        CancellationToken ct)
    {
        await _service.SetActiveAsync(seasonId, code, isActive, ct);
        StatusMessage = isActive
            ? "Kategorie sezóny byla aktivována."
            : "Kategorie sezóny byla zneaktivněna.";
        return RedirectToPage(new { Search, IsActive });
    }
}
