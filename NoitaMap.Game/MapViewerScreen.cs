using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NoitaMap.Game.Map;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osuTK;
using osuTK.Input;

namespace NoitaMap.Game;

public partial class MapViewerScreen : Screen
{
    public string WorldPath;

    public ChunkContainer ChunkContainer = new ChunkContainer();

    private bool Panning = false;

    private Vector2 MouseTranslateOrigin = Vector2.Zero;

    public MapViewerScreen()
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

    protected override bool OnMouseMove(MouseMoveEvent e)
    {
        if (Panning)
        {
            Vector2 currentMousePosition = ScalePosition(e.ScreenSpaceMousePosition) + ChunkContainer.ViewOffset;

            ChunkContainer.ViewOffset += MouseTranslateOrigin - currentMousePosition;
        }

        return base.OnMouseMove(e);
    }

    protected override bool OnMouseDown(MouseDownEvent e)
    {
        if (e.Button == MouseButton.Left)
        {
            Panning = true;
        }

        MouseTranslateOrigin = ScalePosition(e.ScreenSpaceMousePosition) + ChunkContainer.ViewOffset;

        return base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseUpEvent e)
    {
        if (e.Button == MouseButton.Left)
        {
            Panning = false;
        }

        base.OnMouseUp(e);
    }

    protected override bool OnScroll(ScrollEvent e)
    {
        Vector2 originalScaledMouse = ScalePosition(e.ScreenSpaceMousePosition);

        ChunkContainer.ViewScale += new Vector2(e.ScrollDelta.Y) * (ChunkContainer.ViewScale / 10f);
        ChunkContainer.ViewScale = Vector2.Clamp(ChunkContainer.ViewScale, new Vector2(0.1f, 0.1f), new Vector2(20f, 20f));

        Vector2 currentScaledMouse = ScalePosition(e.ScreenSpaceMousePosition);

        // Zoom in on where the mouse is
        ChunkContainer.ViewOffset += originalScaledMouse - currentScaledMouse;

        return base.OnScroll(e);
    }

    private Vector2 ScalePosition(Vector2 position)
    {
        return Vector2.Divide(position, ChunkContainer.ViewScale);
    }
}

