using System.Numerics;
using SixLabors.ImageSharp.PixelFormats;

namespace NoitaMap;

public class PhysicsObject : INoitaSerializable
{
    public byte[] UnknownData1 = new byte[12];

    public Vector2 Position;

    public float Rotation;

    public byte[] UnknownData2 = new byte[49];

    public Rgba32[,] PixelData { get; private set; }

    public int PixelWidth { get; private set; }

    public int PixelHeight { get; private set; }

    public PhysicsObject()
    {
        PixelData = new Rgba32[0, 0];
    }

    public void Deserialize(BinaryReader reader)
    {
        reader.Read(UnknownData1[..12]);

        Position.X = reader.ReadBESingle();
        Position.Y = reader.ReadBESingle();
        Rotation = reader.ReadBESingle();

        reader.Read(UnknownData2[..49]);

        PixelWidth = reader.ReadBEInt32();
        PixelHeight = reader.ReadBEInt32();

        PixelData = new Rgba32[PixelWidth, PixelHeight];

        for (int x = 0; x < PixelWidth; x++)
        {
            for (int y = 0; y < PixelHeight; y++)
            {
                uint value = reader.ReadBEUInt32();
                PixelData[x, y].PackedValue = value;
            }
        }
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(UnknownData1);

        writer.WriteBE(Position.X);
        writer.WriteBE(Position.Y);
        writer.WriteBE(Rotation);

        writer.Write(UnknownData2);

        writer.WriteBE(PixelWidth);
        writer.WriteBE(PixelHeight);

        for (int x = 0; x < PixelWidth; x++)
        {
            for (int y = 0; y < PixelHeight; y++)
            {
                writer.WriteBE(PixelData[x, y].PackedValue);
            }
        }
    }
}