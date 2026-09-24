using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.sport.Pages.Training.Requirement;

public class IndexModel : PageModel
{
    private readonly TrainingRequirementService _service;

    public IndexModel(TrainingRequirementService service)
    {
        _service = service;
    }

    public List<SeasonDto> Seasons { get; private set; } = [];
    public List<SeasonCategoryDto> SeasonCategories { get; private set; } = [];
    public List<LookupSelectItem> TrainingTypes { get; private set; } = [];
    public List<LookupSelectItem> TrainingPhases { get; private set; } = [];
    public List<TrainingRequirementListItem> Requirements { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? SeasonId { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<string> SelectedCategoryNames { get; set; } = [];

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
            SelectedCategoryNames = [];
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
        var validCategoryNames = SeasonCategories
            .Select(category => category.Name)
            .ToHashSet(StringComparer.Ordinal);
        SelectedCategoryNames = SelectedCategoryNames
            .Where(validCategoryNames.Contains)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Requirements = await _service.GetAllAsync(
            SeasonId.Value,
            SelectedCategoryNames,
            SelectedTrainingTypeIds,
            SelectedTrainingPhaseIds,
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
