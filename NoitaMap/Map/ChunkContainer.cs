using System.Numerics;
using System.Text.RegularExpressions;
using NoitaMap.Graphics;
using NoitaMap.Graphics.Atlases;
using NoitaMap.Map;
using NoitaMap.Viewer;
using Silk.NET.Maths;
using Veldrid;

namespace NoitaMap.Map;

public partial class ChunkContainer : IRenderable
{
    private static readonly Regex ChunkPositionRegex = GenerateWorldRegex();

    public readonly Renderer Renderer;

    public readonly MaterialProvider MaterialProvider;

    public readonly ConstantBuffer<VertexConstantBuffer> ConstantBuffer;

    private readonly ChunkAtlasBuffer ChunkAtlas;

    public IReadOnlyList<Chunk> Chunks => ChunkAtlas.Chunks;

    private readonly QuadObjectAtlasBuffer<PhysicsObject> PhysicsObjectAtlas;

    public IReadOnlyList<PhysicsObject> PhysicsObjects => PhysicsObjectAtlas.AtlasObjects;

    private Framebuffer PhysicsObjectFramebuffer;

    private Texture PhysicsObjectFramebufferTexture;

    private ResourceSet PhysicsObjectResourceSet;

    private readonly QuadVertexBuffer<Vertex> VertexBuffer;

    public bool ForceNoFrambuffer = false;

    private bool Disposed;

    public ChunkContainer(Renderer renderer)
    {
        Renderer = renderer;

        ConstantBuffer = Renderer.ConstantBuffer;

        MaterialProvider = Renderer.MaterialProvider;

        ChunkAtlas = new ChunkAtlasBuffer(Renderer);

        PhysicsObjectAtlas = new QuadObjectAtlasBuffer<PhysicsObject>(Renderer);

        PhysicsObjectFramebufferTexture = Renderer.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription()
        {
            Type = TextureType.Texture2D,
            Format = PixelFormat.B8_G8_R8_A8_UNorm,
            Width = (uint)Renderer.Window.Size.X,
            Height = (uint)Renderer.Window.Size.Y,
            Usage = TextureUsage.Sampled | TextureUsage.RenderTarget,
            MipLevels = 1,

            // Nececessary
            Depth = 1,
            ArrayLayers = 1,
            SampleCount = TextureSampleCount.Count1
        });
        PhysicsObjectFramebufferTexture.Name = nameof(PhysicsObjectFramebufferTexture);

