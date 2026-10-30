using Microsoft.AspNetCore.Mvc;
using SportSys.Contract.Models;
using SportSys.Razor.Models;

namespace SportSys.Razor.ViewComponents;

public sealed class TrainingRequirementCoverageViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(
        IEnumerable<TrainingRequirementListItem> requirements,
        IEnumerable<TrainingPlanScheduleItemDto> plans,
        IReadOnlyCollection<int>? selectedCoachIds = null)
        => View(TrainingRequirementCoverageBuilder.Build(
            requirements,
            plans,
            selectedCoachIds));
}
