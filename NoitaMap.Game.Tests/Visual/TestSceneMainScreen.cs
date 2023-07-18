using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Screens;

namespace NoitaMap.Game.Tests.Visual;

[TestFixture]
public partial class TestSceneMainScreen : NoitaMapTestScene
{
    public TestSceneMainScreen()
    {
        Add(new ScreenStack(new MapViewerScreen()));
    }
}
