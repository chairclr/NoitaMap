using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using NoitaMap.Logging;

namespace NoitaMap.Compression;

public partial class FastLZ
{
    static FastLZ()
    {
        NativeLibrary.SetDllImportResolver(typeof(FastLZ).Assembly, static (dllName, assembly, importSearchPath) =>
        {
            if (dllName == "fastlz")
            {
                string libraryPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Libraries");

                if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
                {
                    throw new PlatformNotSupportedException();
                }

                else if (OperatingSystem.IsLinux())
                {
                    return NativeLibrary.Load(Path.Combine(libraryPath, "linux", "libfastlz.so"));
                }
                else if (OperatingSystem.IsMacOS())
                {
                    return NativeLibrary.Load(Path.Combine(libraryPath, "win", "fastlz.dll"));
                }
            }

            Log.LogCrit($"Failed to resolve dll import: {dllName}", new System.Diagnostics.StackTrace(true));

            return 0;
        });
    }

    /// <summary>
    /// Decompresses a span of bytes from <paramref name="input"/> and writes it to <paramref name="output"/>, then returns the number of bytes decompressed
    /// </summary>
    public static int Decompress(Span<byte> input, Span<byte> output)
    {
        return fastlz_decompress(ref input.DangerousGetReference(), input.Length, ref output.DangerousGetReference(), output.Length);
    }

    /// <summary>
    /// Compresses <paramref name="input"/> at using fastlz at <paramref name="level"/> and returns it
    /// </summary>
    public static Span<byte> Compress(Span<byte> input, int level = 1)
    {
        // +10% just in case
        Span<byte> outputBuffer = new byte[input.Length + (int)((float)input.Length * 0.1f)];

        int length = fastlz_compress_level(level, ref input.DangerousGetReference(), input.Length, ref outputBuffer.DangerousGetReference());

        return outputBuffer[..length];
    }

    [LibraryImport("fastlz")]
    private static partial int fastlz_decompress(ref byte input, int inputLength, ref byte output, int outputLength);

    [LibraryImport("fastlz")]
    private static partial int fastlz_compress_level(int level, ref byte input, int inputLength, ref byte output);
}