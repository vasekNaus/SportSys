using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Database.Context;

namespace SportSys.Contract.Services;

public class TrainingRequirementService
{
    private readonly SportSysDbContext _db;

    public TrainingRequirementService(SportSysDbContext db)
    {
        _db = db;
    }

    public async Task<List<SeasonDto>> GetSeasonsAsync(CancellationToken ct = default)
    {
        return await _db.Seasons
            .AsNoTracking()
            .Where(season => season.IsActive)
            .OrderByDescending(season => season.From)
            .Select(season => new SeasonDto
            {
                Id = season.Id,
                Name = season.Name,
            })
            .ToListAsync(ct);
    }

    public async Task<List<SeasonCategoryDto>> GetCategoriesAsync(
        int seasonId,
        CancellationToken ct = default)
    {
        return await _db.SeasonCategories
            .AsNoTracking()
            .Where(category => category.SeasonId == seasonId && category.IsActive)
            .OrderBy(category => category.Order)
            .ThenBy(category => category.Name)
            .Select(category => new SeasonCategoryDto
            {
                SeasonId = category.SeasonId,
                Name = category.Name,
                Order = category.Order,
            })
            .ToListAsync(ct);
    }

    public async Task<List<LookupSelectItem>> GetTrainingTypesAsync(
        CancellationToken ct = default)
    {
        return await _db.TrainingTypes
            .AsNoTracking()
            .OrderBy(type => type.Id)
            .Select(type => new LookupSelectItem
            {
                Id = type.Id,
                Name = type.Name,
            })
            .ToListAsync(ct);
    }

    public async Task<List<LookupSelectItem>> GetTrainingPhasesAsync(
        CancellationToken ct = default)
    {
        return await _db.TrainingPhases
            .AsNoTracking()
            .OrderBy(phase => phase.Id)
            .Select(phase => new LookupSelectItem
            {
                Id = phase.Id,
                Name = phase.Name,
            })
            .ToListAsync(ct);
    }

    public async Task<List<TrainingRequirementListItem>> GetAllAsync(
        int seasonId,
        IReadOnlyCollection<string> categoryNames,
        IReadOnlyCollection<int> trainingTypeIds,
        IReadOnlyCollection<int> trainingPhaseIds,
        CancellationToken ct = default)
    {
        var query = _db.TrainingRequirements
            .AsNoTracking()
            .Where(requirement => requirement.SeasonId == seasonId);

        if (categoryNames.Count > 0)
        {
            query = query.Where(requirement =>
                categoryNames.Contains(requirement.SeasonCategoryName));
        }

        if (trainingTypeIds.Count > 0)
        {
            query = query.Where(requirement =>
                trainingTypeIds.Contains(requirement.TrainingTypeId));
        }

        if (trainingPhaseIds.Count > 0)
        {
            query = query.Where(requirement =>
                trainingPhaseIds.Contains(requirement.TrainingPhaseId));
        }

        return await query
            .OrderByDescending(requirement => requirement.SeasonCategory.Season.From)
            .ThenBy(requirement => requirement.SeasonCategory.Order)
            .ThenBy(requirement => requirement.SeasonCategoryName)
            .ThenBy(requirement => requirement.From)
            .ThenBy(requirement => requirement.To)
            .ThenBy(requirement => requirement.TrainingType.Name)
            .ThenBy(requirement => requirement.TrainingPhase.Name)
            .ThenBy(requirement => requirement.Id)
            .Select(requirement => new TrainingRequirementListItem
            {
                Id = requirement.Id,
                SeasonId = requirement.SeasonId,
                SeasonName = requirement.SeasonCategory.Season.Name,
                SeasonCategoryName = requirement.SeasonCategoryName,
                SeasonCategoryOrder = requirement.SeasonCategory.Order,
                TrainingTypeId = requirement.TrainingTypeId,
                TrainingTypeName = requirement.TrainingType.Name,
                TrainingPhaseId = requirement.TrainingPhaseId,
                TrainingPhaseName = requirement.TrainingPhase.Name,
                From = requirement.From,
                To = requirement.To,
                DurationHours = requirement.DurationHours,
                CoachAssignments = requirement.CoachTrainingRequirements
                    .OrderBy(assignment => assignment.Coach.DisplayName)
                    .ThenBy(assignment => assignment.Coach.PersonalNumber)
                    .ThenBy(assignment => assignment.CoachRole.Name)
                    .ThenBy(assignment => assignment.CoachId)
                    .ThenBy(assignment => assignment.CoachRoleId)
                    .Select(assignment => new TrainingRequirementCoachListItem
                    {
                        CoachId = assignment.CoachId,
                        DisplayName = assignment.Coach.DisplayName,
                        PersonalNumber = assignment.Coach.PersonalNumber,
                        CoachRoleId = assignment.CoachRoleId,
                        CoachRoleName = assignment.CoachRole.Name,
                    })
                    .ToList(),
            })
            .ToListAsync(ct);
    }
}
