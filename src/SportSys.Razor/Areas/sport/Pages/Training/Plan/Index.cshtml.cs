using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Razor.Models.TrainingSchedule;

namespace SportSys.Razor.Areas.sport.Pages.Training.Plan;

public class IndexModel : PageModel
{
    private readonly TrainingScheduleService _service;

    public IndexModel(TrainingScheduleService service)
    {
        _service = service;
    }

    public List<SeasonDto> Seasons { get; private set; } = [];
    public List<SeasonCategoryDto> SeasonCategories { get; private set; } = [];
    public List<LookupSelectItem> TrainingTypes { get; private set; } = [];
    public List<LookupSelectItem> TrainingPhases { get; private set; } = [];
    public List<LookupSelectItem> Locations { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? SeasonId { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<string> SelectedCategoryCodes { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedTrainingTypeIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedLocationIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? TrainingPhaseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? ValidOn { get; set; }

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

    public ITrainingScheduleViewModel? ScheduleView { get; private set; }

    public string? SelectedSeasonName { get; private set; }

    /// <summary>
    /// Záznamy <c>sport.TrainingPlan</c> odpovídající aktuálním filtrům, jeden
    /// řádek = jeden záznam, bez agregace podle <see cref="MergeTrainings"/>
    /// (ta ovlivňuje jen vykreslení grafického rozvrhu v
    /// <see cref="EventModelFactory.CreateTrainingPlans"/>).
    /// </summary>
    public IReadOnlyList<TrainingPlanScheduleItemDto> PlanRows { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Seasons = await _service.GetSeasonsAsync(ct);
        TrainingTypes = await _service.GetTrainingTypesAsync(ct);
        TrainingPhases = await _service.GetTrainingPhasesAsync(ct);
        Locations = await _service.GetTrainingPlanLocationsAsync(ct);

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

        var requestedLocationIds = SelectedLocationIds.ToHashSet();
        SelectedLocationIds = Locations
            .Where(location => requestedLocationIds.Contains(location.Id))
            .Select(location => location.Id)
            .ToList();

        if (TrainingPhaseId.HasValue && TrainingPhases.All(p => p.Id != TrainingPhaseId.Value))
            TrainingPhaseId = null;

        if (SeasonId.HasValue)
        {
            SeasonCategories = await _service.GetCategoriesAsync(SeasonId.Value, ct);
            var validCategoryCodes = SeasonCategories.Select(c => c.Code).ToHashSet();
            SelectedCategoryCodes = SelectedCategoryCodes
                .Where(validCategoryCodes.Contains)
                .Distinct()
                .ToList();
        }

        if (!SeasonId.HasValue)
        {
            return;
        }

        SelectedSeasonName = Seasons.FirstOrDefault(s => s.Id == SeasonId.Value)?.Name;

        var categoryCodes = SelectedCategoryCodes.Count > 0
            ? SelectedCategoryCodes
            : SeasonCategories.Select(c => c.Code).ToList();

        var plans = await _service.GetTrainingPlansAsync(
            SeasonId.Value,
            categoryCodes,
            SelectedTrainingTypeIds,
            SelectedLocationIds,
            TrainingPhaseId,
            ValidOn,
            MergeTrainings,
            ct);

        plans = WeekDayNames.FilterByDay(plans, plan => plan.DayOfWeek, SelectedDaysOfWeek);

        PlanRows = plans;

        var byDay = plans
            .GroupBy(plan => plan.DayOfWeek)
            .ToDictionary(
                group => group.Key,
                group => EventModelFactory
                    .CreateTrainingPlans(group.ToList(), allowEditing: !MergeTrainings)
                    .ToList());

        var weekDays = SelectedDaysOfWeek.Count == 0
            ? WeekDayNames.OrderedDays
            : WeekDayNames.OrderedDays.Where(SelectedDaysOfWeek.Contains);

        var rows = weekDays
            .Select((day, index) => new TrainingScheduleRow
            {
                PrimaryLabel = FormatDayOfWeek(day),
                Parity = index % 2 == 0
                    ? TrainingScheduleRowParity.Odd
                    : TrainingScheduleRowParity.Even,
                IsWeekend = day is DayOfWeek.Saturday or DayOfWeek.Sunday,
                Items = byDay.GetValueOrDefault(day) ?? [],
            })
            .Where(row => ShowEmptyRows || row.Items.Count > 0)
            .ToList();

        var categoryOrder = SeasonCategories
            .Where(c => SelectedCategoryCodes.Count == 0 || SelectedCategoryCodes.Contains(c.Code))
            .Select(c => c.Code)
            .ToList();

        ScheduleView = new TrainingScheduleViewModel(
            rows,
            categoryOrder,
            allowEditing: !MergeTrainings);
    }

    private static string FormatDayOfWeek(DayOfWeek day)
        => day switch
        {
            DayOfWeek.Monday => "Pondělí",
            DayOfWeek.Tuesday => "Úterý",
            DayOfWeek.Wednesday => "Středa",
            DayOfWeek.Thursday => "Čtvrtek",
            DayOfWeek.Friday => "Pátek",
            DayOfWeek.Saturday => "Sobota",
            DayOfWeek.Sunday => "Neděle",
            _ => throw new ArgumentOutOfRangeException(nameof(day)),
        };
}
