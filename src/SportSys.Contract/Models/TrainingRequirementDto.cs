using SportSys.Contract.Models.hr;

namespace SportSys.Contract.Models;

public class TrainingRequirementListItem
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public string SeasonCategoryCode { get; set; } = string.Empty;
    public int SeasonCategoryOrder { get; set; }
    public int TrainingTypeId { get; set; }
    public string TrainingTypeName { get; set; } = string.Empty;
    public int TrainingPhaseId { get; set; }
    public string TrainingPhaseName { get; set; } = string.Empty;
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public decimal DurationHours { get; set; }
    public IReadOnlyList<TrainingRequirementCoachListItem> CoachAssignments { get; set; } = [];
}

public abstract class RequirementCoachListItem : CoachSelectItem
{
    public int CoachRoleId { get; set; }
    public string CoachRoleName { get; set; } = string.Empty;
    public string CoachRoleCode { get; set; } = string.Empty;
    public int CoachRoleOrder { get; set; }

    public string LastName
    {
        get
        {
            var name = DisplayName.Trim();
            if (name.Length == 0)
                return PersonalNumber;

            var lastSpaceIndex = name.LastIndexOf(' ');
            return lastSpaceIndex < 0 ? name : name[(lastSpaceIndex + 1)..];
        }
    }

    public string DisplayText
        => $"{(string.IsNullOrWhiteSpace(DisplayName) ? PersonalNumber : DisplayName)} ({CoachRoleName})";
}

public class TrainingRequirementCoachListItem : RequirementCoachListItem
{
}
