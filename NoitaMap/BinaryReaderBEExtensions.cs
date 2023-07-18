using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;

namespace NoitaMap;

/// <summary>
/// Extensions for <see cref="BinaryReader"/> to read big endian data
/// </summary>
internal static class BinaryReaderBEExtensions
{
    public static int BEReadInt32(this BinaryReader reader)
    {
        int n = reader.ReadInt32();

        return BinaryPrimitives.ReadInt32BigEndian(MemoryMarshal.AsBytes(new ReadOnlySpan<int>(n)));
    }

    public static uint BEReadUInt32(this BinaryReader reader)
    {
        uint n = reader.ReadUInt32();

        return BinaryPrimitives.ReadUInt32BigEndian(MemoryMarshal.AsBytes(new ReadOnlySpan<uint>(n)));
    }

    public static long BEReadInt64(this BinaryReader reader)
    {
        long n = reader.ReadInt64();

        return BinaryPrimitives.ReadInt64BigEndian(MemoryMarshal.AsBytes(new ReadOnlySpan<long>(n)));
    }

    public static ulong BEReadUInt64(this BinaryReader reader)
    {
        ulong n = reader.ReadUInt64();

        return BinaryPrimitives.ReadUInt64BigEndian(MemoryMarshal.AsBytes(new ReadOnlySpan<ulong>(n)));
    }

    public static float BEReadSingle(this BinaryReader reader)
    {
        float n = reader.ReadSingle();

        return BinaryPrimitives.ReadSingleBigEndian(MemoryMarshal.AsBytes(new ReadOnlySpan<float>(n)));
    }

    public static double BEReadDouble(this BinaryReader reader)
    {
        double n = reader.ReadDouble();

        return BinaryPrimitives.ReadDoubleBigEndian(MemoryMarshal.AsBytes(new ReadOnlySpan<double>(n)));
    }
}
