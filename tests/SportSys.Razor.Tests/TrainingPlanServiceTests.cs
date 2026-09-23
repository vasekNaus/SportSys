using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Tests;

public class TrainingPlanServiceTests
{
    [Fact]
    public void HaveConsistentEditableValues_AcceptsMatchingGroup()
    {
        var members = new[]
        {
            CreateMember(1, "U12"),
            CreateMember(2, "U14"),
        };

        Assert.True(TrainingPlanService.HaveConsistentEditableValues(members));
    }

    [Theory]
    [InlineData("From")]
    [InlineData("To")]
    [InlineData("DayName")]
    [InlineData("TimeFrom")]
    [InlineData("TimeTo")]
    [InlineData("Location")]
    public void HaveConsistentEditableValues_RejectsDifferentEditableValue(
        string changedProperty)
    {
        var changed = CreateMember(2, "U14");
        changed = changedProperty switch
        {
            "From" => Clone(changed, from: changed.From.AddDays(1)),
            "To" => Clone(changed, to: changed.To.AddDays(1)),
            "DayName" => Clone(changed, dayName: nameof(DayOfWeek.Tuesday)),
            "TimeFrom" => Clone(changed, timeFrom: changed.TimeFrom.AddMinutes(30)),
            "TimeTo" => Clone(changed, timeTo: changed.TimeTo.AddMinutes(30)),
            "Location" => Clone(changed, location: "Malá hala"),
            _ => throw new ArgumentOutOfRangeException(nameof(changedProperty)),
        };

        var members = new[]
        {
            CreateMember(1, "U12"),
            changed,
        };

        Assert.False(TrainingPlanService.HaveConsistentEditableValues(members));
    }

    [Fact]
    public void CreateVersion_ChangesWhenMemberValueChanges()
    {
        var groupId = Guid.NewGuid();
        var original = new[]
        {
            CreateMember(1, "U12"),
            CreateMember(2, "U14"),
        };
        var changed = new[]
        {
            CreateMember(1, "U12"),
            CreateMember(2, "U14", dayName: nameof(DayOfWeek.Tuesday)),
        };

        var originalVersion = TrainingPlanService.CreateVersion(groupId, original);
        var changedVersion = TrainingPlanService.CreateVersion(groupId, changed);

        Assert.NotEqual(originalVersion, changedVersion);
    }

    [Fact]
    public void CreateVersion_IsIndependentOfMemberOrder()
    {
        var groupId = Guid.NewGuid();
        var first = CreateMember(1, "U12");
        var second = CreateMember(2, "U14");

        var ascending = TrainingPlanService.CreateVersion(groupId, [first, second]);
        var descending = TrainingPlanService.CreateVersion(groupId, [second, first]);

        Assert.Equal(ascending, descending);
    }

    private static TrainingPlanEditMemberDto CreateMember(
        int id,
        string category,
        string location = "Zimní stadion",
        string dayName = nameof(DayOfWeek.Monday))
        => new()
        {
            Id = id,
            SeasonCategoryOrder = id,
            SeasonCategoryName = category,
            TrainingTypeName = "Led",
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2026, 9, 30),
            DayName = dayName,
            TimeFrom = new TimeOnly(17, 0),
            TimeTo = new TimeOnly(18, 0),
            Location = location,
        };

    private static TrainingPlanEditMemberDto Clone(
        TrainingPlanEditMemberDto source,
        DateOnly? from = null,
        DateOnly? to = null,
        string? dayName = null,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null,
        string? location = null)
        => new()
        {
            Id = source.Id,
            SeasonCategoryOrder = source.SeasonCategoryOrder,
            SeasonCategoryName = source.SeasonCategoryName,
            TrainingTypeName = source.TrainingTypeName,
            From = from ?? source.From,
            To = to ?? source.To,
            DayName = dayName ?? source.DayName,
            TimeFrom = timeFrom ?? source.TimeFrom,
            TimeTo = timeTo ?? source.TimeTo,
            Location = location ?? source.Location,
        };
}
