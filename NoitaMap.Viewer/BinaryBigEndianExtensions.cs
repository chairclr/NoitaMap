using System.Buffers.Binary;

namespace NoitaMap;

public static class BinaryBigEndianExtensions
{
    public static short ReadBEInt16(this BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[2];
        reader.Read(buffer);
        return BinaryPrimitives.ReadInt16BigEndian(buffer);
    }

    public static ushort ReadBEUInt16(this BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[2];
        reader.Read(buffer);
        return BinaryPrimitives.ReadUInt16BigEndian(buffer);
    }

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


    public static void WriteBE(this BinaryWriter writer, short value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buffer, value);
        writer.Write(buffer);
    }

    public static void WriteBE(this BinaryWriter writer, ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        writer.Write(buffer);
    }

    public static void WriteBE(this BinaryWriter writer, int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        writer.Write(buffer);
    }

    public static void WriteBE(this BinaryWriter writer, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        writer.Write(buffer);
    }

    public static void WriteBE(this BinaryWriter writer, long value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(buffer, value);
        writer.Write(buffer);
    }

    public static void WriteBE(this BinaryWriter writer, ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
        writer.Write(buffer);
    }

    public static void WriteBE(this BinaryWriter writer, float value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteSingleBigEndian(buffer, value);
        writer.Write(buffer);
    }

    public static void WriteBE(this BinaryWriter writer, double value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleBigEndian(buffer, value);
        writer.Write(buffer);
    }
}