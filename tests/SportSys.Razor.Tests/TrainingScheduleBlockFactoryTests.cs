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
        Assert.Equal(
            ["Novák", "Novák", "Svoboda"],
            block.Coaches.Select(coach => coach.FullName));
        Assert.Equal(1, block.MinimumItemId);
    }

    [Fact]
    public void CreateBlocks_AggregatesVisualizationGroupWithoutPersistedGroup()
    {
        var visualizationGroupId = Guid.NewGuid();
        var items = new ITrainingScheduleItem[]
        {
            CreateTraining(
                2,
                "U14",
                2,
                new TimeOnly(17, 0),
                new TimeOnly(18, 0),
                visualizationGroupId: visualizationGroupId),
            CreateTraining(
                1,
                "U12",
                1,
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                visualizationGroupId: visualizationGroupId),
        };

        var block = Assert.Single(TrainingScheduleBlockFactory.CreateBlocks(items));

        Assert.Equal("U12 + U14", block.Title);
        Assert.Equal(new TimeOnly(16, 0), block.TimeFrom);
        Assert.Equal(new TimeOnly(18, 0), block.TimeTo);
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

    [Fact]
    public void CreateBlocks_CoachSurnameSummaryUsesLastNameNotFullName()
    {
        var item = CreateTraining(
            1,
            "U12",
            1,
            new TimeOnly(16, 0),
            new TimeOnly(17, 0),
            coaches: ["Jan Novák", "Svoboda"]);

        var block = Assert.Single(TrainingScheduleBlockFactory.CreateBlocks([item]));

        Assert.Equal("Jan Novák, Svoboda", block.CoachSummary);
        Assert.Equal("Novák, Svoboda", block.CoachSurnameSummary);
    }

    [Fact]
    public void CreateBlocks_SingleTrainingPropagatesIsDryTraining()
    {
        var item = CreateTraining(
            1,
            "U12",
            1,
            new TimeOnly(16, 0),
            new TimeOnly(17, 0),
            isDryTraining: true);

        var block = Assert.Single(TrainingScheduleBlockFactory.CreateBlocks([item]));

        Assert.True(block.IsDryTraining);
    }

    [Fact]
    public void CreateBlocks_MergedTrainingsWithSameTrainingTypeKeepIsDryTraining()
    {
        var groupId = Guid.NewGuid();
        var items = new ITrainingScheduleItem[]
        {
            CreateTraining(
                2,
                "U14",
                2,
                new TimeOnly(17, 0),
                new TimeOnly(18, 0),
                groupId,
                isDryTraining: true),
            CreateTraining(
                1,
                "U12",
                1,
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                groupId,
                isDryTraining: true),
        };

        var block = Assert.Single(TrainingScheduleBlockFactory.CreateBlocks(items));

        Assert.True(block.IsDryTraining);
    }

    [Fact]
    public void CreateBlocks_MergedTrainingsWithMixedTrainingTypeFallBackToNotDry()
    {
        var groupId = Guid.NewGuid();
        var items = new ITrainingScheduleItem[]
        {
            CreateTraining(
                2,
                "U14",
                2,
                new TimeOnly(17, 0),
                new TimeOnly(18, 0),
                groupId,
                isDryTraining: true),
            CreateTraining(
                1,
                "U12",
                1,
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                groupId,
                isDryTraining: false),
        };

        var block = Assert.Single(TrainingScheduleBlockFactory.CreateBlocks(items));

        Assert.False(block.IsDryTraining);
    }

    [Fact]
    public void CreateBlocks_SingleTrainingHasUniformStateWithIconAndCssClass()
    {
        var item = CreateTraining(
            1,
            "U12",
            1,
            new TimeOnly(16, 0),
            new TimeOnly(17, 0),
            trainingStateId: 2,
            trainingStateName: "Potvrzený KIS");

        var block = Assert.Single(TrainingScheduleBlockFactory.CreateBlocks([item]));

        Assert.True(block.IsUniformState);
        Assert.Equal("training-state-confirmed", block.UniformStateCssClass);
        Assert.Equal("✅", block.UniformStateIcon);
        Assert.Equal("Potvrzený KIS", block.UniformStateName);
    }

    [Fact]
    public void CreateBlocks_MergedTrainingsWithSameStateAreUniform()
    {
        var groupId = Guid.NewGuid();
        var items = new ITrainingScheduleItem[]
        {
            CreateTraining(
                2,
                "U14",
                2,
                new TimeOnly(17, 0),
                new TimeOnly(18, 0),
                groupId,
                trainingStateId: 1,
                trainingStateName: "Plán"),
            CreateTraining(
                1,
                "U12",
                1,
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                groupId,
                trainingStateId: 1,
                trainingStateName: "Plán"),
        };

        var block = Assert.Single(TrainingScheduleBlockFactory.CreateBlocks(items));

        Assert.True(block.IsUniformState);
        Assert.False(block.HasMixedState);
        Assert.Equal("training-state-plan", block.UniformStateCssClass);
        Assert.Equal("📅", block.UniformStateIcon);
        Assert.Equal("Plán", block.UniformStateName);
    }

    [Fact]
    public void CreateBlocks_MergedTrainingsWithDifferentStatesAreNotUniform()
    {
        var groupId = Guid.NewGuid();
        var items = new ITrainingScheduleItem[]
        {
            CreateTraining(
                2,
                "U14",
                2,
                new TimeOnly(17, 0),
                new TimeOnly(18, 0),
                groupId,
                trainingStateId: 5,
                trainingStateName: "Zrušený"),
            CreateTraining(
                1,
                "U12",
                1,
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                groupId,
                trainingStateId: 1,
                trainingStateName: "Plán"),
        };

        var block = Assert.Single(TrainingScheduleBlockFactory.CreateBlocks(items));

        Assert.False(block.IsUniformState);
        Assert.True(block.HasMixedState);
        Assert.Null(block.UniformStateCssClass);
        Assert.Null(block.UniformStateIcon);
        Assert.Null(block.UniformStateName);
        Assert.Equal(
            ["📅", "❌"],
            block.CategorySegments.Select(segment => segment.StateIcon));
    }

    [Fact]
    public void CreateBlocks_BlockWithoutAnyStateIsUniformWithoutCssClass()
    {
        var item = CreateTraining(
            1,
            "U12",
            1,
            new TimeOnly(16, 0),
            new TimeOnly(17, 0));

        var block = Assert.Single(TrainingScheduleBlockFactory.CreateBlocks([item]));

        Assert.True(block.IsUniformState);
        Assert.False(block.HasMixedState);
        Assert.Null(block.UniformStateCssClass);
        Assert.Null(block.UniformStateIcon);
        Assert.Null(block.UniformStateName);
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
        IReadOnlyList<string>? coaches = null,
        Guid? visualizationGroupId = null,
        int? trainingStateId = null,
        string? trainingStateName = null,
        bool isDryTraining = false)
        => new()
        {
            Id = id,
            Date = new DateOnly(2026, 9, 7),
            TimeFrom = timeFrom,
            TimeTo = timeTo,
            GroupId = groupId,
            VisualizationGroupId = visualizationGroupId,
            SeasonCategoryCode = category,
            SeasonCategoryOrder = categoryOrder,
            LocationId = 1,
            LocationName = location,
            TrainingTypeName = trainingType,
            IsDryTraining = isDryTraining,
            TrainingPhaseName = "Season",
            Coaches = (coaches ?? [])
                .Select(fullName => new SimpleCoachDto
                {
                    FullName = fullName,
                    LastName = ExtractLastName(fullName),
                })
                .ToList(),
            TrainingStateId = trainingStateId,
            TrainingStateName = trainingStateName,
        };

    private static string ExtractLastName(string fullName)
    {
        var trimmed = fullName.Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        var lastSpaceIndex = trimmed.LastIndexOf(' ');
        return lastSpaceIndex < 0
            ? trimmed
            : trimmed[(lastSpaceIndex + 1)..];
    }
}
