using System.ComponentModel.DataAnnotations;

namespace SportSys.Contract.Models.hr;

public class UserDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Uživatel je povinný.")]
    public int UserId { get; set; }

    [Display(Name = "Uživatelské jméno")]
    public string? UserName { get; set; }

    [StringLength(256, ErrorMessage = "Zobrazované jméno nesmí přesáhnout 256 znaků.")]
    [Display(Name = "Zobrazované jméno")]
    public string? DisplayName { get; set; }

    [EmailAddress(ErrorMessage = "E-mail nemá platný formát.")]
    [StringLength(256, ErrorMessage = "E-mail nesmí přesáhnout 256 znaků.")]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Telefonní číslo nemá platný formát.")]
    [StringLength(50, ErrorMessage = "Telefonní číslo nesmí přesáhnout 50 znaků.")]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }
}

public class CoachDetailDto : UserDto, IValidatableObject
{
    public int CoachId { get; set; }

    [Required(ErrorMessage = "Osobní číslo je povinné.")]
    [StringLength(20, ErrorMessage = "Osobní číslo nesmí přesáhnout 20 znaků.")]
    [Display(Name = "Osobní číslo")]
    public string PersonalNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rodné číslo je povinné.")]
    [StringLength(20, ErrorMessage = "Rodné číslo nesmí přesáhnout 20 znaků.")]
    [Display(Name = "Rodné číslo")]
    public string BirthNumber { get; set; } = string.Empty;

    public bool HasPhoto { get; set; }

    public string? PhotoFileName { get; set; }

    public List<CoachSettingDto> Settings { get; set; } = [];

    public List<CoachLicenseDto> Licenses { get; set; } = [];

    public List<CoachContractDto> Contracts { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var normalized = (BirthNumber ?? string.Empty)
            .Replace("/", string.Empty)
            .Replace(" ", string.Empty);
        if ((normalized.Length is not 9 and not 10) || normalized.Any(c => !char.IsDigit(c)))
        {
            yield return new ValidationResult(
                "Rodné číslo musí po odstranění lomítka a mezer obsahovat 9 nebo 10 číslic.",
                [nameof(BirthNumber)]);
        }
    }
}

public class CoachFilter
{
    [Display(Name = "Hledat")]
    public string? Search { get; set; }

    [Display(Name = "Sezóna")]
    public int? SeasonId { get; set; }

    [Display(Name = "Pouze s aktivní smlouvou")]
    public bool ActiveContractOnly { get; set; }
}

public class CoachListItem
{
    public int CoachId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PersonalNumber { get; set; } = string.Empty;
    public bool HasPhoto { get; set; }
    public List<string> CurrentLicenseNames { get; set; } = [];
    public List<string> ActiveContractNames { get; set; } = [];
}

public class CoachSettingDto : IValidatableObject
{
    public int Id { get; set; }
    public int CoachId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Platnost od")]
    public DateOnly ValidFrom { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Platnost do")]
    public DateOnly? ValidTo { get; set; }

    [RegularExpression(@"^\d{1,6}$", ErrorMessage = "Předčíslí musí obsahovat nejvýše 6 číslic.")]
    [Display(Name = "Předčíslí účtu")]
    public string? BankAccountPrefix { get; set; }

    [Required(ErrorMessage = "Číslo účtu je povinné.")]
    [RegularExpression(@"^\d{1,10}$", ErrorMessage = "Číslo účtu musí obsahovat nejvýše 10 číslic.")]
    [Display(Name = "Číslo účtu")]
    public string BankAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kód banky je povinný.")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Kód banky musí obsahovat 4 číslice.")]
    [Display(Name = "Kód banky")]
    public string BankCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ulice je povinná.")]
    [StringLength(200, ErrorMessage = "Ulice nesmí přesáhnout 200 znaků.")]
    [Display(Name = "Ulice")]
    public string Street { get; set; } = string.Empty;

    [Required(ErrorMessage = "Město je povinné.")]
    [StringLength(100, ErrorMessage = "Město nesmí přesáhnout 100 znaků.")]
    [Display(Name = "Město")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "PSČ je povinné.")]
    [StringLength(10, ErrorMessage = "PSČ nesmí přesáhnout 10 znaků.")]
    [Display(Name = "PSČ")]
    public string ZipCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kód zdravotní pojišťovny je povinný.")]
    [RegularExpression(@"^\d{3}$", ErrorMessage = "Kód zdravotní pojišťovny musí obsahovat 3 číslice.")]
    [Display(Name = "Zdravotní pojišťovna")]
    public string HealthInsuranceCode { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ValidFrom == default)
        {
            yield return new ValidationResult(
                "Datum začátku platnosti je povinné.",
                [nameof(ValidFrom)]);
        }

        if (ValidTo < ValidFrom)
        {
            yield return new ValidationResult(
                "Datum konce platnosti nesmí být před datem začátku.",
                [nameof(ValidFrom), nameof(ValidTo)]);
        }
    }
}

public class CoachLicenseDto : IValidatableObject
{
    public int Id { get; set; }
    public int CoachId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Typ licence je povinný.")]
    [Display(Name = "Typ licence")]
    public int CoachLicenseTypeId { get; set; }

    public string? CoachLicenseTypeName { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Platnost od")]
    public DateOnly ValidFrom { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Platnost do")]
    public DateOnly? ValidTo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ValidFrom == default)
        {
            yield return new ValidationResult(
                "Datum začátku platnosti je povinné.",
                [nameof(ValidFrom)]);
        }

        if (ValidTo < ValidFrom)
        {
            yield return new ValidationResult(
                "Datum konce platnosti nesmí být před datem začátku.",
                [nameof(ValidFrom), nameof(ValidTo)]);
        }
    }
}

public class CoachContractDto
{
    public int Id { get; set; }
    public int CoachId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Sezóna je povinná.")]
    [Display(Name = "Sezóna")]
    public int SeasonId { get; set; }

    public string? SeasonName { get; set; }

    [EnumDataType(typeof(ECoachContractType), ErrorMessage = "Neplatný typ smlouvy.")]
    [Display(Name = "Typ smlouvy")]
    public ECoachContractType ContractType { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Odměna nesmí být záporná.")]
    [Display(Name = "Odměna")]
    public decimal RewardAmount { get; set; }

    [Display(Name = "Aktivní")]
    public bool IsActive { get; set; } = true;
}

public class CoachLicenseTypeSelectItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class UserSelectItem
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class CoachPhotoDto
{
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
