using SportSys.Contract.Models;
using SportSys.Razor.Models.TrainingSchedule;

namespace SportSys.Razor.Tests;

public class ScheduleEventModelFactoryTests
{
    [Fact]
    public void CreateMatch_MapsOpponentResultAndNoEditTarget()
    {
        var scheduleEvent = ScheduleEventModelFactory.CreateMatch(new MatchScheduleItemDto
        {
            Id = 10,
            SeasonCategoryName = "Dorost",
            SeasonCategoryOrder = 2,
            TimeFrom = new TimeOnly(17, 0),
            TimeTo = new TimeOnly(19, 0),
            OpponentName = "HC Plzeň",
            HomeTeamName = "HC Klatovy",
            AwayTeamName = "HC Plzeň",
            HomeGoals = 3,
            AwayGoals = 2,
            IsHome = true,
            LocationName = "ZS Klatovy",
            MatchTypeName = "Liga",
        });

        Assert.Equal(ScheduleEventType.Match, scheduleEvent.EventType);
        Assert.Equal("HC Plzeň", scheduleEvent.DetailLine1);
        Assert.Equal("3:2", scheduleEvent.DetailLine2);
        Assert.Null(scheduleEvent.EditItemId);
        Assert.Null(scheduleEvent.EditPage);
    }

    [Fact]
    public void CreateMatch_UsesDashWhenResultIsUnknown()
    {
        var scheduleEvent = ScheduleEventModelFactory.CreateMatch(new MatchScheduleItemDto
        {
            Id = 10,
            SeasonCategoryName = "Dorost",
            OpponentName = "HC Plzeň",
            HomeTeamName = "HC Klatovy",
            AwayTeamName = "HC Plzeň",
            IsHome = true,
            LocationName = "ZS Klatovy",
            MatchTypeName = "Liga",
        });

        Assert.Equal("-", scheduleEvent.DetailLine2);
    }

    [Fact]
    public void CreateTrainings_PreservesGroupingStateAndEditTarget()
    {
        var groupId = Guid.NewGuid();
        var events = ScheduleEventModelFactory.CreateTrainings(
            [
                CreateTraining(9, "U14", 2, groupId, 1, "Plán"),
                CreateTraining(4, "U12", 1, groupId, 5, "Zrušený"),
            ],
            allowEditing: true);

        var scheduleEvent = Assert.Single(events);
        Assert.Equal("U12 + U14", scheduleEvent.TitleLine);
        Assert.Equal(4, scheduleEvent.EditItemId);
        Assert.Equal("/Training/Schedule/Edit", scheduleEvent.EditPage);
        Assert.Equal(TrainingStateVisual.UnknownIcon, scheduleEvent.StateIcon);
        Assert.Equal("❌ U12\n📅 U14", scheduleEvent.StateTooltip);
    }

    [Fact]
    public void Component_PutsOverlappingTrainingAndMatchIntoSeparateLanes()
    {
        var model = CreateComponent(
            CreateEvent(ScheduleEventType.Training, 1, 16, 18),
            CreateEvent(ScheduleEventType.Match, 2, 17, 19));

        Assert.Equal(2, Assert.Single(model.Rows).Lanes.Count);
    }

    [Fact]
    public void Component_PutsNonOverlappingTrainingAndMatchIntoSameLane()
    {
        var model = CreateComponent(
            CreateEvent(ScheduleEventType.Training, 1, 16, 17),
            CreateEvent(ScheduleEventType.Match, 2, 17, 19));

        Assert.Single(Assert.Single(model.Rows).Lanes);
        Assert.Equal(2, Assert.Single(Assert.Single(model.Rows).Lanes).Count);
    }

    [Fact]
    public void Component_PutsPointEventsAtSameTimeIntoSeparateLanes()
    {
        var model = CreateComponent(
            CreateEvent(ScheduleEventType.Match, 1, 17, 17),
            CreateEvent(ScheduleEventType.Match, 2, 17, 17));

        var row = Assert.Single(model.Rows);
        Assert.Equal(2, row.Lanes.Count);
        Assert.All(row.Lanes, lane => Assert.True(Assert.Single(lane).Width >= 3));
    }

    private static TrainingScheduleComponentModel CreateComponent(
        params ScheduleEventModel[] items)
        => TrainingScheduleComponentModel.Create(new TrainingScheduleViewModel(
            [
                new TrainingScheduleRow
                {
                    PrimaryLabel = "Po",
                    Parity = TrainingScheduleRowParity.Odd,
                    Items = items,
                },
            ],
            ["U12"],
            allowEditing: true));

    private static ScheduleEventModel CreateEvent(
        ScheduleEventType eventType,
        int id,
        int hourFrom,
        int hourTo)
        => new()
        {
            EventType = eventType,
            SourceId = id,
            SeasonCategoryOrder = 1,
            ColorKey = "U12",
            TitleLine = "U12",
            DetailLine1 = "Detail 1",
            DetailLine2 = "Detail 2",
            TimeFrom = new TimeOnly(hourFrom, 0),
            TimeTo = new TimeOnly(hourTo, 0),
            Tooltip = "Tooltip",
        };

    private static TrainingScheduleItemDto CreateTraining(
        int id,
        string category,
        int categoryOrder,
        Guid groupId,
        int stateId,
        string stateName)
        => new()
        {
            Id = id,
            Date = new DateOnly(2026, 9, 24),
            TimeFrom = new TimeOnly(17, 0),
            TimeTo = new TimeOnly(18, 0),
            GroupId = groupId,
            SeasonCategoryName = category,
            SeasonCategoryOrder = categoryOrder,
            TrainingTypeName = "Led",
            TrainingPhaseName = "Sezóna",
            TrainingStateId = stateId,
            TrainingStateName = stateName,
        };
}
