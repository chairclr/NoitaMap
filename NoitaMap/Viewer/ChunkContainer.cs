using System.Numerics;
using System.Text.RegularExpressions;
using NoitaMap.Graphics;
using NoitaMap.Graphics.Atlases;
using NoitaMap.Map;
using Veldrid;

namespace NoitaMap.Viewer;

public partial class ChunkContainer : IDisposable
{
    private static readonly Regex ChunkPositionRegex = GenerateWorldRegex();

    public readonly ViewerDisplay ViewerDisplay;

    public readonly MaterialProvider MaterialProvider;

    public readonly ConstantBuffer<VertexConstantBuffer> ConstantBuffer;

    private readonly ChunkAtlasBuffer ChunkAtlas;

    public IReadOnlyList<Chunk> Chunks => ChunkAtlas.Chunks;

    private readonly PhysicsObjectAtlasBuffer PhysicsObjectAtlas;

    public IReadOnlyList<PhysicsObject> PhysicsObjects => PhysicsObjectAtlas.PhysicsObjects;

    private Framebuffer PhysicsObjectFramebuffer;

    private Texture PhysicsObjectFramebufferTexture;

    private ResourceSet PhysicsObjectResourceSet;

    private readonly QuadVertexBuffer<Vertex> VertexBuffer;

    public bool ForceNoFrambuffer = false;

    private bool Disposed;

    public ChunkContainer(ViewerDisplay viewerDisplay)
    {
        ViewerDisplay = viewerDisplay;

        ConstantBuffer = ViewerDisplay.ConstantBuffer;

        MaterialProvider = ViewerDisplay.MaterialProvider;

        ChunkAtlas = new ChunkAtlasBuffer(ViewerDisplay);

        PhysicsObjectAtlas = new PhysicsObjectAtlasBuffer(ViewerDisplay);

        PhysicsObjectFramebufferTexture = ViewerDisplay.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription()
        {
            Type = TextureType.Texture2D,
            Format = PixelFormat.B8_G8_R8_A8_UNorm,
            Width = (uint)ViewerDisplay.Window.Size.X,
            Height = (uint)ViewerDisplay.Window.Size.Y,
            Usage = TextureUsage.Sampled | TextureUsage.RenderTarget,
            MipLevels = 1,

            // Nececessary
            Depth = 1,
            ArrayLayers = 1,
            SampleCount = TextureSampleCount.Count1
        });
        PhysicsObjectFramebufferTexture.Name = nameof(PhysicsObjectFramebufferTexture);

        PhysicsObjectFramebuffer = ViewerDisplay.GraphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription()
        {
            ColorTargets = new FramebufferAttachmentDescription[] { new FramebufferAttachmentDescription(PhysicsObjectFramebufferTexture, 0) }
        });
        PhysicsObjectFramebuffer.Name = nameof(PhysicsObjectFramebuffer);

        PhysicsObjectResourceSet = ViewerDisplay.CreateTextureBinding(PhysicsObjectFramebufferTexture);

        InstanceBuffer<VertexInstance> instanceBuffer = new InstanceBuffer<VertexInstance>(ViewerDisplay.GraphicsDevice);

        instanceBuffer.AddInstance(new VertexInstance()
        {
            TexturePosition = new Vector2(0f, 0f),
            TextureSize = new Vector2(1f, 1f),
            Transform = Matrix4x4.Identity
        });

        instanceBuffer.UpdateInstanceBuffer();

        VertexBuffer = new QuadVertexBuffer<Vertex>(ViewerDisplay.GraphicsDevice, (x, y) =>
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

        byte[]? decompressedData = NoitaDecompressor.ReadAndDecompressChunk(chunkFilePath);

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

        PhysicsObjectAtlas.AddPhysicsObjects(chunk.PhysicsObjects!);
    }

    public void Update()
    {
        ChunkAtlas.Update();

        PhysicsObjectAtlas.Update();
    }

    public void Draw(CommandList commandList)
    {
        ChunkAtlas.Draw(commandList);

        if (ViewerDisplay.ViewScale.X > 1f && ViewerDisplay.ViewScale.Y > 1f && !ForceNoFrambuffer)
        {
            Matrix4x4 cachedViewProjection = ConstantBuffer.Data.ViewProjection;

            commandList.SetFramebuffer(PhysicsObjectFramebuffer);

            commandList.ClearColorTarget(0, RgbaFloat.Clear);

            ConstantBuffer.Data.ViewProjection =
                Matrix4x4.CreateTranslation(-new Vector3(MathF.Floor(ViewerDisplay.ViewOffset.X), MathF.Floor(ViewerDisplay.ViewOffset.Y), 0f)) *
                ViewerDisplay.Projection *
                Matrix4x4.CreateTranslation(-1f, -1f, 0f);

            ConstantBuffer.Update(commandList);

            PhysicsObjectAtlas.Draw(commandList);

            commandList.SetFramebuffer(ViewerDisplay.GraphicsDevice.SwapchainFramebuffer);

            ConstantBuffer.Data.ViewProjection =
                Matrix4x4.CreateScale(PhysicsObjectFramebuffer.Width, PhysicsObjectFramebuffer.Height, 1f) *
                Matrix4x4.CreateTranslation(-new Vector3(ViewerDisplay.ViewOffset.X - MathF.Floor(ViewerDisplay.ViewOffset.X), ViewerDisplay.ViewOffset.Y - MathF.Floor(ViewerDisplay.ViewOffset.Y), 0f)) *
                Matrix4x4.CreateScale(new Vector3(ViewerDisplay.ViewScale.X, ViewerDisplay.ViewScale.Y, 1f)) *
                ViewerDisplay.Projection *
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

    public void HandleResize()
    {
        PhysicsObjectFramebufferTexture.Dispose();

        PhysicsObjectFramebuffer.Dispose();

        PhysicsObjectResourceSet.Dispose();

        PhysicsObjectFramebufferTexture = ViewerDisplay.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription()
        {
            Type = TextureType.Texture2D,
            Format = PixelFormat.B8_G8_R8_A8_UNorm,
            Width = (uint)ViewerDisplay.Window.Size.X,
            Height = (uint)ViewerDisplay.Window.Size.Y,
            Usage = TextureUsage.Sampled | TextureUsage.RenderTarget,
            MipLevels = 1,

            // Nececessary
            Depth = 1,
            ArrayLayers = 1,
            SampleCount = TextureSampleCount.Count1
        });

        PhysicsObjectFramebuffer = ViewerDisplay.GraphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription()
        {
            ColorTargets = new FramebufferAttachmentDescription[] { new FramebufferAttachmentDescription(PhysicsObjectFramebufferTexture, 0) }
        });

        PhysicsObjectResourceSet = ViewerDisplay.CreateTextureBinding(PhysicsObjectFramebufferTexture);
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
