using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance;
using SixLabors.ImageSharp.Advanced;

namespace NoitaMap.Map;

public class Material
{
    public readonly string Name;

    public readonly Memory2D<Rgba32> MaterialTexture;

    public Material(string pathToMaterialFile)
    {
        Name = Path.GetFileNameWithoutExtension(pathToMaterialFile);

        Image<Rgba32> image = Image.Load<Rgba32>(pathToMaterialFile);

        if (image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory))
        {
            MaterialTexture = memory.AsMemory2D(image.Height, image.Width);
        }
        else
        {
            MaterialTexture = new Memory2D<Rgba32>(new Rgba32[image.Height, image.Width]);

            for (int y = 0; y < image.Height; y++)
            {
                image.DangerousGetPixelRowMemory(y).Span.CopyTo(MaterialTexture.Span.GetRowSpan(y));
            }
        }
    }

    public Material(string name, Rgba32[,] texture)
    {
        Name = name;

        MaterialTexture = texture;
    }
}
