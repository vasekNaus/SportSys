using SportSys.Contract.Models;
using System.ComponentModel.DataAnnotations;

namespace SportSys.Razor.Tests;

public class TrainingPlanEditDtoTests
{
    [Fact]
    public void Validate_RejectsReversedValidityRange()
    {
        var dto = CreateDto();
        dto.From = new DateOnly(2026, 9, 30);
        dto.To = new DateOnly(2026, 9, 1);

        var results = Validate(dto);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(TrainingPlanEditDto.From)) &&
            result.MemberNames.Contains(nameof(TrainingPlanEditDto.To)));
    }

    [Fact]
    public void Validate_RejectsNonIncreasingTimeRange()
    {
        var dto = CreateDto();
        dto.TimeTo = dto.TimeFrom;

        var results = Validate(dto);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(TrainingPlanEditDto.TimeFrom)) &&
            result.MemberNames.Contains(nameof(TrainingPlanEditDto.TimeTo)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("monday")]
    [InlineData("Funday")]
    public void Validate_RejectsInvalidDayName(string dayName)
    {
        var dto = CreateDto();
        dto.DayName = dayName;

        var results = Validate(dto);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(TrainingPlanEditDto.DayName)));
    }

    [Theory]
    [InlineData(nameof(DayOfWeek.Monday))]
    [InlineData(nameof(DayOfWeek.Tuesday))]
    [InlineData(nameof(DayOfWeek.Wednesday))]
    [InlineData(nameof(DayOfWeek.Thursday))]
    [InlineData(nameof(DayOfWeek.Friday))]
    [InlineData(nameof(DayOfWeek.Saturday))]
    [InlineData(nameof(DayOfWeek.Sunday))]
    public void Validate_AcceptsValidDayName(string dayName)
    {
        var dto = CreateDto();
        dto.DayName = dayName;

        var results = Validate(dto);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_RejectsWhitespaceLocation()
    {
        var dto = CreateDto();
        dto.Location = " ";

        var results = Validate(dto);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(TrainingPlanEditDto.Location)));
    }

    private static TrainingPlanEditDto CreateDto()
        => new()
        {
            Id = 1,
            OriginalVersion = "version",
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2026, 9, 30),
            DayName = nameof(DayOfWeek.Monday),
            TimeFrom = new TimeOnly(17, 0),
            TimeTo = new TimeOnly(18, 0),
            Location = "Zimní stadion",
        };

    private static List<ValidationResult> Validate(TrainingPlanEditDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        return results;
    }
}
