using SportSys.Contract.Models;
using SportSys.Razor.Models.TrainingSchedule;

namespace SportSys.Razor.Tests;

public class TrainingScheduleComponentModelTests
{
    [Fact]
    public void Create_AddsEditIdToTrainingBlock()
    {
        var source = CreateViewModel(
            new TrainingScheduleItemDto
            {
                Id = 7,
                Date = new DateOnly(2026, 9, 8),
                TimeFrom = new TimeOnly(17, 0),
                TimeTo = new TimeOnly(18, 0),
                SeasonCategoryCode = "U12",
                TrainingTypeName = "Led",
                TrainingPhaseName = "Sezóna",
            });

        var model = TrainingScheduleComponentModel.Create(source);

        var block = GetSingleBlock(model);
        Assert.Equal(7, block.EditItemId);
        Assert.Equal("/Training/Schedule/Edit", block.EditPage);
    }

    [Fact]
    public void Create_UsesLowestMemberIdForGroupedTrainingBlock()
    {
        var groupId = Guid.NewGuid();
        var source = CreateViewModel(
            CreateTraining(9, "U14", 2, groupId),
            CreateTraining(4, "U12", 1, groupId));

        var model = TrainingScheduleComponentModel.Create(source);

        var block = GetSingleBlock(model);
        Assert.Equal(4, block.EditItemId);
        Assert.Equal("/Training/Schedule/Edit", block.EditPage);
    }

    [Fact]
    public void Create_UsesLowestMemberIdForVisualizationGroup()
    {
        var visualizationGroupId = Guid.NewGuid();
        var first = CreateTraining(9, "U14", 2, null);
        first.VisualizationGroupId = visualizationGroupId;
        var second = CreateTraining(4, "U12", 1, null);
        second.VisualizationGroupId = visualizationGroupId;
        var source = CreateViewModel(first, second);

        var model = TrainingScheduleComponentModel.Create(source);

        var block = GetSingleBlock(model);
        Assert.Equal("U12 + U14", block.Title);
        Assert.Equal(4, block.EditItemId);
        Assert.Equal("/Training/Schedule/Edit", block.EditPage);
    }

    [Fact]
    public void Create_DisablesEditingWhenViewModelDoesNotAllowIt()
    {
        var source = CreateViewModel(
            allowEditing: false,
            new TrainingScheduleItemDto
            {
                Id = 7,
                Date = new DateOnly(2026, 9, 8),
                TimeFrom = new TimeOnly(17, 0),
                TimeTo = new TimeOnly(18, 0),
                SeasonCategoryCode = "U12",
                TrainingTypeName = "Led",
                TrainingPhaseName = "Sezóna",
            });

        var model = TrainingScheduleComponentModel.Create(source);

        var block = GetSingleBlock(model);
        Assert.Null(block.EditItemId);
        Assert.Null(block.EditPage);
    }

    [Fact]
    public void Create_AddsEditTargetToTrainingPlanBlock()
    {
        var source = CreateViewModel(
            new TrainingPlanScheduleItemDto
            {
                Id = 3,
                From = new DateOnly(2026, 9, 1),
                To = new DateOnly(2026, 9, 30),
                DayName = nameof(DayOfWeek.Monday),
                TimeFrom = new TimeOnly(17, 0),
                TimeTo = new TimeOnly(18, 0),
                SeasonCategoryCode = "U12",
                TrainingTypeName = "Led",
                TrainingPhaseName = "Sezóna",
            });

        var model = TrainingScheduleComponentModel.Create(source);

        var block = GetSingleBlock(model);
        Assert.Equal(3, block.EditItemId);
        Assert.Equal("/Training/Plan/Edit", block.EditPage);
    }

    [Fact]
    public void Create_UsesLowestMemberIdForGroupedTrainingPlanBlock()
    {
        var groupId = Guid.NewGuid();
        var source = CreateViewModel(
            CreateTrainingPlan(8, "U14", 2, groupId),
            CreateTrainingPlan(5, "U12", 1, groupId));

        var model = TrainingScheduleComponentModel.Create(source);

        var block = GetSingleBlock(model);
        Assert.Equal(5, block.EditItemId);
        Assert.Equal("/Training/Plan/Edit", block.EditPage);
    }

    [Fact]
    public void Create_MapsUniformStateIconToBlockWithoutCssClass()
    {
        var source = CreateViewModel(
            new TrainingScheduleItemDto
            {
                Id = 7,
                Date = new DateOnly(2026, 9, 8),
                TimeFrom = new TimeOnly(17, 0),
                TimeTo = new TimeOnly(18, 0),
                SeasonCategoryCode = "U12",
                TrainingTypeName = "Led",
                TrainingPhaseName = "Sezóna",
                TrainingStateId = 5,
                TrainingStateName = "Zrušený",
            });

        var model = TrainingScheduleComponentModel.Create(source);

        var block = GetSingleBlock(model);
        var trainingEvent = Assert.IsType<TrainingEventModel>(block.Source);
        Assert.Equal("❌", trainingEvent.StateIcon);
        Assert.Null(trainingEvent.StateTooltip);
    }

