namespace SportSys.Contract.Models;

public class SeasonDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class SeasonCategoryDto
{
    public int SeasonId { get; set; }
    public string Name { get; set; } = "";
    public int Order { get; set; }
}

public interface ITrainingScheduleItem
{
    int Id { get; }
    TimeOnly TimeFrom { get; }
    TimeOnly TimeTo { get; }
    int? DurationMinutes { get; }
    Guid? GroupId { get; }
    Guid? VisualizationGroupId { get; }
    int SeasonCategoryOrder { get; }
    string SeasonCategoryName { get; }
    string Location { get; }
    string TrainingTypeName { get; }
    string TrainingPhaseName { get; }
    IReadOnlyList<string> CoachFullNames { get; }
    string Note { get; }
    int? TrainingStateId { get; }
    string? TrainingStateName { get; }
}

public class TrainingPlanScheduleItemDto : ITrainingScheduleItem
{
    public int Id { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string DayName { get; set; } = string.Empty;
    public TimeOnly TimeFrom { get; set; }
    public TimeOnly TimeTo { get; set; }
    public int? DurationMinutes { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? VisualizationGroupId { get; set; }
    public int SeasonCategoryOrder { get; set; }
    public string SeasonCategoryName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TrainingTypeName { get; set; } = string.Empty;
    public string TrainingPhaseName { get; set; } = string.Empty;
    public IReadOnlyList<string> CoachFullNames { get; set; } = [];
    public string Note { get; set; } = string.Empty;
    public int? TrainingStateId { get; set; }
    public string? TrainingStateName { get; set; }

    public DayOfWeek DayOfWeek
    {
        get
        {
            if (Enum.TryParse<DayOfWeek>(DayName, ignoreCase: false, out var day) &&
                Enum.IsDefined(day) &&
                DayName == day.ToString())
            {
                return day;
            }

            throw new InvalidOperationException(
                $"TrainingPlan {Id} obsahuje neplatnou hodnotu DayName '{DayName}'.");
        }
    }
}

public class TrainingScheduleItemDto : TrainingPlanScheduleItemDto
{
    public DateOnly Date { get; set; }
}
