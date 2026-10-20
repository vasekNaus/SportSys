using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SportSys.Database.Enums;

namespace SportSys.Database.Models.dboSchema;

public partial class CoachRole
{
    private CoachRole() { Name = null!; Code = null!; }

    [SetsRequiredMembers]
    public CoachRole(ECoachRole id)
    {
        Id   = (int)id;
        Name = Resources.ECoachRole.ResourceManager
                   .GetString(id.ToString(), CultureInfo.GetCultureInfo("cs"))
               ?? id.ToString();
        (Code, Order) = id switch
        {
            ECoachRole.Head            => ("H", 1),
            ECoachRole.Assistant       => ("A", 2),
            ECoachRole.Defenders       => ("O", 3),
            ECoachRole.Goalkeepers     => ("B", 4),
            ECoachRole.Physiotherapist => ("F", 5),
            ECoachRole.Conditioning    => ("K", 6),
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
        };
    }
}
