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
                SeasonCategoryName = "U12",
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
                SeasonCategoryName = "U12",
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
                SeasonCategoryName = "U12",
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
                SeasonCategoryName = "U12",
                TrainingTypeName = "Led",
                TrainingPhaseName = "Sezóna",
                TrainingStateId = 5,
                TrainingStateName = "Zrušený",
            });

        var model = TrainingScheduleComponentModel.Create(source);

        var block = GetSingleBlock(model);
        Assert.True(block.IsUniformState);
        Assert.Equal("❌", block.StateIcon);
        Assert.Null(block.StateTooltip);
        Assert.Single(block.CategorySegments);
        Assert.Equal("❌", block.CategorySegments[0].StateIcon);
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
        Assert.False(block.IsUniformState);
        Assert.Equal(TrainingStateVisual.UnknownIcon, block.StateIcon);
        Assert.Equal("❌ U12\n📅 U14", block.StateTooltip);
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
            SeasonCategoryName = category,
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
            SeasonCategoryName = category,
            SeasonCategoryOrder = categoryOrder,
            TrainingTypeName = "Led",
            TrainingPhaseName = "Sezóna",
        };

    private static TrainingScheduleViewModel CreateViewModel(
        params ITrainingScheduleItem[] items)
        => CreateViewModel(allowEditing: true, items);

    private static TrainingScheduleViewModel CreateViewModel(
        bool allowEditing,
        params ITrainingScheduleItem[] items)
        => new(
            [
                new TrainingScheduleRow
                {
                    PrimaryLabel = "Út",
                    Parity = TrainingScheduleRowParity.Even,
                    Items = items,
                },
            ],
            items.Select(item => item.SeasonCategoryName).ToList(),
            allowEditing);

    private static TrainingScheduleBlock GetSingleBlock(
        TrainingScheduleComponentModel model)
        => Assert.Single(Assert.Single(Assert.Single(model.Rows).Lanes));
}
