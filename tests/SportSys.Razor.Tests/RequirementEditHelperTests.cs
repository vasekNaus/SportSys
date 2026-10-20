using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Database.Context;
using SportSys.Database.Models.sport;

namespace SportSys.Razor.Tests;

public class RequirementEditHelperTests
{
    private static readonly IReadOnlyDictionary<int, string> CoachNames =
        new Dictionary<int, string>
        {
            [1] = "Trenér A",
            [2] = "Trenér B",
        };

    [Fact]
    public void ValidateDuplicateAssignments_AllowsSameCoachInDifferentRoles()
    {
        RequirementCoachAssignmentInput[] assignments =
        [
            new() { CoachId = 1, CoachRoleId = 1, Hours = 2.5m },
            new() { CoachId = 1, CoachRoleId = 2, Hours = 1.25m },
        ];

        Assert.Null(RequirementEditHelper.ValidateDuplicateAssignments(assignments, CoachNames));
    }

    [Fact]
    public void ValidateDuplicateAssignments_RejectsSameCoachInSameRoleRegardlessOfHours()
    {
        RequirementCoachAssignmentInput[] assignments =
        [
            new() { CoachId = 1, CoachRoleId = 1, Hours = 2.5m },
            new() { CoachId = 1, CoachRoleId = 1, Hours = 1.25m },
        ];

        var result = RequirementEditHelper.ValidateDuplicateAssignments(assignments, CoachNames);

        Assert.NotNull(result);
        Assert.Equal(RequirementEditStatus.DuplicateCoach, result.Status);
        Assert.Contains(CoachNames[1], result.Message);
        Assert.Contains("ve stejné roli", result.Message);
    }

    [Fact]
    public void ValidateDuplicateAssignments_AllowsDifferentCoachesInSameRole()
    {
        RequirementCoachAssignmentInput[] assignments =
        [
            new() { CoachId = 1, CoachRoleId = 1 },
            new() { CoachId = 2, CoachRoleId = 1 },
        ];

        Assert.Null(RequirementEditHelper.ValidateDuplicateAssignments(assignments, CoachNames));
    }

    [Fact]
    public void ValidateDuplicateAssignments_AllowsEmptyAssignments()
        => Assert.Null(RequirementEditHelper.ValidateDuplicateAssignments([], CoachNames));

    [Theory]
    [InlineData(typeof(CoachTrainingRequirement), nameof(CoachTrainingRequirement.TrainingRequirementId))]
    [InlineData(typeof(CoachMatchRequirement), nameof(CoachMatchRequirement.MatchRequirementId))]
    public void RequirementAssignmentKey_ContainsCoachRequirementAndRole(Type entityType, string requirementKey)
    {
        var options = new DbContextOptionsBuilder<SportSysDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SportSysRequirementModel;Trusted_Connection=True;",
                sqlServer => sqlServer.UseNetTopologySuite())
            .Options;
        using var db = new SportSysDbContext(options);

        var key = db.Model.FindEntityType(entityType)?.FindPrimaryKey();

        Assert.NotNull(key);
        Assert.Equal(
            new[] { "CoachId", requirementKey, "CoachRoleId" }.Order(),
            key.Properties.Select(property => property.Name).Order());
    }

    [Fact]
    public void ValidateDuplicateAssignments_RejectsDuplicateMatchAssignmentWithoutHours()
    {
        RequirementCoachAssignmentInput[] assignments =
        [
            new() { CoachId = 1, CoachRoleId = 1 },
            new() { CoachId = 1, CoachRoleId = 1 },
        ];

        Assert.Equal(
            RequirementEditStatus.DuplicateCoach,
            RequirementEditHelper.ValidateDuplicateAssignments(assignments, CoachNames)?.Status);
    }
}
