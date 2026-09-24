using SportSys.Contract.Models;

namespace SportSys.Razor.Models.TrainingSchedule;

public class TrainingScheduleViewModel : ITrainingScheduleViewModel
{
    private const int DefaultStartMinutes = 6 * 60;
    private const int DefaultEndMinutes = 22 * 60;
    private const int PaddingMinutes = 60;
    private static readonly string[] CategoryColorPalette =
    [
        "var(--color-chart-1)",
        "var(--color-chart-2)",
        "var(--color-chart-3)",
        "var(--color-chart-4)",
        "var(--color-chart-5)",
        "var(--color-chart-6)",
        "var(--color-chart-7)",
        "var(--color-chart-8)",
    ];

    public TrainingScheduleViewModel(
        IReadOnlyList<TrainingScheduleRow> rows,
        IReadOnlyList<string> categoryNames,
        bool allowEditing = true)
    {
        Rows = rows;
        CategoryColors = CreateCategoryColors(categoryNames);
        AllowEditing = allowEditing;

        var items = rows.SelectMany(r => r.Items).ToList();
        HasItems = items.Count > 0;

        if (!HasItems)
        {
            TimelineStart = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(DefaultStartMinutes));
            TimelineEnd = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(DefaultEndMinutes));
            return;
        }

        var firstStart = items.Min(i => i.TimeFrom.ToTimeSpan().TotalMinutes);
        var lastEnd = items.Max(i => i.TimeTo.ToTimeSpan().TotalMinutes);
        var startMinutes = Math.Max(0, Math.Floor(firstStart / 60) * 60 - PaddingMinutes);
        var endMinutes = Math.Min(
            (24 * 60) - 1,
            Math.Ceiling(lastEnd / 60) * 60 + PaddingMinutes);

        TimelineStart = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(startMinutes));
        TimelineEnd = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(endMinutes));
    }

    public IReadOnlyList<TrainingScheduleRow> Rows { get; }
    public IReadOnlyDictionary<string, string> CategoryColors { get; }
    public TimeOnly TimelineStart { get; }
    public TimeOnly TimelineEnd { get; }
    public bool HasItems { get; }
    public bool AllowEditing { get; }

    private static IReadOnlyDictionary<string, string> CreateCategoryColors(
        IReadOnlyList<string> categoryNames)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < categoryNames.Count; i++)
            result.TryAdd(
                categoryNames[i],
                CategoryColorPalette[i % CategoryColorPalette.Length]);

        return result;
    }
}
