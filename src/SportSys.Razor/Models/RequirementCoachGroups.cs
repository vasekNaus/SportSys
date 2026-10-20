using System.Globalization;
using SportSys.Contract.Models;

namespace SportSys.Razor.Models;

public sealed record RequirementCoachGroup(int RoleId, string RoleName, IReadOnlyList<string> LastNames);

public static class RequirementCoachGroups
{
    private static readonly CultureInfo Czech = CultureInfo.GetCultureInfo("cs-CZ");

    public static IReadOnlyList<RequirementCoachGroup> Group(IEnumerable<RequirementCoachListItem> assignments)
        => assignments
            .GroupBy(assignment => (assignment.CoachRoleId, assignment.CoachRoleOrder))
            .OrderBy(group => group.Key.CoachRoleOrder)
            .ThenBy(group => group.Key.CoachRoleId)
            .Select(group => new RequirementCoachGroup(
                group.Key.CoachRoleId,
                group.First().CoachRoleName,
                group.Select(assignment => assignment.LastName)
                    .Distinct()
                    .Order(StringComparer.Create(Czech, ignoreCase: true))
                    .ToList()))
            .ToList();
}
