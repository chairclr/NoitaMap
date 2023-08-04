using System.Collections.Concurrent;
using System.Numerics;
using CommunityToolkit.HighPerformance;
using NoitaMap.Graphics.Atlases;
using NoitaMap.Map;
using NoitaMap.Viewer;
using Veldrid;

namespace NoitaMap.Graphics;

public class ChunkAtlasBuffer : AtlasedQuadBuffer, IDisposable
{
    private const int SingleAtlasSize = 8192;

    private const int MaxBatchSize = 32;

    public readonly List<Chunk> Chunks = new List<Chunk>();

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
        if (chunk.ReadyToBeAddedToAtlasAsAir)
        {
            chunk.ReadyToBeAddedToAtlasAsAir = false;

            chunk.WorkingTextureData = null;

            return;
        }

        if (!chunk.ReadyToBeAddedToAtlas)
        {
            throw new InvalidOperationException("Chunk not ready to be added to atlas");
        }

        ThreadedChunkQueue.Enqueue(chunk);
    }

    public void Update()
    {
        bool needsUpdate = false;

        int i = 0;

        // i < MaxBatchSize check before we dequeue because we don't want to discard the result
        while (i < MaxBatchSize && ThreadedChunkQueue.TryDequeue(out Chunk? chunk))
        {
            ProcessChunk(chunk);

            chunk.WorkingTextureData = null;

            needsUpdate = true;

            i++;
        }

        if (needsUpdate)
        {
            TransformBuffer.UpdateInstanceBuffer();
        }
    }

    private StatisticTimer AddChunkToAtlasTimer = new StatisticTimer("Add Chunk to Atlas");

    private void ProcessChunk(Chunk chunk)
    {
        if (!chunk.ReadyToBeAddedToAtlas)
        {
            throw new InvalidOperationException("Chunk not ready to be processed");
        }

        chunk.ReadyToBeAddedToAtlas = false;

        AddChunkToAtlasTimer.Begin();

        if (CurrentX >= SingleAtlasSize)
        {
            CurrentX = 0;

            CurrentY += Chunk.ChunkHeight;
        }

        if (CurrentY >= SingleAtlasSize)
        {
            CurrentX = 0;
            CurrentY = 0;

            InstancesPerAtlas.Add(0);

            CurrentAtlasTexture = CreateNewAtlas(SingleAtlasSize, SingleAtlasSize);

            AddAtlas(CurrentAtlasTexture);
        }

        Vector2 pos = new Vector2(CurrentX, CurrentY) / new Vector2(SingleAtlasSize);
        Vector2 size = new Vector2(0.0625f);

        TransformBuffer.AddInstance(new VertexInstance()
        {
            Transform = chunk.WorldMatrix,
            TexturePosition = pos,
            TextureSize = size,
        });

        GraphicsDevice.UpdateTexture(CurrentAtlasTexture, chunk.WorkingTextureData.AsSpan(), (uint)CurrentX, (uint)CurrentY, 0, Chunk.ChunkWidth, Chunk.ChunkHeight, 1, 0, 0);

        CurrentX += Chunk.ChunkWidth;

        Chunks.Add(chunk);

        InstancesPerAtlas[^1]++;

        AddChunkToAtlasTimer.End(StatisticMode.Sum);
    }
}
