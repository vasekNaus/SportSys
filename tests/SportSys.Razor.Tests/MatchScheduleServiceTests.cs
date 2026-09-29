using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Database.Context;
using SportSys.Database.Models.sport;

namespace SportSys.Razor.Tests;

public class MatchScheduleServiceTests
{
    [Fact]
    public void ContractModels_UseSportEventInheritanceOnlyForDatedEvents()
    {
        Assert.IsAssignableFrom<SportEventDto>(new TrainingScheduleItemDto());
        Assert.IsAssignableFrom<SportEventDto>(new MatchScheduleItemDto());
        Assert.IsNotAssignableFrom<SportEventDto>(new TrainingPlanScheduleItemDto());
    }

    [Fact]
    public void CreateDto_WhenOwnTeamIsHome_SelectsAwayOpponent()
    {
        var dto = MatchScheduleService.CreateDto(
            CreateProjection(homeTeam: "HC Klatovy", awayTeam: "HC Plzeň"));

        Assert.True(dto.IsHome);
        Assert.Equal("HC Plzeň", dto.OpponentName);
        Assert.Equal(3, dto.HomeGoals);
        Assert.Equal(2, dto.AwayGoals);
    }

    [Fact]
    public void CreateDto_WhenOwnTeamIsAway_SelectsHomeOpponent()
    {
        var dto = MatchScheduleService.CreateDto(
            CreateProjection(homeTeam: "HC Plzeň", awayTeam: "HC Klatovy"));

        Assert.False(dto.IsHome);
        Assert.Equal("HC Plzeň", dto.OpponentName);
    }

    [Fact]
    public void CreateDto_UsesSameTeamSuffixFallbackAsCsvImport()
    {
        var projection = CreateProjection(
            homeTeam: "HC Klatovy",
            awayTeam: "HC Plzeň",
            competitionTeamName: " HC Klatovy B ");

        var dto = MatchScheduleService.CreateDto(projection);

        Assert.True(dto.IsHome);
        Assert.Equal("HC Plzeň", dto.OpponentName);
    }

    [Fact]
    public void CreateDto_WhenOwnTeamIsNotUnique_ThrowsExplicitError()
    {
        var projection = CreateProjection(
            homeTeam: "HC Plzeň",
            awayTeam: "HC Rokycany");

        var exception = Assert.Throws<InvalidOperationException>(
            () => MatchScheduleService.CreateDto(projection));

        Assert.Contains("Zápas 15", exception.Message, StringComparison.Ordinal);
        Assert.Contains("HC Klatovy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Model_ConfiguresMatchDurationAsPersistedComputedColumn()
    {
        using var db = CreateDbContext();
        var matchType = db.Model.FindEntityType(typeof(Match));
        var duration = matchType?.FindProperty(nameof(Match.DurationMinutes));

        Assert.NotNull(duration);
        Assert.Equal(
            "(datediff(minute,[TimeFrom],[TimeTo]))",
            duration.GetComputedColumnSql());
        Assert.True(duration.GetIsStored());
        Assert.NotNull(matchType?.FindProperty(nameof(Match.TimeTo)));
    }

    private static MatchScheduleService.MatchProjection CreateProjection(
        string homeTeam,
        string awayTeam,
        string competitionTeamName = "HC Klatovy")
        => new()
        {
            Id = 15,
            SeasonId = 2026,
            SeasonCategoryName = "Dorost",
            SeasonCategoryOrder = 2,
            CompetitionTeamName = competitionTeamName,
            Date = new DateOnly(2026, 9, 24),
            TimeFrom = new TimeOnly(17, 0),
            TimeTo = new TimeOnly(19, 0),
            DurationMinutes = 120,
            Note = "Liga",
            LocationId = 1,
            LocationName = "ZS Klatovy",
            MatchTypeName = "Liga",
            HomeTeamName = homeTeam,
            AwayTeamName = awayTeam,
            Result = new MatchResult
            {
                HomeGoals = 3,
                AwayGoals = 2,
            },
        };

    private static SportSysDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SportSysDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SportSysMatchModel;Trusted_Connection=True;",
                sqlServer => sqlServer.UseNetTopologySuite())
            .Options;

        return new SportSysDbContext(options);
    }
}
