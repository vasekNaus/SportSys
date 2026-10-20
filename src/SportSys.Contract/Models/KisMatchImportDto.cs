using System.ComponentModel.DataAnnotations;

namespace SportSys.Contract.Models;

public sealed class KisMatchImportDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Sezóna je povinná.")]
    [Display(Name = "Sezóna")]
    public int SeasonId { get; set; }
}
