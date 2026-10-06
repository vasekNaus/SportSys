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

public sealed class SimpleCoachDto
{
    public required string FullName { get; init; }
    public required string LastName { get; init; }
}

public interface ITrainingScheduleItem
{
    int Id { get; }
    TimeOnly TimeFrom { get; }
    TimeOnly TimeTo { get; }
    int? DurationMinutes { get; }
    Guid? GroupId { get; }
    Guid? VisualizationGroupId { get; set; }
    int SeasonCategoryOrder { get; }
    string SeasonCategoryName { get; }
    string LocationName { get; }
    string TrainingTypeName { get; }
    bool IsDryTraining { get; }
    string TrainingPhaseName { get; }
    IReadOnlyList<SimpleCoachDto> Coaches { get; }
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
    public string Title { get; set; } = string.Empty;
    public TimeOnly TimeFrom { get; set; }
    public TimeOnly TimeTo { get; set; }
    public int? DurationMinutes { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? VisualizationGroupId { get; set; }
    public int SeasonCategoryOrder { get; set; }
    public string SeasonCategoryName { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string TrainingTypeName { get; set; } = string.Empty;
    public bool IsDryTraining { get; set; }
    public string TrainingPhaseName { get; set; } = string.Empty;
    public IReadOnlyList<SimpleCoachDto> Coaches { get; set; } = [];
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

public class TrainingScheduleItemDto : SportEventDto, ITrainingScheduleItem
{
    public Guid? GroupId { get; set; }
    public Guid? VisualizationGroupId { get; set; }
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string TrainingTypeName { get; set; } = string.Empty;
    public bool IsDryTraining { get; set; }
    public string TrainingPhaseName { get; set; } = string.Empty;
    public IReadOnlyList<SimpleCoachDto> Coaches { get; set; } = [];
    public int? TrainingStateId { get; set; }
    public string? TrainingStateName { get; set; }
}
