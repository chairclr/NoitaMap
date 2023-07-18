using System.Runtime.InteropServices;

namespace NoitaMap;

internal static partial class FastLZ
{
    static FastLZ()
    {
        NativeLibrary.SetDllImportResolver(typeof(FastLZ).Assembly, (x, y, z) =>
        {
            // TODO: linux/macos? support
            
            if (x == "fastlz")
            {
                if (Environment.Is64BitProcess)
                {
                    return NativeLibrary.Load(Path.Combine(Path.GetDirectoryName(y.Location)!, "x64", "fastlz.dll"));
                }

                return NativeLibrary.Load(Path.Combine(Path.GetDirectoryName(y.Location)!, "x86", "fastlz.dll"));
            }
            else
            {
                return 0;
            }
        });
    }

    public static int Decompress(Span<byte> input, Span<byte> output)
    {
        return fastlz_decompress(input, input.Length, output, output.Length);
    }

    [LibraryImport("fastlz")]
    private static partial int fastlz_decompress(Span<byte> input, int inputLength, Span<byte> output, int outputLength);
}
