using System.Numerics;

namespace NoitaMap.Map;

public class PhysicsObject
{
    public Vector2 Position;

    public float Rotation;

    public int Width;

    public int Height;

    public Matrix4x4 PrecalculatedWorldMatrix = Matrix4x4.Identity;

    public Rgba32[,]? WorkingTextureData;

    public bool ReadyToBeAddedToAtlas = false;

    public int TextureHash { get; private set; }

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

        WorkingTextureData = new Rgba32[Width, Height];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                uint value = reader.ReadBEUInt32();
                WorkingTextureData[x, y].PackedValue = value;
                TextureHash = HashCode.Combine(TextureHash, value);
            }
        }

        PrecalculatedWorldMatrix = Matrix4x4.CreateScale(Width, Height, 1f) * (Matrix4x4.CreateRotationZ(Rotation) * Matrix4x4.CreateTranslation(new Vector3(Position, 0f)));

        ReadyToBeAddedToAtlas = true;
    }
}
