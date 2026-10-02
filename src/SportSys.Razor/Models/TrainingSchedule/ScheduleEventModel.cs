using SportSys.Contract.Models;
using System.Globalization;

namespace SportSys.Razor.Models.TrainingSchedule;

public enum ScheduleEventType
{
    Training,
    Match,
    TrainingPlan,
}

public sealed class ScheduleEventModel
{
    public required ScheduleEventType EventType { get; init; }
    public int SourceId { get; init; }
    public int SeasonCategoryOrder { get; init; }
    public required string ColorKey { get; init; }
    public required string TitleLine { get; init; }
    public required string DetailLine1 { get; init; }
    public required string DetailLine2 { get; init; }
    public TimeOnly TimeFrom { get; init; }
    public TimeOnly TimeTo { get; init; }
    public bool IsDryTraining { get; init; }
    public required string Tooltip { get; init; }
    public string? EditPage { get; init; }
    public int? EditItemId { get; init; }
    public string? StateIcon { get; init; }
    public string? StateTooltip { get; init; }
}

public static class ScheduleEventModelFactory
{
    public static IReadOnlyList<ScheduleEventModel> CreateTrainings(
        IReadOnlyList<TrainingScheduleItemDto> trainings,
        bool allowEditing)
        => TrainingScheduleBlockFactory.CreateBlocks(
                trainings.Cast<ITrainingScheduleItem>().ToList())
            .Select(block => CreateTrainingBlock(block, allowEditing))
            .ToList();

    public static IReadOnlyList<ScheduleEventModel> CreateTrainingPlans(
        IReadOnlyList<TrainingPlanScheduleItemDto> plans,
        bool allowEditing)
        => TrainingScheduleBlockFactory.CreateBlocks(
                plans.Cast<ITrainingScheduleItem>().ToList())
            .Select(block => CreateTrainingPlanBlock(block, allowEditing))
            .ToList();

    public static ScheduleEventModel CreateMatch(MatchScheduleItemDto match)
    {
        var result = match.HomeGoals.HasValue && match.AwayGoals.HasValue
            ? $"{match.HomeGoals}:{match.AwayGoals}"
            : "-";
        var role = match.IsHome ? "domácí" : "venkovní";
        var tooltipParts = new List<string>
        {
            match.SeasonCategoryName,
            $"{match.HomeTeamName} – {match.AwayTeamName}",
            $"{match.MatchTypeName}, {role}",
            match.LocationName,
        };

        if (match.MatchStateName is not null)
            tooltipParts.Add(match.MatchStateName);

        if (!string.IsNullOrWhiteSpace(match.Note))
            tooltipParts.Add(match.Note);

        var stateInfo = MatchStateVisual.Get(match.MatchStateId);

        return new ScheduleEventModel
        {
            EventType = ScheduleEventType.Match,
            SourceId = match.Id,
            SeasonCategoryOrder = match.SeasonCategoryOrder,
            ColorKey = match.SeasonCategoryName,
            TitleLine = match.SeasonCategoryName,
            DetailLine1 = match.OpponentName,
            DetailLine2 = result,
            TimeFrom = match.TimeFrom,
            TimeTo = match.TimeTo,
            Tooltip = string.Join(" · ", tooltipParts),
            StateIcon = stateInfo?.Icon,
            StateTooltip = match.MatchStateName,
        };
    }

    private static ScheduleEventModel CreateTrainingBlock(
        TrainingScheduleBlockData block,
        bool allowEditing)
    {
        var stateIcon = allowEditing
            ? block.HasMixedState
                ? TrainingStateVisual.UnknownIcon
                : block.UniformStateIcon
            : null;
        var stateTooltip = allowEditing && block.HasMixedState
            ? string.Join(
                "\n",
                block.CategorySegments.Select(segment =>
                    $"{segment.StateIcon} {segment.CategoryName}".Trim()))
            : null;

        return new ScheduleEventModel
        {
            EventType = ScheduleEventType.Training,
            SourceId = block.MinimumItemId,
            SeasonCategoryOrder = block.SeasonCategoryOrder,
            ColorKey = block.Items[0].SeasonCategoryName,
            TitleLine = block.Title,
            DetailLine1 = block.CoachSurnameSummary,
            DetailLine2 = block.LocationSummary,
            TimeFrom = block.TimeFrom,
            TimeTo = block.TimeTo,
            IsDryTraining = block.IsDryTraining,
            Tooltip = string.Join(" | ", block.Items.Select(CreateTooltip)),
            EditItemId = allowEditing ? block.MinimumItemId : null,
            EditPage = allowEditing ? "/Training/Schedule/Edit" : null,
            StateIcon = stateIcon,
            StateTooltip = stateTooltip,
        };
    }

    private static ScheduleEventModel CreateTrainingPlanBlock(
        TrainingScheduleBlockData block,
        bool allowEditing)
        => new()
        {
            EventType = ScheduleEventType.TrainingPlan,
            SourceId = block.MinimumItemId,
            SeasonCategoryOrder = block.SeasonCategoryOrder,
            ColorKey = block.Items[0].SeasonCategoryName,
            TitleLine = block.Title,
            DetailLine1 = block.CoachSurnameSummary,
            DetailLine2 = block.LocationSummary,
            TimeFrom = block.TimeFrom,
            TimeTo = block.TimeTo,
            IsDryTraining = block.IsDryTraining,
            Tooltip = string.Join(" | ", block.Items.Select(CreateTooltip)),
            EditItemId = allowEditing ? block.MinimumItemId : null,
            EditPage = allowEditing ? "/Training/Plan/Edit" : null,
        };

    private static string CreateTooltip(ITrainingScheduleItem item)
    {
        var parts = new List<string>
        {
            item.SeasonCategoryName,
        };

        if (item.TrainingStateName is not null)
            parts.Add(item.TrainingStateName);

        parts.Add(item.TrainingTypeName);
        parts.Add(item.TrainingPhaseName);
        parts.Add(item.LocationName);

        if (item is TrainingPlanScheduleItemDto plan)
        {
            parts.Add(
                $"Platnost {plan.From.ToString("d. M. yyyy", CultureInfo.CurrentCulture)}–" +
                plan.To.ToString("d. M. yyyy", CultureInfo.CurrentCulture));
        }

        if (!string.IsNullOrWhiteSpace(item.Note))
            parts.Add(item.Note);

        if (item.Coaches.Count > 0)
            parts.Add($"Trenéři: {string.Join(", ", item.Coaches.Select(coach => coach.FullName))}");

        return string.Join(" · ", parts);
    }
}
