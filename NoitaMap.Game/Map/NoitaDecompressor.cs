using System;
using System.IO;
using NoitaMap.Game.Compression;

namespace NoitaMap.Game.Map;

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
            outputBuffer = GC.AllocateUninitializedArray<byte>(uncompressedSize);

            fileReader.Read(inputBuffer);
        }

        int decompressedBytes = FastLZ.Decompress(inputBuffer, outputBuffer);

        if (outputBuffer.Length != decompressedBytes)
        {
            throw new Exception();
        }

        return outputBuffer;
    }
}
