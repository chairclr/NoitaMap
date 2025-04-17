using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using SixLabors.ImageSharp.PixelFormats;

namespace NoitaMap;

public class Chunk : INoitaSerializable
{
    public const int ChunkSize = 512;

    public readonly int X;
    public readonly int Y;
    public Vector2 Position => new(X, Y);

    private readonly Cell[,] _cellTable;

    private readonly MaterialProvider _materialProvider;
    private List<Material> _materialMap = [];
    private Dictionary<int, int> _reverseMaterialMap = [];

    public List<PhysicsObject> PhysicsObjects = [];

    public bool IsAllAir { get; private set; }

    public Chunk(int x, int y, MaterialProvider materialProvider)
    {
        X = x;
        Y = y;
        _materialProvider = materialProvider;

        _cellTable = new Cell[ChunkSize, ChunkSize];
    }

    public void Deserialize(BinaryReader reader)
    {
        int version = reader.ReadBEInt32();
        int width = reader.ReadBEInt32();
        int height = reader.ReadBEInt32();

        if (version != 24 || width != ChunkSize || height != ChunkSize)
        {
            throw new InvalidDataException($"Chunk header was not correct. Version = {version} Width = {width} Height = {height}");
        }

        byte[,] unindexedCellTable = new byte[ChunkSize, ChunkSize];

        reader.Read(unindexedCellTable.AsSpan());

        _materialMap = [.. _materialProvider.CreateMaterialMap(ReadMaterialNames(reader))];

        _reverseMaterialMap = [];

        for (int i = 0; i < _materialMap.Count; i++)
        {
            Material material = _materialMap[i];

            if (!material.IsMissing)
            {
                _reverseMaterialMap.Add(material.Index, i);
            }
        }

        Rgba32[] customColors = ReadCustomColors(reader, out _);

        int airMask = 0;

        int customColorIndex = 0;
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                int material = unindexedCellTable[x, y] & (~0x80);
                bool customColor = (unindexedCellTable[x, y] & 0x80) != 0;

                ref Cell cell = ref _cellTable[x, y];

                cell.MaterialIndex = (byte)material;

                if (customColor)
                {
                    Rgba32 col = customColors[customColorIndex];

                    cell = cell with
                    {
                        HasCustomColor = true,
                        CustomColor = col,
                    };

                    // explicit > implicit
                    customColorIndex++;

                    airMask |= 1;
                }
                else
                {
                    airMask |= material;
                }
            }
        }

        // airMask will be 0 if there were no custom color materials and no indexed materials
        if (airMask == 0)
        {
            IsAllAir = true;
        }

        PhysicsObjects = [];
        int physicsObjectCount = reader.ReadBEInt32();

        for (int i = 0; i < physicsObjectCount; i++)
        {
            PhysicsObject physicsObject = new();
            physicsObject.Deserialize(reader);

            PhysicsObjects.Add(physicsObject);
        }
    }

    public void Serialize(BinaryWriter writer)
    {
        byte[,] unindexedCellTable = new byte[ChunkSize, ChunkSize];

        List<Rgba32> customColors = [];

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                unindexedCellTable[x, y] = (byte)_cellTable![x, y].MaterialIndex;

                if (_cellTable[x, y].HasCustomColor)
                {
                    customColors.Add(_cellTable[x, y].CustomColor);

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
        writer.WriteBE(_materialMap!.Count);
        foreach (Material material in _materialMap)
        {
            writer.WriteNoitaString(material.Name);
        }

        writer.WriteBE(customColors!.Count);
        foreach (Rgba32 col in customColors)
        {
            writer.WriteBE(col.PackedValue);
        }

        // --- PHYSICS OBJECTS ---
        writer.WriteBE(PhysicsObjects.Count);

        foreach (PhysicsObject physicsObject in PhysicsObjects)
        {
            physicsObject.Serialize(writer);
        }

        // ?? No idea, but seems to end in an extra 4 null bytes
        writer.WriteBE(0);
    }

    public Rgba32[,] GetPixelData()
    {
        Rgba32[,] pixelData = new Rgba32[ChunkSize, ChunkSize];

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                ref Cell cell = ref _cellTable[x, y];

                int material = cell.MaterialIndex;
                bool customColor = cell.HasCustomColor;

                if (customColor)
                {
                    pixelData[x, y] = cell.CustomColor;
                }
                else
                {
                    if (material == 0)
                    {
                        continue;
                    }

                    Material mat = _materialMap[material];

                    if (mat.IsMissing)
                    {
                        pixelData[x, y] = mat.MaterialPixels.Span[0, 0];
                    }
                    else
                    {
                        int wx = (x + (X * ChunkSize)) * 6;
                        int wy = (y + (Y * ChunkSize)) * 6;

                        int colorX = ((wx % Material.MaterialWidth) + Material.MaterialWidthM1) % Material.MaterialWidthM1;
                        int colorY = ((wy % Material.MaterialHeight) + Material.MaterialHeightM1) % Material.MaterialHeightM1;

                        pixelData[x, y] = mat.MaterialPixels.Span[colorY, colorX];
                    }
                }
            }
        }

        return pixelData;
    }

    public Material GetPixel(int x, int y)
    {
        return _materialMap![_cellTable![x, y].MaterialIndex];
    }

    public void SetPixel(int x, int y, Material material)
    {
        if (!_materialMap!.Contains(material))
        {
            _materialMap.Add(material);
            _reverseMaterialMap!.Add(material.Index, _materialMap.Count - 1);
        }

        _cellTable![x, y].HasCustomColor = false;
        _cellTable![x, y] = _cellTable![x, y] with
        {
            HasCustomColor = false,
            MaterialIndex = (byte)_reverseMaterialMap![material.Index]
        };
    }

    public void SetPixel(int x, int y, Material material, Rgba32 customColor)
    {
        if (!_materialMap!.Contains(material))
        {
            _materialMap.Add(material);
            _reverseMaterialMap!.Add(material.Index, _materialMap.Count - 1);
        }

        _cellTable![x, y] = _cellTable![x, y] with
        {
            HasCustomColor = true,
            CustomColor = customColor,
            MaterialIndex = (byte)_reverseMaterialMap![material.Index]
        };
    }

    public unsafe void SetBulkCircle(int rx, int ry, float r, Material material)
    {
        if (_cellTable is null)
        {
            return;
        }

        if (!_materialMap!.Contains(material))
        {
            _materialMap.Add(material);
            _reverseMaterialMap!.Add(material.Index, _materialMap.Count - 1);
        }

        byte newIndex = (byte)_reverseMaterialMap![material.Index];

        int startX = (int)float.Clamp(rx - r, 0f, ChunkSize);
        int endX = (int)float.Clamp(rx + r, 0f, ChunkSize);

        int startY = (int)float.Clamp(ry - r, 0f, ChunkSize);
        int endY = (int)float.Clamp(ry + r, 0f, ChunkSize);

        float rsqr = r * r;

        Cell newCell = new()
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
                            ref Unsafe.As<Cell, byte>(ref _cellTable.DangerousGetReferenceAt(y, x)),
                            (uint)sizeof(Cell));
                }
            }

            Unsafe.CopyBlock(
                    ref Unsafe.As<Cell, byte>(ref _cellTable.DangerousGetReferenceAt(y, startX)),
                    ref Unsafe.As<Cell, byte>(ref cells.DangerousGetReference()),
                    (uint)cells.Length * (uint)sizeof(Cell));
        }
    }

    private static string[] ReadMaterialNames(BinaryReader reader)
    {
        int materialNameCount = reader.ReadBEInt32();

        string[] materialNames = new string[materialNameCount];

        for (int i = 0; i < materialNameCount; i++)
        {
            materialNames[i] = reader.ReadNoitaString()!;
        }

        return materialNames;
    }

    private static Rgba32[] ReadCustomColors(BinaryReader reader, out int materialWorldColorCount)
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
    public struct Cell
    {
        public Rgba32 CustomColor;

        public byte MaterialIndex;

        public bool HasCustomColor;
    }
}