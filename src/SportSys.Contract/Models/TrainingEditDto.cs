using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SportSys.Contract.Models;

public class TrainingEditDto : IValidatableObject
{
    [HiddenInput(DisplayValue = false)]
    public int Id { get; set; }

    [HiddenInput(DisplayValue = false)]
    public string OriginalVersion { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Datum")]
    public DateOnly Date { get; set; }

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

    [StringLength(50, ErrorMessage = "Poznámka nesmí přesáhnout 50 znaků.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Poznámka")]
    public string? Note { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TimeFrom >= TimeTo)
        {
            yield return new ValidationResult(
                "Čas začátku musí být dříve než čas konce.",
                [nameof(TimeFrom), nameof(TimeTo)]);
        }
    }
}

public class TrainingEditContextDto
{
    public required TrainingEditDto Input { get; init; }
    public required IReadOnlyList<TrainingEditMemberDto> Members { get; init; }
    public bool IsGrouped { get; init; }
    public bool CanEdit { get; init; }

    public IReadOnlyList<string> CategoryNames => Members
        .Select(member => member.SeasonCategoryName)
        .Distinct(StringComparer.Ordinal)
        .ToList();
}

public class TrainingEditMemberDto
{
    public int Id { get; init; }
    public int SeasonCategoryOrder { get; init; }
    public required string SeasonCategoryName { get; init; }
    public required string TrainingTypeName { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly TimeFrom { get; init; }
    public TimeOnly TimeTo { get; init; }
    public required string Location { get; init; }
    public required string Note { get; init; }
}

public enum TrainingUpdateResult
{
    Success,
    NotFound,
    GroupInconsistent,
    Conflict,
    InvalidInput,
}
