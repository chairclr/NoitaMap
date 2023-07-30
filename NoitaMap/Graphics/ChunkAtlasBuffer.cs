﻿using System.Collections.Concurrent;
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

        bool backendSupportsMultithreading = GraphicsDevice.BackendType is GraphicsBackend.Direct3D11 or GraphicsBackend.Vulkan or GraphicsBackend.Metal;

        if (backendSupportsMultithreading)
        {
            ProcessChunk(chunk);

            chunk.WorkingTextureData = null;

            TransformBuffer.UpdateInstanceBuffer();
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

    private StatisticTimer AddChunkToAtlasTimer = new StatisticTimer("Add Chunk to Atlas");

    private void ProcessChunk(Chunk chunk)
    {
        if (!chunk.ReadyToBeAddedToAtlas)
        {
            throw new InvalidOperationException("Chunk not ready to be processed");
        }

        AddChunkToAtlasTimer.Begin();

        chunk.ReadyToBeAddedToAtlas = false;

        int currentX;
        int currentY;

        Texture atlasTexture;

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

                InstancesPerAtlas.Add(0);

                CurrentAtlasTexture = CreateNewAtlas(SingleAtlasSize, SingleAtlasSize);

                AddAtlas(CurrentAtlasTexture);
            }

            currentX = CurrentX;
            currentY = CurrentY;

            Vector2 pos = new Vector2(currentX, currentY) / new Vector2(SingleAtlasSize);
            Vector2 size = new Vector2(0.0625f);

            TransformBuffer.AddInstance(new VertexInstance()
            {
                Transform = chunk.PrecalculatedWorldMatrix,
                TexturePosition = pos,
                TextureSize = size,
            });

            CurrentX += Chunk.ChunkWidth;

            Chunks.Add(chunk);

            atlasTexture = CurrentAtlasTexture!;
        }

        GraphicsDevice.UpdateTexture(atlasTexture, MemoryMarshal.CreateSpan(ref chunk.WorkingTextureData![0, 0], Chunk.ChunkWidth * Chunk.ChunkHeight), (uint)currentX, (uint)currentY, 0, Chunk.ChunkWidth, Chunk.ChunkHeight, 1, 0, 0);

        Interlocked.Increment(ref CollectionsMarshal.AsSpan(InstancesPerAtlas)[^1]);

        AddChunkToAtlasTimer.End(StatisticMode.Sum);
    }
}
