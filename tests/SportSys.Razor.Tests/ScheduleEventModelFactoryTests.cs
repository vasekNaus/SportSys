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
            SeasonCategoryCode = "Dorost",
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
            SeasonCategoryCode = "Dorost",
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

    [Fact]
    public void CreateTrainings_MapsCoachSurnamesLocationAndIsDryTraining()
    {
        var events = ScheduleEventModelFactory.CreateTrainings(
            [
                CreateTraining(
                    1,
                    "U12",
                    1,
                    groupId: null,
                    stateId: 1,
                    stateName: "Plán",
                    location: "Zimní stadion Klatovy",
                    coaches: ["Jan Novák"],
                    isDryTraining: true),
            ],
            allowEditing: false);

        var scheduleEvent = Assert.Single(events);
        Assert.Equal("Novák", scheduleEvent.DetailLine1);
        Assert.Equal("Zimní stadion Klatovy", scheduleEvent.DetailLine2);
        Assert.True(scheduleEvent.IsDryTraining);
    }

    [Fact]
    public void CreateMatch_IsNeverDryTraining()
    {
        var scheduleEvent = ScheduleEventModelFactory.CreateMatch(new MatchScheduleItemDto
        {
            Id = 10,
            SeasonCategoryCode = "Dorost",
            OpponentName = "HC Plzeň",
            HomeTeamName = "HC Klatovy",
            AwayTeamName = "HC Plzeň",
            IsHome = true,
            LocationName = "ZS Klatovy",
            MatchTypeName = "Liga",
        });

        Assert.False(scheduleEvent.IsDryTraining);
    }

    private static TrainingScheduleComponentModel CreateComponent(
        params EventModel[] items)
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

    private static EventModel CreateEvent(
        int id,
        int hourFrom,
        int hourTo)
        => new()
        {
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
        Guid? groupId,
        int stateId,
        string stateName,
        string location = "",
        IReadOnlyList<string>? coaches = null,
        bool isDryTraining = false)
        => new()
        {
            Id = id,
            Date = new DateOnly(2026, 9, 24),
            TimeFrom = new TimeOnly(17, 0),
            TimeTo = new TimeOnly(18, 0),
            GroupId = groupId,
            SeasonCategoryCode = category,
            SeasonCategoryOrder = categoryOrder,
            LocationName = location,
            TrainingTypeName = "Led",
            IsDryTraining = isDryTraining,
            TrainingPhaseName = "Sezóna",
            Coaches = (coaches ?? [])
                .Select(fullName => new SimpleCoachDto
                {
                    FullName = fullName,
                    LastName = ExtractLastName(fullName),
                })
                .ToList(),
            TrainingStateId = stateId,
            TrainingStateName = stateName,
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
