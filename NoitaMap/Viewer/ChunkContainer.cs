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

    private bool Disposed;

    public ChunkContainer(ViewerDisplay viewerDisplay)
    {
        ViewerDisplay = viewerDisplay;

        ConstantBuffer = ViewerDisplay.ConstantBuffer;

        MaterialProvider = ViewerDisplay.MaterialProvider;

        ChunkAtlas = new ChunkAtlasBuffer(ViewerDisplay);

        PhysicsObjectAtlas = new PhysicsObjectAtlasBuffer(ViewerDisplay);
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

        PhysicsObjectAtlas.Draw(commandList);
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
