using System;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;

namespace NoitaMap.Game.Materials;

public class Material
{
    public const int MaterialWidth = 252;

    public const int MaterialHeight = 252;

    public const int MaterialWidthM1 = 251;

    public const int MaterialHeightM1 = 251;

    public readonly string Name;

    public readonly Rgba32[,] Colors;

    public Material(string name, Image<Rgba32> image)
    {
        Name = name;

        Colors = new Rgba32[image.Width, image.Height];

        if (image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory))
        {
            memory.Span.CopyTo(MemoryMarshal.CreateSpan(ref Colors[0, 0], image.Width * image.Height));
        }
        else
        {
            Buffer2D<Rgba32> buffer = image.Frames.RootFrame.PixelBuffer;

            for (int y = 0; y < buffer.Height; y++)
            {
                for (int x = 0; x < buffer.Width; x++)
                {
                    Colors[x, y] = buffer[x, y];
                }
            }
        }
    }
}
