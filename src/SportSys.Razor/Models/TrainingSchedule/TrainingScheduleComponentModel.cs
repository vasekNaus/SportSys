using SportSys.Contract.Models;
using System.Globalization;

namespace SportSys.Razor.Models.TrainingSchedule;

public class TrainingScheduleComponentModel
{
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
        IReadOnlyList<ITrainingScheduleItem> items)
    {
        var lanes = new List<List<TrainingScheduleBlock>>();

        var blocks = TrainingScheduleBlockFactory.CreateBlocks(items)
            .Select(CreateBlock);

        foreach (var block in blocks)
        {
            var lane = lanes.FirstOrDefault(existing =>
                existing.Count == 0 || existing[^1].TimeTo <= block.TimeFrom);

            if (lane is null)
            {
                lane = [];
                lanes.Add(lane);
            }

            lane.Add(block);
        }

        return lanes;
    }

    private TrainingScheduleBlock CreateBlock(TrainingScheduleBlockData block)
    {
        var primaryItem = block.Items[0];
        var editPage = AllowEditing
            ? GetEditPage(block.Items)
            : null;

        return new TrainingScheduleBlock
        {
            Items = block.Items,
            Title = block.Title,
            TrainingTypeSummary = block.TrainingTypeLocationSummary,
            CoachSummary = block.CoachSummary,
            TimeFrom = block.TimeFrom,
            TimeTo = block.TimeTo,
            SeasonCategoryOrder = block.SeasonCategoryOrder,
            MinimumItemId = block.MinimumItemId,
            EditItemId = editPage is null ? null : block.MinimumItemId,
            EditPage = editPage,
            Left = GetLeft(block.TimeFrom),
            Width = GetWidth(block.TimeFrom, block.TimeTo),
            Color = CategoryColors.TryGetValue(primaryItem.SeasonCategoryName, out var color)
                ? color
                : "var(--color-text-muted)",
            Tooltip = string.Join(" | ", block.Items.Select(CreateTooltip)),
        };
    }

    private static string? GetEditPage(IReadOnlyList<ITrainingScheduleItem> items)
    {
        if (items.All(item => item is TrainingScheduleItemDto))
            return "/Training/Schedule/Edit";

        if (items.All(item =>
                item is TrainingPlanScheduleItemDto &&
                item is not TrainingScheduleItemDto))
        {
            return "/Training/Plan/Edit";
        }

        return null;
    }

    private double GetLeft(TimeOnly time)
    {
        var offset = (time.ToTimeSpan() - TimelineStart.ToTimeSpan()).TotalMinutes;
        return Math.Clamp(offset / _totalTimelineMinutes * 100, 0, 100);
    }

    private double GetWidth(TimeOnly timeFrom, TimeOnly timeTo)
    {
        var duration = (timeTo.ToTimeSpan() - timeFrom.ToTimeSpan()).TotalMinutes;
        return Math.Clamp(duration / _totalTimelineMinutes * 100, 0.5, 100);
    }

    private static string CreateTooltip(ITrainingScheduleItem item)
    {
        var parts = new List<string>
        {
            item.SeasonCategoryName,
            item.TrainingTypeName,
            item.TrainingPhaseName,
            item.Location,
        };

        if (item is TrainingPlanScheduleItemDto plan &&
            item is not TrainingScheduleItemDto)
        {
            parts.Add(
                $"Platnost {plan.From.ToString("d. M. yyyy", CultureInfo.CurrentCulture)}–" +
                plan.To.ToString("d. M. yyyy", CultureInfo.CurrentCulture));
        }

        if (!string.IsNullOrWhiteSpace(item.Note))
            parts.Add(item.Note);

        if (item.CoachFullNames.Count > 0)
            parts.Add($"Trenéři: {string.Join(", ", item.CoachFullNames)}");

        return string.Join(" · ", parts);
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
    public required IReadOnlyList<ITrainingScheduleItem> Items { get; init; }
    public required string Title { get; init; }
    public required string TrainingTypeSummary { get; init; }
    public required string CoachSummary { get; init; }
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
