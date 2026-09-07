using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Razor.Models.TrainingSchedule;

namespace SportSys.Razor.Areas.sport.Pages.Training;

public class PlanModel : PageModel
{
    private static readonly DayOfWeek[] WeekDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday,
    ];

    private readonly TrainingScheduleService _service;

    public PlanModel(TrainingScheduleService service)
    {
        _service = service;
    }

    public List<SeasonDto> Seasons { get; private set; } = [];
    public List<SeasonCategoryDto> SeasonCategories { get; private set; } = [];
    public List<LookupSelectItem> TrainingTypes { get; private set; } = [];
    public List<LookupSelectItem> TrainingPhases { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? SeasonId { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<string> SelectedCategories { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedTrainingTypeIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? TrainingPhaseId { get; set; }

    public ITrainingScheduleViewModel? ScheduleView { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Seasons = await _service.GetSeasonsAsync(ct);
        TrainingTypes = await _service.GetTrainingTypesAsync(ct);
        TrainingPhases = await _service.GetTrainingPhasesAsync(ct);

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

        if (TrainingPhaseId.HasValue && TrainingPhases.All(p => p.Id != TrainingPhaseId.Value))
            TrainingPhaseId = null;

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
            SelectedCategories.Count == 0 ||
            !TrainingPhaseId.HasValue)
        {
            return;
        }

        var plans = await _service.GetTrainingPlansAsync(
            SeasonId.Value,
            SelectedCategories,
            SelectedTrainingTypeIds,
            TrainingPhaseId.Value,
            ct);

        var byDay = plans
            .GroupBy(p => p.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Cast<ITrainingScheduleItem>().ToList());

        var rows = WeekDays
            .Select((day, index) => new TrainingScheduleRow
            {
                PrimaryLabel = FormatDayOfWeek(day),
                Parity = index % 2 == 0
                    ? TrainingScheduleRowParity.Odd
                    : TrainingScheduleRowParity.Even,
                IsWeekend = day is DayOfWeek.Saturday or DayOfWeek.Sunday,
                Items = byDay.GetValueOrDefault(day) ?? [],
            })
            .ToList();

        var categoryOrder = SeasonCategories
            .Where(c => SelectedCategories.Contains(c.Name))
            .Select(c => c.Name)
            .ToList();

        ScheduleView = new TrainingScheduleViewModel(rows, categoryOrder);
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
