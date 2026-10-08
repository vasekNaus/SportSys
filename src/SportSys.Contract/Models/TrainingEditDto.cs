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

    [Range(1, int.MaxValue, ErrorMessage = "Lokalita je povinná.")]
    [UIHint("Select")]
    [Display(Name = "Lokalita")]
    public int LocationId { get; set; }

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

    public IReadOnlyList<string> CategoryCodes => Members
        .Select(member => member.SeasonCategoryCode)
        .Distinct(StringComparer.Ordinal)
        .ToList();
}

public class TrainingEditMemberDto
{
    public int Id { get; init; }
    public int SeasonCategoryOrder { get; init; }
    public required string SeasonCategoryCode { get; init; }
    public required string TrainingTypeName { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly TimeFrom { get; init; }
    public TimeOnly TimeTo { get; init; }
    public int LocationId { get; init; }
    public required string LocationName { get; init; }
    public required string Note { get; init; }
}

public enum TrainingUpdateResult
{
    Success,
    NotFound,
    GroupInconsistent,
    LocationUnavailable,
    Conflict,
    InvalidInput,
}
