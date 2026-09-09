namespace SportSys.Contract.Models;

public class TrainingRequirementListItem
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public string SeasonCategoryName { get; set; } = string.Empty;
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

public class TrainingRequirementCoachListItem
{
    public int CoachId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string PersonalNumber { get; set; } = string.Empty;
    public int CoachRoleId { get; set; }
    public string CoachRoleName { get; set; } = string.Empty;

    public string DisplayText
        => $"{(string.IsNullOrWhiteSpace(DisplayName) ? PersonalNumber : DisplayName)} ({CoachRoleName})";
}
