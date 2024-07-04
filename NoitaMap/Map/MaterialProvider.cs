using SixLabors.ImageSharp;

namespace NoitaMap.Map;

public class MaterialProvider
{
    private readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

    private readonly Material MissingMaterial;

    public IReadOnlyDictionary<string, Material> Material => Materials;

    public MaterialProvider()
    {
        string materialPath = Path.Combine(File.Exists(typeof(MaterialProvider).Assembly.Location) ? Path.GetDirectoryName(typeof(MaterialProvider).Assembly.Location)! : Environment.CurrentDirectory, "Assets", "Materials");

        int i = 0;
        foreach (string path in Directory.EnumerateFiles(materialPath, "*.png"))
        {
            Material material = new Material(path);

            material.Index = i++;

            Materials.Add(material.Name, material);
        }

        const int MissingMaterialWidth = 4;
        const int MissingMaterialHeight = 4;

        Rgba32[,] missingMaterialImage = new Rgba32[MissingMaterialWidth, MissingMaterialHeight];

        for (int x = 0; x < MissingMaterialWidth; x++)
        {
            for (int y = 0; y < MissingMaterialHeight; y++)
            {
                Rgba32 color = Color.DeepPink;

                if ((x > MissingMaterialWidth / 2 && y > MissingMaterialHeight / 2) ||
                    (x <= MissingMaterialHeight / 2 && y <= MissingMaterialHeight / 2))
                {
                    color = Color.Black;
                }

                missingMaterialImage[x, y] = color;
            }
        }

        MissingMaterial = new Material("_", missingMaterialImage);
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
