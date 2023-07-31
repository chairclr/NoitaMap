using System.Buffers;
using System.Text;

namespace NoitaMap;

public static class BinaryReaderNoitaExtensions
{
    public static string? ReadNoitaString(this BinaryReader reader)
    {
        int size = reader.ReadBEInt32();

        if (size <= 0)
        {
            return null;
        }

        // rent a buffer here for fast :thumbs_up:
        byte[] stringBuffer = ArrayPool<byte>.Shared.Rent(size);

        reader.Read(stringBuffer.AsSpan()[..size]);

        string str = Encoding.UTF8.GetString(stringBuffer.AsSpan()[..size]);

        ArrayPool<byte>.Shared.Return(stringBuffer);

        return str;
    }
}
