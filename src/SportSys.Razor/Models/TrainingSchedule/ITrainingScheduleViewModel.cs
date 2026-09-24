using SportSys.Contract.Models;

namespace SportSys.Razor.Models.TrainingSchedule;

public interface ITrainingScheduleViewModel
{
    IReadOnlyList<TrainingScheduleRow> Rows { get; }
    IReadOnlyDictionary<string, string> CategoryColors { get; }
    TimeOnly TimelineStart { get; }
    TimeOnly TimelineEnd { get; }
    bool HasItems { get; }
    bool AllowEditing { get; }
}
