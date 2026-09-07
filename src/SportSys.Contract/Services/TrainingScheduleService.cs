using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Database.Context;

namespace SportSys.Contract.Services;

public class TrainingScheduleService
{
    private readonly SportSysDbContext _db;

    public TrainingScheduleService(SportSysDbContext db)
    {
        _db = db;
    }

    public async Task<List<SeasonDto>> GetSeasonsAsync(CancellationToken ct = default)
    {
        return await _db.Seasons
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.From)
            .Select(s => new SeasonDto { Id = s.Id, Name = s.Name })
            .ToListAsync(ct);
    }

    public async Task<List<SeasonCategoryDto>> GetCategoriesAsync(int seasonId, CancellationToken ct = default)
    {
        return await _db.SeasonCategories
            .Where(c => c.SeasonId == seasonId && c.IsActive)
            .OrderBy(c => c.Order)
            .Select(c => new SeasonCategoryDto
            {
                SeasonId = c.SeasonId,
                Name = c.Name,
                Order = c.Order,
            })
            .ToListAsync(ct);
    }

    public async Task<List<LookupSelectItem>> GetTrainingTypesAsync(CancellationToken ct = default)
    {
        return await _db.TrainingTypes
            .OrderBy(t => t.Id)
            .Select(t => new LookupSelectItem
            {
                Id = t.Id,
                Name = t.Name,
            })
            .ToListAsync(ct);
    }

    public async Task<List<LookupSelectItem>> GetTrainingPhasesAsync(CancellationToken ct = default)
    {
        return await _db.TrainingPhases
            .OrderBy(p => p.Id)
            .Select(p => new LookupSelectItem
            {
                Id = p.Id,
                Name = p.Name,
            })
            .ToListAsync(ct);
    }

    public async Task<List<TrainingScheduleItemDto>> GetTrainingsAsync(
        int seasonId,
        IReadOnlyCollection<string> categoryNames,
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken ct = default)
    {
        var trainings = await _db.Training
            .Where(t => t.SeasonId == seasonId
                && categoryNames.Contains(t.SeasonCategoryName)
                && t.Date >= dateFrom
                && t.Date <= dateTo)
            .OrderBy(t => t.Date)
            .ThenBy(t => t.TimeFrom)
            .Select(t => new TrainingScheduleItemDto
            {
                Id = t.Id,
                Date = t.Date,
                From = t.Date,
                To = t.Date,
                TimeFrom = t.TimeFrom,
                TimeTo = t.TimeTo,
                DurationMinutes = t.DurationMinutes,
                GroupId = t.GroupMembership == null
                    ? null
                    : t.GroupMembership.GroupId,
                SeasonCategoryOrder = t.SeasonCategory.Order,
                SeasonCategoryName = t.SeasonCategoryName,
                Location = t.Location,
                TrainingTypeName = t.TrainingType.Name,
                TrainingPhaseName = t.TrainingPhase.Name,
                Note = t.Note,
            })
            .ToListAsync(ct);

        foreach (var training in trainings)
            training.DayName = training.Date.DayOfWeek.ToString();

        return trainings;
    }

    public async Task<List<TrainingPlanScheduleItemDto>> GetTrainingPlansAsync(
        int seasonId,
        IReadOnlyCollection<string> categoryNames,
        IReadOnlyCollection<int> trainingTypeIds,
        int trainingPhaseId,
        CancellationToken ct = default)
    {
        var query = _db.TrainingPlans
            .Where(p => p.SeasonId == seasonId
                && categoryNames.Contains(p.SeasonCategoryName)
                && p.TrainingPhaseId == trainingPhaseId);

        if (trainingTypeIds.Count > 0)
            query = query.Where(p => trainingTypeIds.Contains(p.TrainingTypeId));

        var plans = await query
            .OrderBy(p => p.TimeFrom)
            .ThenBy(p => p.From)
            .Select(p => new TrainingPlanScheduleItemDto
            {
                Id = p.Id,
                From = p.From,
                To = p.To,
                DayName = p.DayName,
                TimeFrom = p.TimeFrom,
                TimeTo = p.TimeTo,
                DurationMinutes = p.DurationMinutes,
                GroupId = p.GroupMembership == null
                    ? null
                    : p.GroupMembership.GroupId,
                SeasonCategoryOrder = p.SeasonCategory.Order,
                SeasonCategoryName = p.SeasonCategoryName,
                Location = p.Location,
                TrainingTypeName = p.TrainingType.Name,
                TrainingPhaseName = p.TrainingPhase.Name,
                Note = string.Empty,
            })
            .ToListAsync(ct);

        return plans
            .OrderBy(p => p.DayOfWeek)
            .ThenBy(p => p.TimeFrom)
            .ThenBy(p => p.From)
            .ToList();
    }
}
