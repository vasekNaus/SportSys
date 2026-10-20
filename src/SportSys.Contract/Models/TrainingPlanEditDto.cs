using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using SportSys.Contract.Models.hr;

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

  [Range(1, int.MaxValue, ErrorMessage = "Lokalita je povinná.")]
  [UIHint("Select")]
  [Display(Name = "Lokalita")]
  public int LocationId { get; set; }

  [StringLength(100, ErrorMessage = "Název nesmí přesáhnout 100 znaků.")]
  [DisplayFormat(ConvertEmptyStringToNull = false)]
  [Display(Name = "Název")]
  public string Title { get; set; } = string.Empty;

  [Display(Name = "Přiřazení trenéři")]
  [ScaffoldColumn(false)]
  public List<TrainingPlanCoachAssignmentInputDto> CoachAssignments { get; set; } = [];

  /// <summary>
  /// Přiřazení trenérů a rolí per spojený trénink (jedna položka na člena skupiny).
  /// Používá se jen pro spojený plán (<see cref="TrainingPlanEditContextDto.IsGrouped"/>);
  /// u nespojeného plánu je zdrojem pravdy <see cref="CoachAssignments"/>.
  /// </summary>
  [ScaffoldColumn(false)]
  public List<TrainingPlanMemberCoachInputDto> MemberCoachAssignments { get; set; } = [];

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

/// <summary>
/// Vstupní položka pro model binding přiřazení trenérů a rolí konkrétního spojeného
/// tréninku (viz <see cref="TrainingPlanEditDto.MemberCoachAssignments"/>).
/// </summary>
public class TrainingPlanMemberCoachInputDto
{
  [HiddenInput(DisplayValue = false)]
  public int TrainingPlanId { get; set; }

  public List<TrainingPlanCoachAssignmentInputDto> CoachAssignments { get; set; } = [];
}

public class TrainingPlanCoachAssignmentInputDto : IValidatableObject
{
  public int? CoachId { get; set; }
  public int? CoachRoleId { get; set; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (CoachId is null or <= 0)
    {
      yield return new ValidationResult(
          "Vyberte trenéra.",
          [nameof(CoachId)]);
    }

    if (CoachRoleId is null or <= 0)
    {
      yield return new ValidationResult(
          "Vyberte roli trenéra.",
          [nameof(CoachRoleId)]);
    }
  }
}

public class TrainingPlanCoachAssignmentDto
{
  public int CoachId { get; init; }
  public required string CoachDisplayName { get; init; }
  public int CoachRoleId { get; init; }
  public required string CoachRoleName { get; init; }
}

public class TrainingPlanEditContextDto
{
  public required TrainingPlanEditDto Input { get; init; }
  public required IReadOnlyList<TrainingPlanEditMemberDto> Members { get; init; }
  public required IReadOnlyList<CoachSelectItem> AvailableCoaches { get; init; }
  public required IReadOnlyList<LookupSelectItem> CoachRoles { get; init; }
  public bool IsGrouped { get; init; }
  public bool CanEdit { get; init; }

  public IReadOnlyList<string> CategoryCodes => Members
      .Select(member => member.SeasonCategoryCode)
      .Distinct(StringComparer.Ordinal)
      .ToList();

  /// <summary>
  /// Dostupní trenéři jako <see cref="SelectListItem"/> pro formulář.
  /// </summary>
  public IReadOnlyList<SelectListItem> AvailableCoachItems => AvailableCoaches
      .Select(coach => new SelectListItem(coach.DisplayName, coach.Id.ToString()))
      .ToList();

  public IReadOnlyList<SelectListItem> CoachRoleItems => CoachRoles
      .Select(role => new SelectListItem(role.Name, role.Id.ToString()))
      .ToList();
}

public class TrainingPlanEditMemberDto
{
  public int Id { get; init; }
  public int SeasonCategoryOrder { get; init; }
  public required string SeasonCategoryCode { get; init; }
  public required string TrainingTypeName { get; init; }
  public DateOnly From { get; init; }
  public DateOnly To { get; init; }
  public required string DayName { get; init; }
  public TimeOnly TimeFrom { get; init; }
  public TimeOnly TimeTo { get; init; }
  public int LocationId { get; init; }

  /// <summary>
  /// Aktuální přiřazení trenérů a rolí tohoto konkrétního plánu (nezávisle na
  /// ostatních členech spojené skupiny).
  /// </summary>
  public IReadOnlyList<TrainingPlanCoachAssignmentDto> CoachAssignments { get; init; } = [];

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
  DuplicateCoachAssignment,
  LocationUnavailable,
}
