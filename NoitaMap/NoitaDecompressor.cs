using NoitaMap.Compression;

namespace NoitaMap;

public static class NoitaFile
{
    public static byte[] LoadCompressedFile(string filePath)
    {
        byte[] inputBuffer;
        byte[] outputBuffer;

        using (FileStream fs = new FileStream(filePath, FileMode.Open))
        {
            using BinaryReader fileReader = new BinaryReader(fs);

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

            outputBuffer = GC.AllocateUninitializedArray<byte>(uncompressedSize);
        }

        int decompressedBytes = FastLZ.Decompress(inputBuffer, outputBuffer);

        if (outputBuffer.Length != decompressedBytes)
        {
            throw new Exception($"Failed to decompress file (compressedSize = {inputBuffer.Length}, uncompressedSize = {outputBuffer.Length}) '{filePath}'");
        }

        return outputBuffer;
    }

    public static void WriteCompressedFile(string filePath, Span<byte> data)
    {
        using FileStream fs = File.OpenWrite(filePath);
        using BinaryWriter bw = new BinaryWriter(fs);

        Span<byte> compressedData = FastLZ.Compress(1, data);

        fs.Position = 0;

        bw.Write(compressedData.Length);
        bw.Write(data.Length);
        bw.Write(compressedData);
        fs.SetLength(fs.Position);
    }
}