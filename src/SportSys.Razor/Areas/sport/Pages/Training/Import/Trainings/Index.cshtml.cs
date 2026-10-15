using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Razor.Services;
using System.Text.Json;

namespace SportSys.Razor.Areas.sport.Pages.Training.Import.Trainings;

public class IndexModel : PageModel
{
    private readonly TrainingScheduleService _scheduleService;
    private readonly KisTrainingImportService _importService;

    public IndexModel(
        TrainingScheduleService scheduleService,
        KisTrainingImportService importService)
    {
        _scheduleService = scheduleService;
        _importService = importService;
    }

    public List<SeasonDto> Seasons { get; private set; } = [];
    public List<LookupSelectItem> TrainingPhases { get; private set; } = [];

    [BindProperty]
    public KisTrainingImportDto Import { get; set; } = new();

    [BindProperty]
    public IFormFile? ImportFile { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ImportWarningsJson { get; set; }

    public IReadOnlyList<KisTrainingImportWarning> ImportWarnings { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadOptionsAsync(ct);
        if (ImportWarningsJson is not null)
        {
            ImportWarnings = JsonSerializer.Deserialize<List<KisTrainingImportWarning>>(ImportWarningsJson)
                ?? [];
        }
    }

    public async Task<IActionResult> OnPostImportAsync(CancellationToken ct)
    {
        var fileError = KisImportFileValidator.GetError(ImportFile);
        if (fileError is not null)
            ModelState.AddModelError(nameof(ImportFile), fileError);

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(ct);
            return Page();
        }

        try
        {
            await using var stream = ImportFile!.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            buffer.Position = 0;

            var result = await _importService.ImportAsync(buffer, Import, ct);
            StatusMessage =
                $"Import dokončen: potvrzeno {result.Confirmed}, změněn čas {result.TimeChanged}, " +
                $"vloženo {result.Inserted}, přeskočeno {result.Skipped}.";
            ImportWarningsJson = result.Warnings.Count == 0
                ? null
                : JsonSerializer.Serialize(result.Warnings.Take(20));

            return RedirectToPage();
        }
        catch (InvalidDataException ex)
        {
            ModelState.AddModelError(nameof(ImportFile), ex.Message);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        await LoadOptionsAsync(ct);
        return Page();
    }

    private async Task LoadOptionsAsync(CancellationToken ct)
    {
        Seasons = await _scheduleService.GetSeasonsAsync(ct);
        TrainingPhases = await _scheduleService.GetTrainingPhasesAsync(ct);
    }
}
