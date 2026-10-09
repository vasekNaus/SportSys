using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Database.Context;
using SportSys.Database.Models.sport;

namespace SportSys.Contract.Services;

public class MatchRequirementService
{
    private readonly SportSysDbContext _db;

    public MatchRequirementService(SportSysDbContext db)
    {
        _db = db;
    }

    public async Task<List<MatchRequirementListItem>> GetAllAsync(
        int seasonId,
        IReadOnlyCollection<string> categoryCodes,
        DateOnly? validOn = null,
        CancellationToken ct = default)
    {
        var query = _db.MatchRequirements
            .AsNoTracking()
            .Where(requirement => requirement.SeasonId == seasonId);

        if (validOn.HasValue)
        {
            var date = validOn.Value;
            query = query.Where(requirement =>
                requirement.From <= date && requirement.To >= date);
        }

        if (categoryCodes.Count > 0)
        {
            query = query.Where(requirement =>
                categoryCodes.Contains(requirement.SeasonCategoryCode));
        }

        return await query
            .OrderByDescending(requirement => requirement.SeasonCategory.Season.From)
            .ThenBy(requirement => requirement.SeasonCategory.Order)
            .ThenBy(requirement => requirement.SeasonCategoryCode)
            .ThenBy(requirement => requirement.From)
            .ThenBy(requirement => requirement.To)
            .ThenBy(requirement => requirement.Id)
            .Select(requirement => new MatchRequirementListItem
            {
                Id = requirement.Id,
                SeasonId = requirement.SeasonId,
                SeasonName = requirement.SeasonCategory.Season.Name,
                SeasonCategoryCode = requirement.SeasonCategoryCode,
                SeasonCategoryOrder = requirement.SeasonCategory.Order,
                From = requirement.From,
                To = requirement.To,
                MatchCount = requirement.MatchCount,
                CoachAssignments = requirement.CoachMatchRequirements
                    .OrderBy(assignment =>
                        assignment.Coach.DisplayName
                        ?? assignment.Coach.UserName
                        ?? assignment.Coach.Email)
                    .ThenBy(assignment => assignment.Coach.PersonalNumber)
                    .ThenBy(assignment => assignment.CoachRole.Name)
                    .ThenBy(assignment => assignment.CoachId)
                    .ThenBy(assignment => assignment.CoachRoleId)
                    .Select(assignment => new MatchRequirementCoachListItem
                    {
                        Id = assignment.CoachId,
                        DisplayName =
                            assignment.Coach.DisplayName
                            ?? assignment.Coach.UserName
                            ?? assignment.Coach.Email
                            ?? assignment.CoachId.ToString(),
                        Email = assignment.Coach.Email,
                        PersonalNumber = assignment.Coach.PersonalNumber,
                        CoachRoleId = assignment.CoachRoleId,
                        CoachRoleName = assignment.CoachRole.Name,
                    })
                    .ToList(),
            })
            .ToListAsync(ct);
    }

    public async Task<RequirementEditContextDto<MatchRequirementEditDto>?> GetEditAsync(
        int id,
        CancellationToken ct = default)
    {
        var requirement = await _db.MatchRequirements
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.From,
                item.To,
                item.MatchCount,
                SeasonName = item.SeasonCategory.Season.Name,
                item.SeasonCategoryCode,
                Assignments = item.CoachMatchRequirements
                    .Select(a => new { a.CoachId, a.CoachRoleId })
                    .ToList(),
            })
            .SingleOrDefaultAsync(ct);

        if (requirement is null)
            return null;

        return new RequirementEditContextDto<MatchRequirementEditDto>
        {
            Input = new MatchRequirementEditDto
            {
                Id = requirement.Id,
                From = requirement.From,
                To = requirement.To,
                MatchCount = requirement.MatchCount,
                CoachAssignments = requirement.Assignments
                    .Select(a => new RequirementCoachAssignmentInput
                    {
                        CoachId = a.CoachId,
                        CoachRoleId = a.CoachRoleId,
                    })
                    .ToList(),
            },
            SeasonName = requirement.SeasonName,
            SeasonCategoryCode = requirement.SeasonCategoryCode,
            AvailableCoaches = await RequirementEditHelper.GetCoachesAsync(_db, ct),
            CoachRoles = await RequirementEditHelper.GetRolesAsync(_db, ct),
        };
    }

    public async Task<RequirementEditResult> UpdateAsync(
        MatchRequirementEditDto dto,
        CancellationToken ct = default)
    {
        if (dto.From > dto.To || dto.MatchCount < 0)
            return new RequirementEditResult(RequirementEditStatus.InvalidInput);

        var invalid = await RequirementEditHelper.ValidateAssignmentsAsync(
            _db, dto.CoachAssignments, ct);
        if (invalid is not null)
            return invalid;

        var requirement = await _db.MatchRequirements
            .Include(item => item.CoachMatchRequirements)
            .SingleOrDefaultAsync(item => item.Id == dto.Id, ct);
        if (requirement is null)
            return new RequirementEditResult(RequirementEditStatus.NotFound);

        requirement.From = dto.From;
        requirement.To = dto.To;
        requirement.MatchCount = dto.MatchCount;

        var wanted = dto.CoachAssignments
            .Select(a => (CoachId: a.CoachId!.Value, RoleId: a.CoachRoleId!.Value))
            .ToHashSet();
        var existing = requirement.CoachMatchRequirements
            .ToDictionary(a => (a.CoachId, RoleId: a.CoachRoleId));

        _db.CoachMatchRequirements.RemoveRange(
            existing.Where(pair => !wanted.Contains(pair.Key)).Select(pair => pair.Value));
        foreach (var key in wanted.Where(key => !existing.ContainsKey(key)))
        {
            _db.CoachMatchRequirements.Add(new CoachMatchRequirement
            {
                CoachId = key.CoachId,
                CoachRoleId = key.RoleId,
                MatchRequirementId = requirement.Id,
            });
        }

        await _db.SaveChangesAsync(ct);
        return new RequirementEditResult(RequirementEditStatus.Success);
    }
}
