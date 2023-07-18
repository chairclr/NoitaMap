using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Platform;

namespace NoitaMap.Game.Tests.Visual;

[TestFixture]
public partial class TestSceneNoitaMapGame : NoitaMapTestScene
{
    private NoitaMapGame Game;

    [BackgroundDependencyLoader]
    private void Load(GameHost host)
    {
        Game = new NoitaMapGame();
        Game.SetHost(host);

        AddGame(Game);
    }
}
