
using System.Numerics;

public interface IAtlasObject
{
    public Matrix4x4 WorldMatrix { get; }

    public Rgba32[,]? WorkingTextureData { get; set; }

    public int TextureWidth { get; }

    public int TextureHeight { get; }

    public int TextureHash { get; }
}