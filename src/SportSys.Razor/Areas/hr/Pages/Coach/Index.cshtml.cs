using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportSys.Contract.Models.hr;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.hr.Pages.Coach;

public class IndexModel : PageModel
{
    private readonly CoachService _service;

    public IndexModel(CoachService service)
    {
        _service = service;
    }

    [BindProperty(SupportsGet = true)]
    public CoachFilter Filter { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public List<CoachListItem> Coaches { get; set; } = [];
    public List<SelectListItem> SeasonSelectList { get; set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Coaches = await _service.GetAllAsync(Filter, ct);
        var seasons = await _service.GetSeasonsAsync(ct);
        SeasonSelectList =
        [
            new("Aktuální sezóna", ""),
            .. seasons.Select(s => new SelectListItem(s.Name, s.Id.ToString())),
        ];
    }
}
