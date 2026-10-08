using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.sport.Pages.Training.Requirement;

public class IndexModel : PageModel
{
    private readonly TrainingRequirementService _service;
    private readonly MatchRequirementService _matchService;

    public IndexModel(
        TrainingRequirementService service,
        MatchRequirementService matchService)
    {
        _service = service;
        _matchService = matchService;
    }

    public List<SeasonDto> Seasons { get; private set; } = [];
    public List<SeasonCategoryDto> SeasonCategories { get; private set; } = [];
    public List<LookupSelectItem> TrainingTypes { get; private set; } = [];
    public List<LookupSelectItem> TrainingPhases { get; private set; } = [];
    public List<TrainingRequirementListItem> Requirements { get; private set; } = [];
    public List<MatchRequirementListItem> MatchRequirements { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? SeasonId { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<string> SelectedCategoryCodes { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedTrainingTypeIds { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedTrainingPhaseIds { get; set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Seasons = await _service.GetSeasonsAsync(ct);
        TrainingTypes = await _service.GetTrainingTypesAsync(ct);
        TrainingPhases = await _service.GetTrainingPhasesAsync(ct);

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

        Requirements = await _service.GetAllAsync(
            SeasonId.Value,
            SelectedCategoryCodes,
            SelectedTrainingTypeIds,
            SelectedTrainingPhaseIds,
            ct);

        MatchRequirements = await _matchService.GetAllAsync(
            SeasonId.Value,
            SelectedCategoryCodes,
            ct);
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
