using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SportSys.Contract.Models;

public class TrainingPlanEditDto : IValidatableObject
{
    [HiddenInput(DisplayValue = false)]
    public int Id { get; set; }

    [HiddenInput(DisplayValue = false)]
    public string OriginalVersion { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Platnost od")]
    public DateOnly From { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Platnost do")]
    public DateOnly To { get; set; }

    [Required(ErrorMessage = "Den je povinný.")]
    [UIHint("Select")]
    [Display(Name = "Den")]
    public string DayName { get; set; } = string.Empty;

    [DataType(DataType.Time)]
    [Display(Name = "Čas od")]
    public TimeOnly TimeFrom { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Čas do")]
    public TimeOnly TimeTo { get; set; }

    [Required(ErrorMessage = "Lokalita je povinná.")]
    [StringLength(100, ErrorMessage = "Lokalita nesmí přesáhnout 100 znaků.")]
    [Display(Name = "Lokalita")]
    public string Location { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From > To)
        {
            yield return new ValidationResult(
                "Začátek platnosti musí být dříve nebo stejně jako konec platnosti.",
                [nameof(From), nameof(To)]);
        }

        if (!IsValidDayName(DayName))
        {
            yield return new ValidationResult(
                "Vyberte platný den v týdnu.",
                [nameof(DayName)]);
        }

        if (TimeFrom >= TimeTo)
        {
            yield return new ValidationResult(
                "Čas začátku musí být dříve než čas konce.",
                [nameof(TimeFrom), nameof(TimeTo)]);
        }
    }

    public static bool IsValidDayName(string? dayName)
        => Enum.TryParse<DayOfWeek>(dayName, ignoreCase: false, out var day) &&
           Enum.IsDefined(day) &&
           dayName == day.ToString();
}

public class TrainingPlanEditContextDto
{
    public required TrainingPlanEditDto Input { get; init; }
    public required IReadOnlyList<TrainingPlanEditMemberDto> Members { get; init; }
    public bool IsGrouped { get; init; }
    public bool CanEdit { get; init; }

    public IReadOnlyList<string> CategoryNames => Members
        .Select(member => member.SeasonCategoryName)
        .Distinct(StringComparer.Ordinal)
        .ToList();
}

public class TrainingPlanEditMemberDto
{
    public int Id { get; init; }
    public int SeasonCategoryOrder { get; init; }
    public required string SeasonCategoryName { get; init; }
    public required string TrainingTypeName { get; init; }
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public required string DayName { get; init; }
    public TimeOnly TimeFrom { get; init; }
    public TimeOnly TimeTo { get; init; }
    public required string Location { get; init; }

    public string DayDisplayName
        => DayName switch
        {
            nameof(DayOfWeek.Monday) => "Pondělí",
            nameof(DayOfWeek.Tuesday) => "Úterý",
            nameof(DayOfWeek.Wednesday) => "Středa",
            nameof(DayOfWeek.Thursday) => "Čtvrtek",
            nameof(DayOfWeek.Friday) => "Pátek",
            nameof(DayOfWeek.Saturday) => "Sobota",
            nameof(DayOfWeek.Sunday) => "Neděle",
            _ => throw new InvalidOperationException(
                $"Tréninkový plán {Id} obsahuje neplatnou hodnotu DayName '{DayName}'."),
        };
}

public enum TrainingPlanUpdateResult
{
    Success,
    NotFound,
    GroupInconsistent,
    Conflict,
    InvalidInput,
}
