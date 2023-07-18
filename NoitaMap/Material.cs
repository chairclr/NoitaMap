using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NoitaMap;

internal class Material
{
    public const int MaterialWidth = 252;

    public const int MaterialHeight = 252;

    public const int MaterialWidthM1 = 251;

    public const int MaterialHeightM1 = 251;

    public readonly string Name;

    public readonly Texture2D Texture;

    public readonly Color[] Colors;

    public Material(string name, string texturePath)
    {
        Name = name;

        Texture = Texture2D.FromFile(GraphicsDeviceProvider.GraphicsDevice, texturePath);

        Colors = new Color[Texture.Width * Texture.Height];

        Texture.GetData(Colors);
    }

    public Material(string name, Texture2D texture)
    {
        Name = name;

        Texture = texture;

        Colors = new Color[Texture.Width * Texture.Height];

        Texture.GetData(Colors);
    }
}
