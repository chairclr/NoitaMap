using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NoitaMap.Game.Map;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osuTK.Graphics;
using osuTK.Graphics.ES11;

namespace NoitaMap.Game;

public partial class MainScreen : Screen
{
    public string WorldPath;

    public ChunkContainer ChunkContainer = new ChunkContainer();

    public MainScreen()
    {
        string localLowPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low";

        WorldPath = Path.Combine(localLowPath, "Nolla_Games_Noita\\save00\\world");
    }

    [BackgroundDependencyLoader]
    private void Load()
    {
        InternalChildren = new Drawable[]
        {
            ChunkContainer
        };

        Task.Run(() =>
        {
            string[] chunkPaths = Directory.EnumerateFiles(WorldPath, "world_*_*.png_petri").ToArray();

            Parallel.ForEach(chunkPaths, chunkPath =>
            {
                ChunkContainer.LoadChunk(chunkPath);
            });
        });
    }
}

