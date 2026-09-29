using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Razor.Models.TrainingSchedule;
using SportSys.Razor.Services;
using System.Globalization;

namespace SportSys.Razor.Areas.sport.Pages.Training.Schedule;

public class IndexModel : PageModel
{
    private readonly TrainingScheduleService _service;
    private readonly MatchScheduleService _matchService;
    private readonly TrainingScheduleExcelExporter _excelExporter;

    public IndexModel(
        TrainingScheduleService service,
        MatchScheduleService matchService,
        TrainingScheduleExcelExporter excelExporter)
    {
        _service = service;
        _matchService = matchService;
        _excelExporter = excelExporter;
    }

    public List<SeasonDto> Seasons { get; private set; } = [];
    public List<SeasonCategoryDto> SeasonCategories { get; private set; } = [];
    public List<LookupSelectItem> TrainingTypes { get; private set; } = [];
    public List<LookupSelectItem> TrainingStates { get; private set; } = [];
    public List<LookupSelectItem> Locations { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? SeasonId { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<string> SelectedCategories { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedTrainingTypeIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedTrainingStateIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedLocationIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public DateOnly? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? DateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowEmptyRows { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public bool MergeTrainings { get; set; }

    public ITrainingScheduleViewModel? ScheduleView { get; private set; }
    public bool HasExportableTrainings { get; private set; }
    public string? ExportErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (!await LoadAndNormalizeFiltersAsync(ct))
            return;

        var filter = GetNormalizedFilter();
        var categories = GetSelectedCategoryNames();
        var trainings = await LoadTrainingsAsync(filter, categories, ct);
        var matches = await _matchService.GetMatchesAsync(
            filter.SeasonId,
            categories,
            filter.DateFrom,
            filter.DateTo,
            ct);
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
        var categories = GetSelectedCategoryNames();
        var trainings = await LoadTrainingsAsync(filter, categories, ct);
        var matches = await _matchService.GetMatchesAsync(
            filter.SeasonId,
            categories,
            filter.DateFrom,
            filter.DateTo,
            ct);
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

    private async Task<bool> LoadAndNormalizeFiltersAsync(CancellationToken ct)
    {
        Seasons = await _service.GetSeasonsAsync(ct);
        TrainingTypes = await _service.GetTrainingTypesAsync(ct);
        TrainingStates = await _service.GetTrainingStatesAsync(ct);
        Locations = await _service.GetTrainingLocationsAsync(SelectedLocationIds, ct);

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
        IReadOnlyCollection<string> categories,
        CancellationToken ct)
        => _service.GetTrainingsAsync(
            filter.SeasonId,
            categories,
            SelectedTrainingTypeIds,
            SelectedTrainingStateIds,
            SelectedLocationIds,
            filter.DateFrom,
            filter.DateTo,
            MergeTrainings,
            ct);

    private ITrainingScheduleViewModel CreateScheduleView(
        IReadOnlyList<TrainingScheduleItemDto> trainings,
        IReadOnlyList<MatchScheduleItemDto> matches,
        NormalizedScheduleFilter filter)
    {
        var byDate = trainings
            .GroupBy(training => training.Date)
            .SelectMany(group => ScheduleEventModelFactory
                .CreateTrainings(group.ToList(), allowEditing: !MergeTrainings)
                .Select(scheduleEvent => new
                {
                    Date = group.Key,
                    Event = scheduleEvent,
                }))
            .Concat(matches.Select(match => new
            {
                match.Date,
                Event = ScheduleEventModelFactory.CreateMatch(match),
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
            .Where(c => SelectedCategories.Count == 0 || SelectedCategories.Contains(c.Name))
            .Select(c => c.Name)
            .ToList();

        return new TrainingScheduleViewModel(
            rows,
            categoryOrder,
            allowEditing: !MergeTrainings);
    }

    private IReadOnlyCollection<string> GetSelectedCategoryNames()
        => SelectedCategories.Count > 0
            ? SelectedCategories
            : SeasonCategories.Select(category => category.Name).ToList();

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
