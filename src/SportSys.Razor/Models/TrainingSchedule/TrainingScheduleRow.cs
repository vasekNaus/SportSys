namespace SportSys.Razor.Models.TrainingSchedule;

public class TrainingScheduleRow
{
    public required string PrimaryLabel { get; init; }
    public string? SecondaryLabel { get; init; }
    public required TrainingScheduleRowParity Parity { get; init; }
    public bool IsWeekend { get; init; }
    public IReadOnlyList<EventModel> Items { get; init; } = [];
}
