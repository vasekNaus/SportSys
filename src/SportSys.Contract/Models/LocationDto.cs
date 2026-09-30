using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SportSys.Contract.Models;

public class LocationDto
{
    [HiddenInput(DisplayValue = false)]
    public int Id { get; set; }

    [Required(ErrorMessage = "Název je povinný.")]
    [StringLength(100, ErrorMessage = "Název nesmí přesáhnout 100 znaků.")]
    [Display(Name = "Název")]
    public string? Name { get; set; }

    [StringLength(200, ErrorMessage = "Ulice nesmí přesáhnout 200 znaků.")]
    [Display(Name = "Ulice")]
    public string? Street { get; set; }

    [StringLength(100, ErrorMessage = "Město nesmí přesáhnout 100 znaků.")]
    [Display(Name = "Město")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "PSČ nesmí přesáhnout 100 znaků.")]
    [Display(Name = "PSČ")]
    public string? ZipCode { get; set; }

    [Display(Name = "Aktivní")]
    public bool IsActive { get; set; } = true;
}
