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
        // Add your top-level game components here.
        // A screen stack and sample screen has been provided for convenience, but you can replace it if you don't want to use screens.
        Child = ScreenStack = new ScreenStack { RelativeSizeAxes = Axes.Both };
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        ScreenStack.Push(new MainScreen());
    }
}
