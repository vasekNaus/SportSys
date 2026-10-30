using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Razor.Models;
using SportSys.Razor.Services;

namespace SportSys.Razor.Areas.sport.Pages.Training.Requirement;

public class IndexModel : PageModel
{
    private readonly TrainingRequirementService _service;
    private readonly MatchRequirementService _matchService;
    private readonly TrainingScheduleService _scheduleService;
    private readonly TrainingOverviewExcelExporter _excelExporter;

    public IndexModel(
        TrainingRequirementService service,
        MatchRequirementService matchService,
        TrainingScheduleService scheduleService,
        TrainingOverviewExcelExporter excelExporter)
    {
        _service = service;
        _matchService = matchService;
        _scheduleService = scheduleService;
        _excelExporter = excelExporter;
    }

    public List<SeasonDto> Seasons { get; private set; } = [];
    public List<SeasonCategoryDto> SeasonCategories { get; private set; } = [];
    public List<LookupSelectItem> TrainingTypes { get; private set; } = [];
    public List<LookupSelectItem> TrainingPhases { get; private set; } = [];
    public List<SelectListItem> CoachSelectList { get; private set; } = [];
    public List<TrainingRequirementListItem> Requirements { get; private set; } = [];
    public List<MatchRequirementListItem> MatchRequirements { get; private set; } = [];
    public IReadOnlyList<TrainingRequirementListItem> CoverageRequirements { get; private set; } = [];
    public IReadOnlyList<TrainingPlanScheduleItemDto> CoveragePlans { get; private set; } = [];
    public IReadOnlyList<TrainingRequirementCoverageRow> CoverageRows { get; private set; } = [];
    public string? ExportErrorMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int? SeasonId { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<string> SelectedCategoryCodes { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedTrainingTypeIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedTrainingPhaseIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedCoachIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public DateOnly? ValidOn { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowTrainings { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public bool ShowMatches { get; set; } = true;

    public async Task OnGetAsync(CancellationToken ct)
    {
        Seasons = await _service.GetSeasonsAsync(ct);
        TrainingTypes = await _service.GetTrainingTypesAsync(ct);
        TrainingPhases = await _service.GetTrainingPhasesAsync(ct);
        var coaches = await _service.GetCoachesAsync(ct);
        CoachSelectList = coaches
            .Select(coach => new SelectListItem(coach.DisplayName, coach.Id.ToString()))
            .ToList();
        var availableCoachIds = coaches.Select(coach => coach.Id).ToHashSet();
        SelectedCoachIds = SelectedCoachIds
            .Where(availableCoachIds.Contains)
            .Distinct()
            .ToList();

        if (!SeasonId.HasValue || Seasons.All(season => season.Id != SeasonId.Value))
        {
            SeasonId = Seasons.FirstOrDefault()?.Id;
            SelectedCategoryCodes = [];
        }

        SelectedTrainingTypeIds = NormalizeIds(
            SelectedTrainingTypeIds,
            TrainingTypes);
        SelectedTrainingPhaseIds = NormalizeIds(
            SelectedTrainingPhaseIds,
            TrainingPhases);

        if (!SeasonId.HasValue)
            return;

        SeasonCategories = await _service.GetCategoriesAsync(SeasonId.Value, ct);
        var validCategoryCodes = SeasonCategories
            .Select(category => category.Code)
            .ToHashSet(StringComparer.Ordinal);
        SelectedCategoryCodes = SelectedCategoryCodes
            .Where(validCategoryCodes.Contains)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Requirements = ShowTrainings
            ? await _service.GetAllAsync(
                SeasonId.Value,
                SelectedCategoryCodes,
                SelectedTrainingTypeIds,
                SelectedTrainingPhaseIds,
                ValidOn,
                SelectedCoachIds,
                ct)
            : [];

        if (ShowTrainings)
        {
            var categoryCodes = SelectedCategoryCodes.Count > 0
                ? SelectedCategoryCodes
                : SeasonCategories.Select(category => category.Code).ToList();

            CoverageRequirements = categoryCodes.Count == 0
                ? []
                : await _service.GetAllAsync(
                    SeasonId.Value,
                    categoryCodes,
                    SelectedTrainingTypeIds,
                    [],
                    ValidOn,
                    SelectedCoachIds,
                    ct);

            CoveragePlans = await _scheduleService.GetTrainingPlansAsync(
                SeasonId.Value,
                categoryCodes,
                SelectedTrainingTypeIds,
                [],
                trainingPhaseId: null,
                validOn: ValidOn,
                mergeOverlapping: false,
                coachIds: SelectedCoachIds,
                ct: ct);
        }

        MatchRequirements = ShowMatches
            ? await _matchService.GetAllAsync(
                SeasonId.Value,
                SelectedCategoryCodes,
                ValidOn,
                SelectedCoachIds,
                ct)
            : [];

        CoverageRows = TrainingRequirementCoverageBuilder.Build(
            CoverageRequirements,
            CoveragePlans,
            SelectedCoachIds);
    }

    public async Task<IActionResult> OnGetExportRequirementsAsync(CancellationToken ct)
    {
        await OnGetAsync(ct);
        if (!ModelState.IsValid)
        {
            ExportErrorMessage = "Export nelze provést. Opravte neplatné hodnoty filtrů.";
            return Page();
        }

        if (Requirements.Count == 0)
        {
            ExportErrorMessage = "Pro zadané filtry nejsou k dispozici žádné požadavky na tréninky k exportu.";
            return Page();
        }

        return File(
            _excelExporter.ExportRequirements(Requirements),
            TrainingOverviewExcelExporter.ContentType,
            "TrainingRequirement.xlsx");
    }

    public async Task<IActionResult> OnGetExportCoverageAsync(CancellationToken ct)
    {
        await OnGetAsync(ct);
        if (!ModelState.IsValid)
        {
            ExportErrorMessage = "Export nelze provést. Opravte neplatné hodnoty filtrů.";
            return Page();
        }

        if (CoverageRows.Count == 0)
        {
            ExportErrorMessage = "Pro zadané filtry nejsou k dispozici žádné řádky pokrytí k exportu.";
            return Page();
        }

        return File(
            _excelExporter.ExportCoverage(CoverageRows),
            TrainingOverviewExcelExporter.ContentType,
            "TrainingRequirementCoverage.xlsx");
    }

    internal static List<int> NormalizeIds(
        IEnumerable<int> requestedIds,
        IEnumerable<LookupSelectItem> availableItems)
    {
        var requested = requestedIds.ToHashSet();
        return availableItems
            .Where(item => requested.Contains(item.Id))
            .Select(item => item.Id)
            .ToList();
    }
}
