using System.Buffers;
using System.Numerics;
using CommunityToolkit.HighPerformance;
using NoitaMap.Viewer;

namespace NoitaMap.Map;

public class Chunk
{
    public const int ChunkWidth = 512;

    public const int ChunkHeight = 512;

    public Vector2 Position;

    public PhysicsObject[]? PhysicsObjects;

    public Memory2D<Rgba32>? WorkingTextureData;

    public Matrix4x4 PrecalculatedWorldMatrix = Matrix4x4.Identity;

    public bool ReadyToBeAddedToAtlas = false;

    public bool ReadyToBeAddedToAtlasAsAir = false;

    public Chunk(Vector2 position)
    {
        Position = position;
    }

    public void Deserialize(BinaryReader reader, MaterialProvider materialProvider)
    {
        byte[] rentedCellTable = ArrayPool<byte>.Shared.Rent(ChunkWidth * ChunkHeight);

        Span2D<byte> cellTable = rentedCellTable.AsSpan().AsSpan2D(ChunkWidth, ChunkHeight);

        cellTable.TryGetSpan(out Span<byte> flatCellTable);

        reader.Read(flatCellTable);

        string[] materialNames = ReadMaterialNames(reader);

        Material[] materials = materialProvider.CreateMaterialMap(materialNames);

        Rgba32[] customColors = ReadCustomColors(reader, out _);

        int chunkX = (int)Position.X;
        int chunkY = (int)Position.Y;

        WorkingTextureData = new Memory2D<Rgba32>(new Rgba32[ChunkHeight * ChunkWidth], ChunkHeight, ChunkWidth);

        Span2D<Rgba32> textureData = WorkingTextureData.Value.Span;

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
                    textureData[x, y] = customColors[customColorIndex];
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
                        textureData[x, y] = mat.MaterialTexture.Span[Math.Abs(x + chunkX * ChunkWidth) % mat.MaterialTexture.Width, Math.Abs(y + chunkY * ChunkHeight) % mat.MaterialTexture.Height];
                    }
                    else
                    {
                        int wx = (x + chunkX * ChunkWidth) * 6;
                        int wy = (y + chunkY * ChunkHeight) * 6;

                        int colorX = ((wx & Material.MaterialWidthM1) + Material.MaterialWidthM1) & Material.MaterialWidthM1;
                        int colorY = ((wy & Material.MaterialHeightM1) + Material.MaterialHeightM1) & Material.MaterialHeightM1;

                        textureData[x, y] = mat.MaterialTexture.Span[colorY, colorX];
                    }
                }
            }
        }

        ArrayPool<byte>.Shared.Return(rentedCellTable);

        ArrayPool<Rgba32>.Shared.Return(customColors);

        // All air optimization
        if (!wasAnyNotAir)
        {
            ReadyToBeAddedToAtlasAsAir = true;
        }
        else
        {
            PrecalculatedWorldMatrix = Matrix4x4.CreateScale(512f, 512f, 1f) * Matrix4x4.CreateTranslation(new Vector3(Position, 0f));

            ReadyToBeAddedToAtlas = true;
        }

        StatisticTimer timer = new StatisticTimer("Load Physics Objects").Begin();

        int physicsObjectCount = reader.ReadBEInt32();

        PhysicsObjects = new PhysicsObject[physicsObjectCount];

        for (int i = 0; i < physicsObjectCount; i++)
        {
            PhysicsObjects[i] = new PhysicsObject();

            PhysicsObjects[i].Deserialize(reader);
        }

        timer.End(StatisticMode.Sum);
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
