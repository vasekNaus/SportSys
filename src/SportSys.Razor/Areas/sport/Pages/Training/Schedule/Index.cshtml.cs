using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Razor.Models.TrainingSchedule;
using SportSys.Razor.Services;
using System.Globalization;
using System.Text.Json;

namespace SportSys.Razor.Areas.sport.Pages.Training.Schedule;

public class IndexModel : PageModel
{
    private readonly TrainingScheduleService _service;
    private readonly MatchScheduleService _matchService;
    private readonly TrainingScheduleExcelExporter _excelExporter;
    private readonly KisTrainingImportService _kisTrainingImportService;
    private readonly KisMatchImportService _kisMatchImportService;

    public IndexModel(
        TrainingScheduleService service,
        MatchScheduleService matchService,
        TrainingScheduleExcelExporter excelExporter,
        KisTrainingImportService kisTrainingImportService,
        KisMatchImportService kisMatchImportService)
    {
        _service = service;
        _matchService = matchService;
        _excelExporter = excelExporter;
        _kisTrainingImportService = kisTrainingImportService;
        _kisMatchImportService = kisMatchImportService;
    }

    public List<SeasonDto> Seasons { get; private set; } = [];
    public List<SeasonCategoryDto> SeasonCategories { get; private set; } = [];
    public List<LookupSelectItem> TrainingTypes { get; private set; } = [];
    public List<LookupSelectItem> TrainingStates { get; private set; } = [];
    public List<LookupSelectItem> TrainingPhases { get; private set; } = [];
    public List<LookupSelectItem> Locations { get; private set; } = [];
    public List<LookupSelectItem> MatchStates { get; private set; } = [];
    public List<LookupSelectItem> MatchTypes { get; private set; } = [];
    public List<SelectListItem> CoachSelectList { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? SeasonId { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<string> SelectedCategoryCodes { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedTrainingTypeIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedTrainingStateIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedLocationIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedCoachIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedMatchStateIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedMatchTypeIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public DateOnly? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? DateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<DayOfWeek> SelectedDaysOfWeek { get; set; } = [];

    public IReadOnlyList<(DayOfWeek Day, string Label)> WeekDayOptions { get; } =
        WeekDayNames.OrderedDays
            .Select(day => (day, WeekDayNames.GetFullName(day)))
            .ToList();

    [BindProperty(SupportsGet = true)]
    public bool ShowEmptyRows { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public bool MergeTrainings { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowTrainings { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public bool ShowMatches { get; set; } = true;

    [BindProperty]
    public KisTrainingImportDto Import { get; set; } = new();

    [BindProperty]
    public IFormFile? ImportFile { get; set; }

    [BindProperty]
    public KisMatchImportDto MatchImport { get; set; } = new();

    [BindProperty]
    public IFormFile? MatchImportFile { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ImportWarningsJson { get; set; }

    [TempData]
    public string? MatchImportWarningsJson { get; set; }

    public ITrainingScheduleViewModel? ScheduleView { get; private set; }
    public bool HasExportableTrainings { get; private set; }
    public string? ExportErrorMessage { get; private set; }
    public IReadOnlyList<KisTrainingImportWarning> ImportWarnings { get; private set; } = [];
    public IReadOnlyList<KisMatchImportWarning> MatchImportWarnings { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        DeserializeImportWarnings();
        if (!await LoadAndNormalizeFiltersAsync(ct))
            return;

        var filter = GetNormalizedFilter();
        var categoryCodes = GetSelectedCategoryCodes();
        var trainings = ShowTrainings
            ? await LoadTrainingsAsync(filter, categoryCodes, ct)
            : [];
        var matches = ShowMatches
            ? await _matchService.GetMatchesAsync(
                filter.SeasonId,
                categoryCodes,
                filter.DateFrom,
                filter.DateTo,
                SelectedMatchStateIds,
                SelectedLocationIds,
                SelectedMatchTypeIds,
                SelectedCoachIds,
                ct)
            : [];
        (trainings, matches) = FilterByDaysOfWeek(trainings, matches);
        HasExportableTrainings = trainings.Count > 0;
        ScheduleView = CreateScheduleView(trainings, matches, filter);
    }

    public async Task<IActionResult> OnGetExportAsync(CancellationToken ct)
    {
        if (!await LoadAndNormalizeFiltersAsync(ct))
        {
            ExportErrorMessage = "Pro export vyberte platnou sezónu a období.";
            return Page();
        }

        var filter = GetNormalizedFilter();
        var categoryCodes = GetSelectedCategoryCodes();
        var trainings = ShowTrainings
            ? await LoadTrainingsAsync(filter, categoryCodes, ct)
            : [];
        var matches = ShowMatches
            ? await _matchService.GetMatchesAsync(
                filter.SeasonId,
                categoryCodes,
                filter.DateFrom,
                filter.DateTo,
                SelectedMatchStateIds,
                SelectedLocationIds,
                SelectedMatchTypeIds,
                SelectedCoachIds,
                ct)
            : [];
        (trainings, matches) = FilterByDaysOfWeek(trainings, matches);
        HasExportableTrainings = trainings.Count > 0;
        ScheduleView = CreateScheduleView(trainings, matches, filter);

        if (trainings.Count == 0)
        {
            ExportErrorMessage = "Pro zadané parametry nebyly nalezeny žádné tréninky k exportu.";
            return Page();
        }

        var content = _excelExporter.Export(trainings);
        var fileName = $"treninky-{filter.DateFrom:yyyyMMdd}-{filter.DateTo:yyyyMMdd}.xlsx";

        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    public async Task<IActionResult> OnPostImportAsync(CancellationToken ct)
    {
        RetainImportModelState(nameof(Import), nameof(ImportFile));
        ValidateImportFile(ImportFile, nameof(ImportFile));

        if (!ModelState.IsValid)
        {
            await LoadAndNormalizeFiltersAsync(ct);
            return Page();
        }

        try
        {
            await using var stream = ImportFile!.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            buffer.Position = 0;

            var result = await _kisTrainingImportService.ImportAsync(buffer, Import, ct);
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

        await LoadAndNormalizeFiltersAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostImportMatchesAsync(CancellationToken ct)
    {
        RetainImportModelState(nameof(MatchImport), nameof(MatchImportFile));
        ValidateImportFile(MatchImportFile, nameof(MatchImportFile));

        if (!ModelState.IsValid)
        {
            await LoadAndNormalizeFiltersAsync(ct);
            return Page();
        }

        try
        {
            await using var stream = MatchImportFile!.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            buffer.Position = 0;

            var result = await _kisMatchImportService.ImportAsync(buffer, MatchImport.SeasonId, ct);
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

        await LoadAndNormalizeFiltersAsync(ct);
        return Page();
    }

    private async Task<bool> LoadAndNormalizeFiltersAsync(CancellationToken ct)
    {
        Seasons = await _service.GetSeasonsAsync(ct);
        TrainingTypes = await _service.GetTrainingTypesAsync(ct);
        TrainingStates = await _service.GetTrainingStatesAsync(ct);
        TrainingPhases = await _service.GetTrainingPhasesAsync(ct);
        Locations = await _service.GetTrainingLocationsAsync(SelectedLocationIds, ct);
        MatchStates = await _matchService.GetMatchStatesAsync(ct);
        MatchTypes = await _matchService.GetMatchTypesAsync(ct);
        var coaches = await _service.GetCoachesAsync(ct);
        CoachSelectList = coaches
            .Select(coach => new SelectListItem(coach.DisplayName, coach.Id.ToString()))
            .ToList();
        var availableCoachIds = coaches.Select(coach => coach.Id).ToHashSet();
        SelectedCoachIds = SelectedCoachIds
            .Where(availableCoachIds.Contains)
            .Distinct()
            .ToList();

        if (SeasonId.HasValue && Seasons.All(s => s.Id != SeasonId.Value))
        {
            SeasonId = null;
            SelectedCategoryCodes = [];
        }

        var requestedTrainingTypeIds = SelectedTrainingTypeIds.ToHashSet();
        SelectedTrainingTypeIds = TrainingTypes
            .Where(t => requestedTrainingTypeIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToList();

        var requestedTrainingStateIds = SelectedTrainingStateIds.ToHashSet();
        SelectedTrainingStateIds = TrainingStates
            .Where(s => requestedTrainingStateIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToList();

        var requestedLocationIds = SelectedLocationIds.ToHashSet();
        SelectedLocationIds = Locations
            .Where(location => requestedLocationIds.Contains(location.Id))
            .Select(location => location.Id)
            .ToList();

        var requestedMatchStateIds = SelectedMatchStateIds.ToHashSet();
        SelectedMatchStateIds = MatchStates
            .Where(state => requestedMatchStateIds.Contains(state.Id))
            .Select(state => state.Id)
            .ToList();

        var requestedMatchTypeIds = SelectedMatchTypeIds.ToHashSet();
        SelectedMatchTypeIds = MatchTypes
            .Where(type => requestedMatchTypeIds.Contains(type.Id))
            .Select(type => type.Id)
            .ToList();

        if (SeasonId.HasValue)
        {
            SeasonCategories = await _service.GetCategoriesAsync(SeasonId.Value, ct);
            var validCategoryCodes = SeasonCategories.Select(c => c.Code).ToHashSet();
            SelectedCategoryCodes = SelectedCategoryCodes
                .Where(validCategoryCodes.Contains)
                .Distinct()
                .ToList();
        }

        if (!SeasonId.HasValue ||
            !DateFrom.HasValue ||
            !DateTo.HasValue ||
            DateFrom > DateTo)
        {
            return false;
        }

        return true;
    }

    private void ValidateImportFile(IFormFile? file, string key)
    {
        if (file is null || file.Length == 0)
            ModelState.AddModelError(key, "Vyberte soubor XLSX.");
        else if (file.Length > 10 * 1024 * 1024)
            ModelState.AddModelError(key, "Soubor může mít maximálně 10 MiB.");
        else if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(key, "Vyberte soubor ve formátu XLSX.");
    }

    private void RetainImportModelState(string inputKey, string fileKey)
    {
        foreach (var key in ModelState.Keys
                     .Where(key =>
                         !key.Equals(inputKey, StringComparison.Ordinal) &&
                         !key.StartsWith($"{inputKey}.", StringComparison.Ordinal) &&
                         !key.Equals(fileKey, StringComparison.Ordinal))
                     .ToList())
        {
            ModelState.Remove(key);
        }
    }

    private void DeserializeImportWarnings()
    {
        if (ImportWarningsJson is not null)
        {
            ImportWarnings = JsonSerializer.Deserialize<List<KisTrainingImportWarning>>(ImportWarningsJson)
                ?? [];
        }

        if (MatchImportWarningsJson is not null)
        {
            MatchImportWarnings = JsonSerializer.Deserialize<List<KisMatchImportWarning>>(MatchImportWarningsJson)
                ?? [];
        }
    }

    private NormalizedScheduleFilter GetNormalizedFilter()
        => new(
            SeasonId ?? throw new InvalidOperationException("Sezóna není nastavena."),
            DateFrom ?? throw new InvalidOperationException("Počáteční datum není nastaveno."),
            DateTo ?? throw new InvalidOperationException("Koncové datum není nastaveno."));

    private Task<List<TrainingScheduleItemDto>> LoadTrainingsAsync(
        NormalizedScheduleFilter filter,
        IReadOnlyCollection<string> categoryCodes,
        CancellationToken ct)
        => _service.GetTrainingsAsync(
            filter.SeasonId,
            categoryCodes,
            SelectedTrainingTypeIds,
            SelectedTrainingStateIds,
            SelectedLocationIds,
            filter.DateFrom,
            filter.DateTo,
            MergeTrainings,
            SelectedCoachIds,
            ct);

    private (List<TrainingScheduleItemDto> Trainings, List<MatchScheduleItemDto> Matches) FilterByDaysOfWeek(
        List<TrainingScheduleItemDto> trainings,
        List<MatchScheduleItemDto> matches)
        => (
            WeekDayNames.FilterByDay(trainings, t => t.Date.DayOfWeek, SelectedDaysOfWeek),
            WeekDayNames.FilterByDay(matches, m => m.Date.DayOfWeek, SelectedDaysOfWeek));

    private ITrainingScheduleViewModel CreateScheduleView(
        IReadOnlyList<TrainingScheduleItemDto> trainings,
        IReadOnlyList<MatchScheduleItemDto> matches,
        NormalizedScheduleFilter filter)
    {
        var byDate = trainings
            .GroupBy(training => training.Date)
            .SelectMany(group => EventModelFactory
                .CreateTrainings(group.ToList(), allowEditing: !MergeTrainings)
                .Select(scheduleEvent => new
                {
                    Date = group.Key,
                    Event = scheduleEvent,
                }))
            .Concat(matches.Select(match => new
            {
                match.Date,
                Event = EventModelFactory.CreateMatch(match),
            }))
            .GroupBy(entry => entry.Date)
            .ToDictionary(
                group => group.Key,
                group => group.Select(entry => entry.Event).ToList());

        var rows = new List<TrainingScheduleRow>();
        for (var date = filter.DateFrom; date <= filter.DateTo; date = date.AddDays(1))
        {
            rows.Add(new TrainingScheduleRow
            {
                PrimaryLabel = FormatDayOfWeek(date.DayOfWeek),
                SecondaryLabel = date.ToString("d.M.", CultureInfo.InvariantCulture),
                Parity = date.Day % 2 == 0
                    ? TrainingScheduleRowParity.Even
                    : TrainingScheduleRowParity.Odd,
                IsWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                Items = byDate.GetValueOrDefault(date) ?? [],
            });
        }

        if (!ShowEmptyRows)
            rows.RemoveAll(row => row.Items.Count == 0);

        var categoryOrder = SeasonCategories
            .Where(c => SelectedCategoryCodes.Count == 0 || SelectedCategoryCodes.Contains(c.Code))
            .Select(c => c.Code)
            .ToList();

        return new TrainingScheduleViewModel(
            rows,
            categoryOrder,
            allowEditing: !MergeTrainings);
    }

    private IReadOnlyCollection<string> GetSelectedCategoryCodes()
        => SelectedCategoryCodes.Count > 0
            ? SelectedCategoryCodes
            : SeasonCategories.Select(category => category.Code).ToList();

    private readonly record struct NormalizedScheduleFilter(
        int SeasonId,
        DateOnly DateFrom,
        DateOnly DateTo);

    private static string FormatDayOfWeek(DayOfWeek day)
        => day switch
        {
            DayOfWeek.Monday => "Po",
            DayOfWeek.Tuesday => "Út",
            DayOfWeek.Wednesday => "St",
            DayOfWeek.Thursday => "Čt",
            DayOfWeek.Friday => "Pá",
            DayOfWeek.Saturday => "So",
            DayOfWeek.Sunday => "Ne",
            _ => throw new ArgumentOutOfRangeException(nameof(day)),
        };
}
