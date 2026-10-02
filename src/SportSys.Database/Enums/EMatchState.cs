using System.ComponentModel.DataAnnotations;

namespace SportSys.Database.Enums;

public enum EMatchState
{
    [Display(Name = "Plan", ResourceType = typeof(SportSys.Database.Resources.EMatchState))]
    Plan = 1,

    [Display(Name = "ConfirmedKis", ResourceType = typeof(SportSys.Database.Resources.EMatchState))]
    ConfirmedKis = 2,

    [Display(Name = "Cancelled", ResourceType = typeof(SportSys.Database.Resources.EMatchState))]
    Cancelled = 3
}
