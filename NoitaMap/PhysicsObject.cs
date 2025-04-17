using System.Numerics;
using SixLabors.ImageSharp.PixelFormats;

namespace NoitaMap;

public class PhysicsObject : INoitaSerializable
{
    public UnknownDataBlock1 UnknownData1;

    public Vector2 Position;

    public float Rotation;

    public UnknownDataBlock2 UnknownData2;

    public Rgba32[,] PixelData { get; private set; }

    public int PixelWidth { get; private set; }

    public int PixelHeight { get; private set; }

    public PhysicsObject()
    {
        PixelData = new Rgba32[0, 0];
    }

    public void Deserialize(BinaryReader reader)
    {
        reader.Read(UnknownData1.Data);

        Position.X = reader.ReadBESingle();
        Position.Y = reader.ReadBESingle();
        Rotation = reader.ReadBESingle();

        reader.Read(UnknownData2.Data);

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
        writer.Write(UnknownData1.Data);

        writer.WriteBE(Position.X);
        writer.WriteBE(Position.Y);
        writer.WriteBE(Rotation);

        writer.Write(UnknownData2.Data);

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

    public unsafe struct UnknownDataBlock1
    {
        private fixed byte _data[12];

        public Span<byte> Data => new(_data, 0, 12);
    }

    public unsafe struct UnknownDataBlock2
    {
        private fixed byte _data[49];

        public Span<byte> Data => new(_data, 0, 49);
    }
}