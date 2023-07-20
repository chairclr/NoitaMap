using System.Collections.Concurrent;
using System.Numerics;
using System.Text.RegularExpressions;
using NoitaMap.Graphics;
using NoitaMap.Map;
using Veldrid;
using static NoitaMap.Viewer.ViewerDisplay;

namespace NoitaMap.Viewer;

public class ChunkContainer
{
    private static readonly Regex ChunkPositionRegex = new Regex("world_(?<x>-?\\d+)_(?<y>-?\\d+)\\.png_petri", RegexOptions.Compiled);

    public readonly ViewerDisplay ViewerDisplay;

    public readonly MaterialProvider MaterialProvider;

    private readonly Dictionary<Vector2, Chunk> Chunks = new Dictionary<Vector2, Chunk>();

    private readonly ConcurrentQueue<Chunk> FinishedChunks = new ConcurrentQueue<Chunk>();

    public readonly ConstantBuffer<VertexConstantBuffer> ConstantBuffer;

    public ChunkContainer(ViewerDisplay viewerDisplay)
    {
        ViewerDisplay = viewerDisplay;

        ConstantBuffer = ViewerDisplay.ConstantBuffer;

        MaterialProvider = ViewerDisplay.MaterialProvider;
    }

    public void LoadChunk(string chunkFilePath)
    {
        Vector2 chunkPosition = GetChunkPositionFromPath(chunkFilePath);

        Chunk chunk = new Chunk(ViewerDisplay, chunkPosition, MaterialProvider!);

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

            chunk.Deserialize(reader);
        }

        decompressedData = null;

        FinishedChunks.Enqueue(chunk);
    }

    public void Draw(CommandList commandList)
    {
        while (FinishedChunks.TryDequeue(out Chunk? chunk))
        {
            Chunks.Add(chunk.Position, chunk);
        }

        foreach (Chunk chunk in Chunks.Values)
        {
            if (chunk.Ready)
            {
                ConstantBuffer.Data.World = chunk.PrecalculatedWorldMatrix;

                ConstantBuffer.Update(commandList);

                chunk.Buffer!.Draw(commandList);
            }
        }
    }

    private static Vector2 GetChunkPositionFromPath(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        Match match = ChunkPositionRegex.Match(fileName);

        return new Vector2(int.Parse(match.Groups["x"].Value), int.Parse(match.Groups["y"].Value));
    }
}
