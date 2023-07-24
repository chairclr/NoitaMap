using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using NoitaMap.Map;
using NoitaMap.Viewer;
using Veldrid;

namespace NoitaMap.Graphics;

public class ChunkAtlasBuffer : AtlasedQuadBuffer, IDisposable
{
    private const int SingleAtlasSize = 8192;

    private readonly List<Chunk> Chunks = new List<Chunk>();

    /// <summary>
    /// Chunk queue for bad graphics APIs that don't support multi-threaded texture creation and updates
    /// </summary>
    private readonly ConcurrentQueue<Chunk> ThreadedChunkQueue = new ConcurrentQueue<Chunk>();

    private int CurrentX;

    private int CurrentY;

    protected override List<int> InstancesPerAtlas { get; } = new List<int>();

    public ChunkAtlasBuffer(ViewerDisplay viewerDisplay)
        : base(viewerDisplay)
    {
        CurrentX = 0;
        CurrentY = 0;

        CurrentAtlasTexture = CreateNewAtlas(SingleAtlasSize, SingleAtlasSize);

        AddAtlas(CurrentAtlasTexture);

        InstancesPerAtlas.Add(0);
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

            chunk.WorkingTextureData = null;

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

            chunk.WorkingTextureData = null;

            needsUpdate = true;
        }

        if (needsUpdate)
        {
            TransformBuffer.UpdateInstanceBuffer();
        }
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

                CurrentAtlasTexture = CreateNewAtlas(SingleAtlasSize, SingleAtlasSize);

                AddAtlas(CurrentAtlasTexture);

                InstancesPerAtlas.Add(0);
            }

            currentX = CurrentX;
            currentY = CurrentY;

            CurrentX += Chunk.ChunkWidth;

            Chunks.Add(chunk);

            InstancesPerAtlas[^1]++;
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
}
