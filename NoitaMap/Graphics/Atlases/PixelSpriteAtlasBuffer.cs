using System.Collections.Concurrent;
using CommunityToolkit.HighPerformance;
using NoitaMap.Map.Components;
using NoitaMap.Viewer;

namespace NoitaMap.Graphics.Atlases;

public class PixelSpriteAtlasBuffer : PackedAtlasedQuadBuffer
{
    public readonly List<PixelSpriteComponent> PixelSprites = new List<PixelSpriteComponent>();

    private readonly ConcurrentQueue<PixelSpriteComponent> ThreadedPixelSpriteQueue = new ConcurrentQueue<PixelSpriteComponent>();

    public PixelSpriteAtlasBuffer(Renderer viewerDisplay)
        : base(viewerDisplay)
    {

    }

    public void AddPixelSprite(PixelSpriteComponent pixelSprite)
    {
        if (pixelSprite.WorkingTextureData is null)
        {
            return;
        }

        if (!pixelSprite.Enabled)
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
        ResourcePosition resourcePosition;

        if (pixelSprite.WorkingTextureData is not null)
        {
            resourcePosition = AddTextureToAtlas(pixelSprite.TextureWidth, pixelSprite.TextureHeight, pixelSprite.TextureHash, pixelSprite.WorkingTextureData.AsSpan());
        }
        else
        {
            throw new InvalidOperationException("No texture data with pixel sprite component");
        }

        // Release working texture data, so that the GC can collect it
        pixelSprite.WorkingTextureData = null;

        PixelSprites.Add(pixelSprite);

        TransformBuffer.InsertInstance(resourcePosition.InstanceIndex, new VertexInstance()
        {
            Transform = pixelSprite.WorldMatrix,
            TexturePosition = resourcePosition.UV,
            TextureSize = resourcePosition.UVSize
        });

        InstancesPerAtlas[resourcePosition.AtlasIndex]++;
    }
}
