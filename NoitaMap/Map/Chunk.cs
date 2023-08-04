using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NoitaMap.Map;

public class Chunk
{
    public const int ChunkWidth = 512;

    public const int ChunkHeight = 512;

    public Vector2 Position;

    public PhysicsObject[]? PhysicsObjects;

    public Rgba32[,]? WorkingTextureData;

    public Matrix4x4 WorldMatrix = Matrix4x4.Identity;

    public bool ReadyToBeAddedToAtlas = false;

    public bool ReadyToBeAddedToAtlasAsAir = false;

    public Chunk(Vector2 position)
    {
        Position = position;
    }

    public void Deserialize(BinaryReader reader, MaterialProvider materialProvider)
    {
        byte[,] cellTable = new byte[ChunkWidth, ChunkHeight];

        reader.Read(MemoryMarshal.CreateSpan(ref cellTable[0, 0], ChunkWidth * ChunkHeight));

        string[] materialNames = ReadMaterialNames(reader);

        Material[] materials = materialProvider.CreateMaterialMap(materialNames);

        Rgba32[] customColors = ReadCustomColors(reader, out _);

        int chunkX = (int)Position.X;
        int chunkY = (int)Position.Y;

        WorkingTextureData = new Rgba32[ChunkWidth, ChunkHeight];

        bool wasAnyNotAir = false;

        int customColorIndex = 0;
        for (int x = 0; x < ChunkWidth; x++)
        {
            for (int y = 0; y < ChunkHeight; y++)
            {
                int material = cellTable[x, y] & (~0x80);
                bool customColor = (cellTable[x, y] & 0x80) != 0;

                if (customColor)
                {
                    WorkingTextureData[x, y] = customColors[customColorIndex];
                    // explicit > implicit
                    customColorIndex++;

                    wasAnyNotAir = true;
                }
                else
                {
                    if (material != 0)
                    {
                        wasAnyNotAir = true;
                    }

                    if (material == 0)
                    {
                        continue;
                    }

                    Material mat = materials[material];

                    if (mat.Name == "_")
                    {
                        WorkingTextureData[x, y] = mat.MaterialTexture.Span[Math.Abs(x + chunkX * ChunkWidth) % mat.MaterialTexture.Width, Math.Abs(y + chunkY * ChunkHeight) % mat.MaterialTexture.Height];
                    }
                    else
                    {
                        int wx = (x + chunkX * ChunkWidth) * 6;
                        int wy = (y + chunkY * ChunkHeight) * 6;

                        int colorX = ((wx & Material.MaterialWidthM1) + Material.MaterialWidthM1) & Material.MaterialWidthM1;
                        int colorY = ((wy & Material.MaterialHeightM1) + Material.MaterialHeightM1) & Material.MaterialHeightM1;

                        WorkingTextureData[x, y] = mat.MaterialTexture.Span[colorY, colorX];
                    }
                }
            }
        }

        ArrayPool<Rgba32>.Shared.Return(customColors);

        // All air optimization
        if (!wasAnyNotAir)
        {
            ReadyToBeAddedToAtlasAsAir = true;
        }
        else
        {
            WorldMatrix = Matrix4x4.CreateScale(512f, 512f, 1f) * Matrix4x4.CreateTranslation(new Vector3(Position, 0f));

            ReadyToBeAddedToAtlas = true;
        }

        int physicsObjectCount = reader.ReadBEInt32();

        PhysicsObjects = new PhysicsObject[physicsObjectCount];

        for (int i = 0; i < physicsObjectCount; i++)
        {
            PhysicsObjects[i] = new PhysicsObject();

            PhysicsObjects[i].Deserialize(reader);
        }
    }

    private string[] ReadMaterialNames(BinaryReader reader)
    {
        int materialNameCount = reader.ReadBEInt32();

        string[] materialNames = new string[materialNameCount];

        for (int i = 0; i < materialNameCount; i++)
        {
            materialNames[i] = reader.ReadNoitaString()!;
        }

        return materialNames;
    }

    private Rgba32[] ReadCustomColors(BinaryReader reader, out int materialWorldColorCount)
    {
        materialWorldColorCount = reader.ReadBEInt32();

        Rgba32[] materialWorldColors = ArrayPool<Rgba32>.Shared.Rent(materialWorldColorCount);

        for (int i = 0; i < materialWorldColorCount; i++)
        {
            materialWorldColors[i].PackedValue = reader.ReadBEUInt32();
        }

        return materialWorldColors;
    }
}
