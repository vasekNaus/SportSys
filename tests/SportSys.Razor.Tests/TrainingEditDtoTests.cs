using SportSys.Contract.Models;
using System.ComponentModel.DataAnnotations;

namespace SportSys.Razor.Tests;

public class TrainingEditDtoTests
{
    [Fact]
    public void Validate_RejectsNonIncreasingTimeRange()
    {
        var dto = CreateDto(
            new TimeOnly(17, 0),
            new TimeOnly(17, 0));

        var results = Validate(dto);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(TrainingEditDto.TimeFrom)) &&
            result.MemberNames.Contains(nameof(TrainingEditDto.TimeTo)));
    }

    [Fact]
    public void Validate_AcceptsValidTraining()
    {
        var dto = CreateDto(
            new TimeOnly(17, 0),
            new TimeOnly(18, 0));

        var results = Validate(dto);

        Assert.Empty(results);
    }

    private static TrainingEditDto CreateDto(TimeOnly timeFrom, TimeOnly timeTo)
        => new()
        {
            Id = 1,
            Date = new DateOnly(2026, 9, 8),
            TimeFrom = timeFrom,
            TimeTo = timeTo,
            Location = "Zimní stadion",
            Note = string.Empty,
        };

    private static List<ValidationResult> Validate(TrainingEditDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        return results;
    }
}
