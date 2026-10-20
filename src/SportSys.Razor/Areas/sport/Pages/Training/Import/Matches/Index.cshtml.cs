using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Razor.Services;
using System.Text.Json;

namespace SportSys.Razor.Areas.sport.Pages.Training.Import.Matches;

public class IndexModel : PageModel
{
    private readonly TrainingScheduleService _scheduleService;
    private readonly KisMatchImportService _importService;

    public IndexModel(
        TrainingScheduleService scheduleService,
        KisMatchImportService importService)
    {
        _scheduleService = scheduleService;
        _importService = importService;
    }

    public List<SeasonDto> Seasons { get; private set; } = [];

    [BindProperty]
    public KisMatchImportDto MatchImport { get; set; } = new();

    [BindProperty]
    public IFormFile? MatchImportFile { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? MatchImportWarningsJson { get; set; }

    public IReadOnlyList<KisMatchImportWarning> MatchImportWarnings { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadOptionsAsync(ct);
        if (MatchImportWarningsJson is not null)
        {
            MatchImportWarnings = JsonSerializer.Deserialize<List<KisMatchImportWarning>>(MatchImportWarningsJson)
                ?? [];
        }
    }

    public async Task<IActionResult> OnPostImportMatchesAsync(CancellationToken ct)
    {
        var fileError = KisImportFileValidator.GetError(MatchImportFile);
        if (fileError is not null)
            ModelState.AddModelError(nameof(MatchImportFile), fileError);

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(ct);
            return Page();
        }

        try
        {
            await using var stream = MatchImportFile!.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            buffer.Position = 0;

            var result = await _importService.ImportAsync(buffer, MatchImport.SeasonId, ct);
            StatusMessage =
                $"Import zápasů dokončen: vloženo {result.Inserted}, " +
                $"již existuje {result.Existing}, přeskočeno {result.Skipped}.";
            MatchImportWarningsJson = result.Warnings.Count == 0
                ? null
                : JsonSerializer.Serialize(result.Warnings.Take(20));

            return RedirectToPage();
        }
        catch (InvalidDataException ex)
        {
            ModelState.AddModelError(nameof(MatchImportFile), ex.Message);
        }
        catch (System.IO.FileFormatException)
        {
            ModelState.AddModelError(nameof(MatchImportFile), "Soubor není platný XLSX export KIS.");
        }
        catch (System.Xml.XmlException)
        {
            ModelState.AddModelError(nameof(MatchImportFile), "Soubor není platný XLSX export KIS.");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError($"{nameof(MatchImport)}.{nameof(MatchImport.SeasonId)}", ex.Message);
        }

        await LoadOptionsAsync(ct);
        return Page();
    }

    private async Task LoadOptionsAsync(CancellationToken ct)
    {
        Seasons = await _scheduleService.GetSeasonsAsync(ct);
    }
}
