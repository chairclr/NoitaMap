using osu.Framework;
using osu.Framework.Platform;

namespace NoitaMap.Game.Tests;

public static class Program
{
    public static void Main()
    {
        using GameHost host = Host.GetSuitableDesktopHost("visual-tests");
        using NoitaMapTestBrowser game = new NoitaMapTestBrowser();

        host.Run(game);
    }
}
