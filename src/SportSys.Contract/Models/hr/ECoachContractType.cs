using System.ComponentModel.DataAnnotations;

namespace SportSys.Contract.Models.hr;

public enum ECoachContractType : byte
{
    [Display(Name = "DPP")]
    Dpp = 1,

    [Display(Name = "OSVČ")]
    SelfEmployed = 2,
}
