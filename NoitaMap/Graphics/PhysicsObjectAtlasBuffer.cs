using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using NoitaMap.Map;
using NoitaMap.Viewer;
using Veldrid;
using Rectangle = System.Drawing.Rectangle;

namespace NoitaMap.Graphics;

public class PhysicsObjectAtlasBuffer : AtlasedQuadBuffer
{
    private const int SingleAtlasSize = 8192;

    protected override IList<int> InstancesPerAtlas { get; } = new List<int>();

    private readonly List<PhysicsObject> PhysicsObjects = new List<PhysicsObject>();

    private readonly ConcurrentQueue<PhysicsObject> ThreadedPhysicsObjectsQueue = new ConcurrentQueue<PhysicsObject>();

    private readonly Dictionary<int, Vector2> MappedAtlasRegions = new Dictionary<int, Vector2>();

    private readonly List<Rectangle> CachedAtlasRegions = new List<Rectangle>();

    private int CurrentAtlasX = 0;

    private int CurrentAtlasY = 0;

    public PhysicsObjectAtlasBuffer(ViewerDisplay viewerDisplay) : base(viewerDisplay)
    {
        CurrentAtlasTexture = CreateNewAtlas(SingleAtlasSize, SingleAtlasSize);

        AddAtlas(CurrentAtlasTexture);

        InstancesPerAtlas.Add(0);
    }

    public void AddPhysicsObjects(PhysicsObject[] physicsObjects)
    {
        bool backendSupportsMultithreading = GraphicsDevice.BackendType is GraphicsBackend.Direct3D11 or GraphicsBackend.Vulkan or GraphicsBackend.Metal;

        if (backendSupportsMultithreading)
        {
            foreach (PhysicsObject physicsObject in physicsObjects)
            {
                ThreadedPhysicsObjectsQueue.Enqueue(physicsObject);
            }
        }
        else
        {
            bool needsUpdate = false;

            foreach (PhysicsObject physicsObject in physicsObjects)
            {
                if (!physicsObject.ReadyToBeAddedToAtlas)
                {
                    throw new InvalidOperationException("Physics object not ready to be added to atlas");
                }

                ProcessPhysicsObject(physicsObject);

                physicsObject.WorkingTextureData = null;

                needsUpdate = true;
            }

            if (needsUpdate)
            {
                lock (TransformBuffer)
                {
                    TransformBuffer.UpdateInstanceBuffer();
                }
            }
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

    private void ProcessPhysicsObject(PhysicsObject physicsObject)
    {
        Vector2 pos;
        Vector2 size = new Vector2(physicsObject.Width, physicsObject.Height) / SingleAtlasSize;

        lock (PhysicsObjects)
        {
            if (!physicsObject.ReadyToBeAddedToAtlas)
            {
                throw new InvalidOperationException("Physics object not ready to be added to atlas");
            }

            pos = AddTextureToAtlas(physicsObject.Width, physicsObject.Height, physicsObject.TextureHash, physicsObject.WorkingTextureData!);

            PhysicsObjects.Add(physicsObject);
        }

        lock (TransformBuffer)
        {
            TransformBuffer.AddInstance(new VertexInstance()
            {
                Transform = physicsObject.PrecalculatedWorldMatrix,
                TexturePosition = pos,
                TextureSize = size
            });
        }
    }

    private Vector2 AddTextureToAtlas(int width, int height, int textureHash, Rgba32[,] texture)
    {
        Rectangle rect;

        if (MappedAtlasRegions.TryGetValue(textureHash, out Vector2 mappedStart))
        {
            return mappedStart;
        }

        if (width > SingleAtlasSize || height > SingleAtlasSize)
        {
            throw new Exception("Texture larger than atlas");
        }

        if (CurrentAtlasX + width >= SingleAtlasSize)
        {
            CurrentAtlasX = 0;

            CurrentAtlasY += height;
        }

        if (CurrentAtlasY >= SingleAtlasSize)
        {
            throw new Exception("Creating new atlas");
        }

        rect = new Rectangle(CurrentAtlasX, CurrentAtlasY, width, height);

        CurrentAtlasX += width;

        while (true)
        {
            if (CachedAtlasRegions.Any(x => rect.IntersectsWith(x)))
            {
                CurrentAtlasY++;

                rect.Y = CurrentAtlasY;

                if (CurrentAtlasY >= SingleAtlasSize)
                {
                    throw new Exception("Creating new atlas");
                }
            }
            else
            {
                break;
            }
        }

        CachedAtlasRegions.Add(rect);

        MappedAtlasRegions.Add(textureHash, new Vector2(rect.X, rect.Y) / new Vector2(SingleAtlasSize));

        InstancesPerAtlas[^1]++;

        GraphicsDevice.UpdateTexture(CurrentAtlasTexture, MemoryMarshal.CreateSpan(ref texture[0, 0], width * height), (uint)rect.X, (uint)rect.Y, 0, (uint)width, (uint)height, 1, 0, 0);

        return new Vector2(rect.X, rect.Y) / new Vector2(SingleAtlasSize);
    }
}
