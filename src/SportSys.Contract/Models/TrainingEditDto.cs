using Microsoft.AspNetCore.Mvc;
using SportSys.Database.Enums;
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

    [ScaffoldColumn(false)]
    public List<TrainingCoachAssignmentInputDto> CoachAssignments { get; set; } = [];

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

public class TrainingCoachAssignmentInputDto : IValidatableObject
{
    [HiddenInput(DisplayValue = false)]
    public int? ExistingCoachId { get; set; }

    [HiddenInput(DisplayValue = false)]
    public int? ExistingTrainingId { get; set; }

    [HiddenInput(DisplayValue = false)]
    public int? ExistingParticipationTypeId { get; set; }

    public int? CoachId { get; set; }
    public int? TrainingId { get; set; }
    public int? ParticipationTypeId { get; set; }

    [StringLength(50, ErrorMessage = "Poznámka trenéra nesmí přesáhnout 50 znaků.")]
    public string? Note { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasExistingKey =
            ExistingCoachId.HasValue ||
            ExistingTrainingId.HasValue ||
            ExistingParticipationTypeId.HasValue;

        if (hasExistingKey)
        {
            if (ExistingCoachId is null or <= 0 ||
                ExistingTrainingId is null or <= 0 ||
                ExistingParticipationTypeId is null or <= 0 ||
                CoachId.HasValue ||
                TrainingId.HasValue)
            {
                yield return new ValidationResult(
                    "Existující přiřazení trenéra není platné.",
                    [
                        nameof(ExistingCoachId),
                        nameof(ExistingTrainingId),
                        nameof(ExistingParticipationTypeId),
                    ]);
            }
        }
        else if (CoachId is null or <= 0 || TrainingId is null or <= 0)
        {
            yield return new ValidationResult(
                "Vyberte trenéra a kategorii.",
                [nameof(CoachId), nameof(TrainingId)]);
        }

        if (ParticipationTypeId is null ||
            !Enum.IsDefined((EParticipationType)ParticipationTypeId.Value))
        {
            yield return new ValidationResult(
                "Vyberte platný stav docházky.",
                [nameof(ParticipationTypeId)]);
        }
    }
}

public class TrainingEditContextDto
{
    public required TrainingEditDto Input { get; init; }
    public required IReadOnlyList<TrainingEditMemberDto> Members { get; init; }
    public IReadOnlyList<TrainingEditCoachAssignmentDto> CoachAssignments { get; init; } = [];
    public IReadOnlyList<TrainingCoachSelectItemDto> AvailableCoaches { get; init; } = [];
    public IReadOnlyList<TrainingCategorySelectItemDto> AvailableCategories { get; init; } = [];
    public IReadOnlyList<TrainingParticipationTypeSelectItemDto> ParticipationTypes { get; init; } = [];
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

public class TrainingEditCoachAssignmentDto
{
    public int CoachId { get; init; }
    public int TrainingId { get; init; }
    public int ParticipationTypeId { get; init; }
    public required string CoachDisplayName { get; init; }
    public required string SeasonCategoryCode { get; init; }
    public required string ParticipationTypeName { get; init; }
    public required string Note { get; init; }
}

public class TrainingCoachSelectItemDto
{
    public int Id { get; init; }
    public required string DisplayName { get; init; }
}

public class TrainingCategorySelectItemDto
{
    public int TrainingId { get; init; }
    public required string CategoryCode { get; init; }
}

public class TrainingParticipationTypeSelectItemDto
{
    public int Id { get; init; }
    public required string DisplayName { get; init; }
    public bool IsDefault { get; init; }
}

internal readonly record struct CoachTrainingAssignmentKey(
    int CoachId,
    int TrainingId,
    int ParticipationTypeId);

internal sealed record CoachTrainingAssignmentUpdate(
    int CoachId,
    int TrainingId,
    int ParticipationTypeId,
    string Note,
    int? ExistingParticipationTypeId);

internal sealed record CoachTrainingAssignmentValidation(
    TrainingUpdateResult Result,
    IReadOnlyList<CoachTrainingAssignmentUpdate> Assignments);

public enum TrainingUpdateResult
{
    Success,
    NotFound,
    GroupInconsistent,
    LocationUnavailable,
    Conflict,
    InvalidInput,
    DuplicateCoachAssignment,
    CoachAssignmentRemovalNotAllowed,
}