        PhysicsObjectFramebuffer = Renderer.GraphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription()
        {
            ColorTargets = new FramebufferAttachmentDescription[] { new FramebufferAttachmentDescription(PhysicsObjectFramebufferTexture, 0) }
        });
        PhysicsObjectFramebuffer.Name = nameof(PhysicsObjectFramebuffer);

        PhysicsObjectResourceSet = Renderer.CreateTextureBinding(PhysicsObjectFramebufferTexture);

        InstanceBuffer<VertexInstance> instanceBuffer = new InstanceBuffer<VertexInstance>(Renderer.GraphicsDevice);

        instanceBuffer.AddInstance(new VertexInstance()
        {
            TexturePosition = new Vector2(0f, 0f),
            TextureSize = new Vector2(1f, 1f),
            Transform = Matrix4x4.Identity
        });

        instanceBuffer.UpdateInstanceBuffer();

        VertexBuffer = new QuadVertexBuffer<Vertex>(Renderer.GraphicsDevice, (x, y) =>
        {
            return new Vertex()
            {
                Position = new Vector3(x, 0f),
                UV = y
            };
        }, instanceBuffer);
    }

    public void LoadChunk(string chunkFilePath)
    {
        Vector2 chunkPosition = GetChunkPositionFromPath(chunkFilePath);

        Chunk chunk = new Chunk(chunkPosition);

        StatisticTimer loadChunkTimer = new StatisticTimer("Load Chunk").Begin();

        byte[]? decompressedData = NoitaFile.LoadCompressedFile(chunkFilePath);

        using (MemoryStream ms = new MemoryStream(decompressedData))
        {
            using BinaryReader reader = new BinaryReader(ms);

            int version = reader.ReadBEInt32();
            int width = reader.ReadBEInt32();
            int height = reader.ReadBEInt32();

            if (version != 24 || width != Chunk.ChunkWidth || height != Chunk.ChunkHeight)
            {
                throw new InvalidDataException($"Chunk header was not correct. Version = {version} Width = {width} Height = {height}");
            }

            chunk.Deserialize(reader, MaterialProvider);
        }

        loadChunkTimer.End(StatisticMode.Sum);

        decompressedData = null;

        ChunkAtlas.AddChunk(chunk);

        PhysicsObjectAtlas.AddAtlasObjects(chunk.PhysicsObjects!);
    }

    public void Update()
    {
        ChunkAtlas.Update();

        PhysicsObjectAtlas.Update();
    }

    public void Render(CommandList commandList)
    {
        ChunkAtlas.Draw(commandList);

        if (Renderer.ViewScale.X > 1f && Renderer.ViewScale.Y > 1f && !ForceNoFrambuffer)
        {
            Matrix4x4 cachedViewProjection = ConstantBuffer.Data.ViewProjection;

            commandList.SetFramebuffer(PhysicsObjectFramebuffer);

            commandList.ClearColorTarget(0, RgbaFloat.Clear);

            ConstantBuffer.Data.ViewProjection =
                Matrix4x4.CreateTranslation(-new Vector3(MathF.Floor(Renderer.ViewOffset.X), MathF.Floor(Renderer.ViewOffset.Y), 0f)) *
                Renderer.Projection *
                Matrix4x4.CreateTranslation(-1f, -1f, 0f);

            ConstantBuffer.Update(commandList);

            PhysicsObjectAtlas.Draw(commandList);

            commandList.SetFramebuffer(Renderer.GraphicsDevice.SwapchainFramebuffer);

            ConstantBuffer.Data.ViewProjection =
                Matrix4x4.CreateScale(PhysicsObjectFramebuffer.Width, PhysicsObjectFramebuffer.Height, 1f) *
                Matrix4x4.CreateTranslation(-new Vector3(Renderer.ViewOffset.X - MathF.Floor(Renderer.ViewOffset.X), Renderer.ViewOffset.Y - MathF.Floor(Renderer.ViewOffset.Y), 0f)) *
                Matrix4x4.CreateScale(new Vector3(Renderer.ViewScale.X, Renderer.ViewScale.Y, 1f)) *
                Renderer.Projection *
                Matrix4x4.CreateTranslation(-1f, -1f, 0f);

            ConstantBuffer.Update(commandList);

            commandList.SetGraphicsResourceSet(2, PhysicsObjectResourceSet);

            VertexBuffer.Draw(commandList, 6, 0);

            ConstantBuffer.Data.ViewProjection = cachedViewProjection;

            ConstantBuffer.Update(commandList);
        }
        else
        {
            PhysicsObjectAtlas.Draw(commandList);
        }
    }

    public void InvalidateChunk(Chunk chunk)
    {
        chunk.Invalidate();

        ChunkAtlas.ReplaceChunk(chunk);
    }

    public void HandleResize(Vector2D<int> newSize)
    {
        PhysicsObjectFramebufferTexture.Dispose();

        PhysicsObjectFramebuffer.Dispose();

        PhysicsObjectResourceSet.Dispose();

        PhysicsObjectFramebufferTexture = Renderer.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription()
        {
            Type = TextureType.Texture2D,
            Format = PixelFormat.B8_G8_R8_A8_UNorm,
            Width = (uint)newSize.X,
            Height = (uint)newSize.Y,
            Usage = TextureUsage.Sampled | TextureUsage.RenderTarget,
            MipLevels = 1,

            // Nececessary
            Depth = 1,
            ArrayLayers = 1,
            SampleCount = TextureSampleCount.Count1
        });

        PhysicsObjectFramebuffer = Renderer.GraphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription()
        {
            ColorTargets = [new FramebufferAttachmentDescription(PhysicsObjectFramebufferTexture, 0)]
        });

        PhysicsObjectResourceSet = Renderer.CreateTextureBinding(PhysicsObjectFramebufferTexture);
    }

    private static Vector2 GetChunkPositionFromPath(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        Match match = ChunkPositionRegex.Match(fileName);

        return new Vector2(int.Parse(match.Groups["x"].Value), int.Parse(match.Groups["y"].Value));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            ConstantBuffer.Dispose();

            ChunkAtlas.Dispose();

            PhysicsObjectAtlas.Dispose();

            PhysicsObjectFramebuffer.Dispose();

            PhysicsObjectFramebufferTexture.Dispose();

            PhysicsObjectResourceSet.Dispose();

            VertexBuffer.Dispose();

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    [GeneratedRegex("world_(?<x>-?\\d+)_(?<y>-?\\d+)\\.png_petri", RegexOptions.Compiled)]
    private static partial Regex GenerateWorldRegex();

}
