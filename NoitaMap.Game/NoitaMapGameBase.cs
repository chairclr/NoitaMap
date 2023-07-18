using NoitaMap.Game.Materials;
using NoitaMap.Resources;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.IO.Stores;
using osuTK;

namespace NoitaMap.Game;

public partial class NoitaMapGameBase : osu.Framework.Game
{
    [Cached]
    protected MaterialProvider MaterialProvider = new MaterialProvider();

    protected override Container<Drawable> Content { get; }

    protected NoitaMapGameBase()
    {
        base.Content.Add(Content = new DrawSizePreservingFillContainer
        {
            TargetDrawSize = new Vector2(1366, 768)
        });
    }

    [BackgroundDependencyLoader]
    private void Load()
    {
        Resources.AddStore(new DllResourceStore(NoitaMapResources.ResourceAssembly));

        MaterialProvider.LoadMaterials(Resources);
    }
}
