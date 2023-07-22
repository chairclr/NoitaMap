using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using NoitaMap.Map;
using NoitaMap.Viewer;
using Veldrid;
using Vulkan.Xlib;

namespace NoitaMap.Graphics;

public class ChunkAtlasBuffer
{
    private const int SingleAtlasSize = 8192;

    private readonly ViewerDisplay ViewerDisplay;

    private readonly GraphicsDevice GraphicsDevice;

    private readonly List<Chunk> Chunks = new List<Chunk>();

    /// <summary>
    /// Chunk queue for bad graphics APIs that don't support multi-threaded texture creation and updates
    /// </summary>
    private readonly ConcurrentQueue<Chunk> ThreadedChunkQueue = new ConcurrentQueue<Chunk>();

    private readonly QuadVertexBuffer<Vertex> DrawBuffer;

    private readonly InstanceBuffer<VertexInstance> TransformBuffer;

    private readonly List<ResourceSet> ResourceAtlases = new List<ResourceSet>();

    private Texture? CurrentAtlasTexture;

    private int CurrentX;

    private int CurrentY;

    public ChunkAtlasBuffer(ViewerDisplay viewerDisplay)
    {
        ViewerDisplay = viewerDisplay;

        GraphicsDevice = viewerDisplay.GraphicsDevice;

        TransformBuffer = new InstanceBuffer<VertexInstance>(GraphicsDevice);

        DrawBuffer = new QuadVertexBuffer<Vertex>(GraphicsDevice, (pos, uv) =>
        {
            return new Vertex()
            {
                Position = new Vector3(pos * 512f, 0f),
                UV = uv
            };
        }, TransformBuffer);

        CurrentAtlasTexture = CreateNewAtlas();

        ResourceAtlases.Add(ViewerDisplay.CreateResourceSet(CurrentAtlasTexture));

        CurrentX = 0;
        CurrentY = 0;
    }

    public void AddChunk(Chunk chunk)
    {
        if (!chunk.ReadyToBeAddedToAtlas)
        {
            throw new InvalidOperationException("Chunk not ready to be added to atlas");
        }

        if (GraphicsDevice.BackendType is GraphicsBackend.Direct3D11 or GraphicsBackend.Vulkan or GraphicsBackend.Metal)
        {
            ProcessChunk(chunk);

            lock (TransformBuffer)
            {
                TransformBuffer.UpdateInstanceBuffer();
            }
        }
        else
        {
            ThreadedChunkQueue.Enqueue(chunk);
        }
    }

    public void Update()
    {
        bool needsUpdate = false;

        while (ThreadedChunkQueue.TryDequeue(out Chunk? chunk))
        {
            ProcessChunk(chunk);

            needsUpdate = true;
        }

        if (needsUpdate)
        {
            TransformBuffer.UpdateInstanceBuffer();
        }
    }

    public ResourceSet TestGetResourceSet()
    {
        return ResourceAtlases.First();
    }

    private void ProcessChunk(Chunk chunk)
    {
        if (!chunk.ReadyToBeAddedToAtlas)
        {
            throw new InvalidOperationException("Chunk not ready to be processed");
        }

        chunk.ReadyToBeAddedToAtlas = false;

        int currentX;
        int currentY;

        lock (Chunks)
        {
            if (CurrentX >= SingleAtlasSize)
            {
                CurrentX = 0;

                CurrentY += Chunk.ChunkHeight;
            }

            if (CurrentY >= SingleAtlasSize)
            {
                CurrentX = 0;
                CurrentY = 0;

                CurrentAtlasTexture = CreateNewAtlas();

                ResourceAtlases.Add(ViewerDisplay.CreateResourceSet(CurrentAtlasTexture));
            }

            currentX = CurrentX;
            currentY = CurrentY;

            CurrentX += Chunk.ChunkWidth;

            Chunks.Add(chunk);
        }

        Vector2 pos = new Vector2(currentX, currentY) / new Vector2(SingleAtlasSize);
        Vector2 size = new Vector2(0.0625f);

        GraphicsDevice.UpdateTexture(CurrentAtlasTexture, MemoryMarshal.CreateSpan(ref chunk.WorkingTextureData![0, 0], Chunk.ChunkWidth * Chunk.ChunkHeight), (uint)currentX, (uint)currentY, 0, Chunk.ChunkWidth, Chunk.ChunkHeight, 1, 0, 0);

        lock (TransformBuffer)
        {
            TransformBuffer.AddInstance(new VertexInstance()
            {
                Transform = chunk.PrecalculatedWorldMatrix,
                TexturePosition = pos,
                TextureSize = size,
            });
        }
    }

    private Texture CreateNewAtlas()
    {
        return GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription()
        {
            Type = TextureType.Texture2D,
            Format = PixelFormat.R8_G8_B8_A8_UNorm,
            Width = SingleAtlasSize,
            Height = SingleAtlasSize,
            Usage = TextureUsage.Sampled,
            MipLevels = 1,

            // Nececessary
            Depth = 1,
            ArrayLayers = 1,
            SampleCount = TextureSampleCount.Count1
        });
    }

    public void Draw(CommandList commandList)
    {
        int instanceCount = Math.Min(256, Chunks.Count);

        for (int i = 0; i < ResourceAtlases.Count; i++)
        {
            ResourceSet resourceSet = ResourceAtlases[i];
            commandList.SetGraphicsResourceSet(0, resourceSet);

            DrawBuffer.Draw(commandList, instanceCount * 6, i * instanceCount);
        }
    }
}
