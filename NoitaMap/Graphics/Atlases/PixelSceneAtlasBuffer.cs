using System.Collections.Concurrent;
using CommunityToolkit.HighPerformance;
using NoitaMap.Map;
using NoitaMap.Viewer;

namespace NoitaMap.Graphics.Atlases;

public class PixelSceneAtlasBuffer : PackedAtlasedQuadBuffer
{
    public readonly List<PixelScene> PixelScenes = new List<PixelScene>();

    private readonly ConcurrentQueue<PixelScene> ThreadedPixelSceneQueue = new ConcurrentQueue<PixelScene>();

    public PixelSceneAtlasBuffer(ViewerDisplay viewerDisplay)
        : base(viewerDisplay)
    {

    }

    public void AddPixelScene(PixelScene pixelScene)
    {
        if (pixelScene.WorkingTextureData is null)
        {
            return;
        }

        ThreadedPixelSceneQueue.Enqueue(pixelScene);
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
        ResourcePosition resourcePosition = AddTextureToAtlas(pixelScene.TextureWidth, pixelScene.TextureHeight, pixelScene.TextureHash, pixelScene.WorkingTextureData.AsSpan());

        // Release working texture data, so that the GC can collect it
        pixelScene.WorkingTextureData = null;

        PixelScenes.Add(pixelScene);

        TransformBuffer.InsertInstance(resourcePosition.InstanceIndex, new VertexInstance()
        {
            Transform = pixelScene.WorldMatrix,
            TexturePosition = resourcePosition.UV,
            TextureSize = resourcePosition.UVSize
        });

        InstancesPerAtlas[resourcePosition.AtlasIndex]++;
    }
}
