using NoitaMap.Compression;
using NoitaMap.Logging;

namespace NoitaMap;

public static class NoitaFile
{
    /// <summary>
    /// Reads a fastlz compressed Noita file into memory and then decompresses it. Throws <see cref="InvalidDataException"/> when decompression fails.
    /// </summary>
    public static byte[] LoadCompressedFile(string filePath)
    {
        byte[] inputBuffer;
        byte[] outputBuffer;

        using (FileStream fs = new(filePath, FileMode.Open))
        {
            using BinaryReader fileReader = new(fs);

            int compressedSize = fileReader.ReadInt32();
            int uncompressedSize = fileReader.ReadInt32();

            // We can be sure that we will fill both of these buffers completely
            inputBuffer = GC.AllocateUninitializedArray<byte>(compressedSize);

            fs.ReadExactly(inputBuffer);

            // If the compressed size is equal to the uncompressed size, then the file isn't compressed
            if (compressedSize == uncompressedSize)
            {
                return inputBuffer;
            }

            // Again, we can use AllocateUninit because we're using the whole thing
            outputBuffer = GC.AllocateUninitializedArray<byte>(uncompressedSize);
        }

        int decompressedBytes = FastLZ.Decompress(inputBuffer, outputBuffer);

        if (outputBuffer.Length != decompressedBytes)
        {
            Log.LogCrit($"Failed to decompress {filePath}: {outputBuffer.Length} != {decompressedBytes}");
            throw new InvalidDataException();
        }

        return outputBuffer;
    }

    /// <summary>
    /// Compresses <paramref name="data"/> and writes it into <paramref name="filePath"/> in Noita's fastlz format
    /// </summary>
    public static void WriteCompressedFile(string filePath, Span<byte> data)
    {
        using FileStream fs = File.OpenWrite(filePath);
        using BinaryWriter bw = new(fs);

        Span<byte> compressedData = FastLZ.Compress(1, data);

        fs.Position = 0;

        bw.Write(compressedData.Length);
        bw.Write(data.Length);
        bw.Write(compressedData);
        fs.SetLength(fs.Position);
    }
}