using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Services;
using SportSys.Database.Context;
using SportSys.Database.Models.sport;

namespace SportSys.Razor.Tests;

public class CoachAssignmentQueryTests
{
    [Fact]
    public void FilterByCoachIds_UsesOrForTrainingsAndExcludesUnassignedRows()
    {
        var trainings = new[]
        {
            CreateTraining(1, 10),
            CreateTraining(2, 20),
            CreateTraining(3, 10, 20),
            CreateTraining(4),
            CreateTraining(5, 30),
        };

        var result = trainings.AsQueryable()
            .FilterByCoachIds([10, 20])
            .Select(training => training.Id)
            .ToList();

        Assert.Equal([1, 2, 3], result);
    }

    [Fact]
    public void FilterByCoachIds_RequiresCoachAssignmentToOverlapPlanValidity()
    {
        var plans = new[]
        {
            CreatePlan(1, (10, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30))),
            CreatePlan(2, (10, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31))),
            CreatePlan(3, (20, new DateOnly(2026, 9, 15), new DateOnly(2026, 10, 15))),
            CreatePlan(4),
        };

        var result = plans.AsQueryable()
            .FilterByCoachIds([10, 20])
            .Select(plan => plan.Id)
            .ToList();

        Assert.Equal([1, 3], result);
    }

    [Fact]
    public void FilterByCoachIds_UsesOrForMatchesAndExcludesUnassignedRows()
    {
        var matches = new[]
        {
            CreateMatch(1, 10),
            CreateMatch(2, 20),
            CreateMatch(3, 10, 20),
            CreateMatch(4),
            CreateMatch(5, 30),
        };

        var result = matches.AsQueryable()
            .FilterByCoachIds([10, 20])
            .Select(match => match.Id)
            .ToList();

        Assert.Equal([1, 2, 3], result);
    }

    [Fact]
    public void FilterByCoachIds_UsesOrForBothRequirementTypes()
    {
        var trainingRequirements = new[]
        {
            CreateTrainingRequirement(1, 10),
            CreateTrainingRequirement(2, 20),
            CreateTrainingRequirement(3, 10, 20),
            CreateTrainingRequirement(4),
        };
        var matchRequirements = new[]
        {
            CreateMatchRequirement(1, 10),
            CreateMatchRequirement(2, 20),
            CreateMatchRequirement(3, 10, 20),
            CreateMatchRequirement(4),
        };

        var trainingResult = trainingRequirements.AsQueryable()
            .FilterByCoachIds([10, 20])
            .Select(requirement => requirement.Id)
            .ToList();
        var matchResult = matchRequirements.AsQueryable()
            .FilterByCoachIds([10, 20])
            .Select(requirement => requirement.Id)
            .ToList();

        Assert.Equal([1, 2, 3], trainingResult);
        Assert.Equal([1, 2, 3], matchResult);
    }

    [Fact]
    public void FilterByCoachIds_LeavesRowsUnchangedWhenSelectionIsEmpty()
    {
        var training = CreateTraining(1);
        var plan = CreatePlan(2);
        var match = CreateMatch(3);
        var trainingRequirement = CreateTrainingRequirement(4);
        var matchRequirement = CreateMatchRequirement(5);

        Assert.Equal(
            [1],
            new[] { training }.AsQueryable()
                .FilterByCoachIds([])
                .Select(item => item.Id));
        Assert.Equal(
            [2],
            new[] { plan }.AsQueryable()
                .FilterByCoachIds([])
                .Select(item => item.Id));
        Assert.Equal(
            [3],
            new[] { match }.AsQueryable()
                .FilterByCoachIds([])
                .Select(item => item.Id));
        Assert.Equal(
            [4],
            new[] { trainingRequirement }.AsQueryable()
                .FilterByCoachIds([])
                .Select(item => item.Id));
        Assert.Equal(
            [5],
            new[] { matchRequirement }.AsQueryable()
                .FilterByCoachIds([])
                .Select(item => item.Id));
    }

    [Fact]
    public void FilterByCoachIds_TranslatesForAllSupportedQueries()
    {
        using var db = CreateDbContext();
        var queries = new[]
        {
            db.Training.FilterByCoachIds([10]).ToQueryString(),
            db.TrainingPlans.FilterByCoachIds([10]).ToQueryString(),
            db.Matches.FilterByCoachIds([10]).ToQueryString(),
            db.TrainingRequirements.FilterByCoachIds([10]).ToQueryString(),
            db.MatchRequirements.FilterByCoachIds([10]).ToQueryString(),
        };

        Assert.All(queries, query =>
            Assert.Contains("CoachId", query, StringComparison.OrdinalIgnoreCase));
    }

    private static Training CreateTraining(int id, params int[] coachIds)
        => new()
        {
            Id = id,
            SeasonCategoryCode = "U12",
            Note = string.Empty,
            CoachTrainings = coachIds
                .Select(coachId => new CoachTraining { CoachId = coachId, Note = string.Empty })
                .ToList(),
        };

    private static TrainingPlan CreatePlan(
        int id,
        params (int CoachId, DateOnly ValidFrom, DateOnly ValidTo)[] assignments)
        => new()
        {
            Id = id,
            SeasonCategoryCode = "U12",
            DayName = nameof(DayOfWeek.Monday),
            Title = string.Empty,
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2026, 9, 30),
            CoachTrainingPlans = assignments
                .Select(assignment => new CoachTrainingPlan
                {
                    CoachId = assignment.CoachId,
                    ValidFrom = assignment.ValidFrom,
                    ValidTo = assignment.ValidTo,
                })
                .ToList(),
        };

    private static Match CreateMatch(int id, params int[] coachIds)
        => new()
        {
            Id = id,
            SeasonCategoryCode = "U12",
            Note = string.Empty,
            CoachMatches = coachIds
                .Select(coachId => new CoachMatch { CoachId = coachId, Note = string.Empty })
                .ToList(),
        };

    private static TrainingRequirement CreateTrainingRequirement(int id, params int[] coachIds)
        => new()
        {
            Id = id,
            SeasonCategoryCode = "U12",
            CoachTrainingRequirements = coachIds
                .Select(coachId => new CoachTrainingRequirement { CoachId = coachId })
                .ToList(),
        };

    private static MatchRequirement CreateMatchRequirement(int id, params int[] coachIds)
        => new()
        {
            Id = id,
            SeasonCategoryCode = "U12",
            CoachMatchRequirements = coachIds
                .Select(coachId => new CoachMatchRequirement { CoachId = coachId })
                .ToList(),
        };

    private static SportSysDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SportSysDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SportSysCoachFilter;Trusted_Connection=True;",
                sqlServer => sqlServer.UseNetTopologySuite())
            .Options;

        return new SportSysDbContext(options);
    }
}
