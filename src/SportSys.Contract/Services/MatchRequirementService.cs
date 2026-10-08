using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Database.Context;

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
        CancellationToken ct = default)
    {
        var query = _db.MatchRequirements
            .AsNoTracking()
            .Where(requirement => requirement.SeasonId == seasonId);

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
}
