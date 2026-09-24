using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Tests;

public class TrainingScheduleVisualizationGroupingTests
{
    [Fact]
    public void ApplyVisualizationGrouping_WhenDisabled_PreservesOnlyPersistedGroups()
    {
        var persistedGroupId = Guid.NewGuid();
        var items = new[]
        {
            CreateTraining(1, new TimeOnly(16, 0), new TimeOnly(17, 0), persistedGroupId),
            CreateTraining(2, new TimeOnly(18, 0), new TimeOnly(19, 0), persistedGroupId),
            CreateTraining(3, new TimeOnly(16, 30), new TimeOnly(17, 30)),
        };

        TrainingScheduleService.ApplyVisualizationGrouping(items, mergeOverlapping: false);

        Assert.Equal(persistedGroupId, items[0].VisualizationGroupId);
        Assert.Equal(persistedGroupId, items[1].VisualizationGroupId);
        Assert.Null(items[2].VisualizationGroupId);
    }

    [Fact]
    public void ApplyVisualizationGrouping_MergesOverlappingIntervals()
    {
        var items = new[]
        {
            CreateTraining(1, new TimeOnly(16, 0), new TimeOnly(17, 0)),
            CreateTraining(2, new TimeOnly(16, 30), new TimeOnly(17, 30)),
        };

        TrainingScheduleService.ApplyVisualizationGrouping(items, mergeOverlapping: true);

        Assert.NotNull(items[0].VisualizationGroupId);
        Assert.Equal(items[0].VisualizationGroupId, items[1].VisualizationGroupId);
    }

    [Fact]
    public void ApplyVisualizationGrouping_MergesTouchingIntervalsTransitively()
    {
        var items = new[]
        {
            CreateTraining(1, new TimeOnly(16, 0), new TimeOnly(17, 0)),
            CreateTraining(2, new TimeOnly(16, 45), new TimeOnly(17, 30)),
            CreateTraining(3, new TimeOnly(17, 30), new TimeOnly(18, 0)),
        };

        TrainingScheduleService.ApplyVisualizationGrouping(items, mergeOverlapping: true);

        Assert.NotNull(items[0].VisualizationGroupId);
        Assert.Single(items.Select(item => item.VisualizationGroupId).Distinct());
    }

    [Fact]
    public void ApplyVisualizationGrouping_DoesNotMergeIntervalsSeparatedByGap()
    {
        var items = new[]
        {
            CreateTraining(1, new TimeOnly(16, 0), new TimeOnly(17, 0)),
            CreateTraining(2, new TimeOnly(17, 1), new TimeOnly(18, 0)),
        };

        TrainingScheduleService.ApplyVisualizationGrouping(items, mergeOverlapping: true);

        Assert.All(items, item => Assert.Null(item.VisualizationGroupId));
    }

    [Fact]
    public void ApplyVisualizationGrouping_DoesNotMergeAcrossDates()
    {
        var items = new[]
        {
            CreateTraining(1, new TimeOnly(16, 0), new TimeOnly(17, 0)),
            CreateTraining(
                2,
                new TimeOnly(16, 30),
                new TimeOnly(17, 30),
                date: new DateOnly(2026, 9, 8)),
        };

        TrainingScheduleService.ApplyVisualizationGrouping(items, mergeOverlapping: true);

        Assert.All(items, item => Assert.Null(item.VisualizationGroupId));
    }

    [Fact]
    public void ApplyVisualizationGrouping_DoesNotUsePersistedGroupEnvelopeForOverlap()
    {
        var persistedGroupId = Guid.NewGuid();
        var items = new[]
        {
            CreateTraining(1, new TimeOnly(16, 0), new TimeOnly(17, 0), persistedGroupId),
            CreateTraining(2, new TimeOnly(18, 0), new TimeOnly(19, 0), persistedGroupId),
            CreateTraining(3, new TimeOnly(17, 15), new TimeOnly(17, 45)),
        };

        TrainingScheduleService.ApplyVisualizationGrouping(items, mergeOverlapping: true);

        Assert.Equal(persistedGroupId, items[0].VisualizationGroupId);
        Assert.Equal(persistedGroupId, items[1].VisualizationGroupId);
        Assert.Null(items[2].VisualizationGroupId);
    }

    [Fact]
    public void ApplyVisualizationGrouping_MergesItemTouchingPersistedGroupMember()
    {
        var persistedGroupId = Guid.NewGuid();
        var items = new[]
        {
            CreateTraining(1, new TimeOnly(16, 0), new TimeOnly(17, 0), persistedGroupId),
            CreateTraining(2, new TimeOnly(18, 0), new TimeOnly(19, 0), persistedGroupId),
            CreateTraining(3, new TimeOnly(17, 30), new TimeOnly(18, 0)),
        };

        TrainingScheduleService.ApplyVisualizationGrouping(items, mergeOverlapping: true);

        Assert.NotNull(items[0].VisualizationGroupId);
        Assert.Single(items.Select(item => item.VisualizationGroupId).Distinct());
        Assert.Equal(persistedGroupId, items[0].GroupId);
        Assert.Equal(persistedGroupId, items[1].GroupId);
        Assert.Null(items[2].GroupId);
    }

    [Fact]
    public void ApplyVisualizationGrouping_DoesNotMergePlansAcrossWeekDays()
    {
        var items = new[]
        {
            CreatePlan(1, DayOfWeek.Monday),
            CreatePlan(2, DayOfWeek.Tuesday),
        };

        TrainingScheduleService.ApplyVisualizationGrouping(items, mergeOverlapping: true);

        Assert.All(items, item => Assert.Null(item.VisualizationGroupId));
    }

    private static TrainingScheduleItemDto CreateTraining(
        int id,
        TimeOnly timeFrom,
        TimeOnly timeTo,
        Guid? groupId = null,
        DateOnly? date = null)
    {
        var trainingDate = date ?? new DateOnly(2026, 9, 7);
        return new TrainingScheduleItemDto
        {
            Id = id,
            Date = trainingDate,
            From = trainingDate,
            To = trainingDate,
            DayName = trainingDate.DayOfWeek.ToString(),
            TimeFrom = timeFrom,
            TimeTo = timeTo,
            GroupId = groupId,
            SeasonCategoryOrder = id,
            SeasonCategoryName = $"Category {id}",
        };
    }

    private static TrainingPlanScheduleItemDto CreatePlan(int id, DayOfWeek day)
        => new()
        {
            Id = id,
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2026, 9, 30),
            DayName = day.ToString(),
            TimeFrom = new TimeOnly(16, 0),
            TimeTo = new TimeOnly(17, 0),
            SeasonCategoryOrder = id,
            SeasonCategoryName = $"Category {id}",
        };
}
