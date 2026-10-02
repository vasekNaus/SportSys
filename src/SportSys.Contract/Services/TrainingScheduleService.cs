using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Database.Context;
using SportSys.Database.Models.sport;

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

    public async Task<List<LookupSelectItem>> GetTrainingStatesAsync(CancellationToken ct = default)
    {
        return await _db.TrainingStates
            .OrderBy(s => s.Id)
            .Select(s => new LookupSelectItem
            {
                Id = s.Id,
                Name = s.Name,
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

    public async Task<List<LookupSelectItem>> GetTrainingLocationsAsync(
        IReadOnlyCollection<int> includeIds,
        CancellationToken ct = default)
    {
        return await _db.SportLocations
            .Where(location => location.IsActive || includeIds.Contains(location.Id))
            .OrderBy(location => location.Name)
            .Select(location => new LookupSelectItem
            {
                Id = location.Id,
                Name = location.Name,
            })
            .ToListAsync(ct);
    }

    public async Task<List<string>> GetTrainingPlanLocationsAsync(CancellationToken ct = default)
    {
        return await _db.TrainingPlans
            .Where(p => p.Location != string.Empty)
            .Select(p => p.Location)
            .Distinct()
            .OrderBy(location => location)
            .ToListAsync(ct);
    }

    public async Task<List<TrainingScheduleItemDto>> GetTrainingsAsync(
        int seasonId,
        IReadOnlyCollection<string> categoryNames,
        IReadOnlyCollection<int> trainingTypeIds,
        IReadOnlyCollection<int> trainingStateIds,
        IReadOnlyCollection<int> locationIds,
        DateOnly dateFrom,
        DateOnly dateTo,
        bool mergeOverlapping,
        CancellationToken ct = default)
    {
        var query = _db.Training
            .Where(t => t.SeasonId == seasonId
                && categoryNames.Contains(t.SeasonCategoryName)
                && t.Date >= dateFrom
                && t.Date <= dateTo);

        if (trainingTypeIds.Count > 0)
            query = query.Where(t => trainingTypeIds.Contains(t.TrainingTypeId));

        if (trainingStateIds.Count > 0)
            query = query.Where(t => trainingStateIds.Contains(t.TrainingStateId));

        if (locationIds.Count > 0)
            query = query.Where(t => locationIds.Contains(t.LocationId));

        var trainings = await query
            .OrderBy(t => t.Date)
            .ThenBy(t => t.TimeFrom)
            .Select(t => new TrainingScheduleItemDto
            {
                Id = t.Id,
                SeasonId = t.SeasonId,
                Date = t.Date,
                TimeFrom = t.TimeFrom,
                TimeTo = t.TimeTo,
                DurationMinutes = t.DurationMinutes,
                GroupId = t.GroupMembership == null
                    ? null
                    : t.GroupMembership.GroupId,
                SeasonCategoryOrder = t.SeasonCategory.Order,
                SeasonCategoryName = t.SeasonCategoryName,
                LocationId = t.LocationId,
                LocationName = t.Location.Name,
                TrainingTypeName = t.TrainingType.Name,
                TrainingPhaseName = t.TrainingPhase.Name,
                TrainingStateId = t.TrainingStateId,
                TrainingStateName = t.TrainingState.Name,
                Note = t.Note,
            })
            .ToListAsync(ct);

        // CoachFullNames se načítá samostatným dotazem, protože EF Core
        // neumí přeložit korelovaný poddotaz s Distinct/OrderBy uvnitř
        // projekce, pokud stejná projekce obsahuje i podmíněnou (ternary)
        // referenční navigaci (GroupMembership) — viz stejný vzor u
        // AssignCoachFullNamesAsync pro tréninkové plány.
        await AssignCoachFullNamesAsync(trainings, ct);

        ApplyVisualizationGrouping(trainings, mergeOverlapping);

        return trainings;
    }

    public async Task<List<TrainingPlanScheduleItemDto>> GetTrainingPlansAsync(
        int seasonId,
        IReadOnlyCollection<string> categoryNames,
        IReadOnlyCollection<int> trainingTypeIds,
        IReadOnlyCollection<string> locations,
        int trainingPhaseId,
        DateOnly? validOn,
        bool mergeOverlapping,
        CancellationToken ct = default)
    {
        var query = _db.TrainingPlans
            .Where(p => p.SeasonId == seasonId
                && categoryNames.Contains(p.SeasonCategoryName)
                && p.TrainingPhaseId == trainingPhaseId);

        if (trainingTypeIds.Count > 0)
            query = query.Where(p => trainingTypeIds.Contains(p.TrainingTypeId));

        if (locations.Count > 0)
            query = query.Where(p => locations.Contains(p.Location));

        query = ApplyValidityFilter(query, validOn);

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
                CoachFullNames = new List<string>(),
                Note = string.Empty,
            })
            .ToListAsync(ct);

        // Přiřazení trenérů se řeší samostatným dotazem, protože EF Core
        // neumí přeložit korelovaný poddotaz filtrovaný na rozsah datumů
        // (ValidFrom/ValidTo), pokud je zároveň součástí kompozitního klíče.
        await AssignCoachFullNamesAsync(plans, ct);

        var orderedPlans = plans
            .OrderBy(p => p.DayOfWeek)
            .ThenBy(p => p.TimeFrom)
            .ThenBy(p => p.From)
            .ToList();

        ApplyVisualizationGrouping(orderedPlans, mergeOverlapping);

        return orderedPlans;
    }

    private async Task AssignCoachFullNamesAsync(
        IReadOnlyList<TrainingScheduleItemDto> trainings,
        CancellationToken ct)
    {
        if (trainings.Count == 0)
            return;

        var trainingIds = trainings.Select(t => t.Id).ToList();

        var assignments = await _db.CoachTrainings
            .Where(c => trainingIds.Contains(c.TrainingId))
            .Select(c => new
            {
                c.TrainingId,
                CoachFullName = c.Coach.DisplayName ?? string.Empty,
            })
            .ToListAsync(ct);

        var assignmentsByTrainingId = assignments
            .ToLookup(a => a.TrainingId, a => a.CoachFullName);

        foreach (var training in trainings)
        {
            training.CoachFullNames = assignmentsByTrainingId[training.Id]
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }
    }

    private async Task AssignCoachFullNamesAsync(
        IReadOnlyList<TrainingPlanScheduleItemDto> plans,
        CancellationToken ct)
    {
        if (plans.Count == 0)
            return;

        var planIds = plans.Select(p => p.Id).ToList();

        var assignments = await _db.CoachTrainingPlans
            .Where(c => planIds.Contains(c.TrainingPlanId))
            .Select(c => new
            {
                c.TrainingPlanId,
                c.ValidFrom,
                c.ValidTo,
                CoachFullName = c.Coach.DisplayName ?? string.Empty,
            })
            .ToListAsync(ct);

        var assignmentsByPlanId = assignments
            .ToLookup(a => a.TrainingPlanId);

        foreach (var plan in plans)
        {
            plan.CoachFullNames = assignmentsByPlanId[plan.Id]
                .Where(a => a.ValidFrom <= plan.To && a.ValidTo >= plan.From)
                .Select(a => a.CoachFullName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }
    }

    internal static void ApplyVisualizationGrouping(
        IReadOnlyList<TrainingScheduleItemDto> items,
        bool mergeOverlapping)
        => ApplyVisualizationGrouping(
            items,
            mergeOverlapping,
            item => item.Date);

    internal static void ApplyVisualizationGrouping(
        IReadOnlyList<TrainingPlanScheduleItemDto> items,
        bool mergeOverlapping)
        => ApplyVisualizationGrouping(
            items,
            mergeOverlapping,
            item => item.DayOfWeek);

    private static void ApplyVisualizationGrouping<TItem, TRow>(
        IReadOnlyList<TItem> items,
        bool mergeOverlapping,
        Func<TItem, TRow> rowSelector)
        where TItem : ITrainingScheduleItem
        where TRow : notnull
    {
        foreach (var item in items)
            item.VisualizationGroupId = item.GroupId;

        if (!mergeOverlapping)
            return;

        foreach (var rowItems in items.GroupBy(rowSelector))
        {
            var row = rowItems
                .OrderBy(item => item.TimeFrom)
                .ThenBy(item => item.TimeTo)
                .ThenBy(item => item.SeasonCategoryOrder)
                .ThenBy(item => item.SeasonCategoryName, StringComparer.Ordinal)
                .ThenBy(item => item.Id)
                .ToList();

            var parents = Enumerable.Range(0, row.Count).ToArray();

            for (var leftIndex = 0; leftIndex < row.Count; leftIndex++)
            {
                for (var rightIndex = leftIndex + 1; rightIndex < row.Count; rightIndex++)
                {
                    var left = row[leftIndex];
                    var right = row[rightIndex];
                    var samePersistedGroup = left.GroupId.HasValue
                        && left.GroupId == right.GroupId;
                    var intervalsTouchOrOverlap = left.TimeFrom <= right.TimeTo
                        && right.TimeFrom <= left.TimeTo;

                    if (samePersistedGroup || intervalsTouchOrOverlap)
                        Union(parents, leftIndex, rightIndex);
                }
            }

            foreach (var component in row
                .Select((item, index) => new { Item = item, Root = Find(parents, index) })
                .GroupBy(entry => entry.Root)
                .Select(group => group.Select(entry => entry.Item).ToList())
                .Where(component => component.Count > 1))
            {
                var persistedGroupIds = component
                    .Select(item => item.GroupId)
                    .Distinct()
                    .ToList();
                var visualizationGroupId = persistedGroupIds.Count == 1
                    && persistedGroupIds[0].HasValue
                        ? persistedGroupIds[0]!.Value
                        : Guid.NewGuid();

                foreach (var item in component)
                    item.VisualizationGroupId = visualizationGroupId;
            }
        }
    }

    private static int Find(int[] parents, int index)
    {
        while (parents[index] != index)
        {
            parents[index] = parents[parents[index]];
            index = parents[index];
        }

        return index;
    }

    private static void Union(int[] parents, int left, int right)
    {
        var leftRoot = Find(parents, left);
        var rightRoot = Find(parents, right);

        if (leftRoot != rightRoot)
            parents[rightRoot] = leftRoot;
    }

    internal static IQueryable<TrainingPlan> ApplyValidityFilter(
        IQueryable<TrainingPlan> query,
        DateOnly? validOn)
    {
        if (!validOn.HasValue)
            return query;

        var date = validOn.Value;
        return query.Where(plan => plan.From <= date && plan.To >= date);
    }
}
