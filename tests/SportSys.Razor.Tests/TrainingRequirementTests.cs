using SportSys.Contract.Models;
using SportSys.Razor.Areas.sport.Pages.Training;

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
    public void NormalizeIds_RemovesInvalidAndDuplicateValues()
    {
        var availableItems = new[]
        {
            new LookupSelectItem { Id = 2, Name = "Druhý" },
            new LookupSelectItem { Id = 1, Name = "První" },
        };

        var result = RequirementModel.NormalizeIds(
            [1, 3, 1, 2],
            availableItems);

        Assert.Equal([2, 1], result);
    }
}