    [Fact]
    public void Create_MixedStatesShowUnknownIconWithTooltip()
    {
        var groupId = Guid.NewGuid();
        var first = CreateTraining(9, "U14", 2, groupId);
        first.TrainingStateId = 1;
        first.TrainingStateName = "Plán";
        var second = CreateTraining(4, "U12", 1, groupId);
        second.TrainingStateId = 5;
        second.TrainingStateName = "Zrušený";
        var source = CreateViewModel(first, second);

        var model = TrainingScheduleComponentModel.Create(source);

        var block = GetSingleBlock(model);
        var trainingEvent = Assert.IsType<TrainingEventModel>(block.Source);
        Assert.Equal(TrainingStateVisual.UnknownIcon, trainingEvent.StateIcon);
        Assert.Equal("❌ U12\n📅 U14", trainingEvent.StateTooltip);
    }

    [Fact]
    public void Create_TrainingPlanWithEmptyTitleHasEmptyPlanTitleSummary()
    {
        var source = CreateViewModel(
            new TrainingPlanScheduleItemDto
            {
                Id = 3,
                From = new DateOnly(2026, 9, 1),
                To = new DateOnly(2026, 9, 30),
                DayName = nameof(DayOfWeek.Monday),
                TimeFrom = new TimeOnly(17, 0),
                TimeTo = new TimeOnly(18, 0),
                SeasonCategoryCode = "U12",
                TrainingTypeName = "Led",
                TrainingPhaseName = "Sezóna",
                Title = string.Empty,
            });

        var model = TrainingScheduleComponentModel.Create(source);

        var block = GetSingleBlock(model);
        var planEvent = Assert.IsType<TrainingPlanEventModel>(block.Source);
        Assert.Equal(string.Empty, planEvent.PlanTitleSummary);
    }

    [Fact]
    public void Create_TrainingPlanWithTitleMapsPlanTitleSummary()
    {
        var source = CreateViewModel(
            new TrainingPlanScheduleItemDto
            {
                Id = 3,
                From = new DateOnly(2026, 9, 1),
                To = new DateOnly(2026, 9, 30),
                DayName = nameof(DayOfWeek.Monday),
                TimeFrom = new TimeOnly(17, 0),
                TimeTo = new TimeOnly(18, 0),
                SeasonCategoryCode = "U12",
                TrainingTypeName = "Led",
                TrainingPhaseName = "Sezóna",
                Title = "Testování",
            });

        var model = TrainingScheduleComponentModel.Create(source);

        var block = GetSingleBlock(model);
        var planEvent = Assert.IsType<TrainingPlanEventModel>(block.Source);
        Assert.Equal("Testování", planEvent.PlanTitleSummary);
    }

    [Fact]
    public void Create_GroupedTrainingPlansWithDifferentTitlesJoinDistinctValues()
    {
        var groupId = Guid.NewGuid();
        var first = CreateTrainingPlan(8, "U14", 2, groupId);
        first.Title = "Herní trénink";
        var second = CreateTrainingPlan(5, "U12", 1, groupId);
        second.Title = "Testování";
        var source = CreateViewModel(first, second);

        var model = TrainingScheduleComponentModel.Create(source);

        var block = GetSingleBlock(model);
        var planEvent = Assert.IsType<TrainingPlanEventModel>(block.Source);
        Assert.Equal("Herní trénink, Testování", planEvent.PlanTitleSummary);
    }

    [Fact]
    public void Create_GroupedTrainingPlanUsesSameOrderedCoachListInBlockAndTooltip()
    {
        var groupId = Guid.NewGuid();
        var firstCategory = CreateTrainingPlan(8, "U14", 2, groupId);
        firstCategory.CoachAssignments =
        [
            CreatePlanCoach(1, "Jan Beta", 1, "H", 1),
            CreatePlanCoach(2, "Adam Alfa", 2, "A", 2),
        ];
        var secondCategory = CreateTrainingPlan(5, "U12", 1, groupId);
        secondCategory.CoachAssignments =
        [
            CreatePlanCoach(1, "Jan Beta", 3, "A", 2),
            CreatePlanCoach(3, "Petr Havel", 1, "H", 1),
        ];
        var source = CreateViewModel(firstCategory, secondCategory);

        var model = TrainingScheduleComponentModel.Create(source);

        var block = GetSingleBlock(model);
        var planEvent = Assert.IsType<TrainingPlanEventModel>(block.Source);
        const string expected = "Havel (H), Beta (A), Beta (H), Alfa (A)";
        Assert.Equal(expected, planEvent.CoachSurnameSummary);
        Assert.Contains($"Trenéři: {expected}", planEvent.Tooltip);
        Assert.Equal(1, CountOccurrences(planEvent.Tooltip, "Trenéři:"));
    }

