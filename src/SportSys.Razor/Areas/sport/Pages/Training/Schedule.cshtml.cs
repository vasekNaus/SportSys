using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Razor.Models.TrainingSchedule;
using SportSys.Razor.Services;
using System.Globalization;

namespace SportSys.Razor.Areas.sport.Pages.Training;

public class ScheduleModel : PageModel
{
    private readonly TrainingScheduleService _service;
    private readonly TrainingScheduleExcelExporter _excelExporter;

    public ScheduleModel(
        TrainingScheduleService service,
        TrainingScheduleExcelExporter excelExporter)
    {
        _service = service;
        _excelExporter = excelExporter;
    }

    public List<SeasonDto> Seasons { get; private set; } = [];
    public List<SeasonCategoryDto> SeasonCategories { get; private set; } = [];
    public List<LookupSelectItem> TrainingTypes { get; private set; } = [];
    public List<string> Locations { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? SeasonId { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<string> SelectedCategories { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedTrainingTypeIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<string> SelectedLocations { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public DateOnly? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? DateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowEmptyRows { get; set; } = true;

    public ITrainingScheduleViewModel? ScheduleView { get; private set; }
    public string? ExportErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (!await LoadAndNormalizeFiltersAsync(ct))
            return;

        var filter = GetNormalizedFilter();
        var trainings = await LoadTrainingsAsync(filter, ct);
        ScheduleView = CreateScheduleView(trainings, filter);
    }

    public async Task<IActionResult> OnGetExportAsync(CancellationToken ct)
    {
        if (!await LoadAndNormalizeFiltersAsync(ct))
        {
            ExportErrorMessage = "Pro export vyberte platnou sezónu a období.";
            return Page();
        }

        var filter = GetNormalizedFilter();
        var trainings = await LoadTrainingsAsync(filter, ct);
        ScheduleView = CreateScheduleView(trainings, filter);

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

    private async Task<bool> LoadAndNormalizeFiltersAsync(CancellationToken ct)
    {
        Seasons = await _service.GetSeasonsAsync(ct);
        TrainingTypes = await _service.GetTrainingTypesAsync(ct);
        Locations = await _service.GetTrainingLocationsAsync(ct);

        if (SeasonId.HasValue && Seasons.All(s => s.Id != SeasonId.Value))
        {
            SeasonId = null;
            SelectedCategories = [];
        }

        var requestedTrainingTypeIds = SelectedTrainingTypeIds.ToHashSet();
        SelectedTrainingTypeIds = TrainingTypes
            .Where(t => requestedTrainingTypeIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToList();

        var requestedLocations = SelectedLocations.ToHashSet();
        SelectedLocations = Locations
            .Where(requestedLocations.Contains)
            .ToList();

        if (SeasonId.HasValue)
        {
            SeasonCategories = await _service.GetCategoriesAsync(SeasonId.Value, ct);
            var validCategories = SeasonCategories.Select(c => c.Name).ToHashSet();
            SelectedCategories = SelectedCategories
                .Where(validCategories.Contains)
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

    private NormalizedScheduleFilter GetNormalizedFilter()
        => new(
            SeasonId ?? throw new InvalidOperationException("Sezóna není nastavena."),
            DateFrom ?? throw new InvalidOperationException("Počáteční datum není nastaveno."),
            DateTo ?? throw new InvalidOperationException("Koncové datum není nastaveno."));

    private Task<List<TrainingScheduleItemDto>> LoadTrainingsAsync(
        NormalizedScheduleFilter filter,
        CancellationToken ct)
        => _service.GetTrainingsAsync(
            filter.SeasonId,
            SelectedCategories.Count > 0
                ? SelectedCategories
                : SeasonCategories.Select(c => c.Name).ToList(),
            SelectedTrainingTypeIds,
            SelectedLocations,
            filter.DateFrom,
            filter.DateTo,
            ct);

    private ITrainingScheduleViewModel CreateScheduleView(
        IReadOnlyList<TrainingScheduleItemDto> trainings,
        NormalizedScheduleFilter filter)
    {
        var byDate = trainings
            .GroupBy(t => t.Date)
            .ToDictionary(g => g.Key, g => g.Cast<ITrainingScheduleItem>().ToList());

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
            .Where(c => SelectedCategories.Count == 0 || SelectedCategories.Contains(c.Name))
            .Select(c => c.Name)
            .ToList();

        return new TrainingScheduleViewModel(rows, categoryOrder);
    }

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
