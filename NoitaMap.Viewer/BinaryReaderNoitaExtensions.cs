using System.Text;

namespace NoitaMap;

public static class BinaryNoitaExtensions
{
    public static string? ReadNoitaString(this BinaryReader reader)
    {
        int size = reader.ReadBEInt32();

        if (size <= 0)
        {
            return null;
        }

        if (size > 8192 * 100)
        {
            return null;
        }

        // stackalloc a buffer here for extra fast
        Span<byte> stringBuffer = stackalloc byte[size];

        reader.Read(stringBuffer);

        return Encoding.UTF8.GetString(stringBuffer);
    }

    public static void WriteNoitaString(this BinaryWriter writer, in string? str)
    {
        writer.WriteBE(str?.Length ?? 0);

        if ((str?.Length ?? 0) <= 0)
        {
            return;
        }

        int size = str!.Length;

        // rent a buffer here for fast :thumbs_up:
        Span<byte> stringBuffer = stackalloc byte[size];

        Encoding.UTF8.GetBytes(str, stringBuffer);

        writer.Write(stringBuffer);
    }
}