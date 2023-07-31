using System.Numerics;
using NoitaMap.Viewer;

namespace NoitaMap.Graphics;

public abstract class PackedAtlasedQuadBuffer : AtlasedQuadBuffer
{
    protected virtual int SingleAtlasSize => 8192;

    protected override IList<int> InstancesPerAtlas { get; } = new List<int>();

    private readonly Dictionary<int, ResourcePosition> MappedAtlasRegions = new Dictionary<int, ResourcePosition>();

    private readonly List<Rectangle> CachedAtlasRegions = new List<Rectangle>();

    private int CurrentAtlasX = 0;

    private int CurrentAtlasY = 0;

    public PackedAtlasedQuadBuffer(ViewerDisplay viewerDisplay)
        : base(viewerDisplay)
    {
        CurrentAtlasTexture = CreateNewAtlas(SingleAtlasSize, SingleAtlasSize);

        AddAtlas(CurrentAtlasTexture);

        InstancesPerAtlas.Add(0);
    }

    protected ResourcePosition AddTextureToAtlas(int width, int height, int hash, Span<Rgba32> texture)
    {
        if (MappedAtlasRegions.TryGetValue(hash, out ResourcePosition region))
        {
            return region;
        }

        if (width > SingleAtlasSize || height > SingleAtlasSize)
        {
            throw new Exception("Texture larger than atlas");
        }

        Rectangle rect = FindPosition(width, height);

        CachedAtlasRegions.Add(rect);

        int index = 0;
        for (int i = 0; i <= ResourceAtlases.Count - 1; i++)
        {
            index += InstancesPerAtlas[i];
        }

        ResourcePosition position = new ResourcePosition(new Vector2(rect.X, rect.Y) / new Vector2(SingleAtlasSize), new Vector2(width, height) / new Vector2(SingleAtlasSize), ResourceAtlases.Count - 1, index);

        MappedAtlasRegions.Add(hash, position);

        GraphicsDevice.UpdateTexture(CurrentAtlasTexture, texture, (uint)rect.X, (uint)rect.Y, 0, (uint)width, (uint)height, 1, 0, 0);


        return position;
    }

    private Rectangle FindPosition(int width, int height)
    {
        if (CurrentAtlasX + width >= SingleAtlasSize)
        {
            CurrentAtlasX = 0;

            CurrentAtlasY += height;
        }

        if ((CurrentAtlasY + height) >= SingleAtlasSize)
        {
            InstancesPerAtlas.Add(0);

            CurrentAtlasTexture = CreateNewAtlas(SingleAtlasSize, SingleAtlasSize);

            AddAtlas(CurrentAtlasTexture);

            CurrentAtlasX = 0;
            CurrentAtlasY = 0;

            CachedAtlasRegions.Clear();
        }

        Rectangle rect = new Rectangle(CurrentAtlasX, CurrentAtlasY, width, height);

        IEnumerable<Rectangle> intersecting = CachedAtlasRegions.Where(rect.IntersectsWith);

        if (!intersecting.Any())
        {
            return rect;
        }

        int maxY = intersecting.Max(x => x.Y + x.Height);

        if (maxY + height > SingleAtlasSize)
        {
            CurrentAtlasTexture = CreateNewAtlas(SingleAtlasSize, SingleAtlasSize);

            AddAtlas(CurrentAtlasTexture);

            InstancesPerAtlas.Add(0);

            CurrentAtlasX = 0;
            CurrentAtlasY = 0;

            CachedAtlasRegions.Clear();
        }
        else
        {
            rect.Y = maxY;
        }

        CurrentAtlasX += width;

        return rect;
    }
}

public readonly record struct ResourcePosition(Vector2 UV, Vector2 UVSize, int AtlasIndex, int InstanceIndex);