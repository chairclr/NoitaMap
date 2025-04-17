using System.Buffers;
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

        // Hardcap to 10 million chars, just in case we make a funny mistake reading bytes
        if (size > 10_000_000)
        {
            return null;
        }

        // If we can fit this string on the stack, do so
        if (size < 500_000)
        {
            // stackalloc a buffer here for extra fast
            Span<byte> stringBuffer = stackalloc byte[size];

            reader.Read(stringBuffer);

            return Encoding.UTF8.GetString(stringBuffer);
        }
        else
        {
            // Rent a buffer here for extra fast
            byte[] stringBuffer = ArrayPool<byte>.Shared.Rent(size);

            reader.Read(stringBuffer);

            string str =Encoding.UTF8.GetString(stringBuffer);

            ArrayPool<byte>.Shared.Return(stringBuffer);

            return str;
        }
    }

    public static void WriteNoitaString(this BinaryWriter writer, in string? str)
    {
        writer.WriteBE(str?.Length ?? 0);

        if ((str?.Length ?? 0) <= 0)
        {
            return;
        }

        int size = str!.Length;


        // If we can fit this string on the stack, do so
        if (size < 500_000)
        {
            // stackalloc a buffer here for extra fast
            Span<byte> stringBuffer = stackalloc byte[size];

            Encoding.UTF8.GetBytes(str, stringBuffer);
            writer.Write(stringBuffer);
        }
        else
        {
            // Rent a buffer here for extra fast
            byte[] stringBuffer = ArrayPool<byte>.Shared.Rent(size);

            Encoding.UTF8.GetBytes(str, stringBuffer);
            writer.Write(stringBuffer);

            ArrayPool<byte>.Shared.Return(stringBuffer);
        }
    }
}