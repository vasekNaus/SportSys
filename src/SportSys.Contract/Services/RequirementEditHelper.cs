using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Contract.Models.hr;
using SportSys.Database.Context;

namespace SportSys.Contract.Services;

internal static class RequirementEditHelper
{
    public static Task<List<CoachSelectItem>> GetCoachesAsync(
        SportSysDbContext db,
        CancellationToken ct)
        => db.Coaches
            .AsNoTracking()
            .OrderBy(coach => coach.DisplayName ?? coach.UserName ?? coach.Email)
            .ThenBy(coach => coach.PersonalNumber)
            .Select(coach => new CoachSelectItem
            {
                Id = coach.Id,
                DisplayName = coach.DisplayName ?? coach.UserName ?? coach.Email ?? coach.Id.ToString(),
                Email = coach.Email,
                PersonalNumber = coach.PersonalNumber,
            })
            .ToListAsync(ct);

    public static Task<List<LookupSelectItem>> GetRolesAsync(
        SportSysDbContext db,
        CancellationToken ct)
        => db.CoachRoles
            .AsNoTracking()
            .OrderBy(role => role.Id)
            .Select(role => new LookupSelectItem { Id = role.Id, Name = role.Name })
            .ToListAsync(ct);

    /// <summary>
    /// Ověří přiřazení trenérů; při chybě vrací výsledek, jinak <c>null</c>.
    /// </summary>
    public static async Task<RequirementEditResult?> ValidateAssignmentsAsync(
        SportSysDbContext db,
        IReadOnlyCollection<RequirementCoachAssignmentInput> assignments,
        CancellationToken ct)
    {
        if (assignments.Any(a => !a.CoachId.HasValue || !a.CoachRoleId.HasValue))
            return new RequirementEditResult(RequirementEditStatus.InvalidInput);

        var coachIds = assignments.Select(a => a.CoachId!.Value).Distinct().ToList();
        var roleIds = assignments.Select(a => a.CoachRoleId!.Value).Distinct().ToList();

        var coachNames = await db.Coaches
            .AsNoTracking()
            .Where(coach => coachIds.Contains(coach.Id))
            .Select(coach => new
            {
                coach.Id,
                Name = coach.DisplayName ?? coach.UserName ?? coach.Email ?? coach.Id.ToString(),
            })
            .ToDictionaryAsync(coach => coach.Id, coach => coach.Name, ct);

        var existingRoleCount = await db.CoachRoles
            .CountAsync(role => roleIds.Contains(role.Id), ct);

        if (coachNames.Count != coachIds.Count || existingRoleCount != roleIds.Count)
            return new RequirementEditResult(RequirementEditStatus.InvalidInput);

        var duplicate = assignments
            .GroupBy(a => a.CoachId!.Value)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            return new RequirementEditResult(
                RequirementEditStatus.DuplicateCoach,
                $"Trenér {coachNames[duplicate.Key]} je již k tomuto požadavku přiřazen. "
                + "Jeden trenér může být v rámci požadavku přiřazen pouze jednou.");
        }

        return null;
    }
}
