using NoitaMap.Game;
using osu.Framework;
using osu.Framework.Platform;

namespace NoitaMap.Desktop;

public static class Program
{
    public static void Main()
    {
        using GameHost host = Host.GetSuitableDesktopHost(@"NoitaMap");
        using osu.Framework.Game game = new NoitaMapGame();
        host.Run(game);
    }
}
