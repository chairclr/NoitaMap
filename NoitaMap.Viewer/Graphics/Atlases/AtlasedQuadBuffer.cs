﻿using System.Numerics;
using NoitaMap.Viewer;
using Veldrid;

namespace NoitaMap.Graphics.Atlases;

public abstract class AtlasedQuadBuffer : IDisposable
{
    protected readonly Renderer Renderer;

    protected readonly GraphicsDevice GraphicsDevice;

    protected readonly List<ResourceSet> ResourceAtlases = new List<ResourceSet>();

    public readonly List<Texture> Textures = new List<Texture>();

    private readonly QuadVertexBuffer<Vertex> DrawBuffer;

    protected readonly InstanceBuffer<VertexInstance> TransformBuffer;

    protected Texture CurrentAtlasTexture = null!;

    protected abstract IList<int> InstancesPerAtlas { get; }

    private bool Disposed;

    public AtlasedQuadBuffer(Renderer renderer)
    {
        Renderer = renderer;

        GraphicsDevice = Renderer.GraphicsDevice;

        TransformBuffer = new InstanceBuffer<VertexInstance>(GraphicsDevice);

        DrawBuffer = new QuadVertexBuffer<Vertex>(GraphicsDevice, (pos, uv) =>
        {
            return new Vertex()
            {
                Position = new Vector3(pos, 0f),
                UV = uv
            };
        }, TransformBuffer);
    }

    public void Draw(CommandList commandList)
    {
        int instacesDrawn = 0;

        for (int i = 0; i < ResourceAtlases.Count; i++)
        {
            int instanceCount = InstancesPerAtlas[i];

            ResourceSet resourceSet = ResourceAtlases[i];
            commandList.SetGraphicsResourceSet(2, resourceSet);

            DrawBuffer.Draw(commandList, instanceCount, instacesDrawn);

            instacesDrawn += instanceCount;
        }
    }

    protected void AddAtlas(Texture atlasTexture)
    {
        ResourceAtlases.Add(Renderer.CreateTextureBinding(atlasTexture));

        Textures.Add(atlasTexture);
    }

    protected Texture CreateNewAtlas(int width, int height)
    {
        return GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription()
        {
            Type = TextureType.Texture2D,
            Format = PixelFormat.R8G8B8A8UNorm,
            Width = (uint)width,
            Height = (uint)height,
            Usage = TextureUsage.Sampled,
            MipLevels = 1,

            // Nececessary
            Depth = 1,
            ArrayLayers = 1,
            SampleCount = TextureSampleCount.Count1
        });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            foreach (Texture texture in Textures)
            {
                texture.Dispose();
            }

            foreach (ResourceSet res in ResourceAtlases)
            {
                res.Dispose();
            }

            TransformBuffer.Dispose();

            DrawBuffer.Dispose();

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
