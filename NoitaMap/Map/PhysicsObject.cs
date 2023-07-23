using System.Numerics;
using NoitaMap.Viewer;

namespace NoitaMap.Map;

public class PhysicsObject
{
    public ViewerDisplay ViewerDisplay;

    public Vector2 Position;

    public float Rotation;

    public int Width;

    public int Height;

    public int TextureHash;

    //public QuadVertexBuffer<Vertex>? Buffer;

    public Matrix4x4 PrecalculatedWorldMatrix = Matrix4x4.Identity;

    public bool Ready = false;

    public PhysicsObject(ViewerDisplay viewerDisplay)
    {
        ViewerDisplay = viewerDisplay;
    }

    public void Deserialize(BinaryReader reader)
    {
        reader.ReadBEUInt64();
        reader.ReadBEUInt32();
        Position.X = reader.ReadBESingle();
        Position.Y = reader.ReadBESingle();
        Rotation = reader.ReadBESingle();
        reader.ReadBEInt64();
        reader.ReadBEInt64();
        reader.ReadBEInt64();
        reader.ReadBEInt64();
        reader.ReadBEInt64();
        reader.ReadBoolean();
        reader.ReadBoolean();
        reader.ReadBoolean();
        reader.ReadBoolean();
        reader.ReadBoolean();
        reader.ReadBESingle();
        Width = reader.ReadBEInt32();
        Height = reader.ReadBEInt32();

        Rgba32[,] textureData = new Rgba32[Width, Height];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                uint value = reader.ReadBEUInt32();
                textureData[x, y].PackedValue = value;
                TextureHash = HashCode.Combine(TextureHash, value);
            }
        }

        //Texture texture = ViewerDisplay.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription()
        //{
        //    Type = TextureType.Texture2D,
        //    Format = PixelFormat.R8_G8_B8_A8_UNorm,
        //    Width = (uint)Width,
        //    Height = (uint)Height,
        //    Usage = TextureUsage.Sampled,
        //    MipLevels = 1,

        //    // Nececessary
        //    Depth = 1,
        //    ArrayLayers = 1,
        //    SampleCount = TextureSampleCount.Count1,
        //});

        //ViewerDisplay.GraphicsDevice.UpdateTexture(texture, MemoryMarshal.CreateSpan(ref textureData[0, 0], Width * Height), 0, 0, 0, (uint)Width, (uint)Height, 1, 0, 0);

        //Buffer = new QuadVertexBuffer<Vertex>(ViewerDisplay.GraphicsDevice, new Vector2(Width, Height), (pos, uv) => new Vertex()
        //{
        //    Position = new Vector3(pos, 0f),
        //    UV = uv
        //}, ViewerDisplay.CreateResourceSet(texture));

        PrecalculatedWorldMatrix = Matrix4x4.CreateTranslation(new Vector3(Position, 0f));

        Ready = true;
    }
}
