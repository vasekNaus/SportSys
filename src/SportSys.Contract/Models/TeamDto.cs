using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SportSys.Contract.Models;

public class TeamDto
{
    [HiddenInput(DisplayValue = false)]
    public int Id { get; set; }

    [Required(ErrorMessage = "Kód je povinný.")]
    [StringLength(5, ErrorMessage = "Kód nesmí přesáhnout 5 znaků.")]
    [Display(Name = "Kód")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Název je povinný.")]
    [StringLength(100, ErrorMessage = "Název nesmí přesáhnout 100 znaků.")]
    [Display(Name = "Název")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Adresa je povinná.")]
    [StringLength(200, ErrorMessage = "Adresa nesmí přesáhnout 200 znaků.")]
    [Display(Name = "Adresa")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Město je povinné.")]
    [StringLength(100, ErrorMessage = "Město nesmí přesáhnout 100 znaků.")]
    [Display(Name = "Město")]
    public string? City { get; set; }

    [UIHint("Select")]
    [Display(Name = "Domácí lokalita")]
    public int? HomeLocationId { get; set; }

    [Display(Name = "Aktivní")]
    public bool IsActive { get; set; } = true;
}

public class TeamListItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? HomeLocationName { get; set; }
    public bool IsActive { get; set; }
}
