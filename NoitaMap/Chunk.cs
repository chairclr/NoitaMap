using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using NoitaMap.Logging;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Direct3D;

namespace NoitaMap;

public class Chunk(Vector2 position)
{
    public const int ChunkSize = 512;

    public Vector2 Position = position;

    private MaterialProvider? MaterialProvider;

    public PhysicsObject[]? PhysicsObjects;

    public Rgba32[,]? WorkingTextureData;

    public Matrix4x4 WorldMatrix = Matrix4x4.Identity;

    public bool ReadyToBeAddedToAtlas = false;

    public bool ReadyToBeAddedToAtlasAsAir = false;

    private Cell[,]? CellTable;

    public List<Material>? MaterialMap;

    public Dictionary<int, int>? ReverseMaterialMap;

    public int ContainingAtlas;

    public int AtlasX;

    public int AtlasY;

    public void Deserialize(BinaryReader reader, MaterialProvider materialProvider)
    {
        MaterialProvider = materialProvider;

        byte[,] unindexedCellTable = new byte[ChunkSize, ChunkSize];
        CellTable = new Cell[ChunkSize, ChunkSize];

        reader.Read(unindexedCellTable.AsSpan());

        MaterialMap = [.. MaterialProvider.CreateMaterialMap(ReadMaterialNames(reader))];

        ReverseMaterialMap = new Dictionary<int, int>();

        for (int i = 0; i < MaterialMap.Count; i++)
        {
            Material material = MaterialMap[i];

            if (!material.IsMissing)
            {
                ReverseMaterialMap.Add(material.Index, i);
            }
        }

        Rgba32[] customColorsUnindexed = ReadCustomColors(reader, out _);

        int chunkX = (int)Position.X;
        int chunkY = (int)Position.Y;

        WorkingTextureData = new Rgba32[ChunkSize, ChunkSize];

        bool wasAnyNotAir = false;

        int customColorIndex = 0;
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                int material = unindexedCellTable[x, y] & (~0x80);
                bool customColor = (unindexedCellTable[x, y] & 0x80) != 0;

                ref Cell cell = ref CellTable[x, y];

                cell.MaterialIndex = (byte)material;

                if (customColor)
                {
                    Rgba32 col = customColorsUnindexed[customColorIndex];

                    WorkingTextureData[x, y] = col;

                    cell = cell with
                    {
                        HasCustomColor = true,
                        CustomColor = col,
                    };

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

                    Material mat = MaterialMap[material];

                    if (mat.IsMissing)
                    {
                        WorkingTextureData[x, y] = mat.MaterialTexture.Span[Math.Abs(x + chunkX * ChunkSize) % mat.MaterialTexture.Width, Math.Abs(y + chunkY * ChunkSize) % mat.MaterialTexture.Height];
                    }
                    else
                    {
                        int wx = (x + chunkX * ChunkSize) * 6;
                        int wy = (y + chunkY * ChunkSize) * 6;

                        int colorX = ((wx % Material.MaterialWidth) + Material.MaterialWidth) % Material.MaterialWidth;
                        int colorY = ((wy % Material.MaterialHeight) + Material.MaterialHeight) % Material.MaterialHeight;

                        WorkingTextureData[x, y] = mat.MaterialTexture.Span[colorY, colorX];
                    }
                }
            }
        }

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

    public void Serialize(BinaryWriter writer)
    {
        byte[,] unindexedCellTable = new byte[ChunkSize, ChunkSize];

        List<Rgba32> customColors = new List<Rgba32>();

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                unindexedCellTable[x, y] = (byte)CellTable![x, y].MaterialIndex;

                if (CellTable[x, y].HasCustomColor)
                {
                    customColors.Add(CellTable[x, y].CustomColor);

                    unindexedCellTable[x, y] |= 0x80;
                }
            }
        }

        // --- HEADER ---
        // Version = 24
        writer.WriteBE(24);
        writer.WriteBE(ChunkSize);
        writer.WriteBE(ChunkSize);

        // --- CELL DATA ---
        writer.Write(unindexedCellTable.AsSpan());

        writer.WriteBE(MaterialMap!.Count);

        foreach (Material material in MaterialMap)
        {
            writer.WriteNoitaString(material.Name);
        }

        writer.WriteBE(customColors!.Count);

        foreach (Rgba32 col in customColors)
        {
            writer.WriteBE(col.PackedValue);
        }

        // --- PHYSICS OBJECTS ---
        if (PhysicsObjects is not null)
        {
            writer.WriteBE(PhysicsObjects.Length);

            foreach (PhysicsObject physicsObject in PhysicsObjects)
            {
                physicsObject.Serialize(writer);
            }
        }
        else
        {
            writer.WriteBE(0);
        }

        // ??
        writer.WriteBE(0);
    }

    public void Invalidate()
    {
        if (MaterialProvider is null || MaterialMap is null || CellTable is null)
        {
            return;
        }

        int chunkX = (int)Position.X;
        int chunkY = (int)Position.Y;

        WorkingTextureData = new Rgba32[ChunkSize, ChunkSize];

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                ref Cell cell = ref CellTable[x, y];

                int material = cell.MaterialIndex;
                bool customColor = cell.HasCustomColor;

                if (customColor)
                {
                    WorkingTextureData[x, y] = cell.CustomColor;
                }
                else
                {
                    if (material == 0)
                    {
                        continue;
                    }

                    Material mat = MaterialMap[material];

                    if (mat.IsMissing)
                    {
                        WorkingTextureData[x, y] = mat.MaterialTexture.Span[Math.Abs(x + chunkX * ChunkSize) % mat.MaterialTexture.Width, Math.Abs(y + chunkY * ChunkSize) % mat.MaterialTexture.Height];
                    }
                    else
                    {
                        int wx = (x + chunkX * ChunkSize) * 6;
                        int wy = (y + chunkY * ChunkSize) * 6;

                        int colorX = ((wx % Material.MaterialWidth) + Material.MaterialWidthM1) % Material.MaterialWidthM1;
                        int colorY = ((wy % Material.MaterialHeight) + Material.MaterialHeightM1) % Material.MaterialHeightM1;

                        WorkingTextureData[x, y] = mat.MaterialTexture.Span[colorY, colorX];
                    }
                }
            }
        }

        ReadyToBeAddedToAtlas = true;
    }

    public void SetPixel(int x, int y, Material material)
    {
        if (!MaterialMap!.Contains(material))
        {
            MaterialMap.Add(material);
            ReverseMaterialMap!.Add(material.Index, MaterialMap.Count - 1);
        }

        CellTable![x, y].HasCustomColor = false;
        CellTable![x, y] = CellTable![x, y] with
        {
            HasCustomColor = false,
            MaterialIndex = (byte)ReverseMaterialMap![material.Index]
        };
    }

    public void SetPixel(int x, int y, Material material, Rgba32 customColor)
    {
        if (!MaterialMap!.Contains(material))
        {
            MaterialMap.Add(material);
            ReverseMaterialMap!.Add(material.Index, MaterialMap.Count - 1);
        }

        CellTable![x, y] = CellTable![x, y] with
        {
            HasCustomColor = true,
            CustomColor = customColor,
            MaterialIndex = (byte)ReverseMaterialMap![material.Index]
        };
    }

    public Material GetPixel(int x, int y)
    {
        return MaterialMap![CellTable![x, y].MaterialIndex];
    }

    public unsafe void SetBulkCircle(int rx, int ry, float r, Material material)
    {
        if (CellTable is null)
        {
            return;
        }

        if (!MaterialMap!.Contains(material))
        {
            MaterialMap.Add(material);
            ReverseMaterialMap!.Add(material.Index, MaterialMap.Count - 1);
        }

        byte newIndex = (byte)ReverseMaterialMap![material.Index];

        int startX = (int)float.Clamp(rx - r, 0f, ChunkSize);
        int endX = (int)float.Clamp(rx + r, 0f, ChunkSize);

        int startY = (int)float.Clamp(ry - r, 0f, ChunkSize);
        int endY = (int)float.Clamp(ry + r, 0f, ChunkSize);

        float rsqr = r * r;

        Cell newCell = new Cell()
        {
            MaterialIndex = newIndex
        };

        Span<Cell> cells = stackalloc Cell[endX - startX];

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                float dx = rx - x;
                float dy = ry - y;

                if (dx * dx + dy * dy < rsqr)
                {
                    cells[x - startX] = newCell;
                }
                else
                {
                    Unsafe.CopyBlock(
                            ref Unsafe.As<Cell, byte>(ref cells.DangerousGetReferenceAt(x - startX)),
                            ref Unsafe.As<Cell, byte>(ref CellTable.DangerousGetReferenceAt(y, x)),
                            (uint)sizeof(Cell));
                }
            }

            Unsafe.CopyBlock(
                    ref Unsafe.As<Cell, byte>(ref CellTable.DangerousGetReferenceAt(y, startX)),
                    ref Unsafe.As<Cell, byte>(ref cells.DangerousGetReference()),
                    (uint)cells.Length * (uint)sizeof(Cell));
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

        Rgba32[] materialWorldColors = new Rgba32[materialWorldColorCount];

        for (int i = 0; i < materialWorldColorCount; i++)
        {
            materialWorldColors[i].PackedValue = reader.ReadBEUInt32();
        }

        return materialWorldColors;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Cell
    {
        public Rgba32 CustomColor;

        public byte MaterialIndex;

        public bool HasCustomColor;
    }
}