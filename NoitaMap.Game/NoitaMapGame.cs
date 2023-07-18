using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Screens;

namespace NoitaMap.Game;

public partial class NoitaMapGame : NoitaMapGameBase
{
    private ScreenStack ScreenStack;

    [BackgroundDependencyLoader]
    private void Load()
    {
        Child = ScreenStack = new ScreenStack();
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        ScreenStack.Push(new MapViewerScreen());
    }
}
