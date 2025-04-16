using System.Collections.Concurrent;
using CommunityToolkit.HighPerformance;
using NoitaMap.Graphics;
using NoitaMap.Graphics.Atlases;

public class QuadObjectAtlasBuffer<T>(Renderer renderer) : PackedAtlasedQuadBuffer(renderer)
    where T : IAtlasObject
{
    public IReadOnlyList<T> AtlasObjects => LoadedAtlasObjects;

    private readonly List<T> LoadedAtlasObjects = new List<T>();

    private readonly ConcurrentQueue<T> AtlasObjectQueue = new ConcurrentQueue<T>();

    public void AddAtlasObject(T atlasObject)
    {
        if (atlasObject.WorkingTextureData is null)
        {
            return;
        }

        AtlasObjectQueue.Enqueue(atlasObject);
    }

    public void AddAtlasObjects(T[] atlasObjects)
    {
        foreach (T atlasObject in atlasObjects)
        {
            AddAtlasObject(atlasObject);
        }
    }

    public void Update()
    {
        bool needsUpdate = false;

        while (AtlasObjectQueue.TryDequeue(out T? atlasObject))
        {
            ProcessAtlasObject(atlasObject);

            needsUpdate = true;
        }

        if (needsUpdate)
        {
            TransformBuffer.UpdateInstanceBuffer();
        }
    }

    public void ProcessAtlasObject(T atlasObject)
    {
        ResourcePosition resourcePosition;

        if (atlasObject.WorkingTextureData is not null)
        {
            resourcePosition = AddTextureToAtlas(atlasObject.TextureWidth, atlasObject.TextureHeight, atlasObject.TextureHash, atlasObject.WorkingTextureData.AsSpan());
        }
        else
        {
            throw new InvalidOperationException("Invalid texture data for atlas object");
        }

        // Release working texture data, so that the GC can collect it
        atlasObject.WorkingTextureData = null;

        LoadedAtlasObjects.Add(atlasObject);

        TransformBuffer.InsertInstance(resourcePosition.InstanceIndex, new VertexInstance()
        {
            Transform = atlasObject.WorldMatrix,
            TexturePosition = resourcePosition.UV,
            TextureSize = resourcePosition.UVSize
        });

        InstancesPerAtlas[resourcePosition.AtlasIndex]++;
    }
}