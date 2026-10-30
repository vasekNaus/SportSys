using System.Globalization;
using SportSys.Contract.Models;

namespace SportSys.Razor.Models.TrainingSchedule;

public sealed class TrainingPlanCoachDisplayItem
{
    public required TrainingPlanCoachDto Coach { get; init; }
    public bool IsDuplicate { get; init; }
    public string DisplayText => $"{Coach.LastName} ({Coach.CoachRoleCode})";
}

public static class TrainingPlanCoachPresentation
{
    private static readonly StringComparer CzechComparer = StringComparer.Create(
        CultureInfo.GetCultureInfo("cs-CZ"),
        ignoreCase: true);

    public static IReadOnlyList<TrainingSchedulePlanCoachAssignment> Order(
        IEnumerable<TrainingSchedulePlanCoachAssignment> assignments)
        => assignments
            .OrderBy(assignment => assignment.SeasonCategoryOrder)
            .ThenBy(assignment => assignment.Coach.CoachRoleOrder)
            .ThenBy(assignment => assignment.Coach.LastName, CzechComparer)
            .ThenBy(assignment => assignment.Coach.CoachId)
            .ThenBy(assignment => assignment.Coach.CoachRoleId)
            .ThenBy(assignment => assignment.TrainingPlanId)
            .ToList();

    public static string Format(IEnumerable<TrainingPlanCoachDto> coaches)
        => Format(Order(coaches.Select(coach =>
            new TrainingSchedulePlanCoachAssignment
            {
                Coach = coach,
            })));

    public static string Format(
        IEnumerable<TrainingSchedulePlanCoachAssignment> assignments)
    {
        var ordered = Order(assignments);
        return ordered.Count == 0
            ? "-"
            : string.Join(", ", ordered.Select(assignment =>
                $"{assignment.Coach.LastName} ({assignment.Coach.CoachRoleCode})"));
    }

    public static IReadOnlyList<TrainingPlanCoachDisplayItem> GetDisplayItems(
        IEnumerable<TrainingSchedulePlanCoachAssignment> assignments)
    {
        var ordered = Order(assignments);
        var duplicateCoachIds = ordered
            .GroupBy(assignment => assignment.Coach.CoachId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        return ordered
            .Select(assignment => new TrainingPlanCoachDisplayItem
            {
                Coach = assignment.Coach,
                IsDuplicate = duplicateCoachIds.Contains(assignment.Coach.CoachId),
            })
            .ToList();
    }
}
