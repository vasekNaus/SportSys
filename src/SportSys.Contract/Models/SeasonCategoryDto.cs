using System.ComponentModel.DataAnnotations;

namespace SportSys.Contract.Models;

public class SeasonCategoryEditDto
{
    [ScaffoldColumn(false)]
    public int SeasonId { get; set; }

    [ScaffoldColumn(false)]
    [Required(ErrorMessage = "Kód je povinný.")]
    [StringLength(10, ErrorMessage = "Kód nesmí přesáhnout 10 znaků.")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Název je povinný.")]
    [StringLength(20, ErrorMessage = "Název nesmí přesáhnout 20 znaků.")]
    [Display(Name = "Název")]
    public string? Name { get; set; }

    [Display(Name = "Pořadí")]
    public int Order { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Délka zápasu musí být kladný počet minut.")]
    [Display(Name = "Délka zápasu (min)")]
    public int MatchDurationMinutes { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Délka rozbruslení nesmí být záporná.")]
    [Display(Name = "Délka rozbruslení (min)")]
    public int WarmupDurationMinutes { get; set; }

    [Required(ErrorMessage = "Kód soutěže je povinný.")]
    [StringLength(10, ErrorMessage = "Kód soutěže nesmí přesáhnout 10 znaků.")]
    [Display(Name = "Kód soutěže")]
    public string? CompetitionCode { get; set; }

    [Required(ErrorMessage = "Název týmu v soutěži je povinný.")]
    [StringLength(100, ErrorMessage = "Název týmu v soutěži nesmí přesáhnout 100 znaků.")]
    [Display(Name = "Název týmu v soutěži")]
    public string? CompetitionTeamName { get; set; }

    [Required(ErrorMessage = "Ročníky narození jsou povinné.")]
    [StringLength(4000, ErrorMessage = "Ročníky narození nesmí přesáhnout 4000 znaků.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Ročníky narození")]
    public string? BirthYears { get; set; }

    [Display(Name = "Aktivní")]
    public bool IsActive { get; set; } = true;
}

public class SeasonCategoryListItem
{
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public int MatchDurationMinutes { get; set; }
    public int WarmupDurationMinutes { get; set; }
    public string CompetitionCode { get; set; } = string.Empty;
    public string CompetitionTeamName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
