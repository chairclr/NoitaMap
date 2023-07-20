using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;

namespace NoitaMap.Graphics;

public class QuadVertexBuffer<T>
    where T : unmanaged
{
    private readonly GraphicsDevice GraphicsDevice;

    //private readonly DeviceBuffer IndexBuffer;

    private readonly DeviceBuffer VertexBuffer;

    private readonly ResourceSet ResourceSet;

    public QuadVertexBuffer(GraphicsDevice graphicsDevice, Vector2 size, Func<Vector2, Vector2, T> constructVert, ResourceSet resourceSet)
    {
        GraphicsDevice = graphicsDevice;

        ResourceSet = resourceSet;

        //IndexBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription()
        //{
        //    SizeInBytes = (uint)(Unsafe.SizeOf<ushort>() * 6),
        //    Usage = BufferUsage.IndexBuffer | BufferUsage.Dynamic
        //});

        //Span<ushort> quadIndicies = stackalloc ushort[6] { 0, 1, 2, 0, 2, 3 };

        //GraphicsDevice.UpdateBuffer(IndexBuffer, 0, quadIndicies);

        VertexBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription()
        {
            SizeInBytes = (uint)(Unsafe.SizeOf<T>() * 6),
            Usage = BufferUsage.VertexBuffer | BufferUsage.Dynamic
        });

        Span<T> verts = stackalloc T[6];

        size /= 2f;

        verts[0] = constructVert(-size, Vector2.Zero);
        verts[1] = constructVert(new Vector2(size.X, -size.X), Vector2.UnitX);
        verts[2] = constructVert(new Vector2(-size.Y, size.Y), Vector2.UnitY);

        verts[3] = constructVert(new Vector2(-size.Y, size.Y), Vector2.UnitY);
        verts[4] = constructVert(new Vector2(size.X, -size.X), Vector2.UnitX);
        verts[5] = constructVert(size, Vector2.One);

        GraphicsDevice.UpdateBuffer(VertexBuffer, 0, verts);
    }

    public void Draw(CommandList commandList)
    {
        commandList.SetGraphicsResourceSet(0, ResourceSet);

        commandList.SetVertexBuffer(0, VertexBuffer);

        commandList.Draw(6);
    }
}
