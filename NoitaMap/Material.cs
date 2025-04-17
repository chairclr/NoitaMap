using CommunityToolkit.HighPerformance;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace NoitaMap;

public class Material
{
    public const int MaterialWidth = 252;

    public const int MaterialHeight = 252;

    public readonly string Name;

    public readonly Memory2D<Rgba32> MaterialPixels;

    public readonly bool IsMissing;

    public int Index;

    public Material(string pathToMaterialFile)
    {
        Name = Path.GetFileNameWithoutExtension(pathToMaterialFile);

        using Image<Rgba32> image = Image.Load<Rgba32>(pathToMaterialFile);

        MaterialPixels = new Memory2D<Rgba32>(new Rgba32[image.Height, image.Width]);

        for (int y = 0; y < image.Height; y++)
        {
            image.DangerousGetPixelRowMemory(y).Span.CopyTo(MaterialPixels.Span.GetRowSpan(y));
        }
    }

    public Material(string name, Rgba32[,] texture)
    {
        Name = name;

        MaterialPixels = texture;

        if (name == "_")
        {
            IsMissing = true;
        }
    }
}