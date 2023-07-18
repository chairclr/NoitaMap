using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NoitaMap;

internal partial class ChunkRenderer
{
    private static readonly Regex ChunkPositionRegex = GenerateChunkPositionRegex();

    public static Chunk RenderChunk(string chunkPath)
    {
        byte[] outputBuffer = ReadAndDecompress(chunkPath);

        using MemoryStream ms = new MemoryStream(outputBuffer);
        using BinaryReader reader = new BinaryReader(ms);

        int version = reader.BEReadInt32();
        int width = reader.BEReadInt32();
        int height = reader.BEReadInt32();

        if (version != 24)
        {
            throw new Exception("Invalid version");
        }

        if (width != Chunk.Width)
        {
            throw new Exception("Invalid width");
        }

        if (height != Chunk.Height)
        {
            throw new Exception("Invalid height");
        }

        (int chunkX, int chunkY) = GetChunkPositionFromPath(chunkPath);

        return new Chunk(chunkX, chunkY, reader);
    }

    private static byte[] ReadAndDecompress(string chunkPath)
    {
        byte[] inputBuffer;
        byte[] outputBuffer;

        using (FileStream fs = new FileStream(chunkPath, FileMode.Open))
        {
            using BinaryReader fileReader = new BinaryReader(fs);

            int compressedSize = fileReader.ReadInt32();
            int uncompressedSize = fileReader.ReadInt32();

            inputBuffer = new byte[compressedSize];
            outputBuffer = new byte[uncompressedSize];

            fileReader.Read(inputBuffer);
        }

        int decompressedBytes = FastLZ.Decompress(inputBuffer, outputBuffer);

        if (outputBuffer.Length != decompressedBytes)
        {
            throw new Exception();
        }

        return outputBuffer;
    }

    private static (int, int) GetChunkPositionFromPath(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        Match match = ChunkPositionRegex.Match(fileName);

        return (int.Parse(match.Groups["x"].Value), int.Parse(match.Groups["y"].Value));
    }

    

    [GeneratedRegex("world_(?<x>-?\\d+)_(?<y>-?\\d+)\\.png_petri")]
    private static partial Regex GenerateChunkPositionRegex();
}