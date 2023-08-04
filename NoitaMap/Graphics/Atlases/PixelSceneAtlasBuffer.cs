using System.Collections.Concurrent;
using System.Numerics;
using CommunityToolkit.HighPerformance;
using NoitaMap.Map;
using NoitaMap.Viewer;
using SixLabors.ImageSharp.Formats;

namespace NoitaMap.Graphics.Atlases;

public class PixelSceneAtlasBuffer : PackedAtlasedQuadBuffer
{
    private readonly List<PixelScene> PixelScenes = new List<PixelScene>();

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

        PixelScenes.Add(pixelScene);

        TransformBuffer.InsertInstance(resourcePosition.InstanceIndex, new VertexInstance()
        {
            Transform = pixelScene.WorldMatrix,
            TexturePosition = resourcePosition.UV,
            TextureSize = resourcePosition.UVSize
        });

        InstancesPerAtlas[resourcePosition.AtlasIndex]++;
    }

    private static Image<Rgba32> LoadPixelSceneImage(string path)
    {
        Configuration configuration = Configuration.Default;

        configuration.PreferContiguousImageBuffers = true;

        Image<Rgba32> image = Image.Load<Rgba32>(new DecoderOptions() { Configuration = configuration }, path);

        return image;
    }
}
