using System.Globalization;
using SportSys.Contract.Models;
using SportSys.Razor.Infrastructure;

namespace SportSys.Razor.Models;

public sealed record RequirementCoachGroup(int RoleId, string RoleName, IReadOnlyList<string> CoachDisplayTexts);

public static class RequirementCoachGroups
{
    private static readonly CultureInfo Czech = CultureInfo.GetCultureInfo("cs-CZ");

    public static IReadOnlyList<RequirementCoachGroup> Group(
        IEnumerable<RequirementCoachListItem> assignments,
        bool includeHours = false)
        => assignments
            .GroupBy(assignment => (assignment.CoachRoleId, assignment.CoachRoleOrder))
            .OrderBy(group => group.Key.CoachRoleOrder)
            .ThenBy(group => group.Key.CoachRoleId)
            .Select(group =>
            {
                var displayTexts = group
                    .OrderBy(assignment => assignment.LastName, StringComparer.Create(Czech, ignoreCase: true))
                    .Select(assignment => includeHours
                        ? $"{assignment.LastName} ({CzechNumberFormat.FormatDecimal(assignment.Hours, "0.##")})"
                        : assignment.LastName);

                return new RequirementCoachGroup(
                    group.Key.CoachRoleId,
                    group.First().CoachRoleName,
                    (includeHours ? displayTexts : displayTexts.Distinct()).ToList());
            })
            .ToList();
}
