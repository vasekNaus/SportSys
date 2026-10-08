using SportSys.Contract.Models;
using System.Globalization;

namespace SportSys.Razor.Models.TrainingSchedule;

/// <summary>
/// Univerzální základ pro položku rozvrhu. Nese jen to, co potřebuje
/// vykreslovací komponenta (pozice v čase, barva, editace, tooltip).
/// Typově specifické detaily patří do konkrétních podtříd.
/// </summary>
public abstract class EventModel
{
    public int SourceId { get; init; }
    public int SeasonCategoryOrder { get; init; }
    public required string ColorKey { get; init; }
    public required string TitleLine { get; init; }
    public TimeOnly TimeFrom { get; init; }
    public TimeOnly TimeTo { get; init; }
    public required string Tooltip { get; init; }
    public string? EditPage { get; init; }
    public int? EditItemId { get; init; }
}

public sealed class MatchEventModel : EventModel
{
    public required string OpponentName { get; init; }
    public required string ResultText { get; init; }
    public bool IsHome { get; init; }
    public string? MatchStateIcon { get; init; }
    public string? MatchStateTooltip { get; init; }
}

/// <summary>
/// Společný základ pro tréninky a plány tréninků — obojí má trenéry,
/// lokaci a příznak suché přípravy, na rozdíl od zápasu.
/// </summary>
public abstract class TrainingLikeEventModel : EventModel
{
    public required string CoachSurnameSummary { get; init; }
    public required string LocationSummary { get; init; }
    public bool IsDryTraining { get; init; }
}

public sealed class TrainingEventModel : TrainingLikeEventModel
{
    public string? StateIcon { get; init; }
    public string? StateTooltip { get; init; }
}

public sealed class TrainingPlanEventModel : TrainingLikeEventModel
{
    public DateOnly ValidFrom { get; init; }
    public DateOnly ValidTo { get; init; }

    /// <summary>
    /// Sloučený a deduplikovaný text z <see cref="TrainingPlanScheduleItemDto.Title"/>
    /// všech položek bloku. Prázdný řetězec, pokud žádná položka Title nemá
    /// vyplněné.
    /// </summary>
    public required string PlanTitleSummary { get; init; }
}

public static class EventModelFactory
{
    public static IReadOnlyList<EventModel> CreateTrainings(
        IReadOnlyList<TrainingScheduleItemDto> trainings,
        bool allowEditing)
        => TrainingScheduleBlockFactory.CreateBlocks(
                trainings.Cast<ITrainingScheduleItem>().ToList())
            .Select(block => CreateTrainingBlock(block, allowEditing))
            .ToList();

    public static IReadOnlyList<EventModel> CreateTrainingPlans(
        IReadOnlyList<TrainingPlanScheduleItemDto> plans,
        bool allowEditing)
        => TrainingScheduleBlockFactory.CreateBlocks(
                plans.Cast<ITrainingScheduleItem>().ToList())
            .Select(block => CreateTrainingPlanBlock(block, allowEditing))
            .ToList();

    public static EventModel CreateMatch(MatchScheduleItemDto match)
    {
        var result = match.HomeGoals.HasValue && match.AwayGoals.HasValue
            ? $"{match.HomeGoals}:{match.AwayGoals}"
            : "-";
        var role = match.IsHome ? "domácí" : "venkovní";
        var tooltipParts = new List<string>
        {
            match.SeasonCategoryCode,
            $"{match.HomeTeamName} – {match.AwayTeamName}",
            $"{match.MatchTypeName}, {role}",
            match.LocationName,
        };

        if (match.MatchStateName is not null)
            tooltipParts.Add(match.MatchStateName);

        if (!string.IsNullOrWhiteSpace(match.Note))
            tooltipParts.Add(match.Note);

        var stateInfo = MatchStateVisual.Get(match.MatchStateId);

        return new MatchEventModel
        {
            SourceId = match.Id,
            SeasonCategoryOrder = match.SeasonCategoryOrder,
            ColorKey = match.SeasonCategoryCode,
            TitleLine = match.SeasonCategoryCode,
            OpponentName = match.OpponentName,
            ResultText = result,
            IsHome = match.IsHome,
            TimeFrom = match.TimeFrom,
            TimeTo = match.TimeTo,
            Tooltip = string.Join(" · ", tooltipParts),
            MatchStateIcon = stateInfo?.Icon,
            MatchStateTooltip = match.MatchStateName,
        };
    }

    private static EventModel CreateTrainingBlock(
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
                    $"{segment.StateIcon} {segment.CategoryCode}".Trim()))
            : null;

        return new TrainingEventModel
        {
            SourceId = block.MinimumItemId,
            SeasonCategoryOrder = block.SeasonCategoryOrder,
            ColorKey = block.Items[0].SeasonCategoryCode,
            TitleLine = block.Title,
            CoachSurnameSummary = block.CoachSurnameSummary,
            LocationSummary = block.LocationSummary,
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

    private static TrainingPlanEventModel CreateTrainingPlanBlock(
        TrainingScheduleBlockData block,
        bool allowEditing)
    {
        var planItems = block.Items.OfType<TrainingPlanScheduleItemDto>().ToList();

        var planTitleSummary = string.Join(
            ", ",
            planItems
                .Select(item => item.Title)
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Distinct(StringComparer.CurrentCulture)
                .OrderBy(title => title, StringComparer.CurrentCulture));

        return new TrainingPlanEventModel
        {
            SourceId = block.MinimumItemId,
            SeasonCategoryOrder = block.SeasonCategoryOrder,
            ColorKey = block.Items[0].SeasonCategoryCode,
            TitleLine = block.Title,
            CoachSurnameSummary = block.CoachSurnameSummary,
            LocationSummary = block.LocationSummary,
            TimeFrom = block.TimeFrom,
            TimeTo = block.TimeTo,
            IsDryTraining = block.IsDryTraining,
            PlanTitleSummary = planTitleSummary,
            ValidFrom = planItems.Count > 0 ? planItems.Min(item => item.From) : default,
            ValidTo = planItems.Count > 0 ? planItems.Max(item => item.To) : default,
            Tooltip = string.Join(" | ", block.Items.Select(CreateTooltip)),
            EditItemId = allowEditing ? block.MinimumItemId : null,
            EditPage = allowEditing ? "/Training/Plan/Edit" : null,
        };
    }

    private static string CreateTooltip(ITrainingScheduleItem item)
    {
        var parts = new List<string>
        {
            item.SeasonCategoryCode,
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
