using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportSys.Contract.Models.hr;

namespace SportSys.Contract.Models;

public class RequirementCoachAssignmentInput
{
    public int? CoachId { get; set; }

    public int? CoachRoleId { get; set; }
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
        if (From > To)
        {
            yield return new ValidationResult(
                "Začátek platnosti musí být dříve nebo stejně jako konec platnosti.",
                [nameof(From), nameof(To)]);
        }

        if (CoachAssignments.Any(assignment =>
                !assignment.CoachId.HasValue || !assignment.CoachRoleId.HasValue))
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
