using System.Buffers.Binary;

namespace NoitaMap;

public static class BinaryReaderBigEndianExtensions
{
    public static int ReadBEInt32(this BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[4];
        reader.Read(buffer);
        return BinaryPrimitives.ReadInt32BigEndian(buffer);
    }

    public static uint ReadBEUInt32(this BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[4];
        reader.Read(buffer);
        return BinaryPrimitives.ReadUInt32BigEndian(buffer);
    }

    public static long ReadBEInt64(this BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[8];
        reader.Read(buffer);
        return BinaryPrimitives.ReadInt64BigEndian(buffer);
    }

    public static ulong ReadBEUInt64(this BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[8];
        reader.Read(buffer);
        return BinaryPrimitives.ReadUInt64BigEndian(buffer);
    }

    public static float ReadBESingle(this BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[4];
        reader.Read(buffer);
        return BinaryPrimitives.ReadSingleBigEndian(buffer);
    }

    public static double ReadBEDouble(this BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[8];
        reader.Read(buffer);
        return BinaryPrimitives.ReadDoubleBigEndian(buffer);
    }
}