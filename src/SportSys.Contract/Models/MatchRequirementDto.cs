using SportSys.Contract.Models.hr;

namespace SportSys.Contract.Models;

public class MatchRequirementListItem
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public string SeasonCategoryCode { get; set; } = string.Empty;
    public int SeasonCategoryOrder { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int MatchCount { get; set; }
    public IReadOnlyList<MatchRequirementCoachListItem> CoachAssignments { get; set; } = [];
}

public class MatchRequirementCoachListItem : RequirementCoachListItem
{
}
