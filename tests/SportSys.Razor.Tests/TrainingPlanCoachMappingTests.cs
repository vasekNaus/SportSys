using SportSys.Contract.Models;
using SportSys.Contract.Services;

namespace SportSys.Razor.Tests;

public class TrainingPlanCoachMappingTests
{
    [Fact]
    public void AssignCoachesToPlans_FiltersByInclusiveValidityAndDeduplicatesByIdentityAndRole()
    {
        var plan = new TrainingPlanScheduleItemDto
        {
            Id = 10,
            From = new DateOnly(2026, 9, 10),
            To = new DateOnly(2026, 9, 20),
        };
        var assignments = new[]
        {
            CreateAssignment(10, 1, "Jan Hráč", 1, "H", 1, 2026, 9, 10, 2026, 9, 14),
            CreateAssignment(10, 1, "Jan Hráč", 1, "H", 1, 2026, 9, 15, 2026, 9, 20),
            CreateAssignment(10, 2, "Jan Hráč", 2, "A", 2, 2026, 9, 20, 2026, 9, 25),
            CreateAssignment(10, 3, "Jan Hráč", 2, "A", 2, 2026, 9, 1, 2026, 9, 10),
            CreateAssignment(10, 4, "Mimo plán", 3, "K", 0, 2026, 9, 1, 2026, 9, 9),
            CreateAssignment(99, 5, "Jiný plán", 4, "V", 0, 2026, 9, 10, 2026, 9, 20),
        };

        TrainingScheduleService.AssignCoachesToPlans([plan], assignments);

        Assert.Collection(
            plan.CoachAssignments,
            coach =>
            {
                Assert.Equal(1, coach.CoachId);
                Assert.Equal("Hráč", coach.LastName);
                Assert.Equal("H", coach.CoachRoleCode);
                Assert.Equal("H", coach.CoachRoleName);
                Assert.Equal(1, coach.CoachRoleOrder);
            },
            coach =>
            {
                Assert.Equal(2, coach.CoachId);
                Assert.Equal("A", coach.CoachRoleCode);
            },
            coach =>
            {
                Assert.Equal(3, coach.CoachId);
                Assert.Equal("A", coach.CoachRoleCode);
            });

        Assert.Single(plan.Coaches);
        Assert.Equal("Jan Hráč", plan.Coaches[0].FullName);
    }

    private static TrainingScheduleService.TrainingPlanCoachAssignmentRow CreateAssignment(
        int planId,
        int coachId,
        string fullName,
        int roleId,
        string roleCode,
        int roleOrder,
        int validFromYear,
        int validFromMonth,
        int validFromDay,
        int validToYear,
        int validToMonth,
        int validToDay)
        => new(
            planId,
            new DateOnly(validFromYear, validFromMonth, validFromDay),
            new DateOnly(validToYear, validToMonth, validToDay),
            coachId,
            fullName,
            roleId,
            roleCode,
            roleCode,
            roleOrder);
}
