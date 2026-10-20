using SportSys.Contract.Models;
using RequirementIndexModel = SportSys.Razor.Areas.sport.Pages.Training.Requirement.IndexModel;

namespace SportSys.Razor.Tests;

public class TrainingRequirementTests
{
    [Fact]
    public void CoachDisplayText_UsesDisplayNameAndRole()
    {
        var assignment = new TrainingRequirementCoachListItem
        {
            DisplayName = "Jan Novák",
            PersonalNumber = "123",
            CoachRoleName = "Hlavní trenér",
        };

        Assert.Equal("Jan Novák (Hlavní trenér)", assignment.DisplayText);
    }

    [Fact]
    public void CoachDisplayText_FallsBackToPersonalNumber()
    {
        var assignment = new TrainingRequirementCoachListItem
        {
            DisplayName = " ",
            PersonalNumber = "123",
            CoachRoleName = "Asistent",
        };

        Assert.Equal("123 (Asistent)", assignment.DisplayText);
    }

    [Fact]
    public void MatchCoachDisplayText_UsesDisplayNameAndRole()
    {
        var assignment = new MatchRequirementCoachListItem
        {
            DisplayName = "Jan Novák",
            PersonalNumber = "123",
            CoachRoleName = "Hlavní trenér",
        };

        Assert.Equal("Jan Novák (Hlavní trenér)", assignment.DisplayText);
    }

    [Fact]
    public void MatchCoachDisplayText_FallsBackToPersonalNumber()
    {
        var assignment = new MatchRequirementCoachListItem
        {
            DisplayName = " ",
            PersonalNumber = "123",
            CoachRoleName = "Asistent",
        };

        Assert.Equal("123 (Asistent)", assignment.DisplayText);
    }

    [Fact]
    public void CoachGroups_GroupByRoleAndSortLastNames()
    {
        TrainingRequirementCoachListItem Item(string name, int roleId, string role)
            => new() { DisplayName = name, CoachRoleId = roleId, CoachRoleName = role };

        var groups = SportSys.Razor.Models.RequirementCoachGroups.Group(
        [
            Item("Petr Vlasák", 6, "Kondiční"),
            Item("Václav Nauš", 1, "Hlavní"),
            Item("Jan Barčák", 1, "Hlavní"),
            Item("Karel Klaus", 2, "Asistent"),
            Item("Jan Barčák", 2, "Asistent"),
        ]);

        Assert.Equal([1, 2, 6], groups.Select(g => g.RoleId));
        Assert.Equal(["Barčák", "Nauš"], groups[0].LastNames);
        Assert.Equal(["Barčák", "Klaus"], groups[1].LastNames);
        Assert.Empty(SportSys.Razor.Models.RequirementCoachGroups.Group([]));
    }

    [Fact]
    public void CoachGroups_OrderByRoleOrderNotId()
    {
        var groups = SportSys.Razor.Models.RequirementCoachGroups.Group(
        [
            new TrainingRequirementCoachListItem { DisplayName = "A B", CoachRoleId = 1, CoachRoleOrder = 2 },
            new TrainingRequirementCoachListItem { DisplayName = "C D", CoachRoleId = 5, CoachRoleOrder = 1 },
        ]);

        Assert.Equal([5, 1], groups.Select(g => g.RoleId));
    }

    [Fact]
    public void CoachRoleSeed_HasUniqueCodeAndOrder()
    {
        var roles = Enum.GetValues<SportSys.Database.Enums.ECoachRole>()
            .Select(e => new SportSys.Database.Models.dboSchema.CoachRole(e))
            .ToList();

        Assert.All(roles, role => Assert.Equal(1, role.Code.Length));
        Assert.Equal(roles.Count, roles.Select(role => role.Code).Distinct().Count());
        Assert.Equal(roles.Count, roles.Select(role => role.Order).Distinct().Count());
    }

    [Fact]
    public void CoachLastName_UsesLastWordOrPersonalNumber()
    {
        Assert.Equal("Nauš", new TrainingRequirementCoachListItem { DisplayName = "Václav Nauš" }.LastName);
        Assert.Equal("123", new TrainingRequirementCoachListItem { DisplayName = " ", PersonalNumber = "123" }.LastName);
    }

    [Fact]
    public void RequirementCoachItems_ShareCommonBase()
    {
        Assert.IsAssignableFrom<RequirementCoachListItem>(new TrainingRequirementCoachListItem());
        Assert.IsAssignableFrom<RequirementCoachListItem>(new MatchRequirementCoachListItem());
    }

    [Fact]
    public void NormalizeIds_RemovesInvalidAndDuplicateValues()
    {
        var availableItems = new[]
        {
            new LookupSelectItem { Id = 2, Name = "Druhý" },
            new LookupSelectItem { Id = 1, Name = "První" },
        };

        var result = RequirementIndexModel.NormalizeIds(
            [1, 3, 1, 2],
            availableItems);

        Assert.Equal([2, 1], result);
    }
}
