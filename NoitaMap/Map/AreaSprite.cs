

using System.Numerics;

namespace NoitaMap.Map;

public class AreaEntitySprite : IAtlasObject
{
    public Matrix4x4 WorldMatrix { get; set; }

    public Rgba32[,]? WorkingTextureData { get; set; }

    public int TextureWidth { get; set; }

    public int TextureHeight { get; set; }

    public int TextureHash { get; set; }

    public AreaEntitySprite(string xmlFilePath, Vector2 position)
    {
        WorldMatrix = Matrix4x4.CreateScale(1f, 1f, 1f) * Matrix4x4.CreateTranslation(position.X, position.Y, 0f);
    }
}

