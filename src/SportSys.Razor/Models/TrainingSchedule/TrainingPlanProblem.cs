using System.Globalization;
using SportSys.Contract.Models;

namespace SportSys.Razor.Models.TrainingSchedule;

public enum TrainingPlanProblemSeverity
{
    Error,
    Warning,
}

public enum TrainingPlanProblemType
{
    CoachConflict,
    CategoryConflict,
}

public sealed class TrainingPlanProblem
{
    public required TrainingPlanProblemSeverity Severity { get; init; }
    public required TrainingPlanProblemType Type { get; init; }
    public required string CategoryText { get; init; }
    public string? CoachName { get; init; }
    public required string TrainingTypeText { get; init; }
    public DayOfWeek Day { get; init; }
    public TimeOnly TimeFrom { get; init; }
    public TimeOnly TimeTo { get; init; }
    public required string FirstPlanTitle { get; init; }
    public required string SecondPlanTitle { get; init; }
    public required string Description { get; init; }
    public string CategorySortKey { get; init; } = string.Empty;
    public int FirstPlanId { get; init; }
    public int SecondPlanId { get; init; }
    public int? CoachId { get; init; }

    public string SeverityText => Severity switch
    {
        TrainingPlanProblemSeverity.Error => "Error",
        TrainingPlanProblemSeverity.Warning => "Warning",
        _ => throw new ArgumentOutOfRangeException(nameof(Severity)),
    };

    public string SeverityCssClass => Severity switch
    {
        TrainingPlanProblemSeverity.Error => "training-plan-problem__severity--error",
        TrainingPlanProblemSeverity.Warning => "training-plan-problem__severity--warning",
        _ => throw new ArgumentOutOfRangeException(nameof(Severity)),
    };

    public string SeverityIconClass => Severity switch
    {
        TrainingPlanProblemSeverity.Error => "fa-circle-exclamation",
        TrainingPlanProblemSeverity.Warning => "fa-triangle-exclamation",
        _ => throw new ArgumentOutOfRangeException(nameof(Severity)),
    };

    public string TypeText => Type switch
    {
        TrainingPlanProblemType.CoachConflict => "Konflikt trenéra",
        TrainingPlanProblemType.CategoryConflict => "Konflikt kategorie",
        _ => throw new ArgumentOutOfRangeException(nameof(Type)),
    };

    public string TimeText =>
        $"{WeekDayNames.GetFullName(Day)}, " +
        $"{TimeFrom.ToString("HH:mm", CultureInfo.InvariantCulture)}–" +
        TimeTo.ToString("HH:mm", CultureInfo.InvariantCulture);
}

public static class TrainingPlanProblemBuilder
{
    private static readonly StringComparer CzechComparer = StringComparer.Create(
        CultureInfo.GetCultureInfo("cs-CZ"),
        ignoreCase: true);

    public static IReadOnlyList<TrainingPlanProblem> Build(
        IReadOnlyList<TrainingPlanScheduleItemDto> plans)
    {
        var problems = new List<TrainingPlanProblem>();

        for (var firstIndex = 0; firstIndex < plans.Count; firstIndex++)
        {
            var firstPlan = plans[firstIndex];

            for (var secondIndex = firstIndex + 1; secondIndex < plans.Count; secondIndex++)
            {
                var secondPlan = plans[secondIndex];
                var (first, second) = firstPlan.Id <= secondPlan.Id
                    ? (firstPlan, secondPlan)
                    : (secondPlan, firstPlan);

                if (first.Id == second.Id || IsSamePersistedGroup(first, second))
                    continue;

                if (first.DayOfWeek != second.DayOfWeek ||
                    !TryGetTimeOverlap(first, second, out var timeFrom, out var timeTo) ||
                    !HasDateOverlapOnOccurrence(first, second))
                {
                    continue;
                }

                AddCoachConflicts(problems, first, second, timeFrom, timeTo);
                AddCategoryConflict(problems, first, second, timeFrom, timeTo);
            }
        }

        return problems
            .OrderBy(problem => problem.Severity)
            .ThenBy(problem => DayOrder(problem.Day))
            .ThenBy(problem => problem.TimeFrom)
            .ThenBy(problem => problem.CategorySortKey, CzechComparer)
            .ThenBy(problem => problem.FirstPlanId)
            .ThenBy(problem => problem.SecondPlanId)
            .ThenBy(problem => problem.Type)
            .ThenBy(problem => problem.CoachId)
            .ToList();
    }

