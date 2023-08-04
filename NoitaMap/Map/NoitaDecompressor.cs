using NoitaMap.Compression;

namespace NoitaMap.Map;

public static class NoitaDecompressor
{
    public static byte[] ReadAndDecompressChunk(string filePath)
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

            fileReader.Read(inputBuffer);

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
}
