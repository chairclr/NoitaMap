using System.Numerics;

namespace NoitaMap.Graphics;

public struct Vertex
{
    public Vector3 Position;

    public Vector2 UV;
}

public struct VertexInstance
{
    public Matrix4x4 Transform;

    public Vector2 TexturePosition;

    public Vector2 TextureSize;
}

public struct VertexConstantBuffer
{
    public Matrix4x4 ViewProjection; // 64 bytes
}
