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
    public void CreateBlocks_OrdersPlanCoachesByCategoryRoleAndSurnameWithoutCrossPlanDeduplication()
    {
        var groupId = Guid.NewGuid();
        var laterCategory = CreateTrainingPlan(
            2,
            "U14",
            2,
            groupId,
            [
                CreatePlanCoach(1, "Jan Beta", 1, "H", 1),
                CreatePlanCoach(3, "Adam Alfa", 3, "A", 2),
            ]);
        var earlierCategory = CreateTrainingPlan(
            1,
            "U12",
            1,
            groupId,
            [
                CreatePlanCoach(1, "Jan Beta", 2, "A", 2),
                CreatePlanCoach(2, "Petr Havel", 1, "H", 1),
            ]);

        var block = Assert.Single(
            TrainingScheduleBlockFactory.CreateBlocks([laterCategory, earlierCategory]));

        Assert.Equal(
            "Havel (H), Beta (A), Beta (H), Alfa (A)",
            TrainingPlanCoachPresentation.Format(block.PlanCoachAssignments));
        Assert.Equal(4, block.PlanCoachAssignments.Count);
        Assert.Equal(
            [false, true, true, false],
            TrainingPlanCoachPresentation
                .GetDisplayItems(block.PlanCoachAssignments)
                .Select(item => item.IsDuplicate));
    }

    [Fact]
    public void CreateBlocks_UsesCoachSurnameBeforeCategoryCodeWhenCategoryOrdersMatch()
    {
        var groupId = Guid.NewGuid();
        var block = Assert.Single(TrainingScheduleBlockFactory.CreateBlocks(
        [
            CreateTrainingPlan(
                1,
                "U12",
                1,
                groupId,
                [CreatePlanCoach(2, "Petr Zulu", 1, "A", 1)]),
            CreateTrainingPlan(
                2,
                "U14",
                1,
                groupId,
                [CreatePlanCoach(1, "Jan Alfa", 1, "A", 1)]),
        ]));

        Assert.Equal(
            "Alfa (A), Zulu (A)",
            TrainingPlanCoachPresentation.Format(block.PlanCoachAssignments));
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

    private static TrainingPlanScheduleItemDto CreateTrainingPlan(
        int id,
        string category,
        int categoryOrder,
        Guid groupId,
        IReadOnlyList<TrainingPlanCoachDto> coaches)
        => new()
        {
            Id = id,
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2026, 9, 30),
            DayName = nameof(DayOfWeek.Monday),
            TimeFrom = new TimeOnly(16, 0),
            TimeTo = new TimeOnly(17, 0),
            GroupId = groupId,
            SeasonCategoryCode = category,
            SeasonCategoryOrder = categoryOrder,
            TrainingTypeName = "Led",
            TrainingPhaseName = "Sezóna",
            Coaches = coaches
                .Select(coach => new SimpleCoachDto
                {
                    FullName = coach.FullName,
                    LastName = coach.LastName,
                })
                .ToList(),
            CoachAssignments = coaches,
        };

    private static TrainingPlanCoachDto CreatePlanCoach(
        int coachId,
        string fullName,
        int roleId,
        string roleCode,
        int roleOrder)
        => new()
        {
            CoachId = coachId,
            FullName = fullName,
            LastName = fullName[(fullName.LastIndexOf(' ') + 1)..],
            CoachRoleId = roleId,
            CoachRoleCode = roleCode,
            CoachRoleOrder = roleOrder,
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
