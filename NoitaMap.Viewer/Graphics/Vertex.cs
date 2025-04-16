using System.Numerics;
using System.Runtime.InteropServices;

namespace NoitaMap.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Position;

    public Vector2 UV;
}

[StructLayout(LayoutKind.Sequential)]
public struct VertexInstance
{
    public Matrix4x4 Transform;

    public Vector2 TexturePosition;

    public Vector2 TextureSize;
}

[StructLayout(LayoutKind.Sequential)]
public struct VertexConstantBuffer
{
    public Matrix4x4 ViewProjection;    // 64 bytes
}