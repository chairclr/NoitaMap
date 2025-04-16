using NoitaMap.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NoitaMap;

public class MaterialProvider
{
    private readonly Dictionary<string, Material> _materials = new();

    private readonly Material _missingMaterial;

    public IReadOnlyDictionary<string, Material> Material => _materials;

    public MaterialProvider()
    {
        Log.LogInfo("Loading material image files");

        string materialPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Materials");

        int i = 0;
        foreach (string path in Directory.EnumerateFiles(materialPath, "*.png"))
        {
            Material material = new(path)
            {
                Index = i++
            };

            _materials.Add(material.Name, material);
        }

        Rgba32[,] missingMaterialImage = new Rgba32[1, 1];
        missingMaterialImage[0, 0] = Color.DeepPink;

        _missingMaterial = new Material("_", missingMaterialImage);
    }

    public Material GetMaterial(string materialName)
    {
        if (_materials.TryGetValue(materialName, out Material? mat))
        {
            return mat;
        }

        return _missingMaterial;
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