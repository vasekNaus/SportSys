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

        Assert.Equal(7, GetSingleBlock(model).EditItemId);
    }

    [Fact]
    public void Create_UsesLowestMemberIdForGroupedTrainingBlock()
    {
        var groupId = Guid.NewGuid();
        var source = CreateViewModel(
            CreateTraining(9, "U14", 2, groupId),
            CreateTraining(4, "U12", 1, groupId));

        var model = TrainingScheduleComponentModel.Create(source);

        Assert.Equal(4, GetSingleBlock(model).EditItemId);
    }

    [Fact]
    public void Create_DoesNotAddEditIdToTrainingPlanBlock()
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

        Assert.Null(GetSingleBlock(model).EditItemId);
    }

    private static TrainingScheduleItemDto CreateTraining(
        int id,
        string category,
        int categoryOrder,
        Guid groupId)
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

    private static TrainingScheduleViewModel CreateViewModel(
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
            items.Select(item => item.SeasonCategoryName).ToList());

    private static TrainingScheduleBlock GetSingleBlock(
        TrainingScheduleComponentModel model)
        => Assert.Single(Assert.Single(Assert.Single(model.Rows).Lanes));
}
