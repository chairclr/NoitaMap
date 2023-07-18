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

        if (width != 512)
        {
            throw new Exception("Invalid width");
        }

        if (height != 512)
        {
            throw new Exception("Invalid height");
        }

        byte[] packedMaterialInfo = new byte[width * height];

        reader.Read(packedMaterialInfo);

        string[] materialNames = ReadMaterialNames(reader);

        Color[] customColors = ReadCustomColors(reader);

        (int chunkX, int chunkY) = GetChunkPositionFromPath(chunkPath);

        Texture2D texture = new Texture2D(GraphicsDeviceProvider.GraphicsDevice, 512, 512);

        Color[] colors = new Color[width * height];
        int customColorIndex = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // for indexing colors[] and packedMaterialInfo[]
                int i = y + x * height;

                int material = packedMaterialInfo[i] & (~0x80);
                bool customColor = (packedMaterialInfo[i] & 0x80) != 0;

                if (customColor)
                {
                    colors[i] = customColors[customColorIndex];
                    // explicit > implicit
                    customColorIndex++;
                }
                else
                {
                    Material mat = MaterialProvider.GetMaterial(materialNames[material]);

                    if (mat.Name == "err")
                    {
                        colors[i] = mat.Colors[(x + y) % 4];
                    }
                    else
                    {
                        int wx = (x + chunkX * width) * 6;
                        int wy = (y + chunkY * height) * 6;

                        int colorX = ((wx % Material.MaterialWidth) + Material.MaterialWidth) % Material.MaterialWidth;
                        int colorY = ((wy % Material.MaterialHeight) + Material.MaterialHeight) % Material.MaterialHeight;

                        colors[i] = mat.Colors[colorX + (colorY * Material.MaterialWidth)];
                    }
                }
            }
        }

        texture.SetData(colors);

        PhysicsObject[] physicsObjects = ReadPhysicsObjects(reader);

        return new Chunk(chunkX, chunkY, texture, physicsObjects);
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

    private static string[] ReadMaterialNames(BinaryReader reader)
    {
        int materialNameCount = reader.BEReadInt32();

        string[] materialNames = new string[materialNameCount];

        for (int i = 0; i < materialNameCount; i++)
        {
            int size = reader.BEReadInt32();

            // rent a buffer here for fast :thumbs_up:
            byte[] stringBuffer = ArrayPool<byte>.Shared.Rent(size);

            reader.Read(stringBuffer.AsSpan()[..size]);

            materialNames[i] = Encoding.UTF8.GetString(stringBuffer.AsSpan()[..size]);

            ArrayPool<byte>.Shared.Return(stringBuffer);
        }

        return materialNames;
    }

    private static Color[] ReadCustomColors(BinaryReader reader)
    {
        int materialWorldColorCount = reader.BEReadInt32();

        Color[] materialWorldColors = new Color[materialWorldColorCount];

        for (int i = 0; i < materialWorldColorCount; i++)
        {
            materialWorldColors[i].PackedValue = reader.BEReadUInt32();
        }

        return materialWorldColors;
    }

    private static PhysicsObject[] ReadPhysicsObjects(BinaryReader reader)
    {
        int physicsObjectCount = reader.BEReadInt32();

        PhysicsObject[] physicsObjects = new PhysicsObject[physicsObjectCount];

        for (int i = 0; i < physicsObjectCount; i++)
        {
            reader.ReadUInt64();
            reader.ReadUInt32();
            float x = reader.BEReadSingle();
            float y = reader.BEReadSingle();
            float rotation = reader.BEReadSingle();
            reader.BEReadInt64();
            reader.BEReadInt64();
            reader.BEReadInt64();
            reader.BEReadInt64();
            reader.BEReadInt64();
            reader.ReadBoolean();
            reader.ReadBoolean();
            reader.ReadBoolean();
            reader.ReadBoolean();
            reader.ReadBoolean();
            reader.BEReadSingle();
            int width = reader.BEReadInt32();
            int height = reader.BEReadInt32();

            Texture2D texture = new Texture2D(GraphicsDeviceProvider.GraphicsDevice, width, height);

            Color[] colors = new Color[width * height];

            for (int j = 0; j < colors.Length; j++)
            {
                colors[j].PackedValue = reader.BEReadUInt32();
            }

            texture.SetData(colors);

            physicsObjects[i] = new PhysicsObject(new Vector2(x, y), new Vector2(width, height), rotation, texture);
        }

        return physicsObjects;
    }

    [GeneratedRegex("world_(?<x>-?\\d+)_(?<y>-?\\d+)\\.png_petri")]
    private static partial Regex GenerateChunkPositionRegex();
}