    [Fact]
    public void Create_VisualizationGroupedPlansKeepCoachAssignments()
    {
        var visualizationGroupId = Guid.NewGuid();
        var firstCategory = CreateTrainingPlan(8, "U14", 2, Guid.NewGuid());
        firstCategory.GroupId = null;
        firstCategory.VisualizationGroupId = visualizationGroupId;
        firstCategory.CoachAssignments = [CreatePlanCoach(1, "Jan Beta", 1, "H", 1)];
        var secondCategory = CreateTrainingPlan(5, "U12", 1, Guid.NewGuid());
        secondCategory.GroupId = null;
        secondCategory.VisualizationGroupId = visualizationGroupId;
        secondCategory.CoachAssignments = [CreatePlanCoach(1, "Jan Beta", 2, "A", 2)];

        var model = TrainingScheduleComponentModel.Create(
            CreateViewModel(firstCategory, secondCategory));

        var planEvent = Assert.IsType<TrainingPlanEventModel>(
            GetSingleBlock(model).Source);
        Assert.Equal("Beta (A), Beta (H)", planEvent.CoachSurnameSummary);
        Assert.Contains("Trenéři: Beta (A), Beta (H)", planEvent.Tooltip);
    }

    [Fact]
    public void Create_TrainingPlanWithoutCoachesShowsDashAndNoTooltipSection()
    {
        var model = TrainingScheduleComponentModel.Create(
            CreateViewModel(CreateTrainingPlan(
                3,
                "U12",
                1,
                Guid.NewGuid())));

        var planEvent = Assert.IsType<TrainingPlanEventModel>(
            GetSingleBlock(model).Source);

        Assert.Equal("-", planEvent.CoachSurnameSummary);
        Assert.DoesNotContain("Trenéři:", planEvent.Tooltip);
    }

    private static TrainingScheduleItemDto CreateTraining(
        int id,
        string category,
        int categoryOrder,
        Guid? groupId)
        => new()
        {
            Id = id,
            Date = new DateOnly(2026, 9, 8),
            TimeFrom = new TimeOnly(17, 0),
            TimeTo = new TimeOnly(18, 0),
            GroupId = groupId,
            SeasonCategoryCode = category,
            SeasonCategoryOrder = categoryOrder,
            TrainingTypeName = "Led",
            TrainingPhaseName = "Sezóna",
        };

    private static TrainingPlanScheduleItemDto CreateTrainingPlan(
        int id,
        string category,
        int categoryOrder,
        Guid groupId)
        => new()
        {
            Id = id,
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2026, 9, 30),
            DayName = nameof(DayOfWeek.Monday),
            TimeFrom = new TimeOnly(17, 0),
            TimeTo = new TimeOnly(18, 0),
            GroupId = groupId,
            SeasonCategoryCode = category,
            SeasonCategoryOrder = categoryOrder,
            TrainingTypeName = "Led",
            TrainingPhaseName = "Sezóna",
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

    private static int CountOccurrences(string text, string value)
        => (text.Length - text.Replace(value, string.Empty, StringComparison.Ordinal).Length)
            / value.Length;

    private static TrainingScheduleViewModel CreateViewModel(
        params ITrainingScheduleItem[] items)
        => CreateViewModel(allowEditing: true, items);

    private static TrainingScheduleViewModel CreateViewModel(
        bool allowEditing,
        params ITrainingScheduleItem[] items)
    {
        var events = items.All(item => item is TrainingScheduleItemDto)
            ? EventModelFactory.CreateTrainings(
                items.Cast<TrainingScheduleItemDto>().ToList(),
                allowEditing)
            : EventModelFactory.CreateTrainingPlans(
                items.Cast<TrainingPlanScheduleItemDto>().ToList(),
                allowEditing);

        return new(
            [
                new TrainingScheduleRow
                {
                    PrimaryLabel = "Út",
                    Parity = TrainingScheduleRowParity.Even,
                    Items = events,
                },
            ],
            items.Select(item => item.SeasonCategoryCode).ToList(),
            allowEditing);
    }

    private static TrainingScheduleBlock GetSingleBlock(
        TrainingScheduleComponentModel model)
        => Assert.Single(Assert.Single(Assert.Single(model.Rows).Lanes));
}
