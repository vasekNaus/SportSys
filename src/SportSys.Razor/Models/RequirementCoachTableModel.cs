using Microsoft.AspNetCore.Mvc.Rendering;
using SportSys.Contract.Models;

namespace SportSys.Razor.Models;

public class RequirementCoachTableModel
{
    public required IReadOnlyList<RequirementCoachAssignmentInput> Assignments { get; init; }
    public required IReadOnlyList<SelectListItem> CoachItems { get; init; }
    public required IReadOnlyList<SelectListItem> RoleItems { get; init; }
}
