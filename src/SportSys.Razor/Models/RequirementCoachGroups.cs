using System.Globalization;
using SportSys.Contract.Models;
using SportSys.Razor.Infrastructure;

namespace SportSys.Razor.Models;

public sealed record RequirementCoachGroup(int RoleId, string RoleName, IReadOnlyList<string> CoachDisplayTexts);

public static class RequirementCoachGroups
{
    private static readonly CultureInfo Czech = CultureInfo.GetCultureInfo("cs-CZ");
    private static readonly StringComparer CzechLastNameComparer =
        StringComparer.Create(Czech, ignoreCase: true);
    private static readonly HashSet<string> DedicatedRoleCodes =
        new(["H", "A", "B", "O"], StringComparer.Ordinal);

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
                    .OrderBy(assignment => assignment.LastName, CzechLastNameComparer)
                    .Select(assignment => includeHours
                        ? FormatWithHours(assignment)
                        : assignment.LastName);

                return new RequirementCoachGroup(
                    group.Key.CoachRoleId,
                    group.First().CoachRoleName,
                    (includeHours ? displayTexts : displayTexts.Distinct()).ToList());
            })
            .ToList();

    public static IReadOnlyList<string> ForRole(
        IEnumerable<RequirementCoachListItem> assignments,
        string roleCode)
        => Group(
                assignments.Where(assignment =>
                    string.Equals(assignment.CoachRoleCode, roleCode, StringComparison.Ordinal)),
                includeHours: true)
            .SelectMany(group => group.CoachDisplayTexts)
            .ToList();

    public static IReadOnlyList<RequirementCoachListItem> OtherRoles(
        IEnumerable<RequirementCoachListItem> assignments)
        => assignments
            .Where(assignment => !DedicatedRoleCodes.Contains(assignment.CoachRoleCode))
            .ToList();

    private static string FormatWithHours(RequirementCoachListItem assignment)
        => $"{assignment.LastName} ({CzechNumberFormat.FormatDecimal(assignment.Hours, "0.##")})";
}
