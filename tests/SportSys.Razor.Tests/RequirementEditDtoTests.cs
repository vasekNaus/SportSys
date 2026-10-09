using System.ComponentModel.DataAnnotations;
using SportSys.Contract.Models;

namespace SportSys.Razor.Tests;

public class RequirementEditDtoTests
{
    private static List<ValidationResult> Validate(object dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void ValidTrainingRequirement_HasNoErrors()
    {
        var dto = new TrainingRequirementEditDto
        {
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2026, 12, 31),
            DurationHours = 10,
            CoachAssignments = [new() { CoachId = 1, CoachRoleId = 1 }],
        };

        Assert.Empty(Validate(dto));
    }

    [Fact]
    public void FromAfterTo_IsInvalid()
    {
        var dto = new MatchRequirementEditDto
        {
            From = new DateOnly(2026, 12, 31),
            To = new DateOnly(2026, 9, 1),
        };

        Assert.NotEmpty(Validate(dto));
    }

    [Fact]
    public void IncompleteCoachRow_IsInvalid()
    {
        var dto = new MatchRequirementEditDto
        {
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2026, 12, 31),
            CoachAssignments = [new() { CoachId = 1 }],
        };

        Assert.NotEmpty(Validate(dto));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void DurationHoursOutOfRange_IsInvalid(int hours)
    {
        var dto = new TrainingRequirementEditDto
        {
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2026, 12, 31),
            DurationHours = hours,
        };

        Assert.NotEmpty(Validate(dto));
    }

    [Fact]
    public void NegativeMatchCount_IsInvalid()
    {
        var dto = new MatchRequirementEditDto
        {
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2026, 12, 31),
            MatchCount = -1,
        };

        Assert.NotEmpty(Validate(dto));
    }
}
