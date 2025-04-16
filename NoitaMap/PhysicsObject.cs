using System.Numerics;
using SixLabors.ImageSharp.PixelFormats;

namespace NoitaMap;

public class PhysicsObject
{
    public Vector2 Position;

    public float Rotation;

    public Matrix4x4 WorldMatrix { get; private set; }

    public Rgba32[,]? WorkingTextureData { get; set; }

    public int TextureWidth { get; private set; }

    public int TextureHeight { get; private set; }

    public int TextureHash { get; private set; }

    private byte[]? SerializationData;

    public void Deserialize(BinaryReader reader)
    {
        long pos = reader.BaseStream.Position;
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
        TextureWidth = reader.ReadBEInt32();
        TextureHeight = reader.ReadBEInt32();

        WorkingTextureData = new Rgba32[TextureWidth, TextureHeight];

        for (int x = 0; x < TextureWidth; x++)
        {
            for (int y = 0; y < TextureHeight; y++)
            {
                uint value = reader.ReadBEUInt32();
                WorkingTextureData[x, y].PackedValue = value;
                TextureHash = HashCode.Combine(TextureHash, value);
            }
        }

        long len = reader.BaseStream.Position - pos;

        // Rereading everything but uh
        SerializationData = new byte[len];
        reader.BaseStream.Position = pos;
        reader.Read(SerializationData);

        WorldMatrix = Matrix4x4.CreateScale(TextureWidth, TextureHeight, 1f) * (Matrix4x4.CreateRotationZ(Rotation) * Matrix4x4.CreateTranslation(new Vector3(Position, 0f)));
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(SerializationData!);
    }
}
