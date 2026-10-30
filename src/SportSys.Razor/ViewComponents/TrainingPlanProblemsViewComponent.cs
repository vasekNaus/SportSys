using Microsoft.AspNetCore.Mvc;
using SportSys.Razor.Models.TrainingSchedule;

namespace SportSys.Razor.ViewComponents;

public sealed class TrainingPlanProblemsViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IReadOnlyList<TrainingPlanProblem> problems)
        => View(problems);
}
