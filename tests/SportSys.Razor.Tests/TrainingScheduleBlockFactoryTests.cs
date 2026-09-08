using SportSys.Contract.Models;
using SportSys.Razor.Models.TrainingSchedule;

namespace SportSys.Razor.Tests;

public class TrainingScheduleBlockFactoryTests
{
    [Fact]
    public void CreateBlocks_LeavesUngroupedTrainingsSeparate()
    {
        var items = new ITrainingScheduleItem[]
        {
            CreateTraining(1, "U12", 1, new TimeOnly(16, 0), new TimeOnly(17, 0)),
            CreateTraining(2, "U14", 2, new TimeOnly(17, 0), new TimeOnly(18, 0)),
        };

        var blocks = TrainingScheduleBlockFactory.CreateBlocks(items);

        Assert.Equal(2, blocks.Count);
        Assert.Equal(["U12", "U14"], blocks.Select(block => block.Title));
    }

    [Fact]
    public void CreateBlocks_AggregatesGroupedTrainings()
    {
        var groupId = Guid.NewGuid();
        var items = new ITrainingScheduleItem[]
        {
            CreateTraining(
                2,
                "U14",
                2,
                new TimeOnly(17, 0),
                new TimeOnly(18, 30),
                groupId,
                "Led",
                "B",
                ["Novák", "Svoboda"]),
            CreateTraining(
                1,
                "U12",
                1,
                new TimeOnly(16, 0),
                new TimeOnly(17, 30),
                groupId,
                "Suchá",
                "A",
                ["Novák"]),
        };

        var block = Assert.Single(TrainingScheduleBlockFactory.CreateBlocks(items));

        Assert.Equal("U12 + U14", block.Title);
        Assert.Equal(new TimeOnly(16, 0), block.TimeFrom);
        Assert.Equal(new TimeOnly(18, 30), block.TimeTo);
        Assert.Equal(["A", "B"], block.TrainingTypeNames);
        Assert.Equal(["Led", "Suchá"], block.Locations);
        Assert.Equal(["Novák", "Svoboda"], block.CoachNames);
        Assert.Equal(1, block.MinimumItemId);
    }

    [Fact]
    public void CreateBlocks_UsesDashWhenNoCoachIsAssigned()
    {
        var item = CreateTraining(
            1,
            "U12",
            1,
            new TimeOnly(16, 0),
            new TimeOnly(17, 0));

        var block = Assert.Single(
            TrainingScheduleBlockFactory.CreateBlocks([item]));

        Assert.Equal("-", block.CoachSummary);
    }

    private static TrainingScheduleItemDto CreateTraining(
        int id,
        string category,
        int categoryOrder,
        TimeOnly timeFrom,
        TimeOnly timeTo,
        Guid? groupId = null,
        string location = "",
        string trainingType = "Led",
        IReadOnlyList<string>? coaches = null)
        => new()
        {
            Id = id,
            Date = new DateOnly(2026, 9, 7),
            From = new DateOnly(2026, 9, 7),
            To = new DateOnly(2026, 9, 7),
            DayName = nameof(DayOfWeek.Monday),
            TimeFrom = timeFrom,
            TimeTo = timeTo,
            GroupId = groupId,
            SeasonCategoryName = category,
            SeasonCategoryOrder = categoryOrder,
            Location = location,
            TrainingTypeName = trainingType,
            TrainingPhaseName = "Season",
            CoachFullNames = coaches ?? [],
        };
}