    private static void AddCoachConflicts(
        ICollection<TrainingPlanProblem> problems,
        TrainingPlanScheduleItemDto first,
        TrainingPlanScheduleItemDto second,
        TimeOnly timeFrom,
        TimeOnly timeTo)
    {
        var secondCoaches = second.CoachAssignments
            .GroupBy(coach => coach.CoachId)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var firstCoach in first.CoachAssignments
            .GroupBy(coach => coach.CoachId)
            .Select(group => group.First()))
        {
            if (!secondCoaches.ContainsKey(firstCoach.CoachId))
                continue;

            problems.Add(CreateProblem(
                TrainingPlanProblemSeverity.Error,
                TrainingPlanProblemType.CoachConflict,
                first,
                second,
                timeFrom,
                timeTo,
                coachName: firstCoach.FullName,
                coachId: firstCoach.CoachId,
                description: "Trenér je přiřazen k více souběžným tréninkům."));
        }
    }

    private static void AddCategoryConflict(
        ICollection<TrainingPlanProblem> problems,
        TrainingPlanScheduleItemDto first,
        TrainingPlanScheduleItemDto second,
        TimeOnly timeFrom,
        TimeOnly timeTo)
    {
        if (!string.Equals(
                first.SeasonCategoryCode,
                second.SeasonCategoryCode,
                StringComparison.Ordinal))
        {
            return;
        }

        var severity = first.IsIceTraining || second.IsIceTraining
            ? TrainingPlanProblemSeverity.Error
            : TrainingPlanProblemSeverity.Warning;
        var description = severity == TrainingPlanProblemSeverity.Error
            ? "Kategorie je současně naplánována na překrývajících se trénincích, z nichž alespoň jeden je na ledě."
            : "Kategorie je současně naplánována na více neledových aktivitách.";

        problems.Add(CreateProblem(
            severity,
            TrainingPlanProblemType.CategoryConflict,
            first,
            second,
            timeFrom,
            timeTo,
            coachName: null,
            coachId: null,
            description));
    }

    private static TrainingPlanProblem CreateProblem(
        TrainingPlanProblemSeverity severity,
        TrainingPlanProblemType type,
        TrainingPlanScheduleItemDto first,
        TrainingPlanScheduleItemDto second,
        TimeOnly timeFrom,
        TimeOnly timeTo,
        string? coachName,
        int? coachId,
        string description)
    {
        var categories = new[] { first, second }
            .GroupBy(plan => plan.SeasonCategoryCode, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(plan => plan.SeasonCategoryCode, CzechComparer)
            .ToList();
        var trainingTypes = new[] { first, second }
            .Select(plan => plan.TrainingTypeName)
            .Distinct(StringComparer.CurrentCulture)
            .ToList();

        return new TrainingPlanProblem
        {
            Severity = severity,
            Type = type,
            CategoryText = string.Join(" / ", categories.Select(GetCategoryName)),
            CoachName = coachName,
            TrainingTypeText = string.Join(" / ", trainingTypes),
            Day = first.DayOfWeek,
            TimeFrom = timeFrom,
            TimeTo = timeTo,
            FirstPlanTitle = GetPlanTitle(first),
            SecondPlanTitle = GetPlanTitle(second),
            Description = description,
            CategorySortKey = string.Join("/", categories.Select(plan => plan.SeasonCategoryCode)),
            FirstPlanId = first.Id,
            SecondPlanId = second.Id,
            CoachId = coachId,
        };
    }

    private static bool IsSamePersistedGroup(
        TrainingPlanScheduleItemDto first,
        TrainingPlanScheduleItemDto second)
        => first.GroupId.HasValue && first.GroupId == second.GroupId;

    private static bool TryGetTimeOverlap(
        TrainingPlanScheduleItemDto first,
        TrainingPlanScheduleItemDto second,
        out TimeOnly timeFrom,
        out TimeOnly timeTo)
    {
        timeFrom = first.TimeFrom > second.TimeFrom ? first.TimeFrom : second.TimeFrom;
        timeTo = first.TimeTo < second.TimeTo ? first.TimeTo : second.TimeTo;
        return timeFrom < timeTo;
    }

    private static bool HasDateOverlapOnOccurrence(
        TrainingPlanScheduleItemDto first,
        TrainingPlanScheduleItemDto second)
    {
        var from = first.From > second.From ? first.From : second.From;
        var to = first.To < second.To ? first.To : second.To;
        if (from > to)
            return false;

        var daysUntilOccurrence =
            ((int)first.DayOfWeek - (int)from.DayOfWeek + 7) % 7;
        return daysUntilOccurrence <= to.DayNumber - from.DayNumber;
    }

    private static int DayOrder(DayOfWeek day)
        => ((int)day + 6) % 7;

    private static string GetCategoryName(TrainingPlanScheduleItemDto plan)
    {
        if (string.IsNullOrWhiteSpace(plan.SeasonCategoryName) ||
            string.Equals(
                plan.SeasonCategoryName,
                plan.SeasonCategoryCode,
                StringComparison.Ordinal))
        {
            return plan.SeasonCategoryCode;
        }

        return $"{plan.SeasonCategoryName} ({plan.SeasonCategoryCode})";
    }

    private static string GetPlanTitle(TrainingPlanScheduleItemDto plan)
    {
        if (!string.IsNullOrWhiteSpace(plan.Title))
            return plan.Title;

        return string.Join(
            " · ",
            new[] { plan.SeasonCategoryCode, plan.TrainingTypeName, plan.LocationName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
