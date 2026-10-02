namespace SportSys.Razor.Models.TrainingSchedule;

public class TrainingScheduleComponentModel
{
    private const double MinimumBlockWidthPercent = 0.5;
    private const double PointEventWidthPercent = 3;
    private readonly double _totalTimelineMinutes;

    private TrainingScheduleComponentModel(ITrainingScheduleViewModel source)
    {
        CategoryColors = source.CategoryColors;
        TimelineStart = source.TimelineStart;
        TimelineEnd = source.TimelineEnd;
        AllowEditing = source.AllowEditing;
        _totalTimelineMinutes =
            (TimelineEnd.ToTimeSpan() - TimelineStart.ToTimeSpan()).TotalMinutes;

        Markers = CreateMarkers();
        Rows = source.Rows
            .Select(row => new TrainingScheduleComponentRow
            {
                PrimaryLabel = row.PrimaryLabel,
                SecondaryLabel = row.SecondaryLabel,
                Parity = row.Parity,
                IsWeekend = row.IsWeekend,
                Lanes = CreateLanes(row.Items),
            })
            .ToList();
        LegendItems = Rows
            .SelectMany(row => row.Lanes)
            .SelectMany(lane => lane)
            .GroupBy(block => block.Title, StringComparer.Ordinal)
            .Select(group => group
                .OrderBy(block => block.SeasonCategoryOrder)
                .ThenBy(block => block.MinimumItemId)
                .First())
            .OrderBy(block => block.SeasonCategoryOrder)
            .ThenBy(block => block.Title, StringComparer.CurrentCulture)
            .Select(block => new TrainingScheduleLegendItem
            {
                Label = block.Title,
                Color = block.Color,
            })
            .ToList();
    }

    public IReadOnlyDictionary<string, string> CategoryColors { get; }
    public TimeOnly TimelineStart { get; }
    public TimeOnly TimelineEnd { get; }
    public bool AllowEditing { get; }
    public IReadOnlyList<TrainingScheduleMarker> Markers { get; }
    public IReadOnlyList<TrainingScheduleComponentRow> Rows { get; }
    public IReadOnlyList<TrainingScheduleLegendItem> LegendItems { get; }

    public static TrainingScheduleComponentModel Create(ITrainingScheduleViewModel source)
        => new(source);

    private IReadOnlyList<TrainingScheduleMarker> CreateMarkers()
    {
        var markers = new List<TrainingScheduleMarker>();
        var startHour = TimelineStart.Hour;
        var endMinutes = TimelineEnd.ToTimeSpan().TotalMinutes;

        for (var hour = startHour; hour * 60 <= endMinutes; hour += 2)
        {
            var time = new TimeOnly(hour, 0);
            markers.Add(new TrainingScheduleMarker
            {
                Label = $"{hour}:00",
                Left = GetLeft(time),
            });
        }

        return markers;
    }

    private IReadOnlyList<IReadOnlyList<TrainingScheduleBlock>> CreateLanes(
        IReadOnlyList<ScheduleEventModel> items)
    {
        var lanes = new List<List<TrainingScheduleBlock>>();

        var blocks = items
            .OrderBy(item => item.TimeFrom)
            .ThenBy(item => item.TimeTo)
            .ThenBy(item => item.SeasonCategoryOrder)
            .ThenBy(item => item.EventType)
            .ThenBy(item => item.SourceId)
            .Select(CreateBlock);

        foreach (var block in blocks)
        {
            var lane = lanes.FirstOrDefault(existing =>
                existing.Count == 0 || CanFollow(existing[^1], block));

            if (lane is null)
            {
                lane = [];
                lanes.Add(lane);
            }

            lane.Add(block);
        }

        return lanes;
    }

    private static bool CanFollow(
        TrainingScheduleBlock previous,
        TrainingScheduleBlock current)
    {
        if (previous.TimeTo < current.TimeFrom)
            return true;

        if (previous.TimeTo > current.TimeFrom)
            return false;

        var previousIsPoint = previous.TimeFrom == previous.TimeTo;
        var currentIsPoint = current.TimeFrom == current.TimeTo;
        return !previousIsPoint && !currentIsPoint;
    }

    private TrainingScheduleBlock CreateBlock(ScheduleEventModel item)
    {
        return new TrainingScheduleBlock
        {
            EventType = item.EventType,
            Title = item.TitleLine,
            DetailLine1 = item.DetailLine1,
            DetailLine2 = item.DetailLine2,
            TimeFrom = item.TimeFrom,
            TimeTo = item.TimeTo,
            SeasonCategoryOrder = item.SeasonCategoryOrder,
            MinimumItemId = item.SourceId,
            EditItemId = item.EditItemId,
            EditPage = item.EditPage,
            Left = GetLeft(item.TimeFrom),
            Width = GetWidth(item.TimeFrom, item.TimeTo),
            IsDryTraining = item.IsDryTraining,
            Color = CategoryColors.TryGetValue(item.ColorKey, out var color)
                ? color
                : "var(--color-text-muted)",
            Tooltip = item.Tooltip,
            StateIcon = item.StateIcon,
            StateTooltip = item.StateTooltip,
        };
    }

    private double GetLeft(TimeOnly time)
    {
        var offset = (time.ToTimeSpan() - TimelineStart.ToTimeSpan()).TotalMinutes;
        return Math.Clamp(offset / _totalTimelineMinutes * 100, 0, 100);
    }

    private double GetWidth(TimeOnly timeFrom, TimeOnly timeTo)
    {
        var duration = (timeTo.ToTimeSpan() - timeFrom.ToTimeSpan()).TotalMinutes;
        var minimumWidth = duration == 0
            ? PointEventWidthPercent
            : MinimumBlockWidthPercent;
        return Math.Clamp(duration / _totalTimelineMinutes * 100, minimumWidth, 100);
    }

}

public class TrainingScheduleComponentRow
{
    public required string PrimaryLabel { get; init; }
    public string? SecondaryLabel { get; init; }
    public required TrainingScheduleRowParity Parity { get; init; }
    public bool IsWeekend { get; init; }
    public IReadOnlyList<IReadOnlyList<TrainingScheduleBlock>> Lanes { get; init; } = [];
}

public class TrainingScheduleBlock
{
    public required ScheduleEventType EventType { get; init; }
    public required string Title { get; init; }
    public required string DetailLine1 { get; init; }
    public required string DetailLine2 { get; init; }
    public TimeOnly TimeFrom { get; init; }
    public TimeOnly TimeTo { get; init; }
    public int SeasonCategoryOrder { get; init; }
    public int MinimumItemId { get; init; }
    public int? EditItemId { get; init; }
    public string? EditPage { get; init; }
    public required string Color { get; init; }
    public required string Tooltip { get; init; }
    public double Left { get; init; }
    public double Width { get; init; }
    public bool IsDryTraining { get; init; }
    public string? StateIcon { get; init; }
    public string? StateTooltip { get; init; }
}

public class TrainingScheduleMarker
{
    public required string Label { get; init; }
    public double Left { get; init; }
}

public class TrainingScheduleLegendItem
{
    public required string Label { get; init; }
    public required string Color { get; init; }
}
