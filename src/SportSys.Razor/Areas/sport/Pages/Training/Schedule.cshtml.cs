using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Razor.Models.TrainingSchedule;
using System.Globalization;

namespace SportSys.Razor.Areas.sport.Pages.Training;

public class ScheduleModel : PageModel
{
    private readonly TrainingScheduleService _service;

    public ScheduleModel(TrainingScheduleService service)
    {
        _service = service;
    }

    public List<SeasonDto> Seasons { get; private set; } = [];
    public List<SeasonCategoryDto> SeasonCategories { get; private set; } = [];
    public List<LookupSelectItem> TrainingTypes { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? SeasonId { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<string> SelectedCategories { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedTrainingTypeIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public DateOnly? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? DateTo { get; set; }

    public ITrainingScheduleViewModel? ScheduleView { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Seasons = await _service.GetSeasonsAsync(ct);
        TrainingTypes = await _service.GetTrainingTypesAsync(ct);

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
            !DateFrom.HasValue ||
            !DateTo.HasValue ||
            DateFrom > DateTo)
        {
            return;
        }

        var trainings = await _service.GetTrainingsAsync(
            SeasonId.Value,
            SelectedCategories,
            SelectedTrainingTypeIds,
            DateFrom.Value,
            DateTo.Value,
            ct);

        var byDate = trainings
            .GroupBy(t => t.Date)
            .ToDictionary(g => g.Key, g => g.Cast<ITrainingScheduleItem>().ToList());

        var rows = new List<TrainingScheduleRow>();
        for (var date = DateFrom.Value; date <= DateTo.Value; date = date.AddDays(1))
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

        var categoryOrder = SeasonCategories
            .Where(c => SelectedCategories.Contains(c.Name))
            .Select(c => c.Name)
            .ToList();

        ScheduleView = new TrainingScheduleViewModel(rows, categoryOrder);
    }

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
