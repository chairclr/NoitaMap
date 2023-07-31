using System.Collections.Concurrent;
using CommunityToolkit.HighPerformance;
using NoitaMap.Map;
using NoitaMap.Viewer;

namespace NoitaMap.Graphics;

public class PhysicsObjectAtlasBuffer : PackedAtlasedQuadBuffer
{
    private readonly List<PhysicsObject> PhysicsObjects = new List<PhysicsObject>();

    private readonly ConcurrentQueue<PhysicsObject> ThreadedPhysicsObjectsQueue = new ConcurrentQueue<PhysicsObject>();

    public PhysicsObjectAtlasBuffer(ViewerDisplay viewerDisplay) : base(viewerDisplay)
    {

    }

    public void AddPhysicsObjects(PhysicsObject[] physicsObjects)
    {
        foreach (PhysicsObject physicsObject in physicsObjects)
        {
            if (!physicsObject.ReadyToBeAddedToAtlas)
            {
                throw new InvalidOperationException("Physics object not ready to be added to atlas");
            }

            ThreadedPhysicsObjectsQueue.Enqueue(physicsObject);
        }
    }

    public void Update()
    {
        bool needsUpdate = false;

        while (ThreadedPhysicsObjectsQueue.TryDequeue(out PhysicsObject? physicsObject))
        {
            ProcessPhysicsObject(physicsObject);

            physicsObject.WorkingTextureData = null;

            needsUpdate = true;
        }

        if (needsUpdate)
        {
            TransformBuffer.UpdateInstanceBuffer();
        }
    }

    private StatisticTimer AddPhysicsObjectsToAtlasTimer = new StatisticTimer("Add Physics Object Textures to Atlas");

    private void ProcessPhysicsObject(PhysicsObject physicsObject)
    {
        if (!physicsObject.ReadyToBeAddedToAtlas)
        {
            throw new InvalidOperationException("Physics object not ready to be added to atlas");
        }

        AddPhysicsObjectsToAtlasTimer.Begin();

        ResourcePosition resourcePosition = AddTextureToAtlas(physicsObject.Width, physicsObject.Height, physicsObject.TextureHash, physicsObject.WorkingTextureData!.AsSpan());

        AddPhysicsObjectsToAtlasTimer.End(StatisticMode.Sum);

        PhysicsObjects.Add(physicsObject);

        TransformBuffer.InsertInstance(resourcePosition.InstanceIndex, new VertexInstance()
        {
            Transform = physicsObject.PrecalculatedWorldMatrix,
            TexturePosition = resourcePosition.UV,
            TextureSize = resourcePosition.UVSize
        });

        InstancesPerAtlas[resourcePosition.AtlasIndex]++;
    }
}
