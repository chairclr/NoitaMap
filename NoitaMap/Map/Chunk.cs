using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using NoitaMap.Viewer;

namespace NoitaMap.Map;

public class Chunk
{
    public const int ChunkWidth = 512;

    public const int ChunkHeight = 512;

    public Vector2 Position;

    public PhysicsObject[]? PhysicsObjects;

    private readonly ViewerDisplay ViewerDisplay;

    private readonly MaterialProvider MaterialProvider;

    public Rgba32[,]? WorkingTextureData;

    public Matrix4x4 PrecalculatedWorldMatrix = Matrix4x4.Identity;

    public bool ReadyToBeAddedToAtlas = false;

    public Chunk(ViewerDisplay viewerDisplay, Vector2 position, MaterialProvider materialProvider)
    {
        ViewerDisplay = viewerDisplay;

        Position = position;

        MaterialProvider = materialProvider;
    }

    public void Deserialize(BinaryReader reader)
    {
        byte[,] cellTable = new byte[ChunkWidth, ChunkHeight];

        reader.Read(MemoryMarshal.CreateSpan(ref cellTable[0, 0], ChunkWidth * ChunkHeight));

        string[] materialNames = ReadMaterialNames(reader);

        Material[] materials = MaterialProvider.CreateMaterialMap(materialNames);

        Rgba32[] customColors = ReadCustomColors(reader);

        int chunkX = (int)Position.X;
        int chunkY = (int)Position.Y;

        WorkingTextureData = new Rgba32[ChunkWidth, ChunkHeight];

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
                }
                else
                {
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

        PrecalculatedWorldMatrix = Matrix4x4.CreateTranslation(new Vector3(Position, 0f));

        ReadyToBeAddedToAtlas = true;
    }

    private string[] ReadMaterialNames(BinaryReader reader)
    {
        int materialNameCount = reader.ReadBEInt32();

        string[] materialNames = new string[materialNameCount];

        for (int i = 0; i < materialNameCount; i++)
        {
            int size = reader.ReadBEInt32();

            // rent a buffer here for fast :thumbs_up:
            byte[] stringBuffer = ArrayPool<byte>.Shared.Rent(size);

            reader.Read(stringBuffer.AsSpan()[..size]);

            materialNames[i] = Encoding.UTF8.GetString(stringBuffer.AsSpan()[..size]);

            ArrayPool<byte>.Shared.Return(stringBuffer);
        }

        return materialNames;
    }

    private Rgba32[] ReadCustomColors(BinaryReader reader)
    {
        int materialWorldColorCount = reader.ReadBEInt32();

        Rgba32[] materialWorldColors = new Rgba32[materialWorldColorCount];

        for (int i = 0; i < materialWorldColorCount; i++)
        {
            materialWorldColors[i].PackedValue = reader.ReadBEUInt32();
        }

        return materialWorldColors;
    }
}
