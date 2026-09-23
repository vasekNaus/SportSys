using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Services;
using SportSys.Database.Context;
using SportSys.Database.Models.sport;

namespace SportSys.Razor.Tests;

public class TrainingPlanValidityFilterTests
{
    private static readonly DateOnly ValidFrom = new(2026, 9, 1);
    private static readonly DateOnly ValidTo = new(2026, 9, 30);

    [Fact]
    public void ApplyValidityFilter_WithNullDate_ReturnsAllPlans()
    {
        var plans = CreatePlans().AsQueryable();

        var result = TrainingScheduleService
            .ApplyValidityFilter(plans, null)
            .Select(plan => plan.Id)
            .ToList();

        Assert.Equal([1, 2, 3], result);
    }

    [Theory]
    [InlineData(2026, 9, 1)]
    [InlineData(2026, 9, 15)]
    [InlineData(2026, 9, 30)]
    public void ApplyValidityFilter_WithDateInsideInclusiveRange_ReturnsPlan(
        int year,
        int month,
        int day)
    {
        var plans = new[] { CreatePlan(1, ValidFrom, ValidTo) }.AsQueryable();

        var result = TrainingScheduleService
            .ApplyValidityFilter(plans, new DateOnly(year, month, day))
            .Single();

        Assert.Equal(1, result.Id);
    }

    [Theory]
    [InlineData(2026, 8, 31)]
    [InlineData(2026, 10, 1)]
    public void ApplyValidityFilter_WithDateOutsideRange_ReturnsNoPlan(
        int year,
        int month,
        int day)
    {
        var plans = new[] { CreatePlan(1, ValidFrom, ValidTo) }.AsQueryable();

        var result = TrainingScheduleService
            .ApplyValidityFilter(plans, new DateOnly(year, month, day))
            .ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void ApplyValidityFilter_WithMultiplePlans_ReturnsOnlyValidPlans()
    {
        var plans = CreatePlans().AsQueryable();

        var result = TrainingScheduleService
            .ApplyValidityFilter(plans, new DateOnly(2026, 9, 15))
            .Select(plan => plan.Id)
            .ToList();

        Assert.Equal([1], result);
    }

    [Fact]
    public void ApplyValidityFilter_IsTranslatedBySqlServerProvider()
    {
        using var db = CreateDbContext();

        var query = TrainingScheduleService.ApplyValidityFilter(
            db.TrainingPlans,
            new DateOnly(2026, 9, 15));

        var sql = query.ToQueryString();

        Assert.Contains("[From]", sql);
        Assert.Contains("[To]", sql);
    }

    private static TrainingPlan[] CreatePlans()
        =>
        [
            CreatePlan(1, ValidFrom, ValidTo),
            CreatePlan(2, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)),
            CreatePlan(3, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)),
        ];

    private static TrainingPlan CreatePlan(int id, DateOnly from, DateOnly to)
        => new()
        {
            Id = id,
            From = from,
            To = to,
            SeasonCategoryName = string.Empty,
            Location = string.Empty,
            DayName = DayOfWeek.Monday.ToString(),
        };

    private static SportSysDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SportSysDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SportSysTrainingPlanValidityFilter;Trusted_Connection=True;",
                sqlServer => sqlServer.UseNetTopologySuite())
            .Options;

        return new SportSysDbContext(options);
    }
}
