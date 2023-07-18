using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NoitaMap;

internal static class MaterialProvider
{
    private const int MaterialCount = 444;

    private static readonly Material MissingMaterial;

    private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(MaterialCount);

    static MaterialProvider()
    {
        Texture2D missingTexture = new Texture2D(GraphicsDeviceProvider.GraphicsDevice, 2, 2);
        missingTexture.SetData(new Color[] { Color.Pink, Color.Black, Color.Pink, Color.Black });

        MissingMaterial = new Material("err", missingTexture);

        int i = 1;
        foreach (string file in Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "Materials")))
        {
            string materialName = Path.GetFileNameWithoutExtension(file);

            Materials.Add(materialName, new Material(materialName, file));

            i++;
        }
    }

    public static Material GetMaterial(string materialName)
    {
        if (Materials.TryGetValue(materialName, out Material? mat))
        {
            return mat;
        }

        return MissingMaterial;
    }

    public static Material[] CreateMaterialMap(string[] materialNames)
    {
        Material[] materials = new Material[materialNames.Length];

        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = GetMaterial(materialNames[i]);
        }

        return materials;
    }
}
