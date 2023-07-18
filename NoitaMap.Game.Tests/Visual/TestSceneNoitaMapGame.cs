using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Platform;

namespace NoitaMap.Game.Tests.Visual;

[TestFixture]
public partial class TestSceneNoitaMapGame : NoitaMapTestScene
{
    // Add visual tests to ensure correct behaviour of your game: https://github.com/ppy/osu-framework/wiki/Development-and-Testing
    // You can make changes to classes associated with the tests and they will recompile and update immediately.

    private NoitaMapGame game;

    [BackgroundDependencyLoader]
    private void load(GameHost host)
    {
        game = new NoitaMapGame();
        game.SetHost(host);

        AddGame(game);
    }
}
