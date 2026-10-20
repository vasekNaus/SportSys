using SportSys.Contract.Models;
using SportSys.Razor.Models.TrainingSchedule;

namespace SportSys.Razor.Tests;

public class CoachParticipationVisualTests
{
    [Theory]
    [InlineData(1, "✅")]
    [InlineData(2, "❓")]
    [InlineData(3, "❌")]
    [InlineData(4, "🔗")]
    [InlineData(5, "↔")]
    public void GetIcon_MapsEveryParticipationType(int id, string expectedIcon)
        => Assert.Equal(expectedIcon, CoachParticipationVisual.GetIcon(id));

    [Fact]
    public void CreateTrainings_MapsCoachStatusAndIncludesItInEventTooltip()
    {
        var participation = CreateParticipation(1, "Jan Novák", 2, "Bez reakce");
        var training = CreateTraining(1, "U12", null, participation);

        var trainingEvent = Assert.IsType<TrainingEventModel>(
            Assert.Single(EventModelFactory.CreateTrainings([training], allowEditing: false)));

        var displayedCoach = Assert.Single(trainingEvent.CoachParticipations);
        Assert.Equal("Novák", displayedCoach.LastName);
        Assert.Equal("❓", displayedCoach.Icon);
        Assert.Equal("Bez reakce", displayedCoach.ParticipationTypeName);
        Assert.Contains("Jan Novák – Bez reakce", trainingEvent.Tooltip, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateTrainings_PreservesDifferentParticipationsForCoachInGroupedTrainings()
    {
        var groupId = Guid.NewGuid();
        var first = CreateTraining(
            1,
            "U12",
            groupId,
            CreateParticipation(7, "Jan Novák", 1, "Účast"));
        var second = CreateTraining(
            2,
            "U14",
            groupId,
            CreateParticipation(7, "Jan Novák", 3, "Neúčast"));

        var trainingEvent = Assert.IsType<TrainingEventModel>(
            Assert.Single(EventModelFactory.CreateTrainings([first, second], allowEditing: false)));

        Assert.Equal(
            [("U12", "✅"), ("U14", "❌")],
            trainingEvent.CoachParticipations
                .Select(coach => (coach.CategoryCode, coach.Icon)));
        Assert.Contains(
            "U12 · Led · Sezóna · Trenéři: Jan Novák – Účast",
            trainingEvent.Tooltip,
            StringComparison.Ordinal);
        Assert.Contains(
            "U14 · Led · Sezóna · Trenéři: Jan Novák – Neúčast",
            trainingEvent.Tooltip,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CreateMatch_AddsCoachAttendanceWithoutReplacingMatchDetails()
    {
        var scheduleEvent = Assert.IsType<MatchEventModel>(
            EventModelFactory.CreateMatch(new MatchScheduleItemDto
            {
                Id = 15,
                SeasonCategoryCode = "Dorost",
                TimeFrom = new TimeOnly(17, 0),
                TimeTo = new TimeOnly(19, 0),
                HomeTeamName = "HC Klatovy",
                AwayTeamName = "HC Plzeň",
                OpponentName = "HC Plzeň",
                IsHome = true,
                HomeGoals = 3,
                AwayGoals = 2,
                LocationName = "ZS Klatovy",
                MatchTypeName = "Liga",
                MatchStateName = "Potvrzený",
                CoachParticipations =
                [
                    CreateParticipation(3, "Jan Novák", 4, "Spojený trénink"),
                ],
            }));

        var coach = Assert.Single(scheduleEvent.CoachParticipations);
        Assert.Equal("Novák", coach.LastName);
        Assert.Equal("🔗", coach.Icon);
        Assert.Equal("HC Plzeň", scheduleEvent.OpponentName);
        Assert.Equal("3:2", scheduleEvent.ResultText);
        Assert.Contains("Jan Novák – Spojený trénink", scheduleEvent.Tooltip, StringComparison.Ordinal);
        Assert.Contains("Potvrzený", scheduleEvent.Tooltip, StringComparison.Ordinal);
    }

    private static TrainingScheduleItemDto CreateTraining(
        int id,
        string category,
        Guid? groupId,
        CoachParticipationDto participation)
        => new()
        {
            Id = id,
            Date = new DateOnly(2026, 9, 24),
            TimeFrom = new TimeOnly(17, 0),
            TimeTo = new TimeOnly(18, 0),
            GroupId = groupId,
            SeasonCategoryCode = category,
            SeasonCategoryOrder = category == "U12" ? 1 : 2,
            TrainingTypeName = "Led",
            TrainingPhaseName = "Sezóna",
            Coaches =
            [
                new SimpleCoachDto
                {
                    FullName = participation.FullName,
                    LastName = participation.LastName,
                },
            ],
            CoachParticipations = [participation],
        };

    private static CoachParticipationDto CreateParticipation(
        int coachId,
        string fullName,
        int participationTypeId,
        string participationTypeName)
        => new()
        {
            CoachId = coachId,
            FullName = fullName,
            LastName = "Novák",
            ParticipationTypeId = participationTypeId,
            ParticipationTypeName = participationTypeName,
        };
}
