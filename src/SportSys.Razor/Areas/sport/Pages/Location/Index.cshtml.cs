using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.sport.Pages.Location;

public class IndexModel : PageModel
{
    private readonly SportLocationService _service;

    public IndexModel(SportLocationService service)
    {
        _service = service;
    }

    public List<LocationDto> Locations { get; set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsActive { get; set; } = true;

    public async Task OnGetAsync(CancellationToken ct)
    {
        Locations = await _service.GetAllAsync(Search, IsActive, ct);
    }

    public async Task<IActionResult> OnPostSetActiveAsync(
        int id,
        bool isActive,
        CancellationToken ct)
    {
        await _service.SetActiveAsync(id, isActive, ct);
        StatusMessage = isActive
            ? "Lokalita byla aktivována."
            : "Lokalita byla zneaktivněna.";
        return RedirectToPage(new { Search, IsActive });
    }
}
