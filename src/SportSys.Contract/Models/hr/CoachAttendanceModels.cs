using System.ComponentModel.DataAnnotations;

namespace SportSys.Contract.Models.hr;

public class CoachAttendanceFilter : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Trenér není platný.")]
    [Display(Name = "Trenér")]
    public int? CoachId { get; set; }

    [Range(1, 9999, ErrorMessage = "Rok období od musí být v rozsahu 1 až 9999.")]
    [Display(Name = "Rok")]
    public int? PeriodFromYear { get; set; }

    [Range(1, 12, ErrorMessage = "Měsíc období od musí být v rozsahu 1 až 12.")]
    [Display(Name = "Měsíc")]
    public int? PeriodFromMonth { get; set; }

    [Range(1, 9999, ErrorMessage = "Rok období do musí být v rozsahu 1 až 9999.")]
    [Display(Name = "Rok")]
    public int? PeriodToYear { get; set; }

    [Range(1, 12, ErrorMessage = "Měsíc období do musí být v rozsahu 1 až 12.")]
    [Display(Name = "Měsíc")]
    public int? PeriodToMonth { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PeriodFromYear.HasValue != PeriodFromMonth.HasValue)
        {
            yield return new ValidationResult(
                "Období od musí obsahovat rok i měsíc.",
                [nameof(PeriodFromYear), nameof(PeriodFromMonth)]);
        }

        if (PeriodToYear.HasValue != PeriodToMonth.HasValue)
        {
            yield return new ValidationResult(
                "Období do musí obsahovat rok i měsíc.",
                [nameof(PeriodToYear), nameof(PeriodToMonth)]);
        }

        if (PeriodFromYear.HasValue
            && PeriodFromMonth.HasValue
            && PeriodToYear.HasValue
            && PeriodToMonth.HasValue
            && (PeriodFromYear.Value > PeriodToYear.Value
                || (PeriodFromYear.Value == PeriodToYear.Value
                    && PeriodFromMonth.Value > PeriodToMonth.Value)))
        {
            yield return new ValidationResult(
                "Období od nesmí být pozdější než období do.",
                [
                    nameof(PeriodFromYear),
                    nameof(PeriodFromMonth),
                    nameof(PeriodToYear),
                    nameof(PeriodToMonth),
                ]);
        }
    }
}

public class CoachAttendanceUploadDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Trenér je povinný.")]
    [Display(Name = "Trenér")]
    public int CoachId { get; set; }

    [Range(1, 9999, ErrorMessage = "Rok musí být v rozsahu 1 až 9999.")]
    [Display(Name = "Rok")]
    public int PeriodYear { get; set; }

    [Range(1, 12, ErrorMessage = "Měsíc musí být v rozsahu 1 až 12.")]
    [Display(Name = "Měsíc")]
    public int PeriodMonth { get; set; }
}

public class CoachAttendanceListItem
{
    public int Id { get; set; }
    public int CoachId { get; set; }
    public string CoachDisplayName { get; set; } = string.Empty;
    public string CoachPersonalNumber { get; set; } = string.Empty;
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string UploadedByDisplayName { get; set; } = string.Empty;

    public DateTime UploadedAtLocal =>
        DateTime.SpecifyKind(UploadedAt, DateTimeKind.Utc).ToLocalTime();
}

public class CoachAttendanceFileDto
{
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public class CoachSelectItem : UserSelectItem
{
    public string PersonalNumber { get; set; } = string.Empty;
}
