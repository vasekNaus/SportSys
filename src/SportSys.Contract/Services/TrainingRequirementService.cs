using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Database.Context;
using SportSys.Database.Models.sport;

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
            .ThenBy(category => category.Code)
            .Select(category => new SeasonCategoryDto
            {
                SeasonId = category.SeasonId,
                Code = category.Code,
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
        IReadOnlyCollection<string> categoryCodes,
        IReadOnlyCollection<int> trainingTypeIds,
        IReadOnlyCollection<int> trainingPhaseIds,
        DateOnly? validOn = null,
        CancellationToken ct = default)
    {
        var query = _db.TrainingRequirements
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
            .ThenBy(requirement => requirement.SeasonCategoryCode)
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
                SeasonCategoryCode = requirement.SeasonCategoryCode,
                SeasonCategoryOrder = requirement.SeasonCategory.Order,
                TrainingTypeId = requirement.TrainingTypeId,
                TrainingTypeName = requirement.TrainingType.Name,
                TrainingPhaseId = requirement.TrainingPhaseId,
                TrainingPhaseName = requirement.TrainingPhase.Name,
                From = requirement.From,
                To = requirement.To,
                DurationHours = requirement.DurationHours,
                CoachAssignments = requirement.CoachTrainingRequirements
                    .OrderBy(assignment =>
                        assignment.Coach.DisplayName
                        ?? assignment.Coach.UserName
                        ?? assignment.Coach.Email)
                    .ThenBy(assignment => assignment.Coach.PersonalNumber)
                    .ThenBy(assignment => assignment.CoachRole.Name)
                    .ThenBy(assignment => assignment.CoachId)
                    .ThenBy(assignment => assignment.CoachRoleId)
                    .Select(assignment => new TrainingRequirementCoachListItem
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

    public async Task<RequirementEditContextDto<TrainingRequirementEditDto>?> GetEditAsync(
        int id,
        CancellationToken ct = default)
    {
        var requirement = await _db.TrainingRequirements
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.From,
                item.To,
                item.DurationHours,
                SeasonName = item.SeasonCategory.Season.Name,
                item.SeasonCategoryCode,
                TrainingTypeName = item.TrainingType.Name,
                TrainingPhaseName = item.TrainingPhase.Name,
                Assignments = item.CoachTrainingRequirements
                    .Select(a => new { a.CoachId, a.CoachRoleId })
                    .ToList(),
            })
            .SingleOrDefaultAsync(ct);

        if (requirement is null)
            return null;

        return new RequirementEditContextDto<TrainingRequirementEditDto>
        {
            Input = new TrainingRequirementEditDto
            {
                Id = requirement.Id,
                From = requirement.From,
                To = requirement.To,
                DurationHours = requirement.DurationHours,
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
            TrainingTypeName = requirement.TrainingTypeName,
            TrainingPhaseName = requirement.TrainingPhaseName,
            AvailableCoaches = await RequirementEditHelper.GetCoachesAsync(_db, ct),
            CoachRoles = await RequirementEditHelper.GetRolesAsync(_db, ct),
        };
    }

    public async Task<RequirementEditResult> UpdateAsync(
        TrainingRequirementEditDto dto,
        CancellationToken ct = default)
    {
        if (dto.From > dto.To || dto.DurationHours <= 0 || dto.DurationHours > 999.99m)
            return new RequirementEditResult(RequirementEditStatus.InvalidInput);

        var invalid = await RequirementEditHelper.ValidateAssignmentsAsync(
            _db, dto.CoachAssignments, ct);
        if (invalid is not null)
            return invalid;

        var requirement = await _db.TrainingRequirements
            .Include(item => item.CoachTrainingRequirements)
            .SingleOrDefaultAsync(item => item.Id == dto.Id, ct);
        if (requirement is null)
            return new RequirementEditResult(RequirementEditStatus.NotFound);

        requirement.From = dto.From;
        requirement.To = dto.To;
        requirement.DurationHours = dto.DurationHours;

        var wanted = dto.CoachAssignments
            .Select(a => (CoachId: a.CoachId!.Value, RoleId: a.CoachRoleId!.Value))
            .ToHashSet();
        var existing = requirement.CoachTrainingRequirements
            .ToDictionary(a => (a.CoachId, RoleId: a.CoachRoleId));

        _db.CoachTrainingRequirements.RemoveRange(
            existing.Where(pair => !wanted.Contains(pair.Key)).Select(pair => pair.Value));
        foreach (var key in wanted.Where(key => !existing.ContainsKey(key)))
        {
            _db.CoachTrainingRequirements.Add(new CoachTrainingRequirement
            {
                CoachId = key.CoachId,
                CoachRoleId = key.RoleId,
                TrainingRequirementId = requirement.Id,
            });
        }

        await _db.SaveChangesAsync(ct);
        return new RequirementEditResult(RequirementEditStatus.Success);
    }
}
