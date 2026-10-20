using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SportSys.Database.Enums;

namespace SportSys.Database.Models.sportSchema;

public partial class MatchState
{
    private MatchState() { Name = null!; }

    [SetsRequiredMembers]
    public MatchState(EMatchState id)
    {
        Id   = (int)id;
        Name = Resources.EMatchState.ResourceManager
                   .GetString(id.ToString(), CultureInfo.GetCultureInfo("cs"))
               ?? id.ToString();
    }
}
