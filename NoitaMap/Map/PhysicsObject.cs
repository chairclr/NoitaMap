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

        PrecalculatedWorldMatrix = Matrix4x4.CreateTranslation(new Vector3(Position, 0f));

        Ready = true;
    }
}
