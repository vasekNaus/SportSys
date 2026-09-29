using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.sport.Pages.Location;

public class EditModel : PageModel
{
    private readonly SportLocationService _service;

    public EditModel(SportLocationService service)
    {
        _service = service;
    }

    [BindProperty]
    public LocationDto Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsNew => Input.Id == 0;

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken ct)
    {
        if (id is null)
            return Page();

        var dto = await _service.GetByIdAsync(id.Value, ct);
        if (dto is null)
            return NotFound();

        Input = dto;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Page();

        if (Input.Id == 0)
        {
            await _service.CreateAsync(Input, ct);
            StatusMessage = "Lokalita byla vytvořena.";
        }
        else
        {
            await _service.UpdateAsync(Input, ct);
            StatusMessage = "Lokalita byla uložena.";
        }

        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostSetActiveAsync(bool isActive, CancellationToken ct)
    {
        await _service.SetActiveAsync(Input.Id, isActive, ct);
        StatusMessage = isActive
            ? "Lokalita byla aktivována."
            : "Lokalita byla zneaktivněna.";
        return RedirectToPage("Index");
    }
}
