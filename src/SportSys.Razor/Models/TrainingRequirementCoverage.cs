using System.Globalization;
using SportSys.Contract.Models;

namespace SportSys.Razor.Models;

public enum TrainingRequirementCoverageStatus
{
    Match,
    OverPlan,
    UnderPlan,
}

public sealed class TrainingRequirementCoverageRow
{
    public int SeasonId { get; init; }
    public required string SeasonName { get; init; }
    public required string SeasonCategoryCode { get; init; }
    public required string SeasonCategoryName { get; init; }
    public int SeasonCategoryOrder { get; init; }
    public int TrainingTypeId { get; init; }
    public required string TrainingTypeName { get; init; }
    public int CoachId { get; init; }
    public required string CoachName { get; init; }
    public int CoachRoleId { get; init; }
    public required string CoachRoleName { get; init; }
    public int CoachRoleOrder { get; init; }
    public decimal RequiredHours { get; init; }
    public decimal PlannedHours { get; init; }
    public TrainingRequirementCoverageStatus Status { get; init; }

    public string StatusText => Status switch
    {
        TrainingRequirementCoverageStatus.Match => "Shoda",
        TrainingRequirementCoverageStatus.OverPlan => "Nad plán",
        TrainingRequirementCoverageStatus.UnderPlan => "Nesplňuje plán",
        _ => throw new ArgumentOutOfRangeException(nameof(Status), Status, null),
    };

    public string StatusTooltip => Status switch
    {
        TrainingRequirementCoverageStatus.Match =>
            "Požadované hodiny odpovídají plánu.",
        TrainingRequirementCoverageStatus.OverPlan =>
            "Plán obsahuje více hodin než požadavek.",
        TrainingRequirementCoverageStatus.UnderPlan =>
            "Plán neobsahuje dostatek hodin.",
        _ => throw new ArgumentOutOfRangeException(nameof(Status), Status, null),
    };

    public string StatusIconClass => Status switch
    {
        TrainingRequirementCoverageStatus.Match => "fa-equals",
        TrainingRequirementCoverageStatus.OverPlan => "fa-triangle-exclamation",
        TrainingRequirementCoverageStatus.UnderPlan => "fa-circle-xmark",
        _ => throw new ArgumentOutOfRangeException(nameof(Status), Status, null),
    };

    public string StatusCssClass => Status switch
    {
        TrainingRequirementCoverageStatus.Match => "coverage-status--match",
        TrainingRequirementCoverageStatus.OverPlan => "coverage-status--over",
        TrainingRequirementCoverageStatus.UnderPlan => "coverage-status--under",
        _ => throw new ArgumentOutOfRangeException(nameof(Status), Status, null),
    };
}

public static class TrainingRequirementCoverageBuilder
{
    private static readonly StringComparer CzechComparer = StringComparer.Create(
        CultureInfo.GetCultureInfo("cs-CZ"),
        ignoreCase: true);

    public static IReadOnlyList<TrainingRequirementCoverageRow> Build(
        IEnumerable<TrainingRequirementListItem> requirements,
        IEnumerable<TrainingPlanScheduleItemDto> plans,
        IReadOnlyCollection<int>? selectedCoachIds = null)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        ArgumentNullException.ThrowIfNull(plans);

        var selectedCoaches = selectedCoachIds is { Count: > 0 }
            ? selectedCoachIds.ToHashSet()
            : null;
        var requiredHours = new Dictionary<CoverageKey, CoverageAggregate>();
        var plannedMinutes = new Dictionary<CoverageKey, CoverageAggregate>();

        foreach (var requirement in requirements)
        {
            foreach (var assignment in requirement.CoachAssignments)
            {
                if (selectedCoaches is not null && !selectedCoaches.Contains(assignment.Id))
                    continue;

                var key = new CoverageKey(
                    requirement.SeasonId,
                    requirement.SeasonCategoryCode,
                    requirement.TrainingTypeId,
                    assignment.Id,
                    assignment.CoachRoleId);
                Add(
                    requiredHours,
                    key,
                    requirement.SeasonName,
                    requirement.SeasonCategoryName,
                    requirement.SeasonCategoryOrder,
                    requirement.TrainingTypeName,
                    GetCoachName(assignment.DisplayName, assignment.PersonalNumber, assignment.Id),
                    assignment.CoachRoleName,
                    assignment.CoachRoleOrder,
                    assignment.Hours);
            }
        }

        foreach (var plan in plans)
        {
            var assignments = plan.CoachAssignments
                .Where(assignment =>
                    selectedCoaches is null || selectedCoaches.Contains(assignment.CoachId))
                .DistinctBy(assignment => (assignment.CoachId, assignment.CoachRoleId))
                .ToList();
            if (assignments.Count == 0)
                continue;

            if (!plan.DurationMinutes.HasValue)
            {
                throw new InvalidOperationException(
                    $"TrainingPlan {plan.Id} nemá vypočtenou délku.");
            }

            foreach (var assignment in assignments)
            {
                var key = new CoverageKey(
                    plan.SeasonId,
                    plan.SeasonCategoryCode,
                    plan.TrainingTypeId,
                    assignment.CoachId,
                    assignment.CoachRoleId);
                Add(
                    plannedMinutes,
                    key,
                    plan.SeasonName,
                    plan.SeasonCategoryName,
                    plan.SeasonCategoryOrder,
                    plan.TrainingTypeName,
                    GetCoachName(assignment.FullName, assignment.CoachId),
                    assignment.CoachRoleName,
                    assignment.CoachRoleOrder,
                    plan.DurationMinutes.Value);
            }
        }

