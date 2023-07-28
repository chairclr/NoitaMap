using System.Collections.Concurrent;
using System.Numerics;
using System.Text.RegularExpressions;
using NoitaMap.Graphics;
using NoitaMap.Map;
using Veldrid;
using static NoitaMap.Viewer.ViewerDisplay;

namespace NoitaMap.Viewer;

public partial class ChunkContainer : IDisposable
{
    private static readonly Regex ChunkPositionRegex = GenerateWorldRegex();

    public readonly ViewerDisplay ViewerDisplay;

    public readonly MaterialProvider MaterialProvider;

    private readonly Dictionary<Vector2, Chunk> Chunks = new Dictionary<Vector2, Chunk>();

    private readonly ChunkAtlasBuffer ChunkAtlas;

    private readonly ConcurrentQueue<Chunk> FinishedChunks = new ConcurrentQueue<Chunk>();

    //private readonly List<PhysicsObject> PhysicsObjects = new List<PhysicsObject>();

    public readonly ConstantBuffer<VertexConstantBuffer> ConstantBuffer;

    private readonly PhysicsObjectAtlasBuffer PhysicsObjectAtlas;

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

        decompressedData = null;

        ChunkAtlas.AddChunk(chunk);

        FinishedChunks.Enqueue(chunk);

        PhysicsObjectAtlas.AddPhysicsObjects(chunk.PhysicsObjects!);
    }

    public void Update()
    {
        ChunkAtlas.Update();

        PhysicsObjectAtlas.Update();

        while (FinishedChunks.TryDequeue(out Chunk? chunk))
        {
            Chunks.Add(chunk.Position, chunk);
        }
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
