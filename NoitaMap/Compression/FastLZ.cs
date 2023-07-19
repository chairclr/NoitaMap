using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance;

namespace NoitaMap.Compression;

public partial class FastLZ
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
        return fastlz_decompress(ref input.DangerousGetReference(), input.Length, ref output.DangerousGetReference(), output.Length);
    }

    [LibraryImport("fastlz")]
    private static partial int fastlz_decompress(ref byte input, int inputLength, ref byte output, int outputLength);
}