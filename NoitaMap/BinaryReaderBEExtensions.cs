using System.Buffers.Binary;
using System.Runtime.InteropServices;

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
}
