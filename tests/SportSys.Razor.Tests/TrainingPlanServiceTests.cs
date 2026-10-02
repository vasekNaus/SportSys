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

    [Fact]
    public void CreateVersion_ChangesWhenCoachAssignmentsChange()
    {
        var groupId = Guid.NewGuid();
        var withoutCoach = new[] { CreateMember(1, "U12") };
        var withCoach = new[] { CreateMember(1, "U12", coachIds: [5]) };

        var withoutCoachVersion = TrainingPlanService.CreateVersion(groupId, withoutCoach);
        var withCoachVersion = TrainingPlanService.CreateVersion(groupId, withCoach);

        Assert.NotEqual(withoutCoachVersion, withCoachVersion);
    }

    [Fact]
    public void CreateVersion_IsIndependentOfCoachIdOrderAndDuplicates()
    {
        var groupId = Guid.NewGuid();
        var ascendingMembers = new[] { CreateMember(1, "U12", coachIds: [1, 2]) };
        var descendingMembers = new[] { CreateMember(1, "U12", coachIds: [2, 1]) };
        var duplicateMembers = new[] { CreateMember(1, "U12", coachIds: [1, 2, 1]) };

        var ascending = TrainingPlanService.CreateVersion(groupId, ascendingMembers);
        var descending = TrainingPlanService.CreateVersion(groupId, descendingMembers);
        var withDuplicate = TrainingPlanService.CreateVersion(groupId, duplicateMembers);

        Assert.Equal(ascending, descending);
        Assert.Equal(ascending, withDuplicate);
    }

    [Fact]
    public void CreateVersion_ChangesWhenDifferentMemberCoachesChange()
    {
        var groupId = Guid.NewGuid();
        var original = new[]
        {
            CreateMember(1, "U12", coachIds: [1]),
            CreateMember(2, "U14", coachIds: [2]),
        };
        var changed = new[]
        {
            CreateMember(1, "U12", coachIds: [1]),
            CreateMember(2, "U14", coachIds: [3]),
        };

        var originalVersion = TrainingPlanService.CreateVersion(groupId, original);
        var changedVersion = TrainingPlanService.CreateVersion(groupId, changed);

        Assert.NotEqual(originalVersion, changedVersion);
    }

    [Fact]
    public void BuildRequestedCoachIdsByPlan_UngroupedPlanUsesSelectedCoachIds()
    {
        var dto = new TrainingPlanEditDto
        {
            Id = 1,
            SelectedCoachIds = [1, 2, 99],
        };

        var result = TrainingPlanService.BuildRequestedCoachIdsByPlan(
            dto, groupId: null, memberIds: [1], availableCoachIds: [1, 2]);

        Assert.Equal([1], result.Keys);
        Assert.Equal([1, 2], result[1]);
    }

    [Fact]
    public void BuildRequestedCoachIdsByPlan_GroupedPlanUsesMemberCoachAssignments()
    {
        var dto = new TrainingPlanEditDto
        {
            Id = 1,
            MemberCoachAssignments =
            [
                new TrainingPlanMemberCoachInputDto { TrainingPlanId = 1, CoachIds = [1] },
                new TrainingPlanMemberCoachInputDto { TrainingPlanId = 2, CoachIds = [2] },
            ],
        };

        var result = TrainingPlanService.BuildRequestedCoachIdsByPlan(
            dto, groupId: Guid.NewGuid(), memberIds: [1, 2], availableCoachIds: [1, 2]);

        Assert.Equal([1], result[1]);
        Assert.Equal([2], result[2]);
    }

    [Fact]
    public void BuildRequestedCoachIdsByPlan_GroupedPlanIgnoresUnknownMemberIds()
    {
        var dto = new TrainingPlanEditDto
        {
            Id = 1,
            MemberCoachAssignments =
            [
                new TrainingPlanMemberCoachInputDto { TrainingPlanId = 1, CoachIds = [1] },
                new TrainingPlanMemberCoachInputDto { TrainingPlanId = 999, CoachIds = [2] },
            ],
        };

        var result = TrainingPlanService.BuildRequestedCoachIdsByPlan(
            dto, groupId: Guid.NewGuid(), memberIds: [1, 2], availableCoachIds: [1, 2]);

        Assert.Equal([1, 2], result.Keys.OrderBy(id => id));
        Assert.Equal([1], result[1]);
        Assert.Empty(result[2]);
    }

    [Fact]
    public void HasDuplicateCoachAcrossPlans_ReturnsFalseWhenCoachesAreDistinct()
    {
        var coachIdsByPlan = new Dictionary<int, List<int>>
        {
            [1] = [1, 2],
            [2] = [3],
        };

        Assert.False(TrainingPlanService.HasDuplicateCoachAcrossPlans(coachIdsByPlan));
    }

    [Fact]
    public void HasDuplicateCoachAcrossPlans_ReturnsTrueWhenCoachAppearsInMultiplePlans()
    {
        var coachIdsByPlan = new Dictionary<int, List<int>>
        {
            [1] = [1, 2],
            [2] = [2],
        };

        Assert.True(TrainingPlanService.HasDuplicateCoachAcrossPlans(coachIdsByPlan));
    }

    [Fact]
    public void NormalizeCoachIds_RemovesInvalidAndDuplicateValues()
    {
        var result = TrainingPlanService.NormalizeCoachIds(
            [1, 3, 1, 2],
            [1, 2]);

        Assert.Equal([1, 2], result);
    }

    [Fact]
    public void ComputeCoachAssignmentDiff_AddsNewlySelectedCoaches()
    {
        var diff = TrainingPlanService.ComputeCoachAssignmentDiff(
            selectedCoachIds: [1, 2],
            existingCoachIds: [1]);

        Assert.Equal([2], diff.ToAdd);
        Assert.Equal([1], diff.ToKeep);
        Assert.Empty(diff.ToRemove);
    }

    [Fact]
    public void ComputeCoachAssignmentDiff_RemovesDeselectedCoaches()
    {
        var diff = TrainingPlanService.ComputeCoachAssignmentDiff(
            selectedCoachIds: [1],
            existingCoachIds: [1, 2]);

        Assert.Empty(diff.ToAdd);
        Assert.Equal([1], diff.ToKeep);
        Assert.Equal([2], diff.ToRemove);
    }

    [Fact]
    public void ComputeCoachAssignmentDiff_EmptySelectionRemovesAllAssignments()
    {
        var diff = TrainingPlanService.ComputeCoachAssignmentDiff(
            selectedCoachIds: [],
            existingCoachIds: [1, 2]);

        Assert.Empty(diff.ToAdd);
        Assert.Empty(diff.ToKeep);
        Assert.Equal([1, 2], diff.ToRemove);
    }

    [Fact]
    public void ComputeCoachAssignmentDiff_DoesNotDuplicateAssignments()
    {
        var diff = TrainingPlanService.ComputeCoachAssignmentDiff(
            selectedCoachIds: [1, 1],
            existingCoachIds: []);

        Assert.Equal([1], diff.ToAdd);
    }

    private static TrainingPlanEditMemberDto CreateMember(
        int id,
        string category,
        string location = "Zimní stadion",
        string dayName = nameof(DayOfWeek.Monday),
        IReadOnlyList<int>? coachIds = null)
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
            CoachIds = coachIds ?? [],
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
