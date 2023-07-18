using System.Collections.Generic;
using System.IO;
using osu.Framework.Allocation;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NoitaMap.Game.Materials;

public partial class MaterialProvider : IDependencyInjectionCandidate
{
    private const int MaterialCount = 444;

    private readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(MaterialCount);

    private readonly Material MissingMaterial;

    public MaterialProvider()
    {
        Configuration configuration = Configuration.Default;
        configuration.PreferContiguousImageBuffers = true;
        using Image<Rgba32> image = new Image<Rgba32>(4, 4);

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                Rgba32 color = Color.DeepPink;

                if ((x > image.Width / 2 && y > image.Height / 2) ||
                    (x <= image.Width / 2 && y <= image.Height / 2))
                {
                    color = Color.Black;
                }

                image[x, y] = color;
            }
        }

        MissingMaterial = new Material("err", image);
    }

    public void LoadMaterials(ResourceStore<byte[]> resources)
    {
        foreach (string resource in resources.GetAvailableResources())
        {
            if (resource.StartsWith("Materials/") && resource.EndsWith(".png"))
            {
                string materialName = Path.GetFileNameWithoutExtension(resource);

                Configuration configuration = Configuration.Default;

                // We can benefit from contiguous buffers by just copying data directly from the imagesharp image to the material colors
                configuration.PreferContiguousImageBuffers = true;

                using Image<Rgba32> image = Image.Load<Rgba32>(resources.GetStream(resource));

                Materials.Add(materialName, new Material(materialName, image));
            }
        }
    }

    public Material GetMaterial(string materialName)
    {
        if (Materials.TryGetValue(materialName, out Material? mat))
        {
            return mat;
        }

        return MissingMaterial;
    }

    public Material[] CreateMaterialMap(string[] materialNames)
    {
        Material[] materials = new Material[materialNames.Length];

        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = GetMaterial(materialNames[i]);
        }

        return materials;
    }
}
