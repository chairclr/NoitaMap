using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using NoitaMap.Graphics;
using NoitaMap.Viewer;
using Veldrid;

namespace NoitaMap.Map;

public class Chunk
{
    public const int ChunkWidth = 512;

    public const int ChunkHeight = 512;

    public Vector2 Position;

    private readonly ViewerDisplay ViewerDisplay;

    private readonly MaterialProvider MaterialProvider;

    public QuadVertexBuffer<Vertex>? Buffer;

    public bool Ready = false;

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

        Rgba32[,] textureData = new Rgba32[ChunkWidth, ChunkHeight];

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
                }
                else
                {
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

                        textureData[x, y] = mat.MaterialTexture.Span[colorX, colorY];
                    }
                }
            }
        }

        Texture texture = ViewerDisplay.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription()
        {
            Type = TextureType.Texture2D,
            Format = PixelFormat.R8_G8_B8_A8_UNorm,
            Width = ChunkWidth,
            Height = ChunkHeight,
            Usage = TextureUsage.Sampled,
            MipLevels = 1,

            // Nececessary
            Depth = 1,
            ArrayLayers = 1,
            SampleCount = TextureSampleCount.Count1,
        });

        ViewerDisplay.GraphicsDevice.UpdateTexture(texture, MemoryMarshal.CreateSpan(ref textureData[0, 0], ChunkWidth * ChunkHeight), 0, 0, 0, ChunkWidth, ChunkHeight, 1, 0, 0);

        Buffer = new QuadVertexBuffer<Vertex>(ViewerDisplay.GraphicsDevice, new Vector2(ChunkWidth, ChunkHeight), (pos, uv) => new Vertex()
        {
            Position = new Vector3(pos, 0f),
            UV = uv
        }, ViewerDisplay.CreateResourceSet(texture));

        Ready = true;

        //ReadyForTextureCreation = true;

        //// Physics objects
        //int physicsObjectCount = reader.ReadBEInt32();

        //PhysicsObjects = new PhysicsObject[physicsObjectCount];

        //for (int i = 0; i < physicsObjectCount; i++)
        //{
        //    PhysicsObjects[i] = new PhysicsObject();

        //    PhysicsObjects[i].Deserialize(reader);
        //}
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
