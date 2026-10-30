using SportSys.Contract.Models;
using SportSys.Razor.Models.TrainingSchedule;

namespace SportSys.Razor.Tests;

public class TrainingPlanCoachPresentationTests
{
    [Fact]
    public void Format_UsesRoleCodeAndReturnsDashForNoCoaches()
    {
        Assert.Equal(
            "Novák (H), Svoboda (Q)",
            TrainingPlanCoachPresentation.Format(
            [
                CreateCoach(2, "Anna Svoboda", 2, "Q", 2),
                CreateCoach(1, "Jan Novák", 1, "H", 1),
            ]));

        Assert.Equal(
            "-",
            TrainingPlanCoachPresentation.Format(Array.Empty<TrainingPlanCoachDto>()));
    }

    [Fact]
    public void Order_UsesCategoryThenRoleThenCzechSurnameAndStableIds()
    {
        var assignments = new[]
        {
            CreateEntry(2, 9, CreateCoach(1, "Ivo Ivan", 1, "H", 1)),
            CreateEntry(1, 20, CreateCoach(9, "Adam Zima", 2, "A", 2)),
            CreateEntry(1, 10, CreateCoach(8, "Zdeněk Hruška", 2, "A", 2)),
            CreateEntry(1, 11, CreateCoach(7, "Petr Chlad", 2, "A", 2)),
            CreateEntry(1, 12, CreateCoach(6, "Jan Havel", 1, "H", 1)),
        };

        var result = TrainingPlanCoachPresentation.Order(assignments);

        Assert.Equal(
            ["Havel (H)", "Hruška (A)", "Chlad (A)", "Zima (A)", "Ivan (H)"],
            result.Select(entry => $"{entry.Coach.LastName} ({entry.Coach.CoachRoleCode})"));
    }

    [Fact]
    public void GetDisplayItems_MarksRepeatedCoachIdsAndRetainsOrder()
    {
        var assignments = new[]
        {
            CreateEntry(1, 1, CreateCoach(1, "Jan Beta", 1, "H", 1)),
            CreateEntry(1, 2, CreateCoach(2, "Petr Alfa", 2, "A", 2)),
            CreateEntry(2, 3, CreateCoach(1, "Jan Beta", 2, "A", 2)),
        };

        var result = TrainingPlanCoachPresentation.GetDisplayItems(assignments);

        Assert.Equal(
            ["Beta (H)", "Alfa (A)", "Beta (A)"],
            result.Select(item => item.DisplayText));
        Assert.Equal([true, false, true], result.Select(item => item.IsDuplicate));
    }

    [Fact]
    public void GetDisplayItems_DoesNotTreatSameNameDifferentCoachIdsAsDuplicates()
    {
        var assignments = new[]
        {
            CreateEntry(1, 1, CreateCoach(1, "Jan Beta", 1, "H", 1)),
            CreateEntry(1, 2, CreateCoach(2, "Petr Beta", 1, "H", 1)),
        };

        var result = TrainingPlanCoachPresentation.GetDisplayItems(assignments);

        Assert.Equal(["Beta (H)", "Beta (H)"], result.Select(item => item.DisplayText));
        Assert.All(result, item => Assert.False(item.IsDuplicate));
    }

    private static TrainingSchedulePlanCoachAssignment CreateEntry(
        int categoryOrder,
        int planId,
        TrainingPlanCoachDto coach)
        => new()
        {
            SeasonCategoryOrder = categoryOrder,
            TrainingPlanId = planId,
            Coach = coach,
        };

    private static TrainingPlanCoachDto CreateCoach(
        int coachId,
        string fullName,
        int roleId,
        string roleCode,
        int roleOrder)
        => new()
        {
            CoachId = coachId,
            FullName = fullName,
            LastName = fullName[(fullName.LastIndexOf(' ') + 1)..],
            CoachRoleId = roleId,
            CoachRoleCode = roleCode,
            CoachRoleOrder = roleOrder,
        };
}
