using System.Resources;

namespace SportSys.Database.Resources;

public static class EMatchState
{
    private static ResourceManager? _resourceManager;

    public static ResourceManager ResourceManager =>
        _resourceManager ??= new ResourceManager(
            "SportSys.Database.Resources.EMatchState",
            typeof(EMatchState).Assembly);
}
