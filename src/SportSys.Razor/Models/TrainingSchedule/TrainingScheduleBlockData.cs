using SportSys.Contract.Models;

namespace SportSys.Razor.Models.TrainingSchedule;

public sealed class TrainingScheduleCategorySegment
{
    public required string CategoryName { get; init; }
    public string? StateIcon { get; init; }
}

public sealed class TrainingScheduleBlockData
{
    public required IReadOnlyList<ITrainingScheduleItem> Items { get; init; }
    public required string Title { get; init; }
    public required IReadOnlyList<string> TrainingTypeNames { get; init; }
    public required IReadOnlyList<string> Locations { get; init; }
    public required IReadOnlyList<SimpleCoachDto> Coaches { get; init; }
    public required IReadOnlyList<string> TrainingTypeLocationSummaries { get; init; }
    public required IReadOnlyList<TrainingScheduleCategorySegment> CategorySegments { get; init; }
    public TimeOnly TimeFrom { get; init; }
    public TimeOnly TimeTo { get; init; }
    public int SeasonCategoryOrder { get; init; }
    public int MinimumItemId { get; init; }
    public bool IsUniformState { get; init; }
    public bool HasMixedState { get; init; }
    public string? UniformStateCssClass { get; init; }
    public string? UniformStateIcon { get; init; }
    public string? UniformStateName { get; init; }
    public bool IsDryTraining { get; init; }

    public string TrainingTypeSummary => string.Join(", ", TrainingTypeNames);
    public string LocationSummary => string.Join(", ", Locations);
    public string CoachSummary => Coaches.Count == 0
        ? "-"
        : string.Join(", ", Coaches.Select(coach => coach.FullName));
    public string CoachSurnameSummary => Coaches.Count == 0
        ? "-"
        : string.Join(", ", Coaches.Select(coach => coach.LastName));
    public string TrainingTypeLocationSummary => string.Join(", ", TrainingTypeLocationSummaries);
}

public static class TrainingScheduleBlockFactory
{
    public static IReadOnlyList<TrainingScheduleBlockData> CreateBlocks(
        IReadOnlyList<ITrainingScheduleItem> items)
    {
        var blocks = new List<TrainingScheduleBlockData>();

        blocks.AddRange(items
            .Where(item => GetVisualizationGroupId(item) is null)
            .Select(item => CreateBlock([item])));

        blocks.AddRange(items
            .Where(item => GetVisualizationGroupId(item) is not null)
            .GroupBy(item => GetVisualizationGroupId(item)!.Value)
            .Select(CreateBlock));

        return blocks
            .OrderBy(block => block.TimeFrom)
            .ThenBy(block => block.TimeTo)
            .ThenBy(block => block.SeasonCategoryOrder)
            .ThenBy(block => block.MinimumItemId)
            .ToList();
    }

    private static Guid? GetVisualizationGroupId(ITrainingScheduleItem item)
        => item.VisualizationGroupId ?? item.GroupId;

    private static TrainingScheduleBlockData CreateBlock(
        IEnumerable<ITrainingScheduleItem> sourceItems)
    {
        var items = sourceItems
            .OrderBy(item => item.SeasonCategoryOrder)
            .ThenBy(item => item.SeasonCategoryName, StringComparer.Ordinal)
            .ThenBy(item => item.Id)
            .ToList();

        var primaryItem = items[0];

        var stateIds = items.Select(item => item.TrainingStateId).ToList();
        var hasAnyState = stateIds.Any(id => id.HasValue);
        var isUniformState = hasAnyState
            && stateIds.All(id => id.HasValue)
            && stateIds.Distinct().Count() == 1;
        var hasMixedState = hasAnyState && !isUniformState;

        var dryFlags = items.Select(item => item.IsDryTraining).Distinct().ToList();
        var isUniformTrainingType = dryFlags.Count == 1;
        var isDryTraining = isUniformTrainingType && dryFlags[0];

        string? uniformStateCssClass = null;
        string? uniformStateIcon = null;
        string? uniformStateName = null;
        if (isUniformState)
        {
            var visual = TrainingStateVisual.Get(stateIds[0]);
            uniformStateCssClass = visual?.CssClass;
            uniformStateIcon = visual?.Icon;
            uniformStateName = primaryItem.TrainingStateName;
        }

        return new TrainingScheduleBlockData
        {
            Items = items,
            Title = string.Join(" + ", items.Select(item => item.SeasonCategoryName)),
            TrainingTypeNames = DistinctOrdered(items.Select(item => item.TrainingTypeName)),
            Locations = DistinctOrdered(items
                .Select(item => item.LocationName)
                .Where(location => !string.IsNullOrWhiteSpace(location))),
            Coaches = items
                .SelectMany(item => item.Coaches)
                .DistinctBy(coach => coach.FullName, StringComparer.Ordinal)
                .OrderBy(coach => coach.FullName, StringComparer.Ordinal)
                .ToList(),
            TrainingTypeLocationSummaries = DistinctOrdered(items.Select(item =>
                string.IsNullOrWhiteSpace(item.LocationName)
                    ? item.TrainingTypeName
                    : $"{item.TrainingTypeName} - {item.LocationName}")),
            CategorySegments = items
                .Select(item => new TrainingScheduleCategorySegment
                {
                    CategoryName = item.SeasonCategoryName,
                    StateIcon = TrainingStateVisual.Get(item.TrainingStateId)?.Icon,
                })
                .ToList(),
            IsUniformState = isUniformState || !hasAnyState,
            HasMixedState = hasMixedState,
            UniformStateCssClass = uniformStateCssClass,
            UniformStateIcon = uniformStateIcon,
            UniformStateName = uniformStateName,
            IsDryTraining = isDryTraining,
            TimeFrom = items.Min(item => item.TimeFrom),
            TimeTo = items.Max(item => item.TimeTo),
            SeasonCategoryOrder = primaryItem.SeasonCategoryOrder,
            MinimumItemId = items.Min(item => item.Id),
        };
    }

    private static IReadOnlyList<string> DistinctOrdered(IEnumerable<string> values)
        => values
            .Distinct(StringComparer.CurrentCulture)
            .OrderBy(value => value, StringComparer.CurrentCulture)
            .ToList();
}
