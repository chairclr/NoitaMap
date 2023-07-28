using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using NoitaMap.Viewer;

namespace NoitaMap.Map;

public class WorldPixelScenes
{
    private ViewerDisplay ViewerDisplay;

    public WorldPixelScenes(ViewerDisplay viewerDisplay)
    {
        ViewerDisplay = viewerDisplay;
    }

    public void Load(string path)
    {
        byte[]? decompressedData = NoitaDecompressor.ReadAndDecompressChunk(path);

        using (MemoryStream ms = new MemoryStream(decompressedData))
        {
            using BinaryReader reader = new BinaryReader(ms);

            int burger = reader.ReadBEInt32();

            if (burger != 3)
            {
                throw new Exception($"Burger wasn't 3 (it was {burger})");
            }

            //reader.ReadBytes(12);

            //int biomeFileLength = reader.ReadBEInt32();

            //(string ObjectPath, string VisualPath)[] biomeFileNames = new (string, string VisualPath)[biomeFileLength];

            //bool nextWithoutExtra = false;

            //for (int i = 0; i < biomeFileLength; i++)
            //{
            //    {
            //        int size = reader.ReadBEInt32();

            //        if (size > 100)
            //            Debugger.Break();

            //        byte[] stringBuffer = ArrayPool<byte>.Shared.Rent(size);

            //        reader.Read(stringBuffer.AsSpan()[..size]);

            //        biomeFileNames[i].ObjectPath = Encoding.UTF8.GetString(stringBuffer.AsSpan()[..size]);

            //        ArrayPool<byte>.Shared.Return(stringBuffer);
            //    }

            //    if (!nextWithoutExtra)
            //    {
            //        int extra = reader.ReadBEInt32();
            //    }

            //    {
            //        int size = reader.ReadBEInt32();

            //        if (size > 100)
            //        {
            //            Debugger.Break();
            //        }

            //        byte[] stringBuffer = ArrayPool<byte>.Shared.Rent(size);

            //        reader.Read(stringBuffer.AsSpan()[..size]);

            //        biomeFileNames[i].VisualPath = Encoding.UTF8.GetString(stringBuffer.AsSpan()[..size]);

            //        ArrayPool<byte>.Shared.Return(stringBuffer);
            //    }

            //    Console.WriteLine($"Read:\tObject: {biomeFileNames[i].ObjectPath}\tVisual: {biomeFileNames[i].VisualPath}");

            //    int after0 = reader.ReadBEInt32();
            //    Console.WriteLine($"A0: {after0}");

            //    int after1 = reader.ReadBEInt32();
            //    Console.WriteLine($"A1: {after1}");

            //    int after2 = reader.ReadBEInt32();
            //    Console.WriteLine($"A2: {after2}");

            //    int after3 = reader.ReadBEInt32();
            //    Console.WriteLine($"A3: {after3}");

            //    ushort hmm = reader.ReadUInt16();

            //    if (hmm == ushort.MaxValue)
            //    {
            //        nextWithoutExtra = true;
            //    }
            //    else
            //    {
            //        nextWithoutExtra = false;
            //    }

            //    Console.WriteLine($"nextWithoutExtra: {nextWithoutExtra}");

            //    short after4 = reader.ReadBEInt16();
            //    Console.WriteLine($"A4: {after4}");

            //    short after5 = reader.ReadBEInt16();
            //    Console.WriteLine($"A5: {after5}");

            //    short after6 = reader.ReadBEInt16();
            //    Console.WriteLine($"A6: {after6}");

            //    //for (int j = 0; j < 6; j++)
            //    //{
            //    //    int after = reader.ReadBEInt32();
            //    //    Console.WriteLine($"A{j}: {after}");
            //    //}
            //}
        }

        decompressedData = null;
    }
}