        return requiredHours.Keys
            .Concat(plannedMinutes.Keys)
            .Distinct()
            .Select(key =>
            {
                var hasRequirement = requiredHours.TryGetValue(key, out var requirement);
                var hasPlan = plannedMinutes.TryGetValue(key, out var plan);
                var metadata = hasRequirement ? requirement! : plan!;
                var requested = hasRequirement ? requirement!.Value : 0m;
                var scheduled = hasPlan
                    ? decimal.Round(plan!.Value / 60m, 2, MidpointRounding.AwayFromZero)
                    : 0m;

                return new TrainingRequirementCoverageRow
                {
                    SeasonId = key.SeasonId,
                    SeasonName = metadata.SeasonName,
                    SeasonCategoryCode = key.SeasonCategoryCode,
                    SeasonCategoryName = metadata.SeasonCategoryName,
                    SeasonCategoryOrder = metadata.SeasonCategoryOrder,
                    TrainingTypeId = key.TrainingTypeId,
                    TrainingTypeName = metadata.TrainingTypeName,
                    CoachId = key.CoachId,
                    CoachName = metadata.CoachName,
                    CoachRoleId = key.CoachRoleId,
                    CoachRoleName = metadata.CoachRoleName,
                    CoachRoleOrder = metadata.CoachRoleOrder,
                    RequiredHours = requested,
                    PlannedHours = scheduled,
                    Status = requested == scheduled
                        ? TrainingRequirementCoverageStatus.Match
                        : scheduled > requested
                            ? TrainingRequirementCoverageStatus.OverPlan
                            : TrainingRequirementCoverageStatus.UnderPlan,
                };
            })
            .OrderBy(row => row.SeasonName, CzechComparer)
            .ThenBy(row => row.SeasonCategoryOrder)
            .ThenBy(row => row.TrainingTypeName, CzechComparer)
            .ThenBy(row => row.CoachName, CzechComparer)
            .ThenBy(row => row.CoachRoleOrder)
            .ThenBy(row => row.CoachName, StringComparer.Ordinal)
            .ThenBy(row => row.CoachRoleName, CzechComparer)
            .ThenBy(row => row.SeasonId)
            .ThenBy(row => row.SeasonCategoryCode, StringComparer.Ordinal)
            .ThenBy(row => row.TrainingTypeId)
            .ThenBy(row => row.CoachId)
            .ThenBy(row => row.CoachRoleId)
            .ToList();
    }

    public static string FormatHours(decimal hours)
    {
        var rounded = decimal.Round(hours, 2, MidpointRounding.AwayFromZero);
        return rounded == decimal.Truncate(rounded)
            ? rounded.ToString("0", CultureInfo.GetCultureInfo("cs-CZ"))
            : rounded.ToString("0.00", CultureInfo.GetCultureInfo("cs-CZ"));
    }

    private static void Add(
        IDictionary<CoverageKey, CoverageAggregate> totals,
        CoverageKey key,
        string seasonName,
        string categoryName,
        int categoryOrder,
        string trainingTypeName,
        string coachName,
        string roleName,
        int roleOrder,
        decimal value)
    {
        if (totals.TryGetValue(key, out var aggregate))
        {
            aggregate.Value += value;
            return;
        }

        totals.Add(
            key,
            new CoverageAggregate(
                seasonName,
                categoryName,
                categoryOrder,
                trainingTypeName,
                coachName,
                roleName,
                roleOrder,
                value));
    }

    private static string GetCoachName(string displayName, string personalNumber, int coachId)
        => string.IsNullOrWhiteSpace(displayName)
            ? string.IsNullOrWhiteSpace(personalNumber)
                ? coachId.ToString(CultureInfo.InvariantCulture)
                : personalNumber
            : displayName;

    private static string GetCoachName(string fullName, int coachId)
        => string.IsNullOrWhiteSpace(fullName)
            ? coachId.ToString(CultureInfo.InvariantCulture)
            : fullName;

    private readonly record struct CoverageKey(
        int SeasonId,
        string SeasonCategoryCode,
        int TrainingTypeId,
        int CoachId,
        int CoachRoleId);

    private sealed class CoverageAggregate(
        string seasonName,
        string categoryName,
        int categoryOrder,
        string trainingTypeName,
        string coachName,
        string roleName,
        int roleOrder,
        decimal value)
    {
        public string SeasonName { get; } = seasonName;
        public string SeasonCategoryName { get; } = categoryName;
        public int SeasonCategoryOrder { get; } = categoryOrder;
        public string TrainingTypeName { get; } = trainingTypeName;
        public string CoachName { get; } = coachName;
        public string CoachRoleName { get; } = roleName;
        public int CoachRoleOrder { get; } = roleOrder;
        public decimal Value { get; set; } = value;
    }
}
