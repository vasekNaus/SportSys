using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportSys.Contract.Models.hr;

namespace SportSys.Contract.Models;

public class RequirementCoachAssignmentInput
{
    public int? CoachId { get; set; }

    public int? CoachRoleId { get; set; }

    [Range(typeof(decimal), "0", "999.99",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Hodiny musí být v intervalu 0,00 až 999,99.")]
    [Display(Name = "Hodiny")]
    public decimal? Hours { get; set; }
}

public abstract class RequirementEditDtoBase : IValidatableObject
{
    [HiddenInput(DisplayValue = false)]
    public int Id { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Platnost od")]
    public DateOnly From { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Platnost do")]
    public DateOnly To { get; set; }

    public List<RequirementCoachAssignmentInput> CoachAssignments { get; set; } = [];

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CoachAssignments is null)
        {
            yield return new ValidationResult(
                "Seznam trenérů je povinný.",
                [nameof(CoachAssignments)]);
            yield break;
        }

        if (From > To)
        {
            yield return new ValidationResult(
                "Začátek platnosti musí být dříve nebo stejně jako konec platnosti.",
                [nameof(From), nameof(To)]);
        }

        if (CoachAssignments.Any(assignment =>
                assignment is null ||
                !assignment.CoachId.HasValue ||
                !assignment.CoachRoleId.HasValue))
        {
            yield return new ValidationResult(
                "Každý řádek trenérů musí mít vyplněného trenéra i roli.",
                [nameof(CoachAssignments)]);
        }
    }
}

public class TrainingRequirementEditDto : RequirementEditDtoBase
{
    [Range(typeof(decimal), "0.01", "999.99",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Rozsah v hodinách musí být v intervalu 0,01 až 999,99.")]
    [Display(Name = "Rozsah v hodinách")]
    public decimal DurationHours { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
            yield return result;

        if (decimal.Round(DurationHours, 2) != DurationHours)
        {
            yield return new ValidationResult(
                "Rozsah v hodinách může mít nejvýše dvě desetinná místa.",
                [nameof(DurationHours)]);
        }

        if (CoachAssignments is null)
            yield break;

        for (var index = 0; index < CoachAssignments.Count; index++)
        {
            var assignment = CoachAssignments[index];
            if (assignment is null ||
                assignment.Hours is null ||
                assignment.Hours < 0 ||
                assignment.Hours > 999.99m ||
                decimal.Round(assignment.Hours.Value, 2) != assignment.Hours.Value)
            {
                yield return new ValidationResult(
                    "Hodiny trenéra musí být v intervalu 0,00 až 999,99 a mohou mít nejvýše dvě desetinná místa.",
                    [$"{nameof(CoachAssignments)}[{index}].{nameof(RequirementCoachAssignmentInput.Hours)}"]);
            }
        }
    }
}

public class MatchRequirementEditDto : RequirementEditDtoBase
{
    [Range(0, int.MaxValue, ErrorMessage = "Počet zápasů nesmí být záporný.")]
    [Display(Name = "Počet zápasů")]
    public int MatchCount { get; set; }
}

public class RequirementEditContextDto<TInput> where TInput : RequirementEditDtoBase
{
    public required TInput Input { get; init; }
    public required string SeasonName { get; init; }
    public required string SeasonCategoryCode { get; init; }
    public string? TrainingTypeName { get; init; }
    public string? TrainingPhaseName { get; init; }
    public required IReadOnlyList<CoachSelectItem> AvailableCoaches { get; init; }
    public required IReadOnlyList<LookupSelectItem> CoachRoles { get; init; }

    public IReadOnlyList<SelectListItem> CoachItems => AvailableCoaches
        .Select(coach => new SelectListItem(coach.DisplayName, coach.Id.ToString()))
        .ToList();

    public IReadOnlyList<SelectListItem> RoleItems => CoachRoles
        .Select(role => new SelectListItem(role.Name, role.Id.ToString()))
        .ToList();
}

public enum RequirementEditStatus
{
    Success,
    NotFound,
    InvalidInput,
    DuplicateCoach,
}

public record RequirementEditResult(RequirementEditStatus Status, string? Message = null);
