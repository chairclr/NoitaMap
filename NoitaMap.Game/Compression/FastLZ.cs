using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NoitaMap.Game.Compression;

public class FastLZ
{
    static FastLZ()
    {
        NativeLibrary.SetDllImportResolver(typeof(FastLZ).Assembly, (dllName, assembly, importSearchPath) =>
        {
            if (dllName == "fastlz")
            {
                if (Environment.Is64BitProcess)
                {
                    return NativeLibrary.Load(Path.Combine(Path.GetDirectoryName(assembly.Location)!, "Compression", "x64", "fastlz.dll"));
                }
                else
                {
                    return NativeLibrary.Load(Path.Combine(Path.GetDirectoryName(assembly.Location)!, "Compression", "x86", "fastlz.dll"));
                }
            }

            return (nint)0;
        });
    }

    /// <summary>
    /// Decompresses a <![CDATA[Span<byte>]]>
    /// </summary>
    /// <param name="input">Bytes to decompress</param>
    /// <param name="output">Output buffer for decompressed bytes</param>
    /// <returns>Number of bytes decompressed</returns>
    public static int Decompress(Span<byte> input, Span<byte> output)
    {
        return fastlz_decompress(ref MemoryMarshal.GetReference(input), input.Length, ref MemoryMarshal.GetReference(output), output.Length);
    }

    [DllImport("fastlz")]
    private static extern int fastlz_decompress(ref byte input, int inputLength, ref byte output, int outputLength);
}
