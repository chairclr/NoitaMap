using System.Collections.Concurrent;
using System.Numerics;
using NoitaMap.Map;
using NoitaMap.Viewer;
using SixLabors.ImageSharp.Formats;
using Veldrid;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace NoitaMap.Graphics;

public class PixelSceneAtlasBuffer : AtlasedQuadBuffer
{
    private const int SingleAtlasSize = 8192;

    protected override IList<int> InstancesPerAtlas { get; } = new List<int>();

    private readonly List<PixelScene> PixelScenes = new List<PixelScene>();

    private readonly ConcurrentQueue<PixelScene> ThreadedPixelSceneQueue = new ConcurrentQueue<PixelScene>();

    private readonly Dictionary<string, RectangleF> MappedAtlasRegions = new Dictionary<string, RectangleF>();

    private readonly List<Rectangle> CachedAtlasRegions = new List<Rectangle>();

    private int CurrentAtlasX = 0;

    private int CurrentAtlasY = 0;

    public PixelSceneAtlasBuffer(ViewerDisplay viewerDisplay)
        : base(viewerDisplay)
    {
        CurrentAtlasTexture = CreateNewAtlas(SingleAtlasSize, SingleAtlasSize);

        AddAtlas(CurrentAtlasTexture);

        InstancesPerAtlas.Add(0);
    }

    public void AddPixelScene(PixelScene pixelScene)
    {
        if (pixelScene.AtlasTexturePath is null)
        {
            return;
        }

        if (!pixelScene.AtlasTexturePath.StartsWith($"data/biome_impl"))
        {
            return;
        }

        string path = Path.Combine("C:\\Users\\chair\\AppData\\LocalLow\\Nolla_Games_Noita", pixelScene.AtlasTexturePath);

        if (!File.Exists(path))
        {
            return;
        }

        bool backendSupportsMultithreading = GraphicsDevice.BackendType is GraphicsBackend.Direct3D11 or GraphicsBackend.Vulkan or GraphicsBackend.Metal;

        if (!backendSupportsMultithreading)
        {
            ThreadedPixelSceneQueue.Enqueue(pixelScene);
        }
        else
        {
            ProcessPixelScene(pixelScene);

            TransformBuffer.UpdateInstanceBuffer();
        }
    }

    public void Update()
    {
        bool needsUpdate = false;

        while (ThreadedPixelSceneQueue.TryDequeue(out PixelScene? pixelScene))
        {
            ProcessPixelScene(pixelScene);

            needsUpdate = true;
        }

        if (needsUpdate)
        {
            TransformBuffer.UpdateInstanceBuffer();
        }
    }

    public void ProcessPixelScene(PixelScene pixelScene)
    {
        lock (PixelScenes)
        {
            RectangleF rect = AddTextureToAtlas(pixelScene);

            PixelScenes.Add(pixelScene);

            TransformBuffer.AddInstance(new VertexInstance()
            {
                Transform = Matrix4x4.CreateScale(rect.Width * SingleAtlasSize, rect.Height * SingleAtlasSize, 1f) * Matrix4x4.CreateTranslation(pixelScene.X, pixelScene.Y, 0f),
                TexturePosition = new Vector2(rect.X, rect.Y),
                TextureSize = new Vector2(rect.Width, rect.Height)
            });

            InstancesPerAtlas[^1]++;
        }
    }

    private RectangleF AddTextureToAtlas(PixelScene pixelScene)
    {
        if (pixelScene.AtlasTexturePath is null)
        {
            throw new Exception("pixelScene.AtlasTexturePath was null when it shouldn't have been");
        }

        Rectangle rect;

        if (MappedAtlasRegions.TryGetValue(pixelScene.AtlasTexturePath, out RectangleF uv))
        {
            return uv;
        }

        Configuration configuration = Configuration.Default;

        configuration.PreferContiguousImageBuffers = true;

        string path = Path.Combine("C:\\Users\\chair\\AppData\\LocalLow\\Nolla_Games_Noita", pixelScene.AtlasTexturePath);

        using Image<Rgba32> image = Image.Load<Rgba32>(new DecoderOptions() { Configuration = configuration }, path);

        if (image.Width > SingleAtlasSize || image.Height > SingleAtlasSize)
        {
            throw new Exception("Texture larger than atlas");
        }

        if (CurrentAtlasX + image.Width >= SingleAtlasSize)
        {
            CurrentAtlasX = 0;

            CurrentAtlasY += image.Height;
        }

        if ((CurrentAtlasY + image.Height) >= SingleAtlasSize)
        {
            InstancesPerAtlas.Add(0);

            CurrentAtlasTexture = CreateNewAtlas(SingleAtlasSize, SingleAtlasSize);

            AddAtlas(CurrentAtlasTexture);

            CurrentAtlasX = 0;
            CurrentAtlasY = 0;

            CachedAtlasRegions.Clear();
        }

        rect = new Rectangle(CurrentAtlasX, CurrentAtlasY, image.Width, image.Height);

        CurrentAtlasX += image.Width;

        while (true)
        {
            if (CachedAtlasRegions.Any(x => rect.IntersectsWith(x)))
            {
                CurrentAtlasY++;

                rect.Y = CurrentAtlasY;

                if ((CurrentAtlasY + image.Height) >= SingleAtlasSize)
                {
                    CurrentAtlasTexture = CreateNewAtlas(SingleAtlasSize, SingleAtlasSize);

                    AddAtlas(CurrentAtlasTexture);

                    InstancesPerAtlas.Add(0);

                    CurrentAtlasX = 0;
                    CurrentAtlasY = 0;

                    CachedAtlasRegions.Clear();

                    MappedAtlasRegions.Clear();

                    break;
                }
            }
            else
            {
                break;
            }
        }

        CachedAtlasRegions.Add(rect);

        RectangleF rf = new RectangleF(rect.X / (float)SingleAtlasSize, rect.Y / (float)SingleAtlasSize, rect.Width / (float)SingleAtlasSize, rect.Height / (float)SingleAtlasSize);

        MappedAtlasRegions.Add(pixelScene.AtlasTexturePath, rf);

        image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory);

        GraphicsDevice.UpdateTexture(CurrentAtlasTexture, memory.Span, (uint)rect.X, (uint)rect.Y, 0, (uint)image.Width, (uint)image.Height, 1, 0, 0);

        return rf;
    }
}
