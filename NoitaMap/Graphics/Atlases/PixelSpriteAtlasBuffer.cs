using System.Collections.Concurrent;
using System.Numerics;
using NoitaMap.Map.Components;
using NoitaMap.Viewer;
using SixLabors.ImageSharp.Formats;

namespace NoitaMap.Graphics.Atlases;

public class PixelSpriteAtlasBuffer : PackedAtlasedQuadBuffer
{
    private readonly PathService PathService;

    private readonly List<PixelSpriteComponent> PixelSprites = new List<PixelSpriteComponent>();

    private readonly ConcurrentQueue<PixelSpriteComponent> ThreadedPixelSpriteQueue = new ConcurrentQueue<PixelSpriteComponent>();

    public PixelSpriteAtlasBuffer(ViewerDisplay viewerDisplay)
        : base(viewerDisplay)
    {
        PathService = viewerDisplay.PathService;
    }

    public void AddPixelSprite(PixelSpriteComponent pixelSprite)
    {
        if (pixelSprite.ImageFile is null)
        {
            return;
        }

        if (PathService.DataPath is null)
        {
            return;
        }

        string? path = null;

        if (pixelSprite.ImageFile.StartsWith("data/"))
        {
            path = Path.Combine(PathService.DataPath, pixelSprite.ImageFile.Remove(0, 5));
        }

        if (!File.Exists(path))
        {
            return;
        }

        ThreadedPixelSpriteQueue.Enqueue(pixelSprite);
    }

    public void Update()
    {
        bool needsUpdate = false;

        while (ThreadedPixelSpriteQueue.TryDequeue(out PixelSpriteComponent? pixelSprite))
        {
            ProcessPixelSprite(pixelSprite);

            needsUpdate = true;
        }

        if (needsUpdate)
        {
            TransformBuffer.UpdateInstanceBuffer();
        }
    }

    public void ProcessPixelSprite(PixelSpriteComponent pixelSprite)
    {
        if (pixelSprite.ImageFile is null)
        {
            throw new Exception("pixelSprite.ImageFile was null when it shouldn't have been");
        }

        string? path = null;

        if (pixelSprite.ImageFile.StartsWith("data/"))
        {
            path = Path.Combine(PathService.DataPath!, pixelSprite.ImageFile.Remove(0, 5));
        }

        using Image<Rgba32> image = LoadPixelSpriteImage(path!);

        image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory);

        ResourcePosition resourcePosition = AddTextureToAtlas(image.Width, image.Height, path!.GetHashCode(), memory.Span);

        PixelSprites.Add(pixelSprite);

        TransformBuffer.InsertInstance(resourcePosition.InstanceIndex, new VertexInstance()
        {
            Transform = Matrix4x4.CreateScale(image.Width, image.Height, 1f) * Matrix4x4.CreateTranslation(pixelSprite.Position.X - pixelSprite.AnchorX, pixelSprite.Position.Y - pixelSprite.AnchorY, 0f),
            TexturePosition = resourcePosition.UV,
            TextureSize = resourcePosition.UVSize
        });

        InstancesPerAtlas[resourcePosition.AtlasIndex]++;
    }

    private static Image<Rgba32> LoadPixelSpriteImage(string path)
    {
        Configuration configuration = Configuration.Default;

        configuration.PreferContiguousImageBuffers = true;

        Image<Rgba32> image = Image.Load<Rgba32>(new DecoderOptions() { Configuration = configuration }, path);

        return image;
    }
}
